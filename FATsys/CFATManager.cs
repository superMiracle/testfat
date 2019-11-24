using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FATsys.Logic;
using FATsys.Logic.SHGold;

using FATsys.Utils;
using FATsys.Site;
using FATsys.Product;

using System.Windows.Forms;
using System.Reflection;

namespace FATsys
{
    class CFATManager
    {
        private List<CLogic> m_lstLogics;
        private List<Dictionary<string, string>> m_configSites;
        private List<Dictionary<string, string>> m_configLogics;
        private List<TWorkTimeInterval> m_configWorkTimes;

        private static TGeneralInfo m_configGeneral;

        public int m_nInvalidTicks = 0;
        public static ERUN_MODE m_nRunMode;
        private ESYS_STATE m_nState;
        public CFATManager(ERUN_MODE nMode = ERUN_MODE.BACKTEST, string sMode= "")
        {
            m_configGeneral = new TGeneralInfo();
            CConfigMng.load_config_general(ref m_configGeneral);
            CMQClient.connectToMQ(m_configGeneral.sHost, m_configGeneral.sUser, m_configGeneral.sPwd);


            CFATLogger.output_proc(string.Format("######  Start Mode = {0} #######", sMode));
            CFATManager.m_nRunMode = nMode;
            m_lstLogics = new List<CLogic>();

            m_configLogics = new List<Dictionary<string, string>>();
            m_configSites = new List<Dictionary<string, string>>();
            m_configWorkTimes = new List<TWorkTimeInterval>();
            
        }
        public static bool isOnlineMode()
        {
            if (m_nRunMode == ERUN_MODE.REALTIME || m_nRunMode == ERUN_MODE.SIMULATION)
                return true;
            return false;
        }
        public bool init()
        {

            if (!CConfigMng.load_config_site(ref m_configSites)) return false;
            if (!CConfigMng.load_config_logic(ref m_configLogics)) return false;
            if (!CConfigMng.load_config_workTime(ref m_configWorkTimes)) return false;
            

            return true;
        }

        /// <summary>
        /// Create Sites
        /// Create Logics
        /// </summary>
        /// <returns></returns>
        private bool create_Objects_site()
        {
            CFATLogger.output_proc("create_Objects_site ------->");
            
            string sSiteName = "";

            string sPipeServer = "";
            string sPipeServerOrder = "";

            string sPipe_rate = "";
            string sPipe_order = "";

            string sID;
            string sPwd;
            string sKey;
            int nSymCnt = 0;
            CSite site;

            string sSymbol = "";
            double dContractSize = 0;
            double dCommissionPercent = 0;

            try
            {
                foreach (Dictionary<string, string> dicSiteItem in m_configSites)
                {
                    // modified by cmh
                    sSiteName = dicSiteItem["name"];
                    if ( dicSiteItem.ContainsKey("pipe_rate"))
                        sPipeServer = dicSiteItem["pipe_rate"];
                    if (dicSiteItem.ContainsKey("pipe_order"))
                        sPipeServerOrder = dicSiteItem["pipe_order"];
                    if ( dicSiteItem.ContainsKey("pipe_rate"))
                        sPipe_rate = dicSiteItem["pipe_rate"];
                    if (dicSiteItem.ContainsKey("pipe_order"))
                        sPipe_order = dicSiteItem["pipe_order"];
                    
                    
                    site = CSiteMng.newSite(sSiteName, sPipeServer, sPipeServerOrder, sPipe_rate, sPipe_order);
                    // ---
                    sID = dicSiteItem["id"];
                    sPwd = dicSiteItem["pwd"];                    
                    site.setID_PWD(sID, sPwd);
                    
                    nSymCnt = Convert.ToInt32(dicSiteItem["sym_count"]);                    
                    for (int i = 0; i < nSymCnt; i++)
                    {
                        sKey = string.Format("sym_{0}", i + 1);
                        sSymbol = dicSiteItem[sKey];

                        sKey = string.Format("c_size_{0}", i + 1);
                        dContractSize = Convert.ToDouble( dicSiteItem[sKey] );
                        CFATLogger.output_proc(string.Format("symbol = {0}, contract = {1}", sSymbol,dContractSize));

                        sKey = string.Format("commission_{0}", i + 1);
                        dCommissionPercent = Convert.ToDouble(dicSiteItem[sKey]);
                        site.addSym(sSymbol, dContractSize, dCommissionPercent);
                    }
                    
                    if (!dicSiteItem.ContainsKey("fix")) continue;
                    //for Fix
                    nSymCnt = Convert.ToInt32(dicSiteItem["sym_fix_count"]);
                    double dMin = 0;
                    double dMax = 0;

                    for ( int i = 0; i < nSymCnt; i ++ )
                    {
                        sKey = string.Format("sym_fix_{0}", i + 1);
                        site.addSym_fix(dicSiteItem[sKey], (i+1).ToString());

                        //Set min max for symbol
                        sKey = string.Format("min_fix_{0}", i + 1);
                        dMin = Convert.ToDouble(dicSiteItem[sKey]);
                        sKey = string.Format("max_fix_{0}", i + 1);
                        dMax = Convert.ToDouble(dicSiteItem[sKey]);
                        site.addSym_min_max((i + 1).ToString(), dMin, dMax);
                    }
                    
                    site.setConfigFile_fix(dicSiteItem["config_data"], dicSiteItem["config_trade"]);
                    site.setFixAccount(dicSiteItem["fix_acc"]);
                }
            }
            catch
            {
                CFATLogger.output_proc("Error : create_Objects_site");
                return false;
            }
            CFATLogger.output_proc("create_Objects_site <-------");
            return true;
        }

