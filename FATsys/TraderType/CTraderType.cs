using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FATsys.Utils;
using FATsys.Site;
using FATsys.Product;
namespace FATsys.TraderType
{
    [Serializable]
    public class TRatesTick
    {
        public TRatesTick() { }
        public DateTime m_dtTime { get; set; }
        public double dBid { get; set; }
        public double dAsk { get; set; }

        public double getMid()
        {
            return (dAsk + dBid) / 2;
        }
    }

    [Serializable]
    public class TRatesMin
    {
        public TRatesMin() { }
        public DateTime m_dtTime { get; set; }

        public double dBid_open { get; set; }
        public double dBid_high { get; set; }
        public double dBid_low { get; set; }
        public double dBid_close { get; set; }

        public double dAsk_open { get; set; }
        public double dAsk_high { get; set; }
        public double dAsk_low { get; set; }
        public double dAsk_close { get; set; }

        public void setVal(double dAskopen, double dAskhigh, double dAsklow, double dAskclose,
                    double dBidopen, double dBidhigh, double dBidlow, double dBidclose)
        {
            dAsk_open = dAskopen;
            dAsk_high = dAskhigh;
            dAsk_low = dAsklow;
            dAsk_close = dAskclose;

            dBid_open = dBidopen;
            dBid_high = dBidhigh;
            dBid_low = dBidlow;
            dBid_close = dBidclose;
        }
        public void setTickVal(double dAsk, double dBid)
        {
            dAsk_close = dAsk;
            dBid_close = dBid;

            if (dAsk_high < dAsk) dAsk_high = dAsk;
            if (dAsk_low > dAsk) dAsk_low = dAsk_low;

            if (dBid_high < dBid) dBid_high = dBid;
            if (dBid_low > dBid) dBid_low = dBid;
        }
    }
    
    public class TPosItem
    {
        public DateTime m_dtOpenTime_req;
        public DateTime m_dtOpenTime_exc;
        public DateTime m_dtCloseTime_req;
        public DateTime m_dtCloseTime_exc;

        public int m_nTicket = 0;
        public string m_sTicket = "";
        public string m_sSymbol;
        public ETRADER_OP m_nCmd;
        public double m_dOpenPrice_req;
        public double m_dOpenPrice_exc;
        public double m_dLots_req;
        public double m_dLots_exc;
        public double m_dClosePrice_req;
        public double m_dClosePrice_exc;
        public double m_dProfit_vt;
        public double m_dProfit_real;
        public double m_dCommission;
        public string m_sLogicID;
        public string m_sComment = "";
        public double m_dContractSize = 1;
        public double m_dCommission_percent = 0;

        public void setClosePrice(double dClosePrice)
        {
            m_dClosePrice_req = dClosePrice;
            m_dClosePrice_exc = dClosePrice;
        }

        public void calcProfit()
        {
            //double dCommission = m_dOpenPrice_exc * m_dLots_exc * m_dContractSize * 2.4 / 100000;
            double dCommission = m_dOpenPrice_exc * m_dLots_exc * m_dContractSize * m_dCommission_percent;

            if (m_dClosePrice_req < CFATCommon.ESP )
            {
                m_dProfit_vt = 0;
                m_dProfit_real = 0;
                return;
            }
            if (m_nCmd == ETRADER_OP.BUY)
            {
                m_dProfit_vt = (m_dClosePrice_req - m_dOpenPrice_exc) * m_dLots_exc * m_dContractSize - dCommission;
                m_dProfit_real = (m_dClosePrice_exc - m_dOpenPrice_exc) * m_dLots_exc * m_dContractSize - dCommission;
            }

            if (m_nCmd == ETRADER_OP.SELL)
            {
                m_dProfit_vt = (m_dOpenPrice_req - m_dClosePrice_exc) * m_dLots_exc * m_dContractSize - dCommission;
                m_dProfit_real = (m_dOpenPrice_exc - m_dClosePrice_exc) * m_dLots_exc * m_dContractSize - dCommission;
            }
        }

        public string getString()
        {
            string sRet = "";
            sRet = string.Format("{0},{1},{2},{3}," + //request open time, excute open time, request close time, request close time
                "{4},{5:0.000},{6:0.000},{7}," + // symbol, request lots, excute lots, command 
                "{8:0.00000},{9:0.00000},{10:0.00000},{11:0.00000}," + // request open price, excute open price, request close price, excute close price
                "{12:0.00},{13},{14}", // profit, logic id, comment
                m_dtOpenTime_req.ToString("yyyy-MM-dd HH:mm:ss.fff"), m_dtOpenTime_exc.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                m_dtCloseTime_req.ToString("yyyy-MM-dd HH:mm:ss.fff"), m_dtCloseTime_exc.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                m_sSymbol, m_dLots_req, m_dLots_exc, TRADER.cmd2String(m_nCmd), 
                m_dOpenPrice_req, m_dOpenPrice_exc, m_dClosePrice_req,  m_dClosePrice_exc, 
                m_dProfit_real, m_sLogicID, m_sComment);
            return sRet;
        }

