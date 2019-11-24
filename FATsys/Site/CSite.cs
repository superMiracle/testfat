using System;
using System.Windows.Forms;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using FATsys.TraderType;
using FATsys.Utils;
using FIXApiDLL;

namespace FATsys.Site
{
    public class CSite
    {
        public string m_sID;
        public string m_sPwd;

        public string m_sSiteName = "";

        public string m_sPipServerName = ""; //For MT4 API

        public string m_sPipe_order = ""; //For MT5 API
        public string m_sPipe_rate = ""; //For MT5 API

        public string m_sBrokerID = ""; //For China future
        public string m_sMDAddr = ""; //For China future

        public int m_nPublishedPosCnt = 0;

        public Dictionary<string, TRatesTick> m_rates = new Dictionary<string, TRatesTick>();
        public Dictionary<string, CCacheData> m_rates_cache = new Dictionary<string, CCacheData>();
        public Dictionary<string, double> m_contractSizes = new Dictionary<string, double>();
        public Dictionary<string, double> m_commissionPercent = new Dictionary<string, double>();


        public List<string> m_sSymbols = new List<string>();
        public Dictionary<string, string> m_sSymbols_fix = new Dictionary<string, string>();
        public Dictionary<string, TRateRange> m_rateRange_fix = new Dictionary<string, TRateRange>();

        public  List<TPosItem> m_lstPos_vt = new List<TPosItem>();
        public List<TPosItem> m_lstPosHistory_vt = new List<TPosItem>();

        public List<TPosItem> m_lstPos_real = new List<TPosItem>();


        public TAccountInfo m_accInfo = new TAccountInfo();
        public TSiteReport m_report = new TSiteReport();
        // added by cmh
        public enum SITE_STATUS
        {
            NONE = 0,
            PROCESSING = 1
        }
        
        public SITE_STATUS m_status;

        public void setStatus(SITE_STATUS status)
        {
            m_status = status;
        }

        public SITE_STATUS getStatus()
        {
            return m_status;
        }
        // ---

        public void setName(string sSiteName)
        {
            m_sSiteName = sSiteName;
        }

        public void setPipeServerName(string sPipeServer)
        {
            m_sPipServerName = sPipeServer;
        }
        public void setPipeServerName_MT5(string sPipe_rate, string sPipe_order)
        {
            m_sPipe_order = sPipe_order;
            m_sPipe_rate = sPipe_rate;
        }

        public void setID_PWD(string sID, string sPwd)
        {
            m_sID = sID;
            m_sPwd = sPwd;
        }
        public void addSym(string sSym, double dContractSize, double dCommissionPercent)
        {
            m_rates.Add(sSym, new TRatesTick());
            m_rates_cache.Add(sSym, new CCacheData());
            m_sSymbols.Add(sSym);
            m_contractSizes.Add(sSym, dContractSize);
            m_commissionPercent.Add(sSym, dCommissionPercent);

        }
        
        public double getCommissionPercent(string sSym)
        {
            if (!m_commissionPercent.ContainsKey(sSym))
                return 0;
            return m_commissionPercent[sSym];
        }

        public double getContractSize(string sSym)
        {
            if (!m_contractSizes.ContainsKey(sSym))
                return 1.0;
            return m_contractSizes[sSym];
        }
        public virtual void addSym_fix(string sSym, string sKey){ }
        public virtual void addSym_min_max(string sKey, double dMin, double dMax) { }
        public virtual void setConfigFile_fix(string sConfig_data, string sConfig_trade) { }
        public virtual void setFixAccount(string sFixAcc) { }
        public virtual void loadRates_Tick() { }

        public virtual bool OnInit()
        {
            CFATLogger.output_proc(string.Format("Site Init : {0} ------>", m_sSiteName));
            m_accInfo.init();
            m_report.init();
            m_lstPos_vt.Clear();
            m_lstPos_real.Clear();
            m_lstPosHistory_vt.Clear();
            m_status = SITE_STATUS.NONE;
            foreach (KeyValuePair<string, CCacheData> entry in m_rates_cache)
                entry.Value.OnInit();

            CFATLogger.output_proc("updateRealPositions : ");
            return updateRealPositions();
        }

