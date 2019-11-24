using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FATsys.Utils;
using FATsys.TraderType;
using FATsys.Product;
using FATsys.Logic.Indicators;

/// <summary>
/// Trading between CC and USDCNY
/// It's meaning get signal from Diff(CC, USDCNY)
/// </summary>
namespace FATsys.Logic
{
    class CLogic_Pair_V3 : CLogic
    {
        double ex_dOpen_usdcny;
        double ex_dClose_usdcny;

        int ex_nCCPeriod = 100;

        double ex_dRenkoStep;

        double ex_dP_ABS;
        double ex_dP_MIN;

        bool ex_bPublishRates = false;
        string ex_sProductType = "ABC";

        CProductCFD m_product_diff = new CProductCFD();

        CIndCC m_indCC = new CIndCC();

        TBenchMarking m_benchMarking = new TBenchMarking();

        public override void loadParams()
        {
            ex_dOpen_usdcny = m_params.getVal_double("ex_dOpen_usdcny");
            ex_dClose_usdcny = m_params.getVal_double("ex_dClose_usdcny");

            ex_dP_ABS = m_params.getVal_double("ex_dP_ABS");
            ex_dP_MIN = m_params.getVal_double("ex_dP_MIN");

            ex_nCCPeriod = (int)m_params.getVal_double("ex_nCCPeriod");

            ex_dRenkoStep = m_params.getVal_double("ex_dRenkoStep");
            ex_bPublishRates = Convert.ToBoolean(m_params.getVal_string("ex_bPublishRates"));
            ex_sProductType = m_params.getVal_string("ex_sProductType");

            base.loadParams();
        }

        public override bool OnInit()
        {
            loadParams();

            //ProductCFD define

            m_product_diff.setProductA(m_products[0]); //SH Gold
            m_product_diff.setProductB(m_products[1]); //LD Gold
            m_product_diff.setProductC(m_products[2]); //USDCNY

            m_product_diff.setProductInfo(m_sLogicID, "SH_LD_DIFF");
            m_product_diff.setProductType(EPRODUCT_TYPE_PRICE.A_BC, EPRODUCT_TYPE_TRADE.A_BC);

            m_product_diff.setRenkoInfo(true, ex_dRenkoStep);

            m_product_diff.setName();

            m_product_diff.OnInit(); //load data from xml file
            //-----------------------------------------------------

            //Indicator define
            m_indCC.OnInit();

            m_indCC.setCacheData(m_product_diff.getCacheData_rk(0), m_product_diff.getCacheData_rk(1));

            //-----------------------------------------------------
            return base.OnInit();
        }

        public override void OnDeInit()
        {
            m_product_diff.OnDeInit();
            base.OnDeInit();
        }

        public override int OnTick()
        {
            if (CFATManager.isOnlineMode())
                m_benchMarking.push_Ontick_start(DateTime.Now); //For bench marking

            updateState_v2(40, 20, 60);//wait check pos : 40s, wait force close : 20s , restart logic after stop by error : 60s

            //Here is update price and indicators pattern
            //First update price, after update indicators
            m_product_diff.updateRates();
            updateIndicators();
            //----------------------------------------------

            if (m_stState.m_nState == ELOGIC_STATE.NORMAL)
            {
                checkForClose();
                checkForOpen();
            }

            publishToMQ();//For Manager

            if (CFATManager.isOnlineMode())
                m_benchMarking.push_Ontick_end(DateTime.Now);   //For bench marking

            return base.OnTick();
        }

        private void checkForClose()
        {
            if (m_product_diff.getPosCount_vt() == 0)
                return;

            //CheckForClose by Signal line
            int nSignal = getSignal();

            ETRADER_OP nCmd = m_product_diff.getPosCmd_vt(0);

            if (nCmd == ETRADER_OP.BUY && TRADER.isContain(nSignal, (int)ETRADER_OP.BUY_CLOSE) )
            {
                requestOrder(ETRADER_OP.BUY_CLOSE);
                return;
            }

            if (nCmd == ETRADER_OP.SELL && TRADER.isContain(nSignal, (int)ETRADER_OP.SELL_CLOSE))
            {
                requestOrder(ETRADER_OP.SELL_CLOSE);
                return;
            }

            /*
            //CheckForClose by Absolution profit
            if ( m_products[0].getPosProfit_vt(0) + m_products[1].getPosProfit_vt(0) * m_products[2].m_dMid >= ex_dP_ABS )
            {
                if (nCmd == ETRADER_OP.BUY)
                    requestOrder(ETRADER_OP.BUY_CLOSE);
                if (nCmd == ETRADER_OP.SELL)
                    requestOrder(ETRADER_OP.SELL_CLOSE);
                return;
            }
            
            //CheckForClose by MarketEndTime with minimum profit, 15:00~ 15:30
            if (CFATCommon.m_dtCurTime.Hour == 15)
            {
                if (m_products[0].getPosProfit_vt(0) + m_products[1].getPosProfit_vt(0) * m_products[2].m_dMid >= ex_dP_MIN)
                {
                    if (nCmd == ETRADER_OP.BUY)
                        requestOrder(ETRADER_OP.BUY_CLOSE);
                    if (nCmd == ETRADER_OP.SELL)
                        requestOrder(ETRADER_OP.SELL_CLOSE);
                    return;
                }
            }
            */
        }