        public bool create_Objects_logic()
        {
            CFATLogger.output_proc("create_Objects_logic ------->");
            string sLogicName = "";
            CLogic logic = null;
            CSite site;
            CParams objParams ;
            CProduct objProduct;
            int nParamCnt = 0;
            int nProductCnt = 0;
            double dContractSize = 0;

            string sKey = "";
            string sSiteName = "";
            string sSymbol = "";

            string[] sVals;
            string sLogicID = "NONE";
            string sMode = "NONE";

            try
            {
                foreach (Dictionary<string, string> dicLogicItem in m_configLogics)
                {
                    
                    sLogicName = dicLogicItem["name"];
                    sLogicID = dicLogicItem["logic_id"];
                    sMode = dicLogicItem["mode"];

                    logic = create_newLogic(sLogicName, sMode);
                    logic.m_sLogicID = sLogicID;
                    //create & set parameters
                    objParams = new CParams();
                    nParamCnt = Convert.ToInt32(dicLogicItem["param_count"]);
                    for (int i = 0; i < nParamCnt; i++)
                    {
                        sKey = string.Format("param_{0}", i + 1);
                        sVals = dicLogicItem[sKey].Split(',');
                        if ( sVals.Length < 5)
                        {
                            CFATLogger.output_proc("Error : Invalid format logic params, ex : 'varName, varVal, [varStart], [varStep], [varEnd]'");
                            return false;
                        }
                        objParams.addParam(sVals[0], sVals[1], sVals[2], sVals[3], sVals[4]);
                    }
                    logic.setParams(objParams);

                    //create & set product
                    nProductCnt = Convert.ToInt32(dicLogicItem["product_count"]);
                    logic.m_products.Clear();
                    for (int i = 0; i < nProductCnt; i++)
                    {
                        objProduct = new CProduct();

                        sKey = string.Format("product_{0}_site", i + 1);
                        sSiteName = dicLogicItem[sKey];
                        site = CSiteMng.getSite(sSiteName);
                        objProduct.setSite(site);
                        objProduct.setLogicID(sLogicID);

                        sKey = string.Format("product_{0}_symbol", i + 1);
                        sSymbol = dicLogicItem[sKey];
                        objProduct.setSymbol(sSymbol);

                        dContractSize = site.getContractSize(sSymbol);
                        objProduct.setContractSize(dContractSize);

                        logic.addProduct(objProduct);
                    }
                    m_lstLogics.Add(logic);

                    logic.publishParams();//
                }
            }
            catch
            {
                CFATLogger.output_proc("Error : create_Objects_logic");
                return false;
            }
            CFATLogger.output_proc("create_Objects_logic <-------");
            return true;
        }


        private CLogic create_newLogic(string sLogicName, string sMode = "NONE")
        {
            CLogic logic = null;
            switch(sLogicName)
            {
                case "ARB_ORG":
                    logic = new CLogic_Arb_org();
                    break;

                case "PAIR_CC_V1":
                    logic = new CLogic_Pair_V1();
                    break;
                case "PAIR_CC_V2":
                    logic = new CLogic_Pair_V2();
                    break;
                case "PAIR_CC_V3":
                    logic = new CLogic_Pair_V3();
                    break;
                case "PAIR_CC_V4":
                    logic = new CLogic_Pair_V4();
                    break;
                case "RATE_REC":
                    logic = new CLogic_Price_Record();
                    break;

                default:
                    break;
            }


            logic.setName(sLogicName);
            logic.setMode(sMode);

            return logic;
        }

        private bool create_Objects()
        {
            if (!create_Objects_site())
                return false;

            if (!create_Objects_logic())
                return false;
            return true;
        }
        private void destroy_Objects()
        {

        }
        public void stop()
        {
            CFATLogger.output_proc("Clicked Stop All !!!");
            m_nState = ESYS_STATE.STOP_ALL;
        }
        public void doProcess()
        {
            Thread threadProcess = new Thread(doProcess_thread);
            threadProcess.Start();
        }

        public bool OnInit()
        {
            if (!CSiteMng.OnInit())
                return false;
            CFATLogger.output_proc("Logic init start!");
            foreach (CLogic objLogic in m_lstLogics)
            {
                if (!objLogic.OnInit())
                    return false;
            }
            CFATLogger.output_proc("Logic init OK!");
            return true;

        }
        public void OnDeInit()
        {
            CSiteMng.OnDeinit();
            foreach (CLogic objLogic in m_lstLogics)
            {
                objLogic.OnDeInit();
            }
        }

