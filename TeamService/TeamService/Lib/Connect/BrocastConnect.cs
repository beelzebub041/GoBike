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
    class BrocastConnect
    {
        [DllImport("kernel32")]
        private static extern int GetPrivateProfileString(string section, string key, string def, StringBuilder retVal, int size, string filePath);

        private string ip = "";

        private int port = -1;

        private WebSocket ws = null;

        private Tools.Logger.Logger log = null;

        public BrocastConnect(Tools.Logger.Logger log)
        {
            this.log = log;
        }

        ~BrocastConnect()
        {

        }
        public bool Initialize()
        {
            bool bReturn = false;

            if (LoadConfig())
            {
                WebSocket ws = new WebSocket($"ws://{ip}:{port}/TeamService");

                ws.Connect();

                bReturn = true;

                log.SaveLog("[Info] BrocastConnect::Initialize, Connect Brocast Service Success");

                bReturn = true;
            }
            else
            {
                log.SaveLog("[Error] BrocastConnect::Initialize, LoadConfig Fail");
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
                string configPath = @"./Config/BrocastSetting.ini";

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

                log.SaveLog("[Error] BrocastConnect::LoadConfig, Config Parameter Error");
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
                // 關閉連線
                ws.Close();

                bReturn = true;

                log.SaveLog("[Info] BrocastConnect::Disconnect, Disconnect Success");

            }
            catch (Exception ex)
            {
                log.SaveLog($"[Error] BrocastConnect::Disconnect,Catch Error, Msg:{ex.Message}");
            }

            return bReturn;
        }

        public WebSocket GetConnect()
        {
            return ws;
        }
    }

}