        public void setData(string sData)
        {
            string[] sVals = sData.Split(',');

            m_dtOpenTime_req = Convert.ToDateTime(sVals[0]);
            m_dtOpenTime_exc = Convert.ToDateTime(sVals[1]);
            m_dtCloseTime_req = Convert.ToDateTime(sVals[2]);
            m_dtCloseTime_exc = Convert.ToDateTime(sVals[3]);

            m_sSymbol = sVals[4];
            m_dLots_req = Convert.ToDouble(sVals[5]);
            m_dLots_exc = Convert.ToDouble(sVals[6]);

            m_nCmd = TRADER.string2Cmd(sVals[7]);

            m_dOpenPrice_req = Convert.ToDouble(sVals[8]);
            m_dOpenPrice_exc = Convert.ToDouble(sVals[9]);

            m_dClosePrice_req = Convert.ToDouble(sVals[10]);
            m_dClosePrice_exc = Convert.ToDouble(sVals[11]);

            m_dProfit_real = Convert.ToDouble(sVals[12]);
            m_sLogicID = sVals[13];
            m_sComment = sVals[14];
        }
    }




    public class TAccountInfo
    {
        public double m_dBalance;
        public double m_dEquity;
        public double m_dMargin;
        public void init()
        {
            m_dBalance = 0;
            m_dEquity = 0;
            m_dMargin = 0;
        }
    }

    public class TSiteReport
    {
        public double m_dProfit;
        public int m_nTradeCnt;
        public void init()
        {
            m_dProfit = 0;
            m_nTradeCnt = 0;
        }
    }

    public class TLogicParamItem
    {
        public string m_sVal;
        public string m_sStart;
        public string m_sStep;
        public string m_sEnd;

        public int m_nCurPos;
        public void init()
        {
            m_nCurPos = 0;
        }

        public string getVal(int nID)
        {
            if (m_sStart.Replace(" ", "") == "")
                return m_sVal;

            double dStart = Convert.ToDouble(m_sStart);
            double dStep = Convert.ToDouble(m_sStep);
            double dEnd = Convert.ToDouble(m_sEnd);

            double dRet = dStart + dStep * nID;

            return string.Format("{0:0.000}", dRet);
        }

        public int getCount()
        {
            if (m_sStart.Replace(" ", "") == "")
                return 0;

            double dStart = Convert.ToDouble(m_sStart);
            double dStep = Convert.ToDouble(m_sStep);
            double dEnd = Convert.ToDouble(m_sEnd);
            int nCount = (int)((dEnd - dStart) / dStep) + 1;
            return nCount;
        }

    }

    public class TReqOrder
    {
        public CProduct m_product = null;
        public ETRADER_OP m_nCmd;
        public double m_dLots_req;
        public double m_dLots_exc;
        
        public double m_dPrice_req;
        public double m_dPrice_exc;

        public EORDER_TYPE m_nOrderType;
        public int m_nPriority;
        public string m_sComment;
        public string m_sLogicID;
        public bool m_bProcessed = false;
        public EFILLED_STATE m_nOrderResult = EFILLED_STATE.FAIL;
        public List<TReqOrder> m_lstSubReqOrders = new List<TReqOrder>();

        public int getProductCode()
        {
            return m_product.getProductCode();
        }
        private void setFilledState_subReqOrders(EFILLED_STATE nResult)
        {
            foreach(TReqOrder reqOrderItem in m_lstSubReqOrders)
            {
                if (nResult == EFILLED_STATE.FULL)
                    reqOrderItem.m_dLots_exc = reqOrderItem.m_dLots_req;
                if ( nResult == EFILLED_STATE.FAIL)
                    reqOrderItem.m_dLots_exc = 0;
                reqOrderItem.m_nOrderResult = nResult;
                reqOrderItem.m_bProcessed = true;
            }
        }

