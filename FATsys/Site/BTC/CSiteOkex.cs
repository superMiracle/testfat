using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using FATsys.Utils;
using FATsys.TraderType;

using OKEXApi;

namespace FATsys.Site.BTC
{
    class CSiteOkex : CSite
    {

        private OKEXApi.OKEXApi apiOKEX = new OKEXApi.OKEXApi();

        public override bool OnInit()
        {
            apiOKEX.subScrib(m_sSymbols);
            return base.OnInit();
        }

        public override EERROR OnTick()
        {

            double dBid = 0;
            double dAsk = 0;

            foreach (string sSymbol in m_sSymbols)
            {
                apiOKEX.getRates(sSymbol, ref dBid, ref dAsk);
                if (dBid < CFATCommon.ESP || dAsk < CFATCommon.ESP)
                {
                    CFATLogger.output_proc("Okex_getRates : Error!");
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
            apiOKEX.UnsubScrib();
            base.OnDeInit();
        }

    }
}
