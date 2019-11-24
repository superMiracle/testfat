using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FATsys.Utils;
using FATsys.TraderType;

using IBApiDLL;
namespace FATsys.Site.Forex
{
    class CSiteIB : CSite
    {
        CIBApi apiIB = new CIBApi();

        string m_sHost = "127.0.0.1";
        int m_nPort = 4001;
        int m_nClientID = 1;

        public override bool OnInit()
        {
            CFATLogger.output_proc(string.Format("IB Init {0}, {1}, {2}--->", m_sHost, m_nPort, m_nClientID));
            if (!apiIB.connectToIB(m_sHost, m_nPort, m_nClientID))
            {
                CFATLogger.output_proc(string.Format("site = {0} : Cannot connect to IB= {1}", m_sSiteName));
                return false;
            }
            Thread.Sleep(1000);
            
            foreach (string sSymbol in m_sSymbols)
            {
                CFATLogger.output_proc("IB subscribe : " + sSymbol);
                apiIB.subScribeMarketData(sSymbol);
            }
            Thread.Sleep(2000);
            return base.OnInit();
        }

        public override EERROR OnTick()
        {
            //Get Rates From API
            double dBid = 0;
            double dAsk = 0;
            
            foreach (string sSymbol in m_sSymbols)
            {
                apiIB.getRates(sSymbol, ref dBid, ref dAsk);
                if (dBid < CFATCommon.ESP || dAsk < CFATCommon.ESP)
                {
                    CFATLogger.output_proc("IB_getRates : Error!");
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
            apiIB.disConnect();
            base.OnDeInit();
        }

        public override EFILLED_STATE reqOrder(string sSymbol, ETRADER_OP nCmd, ref double dLots, ref double dPrice, EORDER_TYPE nOrderType, string sLogicID, string sComment = "",
                double dLots_exc = 0, double dPrice_exc = 0, DateTime dtTime_exc = default(DateTime))
        {
            if (CFATManager.m_nRunMode == ERUN_MODE.SIMULATION)
                return base.reqOrder(sSymbol, nCmd, ref dLots, ref dPrice, nOrderType, sLogicID, sComment, dLots, dPrice, DateTime.Now);

            double dAmount = 0;

            string sCmd = "";

            if (nCmd == ETRADER_OP.BUY || nCmd == ETRADER_OP.SELL_CLOSE)
                sCmd = "BUY";
            if (nCmd == ETRADER_OP.SELL || nCmd == ETRADER_OP.BUY_CLOSE)
                sCmd = "SELL";

            dAmount = dLots * getContractSize(sSymbol);

            CFATLogger.output_proc(string.Format("IB req : sym={0},price={1}, amount={2}, cmd = {3}", sSymbol, dPrice, dAmount, sCmd));
            double dPrice_req = dPrice;
            double dLots_req = dLots;
            bool bRet = apiIB.reqOrder(sSymbol, sCmd, ref dAmount, ref dPrice);
            dLots = dAmount / getContractSize(sSymbol);
            CFATLogger.output_proc(string.Format("IB response : result = {0}, sym={1},price={2}, amount={3}, ", bRet, sSymbol, dPrice, dAmount));
            //if ( nRet == TORDER_RESULT.FILLED) //HSM_???
            if (!bRet)
                return  EFILLED_STATE.FAIL;
            return base.reqOrder(sSymbol, nCmd, ref dLots_req, ref dPrice_req, nOrderType, sLogicID, sComment, dLots, dPrice, DateTime.Now);
            //return false;
        }

        public override bool updateRealPositions()
        {
            if (CFATManager.m_nRunMode != ERUN_MODE.REALTIME)
                return true;

            apiIB.getPositons();

            TPosItem posItem;
            m_lstPos_real.Clear();
            //string sLog = string.Format("update position : {0}\r\n", m_sSiteName);
            foreach (TIBPositionItem ibPosItem in CIBApi.g_lstPositions)
            {
                if (Math.Abs(ibPosItem.dLots) < CFATCommon.ESP)
                    continue;
                posItem = new TPosItem();
                posItem.m_sSymbol = ibPosItem.sSymbol;
                posItem.m_dOpenPrice_exc = ibPosItem.dOpenPrice;
                if (ibPosItem.dLots > 0)
                {
                    posItem.m_dLots_exc = ibPosItem.dLots / getContractSize(ibPosItem.sSymbol);
                    posItem.m_nCmd = ETRADER_OP.BUY;
                }
                else
                {
                    posItem.m_dLots_exc = ibPosItem.dLots * (-1) / getContractSize(ibPosItem.sSymbol);
                    posItem.m_nCmd = ETRADER_OP.SELL;
                }

                m_lstPos_real.Add(posItem);
                //sLog += string.Format("ticket={0}, symbol={1}, cmd = {2}, openPrice={3}, lots={4}, commission={5}\r\n",
                //    posItem.m_nTicket, posItem.m_sSymbol, posItem.m_nCmd, posItem.m_dOpenPrice, posItem.m_dLots, posItem.m_dCommission);
            }
            //CFATLogger.output_proc(sLog);
            return true;
        }


    }
}