        public static string getSystemName()
        {
            return m_configGeneral.sSystemName;
        }

        private void changeLogicParams()
        {
            foreach (CLogic objLogic in m_lstLogics)
            {
                if ( objLogic.m_sLogicID == CEvent_ParamChanged.g_sLogicID)
                {
                    objLogic.changeParams();
                    objLogic.loadParams();
                    return;
                }
            }

        }
        public bool OnTick()
        {
            EERROR nRet = CSiteMng.Ontick();
            if (nRet == EERROR.RATE_INVALID)
            {
                if (CFATManager.isOnlineMode())
                    CFATLogger.output_proc("Invalid price!");
                m_nInvalidTicks++;
                return false;
            }
            if (nRet == EERROR.RATE_END)
            {
                CFATLogger.output_proc("Rates End!");
                m_nInvalidTicks = 10000;
                return false; // For backtest mode, update tick data , if return false rates is end.
            }
            m_nInvalidTicks = 0;
            if (nRet != EERROR.NONE)
            {
                CFATLogger.output_proc("CSiteMng.Ontick() : Unknown Error!");
                return false;
            }

            foreach (CLogic objLogic in m_lstLogics)
            {
                if (objLogic.OnTick() == (int)ETRADE_RET.NEWORDER)
                    break;
            }

            CSiteMng.process_ReqOrders();
            CSiteMng.process_ReqPosMatch();
            return true;
        }

        private int optimize_makeAvailableParams()
        {
            return m_lstLogics[0].makeAvailableParams();
        }

        private bool optimize_nextParam()
        {
            return m_lstLogics[0].nextParams();
        }
        private void process_event()
        {
            //Parameter changed
            if ( CEvent_ParamChanged.g_bChanged )
            {
                CFATLogger.output_proc("Request : parameter change");
                CEvent_ParamChanged.g_bChanged = false;
                changeLogicParams();
            }
            //----------------
        }

        private bool isWorkTime()
        {
            if (!CFATManager.isOnlineMode())
                return true;

            if (m_configWorkTimes.Count == 0)
                return true;
            int nMinsToday = DateTime.Now.Hour * 60 + DateTime.Now.Minute;
            foreach(TWorkTimeInterval workItemInterval in m_configWorkTimes)
            {
                if (nMinsToday >= workItemInterval.m_nStart && nMinsToday <= workItemInterval.m_nEnd)
                    return true;
            }
            return false;
        }
        public void doProcess_run()
        {
            CFATLogger.output_proc("#### Run Start ------>");
            m_nState = ESYS_STATE.RUN;
            OnInit();
            m_nInvalidTicks = 0;
            while (m_nState == ESYS_STATE.RUN)
            {
                if ( !isWorkTime() )
                {
                    CFATLogger.output_proc("No work time!!!");
                    break;
                }

                process_event();// If get event from Manager, do it.
                if (!OnTick())
                {
                    if (m_nInvalidTicks > 200 )
                    {
                        CFATLogger.output_proc("Network connections fail!, Systom will stop!");
                        break;
                    }
                }

                //if ( CFATManager.isOnlineMode())
                //  Thread.Sleep(1);
                //Thread.Sleep(10);

            }

            CFATLogger.output_proc(string.Format("System Stoped , state = {0}", m_nState) );
            if (m_nState != ESYS_STATE.STOP_ALL)
                m_nState = ESYS_STATE.STOP;
            
            makeReport();
            OnDeInit();
            CFATLogger.output_proc("#### Run End <------");
        }

        public void doProcess_optimize()
        {
            if ( m_configLogics.Count > 1 )
            {
                CFATLogger.output_proc("Run End <------");
                return;
            }

            int nOptCnt = optimize_makeAvailableParams();//Make available params pair
            int nOptID = 0;

            while (m_nState != ESYS_STATE.STOP_ALL)
            {
                nOptID++;
                if (!optimize_nextParam())
                    break;
                CFATLogger.output_proc(string.Format("Optimize Start : Optimize count = {0}/{1} ----->", nOptID, nOptCnt));
                doProcess_run();
            }

            CFATLogger.output_proc("Optimize Done!!!");
        }

        public void doProcess_thread()
        {
            if (!create_Objects())
                return;

            CSiteMng.loadRates_Tick();// load backtest rates for backtest

            if ( CFATManager.m_nRunMode != ERUN_MODE.OPTIMIZE)
                doProcess_run();

            if (CFATManager.m_nRunMode == ERUN_MODE.OPTIMIZE)
                doProcess_optimize();

            destroy_Objects();
        }
        public void matchPosVirtual2Real()
        {
            foreach (CLogic objLogic in m_lstLogics)
            {
                objLogic.matchPosVirtual2Real();
            }
        }

        public void makeReport()
        {
            CSiteMng.makeReport();
        }
    }
}
