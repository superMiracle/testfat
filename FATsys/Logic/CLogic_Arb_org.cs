using System;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using FATsys.Utils;
using FATsys.TraderType;
using FATsys.Product;

namespace FATsys.Logic
{
    class CLogic_Arb_org : CLogic
    {
        public const int _FIRST = 0;
        public const int _SECOND = 1;

        double ex_dOpenLevel;
        double ex_dCloseLevel;
        double ex_dSlippage;
        double ex_dAddLots;

        int ex_nPosHoldTimeSec;
        int ex_nNewPosTimeSec;
        int ex_nUseRandomLots = 0;

        int ex_nUseMA = 0;
        int ex_nMAPeriod = 100;
        int ex_nMaxPosCnt = 1;

        bool ex_bAsyncOrder = false;

        TBenchMarking m_benchMarking = new TBenchMarking();

//         CCacheData m_cache_mid = new CCacheData();
        

        bool m_bIsTickUpdated = true;

        double m_dLastProfit = 0;

        ETRADE_RET m_nTradeRet = ETRADE_RET.NONE;

        string m_sErrSite = "";

        private CTradeHistory m_tradeHistory = new CTradeHistory();
        private DateTime m_dtLastFilledTime = DateTime.Now;
        private Random m_rndLots = new Random();
        // added by cmh
        ETRADER_OP m_nCmd4Thread;
        bool m_bIsFirstThreadLive = false;
        bool m_bIsSecondThreadLive = false;
        bool m_bIsOrderHappened = false;
        // ---
        public override bool OnInit()
        {
            loadParams();
            matchPosReal2Virtual();
            return base.OnInit();
        }
        public override void OnDeInit()
        {
            base.OnDeInit();
        }

        /// <summary>
        /// Called with every tick
        /// </summary>
        public override int OnTick()
        {
            if (CFATManager.isOnlineMode())
                m_benchMarking.push_Ontick_start(DateTime.Now); //For bench marking

            m_nTradeRet = ETRADE_RET.NONE;

            updateState();
            getRates();
            if (m_stState.m_nState == ELOGIC_STATE.NORMAL)
            {
                checkForClose();
                checkForOpen();
            }

            if(m_bIsOrderHappened && !m_bIsFirstThreadLive && !m_bIsSecondThreadLive)
            {
                setState(ELOGIC_STATE.NEED_CHECK_POSITION_MATCH);
                m_bIsOrderHappened = false;
            }

            publishToMQ();//For Manager

            if (CFATManager.isOnlineMode()) 
                m_benchMarking.push_Ontick_end(DateTime.Now);   //For bench marking

            base.OnTick();
            return (int)m_nTradeRet;
        }

        private void updateState()
        {
            bool bIsPositionMatch = false;
            if (m_stState.m_nState == ELOGIC_STATE.NEED_CHECK_POSITION_MATCH ||
                m_stState.m_nState == ELOGIC_STATE.WAITING_FORCE_CLOSE)
                bIsPositionMatch = isPositionMatch();

            if ( bIsPositionMatch )
            {//NORMAL
                setState(ELOGIC_STATE.NORMAL);
                return;
            }

            if (m_stState.m_nState == ELOGIC_STATE.NEED_CHECK_POSITION_MATCH)
            {//NEED_CHECK_POSITION_MATCH -> WAITING_FORCE_CLOSE
                if (m_stState.getSeconds_Now2UpdateTime() > 40) //If position don't matched during 40 seconds, Force close
                {
                    clearAllPositions();
                    setState(ELOGIC_STATE.WAITING_FORCE_CLOSE);
                    return;
                }
            }

            if (m_stState.m_nState == ELOGIC_STATE.WAITING_FORCE_CLOSE)
            {//WAITING_FORCE_CLOSE -> LOGIC_STOP_BY_ERROR
                if (m_stState.getSeconds_Now2UpdateTime() > 20)// If position don't matched during 20 seconds after force close
                {
                    setState(ELOGIC_STATE.LOGIC_STOP_BY_ERROR);
                    return;
                }
            }

            if (m_stState.m_nState == ELOGIC_STATE.LOGIC_STOP_BY_ERROR)
            {//LOGIC_STOP_BY_ERROR -> NORMAL
                if (m_stState.getSeconds_Now2UpdateTime() > 60) // logic stopped by error, after 60 seconds will be normal state
                {
                    setState(ELOGIC_STATE.NORMAL);
                    return;
                }
            }
        }

