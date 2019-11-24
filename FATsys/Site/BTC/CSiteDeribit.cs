using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using FATsys.Utils;
using FATsys.TraderType;

using DeribitAPI;

namespace FATsys.Site.BTC
{
    class CSiteDeribit : CSite
    {
        public override bool OnInit()
        {
            FrmDeribit.createDeribitAPI("7nLDQ11O", "T2-I4r2AcEXQAGgUaECwQd-Cch4i5sRMfCPSwQsN3E8");
            foreach (string sSymbol in m_sSymbols)
            {
                FrmDeribit.subScribe(sSymbol);
            }
            Thread.Sleep(2000);
            return base.OnInit();
        }

        public override EERROR OnTick()
        {
            double dBid = 0;
            double dAsk = 0;
            double dBidVol = 0;
            double dAskVol = 0;

            foreach (string sSymbol in m_sSymbols)
            {
                FrmDeribit.getRate(sSymbol, ref dBid, ref dBidVol, ref dAsk, ref dAskVol);
                if (dBid < CFATCommon.ESP || dAsk < CFATCommon.ESP)
                {
                    CFATLogger.output_proc("Deribit_getRates : Error!");
                    return EERROR.RATE_INVALID;
                }

                m_rates[sSymbol].dAsk = dAsk;
                m_rates[sSymbol].dBid = dBid;
                m_rates[sSymbol].m_dtTime = CFATCommon.m_dtCurTime;
            }

            return base.OnTick();
        }

        public override void OnDeInit()
        {
            FrmDeribit.UnSubScribe();
            base.OnDeInit();
        }
    }
}
