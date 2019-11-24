using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FATsys.Utils;
using FATsys.TraderType;

using CNFutureAPI;

namespace FATsys.Site.CN
{
    class CSiteCNFT : CSite
    {
        CCNFtAPI apiCnFt = new CCNFtAPI();
        public override bool OnInit()
        {
            m_sBrokerID = "8000";
            m_sMDAddr = "tcp://180.169.30.170:41213";

            if (!apiCnFt.createMDSpi(m_sBrokerID, m_sMDAddr))
            {
                CFATLogger.output_proc(string.Format("site = {0} : Cannot connect to China Future= {1}", m_sSiteName));
                return false;
            }

            foreach (string sSymbol in m_sSymbols)
            {
                apiCnFt.addSymbol(sSymbol);
            }

            return base.OnInit();
        }

        public override EERROR OnTick()
        {
            double dBid = 0;
            double dAsk = 0;

            foreach (string sSymbol in m_sSymbols)
            {
                apiCnFt.getRates(sSymbol, ref dBid, ref dAsk);
                if (dBid < CFATCommon.ESP || dAsk < CFATCommon.ESP)
                {
                    CFATLogger.output_proc("ChinaFuture_getRates : Error!");
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
            apiCnFt.disConnect();
            base.OnDeInit();
        }
    }
}