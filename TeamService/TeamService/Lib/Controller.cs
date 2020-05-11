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
using TeamPacket.ServerToClient;


namespace TeamService
{
    class Controller
    {
        private Form1 fm1 = null;

        private Logger log = null;                      // Logger

        private DataBaseConnect dbConnect = null;

        private Server wsServer = null;                 // Web Socket Server

        private string ControllerVersion = "Version002";


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

                log.SaveLog($"Controller Version: {ControllerVersion}");

                wsServer = new Server(log.SaveLog, MessageProcess);

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

        public string MessageProcess(string msg)
        {
            log.SaveLog($"Controller::MessageProcess Msg: {msg}");

            string sReturn = string.Empty;

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
                                case (int)C2S_CmdID.emCreateNewTeam:

                                    CreateNewTeam createMsg = JsonConvert.DeserializeObject<CreateNewTeam>(packetData);

                                    sReturn = OnCreateNewTeam(createMsg);

                                    break;

                                case (int)C2S_CmdID.emUpdateTeamData:
                                    UpdateTeamData dataMsg = JsonConvert.DeserializeObject<UpdateTeamData>(packetData);

                                    sReturn = OnUpdateTeamData(dataMsg);

                                    break;

                                case (int)C2S_CmdID.emChangeLander:
                                    ChangeLander changeMsg = JsonConvert.DeserializeObject<ChangeLander>(packetData);

                                    sReturn = OnChangeLander(changeMsg);

                                    break;

                                case (int)C2S_CmdID.emUpdateViceLeaderList:
                                    UpdateViceLeaderList viceLanderMsg = JsonConvert.DeserializeObject<UpdateViceLeaderList>(packetData);

                                    sReturn = OnUpdateViceLeaderList(viceLanderMsg);

                                    break;

                                case (int)C2S_CmdID.emUpdateTeamMemberList:
                                    UpdateTeamMemberList TeamMemberMsg = JsonConvert.DeserializeObject<UpdateTeamMemberList>(packetData);

                                    sReturn = OnUpdateTeamMemberList(TeamMemberMsg);

                                    break;

                                case (int)C2S_CmdID.emUpdateApplyJoinList:
                                    UpdateApplyJoinList applyMsg = JsonConvert.DeserializeObject<UpdateApplyJoinList>(packetData);

                                    sReturn = OnUpdateApplyJoinList(applyMsg);

                                    break;

                                case (int)C2S_CmdID.emUpdateInviteJoinList:
                                    UpdateInviteJoinList inviteMsg = JsonConvert.DeserializeObject<UpdateInviteJoinList>(packetData);

                                    sReturn = OnUpdateInviteJoinList(inviteMsg);

                                    break;

                                default:
                                    log.SaveLog($"[Warning] Controller::MessageProcess Can't Find CmdID {cmdID}");

                                    break;
                            }

                        }
                        else
                        {
                            log.SaveLog("[Warning] Controller::MessageProcess Can't Find Member \"Data\" ");
                        }

                    }
                    else
                    {
                        log.SaveLog("[Warning] Controller::MessageProcess Can't Find Member \"CmdID\" ");
                    }

                }
                catch (Exception ex)
                {
                    log.SaveLog("[Error] Controller::MessageProcess Process Error Msg:" + ex.Message);
                }
            }
            else
            {
                log.SaveLog("[Warning] Controller::MessageProcess Msg Is Empty");
            }

            return sReturn;
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

        /**
         * 建立新車隊
         */
        private string OnCreateNewTeam(CreateNewTeam packet)
        {
            CreateNewTeamResult rData = new CreateNewTeamResult();

            try
            {
                List<TeamData> TeamList = dbConnect.GetSql().Queryable<TeamData>().Where(it => it.TeamName == packet.TeamName).ToList();
                
                // 未包含該車隊名稱
                if (TeamList.Count() == 0)
                {
                    string dateTime = DateTime.UtcNow.ToString("yyyy-MM-dd hh:mm:ss");

                    string guidAll = Guid.NewGuid().ToString();

                    string[] guidList = guidAll.Split('-');


                    // 建立新帳號
                    TeamData data = new TeamData
                    {
                        TeamID = "DbTeam-" + guidList[0],        // 取GUID前8碼
                        CreateDate = dateTime,
                        Leader = packet.MemberID,
                        TeamViceLeaderIDs = "{\"ViceLeader\":[]}",
                        TeamMemberIDs = "{\"Member\":[]}",
                        TeamName = packet.TeamName,
                        TeamInfo = packet.TeamInfo,
                        Avatar = packet.Avatar,
                        FrontCover = packet.FrontCover,
                        County = packet.County,
                        SearchStatus = packet.SearchStatus,
                        ExamineStatus = packet.ExamineStatus,
                        ApplyJoinList = "{\"Apply\":[]}",
                        InviteJoinList = "{\"Invite\":[]}"
                    };

                    if (dbConnect.GetSql().Insertable(data).ExecuteCommand() > 0) 
                    {
                        rData.Result = 1;

                        rData.TeamID = data.TeamID;
                    }
                    else
                    {
                        rData.Result = 0;

                    }

                }
                else
                {
                    rData.Result = 0;
                }

            }
            catch (Exception ex)
            {
                rData.Result = 0;

                log.SaveLog("[Error] Controller::OnCreateNewTeam Create Error Msg:" + ex.Message);
            }


            JObject jsMain = new JObject();
            jsMain.Add("CmdID", (int)S2C_CmdID.emCreateNewTeamResult);
            jsMain.Add("Data", JsonConvert.DeserializeObject<JObject>(JsonConvert.SerializeObject(rData)));

            return jsMain.ToString();

        }

        /**
         * 新車隊資料
         */
        private string OnUpdateTeamData(UpdateTeamData packet)
        {
            UpdateTeamDataResult rData = new UpdateTeamDataResult();

            try
            {
                List<TeamData> TeamList = dbConnect.GetSql().Queryable<TeamData>().Where(it => it.TeamID == packet.TeamID).ToList();

                // 有找到車隊
                if (TeamList.Count() == 1)
                {
                    TeamList[0].CreateDate = Convert.ToDateTime(TeamList[0].CreateDate).ToString("yyyy-MM-dd hh:mm:ss");

                    TeamList[0].TeamName = packet.TeamName == null ? TeamList[0].TeamName : packet.TeamName;
                    TeamList[0].TeamInfo = packet.TeamInfo == null ? TeamList[0].TeamInfo : packet.TeamInfo;
                    TeamList[0].Avatar = packet.Avatar == null ? TeamList[0].Avatar : packet.Avatar;
                    TeamList[0].FrontCover = packet.FrontCover == null ? TeamList[0].FrontCover : packet.FrontCover;
                    TeamList[0].County = packet.County == 0 ? TeamList[0].County : packet.County;
                    TeamList[0].SearchStatus = packet.SearchStatus == 0 ? TeamList[0].SearchStatus : packet.SearchStatus;
                    TeamList[0].ExamineStatus = packet.ExamineStatus == 0 ? TeamList[0].ExamineStatus : packet.ExamineStatus;

                    if (dbConnect.GetSql().Updateable<TeamData>(TeamList[0]).Where(it => it.TeamID == packet.TeamID).ExecuteCommand() > 0)
                    {
                        rData.Result = 1;

                    }
                    else
                    {
                        rData.Result = 0;
                    }
                }
                else
                {
                    rData.Result = 0;
                }
            }
            catch (Exception ex)
            {
                log.SaveLog("[Error] Controller::OnUpdateTeamData Catch Error, Msg:" + ex.Message);

                rData.Result = 0;
            }

            JObject jsMain = new JObject();
            jsMain.Add("CmdID", (int)S2C_CmdID.emUpdateTeamDataResult);
            jsMain.Add("Data", JsonConvert.DeserializeObject<JObject>(JsonConvert.SerializeObject(rData)));

            return jsMain.ToString();
        }

        /**
         * 更換隊長
         */
        private string OnChangeLander(ChangeLander packet)
        {
            ChangeLanderResult rData = new ChangeLanderResult();

            try
            {
                List<TeamData> TeamList = dbConnect.GetSql().Queryable<TeamData>().Where(it => it.TeamID == packet.TeamID).ToList();

                // 有找到車隊
                if (TeamList.Count() == 1)
                {
                    TeamList[0].Leader = packet.MemberID == null ? TeamList[0].Leader : packet.MemberID;

                    if (dbConnect.GetSql().Updateable<TeamData>(TeamList[0]).Where(it => it.TeamID == packet.TeamID).ExecuteCommand() > 0)
                    {
                        rData.Result = 1;

                    }
                    else
                    {
                        rData.Result = 0;
                    }
                }
                else
                {
                    rData.Result = 0;
                }
            }
            catch (Exception ex)
            {
                log.SaveLog("[Error] Controller::OnChangeLander Catch Error, Msg:" + ex.Message);

                rData.Result = 0;
            }

            JObject jsMain = new JObject();
            jsMain.Add("CmdID", (int)S2C_CmdID.emChangeLanderResult);
            jsMain.Add("Data", JsonConvert.DeserializeObject<JObject>(JsonConvert.SerializeObject(rData)));

            return jsMain.ToString();
        }

        /**
         * 更新副隊長列表
         */
        private string OnUpdateViceLeaderList(UpdateViceLeaderList packet)
        {
            UpdateViceLeaderListResult rData = new UpdateViceLeaderListResult();
            rData.Action = packet.Action;

            try
            {
                List<TeamData> TeamList = dbConnect.GetSql().Queryable<TeamData>().Where(it => it.TeamID == packet.TeamID).ToList();

                // 有找到車隊
                if (TeamList.Count() == 1)
                {
                    JObject jsData = JObject.Parse(TeamList[0].TeamViceLeaderIDs);

                    if (jsData.ContainsKey("ViceLeader"))
                    {
                        JArray jsArray = jsData["ViceLeader"] as JArray;

                        List<string> idList = jsArray.ToObject<List<string>>();

                        // 新增
                        if (packet.Action == 1)
                        {
                            if (!idList.Contains(packet.MemberID))
                            {
                                idList.Add(packet.MemberID);

                                rData.Result = 1;

                            }
                            else
                            {
                                rData.Result = 0;
                            }

                        }
                        // 刪除
                        else if (packet.Action == -1)
                        {
                            if (idList.Contains(packet.MemberID))
                            {
                                idList.Remove(packet.MemberID);

                                rData.Result = 1;
                            }
                            else
                            {
                                rData.Result = 0;
                            }
                        }

                        if (rData.Result == 1)
                        {
                            JObject jsNew = new JObject();
                            jsNew.Add("ViceLeader", JArray.FromObject(idList));

                            if (dbConnect.GetSql().Updateable<TeamData>().SetColumns(it => new TeamData() { TeamViceLeaderIDs = jsNew.ToString() }).Where(it => it.TeamID == packet.TeamID).ExecuteCommand() > 0)
                            {
                                rData.Result = 1;
                            }
                            else 
                            {
                                rData.Result = 0;
                            }
                        }

                    }
                    else
                    {
                        rData.Result = 0;
                    }
                        
                }
                else
                {
                    rData.Result = 0;
                }
            }
            catch (Exception ex)
            {
                log.SaveLog("[Error] Controller::OnUpdateViceLeaderList Catch Error, Msg:" + ex.Message);

                rData.Result = 0;
            }

            JObject jsMain = new JObject();
            jsMain.Add("CmdID", (int)S2C_CmdID.emUpdateViceLeaderListResult);
            jsMain.Add("Data", JsonConvert.DeserializeObject<JObject>(JsonConvert.SerializeObject(rData)));

            return jsMain.ToString();
        }

        /**
         * 更新車隊隊員列表
         */
        private string OnUpdateTeamMemberList(UpdateTeamMemberList packet)
        {
            UpdateTeamMemberListResult rData = new UpdateTeamMemberListResult();
            rData.Action = packet.Action;

            try
            {
                List<TeamData> TeamList = dbConnect.GetSql().Queryable<TeamData>().Where(it => it.TeamID == packet.TeamID).ToList();

                // 有找到車隊
                if (TeamList.Count() == 1)
                {
                    JObject jsData = JObject.Parse(TeamList[0].TeamMemberIDs);

                    if (jsData.ContainsKey("Member"))
                    {
                        JArray jsArray = jsData["Member"] as JArray;

                        List<string> idList = jsArray.ToObject<List<string>>();

                        // 新增
                        if (packet.Action == 1)
                        {
                            if (!idList.Contains(packet.MemberID))
                            {
                                idList.Add(packet.MemberID);

                                rData.Result = 1;

                            }
                            else
                            {
                                rData.Result = 0;
                            }

                        }
                        // 刪除
                        else if (packet.Action == -1)
                        {
                            if (idList.Contains(packet.MemberID))
                            {
                                idList.Remove(packet.MemberID);

                                rData.Result = 1;
                            }
                            else
                            {
                                rData.Result = 0;
                            }
                        }

                        if (rData.Result == 1)
                        {
                            JObject jsNew = new JObject();
                            jsNew.Add("Member", JArray.FromObject(idList));


                            if (dbConnect.GetSql().Updateable<TeamData>().SetColumns(it => new TeamData() { TeamViceLeaderIDs = jsNew.ToString() }).Where(it => it.TeamID == packet.TeamID).ExecuteCommand() > 0)
                            {
                                rData.Result = 1;
                            }
                            else
                            {
                                rData.Result = 0;
                            }
                        }

                    }
                    else
                    {
                        rData.Result = 0;
                    }

                }
                else
                {
                    rData.Result = 0;
                }
            }
            catch (Exception ex)
            {
                log.SaveLog("[Error] Controller::OnUpdateTeamMemberList Catch Error, Msg:" + ex.Message);

                rData.Result = 0;
            }

            JObject jsMain = new JObject();
            jsMain.Add("CmdID", (int)S2C_CmdID.emUpdateTeamMemberListResult);
            jsMain.Add("Data", JsonConvert.DeserializeObject<JObject>(JsonConvert.SerializeObject(rData)));

            return jsMain.ToString();
        }

        /**
         * 更新申請加入車隊列表
         */
        private string OnUpdateApplyJoinList(UpdateApplyJoinList packet)
        {
            UpdateApplyJoinListResult rData = new UpdateApplyJoinListResult();
            rData.Action = packet.Action;

            try
            {
                List<TeamData> TeamList = dbConnect.GetSql().Queryable<TeamData>().Where(it => it.TeamID == packet.TeamID).ToList();

                // 有找到車隊
                if (TeamList.Count() == 1)
                {
                    JObject jsData = JObject.Parse(TeamList[0].ApplyJoinList);

                    if (jsData.ContainsKey("Apply"))
                    {
                        JArray jsArray = jsData["Apply"] as JArray;

                        List<string> idList = jsArray.ToObject<List<string>>();


                        // 新增
                        if (packet.Action == 1)
                        {
                            if (!idList.Contains(packet.MemberID))
                            {
                                idList.Add(packet.MemberID);

                                rData.Result = 1;

                            }
                            else
                            {
                                rData.Result = 0;
                            }

                        }
                        // 刪除
                        else if (packet.Action == -1)
                        {
                            if (idList.Contains(packet.MemberID))
                            {
                                idList.Remove(packet.MemberID);

                                rData.Result = 1;
                            }
                            else
                            {
                                rData.Result = 0;
                            }
                        }

                        if (rData.Result == 1)
                        {
                            JObject jsNew = new JObject();
                            jsNew.Add("Apply", JArray.FromObject(idList));


                            if (dbConnect.GetSql().Updateable<TeamData>().SetColumns(it => new TeamData() { ApplyJoinList = jsNew.ToString() }).Where(it => it.TeamID == packet.TeamID).ExecuteCommand() > 0)
                            {
                                rData.Result = 1;
                            }
                            else
                            {
                                rData.Result = 0;
                            }
                        }

                    }
                    else
                    {
                        rData.Result = 0;
                    }

                }
                else
                {
                    rData.Result = 0;
                }
            }
            catch (Exception ex)
            {
                log.SaveLog("[Error] Controller::OnUpdateApplyJoinList Catch Error, Msg:" + ex.Message);

                rData.Result = 0;
            }

            JObject jsMain = new JObject();
            jsMain.Add("CmdID", (int)S2C_CmdID.emUpdateApplyJoinListResult);
            jsMain.Add("Data", JsonConvert.DeserializeObject<JObject>(JsonConvert.SerializeObject(rData)));

            return jsMain.ToString();
        }

        /**
         * 更新邀請加入車隊列表
         */
        private string OnUpdateInviteJoinList(UpdateInviteJoinList packet)
        {
            UpdateInviteJoinListResult rData = new UpdateInviteJoinListResult();
            rData.Action = packet.Action;

            try
            {
                List<TeamData> TeamList = dbConnect.GetSql().Queryable<TeamData>().Where(it => it.TeamID == packet.TeamID).ToList();

                // 有找到車隊
                if (TeamList.Count() == 1)
                {

                    JObject jsData = JObject.Parse(TeamList[0].InviteJoinList);

                    if (jsData.ContainsKey("Invite"))
                    {
                        JArray jsArray = jsData["Invite"] as JArray;

                        List<string> idList = jsArray.ToObject<List<string>>();

                        // 新增
                        if (packet.Action == 1)
                        {
                            if (!idList.Contains(packet.MemberID))
                            {
                                idList.Add(packet.MemberID);

                                rData.Result = 1;

                            }
                            else
                            {
                                rData.Result = 0;
                            }

                        }
                        // 刪除
                        else if (packet.Action == -1)
                        {
                            if (idList.Contains(packet.MemberID))
                            {
                                idList.Remove(packet.MemberID);

                                rData.Result = 1;
                            }
                            else
                            {
                                rData.Result = 0;
                            }
                        }

                        if (rData.Result == 1)
                        {
                            JObject jsNew = new JObject();
                            jsNew.Add("Invite", JArray.FromObject(idList));


                            if (dbConnect.GetSql().Updateable<TeamData>().SetColumns(it => new TeamData() { InviteJoinList = jsNew.ToString() }).Where(it => it.TeamID == packet.TeamID).ExecuteCommand() > 0)
                            {
                                rData.Result = 1;
                            }
                            else
                            {
                                rData.Result = 0;
                            }
                        }

                    }
                    else
                    {
                        rData.Result = 0;
                    }

                }
                else
                {
                    rData.Result = 0;
                }
            }
            catch (Exception ex)
            {
                log.SaveLog("[Error] Controller::OnUpdateInviteJoinList Catch Error, Msg:" + ex.Message);

                rData.Result = 0;
            }

            JObject jsMain = new JObject();
            jsMain.Add("CmdID", (int)S2C_CmdID.emUpdateInviteJoinListResult);
            jsMain.Add("Data", JsonConvert.DeserializeObject<JObject>(JsonConvert.SerializeObject(rData)));

            return jsMain.ToString();
        }


    }

}
