using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

using StackExchange.Redis;
using System.Reflection;

using Tools;

namespace Connect
{   
    /// <summary>
    /// Redis 資料庫的索引
    /// </summary>
    public enum RedisDB : int
    {
        emRedisDB_RideGroup = 0,
        emRedisDB_1,
        emRedisDB_2,
        emRedisDB_3,
        emRedisDB_4,
        emRedisDB_5,
        emRedisDB_6,
        emRedisDB_User,
        emRedisDB_Ride,
        emRedisDB_Team,
        emRedisDB_10,
        emRedisDB_11,
        emRedisDB_12,
        emRedisDB_13,
        emRedisDB_14,
        emRedisDB_15,
    }
    
    class RedisConnect
    {
        [DllImport("kernel32")]
        private static extern int GetPrivateProfileString(string section, string key, string def, StringBuilder retVal, int size, string filePath);

        // ============================================ //

        /// <summary>
        /// RedisConnect的實例
        /// </summary>
        private static RedisConnect instance = null;

        /// <summary>
        /// 連線IP
        /// </summary>
        private string ip = "";

        /// <summary>
        /// 連線Port
        /// </summary>
        private int port = -1;

        /// <summary>
        /// 
        /// </summary>
        ConnectionMultiplexer redis = null;

        /// <summary>
        /// Logger物件
        /// </summary>
        private Logger logger = null;
        
        /// <summary>
        /// 建構式
        /// </summary>
        public RedisConnect()
        {

        }

        /// <summary>
        /// 解構式
        /// </summary>
        ~RedisConnect()
        {

        }

        /// <summary>
        /// 取得 RedisConnect的實例
        /// </summary>
        public static RedisConnect Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new RedisConnect();
                }

                return instance;
            }
        }

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="logger"> Logger物件 </param>
        /// <returns> 是否成功初始化</returns>
        public bool Initialize(Logger logger)
        {
            bool ret = false;

            if (LoadConfig())
            {
                this.logger = logger;
                
                ret = true;
            }
            else
            {
                SaveLog("[Error] RedisConnect::Initialize, LoadConfig Fail");
            }

            return ret;
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
        /// 讀取Config
        /// </summary>
        /// <returns> 是否成功讀取 </returns>
        private bool LoadConfig()
        {
            bool ret = true;

            try
            {
                string configPath = @"./Config/RedisSetting.ini";

                StringBuilder temp = new StringBuilder(255);

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

            }
            catch
            {
                ret = false;

                SaveLog("[Error] RedisConnect::LoadConfig, Config Parameter Error");
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
                RedisConnection.Init($"{ip}:{port}");
                redis = RedisConnection.Instance.ConnectionMultiplexer;

                ret = true;

                SaveLog($"[Info] Connect Redis Success");
            }
            catch (Exception ex)
            {
                SaveLog($"[Error] RedisConnect::Connect, Connect Redis Fail, Catch Msg: {ex.Message}");
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
                redis.Close();

                ret = true;

                SaveLog("[Info] Disconnect Redis Success");

            }
            catch (Exception ex)
            {
                SaveLog($"[Error] RedisConnect::Disconnect, Disconnect Redis Fsil, Catch Msg: {ex.Message}");
            }

            return ret;
        }

        /// <summary>
        /// 取得 Redis資料庫物件
        /// </summary>
        /// <param name="dbIdx"> 資料庫鎖引</param>
        /// <returns> 資料庫物件 </returns>
        public IDatabase GetRedis(int dbIdx)
        {
            IDatabase db = null;

            try
            {
                if (-1 < dbIdx && dbIdx < 16)
                {
                    if (redis != null)
                    {
                        db = redis.GetDatabase(dbIdx);
                    }
                    else
                    {
                        SaveLog($"[Warning] RedisConnect::GetRedis, Redis Object Is Null");
                    }
                }
                else
                {
                    SaveLog($"[Warning] RedisConnect::GetRedis, dbIdx Error: {dbIdx}");
                }
            }
            catch (Exception ex)
            {
                SaveLog($"[Error] RedisConnect::GetRedis, Catch Msg: {ex.Message}");
            }

            return db;
        }
    }

    /**
     * Redis 連線物件
     * 使用Singleton的design pattern避免一直重複建立連線物件
     */
    public sealed class RedisConnection
    {
        private static Lazy<RedisConnection> lazy = new Lazy<RedisConnection>(() =>
        {
            if (String.IsNullOrEmpty(_settingOption)) throw new InvalidOperationException("Please call Init() first.");
            return new RedisConnection();
        });

        private static string _settingOption;

        public readonly ConnectionMultiplexer ConnectionMultiplexer;

        public static RedisConnection Instance
        {
            get
            {
                return lazy.Value;
            }
        }

        private RedisConnection()
        {
            ConnectionMultiplexer = ConnectionMultiplexer.Connect(_settingOption);
        }

        public static void Init(string settingOption)
        {
            _settingOption = settingOption;
        }

    }

}
