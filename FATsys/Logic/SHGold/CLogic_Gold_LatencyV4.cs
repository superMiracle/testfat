using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FATsys.Utils;

namespace FATsys.Logic.SHGold
{
    class CLogic_Gold_LatencyV4 : CLogic
    {
        public const int ID_SHANGHAI = 0;
        public const int ID_LONDON = 1;

        int ex_nOpenCalcSeconds = 5;
        double ex_dOpenLevelStart = 0.8;
        double ex_dOpenLevelEnd = 1.5;

        double ex_dOpenLevelStart_close = 0.8;
        double ex_dOpenLevelEnd_close = 1.5;

        double ex_dSlippage = 0.1;
        double ex_dSARStep = 0.02;
        double ex_dSARMax = 0.2;
        double ex_dLDStepForSAR = 0.1;
        int ex_nATRPeriod = 10;
        double ex_dATRMultiple = 3;
        double ex_dAlphaSAR = 0.001;

        double m_dATR = 0;

        double m_dTrailingStop_ld = 0;
        double m_dTrailingStop_sh = 0;
        double m_dAlpha = 0;
        double m_dMaxVal = 0;

        double m_dBid_Shanghai;
        double m_dAsk_Shanghai;
        double m_dMid_Shanghai;

        double m_dBid_London;
        double m_dAsk_London;
        double m_dMid_London;

        double m_dOpen_ld_midPrice = 0;

        double m_dOpen10TickSeconds = 0;

        bool m_bIsNeedCheckPosition = true;

        //For Log
        double m_dMaxProfit = -100;
        double m_dMinProfit = 100;
        DateTime m_dtMaxProfit;
        DateTime m_dtMinprofit;
        string m_sPrevRatesFolder = "";
        double m_dOpenDiff_sh = 0;
        double m_dOpenDiff_ld = 0;

        public override bool OnInit()
        {
            loadParams();
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
            getRates();
            calcSARVal();
            calcSHTrailingStop();
            //calcATRVal();
            if (checkPosMatch())
            {
                checkForClose();
                checkForOpen();
            }
            publishToMQ();//For Manager
            return base.OnTick();
        }

        private bool checkPosMatch()
        {
            if (CFATManager.m_nRunMode != ERUN_MODE.REALTIME)
            {
                m_bIsNeedCheckPosition = false;
                return true;
            }

            if (!m_bIsNeedCheckPosition)// if need to check position 
                return true;

            if (!m_products[ID_SHANGHAI].isPositionMatch())
                return false;

            m_bIsNeedCheckPosition = false;// Set as don't need check pasition 

            return true;
        }
        private void publishToMQ()
        {
            if (!CFATManager.isOnlineMode())
                return;

            if ((DateTime.Now - m_dtLastPublish).TotalSeconds < 2) //every 2 seconds publish message
                return;
            try
            {
                //parameters : 
                publishParams();

                //variables : logic_id@name1,val1@name2,val2@name3,val3.....
//                 string sTxt = string.Format("{0}@isNeedCheckPosition,{1}@sh_ask,{2}@sh_bid,{3}@ld_ask,{4}@ld_bid,{5}@g_dOpen_London,{6}@m_dExpectTp,{7}@posCnt,{8}@real lots,{9}@ex_nIsNewOrder,{10}@lastPosType,{11}@lastPosTime,{12}@m_nOpen_Ticks,{13}",
//                     m_sLogicID,
//                     m_bIsNeedCheckPosition,
//                     m_dAsk_Shanghai, m_dBid_Shanghai,
//                     m_dAsk_London, m_dBid_London,
//                     g_dOpen_London, m_dExpectTp,
//                     m_products[ID_SHANGHAI].getPosCount(), m_products[ID_SHANGHAI].getPosLots_real(),
//                     ex_nIsNewOrder, lastPosType, lastPosTime, m_nOpen_Ticks);
// 
//                 CMQClient.publish_msg(sTxt, CFATCommon.MQ_TOPIC_VARS);
                m_dtLastPublish = DateTime.Now;
            }
            catch
            {
                CFATLogger.output_proc("CLogic_gold_latencyV2 : publishToMQ error!");
            }
        }

