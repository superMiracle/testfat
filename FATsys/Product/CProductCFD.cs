using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FATsys.Utils;
using FATsys.TraderType;

namespace FATsys.Product
{
    class CProductCFD
    {
        private CProduct m_product_A = null;
        private CProduct m_product_B = null;
        private CProduct m_product_C = null;

        private string m_sSymbolCFD;
        private string m_sLogicID;

        private double USDCNY_C0 = 6.2;

        public EPRODUCT_TYPE_PRICE m_nType_price = 0;
        public EPRODUCT_TYPE_TRADE m_nType_trade = 0;


        public double m_dBid;
        public double m_dAsk;
        public double m_dMid;
        private CCacheData m_rates = new CCacheData();

        private double m_dRatio_AB = 31.103477;

        private DateTime m_dtTime_published_min;

        private double m_dBid_published_tick = 0;
        private double m_dAsk_published_tick = 0;

        #region ####### Renko Info #####################
        private bool m_bUseRenko = false;
        private double m_dRenkoStep = 0;
        private double m_dRenkoVal_last = 0;
        private CCacheData m_product_A_rk;
        private CCacheData m_product_B_rk;
        #endregion

        #region ########### Set Product Info ############
        public void setProductInfo(string sLogicID, string sSymbolCFD)
        {
            m_sSymbolCFD = sSymbolCFD;
            m_sLogicID = sLogicID;
        }
        public void setProductType(EPRODUCT_TYPE_PRICE nType_price, EPRODUCT_TYPE_TRADE nType_trade)
        {
            m_nType_price = nType_price;
            m_nType_trade = nType_trade;
        }
        public void setProductRatio(double dRatio_AB)
        {
            m_dRatio_AB = dRatio_AB;
        }

        public void setProductA(CProduct product)
        {
            m_product_A = product;
        }
        public void setProductB(CProduct product)
        {
            m_product_B = product;
        }
        public void setProductC(CProduct product)
        {
            m_product_C = product;
        }
        #endregion

        public void OnInit()
        {
            m_rates.OnInit();
            if (m_bUseRenko)
            {
                m_product_A_rk.OnInit();
                m_product_B_rk.OnInit();
            }
            m_dRenkoVal_last = 0;
        }
        public void setName()
        {
            m_rates.setName(m_sLogicID + "___" + m_sSymbolCFD + "___" + "m_rates");
            if ( m_bUseRenko )
            {
                m_product_A_rk.setName(m_sLogicID + "___" + m_sSymbolCFD + "___" + "m_product_A_rk");
                m_product_B_rk.setName(m_sLogicID + "___" + m_sSymbolCFD + "___" + "m_product_B_rk");
            }
        }
        public void OnDeInit()
        {
            m_rates.OnDeInit();
            if ( m_bUseRenko)
            {
                m_product_A_rk.OnDeInit();
                m_product_B_rk.OnDeInit();
            }

            CFATLogger.record_reates_done();
        }
        public CCacheData getCacheData()
        {
            return m_rates;
        }
        public CCacheData getCacheData_rk(int nID)
        {
            if (nID == 0) return m_product_A_rk;
            if (nID == 1) return m_product_B_rk;
            return null;
        }
        public void setRenkoInfo(bool bUseRenko = false, double dRenkoStep = 0)
        {
            if (!bUseRenko)
                return ;

            m_bUseRenko = true;
            m_dRenkoStep = dRenkoStep;

            m_product_A_rk = new CCacheData();
            m_product_B_rk = new CCacheData();


            m_rates.setRenkoStep(dRenkoStep);
        }

