using System;
using System.IO;
using System.Windows.Forms;
using System.Xml.Serialization;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FATsys.Utils;

namespace FATsys.TraderType
{
    [Serializable]
    public class CCacheData
    {
        public List<TRatesTick> m_tickData = new List<TRatesTick>();
        public List<TRatesMin> m_minData = new List<TRatesMin>();
        public List<TRatesTick> m_renkoData = new List<TRatesTick>();

        public int m_nCurPos_tick = -1;
        public int m_nCurPos_min = -1;
        public int m_nCurPos_renko = -1;

        private double m_dRenkoStep = -1;
        private double m_dLastRenkoVal = 0;
        private DateTime m_dtLastSavedTime = default(DateTime);

        private string m_sName = "NO_NAME";

        public CCacheData() { }
        public void OnInit()
        {
            clearData();
            loadData();
        }

        public void OnDeInit()
        {
            saveData();
        }
        public void setName(string sName)
        {
            m_sName = sName;
        }
        private void clearData()
        {
            m_nCurPos_tick = -1;
            m_nCurPos_min = -1;

            m_tickData.Clear();
            m_minData.Clear();
        }

        public void setRenkoStep(double dStep)
        {
            m_dRenkoStep = dStep;
        }

        public void pushTick(double dAsk, double dBid, DateTime dtTime)
        {
            m_nCurPos_tick++;
            if (m_tickData.Count < CFATCommon.CACHE_SIZE)
            {
                TRatesTick tick = new TRatesTick();
                tick.dAsk = dAsk;
                tick.dBid = dBid;
                tick.m_dtTime = dtTime;
                m_tickData.Add(tick);
            }
            else
            {
                if (m_nCurPos_tick == CFATCommon.CACHE_SIZE)
                    m_nCurPos_tick = 0;
                m_tickData[m_nCurPos_tick].dAsk = dAsk;
                m_tickData[m_nCurPos_tick].dBid = dBid;
                m_tickData[m_nCurPos_tick].m_dtTime = dtTime;
            }
            pushMin(dAsk, dBid, dtTime);
            pushRenko(dAsk, dBid, dtTime);

            autoSave();
        }

        private void pushRenko(double dAsk, double dBid, DateTime dtTime)
        {
            if (m_dRenkoStep < 0)
                return;
            double dCurVal = (dAsk + dBid) / 2;
            if (Math.Abs(m_dLastRenkoVal - dCurVal) < m_dRenkoStep)
                return;

            m_dLastRenkoVal = dCurVal;
            m_nCurPos_renko++;
            if (m_renkoData.Count < CFATCommon.CACHE_SIZE)
            {
                TRatesTick renkoRates = new TRatesTick();
                renkoRates.dAsk = dAsk;
                renkoRates.dBid = dBid;
                renkoRates.m_dtTime = dtTime;
                m_renkoData.Add(renkoRates);
            }
            else
            {
                if (m_nCurPos_renko == CFATCommon.CACHE_SIZE)
                    m_nCurPos_renko = 0;
                m_renkoData[m_nCurPos_renko].dAsk = dAsk;
                m_renkoData[m_nCurPos_renko].dBid = dBid;
                m_renkoData[m_nCurPos_renko].m_dtTime = dtTime;
            }
        }
        private void pushMin(double dAsk, double dBid, DateTime dtTime)
        {
            bool bIsNewBar = false;
            if (m_minData.Count == 0)
                bIsNewBar = true;
            else
            {
                if ((dtTime - m_minData[m_nCurPos_min].m_dtTime).TotalSeconds >= 60) //60 s after new 1 min data
                    bIsNewBar = true;
            }

            if (bIsNewBar)
                m_nCurPos_min++;

            if (bIsNewBar && m_minData.Count < CFATCommon.CACHE_SIZE)
            {
                TRatesMin minData = new TRatesMin();
                minData.setVal(dAsk, dAsk, dAsk, dAsk, dBid, dBid, dBid, dBid);
                minData.m_dtTime = dtTime; //Set open time of 1 min
                m_minData.Add(minData);
                return;
            }

            if (m_nCurPos_min == CFATCommon.CACHE_SIZE)
                m_nCurPos_min = 0;

            if (bIsNewBar)
            {
                m_minData[m_nCurPos_min].setVal(dAsk, dAsk, dAsk, dAsk, dBid, dBid, dBid, dBid);
                m_minData[m_nCurPos_min].m_dtTime = dtTime; //Set open time of 1 min
            }
            else
                m_minData[m_nCurPos_min].setTickVal(dAsk, dBid);
        }

