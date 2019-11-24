using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EasyNetQ;

namespace FATsys.Utils
{
    public static class CMQClient
    {
        public static IBus m_bus;
        public static void connectToMQ(string sHost, string sUser, string sPwd)
        {
            CMQClient.m_bus = RabbitHutch.CreateBus(string.Format("host={0};username={1};password={2}", sHost, sUser, sPwd));
            CMQClient.m_bus.Subscribe<string>(CFATCommon.MQ_TOPIC_PARAM_V2C + sUser, processMsg_params_updated, config_logicParam_down);
        }

        public static void config_logicParam_down(EasyNetQ.FluentConfiguration.ISubscriptionConfiguration subConfig)
        {
            subConfig.WithTopic(CFATCommon.MQ_TOPIC_PARAM_V2C + ":" + CFATManager.getSystemName());
            subConfig.WithExpires(1000 * 60 * 10);
            subConfig.WithDurable(false);
        }

        public static void processMsg_params_updated(string sMsg)
        {
            try
            {
                CEvent_ParamChanged.g_bChanged = true;
                CEvent_ParamChanged.g_params.Clear();

                string[] sVals = sMsg.Split('@');
                CEvent_ParamChanged.g_sLogicID = sVals[0];

                string[] sSubVals;
                for ( int i = 1; i < sVals.Length; i ++ )
                {
                    sSubVals = sVals[i].Split(',');
                    if (sSubVals.Length < 2)
                        continue;
                    CEvent_ParamChanged.g_params.Add(sSubVals[0], sSubVals[1]);
                }
            }
            catch
            {
                CFATLogger.output_proc("Error : processMsg_params_updated");
            }
        }

        public static void disConnect()
        {
            try
            {
                m_bus.Dispose();
            }
            catch
            {

            }
        }
        public static void publish_msg(string sMsg, string sTopic)
        {
            try
            {
                CMQClient.m_bus.Publish(sMsg, x => x.WithTopic(sTopic));
            }
            catch
            {
                CFATLogger.output_proc("publish_msg Error !");
            }
        }
    }
}