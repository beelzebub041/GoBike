using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

using WebSocketSharp;
using WebSocketSharp.Server;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Tools.Logger;
using Connect;

using ClientPacket.ClientToServer;
using ClientPacket.ServerToClient;

using DataBaseDef;

namespace NotifyService
{
    class Server
    {
        // ============================================ //
        [DllImport("kernel32")]
        private static extern int GetPrivateProfileString(string section, string key, string def, StringBuilder retVal, int size, string filePath);

        private WebSocketServer wsServer = null;

        private Controller ctrl = null;

        private string serverIp = "";

        private string serverPort = "";

        private Dictionary<string, Dictionary<string, ClientHandler>> teamMemberList = new Dictionary<string, Dictionary<string, ClientHandler>>();         // Dictionary<TeamID, Dictionary<MemberID, ClientHandler>>

        private Dictionary<string, TeamServiceHandler> teamServiceList = new Dictionary<string, TeamServiceHandler>();         // Dictionary<TeamID, TeamServiceHandler>

        public Server(Controller ctrl)
        {
            this.ctrl = ctrl;
        }

        ~Server()
        {
            Stop();
        }

        /**
         * 初始化
         */
        public bool Initialize()
        {
            bool bReturn = false;

            try
            {
                if (LoadingConfig())
                {
                    wsServer = new WebSocketServer($"ws://{serverIp}:{serverPort}");
                    wsServer.AddWebSocketService("/Brocast", () => new ClientHandler(this));
                    wsServer.AddWebSocketService("/TeamService", () => new TeamServiceHandler(this));

                    wsServer.Start();

                    // 已開啟監聽
                    if (wsServer.IsListening)
                    {
                        bReturn = true;

                        SaveLog("[Info] Server::Initialize, Create Web Socket Server Success");

                        Console.WriteLine("[Info] Listening on port {0}, and providing WebSocket services:", wsServer.Port);
                        foreach (var path in wsServer.WebSocketServices.Paths)
                            Console.WriteLine("- {0}", path);
                    }
                }
                else
                {
                    SaveLog("[Error] Server::Initialize, LoadingConfig Fail");
                }

            }
            catch
            {
                SaveLog("[Error] Server::Initialize, Try Catch Errpr");

            }

            return bReturn;
        }

        public bool Stop()
        {
            bool bReturn = false;
            
            if (wsServer != null)
            {
                wsServer.Stop();

                bReturn = true;
            }

            return bReturn;
        }

        /**
         * 讀取設定檔
         */
        private bool LoadingConfig()
        {
            bool bReturn = true;

            try
            {
                string configPath = @"./Config/ServerSetting.ini";

                StringBuilder temp = new StringBuilder(255);

                // IP
                if (bReturn && GetPrivateProfileString("CONNECT", "IP", "", temp, 255, configPath) > 0)
                {
                    serverIp = temp.ToString();
                }
                else
                {
                    bReturn = false;
                }

                // Port
                if (bReturn && GetPrivateProfileString("CONNECT", "Port", "", temp, 255, configPath) > 0)
                {
                    serverPort = temp.ToString();
                }
                else
                {
                    bReturn = false;
                }
                
            }
            catch
            {
                bReturn = false;

                SaveLog("[Error] Server::LoadConfig, Config Parameter Error");
            }

            return bReturn;
        }

        /**
         * 儲存Log
         */
        public void SaveLog(string msg)
        {
            ctrl.SaveLog(msg);
        }

        /**
         * 取得資料庫連線
         */
        public Controller GetController()
        {
            return ctrl;
        }

        /**
         * 新增成員
         */
        public bool AddMember(string teamID, string memberID, ClientHandler client)
        {
            bool ret = false;
            
            if (teamMemberList.ContainsKey(teamID)) {
                Dictionary<string, ClientHandler> memberList = teamMemberList[teamID];

                if (!memberList.ContainsKey(memberID))
                {
                    memberList.Add(memberID, client);

                    ret = true;
                }
            }
            else
            {
                Dictionary<string, ClientHandler> memberList = new Dictionary<string, ClientHandler>();

                teamMemberList.Add(teamID, memberList);
            }

            return ret;

        }

        /**
         * 移除成員
         */
        public bool RemoveMember(string teamID, string memberID)
        {
            bool ret = false;

            if (teamMemberList.ContainsKey(teamID))
            {
                Dictionary<string, ClientHandler> memberList = teamMemberList[teamID];

                if (memberList.ContainsKey(memberID))
                {
                    memberList.Remove(memberID);

                    ret = true;
                }
            }

            return ret;

        }

        /**
         * 取得車隊成員列表
         */
        public Dictionary<string, Dictionary<string, ClientHandler>> GetTeamMemberList()
        {
            return teamMemberList;
        }

    }


    class ClientHandler : WebSocketBehavior
    {
        private Server sev = null;

        private string teamID = "";

        private string memberID = "";

        public ClientHandler(Server sev)
        {
            this.sev = sev;
        }

        protected override void OnOpen()
        {
            base.OnOpen();

            SaveLog($"[Info] ClientHandler::OnMessage, Client:{ID} Connect");
        }

        protected override void OnClose(CloseEventArgs e)
        {
            base.OnClose(e);

            SaveLog($"[Info] ClientHandler::OnMessage, Client:{ID} Connect Close");

            if (sev.RemoveMember(teamID, memberID))
            {
                SaveLog($"[Info] ClientHandler::OnMessage, Remove Member:{memberID} Success");
            }
            else
            {
                SaveLog($"[Warning] ClientHandler::OnMessage, Add Member:{memberID} Fail");
            }
        }