        public override void loadParams()
        {

            ex_nOpenCalcSeconds = (int)m_params.getVal_double("ex_nOpenCalcSeconds");
            ex_dOpenLevelStart = m_params.getVal_double("ex_dOpenLevelStart");
            ex_dOpenLevelEnd = m_params.getVal_double("ex_dOpenLevelEnd");

            ex_dOpenLevelStart_close = m_params.getVal_double("ex_dOpenLevelStart_close");
            ex_dOpenLevelEnd_close = m_params.getVal_double("ex_dOpenLevelEnd_close");

            ex_dSlippage = m_params.getVal_double("ex_dSlippage");
            ex_dSARStep = m_params.getVal_double("ex_dSARStep");
            ex_dSARMax = m_params.getVal_double("ex_dSARMax");
            ex_dLDStepForSAR = m_params.getVal_double("ex_dLDStepForSAR");
            ex_nATRPeriod = (int)m_params.getVal_double("ex_nATRPeriod");
            ex_dATRMultiple = m_params.getVal_double("ex_dATRMultiple");
            ex_dAlphaSAR = m_params.getVal_double("ex_dAlphaSAR");

            base.loadParams();
        }

        private void getRates()
        {
            m_dBid_Shanghai = m_products[ID_SHANGHAI].getBid();
            m_dAsk_Shanghai = m_products[ID_SHANGHAI].getAsk();
            m_dMid_Shanghai = (m_dBid_Shanghai + m_dAsk_Shanghai) / 2;

            m_dBid_London = m_products[ID_LONDON].getBid();
            m_dAsk_London = m_products[ID_LONDON].getAsk();
            m_dMid_London = (m_dBid_London + m_dAsk_London) / 2;

            saveRates_forLog();
        }

        private double get10TickSecond()
        {
            return (m_products[ID_LONDON].getTickTime() - m_products[ID_LONDON].getTick(10).m_dtTime).TotalSeconds;
        }
        private bool isMarketCloseTime_forClose()
        {
            //02:29~02:30, 15:29~15:30 close order
            DateTime dtCurTime = m_products[ID_SHANGHAI].getTickTime();
            int nCurTime = dtCurTime.Hour * 60 + dtCurTime.Minute;
            if (nCurTime >= 2 * 60 + 29 && nCurTime <= 2 * 60 + 30)
                return true;

            if (nCurTime >= 15 * 60 + 29 && nCurTime <= 15 * 60 + 30)
                return true;

            return false;
        }

        private void setMinMax_forLog()
        {
            double dProfit = m_products[ID_SHANGHAI].getPosProfit_vt(0);
            if (m_dMaxProfit < dProfit)
            {
                m_dMaxProfit = dProfit;
                m_dtMaxProfit = m_products[ID_LONDON].getTickTime();
            }
            if (m_dMinProfit > dProfit)
            {
                m_dMinProfit = dProfit;
                m_dtMinprofit = m_products[ID_LONDON].getTickTime();
            }
        }

        private string getMinMaxStr_forLog()
        {
            return string.Format("maxTime,{0}, maxProfit,{1}, minTime,{2}, minProfit,{3}, sh_diff,{4}, ld_diff,{5}", 
                m_dtMaxProfit, m_dMaxProfit, m_dtMinprofit, m_dMinProfit, m_dOpenDiff_sh, m_dOpenDiff_ld);
        }

        private void saveRates_forLog()
        {
            if (m_products[ID_SHANGHAI].getPosCount_vt() == 0)
                return;
            if (CFATManager.m_nRunMode != ERUN_MODE.BACKTEST)
                return;
            string sRates = m_products[ID_SHANGHAI].getTickTime().ToString("yyyy/MM/dd HH:mm:ss.fff");
            
            string sLogFolder = "Test\\" + m_products[ID_SHANGHAI].getHistoryCount_vt().ToString();
            if (m_sPrevRatesFolder != sLogFolder)
            {//Write prev 100 ticks
                m_sPrevRatesFolder = sLogFolder;
                for ( int i = 100; i > 0; i --)
                {
                    sRates = m_products[ID_SHANGHAI].getTick(i).m_dtTime.ToString("yyyy/MM/dd HH:mm:ss.fff");
                    sRates += string.Format(",{0},{1},{2},{3},{4}", m_products[ID_SHANGHAI].getTick(i).dBid, m_products[ID_SHANGHAI].getTick(i).dAsk,
                        m_products[ID_LONDON].getTick(i).dBid, m_products[ID_LONDON].getTick(i).dAsk, m_dTrailingStop_ld);
                    CFATLogger.record_rates(sLogFolder, sRates);
                }
            }
            sRates = m_products[ID_SHANGHAI].getTickTime().ToString("yyyy/MM/dd HH:mm:ss.fff");
            sRates += string.Format(",{0},{1},{2},{3},{4},{5},{6}", m_dBid_Shanghai, m_dAsk_Shanghai, m_dBid_London, m_dAsk_London, m_dTrailingStop_ld, m_dMaxVal, m_dAlpha);
            CFATLogger.record_rates(sLogFolder, sRates);

        }