        public override void loadParams()
        {
            // modified by cmh
            try
            {
                ex_dOpenLevel = m_params.getVal_double("ex_dOpenLevel");
                ex_dCloseLevel = m_params.getVal_double("ex_dCloseLevel");
                ex_dSlippage = m_params.getVal_double("ex_dSlippage");
                ex_nPosHoldTimeSec = (int)m_params.getVal_double("ex_nPosHoldTimeSec");
                ex_nNewPosTimeSec = (int)m_params.getVal_double("ex_nNewPosTimeSec");
                ex_nUseRandomLots = (int)m_params.getVal_double("ex_nUseRandomLots");
                ex_dAddLots = m_params.getVal_double("ex_dAddLots");
                ex_nUseMA = (int)m_params.getVal_double("ex_nUseMA");
                ex_nMAPeriod = (int)m_params.getVal_double("ex_nMAPeriod");
                ex_nMaxPosCnt = (int)m_params.getVal_double("ex_nMaxPosCnt");
                ex_bAsyncOrder = Convert.ToBoolean(m_params.getVal_string("ex_bAsyncOrder"));
                base.loadParams();
            }
            catch(Exception e)
            {
                CFATLogger.output_proc("load params: " + e.Message);
            }
            
        }

        private void matchPosReal2Virtual()
        {
            m_products[_FIRST].updateRealPositions();
            m_products[_SECOND].updateRealPositions();
            CFATLogger.output_proc(string.Format("matchPosReal2Virtual : {0}, {1}", m_products[_FIRST].getPosLots_Total_real(), m_products[_SECOND].getPosLots_Total_real()));
            if (Math.Abs(m_products[_FIRST].getPosLots_Total_real() + m_products[_SECOND].getPosLots_Total_real()) < CFATCommon.ESP + ex_dAddLots)
            {
                m_products[_FIRST].matchPosReal2Virtual();
                m_products[_SECOND].matchPosReal2Virtual();
            }
        }

        public override void matchPosVirtual2Real() // Is this function need ???
        {
            m_products[_FIRST].matchPosVirtual2Real();
            m_products[_SECOND].matchPosVirtual2Real();
        }

        private void clearAllPositions()
        {
            CFATLogger.output_proc(string.Format("{0} : Position UnMatched!, clear position !!! ************** ", m_sLogicID));
            m_products[_FIRST].clearPositions();
            m_products[_SECOND].clearPositions();
        }

        private bool isPositionMatch()
        {

            if (CFATManager.m_nRunMode != ERUN_MODE.REALTIME)
                return true;

            foreach (CProduct product in m_products)
            {
                if (!product.isPositionMatch())
                    return false;
            }

            // If there is different virtual lots with first site and second site.
            //Here is problem when using multi position with ex_dAddLots !!!!!!!!!!
            if (Math.Abs(m_products[_FIRST].getPosLots_Total_vt() + m_products[_SECOND].getPosLots_Total_vt()) > ex_dAddLots + CFATCommon.ESP)
                return false;

            // IF there is different real lots with first site and second site
            if (Math.Abs(m_products[_FIRST].getPosLots_Total_real() + m_products[_SECOND].getPosLots_Total_real()) > ex_dAddLots + CFATCommon.ESP)
                return false;

            CFATLogger.output_proc(string.Format("Position matched! sym1={0},real lots = {1}, sym2={2}, real lots = {3}",
                m_products[_FIRST].getSymbol(), m_products[_FIRST].getPosLots_Total_real(),
                m_products[_SECOND].getSymbol(), m_products[_SECOND].getPosLots_Total_real()));

            return true;
        }

