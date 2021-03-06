﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

using StackExchange.Redis;

using Tools.Logger;


namespace Connect
{
    class RedisConnect
    {
        [DllImport("kernel32")]
        private static extern int GetPrivateProfileString(string section, string key, string def, StringBuilder retVal, int size, string filePath);

        private string ip = "";

        private int port = -1;

        private int dbIdx = 0;

        private IDatabase db = null;

        private Logger log = null;

        public RedisConnect(Logger log)
        {
            this.log = log;
        }

        ~RedisConnect()
        {

        }

        public bool Initialize()
        {
            bool bReturn = false;

            if (LoadConfig())
            {
                bReturn = true;
            }
            else
            {
                log.SaveLog("[Error] RedisConnect::Initialize, LoadConfig Fail");
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
                string configPath = @"./Config/RedisSetting.ini";

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

                // DataBase Index
                if (bReturn && GetPrivateProfileString("CONNECT", "DataBaseIndex", "", temp, 255, configPath) > 0)
                {
                    dbIdx = Convert.ToInt32(temp.ToString());
                }
                else
                {
                    bReturn = false;
                }

            }
            catch
            {
                bReturn = false;

                log.SaveLog("[Error] RedisConnect::LoadConfig, Config Parameter Error");
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
                RedisConnection.Init($"{ip}:{port}");
                var redis = RedisConnection.Instance.ConnectionMultiplexer;
                db = redis.GetDatabase(dbIdx);

                bReturn = true;

                log.SaveLog("Connect Redis Success");
            }
            catch (Exception ex)
            {
                log.SaveLog("[Error] RedisConnect::Connect Connect Redis Catch Error, Msg:" + ex.Message);
            }

            return bReturn;
        }

        /**
         * TODO 關閉連線
         */
        public bool Disconnect()
        {
            bool bReturn = false;

            try
            {
                // 關閉連線

                bReturn = true;

                log.SaveLog("[Info] Disconnect Redis Success");

            }
            catch (Exception ex)
            {
                log.SaveLog("[Error] RedisConnect::Disconnect Disconnect Redis Catch Error, Msg:" + ex.Message);
            }

            return bReturn;
        }

        public IDatabase GetRedis()
        {
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