        public virtual void OnDeInit()
        {
            CFATLogger.output_proc(string.Format("Site DeInit : {0} <------", m_sSiteName));
            foreach (KeyValuePair<string, CCacheData> entry in m_rates_cache)
                entry.Value.OnDeInit();
        }

        public virtual bool updateRealPositions()
        {
            return true;
        }

        public void clearVirtualPositions(string sSymbol)
        {
            foreach (TPosItem posItem in m_lstPos_vt)
            {
                if (posItem.m_sSymbol == sSymbol)
                {
                    m_lstPos_vt.Remove(posItem);
                    break;
                }
            }
        }

        public double getPosLots_Total_vt(string sSymbol)
        {
            double dRet = 0;
            foreach(TPosItem posItem in m_lstPos_vt)
            {
                if (posItem.m_sSymbol == sSymbol)
                {
                    if ( posItem.m_nCmd == ETRADER_OP.BUY)
                        dRet += posItem.m_dLots_exc;
                    else
                        dRet -= posItem.m_dLots_exc;
                }
            }
            return dRet;
        }
        public double getPosLots_vt(string sSymbol, int nIndex)
        {
            double dRet = 0;
            int nID = -1;
            foreach (TPosItem posItem in m_lstPos_vt)
            {
                if (posItem.m_sSymbol == sSymbol)
                {
                    nID++;
                    dRet = posItem.m_dLots_exc;
                }

                if (nID == nIndex) break;
            }
            return dRet;
        }

        public double getPosLots_Total_real(string sSymbol)
        {
            double dRet = 0;
            foreach (TPosItem posItem in m_lstPos_real)
            {
                if (posItem.m_sSymbol == sSymbol)
                {
                    if ( posItem.m_nCmd == ETRADER_OP.BUY)
                        dRet += posItem.m_dLots_exc;
                    else
                        dRet -= posItem.m_dLots_exc;
                }
            }
            return dRet;
        }
        public double getPosLots_real(string sSymbol, int nIndex)
        {
            double dRet = 0;
            int nID = -1;
            foreach (TPosItem posItem in m_lstPos_real)
            {
                if (posItem.m_sSymbol == sSymbol)
                {
                    nID++;
                    dRet = posItem.m_dLots_exc;
                }

                if (nID == nIndex) break;
                
            }
            return dRet;
        }

        public double getSymbolLots_real(string sSymbol)
        {
            double dRet = 0;
            foreach (TPosItem posItem in m_lstPos_real)
            {
                if (posItem.m_sSymbol == sSymbol)
                {
                    if (posItem.m_nCmd == ETRADER_OP.BUY)
                        dRet += posItem.m_dLots_exc;
                    else
                        dRet -= posItem.m_dLots_exc;
                }
            }
            return dRet;
        }
        public void matchPosReal2Virtual(string sSymbol, string sLogicID)
        {
            CFATLogger.output_proc(string.Format("matchPosReal2Virtual : {0}, real pos cnt = {1}", m_sSiteName, m_lstPos_real.Count));
            foreach(TPosItem posItem in m_lstPos_real)
            {
                if (posItem.m_sSymbol == sSymbol)// && Math.Abs(getSymbolLots_real(sSymbol)) > CFATCommon.ESP)
                {
                    CFATLogger.output_proc(string.Format("Real->Virtual position : posItemSymbol={0}, symbol = {1}, logicID = {2}", posItem.m_sSymbol, sSymbol, sLogicID));
                    posItem.m_sLogicID = sLogicID;
                    m_lstPos_vt.Add(posItem);
                }
            }
        }


        private void pushTick_cache()
        {
            
            foreach (KeyValuePair<string, CCacheData> entry in m_rates_cache)
            {
                TRatesTick tick_cur = getTick(entry.Key, 0);
                //PROBLEM??? 백금 5분족 시험할때, 챠트에서 US, JP 의 막대개수가 차이남. 같은 시세가 들오어면 빠지는쪽이 있음.
                if (tick_cur.dAsk == m_rates[entry.Key].dAsk && tick_cur.dBid == m_rates[entry.Key].dBid) //If same tick then skip
                    continue;
                entry.Value.pushTick(m_rates[entry.Key].dAsk, m_rates[entry.Key].dBid, m_rates[entry.Key].m_dtTime);
            }
        }

