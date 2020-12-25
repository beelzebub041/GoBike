using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Collections.Concurrent;

using WebSocketSharp;
using WebSocketSharp.Server;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using UserPacket.ClientToServer;
using UserPacket.ServerToClient;
using Tools.DoubleQueue;
using Service.Source.Define.StructClass;

using Tools;

namespace Service.Source
{
    class MessageProcessor
    {
        /// <summary>
        /// MessageProcessor 的實例
        /// </summary>
        private static MessageProcessor instance = null;

        // ==================== Delegate ==================== //

        /// <summary>
        /// MsgFunction的委派函式
        /// </summary>
        /// <param name="msg"> 訊息 </param>
        /// <returns> 回傳訊息 </returns>
        private delegate string MsgFunction(string msg);

        // ============================================ //

        /// <summary>
        /// 訊息鎖
        /// </summary>
        private object msgLock = new object();

        /// <summary>
        /// MsgFunction列表, Key: MsgId, Value: callBack Function
        /// </summary>
        private Dictionary<int, MsgFunction> msgFuncList = null;

        /// <summary>
        /// 訊息函式物件
        /// </summary>
        private MessageFunction msgFunc = null;

        /// <summary>
        /// 訊息Queue
        /// </summary>
        private DoubleQueue<MsgInfo> msgQ = null;

        /// <summary>
        /// Queue用的Timer
        /// </summary>
        private System.Timers.Timer qTimer = null;

        /// <summary>
        /// Logger物件
        /// </summary>
        private Tools.Logger logger = null;

        /// <summary>
        /// 建構式
        /// </summary>
        private MessageProcessor()
        {
            msgFuncList = new Dictionary<int, MsgFunction>();

            msgFunc = new MessageFunction();

            msgQ = new DoubleQueue<MsgInfo>();

            this.qTimer = new System.Timers.Timer();
            this.qTimer.AutoReset = false;
            this.qTimer.Interval = 100;
            this.qTimer.Enabled = false;
            this.qTimer.Elapsed += this.ProcessMsg;

        }

        /// <summary>
        /// 解構式
        /// </summary>
        ~MessageProcessor()
        {

        }

        /// <summary>
        /// 取得 MessageProcessor Instance
        /// </summary>
        public static MessageProcessor Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new MessageProcessor();
                }
                return instance;
            }
        }

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="logger"> Logger物件 </param>
        /// <returns> 是否初始化成功 </returns>
        public bool Initialize(Tools.Logger logger)
        {
            bool ret = true;

            this.logger = logger;

            if (msgFunc.Initialize(logger))
            {
                AddMsgFunc((int)C2S_CmdID.emUserRegistered, msgFunc.OnCreateNewAccount);
                AddMsgFunc((int)C2S_CmdID.emUserLogin, msgFunc.OnUserLogin);
                AddMsgFunc((int)C2S_CmdID.emUpdateUserInfo, msgFunc.OnUpdateUserInfo);
                AddMsgFunc((int)C2S_CmdID.emUpdatePassword, msgFunc.OnUpdatePassword);
                AddMsgFunc((int)C2S_CmdID.emUpdateFriendList, msgFunc.OnUpdateFriendList);
                AddMsgFunc((int)C2S_CmdID.emUpdateBlackList, msgFunc.OnUpdateBlackList);
                AddMsgFunc((int)C2S_CmdID.emUpdateNotifyToken, msgFunc.OnUpdateNotifyToken);

                qTimer.Start();

                ret = true;

                SaveLog("[Info] MessageProcessor::Initialize, Initialize Success");
            }
            else
            {
                SaveLog("[Error] MessageProcessor::Initialize, Initialize Fail");
            }

            return ret;
        }

        /// <summary>
        /// 銷毀
        /// </summary>
        /// <returns> 是否銷毀成功 </returns>
        public bool Destory()
        {
            bool ret = true;

            if (msgFunc != null)
            {
                msgFunc.Destory();
            }

            if (msgFuncList != null)
            {
                msgFuncList.Clear();
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
        /// 註冊Msg Function
        /// </summary>
        /// <param name="msgId"> 訊息編號 </param>
        /// <param name="func"> 對應的函式 </param>
        private void AddMsgFunc(int msgId, MsgFunction func)
        {
            if (!msgFuncList.ContainsKey(msgId))
            {
                msgFuncList.Add(msgId, func);
            }
        }

        /// <summary>
        /// 加入Queue
        /// </summary>
        /// <param name="info"> 訊息資訊 </param>
        public void AddQueue(MsgInfo info)
        {
            msgQ.Enqueue(info);
        }

        /// <summary>
        /// 處理訊息
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ProcessMsg(Object sender, EventArgs e)
        {
            qTimer.Stop();
            
            Queue<MsgInfo> mainQ = msgQ.GetMainQueue();

            while (mainQ.Any())
            {
                MsgInfo info = mainQ.Dequeue();

                info.Send(MessageDispatch(info.msg));
            }

            qTimer.Start();
        }

        /// <summary>
        /// 訊息分派
        /// </summary>
        /// <param name="msgInfo"> 訊息資訊 </param>
        /// <returns> 處理結果 </returns>
        private string MessageDispatch(string msgInfo)
        {
            string ret = string.Empty;

            lock (msgLock)
            {
                SaveLog($"MessageProcessor::MessageDispatch Msg: {msgInfo}");

                if (msgInfo != string.Empty)
                {
                    try
                    {
                        JObject jsMain = JObject.Parse(msgInfo);

                        if (jsMain.ContainsKey("CmdID"))
                        {
                            int cmdID = (int)jsMain["CmdID"];

                            if (jsMain.ContainsKey("Data"))
                            {
                                if (msgFuncList.ContainsKey(cmdID))
                                {
                                    string packetData = jsMain["Data"].ToString();

                                    ret = msgFuncList[cmdID](packetData);
                                }
                                else
                                {
                                    SaveLog($"[Warning] MessageProcessor::MessageDispatch Can't Find Msg Id: {cmdID}");
                                }
                            }
                            else
                            {
                                SaveLog($"[Warning] MessageProcessor::MessageDispatch Can't Find Member \"Data\" ");
                            }

                        }
                        else
                        {
                            SaveLog($"[Warning] MessageProcessor::MessageDispatch Can't Find Member \"CmdID\" ");
                        }

                    }
                    catch (Exception ex)
                    {
                        SaveLog($"[Error] MessageProcessor::MessageDispatch Process Error Msg: {ex.Message}");
                    }

                }
                else
                {
                    SaveLog($"[Warning] MessageProcessor::MessageDispatch Msg Is Empty");
                }
            }

            return ret;
        }

    }

}
