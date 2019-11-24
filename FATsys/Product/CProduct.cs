using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using FATsys.Site;
using FATsys.Utils;
using FATsys.TraderType;

namespace FATsys.Product
{
    public class CProduct
    {
        public string m_sSymbol;
        public CSite m_site;
        public string m_sLogicID;
        public double m_dContractSize = 1;

        public double m_dBid;
        public double m_dAsk;
        public double m_dMid;
        public double m_dBid_published_tick;
        public double m_dAsk_published_tick;

        public DateTime m_dtTime_published_min;

        TReqOrder m_reqOrder_async = new TReqOrder();
        TReqPosMatch m_reqPosMatch = new TReqPosMatch();
        public virtual void getRates()
        {
            m_dBid = getBid();
            m_dAsk = getAsk();
            m_dMid= (m_dBid + m_dAsk) / 2;
        }

        public void setSite(CSite site)
        {
            m_site = site;
        }
        public void setRenkoStep(double dRenkoStep)
        {
            m_site.setRenkoStep(m_sSymbol, dRenkoStep);
        }
        public void setContractSize(double dContractSize)
        {
            m_dContractSize = dContractSize;
        }

        public void setLogicID(string sLogicID)
        {
            m_sLogicID = sLogicID;
        }

        public void clearPositions()
        {
            //Clear Virtual positions
            m_site.clearVirtualPositions(m_sSymbol);

            //Clear Real positions
            double dLots = m_site.getPosLots_Total_real(m_sSymbol);
            if (Math.Abs(dLots) < CFATCommon.ESP)
                return;

            ETRADER_OP nCmd = ETRADER_OP.NONE;

            if (dLots > 0) // Buy 
                nCmd = ETRADER_OP.BUY_CLOSE;
            else // Sell
                nCmd = ETRADER_OP.SELL_CLOSE;

            double dReqLots = Math.Abs(dLots);
            double dReqPrice = 0;

            if (nCmd == ETRADER_OP.BUY_CLOSE) //Sell
                dReqPrice = getTick(0).dBid;

            if (nCmd == ETRADER_OP.SELL_CLOSE) //Buy
                dReqPrice = getTick(0).dAsk;

            reqOrder(nCmd, ref dReqLots, ref dReqPrice, EORDER_TYPE.MARKET);
        }

        public void updateRealPositions()
        {
            m_site.updateRealPositions();
        }

        public bool isPositionMatch()
        {
            if (CFATManager.m_nRunMode != ERUN_MODE.REALTIME)
                return true;

            if ( Math.Abs(m_site.getPosLots_Total_vt(m_sSymbol) - m_site.getPosLots_Total_real(m_sSymbol)) < CFATCommon.ESP )
                return true;
            m_site.updateRealPositions();

            if (Math.Abs(m_site.getPosLots_Total_vt(m_sSymbol) - m_site.getPosLots_Total_real(m_sSymbol) ) < CFATCommon.ESP)
                return true;
            return false;
        }
        public void matchPosReal2Virtual()
        {
            m_site.matchPosReal2Virtual(m_sSymbol, m_sLogicID);
        }
        public void matchPosVirtual2Real()
        {
            double dLots_real = m_site.getPosLots_Total_real(m_sSymbol);
            double dLots_vt = m_site.getPosLots_Total_vt(m_sSymbol);

            double dLots_diff = dLots_vt - dLots_real;
            
            double dAsk = getTick(0).dAsk;
            double dBid = getTick(0).dBid;
            if (Math.Abs(dLots_diff) < CFATCommon.ESP)
                return;

            if ( dLots_diff > 0 )
            {// new buy or close sell
                if (dLots_vt > 0)
                {// new buy
                    reqOrder(ETRADER_OP.BUY, ref dLots_diff, ref dAsk, EORDER_TYPE.MARKET);
                }
                else
                {// close sell
                    reqOrder(ETRADER_OP.SELL_CLOSE, ref dLots_diff, ref dAsk, EORDER_TYPE.MARKET);
                }
            }

            if ( dLots_diff < 0)
            {// new sell or close buy
                if ( dLots_vt < 0)
                {// new sell
                    reqOrder(ETRADER_OP.SELL, ref dLots_diff, ref dBid, EORDER_TYPE.MARKET);
                }
                else
                {// close buy
                    reqOrder(ETRADER_OP.BUY_CLOSE, ref dLots_diff, ref dBid, EORDER_TYPE.MARKET);
                }
            }
        }

        public void publish_tick()
        {
            string sTxt = "";
            if (m_dBid != m_dBid_published_tick || m_dAsk != m_dAsk_published_tick)
            {
                sTxt = string.Format("{0},{1},{2},{3},{4}", getSiteName(), getSymbol(), getTickTime(), m_dBid, m_dAsk);
                CMQClient.publish_msg(sTxt, CFATCommon.MQ_TOPIC_PRICE_TICK);
                m_dBid_published_tick = m_dBid;
                m_dAsk_published_tick = m_dAsk;
            }
        }

        public void publish_min()
        {
            string sTxt = "";
            TRatesMin ratesMin = getMinRates(0);
            DateTime dtTime_cur = ratesMin.m_dtTime;

            if (dtTime_cur != m_dtTime_published_min)
            {
                ratesMin = getMinRates(0);
                sTxt = string.Format("{0},{1},{2},{3},{4},{5},{6}", getSiteName(), getSymbol(), ratesMin.m_dtTime, 
                    ratesMin.dBid_open, ratesMin.dBid_high, ratesMin.dBid_low, ratesMin.dBid_close);
                CMQClient.publish_msg(sTxt, CFATCommon.MQ_TOPIC_PRICE_MIN);
                m_dtTime_published_min = dtTime_cur;
            }
        }
        public void setSymbol(string sSymbol)
        {
            m_sSymbol = sSymbol;
        }
        public virtual string getSymbol()
        {
            return m_sSymbol;
        }

