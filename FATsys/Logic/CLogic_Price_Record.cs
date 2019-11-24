using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FATsys.Utils;
using FATsys.TraderType;
using FATsys.Product;

namespace FATsys.Logic
{
    class CLogic_Price_Record : CLogic
    {
        private string ex_sLogFolder = "default";

        private string m_sPrevVal = "";
        public override void loadParams()
        {
            ex_sLogFolder = m_params.getVal_string("ex_sLogFolder");
            base.loadParams();
        }
        public override bool OnInit()
        {
            CFATLogger.output_proc("Price_record init :");
            loadParams();
            CFATLogger.output_proc("Price_record param done! :");
            return base.OnInit();
        }

        public override void OnDeInit()
        {
            base.OnDeInit();
        }
        public override int OnTick()
        {
            TRatesTick tick_cur;
            
            string sRates = CFATCommon.m_dtCurTime.ToString("yyyy/MM/dd HH:mm:ss.fff");
            string sVal = "";
            foreach (CProduct product in m_products)
            {
                tick_cur = product.getTick(0);
                
                if (tick_cur.dAsk < CFATCommon.ESP) return base.OnTick();
                if (tick_cur.dBid < CFATCommon.ESP) return base.OnTick();
                
                
                //For BTC
                //                 sRates += string.Format(",{0:0.0},{1:0.0}", tick_cur.dAsk, tick_cur.dBid);
                //                 sVal += string.Format(",{0:0.0},{1:0.0}", tick_cur.dAsk, tick_cur.dBid);

                sRates += string.Format(",{0},{1}", tick_cur.dAsk, tick_cur.dBid);
                sVal += string.Format(",{0},{1}", tick_cur.dAsk, tick_cur.dBid);
                
            }
            
            if (m_sPrevVal != sVal)
            {
                CFATLogger.record_rates(ex_sLogFolder, sRates);
                m_sPrevVal = sVal;
            }
            return base.OnTick();
        }

    }
}