        private void getRates()
        {
            m_products[_FIRST].getRates();
            m_products[_SECOND].getRates();

            //If use moving average for signal
//             if (ex_nUseMA == 1)
//             {
//                 double dMid = m_products[_FIRST].m_dMid - m_products[_SECOND].m_dMid;
//                 m_cache_mid.pushTick(dMid, dMid, CFATCommon.m_dtCurTime);
//             }
        }

        private bool isTickUpdated()
        {
            DateTime dtLastTime_first = m_products[_FIRST].getTick(0).m_dtTime;
            DateTime dtLastTime_second = m_products[_SECOND].getTick(0).m_dtTime;

            DateTime dtTimeShift_first = m_products[_FIRST].getTick(20).m_dtTime;
            DateTime dtTimeShift_second = m_products[_SECOND].getTick(20).m_dtTime;

            //is second product update  ?
            if (dtTimeShift_first > dtLastTime_second)
            {
                m_sErrSite = m_products[_SECOND].getSiteName();
                return false;
            }

            //is first product update  ?
            if ( dtTimeShift_second > dtLastTime_first)
            {
                m_sErrSite = m_products[_FIRST].getSiteName();
                return false;
            }

            return true;
        }

        public override void publishVariables()
        {
            m_vars_publish.Clear();
            m_vars_publish.Add("diff", (m_products[_FIRST].m_dMid - m_products[_SECOND].m_dMid).ToString());
            m_vars_publish.Add("ask1", m_products[_FIRST].m_dAsk.ToString());
            m_vars_publish.Add("bid1", m_products[_FIRST].m_dBid.ToString());
            m_vars_publish.Add("ask2", m_products[_SECOND].m_dAsk.ToString());
            m_vars_publish.Add("bid2", m_products[_SECOND].m_dBid.ToString());
            m_vars_publish.Add("logicState", m_stState.m_nState.ToString());
            m_vars_publish.Add("posCnt_1", m_products[_FIRST].getPosCount_vt().ToString());
            m_vars_publish.Add("posCnt_2", m_products[_SECOND].getPosCount_vt().ToString());
            m_vars_publish.Add("vLots_1", m_products[_FIRST].getPosLots_vt(0).ToString());
            m_vars_publish.Add("vLots_2", m_products[_SECOND].getPosLots_vt(0).ToString());
            m_vars_publish.Add("real_lots_01", m_products[_FIRST].getPosLots_Total_real().ToString());
            m_vars_publish.Add("real_lots_02", m_products[_SECOND].getPosLots_Total_real().ToString());
            m_vars_publish.Add("bench_start_start", m_benchMarking.getAverageMilliSecs_start_start(100).ToString());
            m_vars_publish.Add("bench_start_end", m_benchMarking.getAverageMilliSecs_start_end(100).ToString());
            base.publishVariables();
        }

