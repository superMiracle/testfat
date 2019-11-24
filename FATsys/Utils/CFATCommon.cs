using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace FATsys.Utils
{
    enum ERUN_MODE
    {
        REALTIME = 10,
        SIMULATION = 11,
        BACKTEST = 20,
        OPTIMIZE = 30,
    }
    public enum EERROR
    {
        NONE = 0,
        RATE_END = 1,
        RATE_INVALID = 2
    }

    enum ESYS_STATE
    {
        RUN = 0x100,
        PAUSE = 0x101,
        STOP = 0x102,
        STOP_ALL = 0x103
    }

    enum ETRADE_RET
    {
        NONE = 0,
        NEWORDER = 1
    }

    public enum EPRICE_MODE
    {
        BID = 1,
        ASK = 2,
        MIDDLE = 3,
    }
    public enum EPRICE_VAL
    {
        OPEN = 1,
        CLOSE = 2,
        HIGH = 3,
        LOW = 4,
    }
    public enum ETIME_FRAME
    {
        TICK = 0,
        MIN1 = 1,
    }
    public enum EORDER_TYPE
    {
        MARKET = 200,
        PENDING_STOP = 201,
        PENDING_LIMIT = 202
    }

    public enum EFILLED_STATE
    {
        FULL = 0,
        FAIL = 1,
        PARTIAL = 2
    }
    public enum ETRADER_OP
    {
        NONE = 0,
        BUY = 0x1,
        SELL = 0x10,
        BUY_CLOSE = 0x100,
        SELL_CLOSE = 0x1000,
    }

    public enum EPRODUCT_TYPE_PRICE
    {
        A_B = 1,
        A_BC = 2,
        A_B05C = 3,
    }
    public enum EPRODUCT_TYPE_TRADE
    {
        A_B = 1,
        A_BC = 2,
    }
    static class CFATCommon
    {

        public const int CACHE_SIZE = 200;
        public const int RATE_REC_BUF = 1000;
        public const double ESP = 1e-8;


        public const string CONFIG_SITE = "Config\\FATsys_site.config";
        public const string CONFIG_LOGIC = "Config\\FATsys_logic.config";
        public const string CONFIG_WORKTIME = "Config\\worktime.config";
        public const string CONFIG_GENERAL = "Config\\general.config";

        public const string MQ_TOPIC_LOG = "FAT_LOG";
        public const string MQ_TOPIC_ACCOUNT = "FAT_ACCOUNT";
        public const string MQ_TOPIC_POSITIONS = "FAT_POSITIONS";
        public const string MQ_TOPIC_POSHISTORY = "FAT_POSHISTORY";
        public const string MQ_TOPIC_VARS = "FAT_VARIABLE";
        public const string MQ_TOPIC_PARAM_C2V = "FAT_PARAM_C2V";
        public const string MQ_TOPIC_PARAM_V2C = "FAT_PARAM_V2C";
        public const string MQ_TOPIC_PRICE_TICK = "FAT_PRICE_TICK";
        public const string MQ_TOPIC_PRICE_MIN = "FAT_PRICE_MIN";

        public static string m_sOpt_param = "";
        public static DateTime m_dtCurTime;
    }



    static class TRADER
    {

        /// <summary>
        /// If nParent contains nChild then return true, otherwise return false
        /// ex : 0x10100 contains 0x00100
        /// </summary>
        /// <param name="nParent"></param>
        /// <param name="nChild"></param>
        /// <returns></returns>
        public static bool isContain(int nParent, int nChild)
        {
            return ((nParent & nChild) == nChild);
        }

        public static string cmd2String(ETRADER_OP nCmd)
        {
            if (nCmd == ETRADER_OP.BUY) return "BUY";
            if (nCmd == ETRADER_OP.SELL) return "SELL";
            if (nCmd == ETRADER_OP.BUY_CLOSE) return "BUYCLOSE";
            if (nCmd == ETRADER_OP.SELL_CLOSE) return "SELLCLOSE";
            return "NONE";
        }

        public static ETRADER_OP string2Cmd(string sCmd)
        {
            if (sCmd == "BUY") return ETRADER_OP.BUY;
            if (sCmd == "SELL") return ETRADER_OP.SELL;
            if (sCmd == "BUY_CLOSE") return ETRADER_OP.BUY_CLOSE;
            if (sCmd == "SELL_CLOSE") return ETRADER_OP.SELL_CLOSE;

            return ETRADER_OP.NONE;
        }

        public static ETRADER_OP cmdOpposite(ETRADER_OP nCmd)
        {
            if (nCmd == ETRADER_OP.BUY) return ETRADER_OP.SELL;
            if (nCmd == ETRADER_OP.SELL) return ETRADER_OP.BUY;
            if (nCmd == ETRADER_OP.BUY_CLOSE) return ETRADER_OP.SELL_CLOSE;
            if (nCmd == ETRADER_OP.SELL_CLOSE) return ETRADER_OP.BUY_CLOSE;

            return ETRADER_OP.NONE;
        }

        public static ETRADER_OP getCloseCmd(ETRADER_OP nCmd)
        {
            if (nCmd == ETRADER_OP.BUY) return ETRADER_OP.BUY_CLOSE;
            if (nCmd == ETRADER_OP.SELL) return ETRADER_OP.SELL_CLOSE;
            return nCmd;
        }
    }

    public static class CEvent_ParamChanged
    {
        public static bool g_bChanged = false;
        public static string g_sLogicID = "";
        public static Dictionary<string, string> g_params = new Dictionary<string, string>();
    }

    public class TWorkTimeInterval
    {
        public int m_nStart;
        public int m_nEnd;
    }

    public class TGeneralInfo
    {
        public string sSystemName = "FAT System";
        public string sHost = "127.0.0.1";
        public string sUser = "hedgemaster";
        public string sPwd = "hedgemaster";
    }
}
