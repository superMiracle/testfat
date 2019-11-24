using System;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FATsys.Utils;
using FATsys.TraderType;
using ylink;

namespace FATsys.Site.CN
{
    class CSiteSHGold : CSite
    {
        public override bool OnInit()
        {
            bool bRet = CSHGoldAPI.sg_login(m_sID, m_sPwd);
            if (bRet)
                CFATLogger.output_proc("Shanghai gold Login Success!");
            else
                CFATLogger.output_proc("Shanghai gold Login Fail!");

            return base.OnInit();
        }

        public override EERROR OnTick()
        {
            double dBid = 0;
            double dAsk = 0;
            int nBidAmount = 0;
            int nAskAmount = 0;

            foreach (string sSymbol in m_sSymbols)
            {
                CSHGoldAPI.sg_getRates(sSymbol, ref dAsk, ref dBid, ref nAskAmount, ref nBidAmount);
                m_rates[sSymbol].dAsk = dAsk;
                m_rates[sSymbol].dBid = dBid;
                m_rates[sSymbol].m_dtTime = CFATCommon.m_dtCurTime;
            }

            return base.OnTick();
        }

        public override void OnDeInit()
        {
            bool bRet = CSHGoldAPI.sg_logout();
            if (bRet)
                CFATLogger.output_proc("Shanghai gold Logout Success!");
            else
                CFATLogger.output_proc("Shanghai gold Logout Fail!");

            base.OnDeInit();
        }

        public override bool updateRealPositions()
        {
            if (CFATManager.m_nRunMode != ERUN_MODE.REALTIME)
                return true;

            TPosItem posItem;
            m_lstPos_real.Clear();

            string sSymbol = "";
            double dPrice = 0;
            int nLots = 0;

            CSHGoldAPI.sg_getPositionList();
            int nPosCnt = CSHGoldAPI.sg_getPositionCount();
            string sMsg = string.Format("SHGold Position count = {0}\r\n", nPosCnt);
            string sCmd = "";
            for ( int i = 0; i < nPosCnt; i ++ )
            {
                CSHGoldAPI.sg_getPositionInfo(i, ref sSymbol, ref dPrice, ref nLots, ref sCmd);
                sMsg += string.Format("symbol = {0}, open price = {1}, lots = {2}\r\n", sSymbol, dPrice, nLots);
                if (nLots == 0)
                    continue;
                posItem = new TPosItem();
                posItem.m_sSymbol = sSymbol;
                posItem.m_dOpenPrice_exc = dPrice;
                posItem.m_nCmd = TRADER.string2Cmd(sCmd);
                if ( sCmd == "BUY" )
                {//Buy
                    posItem.m_nCmd = ETRADER_OP.BUY;
                    posItem.m_dLots_exc = nLots;
                }
                else
                {//Sell
                    posItem.m_nCmd = ETRADER_OP.SELL;
                    posItem.m_dLots_exc = nLots;
                }

                m_lstPos_real.Add(posItem);
            }
            //CFATLogger.output_proc(sMsg);
            return true;
        }

        public override EFILLED_STATE reqOrder(string sSymbol, ETRADER_OP nCmd, ref double dLots, ref double dPrice, EORDER_TYPE nOrderType, string sLogicID, string sComment = "",
            double dLots_exc = 0, double dPrice_exc = 0, DateTime dtTime_exc = default(DateTime))
        {
            if (CFATManager.m_nRunMode == ERUN_MODE.SIMULATION)
                return base.reqOrder(sSymbol, nCmd, ref dLots, ref dPrice, nOrderType, sLogicID, sComment, dLots, dPrice, DateTime.Now);

            string sCmd = TRADER.cmd2String(nCmd);
            double dLots_req = dLots;
            double dPrice_req = dPrice;

            bool bRet = CSHGoldAPI.sg_newOrder(sSymbol, sCmd, ref dPrice, ref dLots);
            if (!bRet)
                return  EFILLED_STATE.FAIL;

            return base.reqOrder(sSymbol, nCmd, ref dLots_req, ref dPrice_req, nOrderType, sLogicID, sComment, dLots, dPrice, DateTime.Now);
        }

    }
}
