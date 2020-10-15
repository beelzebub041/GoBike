using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

using WebSocketSharp;
using WebSocketSharp.Server;
using System.Collections.Concurrent;

//using Tools.Logger;

namespace Service.Source
{



    class ClientHandler
    {
        // ==================== Delegate ==================== //

        public delegate void LogDelegate(string msg);

        private LogDelegate log = null;

        public delegate string MsgDelegate(string data);

        private MsgDelegate msgProcessor = null;

        // ============================================ //
        [DllImport("kernel32")]
        private static extern int GetPrivateProfileString(string section, string key, string def, StringBuilder retVal, int size, string filePath);

        private WebSocketServer wsServer = null;

        private string serviceName = "/Ride";

        private string serverIp = "";

        private string serverPort = "";

        public ClientHandler(LogDelegate log, MsgDelegate processor)
        {
            this.log = log;

            this.msgProcessor = processor;
        }

        ~ClientHandler()
        {
            Destroy();
        }

        /**
         * 初始化
         */
        public bool Initialize()
        {
            bool ret = false;

            try
            {
                if (LoadingConfig())
                {
                    wsServer = new WebSocketServer($"ws://{serverIp}:{serverPort}");
                    wsServer.AddWebSocketService(serviceName, () => new Handler(SaveLog, MsgDispatch));

                    wsServer.Start();
                    if (wsServer.IsListening)
                    {
                        ret = true;

                        SaveLog($"[Info] Create Client Handler Success");

                        Console.WriteLine("[Info] Listening on port {0}, and providing WebSocket services:", wsServer.Port);
                        foreach (var path in wsServer.WebSocketServices.Paths)
                            Console.WriteLine("- {0}", path);
                    }
                }
                else
                {
                    SaveLog($"[Error] Create Client Handler Fail");
                }

            }
            catch (Exception ex)
            {
                SaveLog($"[Error] ClientHandler::Initialize, Catch Msg: {ex.Message}");

            }

            return ret;
        }

        public bool Destroy()
        {
            bool ret = false;
            
            if (wsServer != null)
            {
                wsServer.Stop();

                ret = true;
            }

            return ret;
        }

        /**
         * 讀取設定檔
         */
        private bool LoadingConfig()
        {
            bool bReturn = true;

            try
            {
                string configPath = @"./Config/ServerSetting.ini";

                StringBuilder temp = new StringBuilder(255);

                // IP
                if (bReturn && GetPrivateProfileString("CONNECT", "IP", "", temp, 255, configPath) > 0)
                {
                    serverIp = temp.ToString();
                }
                else
                {
                    bReturn = false;
                }

                // Port
                if (bReturn && GetPrivateProfileString("CONNECT", "Port", "", temp, 255, configPath) > 0)
                {
                    serverPort = temp.ToString();
                }
                else
                {
                    bReturn = false;
                }
                
            }
            catch
            {
                bReturn = false;

                SaveLog("[Error] Server::LoadConfig, Config Parameter Error");
            }

            return bReturn;
        }

        public void SaveLog(string msg)
        {
            log?.Invoke(msg);
        }

        public string MsgDispatch(string msg)
        {
            return msgProcessor?.Invoke(msg);
        }

    }

    public class Handler : WebSocketBehavior
    {

        public delegate void LogDelegate(string msg);

        private LogDelegate SaveLog = null;

        public delegate string MsgDelegate(string data);

        private MsgDelegate MsgDispatch = null;

        public Handler(LogDelegate log, MsgDelegate MsgDispatch)
        {
            this.SaveLog = log;

            this.MsgDispatch = MsgDispatch;
        }

        protected override void OnOpen()
        {
            base.OnOpen();

            SaveLog($"Client:{ID} Connect");
        }

        protected override void OnClose(CloseEventArgs e)
        {
            base.OnClose(e);

            SaveLog($"Client:{ID} Connect Close");
        }

        protected override void OnMessage(MessageEventArgs e)
        {
            SaveLog($"Receive Client msg: {e.Data}");

            SendToClient(MsgDispatch(e.Data));
        }

        public void SendToClient(string msg)
        {
            try
            {
                Send(msg);

                SaveLog($"Send msg: {msg}");
            }
            catch
            {

            }
        }

    }

}