        private void publishToMQ()
        {
            if (!CFATManager.isOnlineMode())
                return;

//             m_products[_FIRST].publish_tick();
//             m_products[_SECOND].publish_tick();

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

        private void check3Times_loss()
        {
            if (!CFATManager.isOnlineMode())
                return;

            m_dLastProfit = m_tradeHistory.getProfit(0);

            if (ex_nIsNewOrder <= 0)
                return;

            if (m_tradeHistory.getTradeCount() < 3)
                return;

            double dProfit = 0;
            for (int i = 0; i < 3; i++)
            {
                dProfit = m_tradeHistory.getProfit(i);
                if (dProfit > 0)
                    return;
            }

            CFATLogger.output_proc(m_sLogicID + " : 3 times loss, ex_nIsNewOrder will be 0.");
            setParam_newOrder(0);// continuously 3 times loss, stop trading.
        }


        private void checkForClose()
        {
            // added by cmh
            if (m_bIsFirstThreadLive || m_bIsSecondThreadLive)
                return;

            if (m_products[_FIRST].m_site.getStatus() == Site.CSite.SITE_STATUS.PROCESSING || m_products[_SECOND].m_site.getStatus() == Site.CSite.SITE_STATUS.PROCESSING)
                return;
            // ---
            if (m_products[_FIRST].getPosCount_vt() == 0)
                return;

            if ((DateTime.Now - m_dtLastFilledTime).TotalSeconds < ex_nPosHoldTimeSec)
                return;

            
            int nSignal = getSignal();
            ETRADER_OP nCmd = m_products[_FIRST].getPosCmd_vt(0);

            double dLots = ex_dLots;
            if (nCmd == ETRADER_OP.BUY && TRADER.isContain(nSignal, (int)ETRADER_OP.BUY_CLOSE))
            {
                if (CFATManager.isOnlineMode())
                    CFATLogger.output_proc("close buy !");
                requestOrder(ETRADER_OP.BUY_CLOSE);
                check3Times_loss();
                return;
            }

            if (nCmd == ETRADER_OP.SELL && TRADER.isContain(nSignal, (int)ETRADER_OP.SELL_CLOSE))
            {
                if (CFATManager.isOnlineMode())
                    CFATLogger.output_proc("close sell !");
                requestOrder(ETRADER_OP.SELL_CLOSE);
                check3Times_loss();
                return;
            }
        }
        private double getSecondLots(double dLots)
        {
            double dRet = dLots - ex_dAddLots;
            if (dRet < 0.01)
                dRet = 0.01;
            return dRet;
        }
        private double getRandomLots(double dLots)
        {
            double dRet = dLots;
            double dRndLots = m_rndLots.Next(0, 5);
            dRet -= dRndLots / 10;
            if (dRet < 0.1)
                dRet = 0.1;
            return dRet;
        }
        private void requestOrder(ETRADER_OP nCmd)
        {
            // modified by cmh
            if (ex_bAsyncOrder)
            {
                m_nCmd4Thread = nCmd;
                m_bIsFirstThreadLive = true;
                m_bIsSecondThreadLive = true;
                m_bIsOrderHappened = true;
                m_products[_FIRST].m_site.setStatus(Site.CSite.SITE_STATUS.PROCESSING);
                m_products[_SECOND].m_site.setStatus(Site.CSite.SITE_STATUS.PROCESSING);
                Thread thFirstOrder = new Thread(requestFirstSiteOrder);
                Thread thSecondOrder = new Thread(requestSecondSiteOrder);
                thFirstOrder.Start();
                thSecondOrder.Start();
                return;
            }
            // ---

            double dReqPrice_first = 0;
            double dReqPrice_second = 0;
            double dLots = ex_dLots;

            if (ex_nUseRandomLots == 1)
                dLots = getRandomLots(dLots);

            dLots += ex_dAddLots;

            int nProfit_calc_01 = 0;
            int nProfit_calc_02 = 0;
            EFILLED_STATE nRet;

            if ( (nCmd == ETRADER_OP.BUY || nCmd == ETRADER_OP.SELL) &&  m_products[_FIRST].getPosCount_vt() == 0 )
                setParam_newOrder(ex_nIsNewOrder - 1);

            if (nCmd == ETRADER_OP.BUY_CLOSE || nCmd == ETRADER_OP.SELL_CLOSE)
                dLots = Math.Abs(m_products[_FIRST].getPosLots_vt(0));

            if (dLots < CFATCommon.ESP)
            {
                CFATLogger.output_proc("*** Invalid Lots !!!, pls check virtual postion!!!!");
                dLots = ex_dLots;
            }

            if (nCmd == ETRADER_OP.BUY || nCmd == ETRADER_OP.SELL_CLOSE)
            {
                dReqPrice_first = m_products[_FIRST].m_dAsk;
                dReqPrice_second = m_products[_SECOND].m_dBid;
                //dReqPrice_self = m_dAsk_First_low;
                nProfit_calc_01 = -1;
                nProfit_calc_02 = 1;
            }

            if (nCmd == ETRADER_OP.SELL || nCmd == ETRADER_OP.BUY_CLOSE)
            {
                dReqPrice_first = m_products[_FIRST].m_dBid;
                dReqPrice_second = m_products[_SECOND].m_dAsk;
                //dReqPrice_self = m_dBid_First_high;
                nProfit_calc_01 = 1;
                nProfit_calc_02 = -1;
            }

            m_nTradeRet = ETRADE_RET.NEWORDER;
            
            nRet = m_products[_FIRST].reqOrder(nCmd, ref dLots, ref dReqPrice_first, EORDER_TYPE.MARKET);//First Order
            if (nRet == EFILLED_STATE.FULL)
            {
                m_tradeHistory.pushHistory(dReqPrice_first * nProfit_calc_01);
                dLots = getSecondLots(dLots);
                nRet = m_products[_SECOND].reqOrder(TRADER.cmdOpposite(nCmd), ref dLots, ref dReqPrice_second, EORDER_TYPE.MARKET);
                if (nRet == EFILLED_STATE.FULL)
                {
                    m_tradeHistory.pushHistory(dReqPrice_second * nProfit_calc_02);
                    if (nCmd == ETRADER_OP.SELL_CLOSE || nCmd == ETRADER_OP.BUY_CLOSE)
                        m_tradeHistory.settleHistory();
                }
                else
                {
                    CFATLogger.output_proc("second order failed!");
                    m_tradeHistory.setProfitAsZero();
                    clearAllPositions();
                    setState(ELOGIC_STATE.WAITING_FORCE_CLOSE);
                    return;
                }
            }
            else
                CFATLogger.output_proc("first order failed!");
                        
            setState(ELOGIC_STATE.NEED_CHECK_POSITION_MATCH);
            m_dtLastFilledTime = DateTime.Now;
        }

        // added by cmh
        public void requestFirstSiteOrder()
        {
            
            double dReqPrice_first = 0;
            double dLots = ex_dLots;

            if (ex_nUseRandomLots == 1)
                dLots = getRandomLots(dLots);

            dLots += ex_dAddLots;

            int nProfit_calc_01 = 0;
            EFILLED_STATE nRet;

            if ((m_nCmd4Thread == ETRADER_OP.BUY || m_nCmd4Thread == ETRADER_OP.SELL) && m_products[_FIRST].getPosCount_vt() == 0)
                setParam_newOrder(ex_nIsNewOrder - 1);

            if (m_nCmd4Thread == ETRADER_OP.BUY_CLOSE || m_nCmd4Thread == ETRADER_OP.SELL_CLOSE)
                dLots = Math.Abs(m_products[_FIRST].getPosLots_vt(0));

            if (dLots < CFATCommon.ESP)
            {
                CFATLogger.output_proc("*** Invalid Lots !!!, pls check virtual position!!!!");
                dLots = ex_dLots;
            }

            if (m_nCmd4Thread == ETRADER_OP.BUY || m_nCmd4Thread == ETRADER_OP.SELL_CLOSE)
            {
                dReqPrice_first = m_products[_FIRST].m_dAsk;
                nProfit_calc_01 = -1;
            }

            if (m_nCmd4Thread == ETRADER_OP.SELL || m_nCmd4Thread == ETRADER_OP.BUY_CLOSE)
            {
                dReqPrice_first = m_products[_FIRST].m_dBid;
                nProfit_calc_01 = 1;
            }
            
            nRet = m_products[_FIRST].reqOrder(m_nCmd4Thread, ref dLots, ref dReqPrice_first, EORDER_TYPE.MARKET);//First Order
            if (nRet == EFILLED_STATE.FULL)
            {
                m_tradeHistory.pushHistory(dReqPrice_first * nProfit_calc_01);
                dLots = getSecondLots(dLots);                
            }
            else
                CFATLogger.output_proc("first order failed!");

            // setState(ELOGIC_STATE.NEED_CHECK_POSITION_MATCH);
            m_dtLastFilledTime = DateTime.Now;
            m_bIsFirstThreadLive = false;
            m_products[_FIRST].m_site.setStatus(Site.CSite.SITE_STATUS.NONE);
        }

        public void requestSecondSiteOrder()
        {
            
            double dReqPrice_second = 0;
            double dLots = ex_dLots;

            if (ex_nUseRandomLots == 1)
                dLots = getRandomLots(dLots);

            dLots += ex_dAddLots;
            
            int nProfit_calc_02 = 0;
            EFILLED_STATE nRet;
            
            if (m_nCmd4Thread == ETRADER_OP.BUY_CLOSE || m_nCmd4Thread == ETRADER_OP.SELL_CLOSE)
                dLots = Math.Abs(m_products[_SECOND].getPosLots_vt(0));

            if (dLots < CFATCommon.ESP)
            {
                CFATLogger.output_proc("*** Invalid Lots !!!, pls check virtual position!!!!");
                dLots = ex_dLots;
            }

            if (m_nCmd4Thread == ETRADER_OP.BUY || m_nCmd4Thread == ETRADER_OP.SELL_CLOSE)
            {
                dReqPrice_second = m_products[_SECOND].m_dBid;
                nProfit_calc_02 = 1;
            }

            if (m_nCmd4Thread == ETRADER_OP.SELL || m_nCmd4Thread == ETRADER_OP.BUY_CLOSE)
            {
                dReqPrice_second = m_products[_SECOND].m_dAsk;
                nProfit_calc_02 = -1;
            }

            dLots = getSecondLots(dLots);
            nRet = m_products[_SECOND].reqOrder(TRADER.cmdOpposite(m_nCmd4Thread), ref dLots, ref dReqPrice_second, EORDER_TYPE.MARKET);
            if (nRet == EFILLED_STATE.FULL)
            {
                m_tradeHistory.pushHistory(dReqPrice_second * nProfit_calc_02);
                if (m_nCmd4Thread == ETRADER_OP.SELL_CLOSE || m_nCmd4Thread == ETRADER_OP.BUY_CLOSE)
                    m_tradeHistory.settleHistory();
            }
            else
            {
                CFATLogger.output_proc("second order failed!");
                m_tradeHistory.setProfitAsZero();
                clearAllPositions();
                setState(ELOGIC_STATE.WAITING_FORCE_CLOSE);
                return;
            }

            // setState(ELOGIC_STATE.NEED_CHECK_POSITION_MATCH);
            m_dtLastFilledTime = DateTime.Now;
            m_bIsSecondThreadLive = false;
            m_products[_SECOND].m_site.setStatus(Site.CSite.SITE_STATUS.NONE);
        }
        // ---
        private void checkForOpen()
        {
            // added by cmh
            if (m_bIsFirstThreadLive || m_bIsSecondThreadLive)
                return;

            if (m_products[_FIRST].m_site.getStatus() == Site.CSite.SITE_STATUS.PROCESSING || m_products[_SECOND].m_site.getStatus() == Site.CSite.SITE_STATUS.PROCESSING)
                return;
            // ---
            if (m_products[_FIRST].getPosCount_vt() >= ex_nMaxPosCnt)
                return;

            if ((DateTime.Now - m_dtLastFilledTime).TotalSeconds < ex_nNewPosTimeSec)
                return;

            if (m_nTradeRet == ETRADE_RET.NEWORDER)// If position closed at checkForClose with same tick, 
                return;
            

            int nSignal = getSignal();


            if (TRADER.isContain(nSignal, (int)ETRADER_OP.BUY))
            {
                if (m_products[_FIRST].getPosCount_vt() > 0)
                {
                    if (m_products[_FIRST].getPosCmd_vt(0) != ETRADER_OP.BUY)
                        return;
                }

                if (ex_nIsNewOrder > 0)
                {
                    if (CFATManager.isOnlineMode())
                        CFATLogger.output_proc(string.Format("open buy ! bid = {0}, ask = {1}, bid = {2}, ask = {3}", 
                            m_products[_FIRST].m_dBid, m_products[_FIRST].m_dAsk, m_products[_SECOND].m_dBid, m_products[_SECOND].m_dAsk));
                    requestOrder(ETRADER_OP.BUY);
                }
                //                 else
                //                     CFATLogger.output_proc(string.Format("{0} : Signal Buy! diff = {1}, bid = {2}, ask = {3}, bid = {4}, ask = {5}",m_sLogicID, m_products[_SECOND].m_dBid - m_products[_FIRST].m_dAsk,
                //                         m_products[_FIRST].m_dBid, m_products[_FIRST].m_dAsk, m_products[_SECOND].m_dBid, m_products[_SECOND].m_dAsk));
            }

            if (TRADER.isContain(nSignal, (int)ETRADER_OP.SELL))
            {
                if (m_products[_FIRST].getPosCount_vt() > 0)
                {
                    if (m_products[_FIRST].getPosCmd_vt(0) != ETRADER_OP.SELL)
                        return;
                }

                if (ex_nIsNewOrder > 0)
                {
                    if (CFATManager.isOnlineMode())
                        CFATLogger.output_proc(string.Format("open sell ! bid = {0}, ask = {1}, bid = {2}, ask = {3}", 
                            m_products[_FIRST].m_dBid, m_products[_FIRST].m_dAsk, m_products[_SECOND].m_dBid, m_products[_SECOND].m_dAsk));
                    requestOrder(ETRADER_OP.SELL);
                }
                //                 else
                //                     CFATLogger.output_proc(string.Format("{0} : Signal Sell! diff = {1}, bid = {2}, ask = {3}, bid = {4}, ask = {5}",m_sLogicID, m_products[_FIRST].m_dBid - m_products[_SECOND].m_dAsk,
                //                         m_products[_FIRST].m_dBid, m_products[_FIRST].m_dAsk, m_products[_SECOND].m_dBid, m_products[_SECOND].m_dAsk));

            }
        }

        private void checkUpdateTick()
        {
            if (!CFATManager.isOnlineMode())
                return;

            bool bIsTickUpdated = isTickUpdated();

            if (m_bIsTickUpdated != bIsTickUpdated)
            {
                if (!bIsTickUpdated)
                    CFATLogger.output_proc(string.Format("--------->don't update ticks :logic = {0}, site ={1}", m_sLogicID, m_sErrSite));
                else
                    CFATLogger.output_proc(string.Format("<---------update ticks :logic= {0}, site = {1}", m_sLogicID, m_sErrSite));
            }
            m_bIsTickUpdated = bIsTickUpdated;
        }

        private int getSignal()
        {
            int nRetSignal = (int)ETRADER_OP.NONE;

            if (Math.Abs(m_products[_FIRST].m_dMid - m_products[_SECOND].m_dMid) > ex_dOpenLevel * 10) //Invalid price
                return nRetSignal;

            //check Update Tick
            checkUpdateTick();
            if (!m_bIsTickUpdated)
                return nRetSignal;
            //---------------

            double dAvg_Mid = 0;
            
//             if (ex_nUseMA == 1)
//                 dAvg_Mid = m_cache_mid.getMA_tick(ex_nMAPeriod);

            //CheckFor Buy : A -> Buy & B -> Sell
            if (m_products[_FIRST].m_dAsk < m_products[_SECOND].m_dBid - ex_dOpenLevel + dAvg_Mid)
                nRetSignal |= (int)ETRADER_OP.BUY;

            //CheckFor Sell : A -> Sell & B -> Buy
            if (m_products[_FIRST].m_dBid > m_products[_SECOND].m_dAsk + ex_dOpenLevel + dAvg_Mid)
                nRetSignal |= (int)ETRADER_OP.SELL;

            //CheckFor Buy Close : A -> Buy Close & B -> Sell Close
            if (m_products[_FIRST].m_dBid > m_products[_SECOND].m_dAsk + ex_dCloseLevel + dAvg_Mid)
                nRetSignal |= (int)ETRADER_OP.BUY_CLOSE;
            
            //CheckFor Sell Close: A -> Sell Close & B -> Buy Close
            if (m_products[_FIRST].m_dAsk < m_products[_SECOND].m_dBid - ex_dCloseLevel + dAvg_Mid)
                nRetSignal |= (int)ETRADER_OP.SELL_CLOSE;

            return nRetSignal;
        }
    }
}
