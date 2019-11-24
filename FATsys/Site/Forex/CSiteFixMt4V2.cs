using System;
using System.Windows.Forms;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FATsys.Utils;
using FATsys.TraderType;
using FIXApiDLLV2;
using MT4ApiDLL;

/// <summary>
/// Get Rates via FIX 4.4 
/// New Order, Account Info, Position list is MT4
/// </summary>

namespace FATsys.Site.Forex
{
    class CSiteFixMt4V2 : CSite
    {
        CMT4ApiDLL m_mt4ApiDLL = new CMT4ApiDLL();
        CFixAPI m_fixApi_data;
        CFixAPI m_fixApi_trade;

        string m_sFixConfig_data = "";
        string m_sFixConfig_trade = "";

        string m_sFixAcc = ""; //Account name for Global Prime demo

        public override void addSym_fix(string sSym, string sKey)
        {
            m_sSymbols_fix.Add(sKey, sSym);
        }

        public override void addSym_min_max(string sKey, double dMin, double dMax)
        {
            FIXApiDLL.TRateRange rateRange = new FIXApiDLL.TRateRange();
            rateRange.dMax = dMax;
            rateRange.dMin = dMin;
            m_rateRange_fix.Add(sKey, rateRange);
        }

        public override void setConfigFile_fix(string sConfig_data, string sConfig_trade)
        {
            m_sFixConfig_data = sConfig_data;
            m_sFixConfig_trade = sConfig_trade;
        }
        public override void setFixAccount(string sFixAcc)
        {
            m_sFixAcc = sFixAcc;
        }

        public override bool OnInit()
        {
            CFATLogger.output_proc("connecting to pipe : " + m_sPipServerName);
            //MT4 init
            if (!m_mt4ApiDLL.connectToMT4(m_sPipServerName))
            {
                CFATLogger.output_proc(string.Format("site = {0} : Cannot connect to PIP, pipe name = {1}, please check MT4 and parameters of EA!",
                    m_sSiteName, m_sPipServerName));
                return false;
            }
            CFATLogger.output_proc("Connect to MT4 pipe OK! site = " + m_sSiteName);
            //FIX init
            string sConfigFile;


            sConfigFile = Path.Combine(Application.StartupPath, "Config\\Fix", m_sFixConfig_trade);
            CFATLogger.output_proc("loading : " + sConfigFile);
            m_fixApi_trade = new CFixAPI(sConfigFile);

            if (!m_fixApi_trade.fixLogin())
            {
                CFATLogger.output_proc(string.Format("Connect to fix trade server is fail! : site name = {0}", m_sSiteName));
                return false;
            }

            sConfigFile = Path.Combine(Application.StartupPath, "Config\\Fix", m_sFixConfig_data);
            CFATLogger.output_proc("loading : " + sConfigFile);

            m_fixApi_data = new CFixAPI(sConfigFile);

            if (!m_fixApi_data.fixLogin())
            {
                CFATLogger.output_proc(string.Format("Connect to fix data server is fail! : site name = {0}", m_sSiteName));
                return false;
            }
            CFATLogger.output_proc("Connect to Fix OK!");
            //subscrib rates
            FIXApiDLL.TRateRange rateRange;
            foreach (KeyValuePair<string, string> entry in m_sSymbols_fix)
            {
                rateRange = m_rateRange_fix[entry.Key];
                m_fixApi_data.startSubscribe(entry.Value, entry.Key, rateRange.dMin, rateRange.dMax);
                Thread.Sleep(500);
            }
            return base.OnInit();
        }

        public override EERROR OnTick()
        {
            //Get Rates From API
            double dBid = 0;
            double dAsk = 0;
            string sSymbol = "";
            foreach (KeyValuePair<string, string> entry in m_sSymbols_fix)
            {
                //FIX
                sSymbol = entry.Value;
                m_fixApi_data.getRates(entry.Key, ref dAsk, ref dBid);
                m_rates[sSymbol].dAsk = dAsk; //
                m_rates[sSymbol].dBid = dBid;
                m_rates[sSymbol].m_dtTime = CFATCommon.m_dtCurTime;
            }

            return base.OnTick();
        }

