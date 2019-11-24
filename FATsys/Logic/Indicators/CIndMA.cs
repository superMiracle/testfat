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
    class CIndMA : CIndicator
    {
        private string IND_MAIN = "MA";
        public CIndMA()
        {
            m_indVals.Add(IND_MAIN, 0);
        }

        public void setCacheData(CCacheData cacheData)
        {
            m_cacheData_A = cacheData;
        }

        public void calc(int nPeriod, ETIME_FRAME nTimeFrame = ETIME_FRAME.MIN1, EPRICE_MODE nPriceMode = EPRICE_MODE.BID, EPRICE_VAL nPriceVal = EPRICE_VAL.CLOSE)
        {
            if (nPeriod == 0)
            {
                m_indVals[IND_MAIN] = 0;
                return;
            }

            double dRet = 0;
            for (int i = 0; i < nPeriod; i++)
            {
                dRet += getPrice(m_cacheData_A, i, nTimeFrame, nPriceMode, nPriceVal);
            }

            m_indVals[IND_MAIN] = dRet / nPeriod;

        }
        public double getVal()
        {
            return m_indVals[IND_MAIN];
        }

        public int getSignal(double dOpenLevel, double dCloseLevel)
        {
            int nRetSignal = (int)ETRADER_OP.NONE;

            if (m_cacheData_A.getTick(0).dAsk < m_indVals[IND_MAIN] - dOpenLevel)
                nRetSignal |= (int)ETRADER_OP.BUY;

            if (m_cacheData_A.getTick(0).dBid > m_indVals[IND_MAIN] + dOpenLevel)
                nRetSignal |= (int)ETRADER_OP.SELL;

            if (m_cacheData_A.getTick(0).dBid > m_indVals[IND_MAIN] + dCloseLevel)
                nRetSignal |= (int)ETRADER_OP.BUY_CLOSE;

            if (m_cacheData_A.getTick(0).dAsk < m_indVals[IND_MAIN] - dCloseLevel)
                nRetSignal |= (int)ETRADER_OP.SELL_CLOSE;

            return nRetSignal;
        }
    }
}
