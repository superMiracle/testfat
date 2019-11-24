using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

using BitFlyerAPI;
using FATsys.Utils;
using FATsys.TraderType;


namespace FATsys.Site.BTC
{
    class CSiteBitFlyer : CSite
    {
        CBitFlyerAPI apiBitFlyer = new CBitFlyerAPI();
        public override bool OnInit()
        {
            apiBitFlyer.createBitFlyerAPI("", "");
            foreach (string sSymbol in m_sSymbols)
            {
                apiBitFlyer.subScribeTick(sSymbol);
            }

            //normally Ontick event will occurs after 2 seconds.
            for (int i = 0; i < 500; i ++ )
            {
                System.Windows.Forms.Application.DoEvents();
                Thread.Sleep(10);
            }

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
                apiBitFlyer.getRate(sSymbol, ref dBid, ref dBidVol, ref dAsk, ref dAskVol);
                if (dBid < CFATCommon.ESP || dAsk < CFATCommon.ESP)
                {
                    CFATLogger.output_proc("BitFlyer_getRates : Error!");
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
            //Logout 
            apiBitFlyer.disConnect();
            base.OnDeInit();
        }

    }
}