using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MPApiDLL;
using MT4ApiDLL;
using FATsys.Utils;
using FATsys.TraderType;

namespace FATsys.Site.Forex
{
    class CSiteMoneyPartners:CSite
    {
        CMPApiDLL m_mpApiDLL = new CMPApiDLL();
        public override bool OnInit()
        {
            CFATLogger.output_proc("connecting to money-partners : " + m_sID);
            if (!m_mpApiDLL.connectToMP(m_sID, m_sPwd))
            {
                CFATLogger.output_proc(string.Format("site = {0} : Cannot connect to money-partners, loginId = {1}, please check connection!",
                    m_sSiteName, m_sID));
                return false;
            }
            CFATLogger.output_proc("Connect to MP pipe OK! site = " + m_sSiteName);

            for (int i = 0; i < m_sSymbols.Count; i++)
            {
                m_mpApiDLL.MP_registerSymbol(m_sSymbols[i], m_sID, m_sPwd);
            }
            m_mpApiDLL.MP_startSubscribe();
            return base.OnInit();
        }

        public override EERROR OnTick()
        {
            //Get Rates From API
            double dBid = 0;
            double dAsk = 0;

            foreach (string sSymbol in m_sSymbols)
            {
                if (!m_mpApiDLL.MP_getRates(sSymbol, ref dAsk, ref dBid))
                {
                    CFATLogger.output_proc("MP_getRates : Error!");
                    return EERROR.RATE_INVALID;
                }
                m_rates[sSymbol].dAsk = dAsk;
                m_rates[sSymbol].dBid = dBid;
                m_rates[sSymbol].m_dtTime = CFATCommon.m_dtCurTime;
            }

            return base.OnTick();
        }

        public override void OnDeInit()
        {
            //Logout 
            m_mpApiDLL.MP_disConnect();
            base.OnDeInit();
        }

        private bool isValidCloseCommand(ETRADER_OP posCmd, ETRADER_OP reqCmd)
        {
            if (reqCmd == ETRADER_OP.BUY_CLOSE && posCmd == ETRADER_OP.BUY)
                return true;

            if (reqCmd == ETRADER_OP.SELL_CLOSE && posCmd == ETRADER_OP.SELL)
                return true;
            return false;
        }

        public override bool updateRealPositions()
        {
            if (CFATManager.m_nRunMode != ERUN_MODE.REALTIME)
                return true;


            List<TMPPosItem> lstPosMT4 = m_mpApiDLL.MP_getPositions();
            TPosItem posItem;
            m_lstPos_real.Clear();

            for (int i = 0; i < lstPosMT4.Count; i++)
            {
                posItem = new TPosItem();
                posItem.m_sTicket = lstPosMT4[i].m_sTicket;
                posItem.m_sSymbol = lstPosMT4[i].m_sSymbol;
                posItem.m_dOpenPrice_exc = lstPosMT4[i].m_dOpenPrice;
                posItem.m_dLots_exc = lstPosMT4[i].m_dLots;
                posItem.m_dCommission = lstPosMT4[i].m_dCommission;
                posItem.m_nCmd = (ETRADER_OP)lstPosMT4[i].m_nCmd;
                m_lstPos_real.Add(posItem);

            }

            return true;
        }

        private bool closeOrder(string sSymbol, ETRADER_OP nCmd, ref double dLots, ref double dPrice, EORDER_TYPE nOrderType, string sLogicID, string sComment = "")
        {

            CFATLogger.output_proc(string.Format("close order : site = {0}, sym= {1}, cmd = {2}, lots = {3}", m_sSiteName, sSymbol, nCmd, dLots));
            double dRemainLots = dLots;
            TPosItem posItem;
            bool bRet = true;
            for (int i = 0; i < m_lstPos_real.Count; i++)
            {
                posItem = m_lstPos_real[i];

                if (posItem.m_sSymbol != sSymbol) continue;
                //if (posItem.m_nLogicID != nLogicID) continue;

                if (!isValidCloseCommand(posItem.m_nCmd, nCmd)) continue;

                if (dRemainLots >= posItem.m_dLots_exc)
                {
                    CFATLogger.output_proc(string.Format("close item : ticket = {0}, lots = {1}", posItem.m_sTicket, posItem.m_dLots_req));
                    bRet = m_mpApiDLL.MP_reqCloseOrder(posItem.m_sTicket, ref posItem.m_dLots_exc, ref dPrice);
                    dRemainLots -= posItem.m_dLots_exc;
                }
                else
                {
                    CFATLogger.output_proc(string.Format("close item : ticket = {0}, lots = {1}", posItem.m_sTicket, dRemainLots));
                    bRet = m_mpApiDLL.MP_reqCloseOrder(posItem.m_sTicket, ref dRemainLots, ref dPrice);
                    dRemainLots = 0;
                }

                if (Math.Abs(dRemainLots) < CFATCommon.ESP)
                    break;
            }

            return true;
        }

        public override EFILLED_STATE reqOrder(string sSymbol, ETRADER_OP nCmd, ref double dLots, ref double dPrice, EORDER_TYPE nOrderType, string sLogicID, string sComment = "",
            double dLots_exc = 0, double dPrice_exc = 0, DateTime dtTime_exc = default(DateTime))
        {
            if (CFATManager.m_nRunMode == ERUN_MODE.SIMULATION)
                return base.reqOrder(sSymbol, nCmd, ref dLots, ref dPrice, nOrderType, sLogicID, sComment, dLots, dPrice, DateTime.Now);
            bool bRet = true;

            double dLots_req = dLots;
            double dPrice_req = dPrice;

            //Send Request newOrder or Close or
            if (nCmd == ETRADER_OP.BUY)
                bRet = m_mpApiDLL.MP_reqNewOrder(sSymbol, "BUY", ref dLots, ref dPrice);

            if (nCmd == ETRADER_OP.SELL)
                bRet = m_mpApiDLL.MP_reqNewOrder(sSymbol, "SELL", ref dLots, ref dPrice);

            if (nCmd == ETRADER_OP.BUY_CLOSE || nCmd == ETRADER_OP.SELL_CLOSE)
                bRet = closeOrder(sSymbol, nCmd, ref dLots, ref dPrice, nOrderType, sLogicID);

            if (!bRet)
                return EFILLED_STATE.FAIL;
            return base.reqOrder(sSymbol, nCmd, ref dLots_req, ref dPrice_req, nOrderType, sLogicID, sComment, dLots, dPrice, DateTime.Now);
        }
    }
}