        public bool publishTradeHistory()
        {
            if (m_nPublishedPosCnt == m_lstPosHistory_vt.Count)
                return false;

            string sMsg = "";
            sMsg = m_sSiteName;
            sMsg += "@";
            sMsg += m_sID;
            sMsg += "@";
            sMsg += CFATCommon.m_dtCurTime.ToString("yyyy/MM/dd HH:mm:ss.fff");
            sMsg += "@";

            TPosItem posItem = null;
            for ( int i = m_nPublishedPosCnt; i < m_lstPosHistory_vt.Count; i ++ )
            {
                posItem = m_lstPosHistory_vt[i];
                sMsg += posItem.getString();
                sMsg += "@";
            }
            m_nPublishedPosCnt = m_lstPosHistory_vt.Count;

            CMQClient.publish_msg(sMsg, CFATCommon.MQ_TOPIC_POSHISTORY);
            return true;
        }

        public virtual void makeReport(bool bAddParams = true)
        {
            foreach (string sSymbol in m_sSymbols)
            {
                m_report.m_dProfit = 0;
                m_report.m_nTradeCnt = 0;

                //Write Trade history to File
                string sData = "";
                foreach (TPosItem posItem in m_lstPosHistory_vt)
                {
                    if (posItem.m_sSymbol != sSymbol) continue;

                    m_report.m_dProfit += posItem.m_dProfit_real;
                    m_report.m_nTradeCnt++;
                    sData += posItem.getString();
                    sData += "\r\n";
                }
                
                if (CFATManager.m_nRunMode == ERUN_MODE.BACKTEST)
                {
                    string sFile = Path.Combine(Application.StartupPath, string.Format("_log\\backtest_reports\\{0}_{1}_{2}.csv", 
                        DateTime.Now.ToString("yyyy_MM_dd HH_mm_ss_fff"), m_sSiteName, sSymbol));
                    File.WriteAllText(sFile, sData);
                }
                //----------------------------------------

                //Output backtest result
                CFATLogger.output_proc(string.Format("*** BackTest Result : Site {0} : {1} ***", m_sSiteName, sSymbol));
                sData = string.Format(" {0}, {1}, Profit,{2:0.00000}, TradeCount = {3},", m_sSiteName, sSymbol, m_report.m_dProfit, m_report.m_nTradeCnt);
                if (bAddParams)
                    sData += CFATCommon.m_sOpt_param;
                
                CFATLogger.output_opt(sData);
                CFATLogger.output_proc(sData);
                CFATLogger.output_proc("******************************************");
            }
        }

        public void setRenkoStep(string sSymbol, double dRenkoStep)
        {
            m_rates_cache[sSymbol].setRenkoStep(dRenkoStep);
        }

        public TRatesTick getTick(string sSymbol, int nPos)
        {
            return m_rates_cache[sSymbol].getTick(nPos);
        }
        public TRatesTick getRenko(string sSymbol, int nPos)
        {
            return m_rates_cache[sSymbol].getRenko(nPos);
        }
        public TRatesMin getMinRate(string sSymbol, int nPos)
        {
            return m_rates_cache[sSymbol].getMin(nPos);
        }

        public DateTime getTickTime(string sSymbol)
        {
            return m_rates[sSymbol].m_dtTime;
        }
        public int getTick_count(string sSymbol)
        {
            return m_rates_cache[sSymbol].getTickCount();
        }
        public int getRenko_count(string sSymbol)
        {
            return m_rates_cache[sSymbol].getRenkoCount();
        }

        private bool isValidRates()
        {
            foreach(KeyValuePair<string,TRatesTick> tic in m_rates)
            {
                if (tic.Value.dAsk < CFATCommon.ESP || tic.Value.dBid < CFATCommon.ESP)
                    return false;
            }
            /*HSM
            if (CFATManager.isOnlineMode())
            {
                if ((DateTime.Now - getTick(m_sSymbols[0], 0).m_dtTime).TotalSeconds > 60)
                    return false;
            }*/
            return true;
        }

        public virtual EERROR OnTick()
        {
            if (!isValidRates())
            {
                if (CFATManager.isOnlineMode())
                    CFATLogger.output_proc(string.Format("{0}: Price is 0, Error!", m_sSiteName));
                return EERROR.RATE_INVALID;
            }

            // modified by cmh
            if(getStatus() != SITE_STATUS.PROCESSING)
            {
                updatePosList();//Update close price and calc profit 
            }
            // ---
            
            pushTick_cache(); // push cache data 
            return EERROR.NONE;
        }


