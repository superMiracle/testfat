using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FATsys.Utils
{
    static class CConfigMng
    {
        public static bool load_config_logic(ref List<Dictionary<string, string>> configLogics)
        {
            //load logic config
            CFATLogger.output_proc("load logic config ----------->");
            try
            {
                string sConfig = Path.Combine(Application.StartupPath, CFATCommon.CONFIG_LOGIC);
                CIniFile iniFile = new CIniFile(sConfig);

                string sSection;
                Dictionary<string, string> dicItem;

                int nCnt = Convert.ToInt32(iniFile.Read("logic_count", "COMMON"));
                int nSubCnt = 0;
                string sKey = "";
                string sVal = "";
                for (int i = 0; i < nCnt; i++)
                {
                    dicItem = new Dictionary<string, string>();
                    sSection = string.Format("LOGIC_{0}", i + 1);

                    dicItem.Add("name", iniFile.Read("name", sSection));
                    dicItem.Add("logic_id", iniFile.Read("logic_id", sSection));
                    dicItem.Add("mode", iniFile.Read("mode", sSection));

                    sVal = iniFile.Read("param_count", sSection);
                    nSubCnt = Convert.ToInt32(sVal);
                    dicItem.Add("param_count", nSubCnt.ToString());
                    for (int k = 0; k < nSubCnt; k++)
                    {
                        sKey = string.Format("param_{0}", k + 1);
                        dicItem.Add(sKey, iniFile.Read(sKey, sSection));
                    }

                    nSubCnt = Convert.ToInt32(iniFile.Read("product_count", sSection));
                    dicItem.Add("product_count", nSubCnt.ToString());
                    for (int k = 0; k < nSubCnt; k++)
                    {
                        sKey = string.Format("product_{0}_site", k + 1);
                        dicItem.Add(sKey, iniFile.Read(sKey, sSection));

                        sKey = string.Format("product_{0}_symbol", k + 1);
                        dicItem.Add(sKey, iniFile.Read(sKey, sSection));
                    }

                    configLogics.Add(dicItem);
                }
            }
            catch
            {
                CFATLogger.output_proc("Error : load logic config!");
                return false;
            }
            CFATLogger.output_proc("load logic config <-----------");
            return true;
        }
        public static bool load_config_general(ref TGeneralInfo generalInfo)
        {
            //CFATLogger.output_proc("load general config --------->");
            string sConfig = Path.Combine(Application.StartupPath, CFATCommon.CONFIG_GENERAL);
            if (!File.Exists(sConfig))
                return true;

            CIniFile iniFile = new CIniFile(sConfig);
            generalInfo.sSystemName = iniFile.Read("name", "GENERAL");
            generalInfo.sHost = iniFile.Read("host", "SERVER");
            generalInfo.sUser = iniFile.Read("user","SERVER");
            generalInfo.sPwd = iniFile.Read("pwd", "SERVER");
            //CFATLogger.output_proc("load general config <---------");
            return true;
        }
        public static bool load_config_workTime(ref List<TWorkTimeInterval> workTimes)
        {
            CFATLogger.output_proc("load worktime config --------->");
            workTimes.Clear();
            string sConfig = Path.Combine(Application.StartupPath, CFATCommon.CONFIG_WORKTIME);
            if (!File.Exists(sConfig))
                return true;
            CIniFile iniFile = new CIniFile(sConfig);
            int nCnt = Convert.ToInt32(iniFile.Read("count", "WORK_TIME"));
            string sKey = "";
            
            TWorkTimeInterval workTimeInterval;
            for ( int i = 0; i < nCnt; i ++ )
            {
                workTimeInterval = new TWorkTimeInterval();

                sKey = string.Format("start_{0}", (i+1).ToString());
                workTimeInterval.m_nStart = Convert.ToInt32(iniFile.Read(sKey, "WORK_TIME"));

                sKey = string.Format("end_{0}", (i + 1).ToString());
                workTimeInterval.m_nEnd = Convert.ToInt32(iniFile.Read(sKey, "WORK_TIME"));

                workTimes.Add(workTimeInterval);
            }
            CFATLogger.output_proc("load worktime config <---------");
            return true;
        }
        public static bool load_config_site(ref List<Dictionary<string, string>> configSites)
        {
            // load site config
            CFATLogger.output_proc("load site config ----------->");
            try
            {
                string sConfig = Path.Combine(Application.StartupPath, CFATCommon.CONFIG_SITE);

                CIniFile iniFile = new CIniFile(sConfig);
                int nCnt = Convert.ToInt32(iniFile.Read("site_count", "COMMON"));

                string sSection, sVal, sKey;
                Dictionary<string, string> dicItem;
                int nSymCnt = 0;

                for (int i = 0; i < nCnt; i++)
                {
                    dicItem = new Dictionary<string, string>();
                    sSection = string.Format("SITE_{0}", (i + 1).ToString());

                    dicItem.Add("name", iniFile.Read("name", sSection));
                    dicItem.Add("id", iniFile.Read("id", sSection));
                    dicItem.Add("pwd", iniFile.Read("pwd", sSection));
                    dicItem.Add("pipe1", iniFile.Read("pipe1", sSection));
                    dicItem.Add("pipe2", iniFile.Read("pipe2", sSection));
                    dicItem.Add("pipe_rate", iniFile.Read("pipe_rate", sSection));
                    dicItem.Add("pipe_order", iniFile.Read("pipe_order", sSection));

                    sVal = iniFile.Read("sym_count", sSection);
                    dicItem.Add("sym_count", sVal);
                    nSymCnt = Convert.ToInt32(sVal);

                    for ( int k = 0; k < nSymCnt; k ++ )
                    {
                        sKey = string.Format("sym_{0}", k+1);
                        dicItem.Add(sKey, iniFile.Read(sKey, sSection));

                        sKey = string.Format("c_size_{0}", k + 1);
                        dicItem.Add(sKey, iniFile.Read(sKey, sSection));

                        sKey = string.Format("commission_{0}", k + 1);
                        dicItem.Add(sKey, iniFile.Read(sKey, sSection));
                    }

                    //For Fix info
                    if (iniFile.Read("fix", sSection) != "1")
                    {
                        configSites.Add(dicItem);
                        continue;
                    }

                    dicItem.Add("fix", "1");
                    sVal = iniFile.Read("sym_fix_count", sSection);
                    dicItem.Add("sym_fix_count", sVal);
                    nSymCnt = Convert.ToInt32(sVal);

                    for ( int k = 0; k < nSymCnt; k ++ )
                    {
                        sKey = string.Format("sym_fix_{0}", k + 1);
                        dicItem.Add(sKey, iniFile.Read(sKey, sSection));

                        sKey = string.Format("min_fix_{0}", k + 1);
                        dicItem.Add(sKey, iniFile.Read(sKey, sSection));

                        sKey = string.Format("max_fix_{0}", k + 1);
                        dicItem.Add(sKey, iniFile.Read(sKey, sSection));

                    }

                    dicItem.Add("config_data", iniFile.Read("config_data", sSection));
                    dicItem.Add("config_trade", iniFile.Read("config_trade", sSection));
                    dicItem.Add("fix_acc", iniFile.Read("fix_acc", sSection));

                    configSites.Add(dicItem);
                }
            }
            catch
            {
                CFATLogger.output_proc("Error : load site config!");
                return false;
            }
            CFATLogger.output_proc("load site config <-----------");
            return true;
        }
    }
}
