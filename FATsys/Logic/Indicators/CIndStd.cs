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
    class CIndStd : CIndicator
    {

        private CIndMA m_indMA = new CIndMA();
        private string IND_MAIN = "STD";
        public CIndStd()
        {
            addIndVal(IND_MAIN);
        }

        public void setCacheData(CCacheData cacheData)
        {
            m_cacheData_A = cacheData;
            m_indMA.setCacheData(cacheData);
        }

        public void calc(int nPeriod, ETIME_FRAME nTimeFrame = ETIME_FRAME.MIN1, EPRICE_MODE nPriceMode = EPRICE_MODE.BID, EPRICE_VAL nPriceVal = EPRICE_VAL.CLOSE)
        {
            m_indMA.calc(nPeriod, nTimeFrame, nPriceMode, nPriceVal);

            double dSum = 0;
            double dVal = 0;
            if (nPeriod == 0)
            {
                m_indVals[IND_MAIN] = 0;
                return;
            }

            double dMA = m_indMA.getVal();

            for (int i = 0; i < nPeriod; i++)
            {
                dVal = getPrice(m_cacheData_A, i, nTimeFrame, nPriceMode, nPriceVal);
                dSum += (dVal - dMA) * (dVal - dMA);
            }

            m_indVals[IND_MAIN] = Math.Sqrt(dSum / nPeriod);
        }
        public double getVal()
        {
            return m_indVals[IND_MAIN];
        }
    }
}
