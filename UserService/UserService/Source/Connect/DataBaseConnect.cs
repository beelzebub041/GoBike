using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

using SqlSugar;

namespace Connect
{
    class DataBaseConnect
    {
        [DllImport("kernel32")]
        private static extern int GetPrivateProfileString(string section, string key, string def, StringBuilder retVal, int size, string filePath);

        // ==================== Delegate ==================== //

        public delegate void LogDelegate(string msg);

        private LogDelegate SaveLog = null;

        // ============================================ //

        private string dataBaseName = "";

        private string ip = "";

        private int port = -1;

        private string account = "";

        private string password = "";

        private SqlSugarClient sql = null;     // My SQL 連線物件


        public DataBaseConnect(LogDelegate log)
        {
            this.SaveLog = log;
        }

        public bool Initialize()
        {
            bool ret = false;

            if (LoadConfig())
            {
                ConnectionConfig conConfig = new ConnectionConfig()
                {
                    ConnectionString = $"server={ip};port={port};user={account};password={password}; database={dataBaseName};",
                    DbType = DbType.MySql,
                    IsAutoCloseConnection = true,
                    InitKeyType = InitKeyType.Attribute
                };


                sql = new SqlSugarClient(conConfig);

                ret = true;

            }
            else
            {
                SaveLog("[Error] DataBaseConnect::Initialize, Initialize Fail");
            }

            return ret;
        }

        /**
         * 讀取Config
         */
        private bool LoadConfig()
        {
            bool ret = true;

            try
            {
                string configPath = @"./Config/SqlSetting.ini";

                StringBuilder temp = new StringBuilder(255);

                // Data Base Name
                if (GetPrivateProfileString("CONNECT", "DataBaseName", "", temp, 255, configPath) > 0)
                {
                    dataBaseName = temp.ToString();
                }
                else
                {
                    ret = false;
                }

                // IP
                if (ret && GetPrivateProfileString("CONNECT", "IP", "", temp, 255, configPath) > 0)
                {
                    ip = temp.ToString();
                }
                else
                {
                    ret = false;
                }

                // Port
                if (ret && GetPrivateProfileString("CONNECT", "Port", "", temp, 255, configPath) > 0)
                {
                    port = Convert.ToInt32(temp.ToString());
                }
                else
                {
                    ret = false;
                }

                // Account
                if (ret && GetPrivateProfileString("CONNECT", "Account", "", temp, 255, configPath) > 0)
                {
                    account = temp.ToString();
                }
                else
                {
                    ret = false;
                }

                // Password
                if (ret && GetPrivateProfileString("CONNECT", "Password", "", temp, 255, configPath) > 0)
                {
                    password = temp.ToString();
                }
                else
                {
                    ret = false;
                }

            }
            catch
            {
                ret = false;

                SaveLog("[Error] DataBaseConnect::LoadConfig, Config Parameter Error");
            }

            return ret;
        }

        /**
         * 建立連線
         */
        public bool Connect()
        {
            bool ret = false;

            try
            {
                // 建立連線
                sql.Open();

                ret = true;

                SaveLog($"[Info] Connect Data Base Success");
            }
            catch (Exception ex)
            {
                SaveLog($"[Error] DataBaseConnect::Connect, Connect Data Base Fail, Catch Msg: {ex.Message}");
            }

            return ret;
        }

        /**
         * 關閉連線
         */
        public bool Disconnect()
        {
            bool ret = false;

            try
            {
                // 關閉連線
                sql.Close();

                ret = true;

                SaveLog($"[Info] Disconnect Data Base Success");

            }
            catch (Exception ex)
            {
                SaveLog($"[Error] DataBaseConnect::Disconnect, Disconnect Data Base Fail, Catch Msg: {ex.Message}");
            }

            return ret;
        }

        public SqlSugarClient GetSql()
        {
            return sql;
        }

    }

}
