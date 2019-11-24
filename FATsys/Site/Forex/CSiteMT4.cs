using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using FATsys.Utils;
using FATsys.TraderType;

using MT4ApiDLL;


namespace FATsys.Site.Forex
{
    class CSiteMT4 : CSite
    {
        CMT4ApiDLL m_mt4ApiDLL = new CMT4ApiDLL();
        public override bool OnInit()
        {
            CFATLogger.output_proc("connecting to pipe : " + m_sPipServerName);
            if (!m_mt4ApiDLL.connectToMT4(m_sPipServerName))
            {
                CFATLogger.output_proc(string.Format("site = {0} : Cannot connect to PIP, pipe name = {1}, please check MT4 and parameters of EA!",
                    m_sSiteName, m_sPipServerName));
                return false;
            }
            CFATLogger.output_proc("Connect to MT4 pipe OK! site = " + m_sSiteName);

            for ( int i = 0; i < m_sSymbols.Count; i ++ )
            {
                m_mt4ApiDLL.mt4_registerSymbol(m_sSymbols[i]);
            }
            m_mt4ApiDLL.mt4_startSubscribe();
            return base.OnInit();
        }

        public override EERROR OnTick()
        {
            //Get Rates From API
            double dBid = 0;
            double dAsk = 0;
            
            foreach (string sSymbol in m_sSymbols)
            {
                if (!m_mt4ApiDLL.mt4_getRates(sSymbol, ref dAsk, ref dBid))
                {
                    CFATLogger.output_proc("mt4_getRates : Error!");
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
            m_mt4ApiDLL.mt4_disConnect();
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


            List<TMT4PosItem> lstPosMT4 = m_mt4ApiDLL.mt4_getPositions();

            TPosItem posItem;
            m_lstPos_real.Clear();
            //string sLog = string.Format("update position : {0}\r\n", m_sSiteName);
            for ( int i = 0; i < lstPosMT4.Count; i ++ )
            {
                posItem = new TPosItem();
                posItem.m_nTicket = lstPosMT4[i].m_nTicket;
                posItem.m_sSymbol = lstPosMT4[i].m_sSymbol;
                posItem.m_dOpenPrice_exc = lstPosMT4[i].m_dOpenPrice;
                posItem.m_dLots_exc = lstPosMT4[i].m_dLots;
                posItem.m_dCommission = lstPosMT4[i].m_dCommission;
                posItem.m_nCmd = (ETRADER_OP)lstPosMT4[i].m_nCmd;
                m_lstPos_real.Add(posItem);
                //sLog += string.Format("ticket={0}, symbol={1}, cmd = {2}, openPrice={3}, lots={4}, commission={5}\r\n", 
                //    posItem.m_nTicket, posItem.m_sSymbol, posItem.m_nCmd, posItem.m_dOpenPrice, posItem.m_dLots, posItem.m_dCommission);
            }
            //CFATLogger.output_proc(sLog);
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
                    CFATLogger.output_proc(string.Format("close item : ticket = {0}, lots = {1}", posItem.m_nTicket, posItem.m_dLots_req));
                    bRet = m_mt4ApiDLL.mt4_reqCloseOrder(posItem.m_nTicket, ref posItem.m_dLots_exc, ref dPrice);
                    dRemainLots -= posItem.m_dLots_exc;
                }
                else
                {
                    CFATLogger.output_proc(string.Format("close item : ticket = {0}, lots = {1}", posItem.m_nTicket, dRemainLots));
                    bRet = m_mt4ApiDLL.mt4_reqCloseOrder(posItem.m_nTicket, ref dRemainLots, ref dPrice);
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
                bRet = m_mt4ApiDLL.mt4_reqNewOrder(sSymbol, "BUY", ref dLots, ref dPrice);

            if (nCmd == ETRADER_OP.SELL)
                bRet = m_mt4ApiDLL.mt4_reqNewOrder(sSymbol, "SELL", ref dLots, ref dPrice);

            if (nCmd == ETRADER_OP.BUY_CLOSE || nCmd == ETRADER_OP.SELL_CLOSE)
                bRet = closeOrder(sSymbol, nCmd, ref dLots, ref dPrice, nOrderType, sLogicID);

            if (!bRet)
                return  EFILLED_STATE.FAIL;
            return base.reqOrder(sSymbol, nCmd, ref dLots_req, ref dPrice_req, nOrderType, sLogicID, sComment, dLots, dPrice, DateTime.Now);
        }
    }
}
