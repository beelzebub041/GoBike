using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Threading;

using Service.Interface;
using Connect;
using Tools;

namespace Service.Source
{
    class ControlCenter: IControlCenter
    {
        /// <summary>
        /// ControlCenter 實例
        /// </summary>
        private static ControlCenter instance = null;

        /// <summary>
        /// 版本號
        /// </summary>
        private readonly string version = "Post004";

        /// <summary>
        /// Loger 物件
        /// </summary>
        private Logger logger = null;

        /// <summary>
        /// ClientHandler物件
        /// </summary>
        private ClientHandler clientHandler = null;

        /// <summary>
        /// 建構式
        /// </summary>
        private ControlCenter()
        {
            
        }

        /// <summary>
        /// 解構式
        /// </summary>
        ~ControlCenter()
        {
            
        }

        /// <summary>
        /// 取得 ControlCenter 實例
        /// </summary>
        public static ControlCenter Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new ControlCenter();
                }
                return instance;
            }
        }

        /// <summary>
        /// 初始化
        /// </summary>
        /// <returns> 是否成功初始化 </returns>
        public bool Initialize(Logger logger)
        {
            bool result = false;

            try
            {
                this.logger = logger;

                SaveLog($"[Info] Post Service Version: {version}");

                clientHandler = new ClientHandler("/Post");

                if (MessageProcessor.Instance.Initialize(logger) && clientHandler.Initialize(logger, MessageProcessor.Instance.AddQueue) &&
                    DataBaseConnect.Instance.Initialize(logger) && DataBaseConnect.Instance.Connect() && 
                    RedisConnect.Instance.Initialize(logger) && RedisConnect.Instance.Connect() && GRPCServer.Instance.Initialize(logger))
                {
                    result = true;

                    SaveLog($"[Info] ControlCenter::Initialize, Success");
                }
                else
                {
                    SaveLog($"[Info] ControlCenter::Initialize, Fail");
                }
            }
            catch (Exception ex)
            {
                SaveLog($"[Error] ControlCenter::Initialize, Catch Error, Msg:{ex.Message}");
            }

            return result;
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
