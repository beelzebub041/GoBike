using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

using SqlSugar;

using Tools.Logger;

namespace Connect
{
    class DataBaseConnect
    {
        [DllImport("kernel32")]
        private static extern int GetPrivateProfileString(string section, string key, string def, StringBuilder retVal, int size, string filePath);

        private string dataBaseName = "";

        private string ip = "";

        private int port = -1;

        private string account = "";

        private string password = "";

        private SqlSugarClient sql = null;     // My SQL 連線物件

        private Logger log = null;


        public DataBaseConnect(Logger log)
        {
            this.log = log;

        }

        public bool Initialize()
        {
            bool bReturn = false;

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

                bReturn = true;

            }
            else
            {
                log.SaveLog("[Error] DataBaseConnect::Initialize, Initialize Fail");
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
                string configPath = @"./Config/SqlSetting.ini";

                StringBuilder temp = new StringBuilder(255);

                // Data Base Name
                if (GetPrivateProfileString("CONNECT", "DataBaseName", "", temp, 255, configPath) > 0)
                {
                    dataBaseName = temp.ToString();
                }
                else
                {
                    bReturn = false;
                }

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

                // Account
                if (bReturn && GetPrivateProfileString("CONNECT", "Account", "", temp, 255, configPath) > 0)
                {
                    account = temp.ToString();
                }
                else
                {
                    bReturn = false;
                }

                // Password
                if (bReturn && GetPrivateProfileString("CONNECT", "Password", "", temp, 255, configPath) > 0)
                {
                    password = temp.ToString();
                }
                else
                {
                    bReturn = false;
                }

            }
            catch
            {
                bReturn = false;

                log.SaveLog("[Error] DataBaseConnect::LoadConfig, Config Parameter Error");
            }

            return bReturn;
        }

        /**
         * 建立連線
         */
        public bool Connect()
        {
            bool bReturn = false;

            try
            {
                // 建立連線
                sql.Open();

                bReturn = true;

                log.SaveLog("Connect SQL Success");
            }
            catch (Exception ex)
            {
                log.SaveLog("[Error] DataBaseConnect::Connect SQL Connect Fail, Error Msg:" + ex.Message);
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
                sql.Close();

                bReturn = true;

                log.SaveLog("[Info] Disconnect SQL Success");

            }
            catch (Exception ex)
            {
                log.SaveLog("[Error] DataBaseConnect::Disconnect SQL Disconnect Fail, Error Msg:" + ex.Message);
            }

            return bReturn;
        }

        public SqlSugarClient GetSql()
        {
            return sql;
        }

    }

}
