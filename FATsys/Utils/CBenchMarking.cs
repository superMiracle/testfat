using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FATsys.Utils
{
    public class TBenchMarking
    {
        private int BUFFER_SIZE = 1000;

        private List<double> m_lstOnTick_start_start = new List<double>();
        private List<double> m_lstOnTick_start_end = new List<double>();

        private int m_nPos_start_start = -1;
        private int m_nPos_start_end = -1;

        private DateTime m_dtOnTick_start_prev = DateTime.Now;

        public void push_Ontick_start(DateTime dtTime)
        {
            DateTime dtStart = DateTime.Now;
            double dStart_start = (dtStart - m_dtOnTick_start_prev).TotalMilliseconds;

            m_nPos_start_start++;
            if (m_lstOnTick_start_start.Count < BUFFER_SIZE)
                m_lstOnTick_start_start.Add(dStart_start);
            else
            {
                if (m_nPos_start_start == m_lstOnTick_start_start.Count)
                    m_nPos_start_start = 0;
                m_lstOnTick_start_start[m_nPos_start_start] = dStart_start;
            }
            m_dtOnTick_start_prev = dtStart;
        }

        public void push_Ontick_end(DateTime dtTime)
        {
            DateTime dtEnd = DateTime.Now;
            double dStart_end = (dtEnd - m_dtOnTick_start_prev).TotalMilliseconds;

            m_nPos_start_end++;
            if (m_lstOnTick_start_end.Count < BUFFER_SIZE)
                m_lstOnTick_start_end.Add(dStart_end);
            else
            {
                if (m_nPos_start_end == m_lstOnTick_start_end.Count)
                    m_nPos_start_end = 0;
                m_lstOnTick_start_end[m_nPos_start_end] = dStart_end;
            }
        }

        public double getAverageMilliSecs_start_start(int nPeriod)
        {
            if (m_lstOnTick_start_start.Count == 0)
                return 0;

            double dRet = 0;
            int nPos = m_nPos_start_start;
            int nCount = 0;
            for (int i = 0; i < nPeriod; i++)
            {
                dRet += m_lstOnTick_start_start[nPos];
                nCount++;
                nPos--;
                if (nPos < 0)
                {
                    if (m_lstOnTick_start_start.Count < BUFFER_SIZE)
                        break;
                    nPos = m_lstOnTick_start_start.Count - 1;
                }
            }
            return dRet / nCount;
        }

        public double getAverageMilliSecs_start_end(int nPeriod)
        {
            if (m_lstOnTick_start_end.Count == 0)
                return 0;

            double dRet = 0;
            int nPos = m_nPos_start_end;
            int nCount = 0;
            for (int i = 0; i < nPeriod; i++)
            {
                dRet += m_lstOnTick_start_end[nPos];
                nCount++;
                nPos--;
                if (nPos < 0)
                {
                    if (m_lstOnTick_start_end.Count < BUFFER_SIZE)
                        break;
                    nPos = m_lstOnTick_start_end.Count - 1;
                }
            }
            return dRet / nCount;
        }
    }
}