        public virtual string getSiteName()
        {
            return m_site.m_sSiteName;
        }

        public double getBid()
        {
            return m_site.getBid(m_sSymbol);
        }
        public double getAsk()
        {
            return m_site.getAsk(m_sSymbol);
        }
        public double getMid()
        {
            return m_site.getMid(m_sSymbol);
        }
        public virtual DateTime getTickTime()
        {
            return m_site.getTickTime(m_sSymbol);
        }
        public virtual TRatesTick getTick(int nPos)
        {
            return m_site.getTick(m_sSymbol, nPos);
        }
        public virtual TRatesTick getRenko(int nPos)
        {
            return m_site.getRenko(m_sSymbol, nPos);
        }

        public virtual TRatesMin getMinRates(int nPos)
        {
            return m_site.getMinRate(m_sSymbol, nPos);
        }
        public virtual int getTick_count()
        {
            return m_site.getTick_count(m_sSymbol);
        }
        public virtual int getRenko_count()
        {
            return m_site.getRenko_count(m_sSymbol);
        }

        public double getPosLots_Total_vt()
        {
            return m_site.getPosLots_Total_vt(m_sSymbol);
        }
        public double getPosLots_vt(int nIndex)
        {
            return m_site.getPosLots_vt(m_sSymbol, nIndex);
        }

        public double getPosLots_Total_real()
        {
            if ( !CFATManager.isOnlineMode())
                return m_site.getPosLots_Total_vt(m_sSymbol);
            return m_site.getPosLots_Total_real(m_sSymbol);
        }

        public double getPosLots_real(int nIndex)
        {
            if (!CFATManager.isOnlineMode())
                return getPosLots_vt(nIndex);
            return m_site.getPosLots_real(m_sSymbol, nIndex);
        }

        public int getPosCount_vt()
        {
            return m_site.getPosCount_vt(m_sSymbol, m_sLogicID);
        }
        public int getHistoryCount_vt()
        {
            return m_site.getHistoryCount_vt(m_sSymbol, m_sLogicID);
        }
        public int getProductCode()
        {
            return (m_site.GetHashCode() + m_sSymbol.GetHashCode());
        }
        public ETRADER_OP getPosCmd_vt(int nPosIndex)
        {
            return m_site.getPosCmd_vt(m_sSymbol, nPosIndex, m_sLogicID);
        }
        public double getPosProfit_vt(int nPosIndex)
        {
            return m_site.getPosProfit_vt(m_sSymbol, nPosIndex, m_sLogicID);
        }
        public bool reqCloseAll(string sComment = "")
        {
            int nPosCnt = m_site.getPosCount_vt(m_sSymbol, m_sLogicID);
            ETRADER_OP nCmd = ETRADER_OP.NONE;
            double dPrice = 0;
            double dLots = 0;
            for ( int i = 0; i < nPosCnt; i ++ )
            {
                nCmd = m_site.getPosCmd_vt(m_sSymbol, i, m_sLogicID);
                if (nCmd == ETRADER_OP.BUY)
                    nCmd = ETRADER_OP.BUY_CLOSE;
                if (nCmd == ETRADER_OP.SELL)
                    nCmd = ETRADER_OP.SELL_CLOSE;

                dPrice = m_site.getPosClosePrice_req(m_sSymbol, i, m_sLogicID);
                dLots = m_site.getPosLots_exc(m_sSymbol, i, m_sLogicID);
                m_site.reqOrder(m_sSymbol, nCmd, ref dLots, ref dPrice, EORDER_TYPE.MARKET, m_sLogicID, sComment);
            }

            return true;
        } 

        public EFILLED_STATE reqOrder(ETRADER_OP nCmd, ref double dLots, ref double dPrice, EORDER_TYPE nOrderType, string sComment = "")
        {
            return m_site.reqOrder(m_sSymbol, nCmd, ref dLots, ref dPrice, nOrderType, m_sLogicID, sComment);
        }

        // modified by cmh
        public EFILLED_STATE reqOrder_withoutEXC(ETRADER_OP nCmd, ref double dLots, ref double dPrice, EORDER_TYPE nOrderType, string sLogicID, string sComment = "")
        {
            return m_site.reqOrder_withoutEXC(m_sSymbol, nCmd, ref dLots, ref dPrice, nOrderType, sLogicID, sComment);
        }
        // ---

        public int reqOrder_async(ETRADER_OP nCmd, double dLots, double dPrice, EORDER_TYPE nOrderType, int nPriority, string sComment = "")
        {
            m_reqOrder_async.m_bProcessed = false;

            m_reqOrder_async.setProduct(m_sSymbol, m_site, m_sLogicID, m_dContractSize);
            m_reqOrder_async.setVal(nCmd, dLots, dPrice, nOrderType, nPriority, m_sLogicID, sComment);

            return CSiteMng.registerOrder(m_reqOrder_async);
        }

        public bool isOrderProcessed()
        {
            return m_reqOrder_async.m_bProcessed;
        }

        public void reqPosMatch()
        {
            m_reqPosMatch.m_product = this;
            CSiteMng.registerPosMatch(m_reqPosMatch);
        }

        public bool isPositionMatch_v2()
        {
            return m_reqPosMatch.m_bIsPosMatch;
        }
    }
}