        private void checkForOpen()
        {
            if (m_product_diff.getPosCount_vt() > 0)
                return;
            if (CFATCommon.m_dtCurTime.Hour == 15)
                return;

            int nSignal = getSignal();

            if (TRADER.isContain(nSignal, (int)ETRADER_OP.BUY))
            {
                if (ex_nIsNewOrder > 0)
                    requestOrder(ETRADER_OP.BUY);
            }

            if (TRADER.isContain(nSignal, (int)ETRADER_OP.SELL))
            {
                if (ex_nIsNewOrder > 0)
                    requestOrder(ETRADER_OP.SELL);
            }
        }

        public void requestOrder(ETRADER_OP nCmd)
        {
            if (nCmd == ETRADER_OP.BUY || nCmd == ETRADER_OP.SELL)
                setParam_newOrder(ex_nIsNewOrder - 1);

            if (CFATManager.isOnlineMode())
                CFATLogger.output_proc(string.Format("Order : {0}, diff = {1}", nCmd.ToString(), m_product_diff.m_dMid));

            m_product_diff.requestOrder(nCmd, ex_dLots, true);
            if (CFATManager.isOnlineMode())
                setState(ELOGIC_STATE.WAITING_ORDER_RESPONSE);
            else
                setState(ELOGIC_STATE.NORMAL);
            //setState(ELOGIC_STATE.NEED_CHECK_POSITION_MATCH);
        }

        private int getSignal()
        {
            if (m_sMode == "unit_test")
                return getSignal_unitTest();

            int nRetSig = (int)ETRADER_OP.NONE;

            double dCC = m_indCC.getVal("CC");
            if (dCC == 1.0)
                return nRetSig;
            double dCCUSDCNH = dCC * 31.103477;
            double dDiff = m_products[2].getMid() - dCCUSDCNH;

            if ( dDiff > ex_dOpen_usdcny )
            {//SH Buy, LD Sell, FX Sell
                nRetSig |= (int)ETRADER_OP.BUY;
            }
            if ( dDiff < ex_dOpen_usdcny * (-1))
            {//SH Sell, LD Buy, FX Buy
                nRetSig |= (int)ETRADER_OP.SELL;
            }
            if (dDiff > ex_dClose_usdcny)
            {//SH SellClose, LD BuyClose, FX BuyClose
                nRetSig |= (int)ETRADER_OP.SELL_CLOSE;
            }

            if (dDiff < ex_dClose_usdcny * (-1))
            {//SH BuyClose, LD SellClose, FX SellClose
                nRetSig |= (int)ETRADER_OP.BUY_CLOSE;
            }

            return nRetSig;
        }

        public void updateIndicators()
        {
            //HSM_CCV2
            //             m_indCC.calc(m_products[0].getAsk(),
            //                 m_products[1].getBid() * m_products[2].getBid() / 31.103477, ex_nCCPeriod);

            m_indCC.calc(m_products[2].getMid(),
                m_products[2].getMid(), ex_nCCPeriod);

        }

        public override void doForceClose()
        {
            clearAllPositions();
            setState(ELOGIC_STATE.WAITING_FORCE_CLOSE);
        }
        public override void waitOrderFilled()
        {
            if (!m_product_diff.isOrderProcessed())
                return;
            setState(ELOGIC_STATE.NEED_CHECK_POSITION_MATCH);
        }

        #region ##### publish to MQ #########
        private void publish_rates_indicators()
        {
            if (CFATManager.m_nRunMode == ERUN_MODE.OPTIMIZE)
                return;

            if (!ex_bPublishRates) return;

            if (CFATManager.m_nRunMode == ERUN_MODE.BACKTEST)
            {
                m_product_diff.publish_min();
                m_indCC.publish_min();
                return;
            }

            if (CFATManager.isOnlineMode())
            {
                m_product_diff.publish_tick();
                m_indCC.publish_tick();
                return;
            }
        }

        public override void publishVariables()
        {
            m_vars_publish.Clear();
            m_vars_publish.Add("diff", m_product_diff.m_dMid.ToString());
            m_vars_publish.Add("SH price", m_products[0].getBid().ToString());
            m_vars_publish.Add("XAUUSD", m_products[1].getBid().ToString());
            m_vars_publish.Add("USDCNH", m_products[2].getBid().ToString());
            m_vars_publish.Add("logicState", m_stState.m_nState.ToString());
            m_vars_publish.Add("bench_start_start", m_benchMarking.getAverageMilliSecs_start_start(100).ToString());
            m_vars_publish.Add("bench_start_end", m_benchMarking.getAverageMilliSecs_start_end(100).ToString());
            base.publishVariables();
        }

        private void publishToMQ()
        {
            return;//HSM???
            publish_rates_indicators();

            if (!CFATManager.isOnlineMode())
                return;

            if ((DateTime.Now - m_dtLastPublish).TotalSeconds < 2) //every 2 seconds publish message
                return;

            try
            {
                //parameters : 
                publishParams();
                //history
                publishTradeHistory();
                //Variables
                publishVariables();

                if (m_stState.m_nState == ELOGIC_STATE.LOGIC_STOP_BY_ERROR)
                    CFATLogger.output_proc(string.Format(" **** {0} Logic Stoped!!!", m_sLogicID));

                m_dtLastPublish = DateTime.Now;
            }
            catch
            {
                CFATLogger.output_proc("CLogic_Arb_org : publishToMQ error!");
            }
        }
        #endregion

        private void clearAllPositions()
        {
            CFATLogger.output_proc(string.Format("{0} : Position UnMatched!, clear position !!! ************** ", m_sLogicID));
            m_product_diff.clearPositions();
        }
    }
}
