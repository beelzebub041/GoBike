using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Runtime.InteropServices;
using System.IO;

using DataBaseService.Handler;
using DataBaseService.Communication;
using Tools.Logger;

namespace DataBaseService
{
    class DataBaseService
    {
        [DllImport("kernel32")]
        private static extern int GetPrivateProfileString(string section, string key, string def, StringBuilder retVal, int size, string filePath);

        private Logger log = null;              // Logger Class

        private ClientConnect sc = null;        // SocketConnect Class

        private string localIp = "";            // SocketConnect IP

        private int localPort = 0;              // SocketConnect Port

        private DbHandler dbh = null;

        // LoginService 建構式
        public DataBaseService(Logger log)
        {
            this.log = log;
        }

        // LoginService 解構式
        ~DataBaseService()
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

            if (dbh != null)
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
                    log.saveLog("[Info] LoginService::Initialize, Loading Config Success");


                    dbh = new DbHandler();

                    if (dbh.ConnectSQL())
                    {

                        sc = new ClientConnect(localIp, localPort, dbh, log);

                        sc.Start();

                        bReturn = true;
                    }
                    
                }

            }
            else
            {
                log.saveLog("[Info][Error] LoginService::Initialize, Config File not exist");
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

            }
            catch
            {
                bReturn = false;

                log.saveLog("[Info][Error] LoginService::LoadingConfig, Config Parameter error");
            }

            return bReturn;
        }

        // 停止程序
        public bool StopProcess()
        {
            bool bReturn = true;

            sc.Stop();

            dbh.DisconnectSQL();

            return bReturn;

        }
    }
}
