using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;

namespace FATsys.Utils
{
    static class CFATLogger
    {
        /// <summary>
        /// Define Path of Log folder
        /// </summary>
        private static string PATH_PROC = "_log\\log_proc";
        private static string PATH_OPT = "_log\\log_opt";
        private static string PATH_RATE = "_log\\Rates_record";
        public static frmMain g_frm;
        private static List<string> m_lstLog_Rates = new List<string>();
        /// <summary>
        /// log file name
        /// </summary>
        private static string m_sLogFile_proc = "";
        private static string m_sLogFile_opt = "";
        private static string m_sLogFile_rates = "";

        // added by cmh
        private static EventWaitHandle waitHandle;
        // ---
        private static void init()
        {
            m_sLogFile_proc = Path.Combine(Application.StartupPath, PATH_PROC, DateTime.Now.ToString("yyyy-MM-dd HH_mm_ss_fff")+ ".log");
            string sFolder = Path.Combine(Application.StartupPath, PATH_PROC);
            if (!Directory.Exists(sFolder))
                Directory.CreateDirectory(sFolder);

            m_sLogFile_opt = Path.Combine(Application.StartupPath, PATH_OPT, DateTime.Now.ToString("yyyy-MM-dd HH_mm_ss_fff") + ".csv");
            sFolder = Path.Combine(Application.StartupPath, PATH_OPT);
            if (!Directory.Exists(sFolder))
                Directory.CreateDirectory(sFolder);

            m_lstLog_Rates.Clear();
            waitHandle = new EventWaitHandle(true, EventResetMode.AutoReset, "SHARED_BY_ALL_PROCESSES");
        }
        public static void output_proc(string sTxt)
        {
            CMQClient.publish_msg(sTxt, CFATCommon.MQ_TOPIC_LOG);
            if (m_sLogFile_proc.Length == 0 )
                init();
            string sLog = string.Format("[{0}],{1}\r\n", DateTime.Now.ToString("yyyy-MM-dd HH_mm_ss_fff"), sTxt);
            g_frm.addLog(sLog);

            waitHandle.WaitOne();
            File.AppendAllText(m_sLogFile_proc, sLog);
            waitHandle.Set();
        }

        public static void output_opt(string sTxt)
        {
            if (m_sLogFile_opt.Length == 0)
                init();
            string sLog = string.Format("{0},", sTxt);
            //g_frm.addLog(sLog);

            waitHandle.WaitOne();
            File.AppendAllText(m_sLogFile_opt, sLog);
            waitHandle.Set();
        }
        public static void output_opt_newLine()
        {
            if (m_sLogFile_opt.Length == 0)
                init();

            waitHandle.WaitOne();
            File.AppendAllText(m_sLogFile_opt, "\r\n");
            waitHandle.Set();
        }

        public static void registerForm(frmMain frm)
        {
            g_frm = frm;
        }

        public static void makeRatesFileName(string sLogFolder)
        {
            m_sLogFile_rates = Path.Combine(Application.StartupPath, PATH_RATE, sLogFolder, DateTime.Now.ToString("yyyy-MM-dd HH_mm_ss_fff") + ".csv");
            string sFolder = Path.Combine(Application.StartupPath, PATH_RATE, sLogFolder);
            if (!Directory.Exists(sFolder))
                Directory.CreateDirectory(sFolder);
        }

        public static void record_reates_done()
        {
            if (m_lstLog_Rates.Count > 0)
            {
                waitHandle.WaitOne();
                File.AppendAllLines(m_sLogFile_rates, m_lstLog_Rates);
                waitHandle.Set();

                m_lstLog_Rates.Clear();
            }
        }

        public static void record_rates(string sLogFolder, string sRates)
        {
            if (m_sLogFile_rates == "")
                makeRatesFileName(sLogFolder);
            Stream myFile;
            TextWriterTraceListener myTextListener;
            //If change folder name , 
            if (!m_sLogFile_rates.Contains(sLogFolder))
            {
                if (m_lstLog_Rates.Count > 0)
                {
                    waitHandle.WaitOne();
                    File.AppendAllLines(m_sLogFile_rates, m_lstLog_Rates);
                    waitHandle.Set();

                    m_lstLog_Rates.Clear();
                }
                makeRatesFileName(sLogFolder);
            }
            
            if (m_lstLog_Rates.Count < CFATCommon.RATE_REC_BUF)
            {
                m_lstLog_Rates.Add(sRates);
                return;
            }
            waitHandle.WaitOne();
            File.AppendAllLines(m_sLogFile_rates, m_lstLog_Rates);
            waitHandle.Set();

            m_lstLog_Rates.Clear();
            m_lstLog_Rates.Add(sRates);
            //If FileSize over than 2MB
            FileInfo fileInfo = new FileInfo(m_sLogFile_rates);
            if (fileInfo.Length > 2 * 1024 * 1024)
                makeRatesFileName(sLogFolder);

        }
    }


}
