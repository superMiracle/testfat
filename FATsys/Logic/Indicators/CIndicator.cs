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
    class CIndicator
    {
        public CCacheData m_cacheData_main = new CCacheData();

        public CCacheData m_cacheData_A = null;
        public CCacheData m_cacheData_B = null;

//         public string m_sName = "NONE";
//         public double m_dIndVal = 0;

        public Dictionary<string, double> m_indVals = new Dictionary<string, double>();
        public List<string> m_indDisablePublish = new List<string>();

        public DateTime m_dtTime_published_min;

        public void OnInit()
        {
            m_cacheData_main.OnInit();
        }

        public void OnDeInit()
        {
            m_cacheData_main.OnDeInit();
        }

        public void addIndVal(string sName)
        {
            m_indVals.Add(sName, 0);
        }
        public CCacheData getCacheData()
        {
            return m_cacheData_main;
        }

        public double getPrice(CCacheData cacheData, int nPos, ETIME_FRAME nTimeFrame = ETIME_FRAME.MIN1, EPRICE_MODE nPriceMode = EPRICE_MODE.BID, EPRICE_VAL nPriceVal = EPRICE_VAL.CLOSE)
        {
            double dRet = 0;
            if ( nTimeFrame == ETIME_FRAME.MIN1)
            {
                if (nPriceMode == EPRICE_MODE.ASK)
                {
                    if (nPriceVal == EPRICE_VAL.OPEN ) return cacheData.getMin(nPos).dAsk_open;
                    if (nPriceVal == EPRICE_VAL.HIGH) return cacheData.getMin(nPos).dAsk_high;
                    if (nPriceVal == EPRICE_VAL.LOW) return cacheData.getMin(nPos).dAsk_low;
                    if (nPriceVal == EPRICE_VAL.CLOSE) return cacheData.getMin(nPos).dAsk_close;
                }

                if ( nPriceMode == EPRICE_MODE.BID)
                {
                    if (nPriceVal == EPRICE_VAL.OPEN) return cacheData.getMin(nPos).dBid_open;
                    if (nPriceVal == EPRICE_VAL.HIGH) return cacheData.getMin(nPos).dBid_high;
                    if (nPriceVal == EPRICE_VAL.LOW) return cacheData.getMin(nPos).dBid_low;
                    if (nPriceVal == EPRICE_VAL.CLOSE) return cacheData.getMin(nPos).dBid_close;
                }
            }

            if ( nTimeFrame == ETIME_FRAME.TICK)
            {
                if (nPriceMode == EPRICE_MODE.ASK ) return cacheData.getTick(nPos).dAsk;
                if (nPriceMode == EPRICE_MODE.BID) return cacheData.getTick(nPos).dBid;
            }

            return dRet;
        }

        public virtual void publish_min()
        {
            //Publish self data
            string sTxt = "";

            DateTime dtTime_cur = CFATCommon.m_dtCurTime;

            if (dtTime_cur != m_dtTime_published_min)
            {
                foreach (KeyValuePair<string, double> indItem in m_indVals)
                {
                    if (m_indDisablePublish.Contains(indItem.Key)) continue;
                    sTxt = string.Format("{0},{1},{2},{3},{4},{5},{6}", "Indicators", indItem.Key, dtTime_cur,
                        indItem.Value, indItem.Value, indItem.Value, indItem.Value);
                    CMQClient.publish_msg(sTxt, CFATCommon.MQ_TOPIC_PRICE_MIN);
                }
                m_dtTime_published_min = dtTime_cur;
            }
            //-----------------------
        }

        public void publish_tick()
        {
//             string sTxt = "";
//             DateTime dtTime_cur = m_cacheData_A.getTick(0).m_dtTime;
//             if (m_dIndVal != m_dBid_published_tick || m_dIndVal != m_dAsk_published_tick)
//             {
//                 sTxt = string.Format("{0},{1},{2},{3},{4}", "Indicators", m_sName, 
//                     dtTime_cur, m_dIndVal, m_dIndVal);
//                 CMQClient.publish_msg(sTxt, CFATCommon.MQ_TOPIC_PRICE_TICK);
//                 m_dBid_published_tick = m_dIndVal;
//                 m_dAsk_published_tick = m_dIndVal;
//             }
        }
    }
}