        public virtual double getBid(string sSymbol)
        {
            return m_rates[sSymbol].dBid;
        }

        public virtual double getAsk(string sSymbol)
        {
            return m_rates[sSymbol].dAsk;
        }

        public virtual double getMid(string sSymbol)
        {
            return m_rates[sSymbol].getMid();
        }

        public virtual int getPosCount_vt(string sSymbol, string sLogicID)
        {
            if (m_lstPos_vt.Count == 0)
                return 0;
            int nRet = 0;
            foreach(TPosItem posItem in m_lstPos_vt)
            {
                if (posItem.m_sSymbol == sSymbol && posItem.m_sLogicID == sLogicID)
                    nRet++;
            }
            return nRet;
        }
        public virtual int getHistoryCount_vt(string sSymbol, string sLogicID)
        {
            if (m_lstPosHistory_vt.Count == 0)
                return 0;
            int nRet = 0;
            foreach (TPosItem posItem in m_lstPosHistory_vt)
            {
                if (posItem.m_sSymbol == sSymbol && posItem.m_sLogicID == sLogicID)
                    nRet++;
            }
            return nRet;
        }
        public virtual ETRADER_OP getPosCmd_vt(string sSymbol, int nPosIndex, string sLogicID)
        {
            int nPosId = -1;
            foreach (TPosItem posItem in m_lstPos_vt)
            {
                if (posItem.m_sSymbol == sSymbol && posItem.m_sLogicID == sLogicID)
                {
                    nPosId++;
                    if (nPosId == nPosIndex)
                        return (ETRADER_OP)posItem.m_nCmd;
                }
            }
            return ETRADER_OP.NONE;
        }
        public virtual double getPosProfit_vt(string sSymbol, int nPosIndex, string sLogicID)
        {
            int nPosId = -1;
            foreach (TPosItem posItem in m_lstPos_vt)
            {
                if (posItem.m_sSymbol == sSymbol && posItem.m_sLogicID == sLogicID)
                {
                    nPosId++;
                    if (nPosId == nPosIndex)
                        return posItem.m_dProfit_vt;
                }
            }
            return 0;
        }

        public virtual double getPosClosePrice_req(string sSymbol, int nPosIndex, string sLogicID)
        {
            int nPosId = -1;
            foreach (TPosItem posItem in m_lstPos_vt)
            {
                if (posItem.m_sSymbol == sSymbol && posItem.m_sLogicID == sLogicID)
                {
                    nPosId++;
                    if (nPosId == nPosIndex)
                        return posItem.m_dClosePrice_req;
                }
            }
            return -1;
        }
        public virtual double getPosLots_exc(string sSymbol, int nPosIndex, string sLogicID)
        {
            int nPosId = -1;
            foreach (TPosItem posItem in m_lstPos_vt)
            {
                if (posItem.m_sSymbol == sSymbol && posItem.m_sLogicID == sLogicID)
                {
                    nPosId++;
                    if (nPosId == nPosIndex)
                        return posItem.m_dLots_exc;
                }
            }
            return 0;
        }