        public override void OnDeInit()
        {
            //Logout 
            m_mt4ApiDLL.mt4_disConnect();

            m_fixApi_data.Stop();
            m_fixApi_trade.Stop();

            base.OnDeInit();
        }

        private bool isHedgePosition(string sSymbol, double dLots, ETRADER_OP nCmd)
        {
            foreach (TPosItem posItem in m_lstPos_real)
            {
                if (posItem.m_sSymbol != sSymbol)
                    continue;
                if (Math.Abs(posItem.m_dLots_exc - dLots) > CFATCommon.ESP)
                    continue;

                if (posItem.m_nCmd != nCmd)
                {//Hedge position
                    m_lstPos_real.Remove(posItem);
                    return true;
                }
            }
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
            for (int i = 0; i < lstPosMT4.Count; i++)
            {
                if (isHedgePosition(lstPosMT4[i].m_sSymbol, lstPosMT4[i].m_dLots, (ETRADER_OP)lstPosMT4[i].m_nCmd))
                    continue;

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
                    CFATLogger.output_proc(string.Format("close item : ticket = {0}, lots = {1}", posItem.m_nTicket, posItem.m_dLots_exc));
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

        //public override bool reqOrder_mt4(...
        public EFILLED_STATE reqOrder_mt4(string sSymbol, ETRADER_OP nCmd, ref double dLots, ref double dPrice, EORDER_TYPE nOrderType, string sLogicID, string sComment = "",
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

        public override EFILLED_STATE reqOrder(string sSymbol, ETRADER_OP nCmd, ref double dLots, ref double dPrice, EORDER_TYPE nOrderType, string sLogicID, string sComment = "",
            double dLots_exc = 0, double dPrice_exc = 0, DateTime dtTime_exc = default(DateTime))
        {
            if (CFATManager.m_nRunMode == ERUN_MODE.SIMULATION)
                return base.reqOrder(sSymbol, nCmd, ref dLots, ref dPrice, nOrderType, sLogicID, sComment, dLots, dPrice, DateTime.Now);
            //Send Request newOrder or Close or
            //MessageBox.Show("CSiteFixMt4::reqOrder !");
            string sOrderID = "FAT" + DateTime.Now.ToString("yyyyMMddHHmmssfff");
            double dAmount = 0;

            string sCmd = "";

            if (nCmd == ETRADER_OP.BUY || nCmd == ETRADER_OP.SELL_CLOSE)
                sCmd = "BUY";
            if (nCmd == ETRADER_OP.SELL || nCmd == ETRADER_OP.BUY_CLOSE)
                sCmd = "SELL";

            dAmount = dLots * getContractSize(sSymbol);

            //MessageBox.Show("Before QueryEnterOrder!");
            CFATLogger.output_proc(string.Format("Fix req : sym={0},price={1}, amount={2}, order id= {3}, acc = {4}, cmd = {5}", sSymbol, dPrice, dAmount, sOrderID, m_sFixAcc, sCmd));
            double dPrice_req = dPrice;
            double dLots_req = dLots;
            int nRet = m_fixApi_trade.QueryEnterOrder(sSymbol, sCmd, ref dPrice, ref dAmount, sOrderID, m_sFixAcc);
            dLots = dAmount / getContractSize(sSymbol);
            CFATLogger.output_proc(string.Format("Fix response : result = {0}, sym={1},price={2}, amount={3}, ", nRet, sSymbol, dPrice, dAmount));
            //if ( nRet == TORDER_RESULT.FILLED) //HSM_???
            return base.reqOrder(sSymbol, nCmd, ref dLots_req, ref dPrice_req, nOrderType, sLogicID, sComment, dLots, dPrice, DateTime.Now);
            //return false;
        }

        private bool isValidCloseCommand(ETRADER_OP posCmd, ETRADER_OP reqCmd)
        {
            if (reqCmd == ETRADER_OP.BUY_CLOSE && posCmd == ETRADER_OP.BUY)
                return true;

            if (reqCmd == ETRADER_OP.SELL_CLOSE && posCmd == ETRADER_OP.SELL)
                return true;
            return false;
        }

    }
}
