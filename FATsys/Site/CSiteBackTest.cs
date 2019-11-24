using System;
using System.IO;
using System.Collections.Generic;
using CsvHelper;
using System.Text;
using System.Linq;
using System.Windows.Forms;
using System.Threading.Tasks;
using FATsys.TraderType;
using FATsys.Utils;

namespace FATsys.Site
{
    class CSiteBackTest : CSite
    {
        private int m_nPos_rate;
        private Dictionary<string, List<TRatesTick>> m_ratesTick = new Dictionary<string, List<TRatesTick>>();
        /// <summary>
        /// load backtest tick rates from file
        /// </summary>
        public override void loadRates_Tick()
        {
            string sFile = "";
            foreach (string sSym in m_sSymbols)
            {
                sFile = Path.Combine(Application.StartupPath, 
                    "_log\\backtest_rates\\" + m_sSiteName,sSym + ".csv");
                CFATLogger.output_proc("loadRates_Tick : ----->" + sFile);
                var objSR = new StreamReader(sFile);
                var objCSVR = new CsvReader(objSR);
                objCSVR.Configuration.HasHeaderRecord = false;
                List<TRatesTick> objRecords = new List<TRatesTick>();
                objRecords = objCSVR.GetRecords<TRatesTick>().ToList();
                m_ratesTick.Add(sSym, objRecords);
                CFATLogger.output_proc("loadRates_Tick : <-----" + sFile);
            }
        }

        public override bool OnInit()
        {
            m_nPos_rate = 0;
            return base.OnInit();
        }
        public override EERROR OnTick()
        {
            int nRatesCnt = 0;
            foreach(string sSym in m_sSymbols)
            {
                m_rates[sSym] = m_ratesTick[sSym][m_nPos_rate];
                nRatesCnt = m_ratesTick[sSym].Count;
                CFATCommon.m_dtCurTime = m_rates[sSym].m_dtTime;
            }

            m_nPos_rate++;
            if (m_nPos_rate == nRatesCnt) return EERROR.RATE_END;

            return base.OnTick();
        }

        public override void OnDeInit()
        {
            base.OnDeInit();
        }


        public override EFILLED_STATE reqOrder(string sSymbol, ETRADER_OP nCmd, ref double dLots, ref double dPrice, EORDER_TYPE nOrderType, string sLogicID, string sComment = "",
                double dLots_exc = 0, double dPrice_exc = 0, DateTime dtTime_exc = default(DateTime))
        {
            EFILLED_STATE nRet = EFILLED_STATE.FULL;
            ETRADER_OP nCmd_req = nCmd;
            double dLots_req = dLots;
            available_lots_cmd(sSymbol, ref nCmd_req, ref dLots_req);

            if (Math.Abs(dLots_req - dLots) > CFATCommon.ESP)
            {
                nRet = EFILLED_STATE.PARTIAL;
                dLots = dLots_req;
            }

            base.reqOrder(sSymbol, nCmd_req, ref dLots_req, ref dPrice, nOrderType, sLogicID, sComment, dLots_req, dPrice, CFATCommon.m_dtCurTime);
            return nRet;
        }

    }
}
