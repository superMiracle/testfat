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
    class CIndCC : CIndicator
    {
        private string IND_MAIN = "DiffWithCC";
        private string IND_CC = "CC";
        public CIndCC()
        {
            m_indVals.Add(IND_MAIN, 0);
            m_indVals.Add(IND_CC, 1);
            m_indDisablePublish.Add(IND_CC);
        }

        public void setCacheData(CCacheData cacheData_A, CCacheData cacheData_B)
        {
            m_cacheData_A = cacheData_A;
            m_cacheData_B = cacheData_B;
        }

        public double getVal(string sIndName)
        {
            return m_indVals[sIndName];
        }

        public void clac2(double dPrice_A, double dPrice_B, int nPeriod,
            ETIME_FRAME nTimeFrame = ETIME_FRAME.MIN1, EPRICE_MODE nPriceMode = EPRICE_MODE.BID,
            EPRICE_VAL nPriceVal = EPRICE_VAL.CLOSE)
        {
            if (m_cacheData_A.getTickCount() < nPeriod)
            {
                m_indVals[IND_MAIN] = 0;
                m_indVals[IND_CC] = 1;
                return;
            }

            double sumyy = 0, sumxyxy = 0;
            double x, y, xx, yy;

            for (int i = 0; i < nPeriod; i++)
            {
                x = getPrice(m_cacheData_A, i, nTimeFrame, nPriceMode, nPriceVal);
                y = getPrice(m_cacheData_B, i, nTimeFrame, nPriceMode, nPriceVal);
                xx = getPrice(m_cacheData_A, i + 1, nTimeFrame, nPriceMode, nPriceVal);
                yy = getPrice(m_cacheData_B, i + 1, nTimeFrame, nPriceMode, nPriceVal);

                sumyy += y * yy;
                sumxyxy += x * yy + xx * y;
            }

            m_indVals[IND_CC] = sumxyxy / sumyy / 2;
            m_indVals[IND_MAIN] = dPrice_A - dPrice_B * m_indVals[IND_CC];
            m_cacheData_main.pushTick(m_indVals[IND_MAIN], m_indVals[IND_MAIN], CFATCommon.m_dtCurTime);
        }

        public void calc(double dPrice_A, double dPrice_B, int nPeriod, 
            ETIME_FRAME nTimeFrame = ETIME_FRAME.MIN1, EPRICE_MODE nPriceMode = EPRICE_MODE.BID, 
            EPRICE_VAL nPriceVal = EPRICE_VAL.CLOSE)
        {
            if (m_cacheData_A.getTickCount() < nPeriod)
            {
                m_indVals[IND_MAIN] = 0;
                m_indVals[IND_CC] = 1;
                return;
            }

            double sumx = 0, sumy = 0, sumxx = 0, sumyy = 0, sumxyxy = 0, sumxy = 0;
            double dRet = 0;
            double x, y, xx, yy;


            for (int i = 0; i < nPeriod; i++)
            {
                x = getPrice(m_cacheData_A, i, nTimeFrame, nPriceMode, nPriceVal);
                y = getPrice(m_cacheData_B, i, nTimeFrame, nPriceMode, nPriceVal);
                xx = getPrice(m_cacheData_A, i + 1, nTimeFrame, nPriceMode, nPriceVal);
                yy = getPrice(m_cacheData_B, i + 1, nTimeFrame, nPriceMode, nPriceVal);

                sumx += x * x;
                sumxx += x * xx;
                sumy += y * y;
                sumyy += y * yy;
                sumxy += x * y;
                sumxyxy += x * yy + xx * y;
            }

            double a, b, c;


            a = sumy * sumxyxy - 2 * sumxy * sumyy;
            b = sumyy * sumx - sumy * sumxx;
            c = 2 * sumxy * sumxx - sumxyxy * sumx;
            dRet = (Math.Sqrt(b * b - a * c) - b) / a;
            m_indVals[IND_CC] = dRet;

            //m_indVals[IND_MAIN] = dPrice_A - m_indVals[IND_CC] * 31.103477; //For Logic_Pair_V3
            m_indVals[IND_MAIN] = dPrice_A - dPrice_B * dRet;

            m_cacheData_main.pushTick(m_indVals[IND_MAIN], m_indVals[IND_MAIN], CFATCommon.m_dtCurTime);

//             string sRates = string.Format("{0},{1}", CFATCommon.m_dtCurTime, m_dIndVal);
//             CFATLogger.record_rates("Test\\CC", sRates);
        }

        public int getSignal(double dOpenLevel, double dCloseLevel)
        {
            int nRetSignal = (int)ETRADER_OP.NONE;

            if (m_indVals[IND_MAIN] < dOpenLevel * (-1))
                nRetSignal |= (int)ETRADER_OP.BUY;

            if (m_indVals[IND_MAIN] > dOpenLevel)
                nRetSignal |= (int)ETRADER_OP.SELL;

            if (m_indVals[IND_MAIN] > dCloseLevel)
                nRetSignal |= (int)ETRADER_OP.BUY_CLOSE;

            if (m_indVals[IND_MAIN] < dCloseLevel * (-1))
                nRetSignal |= (int)ETRADER_OP.SELL_CLOSE;

            return nRetSignal;
        }
    }
}
