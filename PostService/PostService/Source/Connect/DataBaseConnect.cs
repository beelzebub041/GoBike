using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

using SqlSugar;

using Tools;

namespace Connect
{
    class DataBaseConnect
    {
        [DllImport("kernel32")]
        private static extern int GetPrivateProfileString(string section, string key, string def, StringBuilder retVal, int size, string filePath);

        // ============================================ //

        private static DataBaseConnect instance = null;         // DataBaseConnect 實例

        private string dataBaseName = "";                       // 資料庫名稱

        private string ip = "";                                 // 連線IP

        private int port = -1;                                  // 連線Port

        private string account = "";                            // 帳號

        private string password = "";                           // 密碼

        private SqlSugarClient sql = null;                      // My SQL 連線物件

        private Logger logger = null;                           // Logger 物件


        /// <summary>
        /// 建構式
        /// </summary>
        private DataBaseConnect()
        {

        }

        /// <summary>
        /// 取得 DataBaseConnect 實例
        /// </summary>
        public static DataBaseConnect Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new DataBaseConnect(); 
                }

                return instance;
            }
        }

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="logger"> Logger 物件</param>
        /// <returns> 是否成功初始化 </returns>
        public bool Initialize(Logger logger)
        {
            bool ret = false;

            if (LoadConfig())
            {
                this.logger = logger;

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

        /// <summary>
        /// 儲存Log
        /// </summary>
        /// <param name="msg"></param>
        private void SaveLog(string msg)
        {
            if (logger != null)
            {
                logger.AddLog(msg);
            }
        }

        /// <summary>
        /// 讀取Config
        /// </summary>
        /// <returns> 是否讀取成功 </returns>
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

        /// <summary>
        /// 建立連線
        /// </summary>
        /// <returns> 是否成功連線 </returns>
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

        /// <summary>
        /// 關閉連線
        /// </summary>
        /// <returns> 是否成功關閉連線 </returns>
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

        /// <summary>
        /// 取得Sql連線物件
        /// </summary>
        /// <returns></returns>
        public SqlSugarClient GetSql()
        {
            return sql;
        }

    }

}
