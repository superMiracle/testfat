using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FATsys.Utils;
using FATsys.TraderType;
using FATsys.Product;
using FATsys.Logic.Indicators;

namespace FATsys.Logic
{
    class CLogic_Pair_V1 : CLogic
    {
        double ex_dOpenLevel;
        double ex_dCloseLevel;
        double ex_dSlippage;
        int ex_nBandPeriod = 90;
        int ex_nCCPeriod = 100;
        double ex_dRenkoStep;
        bool ex_bPublishRates = false;
        string ex_sProductType = "ABC";

        CProductCFD m_product_diff = new CProductCFD();

        CIndBand m_indBand = new CIndBand();
        CIndCC m_indCC = new CIndCC();

        TBenchMarking m_benchMarking = new TBenchMarking();

        public override void loadParams()
        {
            ex_dOpenLevel = m_params.getVal_double("ex_dOpenLevel");
            ex_dCloseLevel = m_params.getVal_double("ex_dCloseLevel");
            ex_dSlippage = m_params.getVal_double("ex_dSlippage");
            ex_nBandPeriod = (int)m_params.getVal_double("ex_nBandPeriod");
            ex_nCCPeriod = (int)m_params.getVal_double("ex_nBandPeriod");
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
            m_indBand.OnInit();
            m_indCC.OnInit();

            m_indCC.setCacheData(m_product_diff.getCacheData_rk(0), m_product_diff.getCacheData_rk(1));
            m_indBand.setCacheData(m_indCC.getCacheData());
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
            if ( m_product_diff.getPosCount_vt() == 0)
                return;

            int nSignal = getSignal();
            ETRADER_OP nCmd = m_product_diff.getPosCmd_vt(0);

            if (nCmd == ETRADER_OP.BUY && TRADER.isContain(nSignal, (int)ETRADER_OP.BUY_CLOSE))
                requestOrder(ETRADER_OP.BUY_CLOSE);

            if (nCmd == ETRADER_OP.SELL && TRADER.isContain(nSignal, (int)ETRADER_OP.SELL_CLOSE))
                requestOrder(ETRADER_OP.SELL_CLOSE);
        }

        private void checkForOpen()
        {
            if (m_product_diff.getPosCount_vt() > 0)
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

            if ( CFATManager.isOnlineMode() )
                CFATLogger.output_proc(string.Format("Order : {0}, diff = {1}", nCmd.ToString(), m_product_diff.m_dMid));

            m_product_diff.requestOrder(nCmd, ex_dLots, true);
            if ( CFATManager.isOnlineMode() )
                setState(ELOGIC_STATE.WAITING_ORDER_RESPONSE);
            else
                setState(ELOGIC_STATE.NORMAL);
            //setState(ELOGIC_STATE.NEED_CHECK_POSITION_MATCH);
        }

        private int getSignal()
        {
            if (m_sMode == "unit_test")
                return getSignal_unitTest();

            int nSigCC = m_indBand.getSignal(ex_dOpenLevel, ex_dCloseLevel);

            return nSigCC;
        }

        public void updateIndicators()
        {
//HSM_CCV2
            m_indCC.calc(m_products[0].getAsk(), 
                m_products[1].getBid() * m_products[2].getBid() / 31.103477 ,ex_nCCPeriod);

//             m_indCC.calc(m_products[0].getAsk(),
//                 m_products[1].getBid(), ex_nCCPeriod);

            m_indBand.calc(ex_nBandPeriod);
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
                m_indBand.publish_min();
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
            //return;//HSM???
            publish_rates_indicators();

            if ( !CFATManager.isOnlineMode() )
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
