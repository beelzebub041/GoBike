using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

using Tools;

using Grpc.Core;
using PostProto;

using Service.Source;

namespace Connect
{
    class GRPCClient
    {
        [DllImport("kernel32")]
        private static extern int GetPrivateProfileString(string section, string key, string def, StringBuilder retVal, int size, string filePath);

        // ===========================================

        /// <summary>
        /// Logger物件
        /// </summary>
        private Tools.Logger logger = null;

        // ===========================================

        /// <summary>
        /// GRPCClient的實例
        /// </summary>
        private static GRPCClient instance = null;

        string channelIP = "";

        int channelPort = -1;

        private Channel grpcChannel = null;

        private Post.PostClient grpcClient = null;

        private GRPCClient()
        {

        }

        ~GRPCClient()
        {
            if (grpcChannel != null)
            {
                grpcChannel.ShutdownAsync().Wait();
            }
        }

        /// <summary>
        /// 取得 GRPCClient的實例
        /// </summary>
        public static GRPCClient Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new GRPCClient();
                }

                return instance;
            }
        }

        /// <summary>
        /// 儲存Log
        /// </summary>
        /// <param name="msg"> 訊息 </param>
        private void SaveLog(string msg)
        {
            if (logger != null)
            {
                logger.AddLog(msg);
            }
        }

        /// <summary>
        /// 初始化
        /// </summary>
        /// <returns> 是否初始化成功</returns>
        public bool Initialize(Tools.Logger logger)
        {
            bool ret = false;

            try
            {
                if (this.LoadConfig())
                {
                    this.logger = logger;

                    ret = true;
                }
                else
                {
                    SaveLog("[Error] GRPCClient::Initialize, Initialize Fail");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{ex.Message}");

                SaveLog($"[Error] GRPCClient::Initialize, Catch Error, Msg:{ex.Message}");
            }

            return ret;
        }

        private bool LoadConfig()
        {
            bool ret = true;

            try
            {
                string configPath = @"./Config/GRPCClient.ini";

                StringBuilder temp = new StringBuilder(255);

                // Channel IP
                if (ret && GetPrivateProfileString("CLIENT", "IP", "", temp, 255, configPath) > 0)
                {
                    channelIP = temp.ToString();
                }
                else
                {
                    ret = false;
                }

                // Channel Port
                if (ret && GetPrivateProfileString("CLIENT", "Port", "", temp, 255, configPath) > 0)
                {
                    channelPort = Convert.ToInt32(temp.ToString());

                }
                else
                {
                    ret = false;
                }

            }
            catch
            {
                ret = false;

                SaveLog("[Error] GRPCClient::LoadConfig, Config Parameter Error");
            }

            return ret;
        }

        public bool CreateClient()
        {
            bool ret = false;

            try
            {
                string channelInfo = $"{channelIP}:{channelPort}";
                
                grpcChannel = new Channel(channelInfo, ChannelCredentials.Insecure);

                grpcClient = new Post.PostClient(grpcChannel);

                ret = true;

            }
            catch (Exception ex)
            {
                Console.WriteLine($"{ex.Message}");

                SaveLog($"[Error] GRPCClient::CreateClient, Catch Error, Msg:{ex.Message}");
            }

            return ret;
        }

        public Post.PostClient GetClient()
        {
            return grpcClient;
        }


    }



}
