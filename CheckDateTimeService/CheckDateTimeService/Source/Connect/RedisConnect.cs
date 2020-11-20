using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

using StackExchange.Redis;
using System.Reflection;


namespace Connect
{
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

        // ==================== Delegate ==================== //

        public delegate void LogDelegate(string msg);

        private LogDelegate SaveLog = null;

        // ============================================ //

        private string ip = "";

        private int port = -1;

        ConnectionMultiplexer redis = null;

        public RedisConnect(LogDelegate log)
        {
            this.SaveLog = log;
        }

        ~RedisConnect()
        {

        }

        public bool Initialize()
        {
            bool ret = false;

            if (LoadConfig())
            {
                ret = true;
            }
            else
            {
                SaveLog("[Error] RedisConnect::Initialize, LoadConfig Fail");
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

        /**
         * 建立連線
         */
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

        /**
         * TODO 關閉連線
         */
        public bool Disconnect()
        {
            bool ret = false;

            try
            {
                // 關閉連線

                ret = true;

                SaveLog("[Info] Disconnect Redis Success");

            }
            catch (Exception ex)
            {
                SaveLog($"[Error] RedisConnect::Disconnect, Disconnect Redis Fsil, Catch Msg: {ex.Message}");
            }

            return ret;
        }

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
