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

using TeamPacket.ClientToServer;
using TeamPacket.ServerToClient;

namespace Service.Source
{
    class MessageProcessor
    {
        // ==================== Delegate ==================== //

        public delegate void LogDelegate(string msg);

        private LogDelegate log = null;

        public delegate string MsgFunction(string data);


        // ============================================ //

        private object msgLock = new object();

        private Dictionary<int, MsgFunction> msgFuncList = null;

        private MessageFunction msgFunc = null;

        public MessageProcessor(LogDelegate log)
        {
            this.log = log;

            msgFuncList = new Dictionary<int, MsgFunction>();

            msgFunc = new MessageFunction(SaveLog);

        }

        ~MessageProcessor()
        {

        }

        public void SaveLog(string msg)
        {
            log?.Invoke(msg);
        }

        public bool Initialize()
        {
            bool ret = true;

            if (msgFunc.Initialize())
            {
                AddMsgFunc((int)C2S_CmdID.emCreateNewTeam, msgFunc.OnCreateNewTeam);
                AddMsgFunc((int)C2S_CmdID.emUpdateTeamData, msgFunc.OnUpdateTeamData);
                AddMsgFunc((int)C2S_CmdID.emChangeLander, msgFunc.OnChangeLander);
                AddMsgFunc((int)C2S_CmdID.emUpdateViceLeaderList, msgFunc.OnUpdateViceLeaderList);
                //AddMsgFunc((int)C2S_CmdID.emUpdateTeamMemberList, msgFunc.OnUpdateTeamMemberList); 不使用
                AddMsgFunc((int)C2S_CmdID.emUpdateApplyJoinList, msgFunc.OnUpdateApplyJoinList);
                AddMsgFunc((int)C2S_CmdID.emUpdateBulletin, msgFunc.OnUpdateBulletin);
                AddMsgFunc((int)C2S_CmdID.emUpdateActivity, msgFunc.OnUpdateActivity);
                AddMsgFunc((int)C2S_CmdID.emDeleteTeam, msgFunc.OnDeleteTeam);
                AddMsgFunc((int)C2S_CmdID.emJoinOrLeaveTeamActivity, msgFunc.OnJoinOrLeaveTeamActivity);
                AddMsgFunc((int)C2S_CmdID.emJoinOrLeaveTeam, msgFunc.OnJoinOrLeaveTeam);
                AddMsgFunc((int)C2S_CmdID.emKickTeamMember, msgFunc.OnKickTeamMember);

                SaveLog("[Info] MessageProcessor::Initialize, Initialize Success");

            }
            else
            {
                SaveLog("[Error] MessageProcessor::Initialize, Initialize Fail");
            }

            return ret;
        }

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

        private void AddMsgFunc(int msgId, MsgFunction func)
        {
            if (!msgFuncList.ContainsKey(msgId))
            {
                msgFuncList.Add(msgId, func);
            }
        }

        public string MessageDispatch(string msgInfo)
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
