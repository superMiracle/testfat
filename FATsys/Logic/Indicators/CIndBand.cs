using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FATsys.Product;
using FATsys.Utils;
using FATsys.TraderType;

namespace FATsys.Logic.Indicators
{
    class CIndBand : CIndicator
    {
        private string IND_BAND_MID = "IndBand_mid";
        private string IND_BAND_UP = "IndBand_up";
        private string IND_BAND_DOWN = "IndBand_down";

        private CIndMA m_indMA = new CIndMA();
        private CIndStd m_indSTD = new CIndStd();

        public CIndBand()
        {
            m_indVals.Add(IND_BAND_MID, 0);
            m_indVals.Add(IND_BAND_UP, 0);
            m_indVals.Add(IND_BAND_DOWN, 0);
        }

        public void setCacheData(CCacheData cacheData)
        {
            m_cacheData_A = cacheData;

            m_indMA.setCacheData(cacheData);
            m_indSTD.setCacheData(cacheData);
        }

        public void calc(int nPeriod, ETIME_FRAME nTimeFrame = ETIME_FRAME.MIN1, EPRICE_MODE nPriceMode = EPRICE_MODE.BID, EPRICE_VAL nPriceVal = EPRICE_VAL.CLOSE)
        {
            m_indSTD.calc(nPeriod, nTimeFrame, nPriceMode, nPriceVal);
            m_indMA.calc(nPeriod, nTimeFrame, nPriceMode, nPriceVal);

            double dStd = m_indSTD.getVal();

            m_indVals[IND_BAND_MID] = m_indMA.getVal();
            m_indVals[IND_BAND_UP] = m_indVals[IND_BAND_MID] + 2 * dStd;
            m_indVals[IND_BAND_DOWN] = m_indVals[IND_BAND_MID] - 2 * dStd;

//             string sRates = string.Format("{0},{1},{2},{3},{4}", CFATCommon.m_dtCurTime, 
//                 m_cacheData_A.getTick(0).getMid(), 
//                 m_indVals[IND_BAND_UP], m_indVals[IND_BAND_MID], m_indVals[IND_BAND_DOWN]);
//             CFATLogger.record_rates("Test\\Band", sRates);
        }

        public void getVal(ref double dBand_up, ref double dBand_mid, ref double dBand_down)
        {
            dBand_up = m_indVals[IND_BAND_UP];
            dBand_mid = m_indVals[IND_BAND_MID];
            dBand_down = m_indVals[IND_BAND_DOWN];
        }

        public int getSignal(double dOpenLevel = 0, double dCloseLevel = 0)
        {
            int nRetSignal = (int)ETRADER_OP.NONE;

            if (m_cacheData_A.getTick(0).dAsk < m_indVals[IND_BAND_DOWN] && 
                m_cacheData_A.getTick(0).dAsk < dOpenLevel * (-1))
                nRetSignal |= (int)ETRADER_OP.BUY;

            if (m_cacheData_A.getTick(0).dBid > m_indVals[IND_BAND_UP] && 
                m_cacheData_A.getTick(0).dBid > dOpenLevel)
                nRetSignal |= (int)ETRADER_OP.SELL;

            if (m_cacheData_A.getTick(0).dBid > m_indVals[IND_BAND_UP] && 
                m_cacheData_A.getTick(0).dBid > dCloseLevel)
                nRetSignal |= (int)ETRADER_OP.BUY_CLOSE;

            if (m_cacheData_A.getTick(0).dAsk < m_indVals[IND_BAND_DOWN] && 
                m_cacheData_A.getTick(0).dAsk < dCloseLevel * (-1))
                nRetSignal |= (int)ETRADER_OP.SELL_CLOSE;

            return nRetSignal;
        }


    }
}
