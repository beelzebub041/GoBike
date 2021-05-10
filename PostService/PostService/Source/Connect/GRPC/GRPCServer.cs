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
    class GRPCServer
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
        /// GRPCServer的實例
        /// </summary>
        private static GRPCServer instance = null;

        string serverIP = "";

        int serverPort = -1;

        private Server grpcServer = null;

        /// <summary>
        /// GRPC Impl 物件
        /// </summary>
        private GRPCImpl gRpcImpl = null;

        public GRPCServer()
        {

        }

        ~GRPCServer()
        {
            if (grpcServer != null)
            {
                grpcServer.ShutdownAsync().Wait();
            }

        }

        /// <summary>
        /// 取得 GRPCServer的實例
        /// </summary>
        public static GRPCServer Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new GRPCServer();
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
                gRpcImpl = new GRPCImpl();

                if (this.LoadConfig() && gRpcImpl.Initialize(logger, MessageProcessor.Instance.AddQueue))
                {
                    this.logger = logger;

                    ret = CreatServer(gRpcImpl);
                }
                else
                {
                    SaveLog("[Error] GRPCServer::Initialize, Initialize Fail");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{ex.Message}");

                SaveLog($"[Error] GRPCServer::Initialize, Catch Error, Msg:{ex.Message}");
            }

            return ret;
        }

        private bool LoadConfig()
        {
            bool ret = true;

            try
            {
                string configPath = @"./Config/GRPCServer.ini";

                StringBuilder temp = new StringBuilder(255);

                // Server IP
                if (ret && GetPrivateProfileString("SERVER", "IP", "", temp, 255, configPath) > 0)
                {
                    serverIP = temp.ToString();
                }
                else
                {
                    ret = false;
                }

                // Server Port
                if (ret && GetPrivateProfileString("SERVER", "Port", "", temp, 255, configPath) > 0)
                {
                    serverPort = Convert.ToInt32(temp.ToString());
                }
                else
                {
                    ret = false;
                }

            }
            catch
            {
                ret = false;

                SaveLog("[Error] GRPCServer::LoadConfig, Config Parameter Error");
            }

            return ret;
        }

        /// <summary>
        /// 建立GRPC Server
        /// </summary>
        /// <param name="rpcInstance"> 綁定的Service 實例</param>
        /// <returns> 是否建立成功 </returns>
        private bool CreatServer(GRPCImpl impl)
        {
            bool ret = false;

            try
            {
                grpcServer = new Server
                {
                    Services = { Post.BindService(impl) },
                    Ports = { new ServerPort(serverIP, serverPort, ServerCredentials.Insecure) }
                };

                grpcServer.Start();

                ret = true;
            }
            catch (Exception ex)
            { 
                Console.WriteLine($"{ex.Message}");

                SaveLog($"[Error] GRPCServer::CreatServer, Catch Error, Msg:{ex.Message}");
            }

            return ret;
        }
      
    }



}
