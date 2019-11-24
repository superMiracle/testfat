using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FATsys.Utils;
using FATsys.TraderType;

namespace FATsys.Site
{
    class CReqOrderMng
    {
        private static List<TReqOrder> m_lstReqOrders = new List<TReqOrder>();
        private static TReqOrder m_reqMergeOrder = new TReqOrder();

        public void clearReqOrders()
        {
            m_lstReqOrders.Clear();
        }

        public int registerOrder(TReqOrder reqOrder)
        {
            m_lstReqOrders.Add(reqOrder);
            return 0;
        }

        private void process_MergeOrder(bool bMustExc)
        {
            m_reqMergeOrder.reqOrder(bMustExc);
        }

        private void setResult_Merge2Suborders()
        {
            m_reqMergeOrder.setResult_subReqOrders();
        }

        //Update reqOrders depends on process of first product
        //Modify reqOrders which FilledState is PARTIAL & FAILED
        private void update_reqOrders()
        {
            foreach (TReqOrder reqOrderItem in m_lstReqOrders)
            {
                if (reqOrderItem.m_nOrderResult == EFILLED_STATE.FULL) continue;
                if (reqOrderItem.m_nPriority != 0) continue; // If there is no first product then skip
                modify_reqOrders(reqOrderItem.m_sLogicID, reqOrderItem.m_dLots_req, reqOrderItem.m_dLots_exc);
            }
        }

        private void modify_reqOrders(string sLogicID, double dLots_req, double dLots_exc)
        {
            foreach(TReqOrder reqOrderItem in m_lstReqOrders)
            {
                if (reqOrderItem.m_nPriority == 0) continue; // If there is first product then skip
                if (reqOrderItem.m_sLogicID != sLogicID) continue; // If there is different logicID then skip
                reqOrderItem.m_dLots_req = reqOrderItem.m_dLots_req * dLots_exc / dLots_req; //??? normalize double lots ?
            }
        }

        private void log_reqOrders()
        {
            string sLog = "";
            foreach(TReqOrder reqOrderItem in m_lstReqOrders)
            {
                sLog = string.Format("{0},{1},cmd={2},reqLots={3}, excLots = {4}, processed = {5}, orderResult = {6}", 
                    reqOrderItem.m_sLogicID, reqOrderItem.m_product.m_sSymbol, reqOrderItem.m_nCmd,
                    reqOrderItem.m_dLots_req, reqOrderItem.m_dLots_exc, reqOrderItem.m_bProcessed, reqOrderItem.m_nOrderResult);
                CFATLogger.output_proc(sLog);
            }
        }

        //Here is Main fucntion
        // merge -> sort by priority -> process -> set result
        public void process_ReqOrders()
        {
            if (m_lstReqOrders.Count == 0)
                return ;

//             CFATLogger.output_proc(string.Format("### Requested orders {0} ###>>>", CFATCommon.m_dtCurTime));
//             log_reqOrders();
            //1. merge orders : first product
            bool bRet = merge_reqOrders(0);

            if (!bRet) // There is no merged count 
            {
                m_lstReqOrders.Clear();
                return;
            }
            //2. process order : first product
            process_MergeOrder(false);

            //3. set result
            setResult_Merge2Suborders();

//             CFATLogger.output_proc("   setResult for first product : Requested orders ");
//             log_reqOrders();

            update_reqOrders();

//             CFATLogger.output_proc("   update_reqOrders : Requested orders ");
//             log_reqOrders();

            //4. other product.. such as second, third...
            int nPriority = 1;
            while (true)
            {
                if (!merge_reqOrders(nPriority))
                    break;
                process_MergeOrder(true);
                setResult_Merge2Suborders();
                nPriority++;
            }

//             CFATLogger.output_proc("   process other product : Requested orders ");
//             log_reqOrders();

            m_lstReqOrders.Clear();
            return;
        }

        /// <summary>
        /// Merge reqOrders by product code ( defined by site & symbol)
        /// There is some problems.
        /// 1. IF different to request price on each logic
        /// 2. IF different to order type on each logic
        /// 3. IF different to priority on each logic
        /// </summary>
        /// <param name="nProductCode"></param>
        /// <returns></returns>
        private bool merge_reqOrders(int nPriority)
        {
            double dMergeLots = 0;
            int nMergeCnt = 0;
            m_reqMergeOrder.m_lstSubReqOrders.Clear();
            m_reqMergeOrder.m_product = null;

            foreach (TReqOrder reqOrder in m_lstReqOrders)
            {
                if (reqOrder.m_nPriority != nPriority)
                    continue;

                nMergeCnt++;
                if (m_reqMergeOrder.m_product == null )
                    m_reqMergeOrder.setProduct(reqOrder.m_product.m_sSymbol, reqOrder.m_product.m_site, "REQ_MERGE", reqOrder.m_product.m_dContractSize);

                m_reqMergeOrder.setVal( ETRADER_OP.NONE, 0, reqOrder.m_dPrice_req, 
                    reqOrder.m_nOrderType, reqOrder.m_nPriority, "REQ_MERGE", "");

                if (reqOrder.m_nCmd == ETRADER_OP.BUY || reqOrder.m_nCmd == ETRADER_OP.SELL_CLOSE)
                    dMergeLots += reqOrder.m_dLots_req;
                if (reqOrder.m_nCmd == ETRADER_OP.SELL || reqOrder.m_nCmd == ETRADER_OP.BUY_CLOSE)
                    dMergeLots -= reqOrder.m_dLots_req;
                m_reqMergeOrder.m_lstSubReqOrders.Add(reqOrder);// register sub req orders...
            }

            if (nMergeCnt == 0)
                return false;

            if (Math.Abs(dMergeLots) < CFATCommon.ESP)
            {
                m_reqMergeOrder.m_dLots_exc = 0;
                m_reqMergeOrder.m_dLots_req = 0;
                return true;
            }

            if (dMergeLots > CFATCommon.ESP)
                m_reqMergeOrder.m_nCmd = ETRADER_OP.BUY;

            if (dMergeLots < 0)
                m_reqMergeOrder.m_nCmd = ETRADER_OP.SELL;

            m_reqMergeOrder.m_dLots_req = Math.Abs(dMergeLots);

            return true;
        }
    }
}