        public int getTickCount()
        {
            return m_tickData.Count;
        }
        public int getRenkoCount()
        {
            return m_renkoData.Count;
        }
        public int getMinCount()
        {
            return m_minData.Count;
        }

        public TRatesTick getTick(int nPos)
        {
            if (m_tickData.Count == 0)
                return new TRatesTick();
            if (m_nCurPos_tick >= nPos)
                return m_tickData[m_nCurPos_tick - nPos];
            
            if ( m_tickData.Count < CFATCommon.CACHE_SIZE)
                return m_tickData[0];

            int nRetPos = m_tickData.Count - (nPos - m_nCurPos_tick);
            if (nRetPos < 0) nRetPos = 0;
            if (nRetPos > m_tickData.Count - 1) nRetPos = m_tickData.Count - 1;
            return m_tickData[nRetPos];
        }

        public TRatesTick getRenko(int nPos)
        {
            if (m_renkoData.Count == 0)
                return getTick(nPos);

            if (m_nCurPos_renko >= nPos)
                return m_renkoData[m_nCurPos_renko - nPos];

            if (m_renkoData.Count < CFATCommon.CACHE_SIZE)
                return m_renkoData[0];

            int nRetPos = m_renkoData.Count - (nPos - m_nCurPos_renko);
            if (nRetPos < 0) nRetPos = 0;
            if (nRetPos > m_renkoData.Count - 1) nRetPos = m_renkoData.Count - 1;
            return m_renkoData[nRetPos];
        }

        public TRatesMin getMin(int nPos)
        {
            if (m_minData.Count == 0)
                return new TRatesMin();

            if (m_nCurPos_min >= nPos)
                return m_minData[m_nCurPos_min - nPos];

            if (m_minData.Count < CFATCommon.CACHE_SIZE)
                return m_minData[0];

            int nRetPos = m_minData.Count - (nPos - m_nCurPos_min);
            if (nRetPos < 0) nRetPos = 0;
            if (nRetPos > m_minData.Count - 1) nRetPos = m_minData.Count - 1;

            return m_minData[nRetPos];
        }


        #region  ############### Save & Load ###################

        //Aut save every 1 mins : only works in Online MODE 
        private void autoSave()
        {
            if ((DateTime.Now - m_dtLastSavedTime).TotalSeconds < 60)
                return;

            saveData();
        }

        private string getStoreFileName()
        {
            string sRet = Path.Combine(Application.StartupPath, "_log\\store_var\\" + m_sName + ".xml");

            return sRet;
        }
        private void saveData()
        {
            if (m_sName == "NO_NAME")
                return;
            if (!CFATManager.isOnlineMode())
                return;

            string sFile = getStoreFileName();
            if (File.Exists(sFile))
                File.Delete(sFile);

            var xs = new XmlSerializer(typeof(CCacheData));
            using (TextWriter sw = new StreamWriter(sFile))
            {
                xs.Serialize(sw, this);
            }

            CFATLogger.output_proc("File saved : " + sFile);
            m_dtLastSavedTime = DateTime.Now;
        }

        private void loadData()
        {
            if (m_sName == "NO_NAME")
                return;
            if (!CFATManager.isOnlineMode())
                return;

            string sFile = getStoreFileName();

            if ( !File.Exists(sFile))
            {
                CFATLogger.output_proc("File don't exist: " + sFile);
                return;
            }

            var xs = new XmlSerializer(typeof(CCacheData));

            using (var sr = new StreamReader(sFile))
            {
                var tempObject = (CCacheData)xs.Deserialize(sr);
                m_tickData = tempObject.m_tickData;
                m_minData = tempObject.m_minData;
                m_renkoData = tempObject.m_renkoData;

                m_nCurPos_tick = tempObject.m_nCurPos_tick;
                m_nCurPos_min = tempObject.m_nCurPos_min;
                m_nCurPos_renko = tempObject.m_nCurPos_renko;

            }

            CFATLogger.output_proc("File loaded : " + sFile);
        }

        #endregion
    }
}
