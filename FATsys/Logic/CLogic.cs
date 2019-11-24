using System;
using System.IO;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using FATsys.Utils;
using FATsys.TraderType;
using FATsys.Site;
using FATsys.Product;


namespace FATsys.Logic
{
    enum ELOGIC_STATE
    {
        NORMAL = 0,
        NEED_CHECK_POSITION_MATCH = 1,
        WAITING_POSITION_MATCH = 2,
        NEED_FORCE_CLOSE = 3,
        WAITING_FORCE_CLOSE = 4,
        LOGIC_STOP_BY_ERROR = 5,
        WAITING_ORDER_RESPONSE = 6,
        ORDER_FILLED = 7,
    }

    struct TUNIT_TEST
    {
        public DateTime m_dtTime;
        public string m_sCmd;
    }
    
    struct TLOGIC_STATE
    {
        public ELOGIC_STATE m_nState;
        public DateTime m_dtUpdatedTime;
        public double getSeconds_Now2UpdateTime()
        {
            return (DateTime.Now - m_dtUpdatedTime).TotalSeconds;
        }
        public string getString()
        {
            if (m_nState == ELOGIC_STATE.NORMAL)
                return "NORMAL";
            if (m_nState == ELOGIC_STATE.NEED_CHECK_POSITION_MATCH)
                return "NeedCheckPosMatch";
            if (m_nState == ELOGIC_STATE.WAITING_FORCE_CLOSE)
                return "WaitingForceClose";
            if (m_nState == ELOGIC_STATE.LOGIC_STOP_BY_ERROR)
                return "StopByError";
            return "NONE";
        }
    }

    class CLogic
    {

        public List<CProduct> m_products = new List<CProduct>();
        public Dictionary<string, string> m_vars_publish = new Dictionary<string, string>();

        public List<TUNIT_TEST> m_lstUnitTest_signal = new List<TUNIT_TEST>();
        public CParams m_params;
        public string m_sLogicName;
        public string m_sMode;

        public TLOGIC_STATE m_stState = new TLOGIC_STATE();
        private int m_nOptimizeID;
        public List<Dictionary<string, string>> m_optimize_params = new List<Dictionary<string, string>>();


        public double ex_dLots;
        public int ex_nIsNewOrder;
        public bool ex_bIsNewSession;
        public string m_sLogicID;
        public DateTime m_dtLastPublish;

         
        public void setState(ELOGIC_STATE nState)
        {
            m_stState.m_nState = nState;
            m_stState.m_dtUpdatedTime = DateTime.Now;
            //HSM???
            if ( CFATManager.isOnlineMode())
                CFATLogger.output_proc(string.Format("{0},{1} : Change State -> {2}",
                    CFATCommon.m_dtCurTime, m_sLogicID, m_stState.m_nState));
        }

        public virtual bool OnInit()
        {
            CFATLogger.output_proc(string.Format("Logic Init : {0} ------>", m_sLogicName));
            string sParams = m_params.getString();
            CFATCommon.m_sOpt_param = sParams;
            setState(ELOGIC_STATE.NORMAL);
            CFATLogger.output_proc("Params : " + sParams);

            if ( m_sMode == "unit_test")
                load_unitTest();

            return true;
        }
        public virtual void OnDeInit()
        {
            CFATLogger.output_proc(string.Format("Logic DeInit : {0} <------", m_sLogicName));
        }

        public virtual int OnTick()
        {
            return (int)ETRADE_RET.NONE;
        }

        public void setParam_newOrder(int nVal)
        {
            ex_nIsNewOrder = nVal;
            setParams("ex_nIsNewOrder", ex_nIsNewOrder.ToString());
        }


        public void changeParams()
        {
            try
            {
                foreach (KeyValuePair<string, string> entry in CEvent_ParamChanged.g_params)
                {
                    m_params.setVal(entry.Key, entry.Value);
                }
            }
            catch
            {
                CFATLogger.output_proc("Error : CLogic::changeParams ");
            }
            CFATLogger.output_proc(string.Format("Logic : {0}, parameter changed!", m_sLogicID));
        }