        public void available_lots_cmd(string sSymbol, ref ETRADER_OP nCmd, ref double dLots)
        {
            double dLots_real = 0;

            if (CFATManager.m_nRunMode == ERUN_MODE.REALTIME)
                dLots_real = getPosLots_Total_real(sSymbol);
            else
                dLots_real = getPosLots_Total_vt(sSymbol);

            //Case 1 : 
            if (Math.Abs(dLots_real) < CFATCommon.ESP)
            {
                if (nCmd == ETRADER_OP.BUY_CLOSE)
                    nCmd = ETRADER_OP.SELL;
                if (nCmd == ETRADER_OP.SELL_CLOSE)
                    nCmd = ETRADER_OP.BUY;
                return;
            }

            ETRADER_OP nCmd_real = ETRADER_OP.NONE;
            dLots_real = Math.Abs(dLots_real);

            if (dLots_real > CFATCommon.ESP)
                nCmd_real = ETRADER_OP.BUY;
            else
                nCmd_real = ETRADER_OP.SELL;

            //Case 2 : 
            if (nCmd_real == nCmd )
                return;

            //Case 3 : 
            if ( nCmd_real == ETRADER_OP.BUY )
            {
                if ( nCmd == ETRADER_OP.SELL )
                {//Case 3-1 :
                    nCmd = ETRADER_OP.BUY_CLOSE;
                    if (dLots_real < dLots - CFATCommon.ESP)
                        dLots = dLots_real;
                    return;
                }

                if ( nCmd == ETRADER_OP.BUY_CLOSE)
                {//Case 3-2 :
                    if (dLots_real < dLots - CFATCommon.ESP)
                        dLots = dLots_real;
                    return;
                }

                if ( nCmd == ETRADER_OP.SELL_CLOSE )
                {//Case 3-3 : 
                    return;
                }
            }

            //Case 4 : 
            if (nCmd_real == ETRADER_OP.SELL)
            {
                if (nCmd == ETRADER_OP.BUY)
                {//Case 4-1 :
                    nCmd = ETRADER_OP.SELL_CLOSE;
                    if (dLots_real < dLots - CFATCommon.ESP)
                        dLots = dLots_real;
                    return;
                }

                if (nCmd == ETRADER_OP.SELL_CLOSE)
                {//Case 4-2 :
                    if (dLots_real < dLots - CFATCommon.ESP)
                        dLots = dLots_real;
                    return;
                }

                if (nCmd == ETRADER_OP.BUY_CLOSE)
                {//Case 4-3 : 
                    return;
                }
            }


        }

        public EFILLED_STATE reqOrder_withoutEXC(string sSymbol, ETRADER_OP nCmd, ref double dLots, ref double dPrice, EORDER_TYPE nOrderType, string sLogicID, string sComment = "",
                double dLots_exc = 0, double dPrice_exc = 0, DateTime dtTime_exc = default(DateTime))
        {
            return register2Vt(sSymbol, nCmd, ref dLots, ref dPrice, 
                nOrderType, sLogicID, sComment, dLots_exc, dPrice_exc, dtTime_exc);
        }
        private EFILLED_STATE register2Vt(string sSymbol, ETRADER_OP nCmd, ref double dLots, ref double dPrice, EORDER_TYPE nOrderType, string sLogicID, string sComment = "",
            double dLots_exc = 0, double dPrice_exc = 0, DateTime dtTime_exc = default(DateTime))
        {
            
            if (dLots_exc == 0) dLots_exc = dLots;
            if (dPrice_exc == 0) dPrice_exc = dPrice;

            if (dtTime_exc == default(DateTime)) dtTime_exc = m_rates[sSymbol].m_dtTime;

            if (nCmd == ETRADER_OP.BUY || nCmd == ETRADER_OP.SELL)
            {//For new order
                TPosItem posItem = new TPosItem();
                posItem.m_dtOpenTime_req = m_rates[sSymbol].m_dtTime;
                posItem.m_dtOpenTime_exc = dtTime_exc;

                posItem.m_dOpenPrice_req = dPrice;
                posItem.m_dOpenPrice_exc = dPrice_exc;

                posItem.m_dLots_req = dLots;
                posItem.m_dLots_exc = dLots_exc;

                posItem.m_nCmd = nCmd;
                posItem.m_sSymbol = sSymbol;
                posItem.m_dProfit_vt = 0;
                posItem.m_sLogicID = sLogicID;
                posItem.m_sComment = sComment;
                posItem.m_dContractSize = getContractSize(sSymbol);
                posItem.m_dCommission_percent = getCommissionPercent(sSymbol);

                if (sLogicID != "REQ_MERGE")
                    m_lstPos_vt.Add(posItem);
            }

            if (nCmd == ETRADER_OP.BUY_CLOSE || nCmd == ETRADER_OP.SELL_CLOSE)
            {//For close order
                closeOrder(sSymbol, nCmd, dLots, dPrice, nOrderType, sLogicID, sComment, dLots_exc, dPrice_exc, dtTime_exc);
            }

            EFILLED_STATE nRet = EFILLED_STATE.PARTIAL;
            if (Math.Abs(dLots - dLots_exc) < CFATCommon.ESP)
                nRet = EFILLED_STATE.FULL;
            if (Math.Abs(dLots_exc) < CFATCommon.ESP)
                nRet = EFILLED_STATE.FAIL;
            return nRet;// updateRealPositions();
        }
        public virtual EFILLED_STATE reqOrder(string sSymbol, ETRADER_OP nCmd, ref double dLots, ref double dPrice, EORDER_TYPE nOrderType, string sLogicID, string sComment = "",
            double dLots_exc = 0, double dPrice_exc = 0, DateTime dtTime_exc = default(DateTime))
        {
            return register2Vt(sSymbol, nCmd, ref dLots, ref dPrice, nOrderType, sLogicID, sComment, dLots_exc, dPrice_exc, dtTime_exc);
        }