        private void push_renkoVal()
        {
            if (m_rates.getRenko(0) == null) return;
            if ( Math.Abs(m_dRenkoVal_last - m_rates.getRenko(0).dAsk) < CFATCommon.ESP )
                return;

            m_product_A_rk.pushTick(m_product_A.m_dAsk, m_product_A.m_dBid, m_product_A.getTickTime());
            
//HSM_CCV2
            if (m_nType_price == EPRODUCT_TYPE_PRICE.A_BC)
            {
                m_product_B_rk.pushTick(m_product_B.m_dAsk * m_product_C.getRenko(0).dAsk / m_dRatio_AB,
                                        m_product_B.m_dBid * m_product_C.getRenko(0).dBid / m_dRatio_AB,
                                        m_product_B.getTickTime());
            }

            if (m_nType_price == EPRODUCT_TYPE_PRICE.A_B05C)
            {
                m_product_B_rk.pushTick(m_product_B.m_dAsk * 0.5 * (m_product_C.m_dAsk  + USDCNY_C0)/ m_dRatio_AB,
                                        m_product_B.m_dBid * 0.5 * (m_product_C.m_dBid  + USDCNY_C0)/ m_dRatio_AB,
                                        m_product_B.getTickTime());
            }

            if ( m_nType_price == EPRODUCT_TYPE_PRICE.A_B)
            {
                m_product_B_rk.pushTick(m_product_B.m_dAsk, m_product_B.m_dBid , m_product_B.getTickTime());
            }

//             string sRates = string.Format("{0},{1},{2},{3},{4}", CFATCommon.m_dtCurTime, 
//                 m_product_A_rk.getTick(0).dAsk, m_product_A_rk.getTick(0).dBid,
//                 m_product_B_rk.getTick(0).dAsk, m_product_B_rk.getTick(0).dBid);
//             CFATLogger.record_rates("Test\\renko", sRates);

            m_dRenkoVal_last = m_rates.getRenko(0).dAsk;
        }

        public bool updateRates()
        {
            m_product_A.getRates();
            m_product_B.getRates();

            if (m_nType_price != EPRODUCT_TYPE_PRICE.A_B)
                m_product_C.getRates();

            switch (m_nType_price)
            {
                case EPRODUCT_TYPE_PRICE.A_B:
                    m_dBid = m_product_A.m_dBid - m_product_B.m_dAsk;
                    m_dAsk = m_product_A.m_dAsk - m_product_B.m_dBid;
                    break;
                case EPRODUCT_TYPE_PRICE.A_BC:
                    m_dBid = m_product_A.m_dBid - m_product_B.m_dAsk * m_product_C.getRenko(0).dAsk / m_dRatio_AB;
                    m_dAsk = m_product_A.m_dAsk - m_product_B.m_dBid * m_product_C.getRenko(0).dBid / m_dRatio_AB;
                    break;
                case EPRODUCT_TYPE_PRICE.A_B05C:
                    m_dBid = m_product_A.m_dBid - m_product_B.m_dAsk * 0.5 * (m_product_C.m_dAsk  + USDCNY_C0  )/ m_dRatio_AB;
                    m_dAsk = m_product_A.m_dAsk - m_product_B.m_dBid * 0.5 * (m_product_C.m_dBid  + USDCNY_C0  )/ m_dRatio_AB;
                    break;
                default:
                    return false;
            }

            m_rates.pushTick(m_dAsk, m_dBid, m_product_A.getTickTime());
            m_dMid = (m_dBid + m_dAsk) / 2;

            if ( m_bUseRenko)
                push_renkoVal();

            return true;
        }

        public bool isOrderProcessed()
        {
            if (!m_product_A.isOrderProcessed()) return false;
            if (!m_product_B.isOrderProcessed()) return false;

            if (m_nType_trade != EPRODUCT_TYPE_TRADE.A_B)
                if (!m_product_C.isOrderProcessed()) return false;
            return true;
        }

        public void publish_tick()
        {
            m_product_A.publish_tick();
            m_product_B.publish_tick();
            if (m_nType_price != EPRODUCT_TYPE_PRICE.A_B)
                m_product_C.publish_tick();

            //Publish self data
            string sTxt = "";
            TRatesMin ratesMin = m_rates.getMin(0);
            DateTime dtTime_cur = ratesMin.m_dtTime;

            if (m_dBid != m_dBid_published_tick || m_dAsk != m_dAsk_published_tick)
            {
                sTxt = string.Format("{0},{1},{2},{3},{4}", "CFD", m_sSymbolCFD, ratesMin.m_dtTime, m_dBid, m_dAsk);
                CMQClient.publish_msg(sTxt, CFATCommon.MQ_TOPIC_PRICE_TICK);
                m_dBid_published_tick = m_dBid;
                m_dAsk_published_tick = m_dAsk;
            }
            //-----------------------

        }