        public virtual void matchPosVirtual2Real()
        {

        }
        public virtual void loadParams()
        {
            ex_dLots = m_params.getVal_double("ex_dLots");
            ex_nIsNewOrder = Convert.ToInt32( m_params.getVal_string("ex_nIsNewOrder") );
            ex_bIsNewSession = Convert.ToBoolean(m_params.getVal_string("ex_bIsNewSession"));
        }
        public void setParams(string sName, string sVal)
        {
            m_params.setVal(sName, sVal);
        }
        public void publishTradeHistory()
        {
            CSiteMng.publishTradeHistory();
        }
        public void publishParams()
        {
            string sMsg = m_sLogicID;
            string sName = "";
            for ( int i = 0; i < m_params.getCount(); i ++ )
            {
                sName = m_params.getName(i);
                sMsg += "@";
                sMsg += sName;
                sMsg += ",";
                sMsg += m_params.getVal_string(sName);
            }
            //Add System name 
            sMsg += "@";
            sMsg += "system_name,";
            sMsg += CFATManager.getSystemName();

            CMQClient.publish_msg(sMsg, CFATCommon.MQ_TOPIC_PARAM_C2V);
        }
        public void setName(string sLogicName)
        {
            m_sLogicName = sLogicName;
        }
        public void setMode(string sMode)
        {
            m_sMode = sMode;
        }
        public void setParams(CParams objParams)
        {
            m_params = objParams;
        }

        public void addProduct(CProduct product)
        {
            m_products.Add(product);
        }

        private void make_opt_addParam(List<int> lstSubIndexs)
        {
            TLogicParamItem logicParamItem;
            string sName = "";
            string sVal = "";
            Dictionary<string, string> paramPair = new Dictionary<string, string>();
            string sLog = "";
            for (int i = 0; i < m_params.getCount(); i++)
            {
                sName = m_params.getName(i);
                logicParamItem = m_params.getParamItem(i);
                sVal = logicParamItem.getVal(lstSubIndexs[i]);
                paramPair.Add(sName, sVal);
                sLog += string.Format("{0},{1}, ", sName, sVal);
            }
            //CFATLogger.output_proc(sLog);
            m_optimize_params.Add(paramPair);
        }

        private void make_opt_param(List<int> lstSubIndexs)
        {
            if (lstSubIndexs.Count == m_params.getCount())
            {
                make_opt_addParam(lstSubIndexs);
                return;
            }

            TLogicParamItem logicParamItem;
            int nParamIndex = lstSubIndexs.Count;
            logicParamItem = m_params.getParamItem(nParamIndex);

            lstSubIndexs.Add(0);
            int nCnt = logicParamItem.getCount();
            if ( nCnt == 0 )
            {
                make_opt_param(new List<int>(lstSubIndexs));
                return;
            }
            for (int i = 0; i < nCnt; i++)
            {
                lstSubIndexs[nParamIndex] = i;
                make_opt_param(new List<int>(lstSubIndexs));
            }
        }

        public int makeAvailableParams()
        {
            m_optimize_params.Clear();
            m_nOptimizeID = 0;
            List<int> lstSubIndexs = new List<int>();
            make_opt_param(lstSubIndexs);
            return m_optimize_params.Count;
        }

        public bool nextParams()
        {
            if (m_nOptimizeID >= m_optimize_params.Count)
                return false;
            Dictionary<string, string> dicParams = m_optimize_params[m_nOptimizeID];
            for ( int i = 0; i < dicParams.Count; i ++ )
            {
                m_params.setVal(dicParams.ElementAt(i).Key, dicParams.ElementAt(i).Value);
            }
            m_nOptimizeID++;
            return true;
        }

        public virtual void publishVariables()
        {
            string sTxt = m_sLogicID;
            foreach(KeyValuePair<string, string> varItem in m_vars_publish )
            {
                sTxt += "@";
                sTxt += varItem.Key;
                sTxt += ",";
                sTxt += varItem.Value;
            }
            CMQClient.publish_msg(sTxt, CFATCommon.MQ_TOPIC_VARS);
        }