        private void checkForClose()
        {
            if (m_products[ID_SHANGHAI].getPosCount_vt() == 0)
                return;

            ETRADER_OP nCmd = m_products[ID_SHANGHAI].getPosCmd_vt(0);
            ETRADER_OP nCloseCmd = TRADER.getCloseCmd(nCmd);

            double dProfit = m_products[ID_SHANGHAI].getPosProfit_vt(0);
            setMinMax_forLog();

            if (isMarketCloseTime_forClose())
            {
                //m_products[ID_SHANGHAI].reqCloseAll("Market Time Close");
                requestOrder(nCloseCmd, getMinMaxStr_forLog());
                return;
            }


            int nSignal = getSignal();

            if (nCmd == ETRADER_OP.BUY && TRADER.isContain(nSignal, (int)ETRADER_OP.BUY_CLOSE))
            {
                //m_products[ID_SHANGHAI].reqCloseAll();
                requestOrder(ETRADER_OP.BUY_CLOSE, getMinMaxStr_forLog());
                return;
            }

            if (nCmd == ETRADER_OP.SELL && TRADER.isContain(nSignal, (int)ETRADER_OP.SELL_CLOSE))
            {

                //m_products[ID_SHANGHAI].reqCloseAll();
                requestOrder(ETRADER_OP.SELL_CLOSE, getMinMaxStr_forLog());
                return;
            }
        }

        private bool isMarketCloseTime_forOpen()
        {
            //02:25~02:30, 15:25~15:30 there is no enter.
            DateTime dtCurTime = m_products[ID_SHANGHAI].getTickTime();
            int nCurTime = dtCurTime.Hour * 60 + dtCurTime.Minute;
            if (nCurTime >= 2 * 60 + 25 && nCurTime <= 2 * 60 + 30)
                return true;

            if (nCurTime >= 15 * 60 + 25 && nCurTime <= 15 * 60 + 30)
                return true;
            return false;
        }

        private void requestOrder(ETRADER_OP nCmd, string sComment = "")
        {
            double dLots = ex_dLots;
            double dPrice = 0;
            double dCurPrice = 0;
            if (nCmd == ETRADER_OP.BUY || nCmd == ETRADER_OP.SELL_CLOSE)
            {//Buy or SellClose
                dCurPrice = m_dAsk_Shanghai;
                dPrice = m_dAsk_Shanghai + ex_dSlippage;
                if (nCmd == ETRADER_OP.BUY)
                {
                    m_dOpen_ld_midPrice = m_dMid_London;
                    m_dMaxProfit = -100;
                    m_dMinProfit = 100;
                }
                m_dOpen10TickSeconds = get10TickSecond();
            }
            else//Sell or BuyClose
            {
                dCurPrice = m_dBid_Shanghai;
                dPrice = m_dBid_Shanghai - ex_dSlippage;
                if (nCmd == ETRADER_OP.SELL)
                {
                    m_dOpen_ld_midPrice = m_dMid_London;
                    m_dMaxProfit = -100;
                    m_dMinProfit = 100;
                }
                m_dOpen10TickSeconds = get10TickSecond();
            }

            

            //For BackTest
            if ( !CFATManager.isOnlineMode() )
            {
                m_products[ID_SHANGHAI].reqOrder(nCmd, ref dLots, ref dPrice, EORDER_TYPE.MARKET, sComment);
                double dLDPrice = m_dMid_London;
                m_products[ID_LONDON].reqOrder(nCmd, ref dLots, ref dLDPrice, EORDER_TYPE.MARKET, sComment);
                return;
            }

            //For RealTime
            if (nCmd == ETRADER_OP.BUY_CLOSE || nCmd == ETRADER_OP.SELL_CLOSE)
            {
                if (ex_nIsNewOrder <= 0)
                    ex_nIsNewOrder = 1;
            }

            if (ex_nIsNewOrder > 0)
            {
                CFATLogger.output_proc(string.Format("Request order, cmd = {0}, current price ={1}, request price = {2}, request lost = {3}", TRADER.cmd2String(nCmd), dCurPrice, dPrice, dLots));
                m_products[ID_SHANGHAI].reqOrder(nCmd, ref dLots, ref dPrice, EORDER_TYPE.MARKET, sComment);
                CFATLogger.output_proc(string.Format("Response order, response price = {0}, response lost = {1}", dPrice, dLots));
                ex_nIsNewOrder--;
                m_bIsNeedCheckPosition = true;
            }
            else
                CFATLogger.output_proc(string.Format("new Signal = {0}, ex_nIsNewOrder = {1}", TRADER.cmd2String(nCmd), ex_nIsNewOrder));

        }

