using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using FATsys.Utils;
using FATsys.Site.BTC;
using FATsys.Site.CN;
using FATsys.TraderType;
using FATsys.Site.Forex;
using ylink;

namespace FATsys.Site
{
    static class CSiteMng
    {
        private static Dictionary<string, CSite> g_allSites = new Dictionary<string, CSite>();

        private static CReqOrderMng g_reqOrderMng = new CReqOrderMng();

        private static List<TReqPosMatch> g_lstReqPosMatch = new List<TReqPosMatch>();

        public static CSite newSite(string sSiteName, string sPipeSerName = "", string sPipeSerOrderName = "", string sPipe_rate = "", string sPipe_order = "")
        {
            CSite site = null;
            if (!CFATManager.isOnlineMode())
                return CSiteMng.newSite_backtest(sSiteName);
            // modified by cmh
            //if (sSiteName == "TRADE_VIEW" || sSiteName == "FXCM" || sSiteName == "GP_MT4")            
            //             {
            //                 site = new CSiteMT4();
            //                 site.setName(sSiteName);
            //                 site.setPipeServerName(sPipeSerName);
            //                 g_allSites.Add(sSiteName, site);
            //             }

            if (sSiteName == "TRADE_VIEW_MT4" || sSiteName == "GP_MT4" || sSiteName == "AAFX_MT4" || sSiteName == "EAGLEFX_MT4" || sSiteName == "IC_MT4" || sSiteName == "XTREAM_MT4")
            {
                site = new CSiteMT5();
                site.setName(sSiteName);
                site.setPipeServerName_MT5(sPipeSerName, sPipeSerOrderName);
                g_allSites.Add(sSiteName, site);
            }
            
            if (sSiteName == "TRADE_VIEW_MT5" || sSiteName == "GP_MT5" || sSiteName == "AAFX_MT5" || sSiteName == "EAGLEFX_MT5" || sSiteName == "IC_MT5" || sSiteName == "XTREAM_MT5")
            {
                site = new CSiteMT5();
                site.setName(sSiteName);
                site.setPipeServerName_MT5(sPipe_rate, sPipe_order);
                g_allSites.Add(sSiteName, site);
            }
            // ---

            if (sSiteName == "TRADE_VIEW")
            {
                site = new CSiteFixMt4V2();
                site.setName(sSiteName);
                site.setPipeServerName(sPipeSerName);
                g_allSites.Add(sSiteName, site);
            }

            if (sSiteName == "GP")
            {
                site = new CSiteFixMt4();
                //site = new CSiteMT4();
                site.setName(sSiteName);
                site.setPipeServerName(sPipeSerName);
                g_allSites.Add(sSiteName, site);
            }

            if ( sSiteName == "SHGOLD")
            {
                site = new CSiteSHGold();
                site.setName(sSiteName);
                site.setPipeServerName(sPipeSerName);
                g_allSites.Add(sSiteName, site);
            }

            if ( sSiteName == "IB")
            {
                site = new CSiteIB();
                site.setName(sSiteName);
                g_allSites.Add(sSiteName, site);
            }

            if ( sSiteName == "BITMEX")
            {
                site = new CSiteBitMex();
                site.setName(sSiteName);
                g_allSites.Add(sSiteName, site);
            }

            if (sSiteName == "OKEX")
            {
                site = new CSiteOkex();
                site.setName(sSiteName);
                g_allSites.Add(sSiteName, site);
            }

            if ( sSiteName == "DERIBIT")
            {
                site = new CSiteDeribit();
                site.setName(sSiteName);
                g_allSites.Add(sSiteName, site);
            }

            if (sSiteName == "BITFLY")
            {
                site = new CSiteBitFlyer();
                site.setName(sSiteName);
                g_allSites.Add(sSiteName, site);
            }
            if ( sSiteName == "CNFUTURE")
            {
                site = new CSiteCNFT();
                site.setName(sSiteName);
                g_allSites.Add(sSiteName, site);
            }
            return site;
        }

        public static CSite newSite_backtest(string sSiteName)
        {
            CSite site = null;
            site = new CSiteBackTest();
            site.setName(sSiteName);
            g_allSites.Add(sSiteName, site);
            return site;
        }

        public static bool OnInit()
        {
            foreach (KeyValuePair<string, CSite> entry in CSiteMng.g_allSites)
            {
                if (!entry.Value.OnInit())
                    return false;
            }
            return true;
        }
        public static void OnDeinit()
        {
            foreach (KeyValuePair<string, CSite> entry in CSiteMng.g_allSites)
            {
                entry.Value.OnDeInit();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns>
        /// if one of sites return error, return error
        /// </returns>
        public static EERROR Ontick()
        {
            EERROR nRet = EERROR.NONE;
            EERROR nErr = EERROR.NONE;
            CFATCommon.m_dtCurTime = DateTime.Now;//For synchronize of all CSite.m_rates.time
            foreach (KeyValuePair<string, CSite> entry in CSiteMng.g_allSites)
            {
                nRet = entry.Value.OnTick();
                if (nRet != EERROR.NONE)
                    nErr = nRet;
            }

            return nErr;
        }
        public static CSite getSite(string sSiteName)
        {
            return g_allSites[sSiteName];
        }

        public static void loadRates_Tick()
        {
            foreach (KeyValuePair<string, CSite> entry in CSiteMng.g_allSites)
            {
                entry.Value.loadRates_Tick();
            }
        }

        public static void makeReport()
        {
            bool bFirstSite = true;
            foreach (KeyValuePair<string, CSite> entry in CSiteMng.g_allSites)
            {
                entry.Value.makeReport(bFirstSite);
                bFirstSite = false;
            }

            if (CFATManager.m_nRunMode == ERUN_MODE.OPTIMIZE)
                CFATLogger.output_opt_newLine();
        }

        public static void publishTradeHistory()
        {
            bool bPublished = false;
            foreach (KeyValuePair<string, CSite> entry in CSiteMng.g_allSites)
            {
                bPublished |= entry.Value.publishTradeHistory();
            }

            if ( bPublished)
                CMQClient.publish_msg("DONE", CFATCommon.MQ_TOPIC_POSHISTORY);
        }

        public static void clearReqOrders()
        {
            g_reqOrderMng.clearReqOrders();
        }

        public static int registerOrder(TReqOrder reqOrder)
        {
            return g_reqOrderMng.registerOrder(reqOrder);
        }

        public static void process_ReqOrders()
        {
            g_reqOrderMng.process_ReqOrders();
        }

        public static void registerPosMatch(TReqPosMatch reqPosMatch)
        {
            g_lstReqPosMatch.Add(reqPosMatch);
        }

        public static void process_ReqPosMatch()
        {
            if (g_lstReqPosMatch.Count == 0)
                return;

            foreach(TReqPosMatch reqPosMatch in g_lstReqPosMatch)
            {
                reqPosMatch.process_PosMatch();
            }
            g_lstReqPosMatch.Clear();
        }
    }
}
