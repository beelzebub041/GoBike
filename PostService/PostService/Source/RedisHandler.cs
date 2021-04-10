using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Tools;
using Connect;

namespace Service.Source
{
    class RedisHandler
    {
        private static RedisHandler instance = null;

        /// <summary>
        /// Logger物件
        /// </summary>
        private Tools.Logger logger = null;

        private RedisHandler()
        {

        }

        ~RedisHandler()
        {

        }

        public static RedisHandler Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new RedisHandler();
                }

                return instance;
            }
        }

        public bool Initialize(Logger logger)
        {
            bool ret = false;

            try
            {
                this.logger = logger;

                if (RedisConnect.Instance.Initialize(logger) && RedisConnect.Instance.Connect())
                {
                    ret = true;

                    SaveLog($"[Info] RedisHandler::Initialize, Success");
                }
                else
                {
                    SaveLog($"[Info] RedisHandler::Initialize, Fail");
                }
            }
            catch (Exception ex)
            {
                SaveLog($"[Error] RedisHandler::Initialize, Catch Error, Msg:{ex.Message}");

            }

            return ret;
        }

        /// <summary>
        /// 銷毀
        /// </summary>
        /// <returns> 是否成功銷毀 </returns>
        public bool Destroy()
        {
            bool result = true;

            return result;
        }

        private void SaveLog(string msg)
        {
            if (logger != null)
            {
                logger.AddLog(msg);
            }
        }

    }
}