        private void checkForOpen()
        {
            if (m_products[ID_SHANGHAI].getPosCount_vt() == 1)
                return;

            DateTime dtCurTime = m_products[ID_SHANGHAI].getTickTime();

            if (isMarketCloseTime_forOpen())
                return;

            int nSignal = getSignal();

            if (TRADER.isContain(nSignal, (int)ETRADER_OP.BUY))
                requestOrder(ETRADER_OP.BUY);

            if (TRADER.isContain(nSignal, (int)ETRADER_OP.SELL))
                requestOrder(ETRADER_OP.SELL);
        }

        private void calcATRVal()
        {
            if (m_products[ID_SHANGHAI].getPosCount_vt() == 0)
                return;
            ETRADER_OP nCmd = m_products[ID_SHANGHAI].getPosCmd_vt(0);

            
            for ( int i = 0; i < ex_nATRPeriod; i ++ )
            {
                m_dATR += Math.Abs(m_products[ID_LONDON].getTick(i).dBid - m_products[ID_LONDON].getTick(i + 1).dBid);
            }
            m_dATR = m_dATR / ex_nATRPeriod;
            
            //m_dATR = m_products[ID_LONDON].getStd_tick(ex_nATRPeriod);
            if (m_dATR < 0.2)
                m_dATR = 0.2;
            double dSL = 0;
            if ( nCmd == ETRADER_OP.BUY)
            {
                dSL = m_dMid_London - m_dATR * ex_dATRMultiple;
                if (m_dTrailingStop_ld < dSL)
                    m_dTrailingStop_ld = (m_dTrailingStop_ld + dSL ) / 2;
            }

            if (nCmd == ETRADER_OP.SELL)
            {
                dSL = m_dMid_London + m_dATR * ex_dATRMultiple;
                if (m_dTrailingStop_ld > dSL)
                    m_dTrailingStop_ld = (m_dTrailingStop_ld + dSL) / 2;
            }

        }
        private void calcSHTrailingStop()
        {
            //m_dTrailingStop_sh
            if (m_products[ID_SHANGHAI].getPosCount_vt() == 0)
                return;
            ETRADER_OP nCmd = m_products[ID_SHANGHAI].getPosCmd_vt(0);
            if (m_products[ID_SHANGHAI].getPosProfit_vt(0) < 500)
                return;

            double dSL = 0;
            if ( nCmd == ETRADER_OP.BUY)
            {
                dSL = m_dBid_Shanghai - 0.5;
                if (m_dTrailingStop_sh < dSL)
                    m_dTrailingStop_sh = dSL;
            }
            if (nCmd == ETRADER_OP.SELL)
            {
                dSL = m_dAsk_Shanghai + 0.5;
                if (m_dTrailingStop_sh > dSL)
                    m_dTrailingStop_sh = dSL;
            }

        }
        private void calcSARVal()
        {
            if (m_products[ID_SHANGHAI].getPosCount_vt() == 0)
                return;
            ETRADER_OP nCmd = m_products[ID_SHANGHAI].getPosCmd_vt(0);

            if ( nCmd == ETRADER_OP.BUY)
            {
                if (m_dMaxVal + ex_dLDStepForSAR < m_dMid_London)
                {
                    m_dMaxVal = m_dMid_London;
                    m_dAlpha += ex_dSARStep;
                    if (m_dAlpha > ex_dSARMax)
                        m_dAlpha = ex_dSARMax;
                    m_dTrailingStop_ld = m_dTrailingStop_ld + m_dAlpha * (m_dMaxVal - m_dTrailingStop_ld);
                }
            }

            if (nCmd == ETRADER_OP.SELL)
            {
                if (m_dMaxVal - ex_dLDStepForSAR > m_dMid_London)
                {
                    m_dMaxVal = m_dMid_London;
                    m_dAlpha += ex_dSARStep;
                    if (m_dAlpha > ex_dSARMax)
                        m_dAlpha = ex_dSARMax;
                    m_dTrailingStop_ld = m_dTrailingStop_ld + m_dAlpha * (m_dMaxVal - m_dTrailingStop_ld);
                }
            }
            m_dTrailingStop_ld = m_dTrailingStop_ld + (ex_dAlphaSAR ) * (m_dMaxVal - m_dTrailingStop_ld);
        }