        protected override void OnMessage(MessageEventArgs e)
        {
            SaveLog($"[Info] ClientHandler::OnMessage, Receive Client Msg: {e.Data}");

            MessageProcess(e.Data);
        }

        /**
         * 送出訊息
         */
        public void SendMsg(string msg)
        {
            try
            {
                Send(msg);
            }
            catch (Exception ex)
            {
                SaveLog($"[Warning] ClientHandler::SendMsg Catch Error:{ex.Data}");
            }
        }

        /**
         * 紀錄Log
         */
        private void SaveLog(string log)
        {
            sev.SaveLog(log);
        }

        /**
         * 訊息分派
         */
        public void MessageProcess(string msg)
        {
            // TODO Lock ??

            if (msg != string.Empty)
            {
                try
                {
                    JObject jsMain = JObject.Parse(msg);

                    if (jsMain.ContainsKey("CmdID"))
                    {
                        int cmdID = (int)jsMain["CmdID"];

                        if (jsMain.ContainsKey("Data"))
                        {
                            string packetData = jsMain["Data"].ToString();

                            switch (cmdID)
                            {
                                case (int)C2S_CmdID.emJoinService:

                                    JoinService joinMsg = JsonConvert.DeserializeObject<JoinService>(packetData);

                                    OnJoinService(joinMsg);

                                    break;

                                

                                default:
                                    SaveLog($"[Warning] ClientHandler::MessageProcess Can't Find CmdID {cmdID}");

                                    break;
                            }

                        }
                        else
                        {
                            SaveLog("[Warning] ClientHandler::MessageProcess Can't Find Member \"Data\" ");
                        }

                    }
                    else
                    {
                        SaveLog("[Warning] ClientHandler::MessageProcess Can't Find Member \"CmdID\" ");
                    }

                }
                catch (Exception ex)
                {
                    SaveLog("[Error] ClientHandler::MessageProcess Process Error Msg:" + ex.Message);
                }
            }
            else
            {
                SaveLog("[Warning] ClientHandler::MessageProcess Msg Is Empty");
            }

        }

        /**
         * 使用者加入
         */
        private void OnJoinService(JoinService packet)
        {
            JoinServiceResult rData = new JoinServiceResult();
            rData.Result = 0;

            DataBaseConnect dbConnect = sev.GetController().GetDataBase();

            try
            {
                List<UserAccount> accountList = dbConnect.GetSql().Queryable<UserAccount>().Where(it => it.Email == packet.Email && it.Password == packet.Password).ToList();

                // 有找到帳號
                if (accountList.Count() == 1)
                {
                    if (accountList[0].Password == packet.Password)
                    {
                        List<UserInfo> infoList = dbConnect.GetSql().Queryable<UserInfo>().Where(it => it.MemberID == accountList[0].MemberID).ToList();

                        // 有找到會員
                        if (infoList.Count() == 1)
                        {
                            if (sev.AddMember(teamID, memberID, this))
                            {
                                rData.Result = 1;

                                SaveLog($"[Info] ClientHandler::onUserJoin, Add Member:{memberID} Success");
                            }
                            else
                            {
                                SaveLog($"[Warning] ClientHandler::onUserJoin, Add Member:{memberID} Fail");
                            }
                        }
                        else
                        {
                            SaveLog("[Warning] ClientHandler::onUserJoin, Can't Find User Info");

                            rData.Result = 0;
                        }
                    }
                    else
                    {
                        rData.Result = 3;

                        SaveLog("[Info] ClientHandler::onUserJoin, Can't Password Error");
                    }
                }
                else
                {
                    rData.Result = 2;
                }
            }
            catch (Exception ex)
            {
                SaveLog($"[Error] ClientHandler::onUserJoin, Catch Error, Msg:{ex.Message}");
            }

            JObject jsMain = new JObject();
            jsMain.Add("CmdID", (int)S2C_CmdID.emJoinServiceResult);
            jsMain.Add("Data", JsonConvert.DeserializeObject<JObject>(JsonConvert.SerializeObject(rData)));

            Send(jsMain.ToString());

            // 加入失敗
            if (rData.Result != 1)
            {
                //SaveLog($"[Warning] ClientHandler::onUserJoin, Can Not Find Member");

                // 強制斷線
            }
        }


    }

    class TeamServiceHandler : WebSocketBehavior
    {
        private Server sev = null;

        public TeamServiceHandler(Server sev)
        {
            this.sev = sev;
        }

        protected override void OnOpen()
        {
            base.OnOpen();

            SaveLog($"[Info] TeamServiceHandler::OnMessage, TeamService:{ID} Connect");
        }

        protected override void OnClose(CloseEventArgs e)
        {
            base.OnClose(e);

            SaveLog($"[Info] TeamServiceHandler::OnMessage, TeamService:{ID} Connect Close");

        }

        protected override void OnMessage(MessageEventArgs e)
        {
            SaveLog($"[Info] TeamServiceHandler::OnMessage, Receive Team Service Msg: {e.Data}");

            sev.GetController().TeamServiceMsgProcess(e.Data);
        }

        /**
         * 紀錄Log
         */
        private void SaveLog(string log)
        {
            sev.SaveLog(log);
        }
    }

}