        private void register_vtPos2subReqOrders()
        {
            foreach(TReqOrder reqOrderItem in m_lstSubReqOrders)
            {
                if (reqOrderItem.m_nOrderResult == EFILLED_STATE.FAIL) continue;
                reqOrderItem.m_product.reqOrder_withoutEXC(reqOrderItem.m_nCmd, ref reqOrderItem.m_dLots_req, 
                    ref reqOrderItem.m_dPrice_req, reqOrderItem.m_nOrderType, reqOrderItem.m_sLogicID, reqOrderItem.m_sComment);
            }
        }
        public void setResult_subReqOrders()
        {
            if (m_nOrderResult == EFILLED_STATE.FAIL || 
                m_nOrderResult == EFILLED_STATE.FULL )  //If all lots fail or success
            {
                setFilledState_subReqOrders(m_nOrderResult);
                register_vtPos2subReqOrders();
                return;
            }

            //If part lots success filled

            //Set success to opposite reqOrders
            double dTotalLots_exc = m_dLots_exc;
            foreach (TReqOrder reqOrderItem in m_lstSubReqOrders)
            {
                if ( reqOrderItem.m_nCmd != m_nCmd )
                {
                    reqOrderItem.m_dLots_exc = reqOrderItem.m_dLots_req;
                    reqOrderItem.m_nOrderResult = EFILLED_STATE.FULL;
                    reqOrderItem.m_bProcessed = true;
                    reqOrderItem.m_dPrice_exc = m_dPrice_exc;
                    dTotalLots_exc += reqOrderItem.m_dLots_exc;
                }
            }

            //Set excute lots from first reqOrders which same direction
            foreach(TReqOrder reqOrderItem in m_lstSubReqOrders)
            {
                if (reqOrderItem.m_nCmd != m_nCmd) continue;
                if (Math.Abs(dTotalLots_exc) < CFATCommon.ESP) break;

                if ( dTotalLots_exc >= reqOrderItem.m_dLots_req - CFATCommon.ESP )
                {
                    reqOrderItem.m_dLots_exc = reqOrderItem.m_dLots_req;
                    reqOrderItem.m_nOrderResult = EFILLED_STATE.FULL;
                    reqOrderItem.m_bProcessed = true;
                    reqOrderItem.m_dPrice_exc = m_dPrice_exc;
                    dTotalLots_exc -= reqOrderItem.m_dLots_exc;
                    continue;
                }

                reqOrderItem.m_dLots_exc = dTotalLots_exc;
                reqOrderItem.m_nOrderResult = EFILLED_STATE.PARTIAL;
                reqOrderItem.m_bProcessed = true;
                reqOrderItem.m_dPrice_exc = m_dPrice_exc;
                dTotalLots_exc  = 0;
            }
            register_vtPos2subReqOrders();
        }

        public void setProduct(string sSymbol, CSite site, string sLogicID, double dContractSize)
        {
            if (m_product == null)
                m_product = new CProduct();
            m_product.setSymbol(sSymbol);
            m_product.setSite(site);
            m_product.setLogicID(sLogicID);
            m_product.setContractSize(dContractSize);
        }
        public void setVal(ETRADER_OP nCmd, double dLots_req, double dPrice_req, EORDER_TYPE nOrderType, 
            int nPriority, string sLogicID, string sComment)
        {
            m_nCmd = nCmd;
            m_dLots_req = dLots_req;
            m_dPrice_req = dPrice_req;
            m_nOrderType = nOrderType;
            m_nPriority = nPriority;
            m_sLogicID = sLogicID;
            m_sComment = sComment;

            m_dLots_exc = 0;
            m_nOrderResult = EFILLED_STATE.FAIL;
            m_dPrice_exc = 0;
            
        }

        public void reqOrder(bool bMustExc)
        {
            if ( Math.Abs(m_dLots_req) < CFATCommon.ESP )
            {
                m_nOrderResult = EFILLED_STATE.FULL;
                m_dLots_exc = 0;
                m_bProcessed = true;
                return;
            }

            m_dLots_exc = m_dLots_req;
            m_dPrice_exc = m_dPrice_req;

            EFILLED_STATE nRet = m_product.reqOrder(m_nCmd, ref m_dLots_exc, ref m_dPrice_exc, m_nOrderType, m_sComment);

            m_nOrderResult = nRet;
            m_bProcessed = true;
        }
    }
    public class CTradeHistory
    {
        private double m_dProfit = 0;
        private List<double> m_lstProfit = new List<double>();
        public void pushHistory(double dPrice)
        {
            m_dProfit += dPrice;
        }
        public void setProfitAsZero()
        {
            m_dProfit = 0;
        }
        public void settleHistory()
        {
            m_lstProfit.Add(m_dProfit);
            m_dProfit = 0;
        }
        public double getProfit(int nPos)
        {
            int nPos2 = nPos;
            if (nPos >= m_lstProfit.Count)
                nPos2 = m_lstProfit.Count - 1;
            if (nPos2 < 0)
                return 0;
            return m_lstProfit[nPos2];
        }

        public int getTradeCount()
        {
            return m_lstProfit.Count;
        }
    }

    public class TReqPosMatch
    {
        public CProduct m_product;
        public bool m_bIsPosMatch;

        public void process_PosMatch()
        {
            m_bIsPosMatch = m_product.isPositionMatch();
        }
    }

   
}