        private bool isValidCloseCommand(ETRADER_OP posCmd, ETRADER_OP reqCmd)
        {
            if (reqCmd == ETRADER_OP.BUY_CLOSE && posCmd == ETRADER_OP.BUY)
                return true;

            if (reqCmd == ETRADER_OP.SELL_CLOSE && posCmd == ETRADER_OP.SELL)
                return true;
            return false;
        }
        private void addToHistoryItem(TPosItem posItem)
        {
            posItem.m_dtCloseTime_exc = m_rates[posItem.m_sSymbol].m_dtTime;
            m_lstPosHistory_vt.Add(posItem);
            m_report.m_dProfit += posItem.m_dProfit_real; //Update Profit
        }

        private void closeOrder(string sSymbol, ETRADER_OP nCmd, double dLots, double dPrice, EORDER_TYPE nOrderType, string sLogicID, string sComment = "",
            double dLots_exc = 0, double dPrice_exc = 0, DateTime dtTime_exc = default(DateTime))
        {
            if (sLogicID == "REQ_MERGE")
                return;
            double dRemainLots = dLots_exc;
            TPosItem posItem;
            for (int i = 0; i < m_lstPos_vt.Count; i++ )
            {
                posItem = m_lstPos_vt[i];

                if (posItem.m_sSymbol != sSymbol) continue;
                if (posItem.m_sLogicID != sLogicID) continue;

                if (!isValidCloseCommand(posItem.m_nCmd, nCmd)) continue;

                if ( dRemainLots >= posItem.m_dLots_exc - CFATCommon.ESP )
                {
                    m_lstPos_vt.Remove(posItem);
                    i--;
                    posItem.m_sComment += "@";
                    posItem.m_sComment += sComment;
                    posItem.m_dClosePrice_exc = dPrice_exc;
                    posItem.m_dtCloseTime_req = m_rates[sSymbol].m_dtTime;
                    posItem.m_dtCloseTime_exc = dtTime_exc;
                    posItem.calcProfit();
                    addToHistoryItem(posItem);
                    dRemainLots -= posItem.m_dLots_exc;
                }
                else
                {
                    posItem.m_dLots_exc -= dRemainLots;
                    posItem.calcProfit();
                    m_lstPos_vt[i] = posItem;

                    posItem.m_dLots_exc = dRemainLots;
                    posItem.m_sComment += "@";
                    posItem.m_sComment += sComment;
                    posItem.m_dClosePrice_exc = dPrice_exc;
                    posItem.m_dtCloseTime_req = m_rates[sSymbol].m_dtTime;
                    posItem.m_dtCloseTime_exc = dtTime_exc;
                    posItem.calcProfit();
                    addToHistoryItem(posItem);
                    dRemainLots = 0;
                }

                if (Math.Abs(dRemainLots) < CFATCommon.ESP)
                    break;
            }
        }

        /// <summary>
        /// Update position list with close price and profit
        /// </summary>
        private void updatePosList()
        {
            TPosItem posItem;
            
            for (int i = 0; i < m_lstPos_vt.Count; i ++ )
            {
                posItem = m_lstPos_vt[i];
                if ( posItem.m_nCmd == ETRADER_OP.BUY )
                    posItem.setClosePrice(m_rates[posItem.m_sSymbol].dBid);

                if (posItem.m_nCmd == ETRADER_OP.SELL)
                    posItem.setClosePrice(m_rates[posItem.m_sSymbol].dAsk);

                posItem.calcProfit();
                m_lstPos_vt[i] = posItem;
            }
        }


    }
}
