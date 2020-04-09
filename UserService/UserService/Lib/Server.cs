using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

using WebSocketSharp;
using WebSocketSharp.Server;

using Tools.Logger;

namespace UserService
{
    class Server
    {
        // ==================== Delegate ==================== //

        public delegate void LogDelegate(string msg);

        private LogDelegate log = null;

        public delegate string MsgDelegate(string msg);

        private MsgDelegate msg = null;

        // ============================================ //
        [DllImport("kernel32")]
        private static extern int GetPrivateProfileString(string section, string key, string def, StringBuilder retVal, int size, string filePath);

        private WebSocketServer wsServer = null;

        private string serverIp = "";

        private string serverPort = "";

        public Server(LogDelegate log, MsgDelegate msg)
        {
            this.log = log;

            this.msg = msg;
        }

        ~Server()
        {
            Stop();
        }

        /**
         * 初始化
         */
        public bool Initialize()
        {
            bool bReturn = false;

            try
            {
                if (LoadingConfig())
                {
                    wsServer = new WebSocketServer($"ws://{serverIp}:{serverPort}");
                    wsServer.AddWebSocketService("/User", () => new UserHandler(SaveLog, MessageProcess));
                    wsServer.Start();
                    if (wsServer.IsListening)
                    {
                        bReturn = true;

                        log("Create Web Socket Server Success");

                        Console.WriteLine("[Info] Listening on port {0}, and providing WebSocket services:", wsServer.Port);
                        foreach (var path in wsServer.WebSocketServices.Paths)
                            Console.WriteLine("- {0}", path);
                    }
                }
                else
                {
                    log("[Error] Server::Initialize, LoadingConfig Fail");
                }

            }
            catch
            {
                log("[Error] Server::Initialize, Try Catch Errpr");

            }

            return bReturn;
        }

        public bool Stop()
        {
            bool bReturn = false;
            
            if (wsServer != null)
            {
                wsServer.Stop();

                bReturn = true;
            }

            return bReturn;
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

                log("[Error] Server::LoadConfig, Config Parameter Error");
            }

            return bReturn;
        }

        public void SaveLog(string msg)
        {
            this.log(msg);

        }

        public string MessageProcess(string msg)
        {
            return this.msg(msg);
        }

    }


    public class UserHandler : WebSocketBehavior
    {

        public delegate void LogDelegate(string msg);

        private LogDelegate log = null;


        public delegate string MsgDelegate(string msg);

        private MsgDelegate msgProcess = null;

        public UserHandler(LogDelegate log, MsgDelegate msg)
        {
            this.log = log;

            this.msgProcess = msg;
        }

        protected override void OnOpen()
        {
            base.OnOpen();

            log($"Client:{ID} Connect");
            //Console.WriteLine("Client:{0} Connect", ID);

        }

        protected override void OnClose(CloseEventArgs e)
        {
            base.OnClose(e);

            log($"Client:{ID} Connect Close");
            //Console.WriteLine("Connect Close");

        }

        protected override void OnMessage(MessageEventArgs e)
        {
            log($"Receive Client msg: {e.Data}");

            string sendMsg = msgProcess(e.Data);

            Send(sendMsg);

        }
    }

}
