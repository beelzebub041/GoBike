using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

using WebSocketSharp;

using Tools.Logger;

namespace Connect
{
    class NotifyConnect
    {
        [DllImport("kernel32")]
        private static extern int GetPrivateProfileString(string section, string key, string def, StringBuilder retVal, int size, string filePath);

        private string ip = "";

        private int port = -1;

        private WebSocket ws = null;

        private Tools.Logger.Logger log = null;

        public NotifyConnect(Tools.Logger.Logger log)
        {
            this.log = log;
        }

        ~NotifyConnect()
        {

        }
        public bool Initialize()
        {
            bool bReturn = false;

            if (LoadConfig())
            {
                ws = new WebSocket($"ws://{ip}:{port}/TeamService");

                bReturn = true;

                log.SaveLog("[Info] NotifyConnect::Initialize, Connect Notify Service Success");
            }
            else
            {
                log.SaveLog("[Error] NotifyConnect::Initialize, LoadConfig Fail");
            }

            return bReturn;
        }

        /**
         * 讀取Config
         */
        private bool LoadConfig()
        {
            bool bReturn = true;

            try
            {
                string configPath = @"./Config/NotifySetting.ini";

                StringBuilder temp = new StringBuilder(255);

                // IP
                if (bReturn && GetPrivateProfileString("CONNECT", "IP", "", temp, 255, configPath) > 0)
                {
                    ip = temp.ToString();
                }
                else
                {
                    bReturn = false;
                }

                // Port
                if (bReturn && GetPrivateProfileString("CONNECT", "Port", "", temp, 255, configPath) > 0)
                {
                    port = Convert.ToInt32(temp.ToString());
                }
                else
                {
                    bReturn = false;
                }

            }
            catch
            {
                bReturn = false;

                log.SaveLog("[Error] NotifyConnect::LoadConfig, Config Parameter Error");
            }

            return bReturn;
        }

        /**
         * 開啟連線
         */
        public bool Connect()
        {
            bool bReturn = false;

            try
            {
                if (ws != null)
                {
                    ws.Connect();

                    if (ws.IsAlive)
                    {
                        bReturn = true;

                        log.SaveLog("[Info] NotifyConnect::Connect, Connect Success");
                    }
                    else
                    {
                        log.SaveLog($"[Error] NotifyConnect::Connect, Connect Fail");
                    }
                }
                else
                {
                    log.SaveLog($"[Error] NotifyConnect::Connect, Web Socket Object Is Null");
                }

            }
            catch (Exception ex)
            {
                log.SaveLog($"[Error] NotifyConnect::Connect,Catch Error, Msg:{ex.Message}");
            }

            return bReturn;
        }

        /**
         * 關閉連線
         */
        public bool Disconnect()
        {
            bool bReturn = false;

            try
            {
                if (ws != null)
                {
                    // 關閉連線
                    ws.Close();

                    bReturn = true;

                    log.SaveLog("[Info] NotifyConnect::Disconnect, Disconnect Success");
                }
                else
                {
                    log.SaveLog($"[Error] NotifyConnect::Disconnect, Web Socket Object Is Null");
                }

            }
            catch (Exception ex)
            {
                log.SaveLog($"[Error] NotifyConnect::Disconnect,Catch Error, Msg:{ex.Message}");
            }

            return bReturn;
        }

        public WebSocket GetConnect()
        {
            return ws;
        }
    }

}
