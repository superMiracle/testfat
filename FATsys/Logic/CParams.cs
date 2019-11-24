using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using FATsys.TraderType;

namespace FATsys.Logic
{
    class CParams
    {
        private Dictionary<string, TLogicParamItem> m_params = new Dictionary<string, TLogicParamItem>();

        public string getVal_string(string sName)
        {
            return m_params[sName].m_sVal;
        }
        public double getVal_double(string sName)
        {
            return Convert.ToDouble(m_params[sName].m_sVal);
        }
        public void addParam(string sKey, string sVal,string sStart, string sStep, string sEnd)
        {
            TLogicParamItem param = new TLogicParamItem();
            param.m_sVal = sVal;
            param.m_sStart = sStart;
            param.m_sStep = sStep;
            param.m_sEnd = sEnd;

            m_params.Add(sKey, param);
        }
        public int getCount()
        {
            return m_params.Count();
        }
        public TLogicParamItem getParamItem(int nParamIndex)
        {
            return m_params.ElementAt(nParamIndex).Value;
        }
        public string getName(int nParamIndex)
        {
            return m_params.ElementAt(nParamIndex).Key;
        }

        public void setVal(string sName, string sVal)
        {
            if ( m_params.ContainsKey(sName) )
                m_params[sName].m_sVal = sVal;
        }

        public string getString()
        {
            string sRet = "";
            for (int i = 0; i < m_params.Count; i++)
                sRet += string.Format("{0}={1},", m_params.ElementAt(i).Key, m_params.ElementAt(i).Value.m_sVal);
            return sRet;
        }

    }
}