        private int getSignal()
        {
            int nRetSignal = (int)ETRADER_OP.NONE;
            int difftime = 0;
            double dSumLD = 0;
            double dExpectDiff = 0;
            double dDiffPrice_ld = 0;
            double dDiffPrice_sh = 0;
            double dExpectDiff_close = 0;

            if (m_products[ID_SHANGHAI].getPosCount_vt() == 0)
            {
                for (int i = 0; i < m_products[ID_LONDON].getTick_count(); i++)
                {
                    difftime = (int)(m_products[ID_LONDON].getTickTime() - m_products[ID_LONDON].getTick(i).m_dtTime).TotalSeconds;
                    if (difftime > ex_nOpenCalcSeconds)
                        break;

                    dSumLD += m_products[ID_LONDON].getTick(i).dAsk;
                    //dExpectDiff = ex_dOpenLevelStart + (ex_dOpenLevelEnd - ex_dOpenLevelStart) * difftime / ex_nOpenCalcSeconds;
                    dExpectDiff = ex_dOpenLevelStart + (ex_dOpenLevelEnd - ex_dOpenLevelStart) * i /(5);
                    dExpectDiff_close = ex_dOpenLevelStart_close + (ex_dOpenLevelEnd_close - ex_dOpenLevelStart_close) * i / (5);
                    dDiffPrice_ld = m_products[ID_LONDON].getTick(0).dBid - m_products[ID_LONDON].getTick(i).dBid;
                    dDiffPrice_sh = m_products[ID_SHANGHAI].getTick(0).dBid - m_products[ID_SHANGHAI].getTick(i).dBid;

                    if (dDiffPrice_ld > dExpectDiff)// && dDiffPrice_sh < dDiffPrice_ld * 6.88 / 31.103477)
                    {//Buy Signal
                        nRetSignal |= (int)ETRADER_OP.BUY;
                        nRetSignal |= (int)ETRADER_OP.SELL_CLOSE;
                        m_dOpenDiff_sh = dDiffPrice_sh;
                        m_dOpenDiff_ld = dDiffPrice_ld;

                        m_dAlpha = ex_dSARStep;
                        m_dMaxVal = m_products[ID_LONDON].getTick(0).dBid;
                        m_dTrailingStop_ld = m_products[ID_LONDON].getTick(i).dBid;
                        m_dTrailingStop_sh = m_dMid_Shanghai - ex_dSlippage - 0.3;
                        break;
                    }
                    if (dDiffPrice_ld > dExpectDiff_close)
                    {//Sell Close
                        nRetSignal |= (int)ETRADER_OP.SELL_CLOSE;
                    }

                    if (dDiffPrice_ld * (-1) > dExpectDiff)// && dDiffPrice_sh > dDiffPrice_ld * 6.88 / 31.103477)
                    {//Sell Signal
                        nRetSignal |= (int)ETRADER_OP.SELL;
                        nRetSignal |= (int)ETRADER_OP.BUY_CLOSE;
                        m_dOpenDiff_sh = dDiffPrice_sh * (-1);
                        m_dOpenDiff_ld = dDiffPrice_ld * (-1);

                        m_dAlpha = ex_dSARStep;
                        m_dMaxVal = m_products[ID_LONDON].getTick(0).dBid;
                        m_dTrailingStop_ld = m_products[ID_LONDON].getTick(i).dBid;
                        m_dTrailingStop_sh = m_dMid_Shanghai + ex_dSlippage + 0.3;
                        break;
                    }

                    if (dDiffPrice_ld * (-1) > dExpectDiff_close)
                    {//Buy close
                        nRetSignal |= (int)ETRADER_OP.BUY_CLOSE;
                    }
                }
            }

            //return nRetSignal;
            //Close signal
            if (m_products[ID_SHANGHAI].getPosCount_vt() == 0) return nRetSignal;

            ETRADER_OP nCmd = m_products[ID_SHANGHAI].getPosCmd_vt(0);

            //Check TrailingStop
            if ( nCmd == ETRADER_OP.BUY )
            {
                if ( m_dMid_London < m_dTrailingStop_ld)
                {
                    nRetSignal |= (int)ETRADER_OP.BUY_CLOSE;
                    return nRetSignal;
                }

                if ( m_dBid_Shanghai < m_dTrailingStop_sh)
                {
                    nRetSignal |= (int)ETRADER_OP.BUY_CLOSE;
                    return nRetSignal;
                }
            }

            if (nCmd == ETRADER_OP.SELL)
            {
                if (m_dMid_London > m_dTrailingStop_ld)
                {
                    nRetSignal |= (int)ETRADER_OP.SELL_CLOSE;
                    return nRetSignal;
                }
                if ( m_dAsk_Shanghai > m_dTrailingStop_sh)
                {
                    nRetSignal |= (int)ETRADER_OP.SELL_CLOSE;
                    return nRetSignal;
                }
            }

            //
            return nRetSignal;
        }
    }
}