        public bool isPositionMatch_v2()
        {
            //if (CFATManager.m_nRunMode != ERUN_MODE.REALTIME)
            //    return true;

            foreach(CProduct product in m_products)
            {
                product.reqPosMatch();
            }

            return true;
        }

        public virtual void doForceClose()
        {
        }
        public virtual void waitOrderFilled()
        {

        }

        public void updateState_v2(int nWaitNeedCheckPosTime, int nWaitForceCloseTime, int nRestartFromErrorStop)
        {
            bool bIsPositionMatch = false;
            if (m_stState.m_nState == ELOGIC_STATE.NEED_CHECK_POSITION_MATCH ||
                m_stState.m_nState == ELOGIC_STATE.WAITING_FORCE_CLOSE)
            {
                bIsPositionMatch = isPositionMatch_v2();
                if (bIsPositionMatch)
                    setState(ELOGIC_STATE.WAITING_POSITION_MATCH);
                return;
            }


            if (m_stState.m_nState == ELOGIC_STATE.WAITING_POSITION_MATCH)
            {
                if (m_stState.getSeconds_Now2UpdateTime() > nWaitNeedCheckPosTime) //If position don't matched during 40 seconds, Force close
                {
                    setState(ELOGIC_STATE.NEED_FORCE_CLOSE);
                    return;
                }
            }

            if (m_stState.m_nState == ELOGIC_STATE.WAITING_POSITION_MATCH)
            {
                foreach (CProduct product in m_products)
                {
                    if (!product.isPositionMatch_v2())
                        return;
                }
                setState(ELOGIC_STATE.NORMAL);
                return;
            }

            if (m_stState.m_nState == ELOGIC_STATE.WAITING_FORCE_CLOSE)
            {
                if (m_stState.getSeconds_Now2UpdateTime() > nWaitForceCloseTime)// If position don't matched during 20 seconds after force close
                {
                    setState(ELOGIC_STATE.LOGIC_STOP_BY_ERROR);
                    return;
                }
            }

            if (m_stState.m_nState == ELOGIC_STATE.LOGIC_STOP_BY_ERROR)
            {
                if (m_stState.getSeconds_Now2UpdateTime() > nRestartFromErrorStop) // logic stoped by error, after 60 seconds will be normal state
                {
                    setState(ELOGIC_STATE.NORMAL);
                    return;
                }
            }

            if (m_stState.m_nState == ELOGIC_STATE.NEED_FORCE_CLOSE)
                doForceClose();

            if (m_stState.m_nState == ELOGIC_STATE.WAITING_ORDER_RESPONSE)
                waitOrderFilled();

        }

        private void load_unitTest()
        {
            m_lstUnitTest_signal.Clear();

            string sFile = Path.Combine(Application.StartupPath, "_unitTest\\" + m_sLogicID + ".txt");

            if (!File.Exists(sFile))
                return;

            string[] sLines = File.ReadAllLines(sFile);
            string[] sVals;
            TUNIT_TEST uTestData;
            for ( int i = 0; i < sLines.Length; i ++ )
            {
                sVals = sLines[i].Split(',');
                if (sVals.Length < 2) continue;
                uTestData = new TUNIT_TEST();
                uTestData.m_dtTime = Convert.ToDateTime(sVals[0]);
                uTestData.m_sCmd = sVals[1];
                m_lstUnitTest_signal.Add(uTestData);
            }
        }

        public int getSignal_unitTest()
        {
            for (int i = 0; i < m_lstUnitTest_signal.Count; i ++ )
            {
                if (m_lstUnitTest_signal[i].m_dtTime == CFATCommon.m_dtCurTime)
                {
                    CFATLogger.output_proc(string.Format("unitTest signal : {0},{1}", m_sLogicID, m_lstUnitTest_signal[i].m_sCmd));
                    return (int)TRADER.string2Cmd(m_lstUnitTest_signal[i].m_sCmd);
                }
            }
            return (int)ETRADER_OP.NONE;
        }
    }
}
