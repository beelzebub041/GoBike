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

        private BrocastConnect bsConnect = null;        // Brocast Service Connect

        private string version = "Team009";


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

            if (wsServer != null)
            {
                wsServer.Stop();
            }

            if (bsConnect != null)
            {
                bsConnect.Disconnect();
            }
        }

        public bool Initialize()
        {
            bool bReturn = false;

            try
            {
                log = new Logger(fm1);

                log.SaveLog($"Controller Version: {version}");

                wsServer = new Server(log.SaveLog, MessageProcess);

                bsConnect = new BrocastConnect(log);

                dbConnect = new DataBaseConnect(log);

                if (dbConnect.Initialize())
                {
                    if (dbConnect.Connect())
                    {
                        if (wsServer.Initialize())
                        {
                            //if (bsConnect.Initialize())
                            {
                                bReturn = true;
                            }
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

                                case (int)C2S_CmdID.emUpdateBulletin:
                                    UpdateBulletin bulletinMsg = JsonConvert.DeserializeObject<UpdateBulletin>(packetData);

                                    sReturn = OnUpdateBulletin(bulletinMsg);

                                    break;

                                case (int)C2S_CmdID.emUpdateActivity:
                                    UpdateActivity actMsg = JsonConvert.DeserializeObject<UpdateActivity>(packetData);

                                    sReturn = OnUpdateActivity(actMsg);

                                    break;

                                case (int)C2S_CmdID.emDeleteTeam:
                                    DeleteTeam delMsg = JsonConvert.DeserializeObject<DeleteTeam>(packetData);

                                    sReturn = OnDeleteTeam(delMsg);

                                    break;

                                case (int)C2S_CmdID.emJoinOrLeaveTeamActivity:
                                    JoinOrLeaveTeamActivity joinOrLeaveActMsg = JsonConvert.DeserializeObject<JoinOrLeaveTeamActivity>(packetData);

                                    sReturn = OnJoinOrLeaveTeamActivity(joinOrLeaveActMsg);

                                    break;

                                case (int)C2S_CmdID.emJoinOrLeaveTeam:
                                    JoinOrLeaveTeam joinOrLeaveMsg = JsonConvert.DeserializeObject<JoinOrLeaveTeam>(packetData);

                                    sReturn = OnJoinOrLeaveTeam(joinOrLeaveMsg);

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
         * 更新使用者車隊資訊
         */
        private void UpdateUserTeamList(string memberID, string teamID, int action)
        {
            // 更新UserInfo的車隊資料
            List<UserInfo> userInfo = dbConnect.GetSql().Queryable<UserInfo>().Where(it => it.MemberID == memberID).ToList();

            // 有找到會員
            if (userInfo.Count() == 1)
            {

                JArray jsData = JArray.Parse(userInfo[0].TeamList);

                List<string> idList = jsData.ToObject<List<string>>();

                bool success = false;

                // 新增
                if (action == 1)
                {
                    if (!idList.Contains(teamID))
                    {
                        idList.Add(teamID);

                        success = true;

                        log.SaveLog($"[Info] Controller::UpdateUserTeamList, Add User:{memberID} Team List Success");
                    }
                    else
                    {
                        log.SaveLog($"[Warning] Controller::UpdateUserTeamList, Add User:{memberID} Team List Fail, User Repeat");
                    }
                }
                // 刪除
                else if (action == -1)
                {
                    if (idList.Contains(teamID))
                    {
                        idList.Remove(teamID);

                        success = true;

                        log.SaveLog($"[Info] Controller::UpdateUserTeamList, Remove User:{memberID} Team List Success");
                    }
                    else
                    {
                        log.SaveLog($"[Warning] Controller::UpdateUserTeamList, Remove User:{memberID} Team List Fail, Can Not Find User");
                    }
                }

                if (success)
                {
                    JArray jsNew = JArray.FromObject(idList);

                    if (dbConnect.GetSql().Updateable<UserInfo>().SetColumns(it => new UserInfo() { TeamList = jsNew.ToString() }).Where(it => it.MemberID == memberID).ExecuteCommand() > 0)
                    {
                        log.SaveLog($"[Warning] Controller::UpdateUserTeamList, Update User:{memberID} Team List Success");
                    }
                    else
                    {
                        log.SaveLog($"[Warning] Controller::UpdateUserTeamList, Update User:{memberID} Team List Fail");
                    }
                }

            }
            else
            {
                log.SaveLog($"[Warning] Controller::UpdateUserTeamList, Can Not Find User:{memberID}");
            }
        }

       /**
        * 建立新車隊
        */
        private string OnCreateNewTeam(CreateNewTeam packet)
        {
            CreateNewTeamResult rData = new CreateNewTeamResult();

            try
            {
                List<TeamData> TeamList = dbConnect.GetSql().Queryable<TeamData>().Where(it => it.TeamName == packet.TeamName ).ToList();

                List<TeamData> sameLeaderList = dbConnect.GetSql().Queryable<TeamData>().Where(it => it.Leader == packet.MemberID).ToList();

                // 未包含該車隊名稱
                if (TeamList.Count() == 0)
                {
                    // 非其他車隊的隊長
                    if (sameLeaderList.Count() == 0)
                    {
                        string dateTime = DateTime.UtcNow.ToString("yyyy-MM-dd hh:mm:ss");

                        string guidAll = Guid.NewGuid().ToString();

                        string[] guidList = guidAll.Split('-');

                        // 建立新車隊
                        TeamData data = new TeamData
                        {
                            TeamID = "DbTeam-" + guidList[0],        // 取GUID前8碼
                            CreateDate = dateTime,
                            Leader = packet.MemberID,
                            TeamViceLeaderIDs = "[]",
                            TeamMemberIDs = "[]",
                            TeamName = packet.TeamName,
                            TeamInfo = packet.TeamInfo,
                            Avatar = packet.Avatar,
                            FrontCover = packet.FrontCover,
                            County = packet.County,
                            SearchStatus = packet.SearchStatus,
                            ExamineStatus = packet.ExamineStatus,
                            ApplyJoinList = "[]",
                            InviteJoinList = "[]"
                        };

                        if (dbConnect.GetSql().Insertable(data).ExecuteCommand() > 0)
                        {
                            rData.Result = 1;

                            rData.TeamID = data.TeamID;

                            log.SaveLog($"[Info] Controller::OnCreateNewTeam, Create Team Success, TeamID:{rData.TeamID}");

                            UpdateUserTeamList(packet.MemberID, rData.TeamID, 1);

                        }
                        else
                        {
                            rData.Result = 0;

                            log.SaveLog($"[Warning] Controller::OnCreateNewTeam, Create Team Fail, Data Base Can Not Inseart");
                        }


                    }
                    else
                    {
                        rData.Result = 2;

                        log.SaveLog($"[Info] Controller::OnCreateNewTeam, Leader:{packet.MemberID} Repeat");
                    }
                }
                else
                {
                    rData.Result = 0;

                    log.SaveLog($"[Info] Controller::OnCreateNewTeam, Team Name:{packet.TeamName} Repeat");
                }

            }
            catch (Exception ex)
            {
                rData.Result = 0;

                log.SaveLog($"[Error] Controller::OnCreateNewTeam Create Error Msg:{ex.Message}");
            }


            JObject jsMain = new JObject();
            jsMain.Add("CmdID", (int)S2C_CmdID.emCreateNewTeamResult);
            jsMain.Add("Data", JsonConvert.DeserializeObject<JObject>(JsonConvert.SerializeObject(rData)));

            return jsMain.ToString();

        }

        /**
         * 更新車隊資料
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

                List<TeamData> LeaderList = dbConnect.GetSql().Queryable<TeamData>().Where(it => it.Leader == packet.MemberID).ToList();

                // 有找到車隊 
                if (TeamList.Count() == 1)
                {   
                    // 只有隊長有更改權限
                    if (TeamList[0].Leader == packet.LeaderID)
                    {
                        // 新隊長不為其他車隊的隊長
                        if (LeaderList.Count() == 0)
                        {
                            string oldLeaderID = TeamList[0].Leader;

                            TeamList[0].Leader = packet.MemberID == null ? TeamList[0].Leader : packet.MemberID;

                            if (dbConnect.GetSql().Updateable<TeamData>(TeamList[0]).Where(it => it.TeamID == packet.TeamID).ExecuteCommand() > 0)
                            {
                                rData.Result = 1;

                                log.SaveLog($"[Info] Controller::OnChangeLander, Change New Leader:{packet.MemberID} Success");

                                // 舊隊長加入車隊隊員列表
                                UpdateTeamMemberList oldLeader = new UpdateTeamMemberList
                                {
                                    TeamID = packet.TeamID,
                                    Action = 1,
                                    MemberID = oldLeaderID
                                };

                                OnUpdateTeamMemberList(oldLeader);

                                // 新隊長從車隊隊員列表中移除
                                UpdateTeamMemberList newLeader = new UpdateTeamMemberList
                                {
                                    TeamID = packet.TeamID,
                                    Action = -1,
                                    MemberID = packet.MemberID
                                };

                                OnUpdateTeamMemberList(oldLeader);
                            }
                            else
                            {
                                rData.Result = 0;

                                log.SaveLog($"[Warning] Controller::OnChangeLander, Change New Leader:{packet.MemberID} Data Base Can Not Update");
                            }

                        }
                        else
                        {
                            rData.Result = 0;

                            log.SaveLog($"[Info] Controller::OnChangeLander, New Leader:{packet.MemberID} That Is Other Team Leader");
                        }

                    }
                    else
                    {
                        rData.Result = 3;

                        log.SaveLog($"[Info] Controller::OnChangeLander, {packet.LeaderID} Can Not Change Leader");
                    }

                }
                else
                {
                    rData.Result = 0;

                    log.SaveLog($"[Warning] Controller::OnChangeLander, Can Not Find Team:{packet.TeamID}");
                }
            }
            catch (Exception ex)
            {
                rData.Result = 0;

                log.SaveLog($"[Error] Controller::OnChangeLander Catch Error, Msg:{ex.Message}");
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
                    // 只有隊長有更改權限
                    if (TeamList[0].Leader == packet.LeaderID)
                    {

                        JArray jsData = JArray.Parse(TeamList[0].TeamViceLeaderIDs);

                        List<string> idList = jsData.ToObject<List<string>>();

                        // 新增
                        if (packet.Action == 1)
                        {
                            if (!idList.Contains(packet.MemberID))
                            {
                                idList.Add(packet.MemberID);

                                rData.Result = 1;

                                log.SaveLog($"[Info] Controller::OnChangeLander, Add Vice Leader:{packet.TeamID} Success");

                            }
                            else
                            {
                                rData.Result = 0;

                                log.SaveLog($"[Info] Controller::OnChangeLander, Vice Leader:{packet.TeamID} Repeat");
                            }

                        }
                        // 刪除
                        else if (packet.Action == -1)
                        {
                            if (idList.Contains(packet.MemberID))
                            {
                                idList.Remove(packet.MemberID);

                                rData.Result = 1;

                                log.SaveLog($"[Info] Controller::OnChangeLander, Remove Vice Leader:{packet.TeamID} Success");
                            }
                            else
                            {
                                rData.Result = 0;

                                log.SaveLog($"[Info] Controller::OnChangeLander, Can Not Find Vice Leader:{packet.TeamID}");

                            }
                        }

                        if (rData.Result == 1)
                        {
                            JArray jsNew = JArray.FromObject(idList);

                            if (dbConnect.GetSql().Updateable<TeamData>().SetColumns(it => new TeamData() { TeamViceLeaderIDs = jsNew.ToString() }).Where(it => it.TeamID == packet.TeamID).ExecuteCommand() > 0)
                            {
                                rData.Result = 1;

                                log.SaveLog($"[Info] Controller::OnChangeLander, Update Vice Leader:{packet.TeamID} Success");

                                if (packet.Action == 1)
                                {
                                    // 新副隊長從車隊隊員列表中移除
                                    UpdateTeamMemberList oldLeader = new UpdateTeamMemberList
                                    {
                                        TeamID = packet.TeamID,
                                        Action = -1,
                                        MemberID = packet.MemberID
                                    };

                                    OnUpdateTeamMemberList(oldLeader);
                                }
                                else if (packet.Action == -1)
                                {
                                    // 舊副隊長加入車隊隊員列表中
                                    UpdateTeamMemberList oldLeader = new UpdateTeamMemberList
                                    {
                                        TeamID = packet.TeamID,
                                        Action = 1,
                                        MemberID = packet.MemberID
                                    };

                                    OnUpdateTeamMemberList(oldLeader);
                                }
                            }
                            else
                            {
                                rData.Result = 0;

                                log.SaveLog($"[Info] Controller::OnChangeLander, Update Vice Leader:{packet.TeamID} Fail, Data Base Con Not Update");
                            }
                        }

                    }
                    else
                    {
                        rData.Result = 2;

                        log.SaveLog($"[Info] Controller::OnUpdateViceLeaderList, {packet.LeaderID} Can Not Update");
                    }

                }
                else
                {
                    rData.Result = 0;

                    log.SaveLog($"[Warning] Controller::OnChangeLander, Can Not Find Team:{packet.TeamID}");
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
                    JArray jsData = JArray.Parse(TeamList[0].TeamMemberIDs);

                    List<string> idList = jsData.ToObject<List<string>>();

                    // 新增
                    if (packet.Action == 1)
                    {
                        if (!idList.Contains(packet.MemberID))
                        {
                            idList.Add(packet.MemberID);

                            rData.Result = 1;

                            log.SaveLog($"[Info] Controller::OnUpdateTeamMemberList, Add Team Member:{packet.MemberID} Success");

                        }
                        else
                        {
                            rData.Result = 0;

                            log.SaveLog($"[Warning] Controller::OnUpdateTeamMemberList, Add Team Member:{packet.MemberID} Fail, User Repeat");

                        }

                    }
                    // 刪除
                    else if (packet.Action == -1)
                    {
                        if (idList.Contains(packet.MemberID))
                        {
                            idList.Remove(packet.MemberID);

                            rData.Result = 1;

                            log.SaveLog($"[Info] Controller::OnUpdateTeamMemberList, Remove Team Member:{packet.MemberID} Success");

                        }
                        else
                        {
                            rData.Result = 0;

                            log.SaveLog($"[Warning] Controller::OnUpdateTeamMemberList, Remove Team Member:{packet.MemberID} Fail, Can Not Find User");

                        }
                    }

                    if (rData.Result == 1)
                    {
                        JArray jsNew = JArray.FromObject(idList);

                        if (dbConnect.GetSql().Updateable<TeamData>().SetColumns(it => new TeamData() { TeamMemberIDs = jsNew.ToString() }).Where(it => it.TeamID == packet.TeamID).ExecuteCommand() > 0)
                        {
                            rData.Result = 1;

                            log.SaveLog($"[Info] Controller::OnUpdateTeamMemberList, Update Team Member:{packet.MemberID} Success");

                            UpdateUserTeamList(packet.MemberID, packet.TeamID, packet.Action);

                        }
                        else
                        {
                            rData.Result = 0;

                            log.SaveLog($"[Info] Controller::OnUpdateTeamMemberList, Update Team Member:{packet.MemberID} Fail, Data Base Con Not Update");
                        }
                    }

                }
                else
                {
                    rData.Result = 0;

                    log.SaveLog($"[Warning] Controller::OnUpdateTeamMemberList, Can Not Find Tram:{packet.TeamID}");
                }

            }
            catch (Exception ex)
            {
                log.SaveLog($"[Error] Controller::OnUpdateTeamMemberList Catch Error, Msg:{ex.Message}");

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
                    JArray jsData = JArray.Parse(TeamList[0].ApplyJoinList);

                    List<string> idList = jsData.ToObject<List<string>>();

                    // 新增
                    if (packet.Action == 1)
                    {
                        if (!idList.Contains(packet.MemberID))
                        {
                            idList.Add(packet.MemberID);

                            rData.Result = 1;

                            log.SaveLog($"[Info] Controller::OnUpdateApplyJoinList, Add {packet.MemberID} to Apply List Success");
                        }
                        else
                        {
                            rData.Result = 0;

                            log.SaveLog($"[Warning] Controller::OnUpdateApplyJoinList, Add {packet.MemberID} to Apply List Fail, User Repeat");
                        }

                    }
                    // 刪除
                    else if (packet.Action == -1)
                    {
                        if (idList.Contains(packet.MemberID))
                        {
                            idList.Remove(packet.MemberID);

                            rData.Result = 1;

                            log.SaveLog($"[Info] Controller::OnUpdateApplyJoinList, Remove {packet.MemberID} From Apply List Success");
                        }
                        else
                        {
                            rData.Result = 0;

                            log.SaveLog($"[Info] Controller::OnUpdateApplyJoinList, Remove {packet.MemberID} From Apply List Fail, Can Not Find User");
                        }
                    }

                    if (rData.Result == 1)
                    {
                        JArray jsNew = JArray.FromObject(idList);

                        if (dbConnect.GetSql().Updateable<TeamData>().SetColumns(it => new TeamData() { ApplyJoinList = jsNew.ToString() }).Where(it => it.TeamID == packet.TeamID).ExecuteCommand() > 0)
                        {
                            rData.Result = 1;

                            log.SaveLog($"[Info] Controller::OnUpdateApplyJoinList, {packet.MemberID} Update Apply List Success");
                        }
                        else
                        {
                            rData.Result = 0;

                            log.SaveLog($"[Info] Controller::OnUpdateApplyJoinList, {packet.MemberID} Update Apply List Fail");
                        }
                    }

                }
                else
                {
                    rData.Result = 0;

                    log.SaveLog($"[Info] Controller::OnUpdateApplyJoinList, Can Not Find Team:{packet.TeamID}");
                }

            }
            catch (Exception ex)
            {
                log.SaveLog($"[Error] Controller::OnUpdateApplyJoinList Catch Error, Msg:{ex.Message}");

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
                    JArray jsData = JArray.Parse(TeamList[0].InviteJoinList);

                    List<string> idList = jsData.ToObject<List<string>>();

                    // 新增
                    if (packet.Action == 1)
                    {
                        if (!idList.Contains(packet.MemberID))
                        {
                            idList.Add(packet.MemberID);

                            rData.Result = 1;

                            log.SaveLog($"[Info] Controller::OnUpdateInviteJoinList, Add {packet.MemberID} to Invite List Success");
                        }
                        else
                        {
                            rData.Result = 0;

                            log.SaveLog($"[Warning] Controller::OnUpdateInviteJoinList, Add {packet.MemberID} to Invite List Fail, User Repeat");
                        }

                    }
                    // 刪除
                    else if (packet.Action == -1)
                    {
                        if (idList.Contains(packet.MemberID))
                        {
                            idList.Remove(packet.MemberID);

                            rData.Result = 1;

                            log.SaveLog($"[Info] Controller::OnUpdateInviteJoinList, Remove {packet.MemberID} From Apply List Success");
                        }
                        else
                        {
                            rData.Result = 0;

                            log.SaveLog($"[Warning] Controller::OnUpdateInviteJoinList, Remove {packet.MemberID} From Apply List Fail, Can Not Find User");
                        }
                    }

                    if (rData.Result == 1)
                    {
                        JArray jsNew = JArray.FromObject(idList);

                        if (dbConnect.GetSql().Updateable<TeamData>().SetColumns(it => new TeamData() { InviteJoinList = jsNew.ToString() }).Where(it => it.TeamID == packet.TeamID).ExecuteCommand() > 0)
                        {
                            rData.Result = 1;

                            log.SaveLog($"[Info] Controller::OnUpdateInviteJoinList, Upsate {packet.MemberID} To Apply List Success");
                        }
                        else
                        {
                            rData.Result = 0;

                            log.SaveLog($"[Warning] Controller::OnUpdateInviteJoinList, Upsate {packet.MemberID} To Apply List Fail");
                        }
                    }

                }
                else
                {
                    rData.Result = 0;

                    log.SaveLog($"[Warning] Controller::OnUpdateInviteJoinList, Can Not Find Team:{packet.MemberID}");
                }

            }
            catch (Exception ex)
            {
                log.SaveLog($"[Error] Controller::OnUpdateInviteJoinList Catch Error, Msg:{ex.Message}");

                rData.Result = 0;
            }

            JObject jsMain = new JObject();
            jsMain.Add("CmdID", (int)S2C_CmdID.emUpdateInviteJoinListResult);
            jsMain.Add("Data", JsonConvert.DeserializeObject<JObject>(JsonConvert.SerializeObject(rData)));

            return jsMain.ToString();
        }

        /**
         * 更新公告
         */
        private string OnUpdateBulletin(UpdateBulletin packet)
        {
            UpdateBulletinResult rData = new UpdateBulletinResult();
            rData.Action = packet.Action;
            rData.BulletinID = packet.BulletinID;

            UpdateBulletin rSendToBs = packet;

            try
            {   
                List<TeamData> TeamList = dbConnect.GetSql().Queryable<TeamData>().Where(it => it.TeamID == packet.TeamID).ToList();

                // 有找到車隊
                if (TeamList.Count() == 1)
                {
                    // 取得副隊長列表
                    JArray jsViceLeader = JArray.Parse(TeamList[0].TeamViceLeaderIDs);

                    List<string> viceLeaderList = jsViceLeader.ToObject<List<string>>();

                    // 檢查發公告的人是否為隊長或副隊長
                    if (packet.MemberID == TeamList[0].Leader || viceLeaderList.Contains(packet.MemberID))
                    {
                        // 新增公告
                        if (packet.Action == 1)
                        {
                            string dateTime = DateTime.UtcNow.ToString("yyyy-MM-dd hh:mm:ss");

                            string guidAll = Guid.NewGuid().ToString();

                            string[] guidList = guidAll.Split('-');

                            TeamBulletin teamBu = new TeamBulletin
                            {
                                BulletinID = "DbBu-" + guidList[0],        // 取GUID前8碼
                                TeamID = packet.TeamID,
                                MemberID = packet.MemberID,
                                CreateDate = dateTime,
                                Content = packet.Content,
                                Day = packet.Day
                            };

                            if (dbConnect.GetSql().Insertable(teamBu).ExecuteCommand() > 0)
                            {
                                rData.Result = 1;

                                rData.BulletinID = teamBu.BulletinID;

                                log.SaveLog($"[Info] Controller::OnUpdateBulletin, Create Team Bulletin Success, BulletinID:{rData.BulletinID}");
                            }
                            else
                            {
                                rData.Result = 0;

                                log.SaveLog($"[Warning] Controller::OnUpdateBulletin, Create Team Bulletin Fail, Data Base Can Not Inseart");
                            }
                        }
                        // 刪除公告
                        else if (packet.Action == -1)
                        {
                            if (dbConnect.GetSql().Deleteable<TeamBulletin>().Where(it => it.BulletinID == packet.BulletinID).ExecuteCommand() > 0)
                            {
                                rData.Result = 1;

                                log.SaveLog($"[Info] Controller::OnUpdateBulletin, Remove Team Bulletin Success, BulletinID:{rData.BulletinID}");
                            }
                            else
                            {
                                rData.Result = 0;

                                log.SaveLog($"[Info] Controller::OnUpdateBulletin, Remove Team Bulletin Fail, BulletinID:{rData.BulletinID}");
                            }
                        }
                        // 修改公告
                        else if (packet.Action == 2)
                        {
                            List<TeamBulletin> BulletinList = dbConnect.GetSql().Queryable<TeamBulletin>().Where(it => it.BulletinID == packet.BulletinID).ToList();

                            // 有找到公告
                            if (BulletinList.Count() == 1)
                            {
                                BulletinList[0].Content = packet.Content == null ? BulletinList[0].Content : packet.Content;
                                BulletinList[0].Day = packet.Day == 0 ? BulletinList[0].Day : packet.Day;

                                if (dbConnect.GetSql().Updateable<TeamBulletin>(TeamList[0]).Where(it => it.BulletinID == packet.BulletinID).ExecuteCommand() > 0)
                                {
                                    rData.Result = 1;

                                    rSendToBs.Action = 2;
                                    rSendToBs.TeamID = BulletinList[0].TeamID;
                                    rSendToBs.MemberID = BulletinList[0].MemberID;
                                    rSendToBs.Content = BulletinList[0].Content;
                                    rSendToBs.Day = BulletinList[0].Day;

                                    log.SaveLog($"[Info] Controller::OnUpdateBulletin, Update Team Bulletin Success, BulletinID:{rData.BulletinID}");
                                }
                                else
                                {
                                    rData.Result = 0;

                                    log.SaveLog($"[Info] Controller::OnUpdateBulletin, Update Team Bulletin Fail, BulletinID:{rData.BulletinID}");
                                }
                            }
                            else
                            {
                                rData.Result = 0;

                                log.SaveLog($"[Info] Controller::OnUpdateBulletin, Update Team Bulletin Fail, BulletinID:{rData.BulletinID}");
                            }
                        }
                    }
                    else
                    {
                        rData.Result = 2;

                        log.SaveLog($"[Warning] Controller::OnUpdateBulletin, {packet.MemberID} Not Leader or Vice Leader");
                    }

                }
                else
                {
                    rData.Result = 0;

                    log.SaveLog($"[Warning] Controller::OnUpdateBulletin, Can Not Find Team:{packet.MemberID}");
                }

            }
            catch (Exception ex)
            {
                log.SaveLog($"[Error] Controller::OnUpdateBulletin Catch Error, Msg:{ex.Message}");

                rData.Result = 0;
            }

            // 新增或修改動作 且 有成功
            if ((packet.Action == 1 || packet.Action == 2) && rData.Result == 1)
            {
                string sData = JsonConvert.SerializeObject(rSendToBs);

                JObject jsSendToBs = new JObject();

                jsSendToBs.Add("CmdID", (int)C2S_CmdID.emUpdateBulletin);
                jsSendToBs.Add("Data", JsonConvert.DeserializeObject<JObject>(sData));

                bsConnect.GetConnect().Send(jsSendToBs.ToString());
            }

            JObject jsMain = new JObject();
            jsMain.Add("CmdID", (int)S2C_CmdID.emUpdateBulletinResult);
            jsMain.Add("Data", JsonConvert.DeserializeObject<JObject>(JsonConvert.SerializeObject(rData)));

            return jsMain.ToString();
        }

        /**
         * 更新活動
         */
        private string OnUpdateActivity(UpdateActivity packet)
        {
            UpdateActivityResult rData = new UpdateActivityResult();
            rData.Action = packet.Action;
            rData.ActID = packet.ActID;

            UpdateActivity rSendToBs = packet;

            try
            {
                List<TeamData> TeamList = dbConnect.GetSql().Queryable<TeamData>().Where(it => it.TeamID == packet.TeamID).ToList();

                // 有找到車隊
                if (TeamList.Count() == 1)
                {
                    // 取得副隊長列表
                    JArray jsViceLeader = JArray.Parse(TeamList[0].TeamViceLeaderIDs);

                    List<string> viceLeaderList = jsViceLeader.ToObject<List<string>>();

                    // 檢查發活動的人是否為隊長或副隊長
                    if (packet.MemberID == TeamList[0].Leader || viceLeaderList.Contains(packet.MemberID))
                    {
                        // 新增活動
                        if (packet.Action == 1)
                        {
                            string dateTime = DateTime.UtcNow.ToString("yyyy-MM-dd hh:mm:ss");

                            string guidAll = Guid.NewGuid().ToString();

                            string[] guidList = guidAll.Split('-');

                            TeamActivity teamAct = new TeamActivity
                            {
                                ActID = "DbAct-" + guidList[0],        // 取GUID前8碼,
                                CreateDate = dateTime,
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
                                Photo = packet.Photo

                            };

                            if (dbConnect.GetSql().Insertable(teamAct).ExecuteCommand() > 0)
                            {
                                rData.Result = 1;

                                rData.ActID = teamAct.ActID;

                                log.SaveLog($"[Info] Controller::OnUpdateActivity, Create Team Activity Success, ActID:{rData.ActID}");
                            }
                            else
                            {
                                rData.Result = 0;

                                log.SaveLog($"[Warning] Controller::OnUpdateActivity, Create Team Activity Fail, Data Base Can Not Inseart");
                            }
                        }
                        // 刪除公告
                        else if (packet.Action == -1)
                        {
                            if (dbConnect.GetSql().Deleteable<TeamActivity>().Where(it => it.ActID == packet.ActID).ExecuteCommand() > 0)
                            {
                                rData.Result = 1;

                                log.SaveLog($"[Info] Controller::OnUpdateActivity, Remove Team Activity Success, BulletinID:{rData.ActID}");
                            }
                            else
                            {
                                rData.Result = 0;

                                log.SaveLog($"[Info] Controller::OnUpdateActivity, Remove Team Activity Fail, BulletinID:{rData.ActID}");
                            }
                        }
                        // 修改公告
                        else if (packet.Action == 2)
                        {
                            List<TeamActivity> ActList = dbConnect.GetSql().Queryable<TeamActivity>().Where(it => it.ActID == packet.ActID).ToList();

                            // 有找到公告
                            if (ActList.Count() == 1)
                            {
                                ActList[0].MemberList = packet.MemberList == null ? ActList[0].MemberList : packet.MemberList;
                                ActList[0].ActDate = packet.ActDate == null ? ActList[0].ActDate : packet.ActDate;
                                ActList[0].Title = packet.Title == null ? ActList[0].Title : packet.Title;
                                ActList[0].MeetTime = packet.MeetTime == null ? ActList[0].MeetTime : packet.MeetTime;
                                ActList[0].TotalDistance = packet.TotalDistance == 0 ? ActList[0].TotalDistance : packet.TotalDistance;
                                ActList[0].MaxAltitude = packet.MaxAltitude == 0 ? ActList[0].MaxAltitude : packet.MaxAltitude;
                                ActList[0].Route = packet.Route == null ? ActList[0].Route : packet.Route;
                                ActList[0].Description = packet.Description == null ? ActList[0].Description : packet.Description;
                                ActList[0].Photo = packet.Photo == null ? ActList[0].Photo : packet.Photo;

                                if (dbConnect.GetSql().Updateable<TeamActivity>(TeamList[0]).Where(it => it.ActID == packet.ActID).ExecuteCommand() > 0)
                                {
                                    rData.Result = 1;

                                    rSendToBs.Action = packet.Action;
                                    rSendToBs.TeamID = ActList[0].TeamID;
                                    rSendToBs.MemberID = ActList[0].MemberID;
                                    rSendToBs.MemberList = ActList[0].MemberList;
                                    rSendToBs.MemberList = ActList[0].MemberList;
                                    rSendToBs.ActDate = ActList[0].ActDate;
                                    rSendToBs.Title = ActList[0].Title;
                                    rSendToBs.MeetTime = ActList[0].MeetTime;
                                    rSendToBs.TotalDistance = ActList[0].TotalDistance;
                                    rSendToBs.MaxAltitude = ActList[0].MaxAltitude;
                                    rSendToBs.Route = ActList[0].Route;
                                    rSendToBs.Description = ActList[0].Description;
                                    rSendToBs.Photo = ActList[0].Photo;

                                    log.SaveLog($"[Info] Controller::OnUpdateActivity, Update Team Activity Success, ActID:{rData.ActID}");
                                }
                                else
                                {
                                    rData.Result = 0;

                                    log.SaveLog($"[Info] Controller::OnUpdateActivity, Update Team Activity Fail, ActID:{rData.ActID}");
                                }
                            }
                            else
                            {
                                rData.Result = 0;

                                log.SaveLog($"[Info] Controller::OnUpdateActivity, Update Team Bulletin Fail, ActID:{rData.ActID}");
                            }
                        }
                    }
                    else
                    {
                        rData.Result = 2;

                        log.SaveLog($"[Warning] Controller::OnUpdateActivity, {packet.MemberID} Not Leader or Vice Leader");
                    }

                }
                else
                {
                    rData.Result = 0;

                    log.SaveLog($"[Warning] Controller::OnUpdateActivity, Can Not Find Team:{packet.TeamID}");
                }

            }
            catch (Exception ex)
            {
                log.SaveLog($"[Error] Controller::OnUpdateActivity Catch Error, Msg:{ex.Message}");

                rData.Result = 0;
            }

            // 新增或修改動作 且 有成功
            if ((packet.Action == 1 || packet.Action == 2) && rData.Result == 1)
            {
                string sData = JsonConvert.SerializeObject(rSendToBs);

                JObject jsSendToBs = new JObject();

                jsSendToBs.Add("CmdID", (int)C2S_CmdID.emUpdateActivity);
                jsSendToBs.Add("Data", JsonConvert.DeserializeObject<JObject>(sData));

                bsConnect.GetConnect().Send(jsSendToBs.ToString());
            }

            JObject jsMain = new JObject();
            jsMain.Add("CmdID", (int)S2C_CmdID.emUpdateActivityResult);
            jsMain.Add("Data", JsonConvert.DeserializeObject<JObject>(JsonConvert.SerializeObject(rData)));

            return jsMain.ToString();
        }

        /**
         * 解散車隊
         */
        private string OnDeleteTeam(DeleteTeam packet)
        {
            DeleteTeamResult rData = new DeleteTeamResult();

            try
            {
                List<TeamData> TeamList = dbConnect.GetSql().Queryable<TeamData>().Where(it => it.TeamID == packet.TeamID).ToList();

                // 有找到車隊
                if (TeamList.Count() == 1)
                {
                    // 檢查解散車隊的人是否為隊長
                    if (packet.MemberID == TeamList[0].Leader)
                    {
                        
                        // 移入暫時區, 帶時間到期後刪除
                        TeamDataStorageCache stroge = new TeamDataStorageCache();
                        stroge.TeamID = TeamList[0].TeamID;
                        stroge.CreateDate = TeamList[0].CreateDate;
                        stroge.Leader = TeamList[0].Leader;
                        stroge.TeamViceLeaderIDs = TeamList[0].TeamViceLeaderIDs;
                        stroge.TeamMemberIDs = TeamList[0].TeamMemberIDs;
                        stroge.TeamName = TeamList[0].TeamName;
                        stroge.TeamInfo = TeamList[0].TeamInfo;
                        stroge.Avatar = TeamList[0].Avatar;
                        stroge.FrontCover = TeamList[0].FrontCover;
                        stroge.County = TeamList[0].County;
                        stroge.SearchStatus = TeamList[0].SearchStatus;
                        stroge.ExamineStatus = TeamList[0].ExamineStatus;
                        stroge.ApplyJoinList = TeamList[0].ApplyJoinList;
                        stroge.InviteJoinList = TeamList[0].InviteJoinList;
                        stroge.StorageDate = DateTime.UtcNow.ToString("yyyy-MM-dd");

                        if (dbConnect.GetSql().Insertable(stroge).ExecuteCommand() > 0)
                        {
                            // 刪除車隊
                            if (dbConnect.GetSql().Deleteable<TeamActivity>().Where(it => it.TeamID == packet.TeamID).ExecuteCommand() > 0)
                            {
                                // 變更車隊成員的車隊列表
                                JArray jsData = JArray.Parse(TeamList[0].TeamMemberIDs);

                                List<string> idList = jsData.ToObject<List<string>>();

                                for (int idx = 0; idx < idList.Count(); idx++)
                                {
                                    // 更新UserInfo的車隊資料
                                    UpdateUserTeamList(idList[idx], TeamList[0].TeamID, -1);
                                }

                                rData.Result = 1;

                                log.SaveLog($"[Info] Controller::OnDeleteTeam, Remove Team:{packet.TeamID} Success");
                            }
                            else
                            {
                                rData.Result = 0;

                                log.SaveLog($"[Info] Controller::OnDeleteTeam, Remove Team:{packet.TeamID} Fail");
                            }
                        }
                        else
                        {
                            rData.Result = 0;

                            log.SaveLog($"[Info] Controller::OnDeleteTeam, Back Up Team Data Fail, Team ID:{packet.TeamID}");
                        }
                    }
                    else
                    {
                        rData.Result = 2;

                        log.SaveLog($"[Warning] Controller::OnDeleteTeam, {packet.MemberID} Not Leader, Can Not Delete Team");
                    }

                }
                else
                {
                    rData.Result = 0;

                    log.SaveLog($"[Warning] Controller::OnDeleteTeam, Can Not Find Team:{packet.TeamID}");
                }

            }
            catch (Exception ex)
            {
                log.SaveLog($"[Error] Controller::OnDeleteTeam Catch Error, Msg:{ex.Message}");

                rData.Result = 0;
            }

            JObject jsMain = new JObject();
            jsMain.Add("CmdID", (int)S2C_CmdID.emDeleteTeamResult);
            jsMain.Add("Data", JsonConvert.DeserializeObject<JObject>(JsonConvert.SerializeObject(rData)));

            return jsMain.ToString();
        }

        /**
         * 加入或離開車隊活動
         */
        private string OnJoinOrLeaveTeamActivity(JoinOrLeaveTeamActivity packet)
        {
            JoinOrLeaveTeamActivityResult rData = new JoinOrLeaveTeamActivityResult();
            rData.Action = packet.Action;

            try
            {
                List<TeamData> TeamList = dbConnect.GetSql().Queryable<TeamData>().Where(it => it.TeamID == packet.TeamID).ToList();

                // 有找到車隊
                if (TeamList.Count() == 1)
                {
                    List<TeamActivity> ActList = dbConnect.GetSql().Queryable<TeamActivity>().Where(it => it.ActID == packet.ActID).ToList();

                    // 有該活動
                    if (ActList.Count() == 1)
                    {
                        JArray jsTeamMember = JArray.Parse(TeamList[0].TeamMemberIDs);

                        List<string> memberList = jsTeamMember.ToObject<List<string>>();

                        // 加入或離開的人為該車隊的隊員
                        if (memberList.Contains(packet.MemberID))
                        {
                            JArray jsActMember = JArray.Parse(ActList[0].MemberList);

                            List<string> actMemberList = jsActMember.ToObject<List<string>>();

                            // 離開
                            if (packet.Action == -1)
                            {
                                if (actMemberList.Contains(packet.MemberID))
                                {
                                    actMemberList.Remove(packet.MemberID);

                                    rData.Result = 1;

                                    log.SaveLog($"[Info] Controller::OnJoinOrLeaveTeamActivity, {packet.MemberID} Remove Team:{packet.TeamID} Activity:{packet.ActID} Success");
                                }
                                else
                                {
                                    rData.Result = 0;

                                    log.SaveLog($"[Warning] Controller::OnJoinOrLeaveTeamActivity, {packet.MemberID} Remove Team:{packet.TeamID} Activity:{packet.ActID} Fail, Can Not Find Member In Activity Member List");
                                }
                            }
                            // 加入
                            else if (packet.Action == 1)
                            {
                                if (!actMemberList.Contains(packet.MemberID))
                                {
                                    actMemberList.Add(packet.MemberID);

                                    rData.Result = 1;

                                    log.SaveLog($"[Info] Controller::OnJoinOrLeaveTeamActivity, {packet.MemberID} Add To Team:{packet.TeamID} Activity:{packet.ActID} Success");
                                }
                                else
                                {
                                    rData.Result = 0;

                                    log.SaveLog($"[Warning] Controller::OnJoinOrLeaveTeamActivity, {packet.MemberID} Add To Team:{packet.TeamID} Activity:{packet.ActID} Fail, Member Repeat");
                                }
                            }
                            else
                            {
                                rData.Result = 0;

                                log.SaveLog($"[Warning] Controller::OnJoinOrLeaveTeamActivity, {packet.MemberID} Action = 0");
                            }

                            if (rData.Result == 1)
                            {
                                JArray jsNew = JArray.FromObject(actMemberList);

                                if (dbConnect.GetSql().Updateable<TeamActivity>().SetColumns(it => new TeamActivity() { MemberList = jsNew.ToString() }).Where(it => it.ActID == packet.ActID).ExecuteCommand() > 0)
                                {
                                    rData.Result = 1;

                                    log.SaveLog($"[Info] Controller::OnJoinOrLeaveTeamActivity, Upsate {packet.ActID} To Member List Success");
                                }
                                else
                                {
                                    rData.Result = 0;

                                    log.SaveLog($"[Warning] Controller::OnJoinOrLeaveTeamActivity, Upsate {packet.ActID} To Member List Fail");
                                }
                            }
                        }
                        else
                        {
                            rData.Result = 0;

                            log.SaveLog($"[Info] Controller::OnJoinOrLeaveTeamActivity, {packet.MemberID} Not Team Member = 0");
                        }

                    }
                    else
                    {
                        rData.Result = 0;

                        log.SaveLog($"[Warning] Controller::OnDeleteTeam, Can Not Find Actovoty:{packet.ActID}");
                    }

                }
                else
                {
                    rData.Result = 0;

                    log.SaveLog($"[Warning] Controller::OnDeleteTeam, Can Not Find Team:{packet.TeamID}");
                }

            }
            catch (Exception ex)
            {
                log.SaveLog($"[Error] Controller::OnDeleteTeam Catch Error, Msg:{ex.Message}");

                rData.Result = 0;
            }

            JObject jsMain = new JObject();
            jsMain.Add("CmdID", (int)S2C_CmdID.emDeleteTeamResult);
            jsMain.Add("Data", JsonConvert.DeserializeObject<JObject>(JsonConvert.SerializeObject(rData)));

            return jsMain.ToString();
        }

        /**
         * 加入或離開車隊
         */
        private string OnJoinOrLeaveTeam(JoinOrLeaveTeam packet)
        {
            JoinOrLeaveTeamResult rData = new JoinOrLeaveTeamResult();
            rData.Action = packet.Action;

            try
            {
                List<TeamData> TeamList = dbConnect.GetSql().Queryable<TeamData>().Where(it => it.TeamID == packet.TeamID).ToList();

                // 有找到車隊
                if (TeamList.Count() == 1)
                {
                    
                    if(packet.Action == 1)
                    {
                        JArray jsApplyList = JArray.Parse(TeamList[0].ApplyJoinList);
                        List<string> ApplyList = jsApplyList.ToObject<List<string>>();

                        JArray jsInviteList = JArray.Parse(TeamList[0].InviteJoinList);
                        List<string> InviteList = jsInviteList.ToObject<List<string>>();

                        // 若包含在申請加入 或 邀請加入列表中
                        if (ApplyList.Contains(packet.MemberID))
                        {
                            ApplyList.Remove(packet.MemberID);

                            rData.Result = 1;

                            log.SaveLog($"[Info] Controller::OnJoinOrLeaveTeam, Remove Member:{packet.MemberID} From Team:{packet.TeamID}'s ApplyJoinList");

                        }
                        else if (InviteList.Contains(packet.MemberID))
                        {
                            InviteList.Remove(packet.MemberID);

                            rData.Result = 1;

                            log.SaveLog($"[Info] Controller::OnJoinOrLeaveTeam, Remove Member:{packet.MemberID} From Team:{packet.TeamID}'s InviteJoinList");
                        }
                        else
                        {
                            rData.Result = 0;

                            log.SaveLog($"[Info] Controller::OnJoinOrLeaveTeam, Can Not Find Member:{packet.MemberID} From Team:{packet.TeamID}'s ApplyJoinList Or InviteJoinList");
                        }

                        if (rData.Result == 1)
                        {
                            JArray jsMemberList = JArray.Parse(TeamList[0].TeamMemberIDs);
                            List<string> MemberList = jsMemberList.ToObject<List<string>>();

                            // 車隊成員列表未包含要加入的會員
                            if (!MemberList.Contains(packet.MemberID))
                            {
                                MemberList.Add(packet.MemberID);

                                JArray jsNew = JArray.FromObject(MemberList);

                                if (dbConnect.GetSql().Updateable<TeamData>().SetColumns(it => new TeamData() { TeamMemberIDs = jsNew.ToString() }).Where(it => it.TeamID == packet.TeamID).ExecuteCommand() > 0)
                                {
                                    rData.Result = 1;

                                    log.SaveLog($"[Info] Controller::OnJoinOrLeaveTeam, Join Member:{packet.MemberID} To Team:{packet.TeamID}'s TeamMemberIDs Success");

                                    // 更新新加入會員的車隊列表
                                    UpdateUserTeamList(packet.MemberID, packet.TeamID, 1);

                                }
                                else
                                {
                                    rData.Result = 0;

                                    log.SaveLog($"[Info] Controller::OnJoinOrLeaveTeam, Join Member:{packet.MemberID} To Team:{packet.TeamID}'s TeamMemberIDs Fail");
                                }
                            }
                            else
                            {
                                rData.Result = 0;

                                log.SaveLog($"[Warning] Controller::OnJoinOrLeaveTeam, Member:{packet.MemberID} Action = 0");

                            }
                        }
                    }
                    else if (packet.Action == -1)
                    {
                        JArray jsMemberList = JArray.Parse(TeamList[0].TeamMemberIDs);
                        List<string> MemberList = jsMemberList.ToObject<List<string>>();

                        // 車隊列表有該名會員
                        if (MemberList.Contains(packet.MemberID))
                        {
                            MemberList.Remove(packet.MemberID);

                            JArray jsNew = JArray.FromObject(MemberList);

                            if (dbConnect.GetSql().Updateable<TeamData>().SetColumns(it => new TeamData() { TeamMemberIDs = jsNew.ToString() }).Where(it => it.TeamID == packet.TeamID).ExecuteCommand() > 0)
                            {
                                rData.Result = 1;

                                log.SaveLog($"[Info] Controller::OnJoinOrLeaveTeam, Remove Member:{packet.MemberID} To Team:{packet.TeamID}'s TeamMemberIDs Success");

                                // 更新新加入會員的車隊列表
                                UpdateUserTeamList(packet.MemberID, packet.TeamID, -1);

                            }
                            else
                            {
                                rData.Result = 0;

                                log.SaveLog($"[Info] Controller::OnJoinOrLeaveTeam, Remove Member:{packet.MemberID} To Team:{packet.TeamID}'s TeamMemberIDs Fail");
                            }
                        }
                        else
                        {
                            rData.Result = 0;

                            log.SaveLog($"[Warning] Controller::OnJoinOrLeaveTeam, Can Not Find Member:{packet.MemberID}");
                        }
                    }
                    else
                    {
                        log.SaveLog($"[Warning] Controller::OnJoinOrLeaveTeam, Join Member:{packet.MemberID} To Team:{packet.TeamID}'s TeamMemberIDs Fail");
                    }

                }
                else
                {
                    rData.Result = 0;

                    log.SaveLog($"[Warning] Controller::OnJoinOrLeaveTeam, Can Not Find Team:{packet.TeamID}");
                }

            }
            catch (Exception ex)
            {
                log.SaveLog($"[Error] Controller::OnDeleteTeam Catch Error, Msg:{ex.Message}");

                rData.Result = 0;
            }

            JObject jsMain = new JObject();
            jsMain.Add("CmdID", (int)S2C_CmdID.emJoinOrLeaveTeamResult);
            jsMain.Add("Data", JsonConvert.DeserializeObject<JObject>(JsonConvert.SerializeObject(rData)));

            return jsMain.ToString();
        }
    }

}