        public void publish_min()
        {
            m_product_A.publish_min();
            m_product_B.publish_min();
            if (m_nType_price != EPRODUCT_TYPE_PRICE.A_B)
                m_product_C.publish_min();


            //Publish self data
            string sTxt = "";
            TRatesMin ratesMin = m_rates.getMin(0);
            DateTime dtTime_cur = ratesMin.m_dtTime;

            if (dtTime_cur != m_dtTime_published_min)
            {
                ratesMin = m_rates.getMin(1);
                sTxt = string.Format("{0},{1},{2},{3},{4},{5},{6}", "CFD", m_sSymbolCFD, ratesMin.m_dtTime,
                    ratesMin.dBid_open, ratesMin.dBid_high, ratesMin.dBid_low, ratesMin.dBid_close);
                CMQClient.publish_msg(sTxt, CFATCommon.MQ_TOPIC_PRICE_MIN);
                m_dtTime_published_min = dtTime_cur;
            }
            //-----------------------
        }

        public void clearPositions()
        {
            m_product_A.clearPositions();
            m_product_B.clearPositions();
            if (m_nType_trade == EPRODUCT_TYPE_TRADE.A_BC)
                m_product_C.clearPositions();
        }

        public int getPosCount_vt()
        {
            return m_product_A.getPosCount_vt();
        }

        public ETRADER_OP getPosCmd_vt(int nPosIndex)
        {
            return m_product_A.getPosCmd_vt(nPosIndex);
        }

        public void requestOrder(ETRADER_OP nCmd, double dLots_req, bool bReqAsync = false)
        {
            double dReqPrice_A = 0;
            double dReqPrice_B = 0;
            double dReqPrice_C = 0;
            double dLots = dLots_req;

//HSM???
//             if (nCmd == ETRADER_OP.BUY_CLOSE || nCmd == ETRADER_OP.SELL_CLOSE)
//                 dLots = Math.Abs(m_product_A.getPosLots_Total_real()); //Some times there is different virtual & real lots

            if (nCmd == ETRADER_OP.BUY || nCmd == ETRADER_OP.SELL_CLOSE)
            {
                dReqPrice_A = m_product_A.m_dAsk;
                dReqPrice_B = m_product_B.m_dBid;
                if ( m_nType_trade == EPRODUCT_TYPE_TRADE.A_BC)
                    dReqPrice_C= m_product_C.m_dBid;
            }

            if (nCmd == ETRADER_OP.SELL || nCmd == ETRADER_OP.BUY_CLOSE)
            {
                dReqPrice_A = m_product_A.m_dBid;
                dReqPrice_B = m_product_B.m_dAsk;
                if (m_nType_trade == EPRODUCT_TYPE_TRADE.A_BC)
                    dReqPrice_C = m_product_C.m_dAsk;
            }

            if (!bReqAsync)
            {
                m_product_A.reqOrder(nCmd, ref dLots, ref dReqPrice_A, EORDER_TYPE.MARKET);//First Order
                m_product_B.reqOrder(TRADER.cmdOpposite(nCmd), ref dLots, ref dReqPrice_B, EORDER_TYPE.MARKET);

                if (m_nType_trade == EPRODUCT_TYPE_TRADE.A_BC)
                    m_product_C.reqOrder(TRADER.cmdOpposite(nCmd), ref dLots, ref dReqPrice_C, EORDER_TYPE.MARKET);
            }
            else
            {
                m_product_A.reqOrder_async(nCmd, dLots, dReqPrice_A, EORDER_TYPE.PENDING_STOP, 0);//First Order
                m_product_B.reqOrder_async(TRADER.cmdOpposite(nCmd), dLots * 0.32, dReqPrice_B, EORDER_TYPE.MARKET, 1);

                if (m_nType_trade == EPRODUCT_TYPE_TRADE.A_BC)
                    m_product_C.reqOrder_async(TRADER.cmdOpposite(nCmd), dLots * 0.46, dReqPrice_C, EORDER_TYPE.MARKET, 2);
            }
        }


    }
}