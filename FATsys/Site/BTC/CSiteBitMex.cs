using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FATsys.Utils;
using FATsys.TraderType;
using BitMexApiDLL;

namespace FATsys.Site.BTC
{
    class CSiteBitMex : CSite
    {
        public override bool OnInit()
        {
            BitMexAPI.init("https://www.bitmex.com", "bpRUsUmQfm2KqeMthN2qfZzz", "-9WuKoYuvxN9uKH-mAI6zJpztgIWSmx14ATRFdKMs3vNRWnP");
            BitMexAPI.subScribe(m_sSymbols);
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
                BitMexAPI.bitmex_getRates(sSymbol, ref dAsk, ref dAskVol, ref dBid, ref dBidVol);
                if (dBid < CFATCommon.ESP || dAsk < CFATCommon.ESP)
                {
                    CFATLogger.output_proc("BitMex_getRates : Error!");
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
            BitMexAPI.bitmex_logout();
            base.OnDeInit();
        }
    }
}
