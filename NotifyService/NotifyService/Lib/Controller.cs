using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Windows.Forms;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Tools.Logger;

using DataBaseDef;
using Connect;

using TeamPacket.ClientToServer;
using ClientPacket.ServerToClient;

namespace NotifyService
{
    class Controller
    {
        private Form1 fm1 = null;

        private Logger log = null;                      // Logger

        private DataBaseConnect dbConnect = null;

        private Server wsServer = null;                 // Web Socket Server

        private readonly string version = "Brocast001";


        public Controller(Form1 fm1)
        {
            this.fm1 = fm1;
        }

        ~Controller()
        {
            if (dbConnect != null)
            {
                dbConnect.Disconnect();
            }
        }

        public bool Initialize()
        {
            bool bReturn = false;

            try
            {
                log = new Logger(fm1);

                log.SaveLog($"Controller Version: {version}");

                wsServer = new Server(this);

                dbConnect = new DataBaseConnect(log);

                if (dbConnect.Initialize())
                {
                    if (dbConnect.Connect())
                    {
                        if (wsServer.Initialize())
                        {
                            bReturn = true;
                        }
                    }

                }

                if (!bReturn)
                {
                    log.SaveLog("[Error] Controller::Initialize, Initialize Fail");
                }

            }
            catch (Exception ex)
            {
                log.SaveLog($"[Error] Controller::Initialize, Catch Error, Msg:{ex.Message}");

            }

            return bReturn;
        }

        /**
         * 停止程序
         */
        public bool StopProcess()
        {
            bool bReturn = true;

            wsServer.Stop();

            dbConnect.Disconnect();

            return bReturn;

        }

        // 儲存Log
        public void SaveLog(string log)
        {
            this.log.SaveLog(log);
        }

        /**
         * 取得資料庫連線
         */
        public DataBaseConnect GetDataBase()
        {
            return dbConnect;
        }

        /**
         * Team Sevice 訊息處理
         */
        public void TeamServiceMsgProcess(string msg)
        {
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
                                case (int)TeamPacket.ClientToServer.C2S_CmdID.emUpdateBulletin:

                                    UpdateBulletin bulletinMsg = JsonConvert.DeserializeObject<UpdateBulletin>(packetData);

                                    OnTeamBulletin(bulletinMsg);

                                    break;

                                case (int)TeamPacket.ClientToServer.C2S_CmdID.emUpdateActivity:

                                    UpdateActivity actMsg = JsonConvert.DeserializeObject<UpdateActivity>(packetData);

                                    OnTeamActivity(actMsg);

                                    break;

                                default:
                                    SaveLog($"[Warning] Controller::TeamServiceMsgProcess, Can't Find CmdID {cmdID}");

                                    break;
                            }

                        }
                        else
                        {
                            SaveLog("[Warning] Controller::TeamServiceMsgProcess, Can't Find Member \"Data\" ");
                        }

                    }
                    else
                    {
                        SaveLog("[Warning] Controller::TeamServiceMsgProcess, Can't Find Member \"CmdID\" ");
                    }

                }
                catch (Exception ex)
                {
                    SaveLog("[Error] Controller::TeamServiceMsgProcess, Process Error Msg:" + ex.Message);
                }
            }
            else
            {
                SaveLog("[Warning] Controller::TeamServiceMsgProcess, Msg Is Empty");
            }

        }

        /**
         * 車隊公告
         */
        private void OnTeamBulletin(UpdateBulletin packet)
        {
            NotifyTeamBulletin rData = new NotifyTeamBulletin()
            {
                TeamID = packet.TeamID,
                MemberID = packet.MemberID,
                Content = packet.Content,
                Day = packet.Day
            };

            JObject jsMain = new JObject();
            jsMain.Add("CmdID", (int)ClientPacket.ServerToClient.S2C_CmdID.emNotifyTeamBulletin);
            jsMain.Add("Data", JsonConvert.DeserializeObject<JObject>(JsonConvert.SerializeObject(rData)));

            BrocastMsg(packet.TeamID, jsMain.ToString());

        }

        /**
         * 車隊活動
         */
        private void OnTeamActivity(UpdateActivity packet)
        {
            NotifyTeamActivity rData = new NotifyTeamActivity()
            {
                TeamID = packet.TeamID,
                MemberID = packet.MemberID,
                MemberList = packet.MemberList,
                ActDate = packet.ActDate,
                Title = packet.Title,
                MeetTime = packet.MeetTime,
                TotalDistance = packet.TotalDistance,
                MaxAltitude = packet.MaxAltitude,
                Route = packet.Route,
                Description = packet.Description,
            };

            JObject jsMain = new JObject();
            jsMain.Add("CmdID", (int)ClientPacket.ServerToClient.S2C_CmdID.emNotifyTeamActivity);
            jsMain.Add("Data", JsonConvert.DeserializeObject<JObject>(JsonConvert.SerializeObject(rData)));

            BrocastMsg(packet.TeamID, jsMain.ToString());
        }

        /**
         * 廣播訊息
         */
        private void BrocastMsg(string teamID, string msg)
        {
            // Lock??

            Dictionary<string, Dictionary<string, ClientHandler>> teamList = wsServer.GetTeamMemberList();

            if (teamList.ContainsKey(teamID))
            {
                Dictionary<string, ClientHandler> memberList = teamList[teamID];

                foreach (KeyValuePair<string, ClientHandler> member in memberList)
                {
                    ClientHandler client = member.Value;
                    client.SendMsg(msg);
                }
            }
            else
            {
                SaveLog($"[Warning] Controller::onTeamBulletin, Can Not Find Team:{teamID}");
            }
        }

    }

}
