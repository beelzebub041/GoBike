using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Runtime.InteropServices;
using System.IO;

using AccountService.Communication;
using Tools.Logger;

namespace AccountService
{
    class AccountService
    {
        [DllImport("kernel32")]
        private static extern int GetPrivateProfileString(string section, string key, string def, StringBuilder retVal, int size, string filePath);

        private Logger log = null;                          // Logger Class

        private ClientConnect sc = null;                    // SocketConnect Class

        private string localIp = "";                        // SocketConnect IP

        private int localPort = 0;                          // SocketConnect Port

        private DataBaseServiceConnect dbsc = null;         // DataBaseServiceConnect Class

        private string dbscIp = "";                         // DataBaseServiceConnect IP

        private int dbscPort = 0;                           // DataBaseServiceConnect Port

        private List<string> C2S_PacketList;                // Client Packet List



        // LoginService 建構式
        public AccountService()
        {
            log = new Logger();

        }

        // LoginService 解構式
        ~AccountService()
        {
            StopProcess();

            if (log != null)
            {
                // TODO 刪除物件
            }

            if (sc != null)
            {
                // TODO 刪除物件
            }

            if (dbsc != null)
            {
                // TODO 刪除物件
            }
        }

        // 初始化
        public bool Initialize()
        {
            bool bReturn = false;

            // 資料夾存在 與 檔案存在
            if (Directory.Exists(@"./Config") && File.Exists(@"./Config/setting.ini"))
            {   
                // 讀取設定檔
                if (LoadingConfig())
                {
                    log.saveLog("[Info] AccountService::Initialize, Loading Config Success");

                    // 建立Server Socket物件
                    sc = new ClientConnect(localIp, localPort, this);

                    sc.Start();

                    // 建立DataBaseService連線物件
                    dbsc = new DataBaseServiceConnect();

                    dbsc.Connect(dbscIp, dbscPort, this);

                    // 註冊訊息
                    AddMessage("C2S_UserRegistered");
                    AddMessage("C2S_UserLogin");
                    AddMessage("C2S_UserLogout");


                    bReturn = true;
                }

            }
            else
            {
                log.saveLog("[Info][Error] AccountService::Initialize, Config File not exist");
            }

            return bReturn;
        }

        // 讀取設定檔
        private bool LoadingConfig()
        {
            bool bReturn = true;

            try
            {
                string configPath = @"./Config/setting.ini";

                StringBuilder temp = new StringBuilder(255);

                if (GetPrivateProfileString("Local", "Ip", "", temp, 255, configPath) > 0)
                {
                    localIp = temp.ToString();
                }
                else
                {
                    bReturn = false;
                }

                if (bReturn && GetPrivateProfileString("Local", "Port", "", temp, 255, configPath) > 0)
                {
                    localPort = Convert.ToInt32(temp.ToString());
                }
                else
                {
                    bReturn = false;
                }

                if (bReturn && GetPrivateProfileString("DbHandler", "Ip", "", temp, 255, configPath) > 0)
                {
                    dbscIp = temp.ToString();
                }
                else
                {
                    bReturn = false;
                }

                if (bReturn && GetPrivateProfileString("DbHandler", "Port", "", temp, 255, configPath) > 0)
                {
                    dbscPort = Convert.ToInt32(temp.ToString());
                }
                else
                {
                    bReturn = false;
                }

            }
            catch
            {
                bReturn = false;

                log.saveLog("[Info][Error] AccountService::LoadingConfig, Config Parameter error");
            }

            return bReturn;
        }

        // 停止程序
        public bool StopProcess()
        {
            bool bReturn = true;

            sc.Stop();

            dbsc.Disconnect();

            return bReturn;

        }

        // 註冊訊息
        private bool AddMessage(string name)
        {
            bool bReturn = false;

            //if (!C2S_PacketList.Contains(name))
            //{
            //    C2S_PacketList.Add(name);

            //    bReturn = true;
            //}

            return bReturn;
        }

        // 分派訊息
        public void DispatchMessage(string type, string msg)
        {
            if (type == "ToDataBaseService")
            {
                dbsc.SendMsg(msg);

            }
            else if (type == "ToClient")
            {
                sc.SendMsg(msg);
            }
            
        }
    }
}
