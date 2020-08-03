using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Windows.Forms;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Tools.Logger;
using Tools.NotifyMessage;

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

        private NotifyMessage ntMsg = null;

        private DataBaseConnect dbConnect = null;

        private Server wsServer = null;                 // Web Socket Server

        private object msgLock = new object();

        private string version = "Team026";

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
        }

        public bool Initialize()
        {
            bool bReturn = false;

            try
            {
                log = new Logger(fm1);

                log.SaveLog($"Team Service Version: {version}");

                wsServer = new Server(log.SaveLog, MessageProcess);

                dbConnect = new DataBaseConnect(log);

                ntMsg = new NotifyMessage(log);

                if (dbConnect.Initialize() && dbConnect.Connect() && wsServer.Initialize() && ntMsg.Initialize())
                {
                    bReturn = true;
                }
                else
                {
                    log.SaveLog($"[Error] Controller::Initialize, Initialize Fail");
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

            if (msg != string.Empty)
            {
                lock (msgLock)
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
         * 取得車隊所有成員列表 (含隊長與副隊長)
         */
        private List<string> GetAllMemberID(TeamData data)
        {
            List<string> ret = new List<string>();

            ret.Add(data.Leader);

            JArray jsViceLeaderList = JArray.Parse(data.TeamViceLeaderIDs);
            List<string> viceLeaderList = jsViceLeaderList.ToObject<List<string>>();
            for (int idx = 0; idx < viceLeaderList.Count(); idx++)
            {
                ret.Add(viceLeaderList[idx]);
            }

            JArray jsMemberList = JArray.Parse(data.TeamMemberIDs);
            List<string> memberList = jsMemberList.ToObject<List<string>>();
            for (int idx = 0; idx < memberList.Count(); idx++)
            {
                ret.Add(memberList[idx]);
            }

            return ret;
        }

        /**
         * 更新使用者車隊資訊
         */
        private void UpdateUserTeamList(string memberID, string teamID, int action)
        {
            // 更新UserInfo的車隊資料
            UserInfo userInfo = dbConnect.GetSql().Queryable<UserInfo>().With(SqlSugar.SqlWith.RowLock).Where(it => it.MemberID == memberID).Single();

            // 有找到會員
            if (userInfo != null)
            {
                JArray jsData = JArray.Parse(userInfo.TeamList);

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

                    if (dbConnect.GetSql().Updateable<UserInfo>().SetColumns(it => new UserInfo() { TeamList = jsNew.ToString() }).With(SqlSugar.SqlWith.RowLock).Where(it => it.MemberID == memberID).ExecuteCommand() > 0)
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
                TeamData teamData = dbConnect.GetSql().Queryable<TeamData>().With(SqlSugar.SqlWith.RowLock).Where(it => it.TeamName == packet.TeamName ).Single();

                List<TeamData> sameLeaderTeamList = dbConnect.GetSql().Queryable<TeamData>().With(SqlSugar.SqlWith.RowLock).Where(it => it.Leader == packet.MemberID).ToList();

                // 未包含該車隊
                if (teamData == null)
                {
                    // 非其他車隊的隊長
                    if (sameLeaderTeamList.Count() == 0)
                    {
                        string dateTime = DateTime.UtcNow.ToString("yyyy-MM-dd hh:mm:ss");

                        string guidAll = Guid.NewGuid().ToString();

                        string[] guidList = guidAll.Split('-');

                        // 建立新車隊
                        TeamData newTeamData = new TeamData
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
                            ApplyJoinList = "[]"
                        };

                        if (dbConnect.GetSql().Insertable(newTeamData).With(SqlSugar.SqlWith.RowLock).ExecuteCommand() > 0)
                        {
                            rData.Result = 1;

                            rData.TeamID = newTeamData.TeamID;

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
                TeamData teamData = dbConnect.GetSql().Queryable<TeamData>().With(SqlSugar.SqlWith.RowLock).Where(it => it.TeamID == packet.TeamID).Single();

                // 有找到車隊
                if (teamData != null)
                {
                    JArray jsViceLeaderList = JArray.Parse(teamData.TeamViceLeaderIDs);
                    List<string> viceLeaderList = jsViceLeaderList.ToObject<List<string>>();

                    // 為隊長或副隊長
                    if (teamData.Leader == packet.MemberID || viceLeaderList.Contains(packet.MemberID))
                    {
                        teamData.CreateDate = Convert.ToDateTime(teamData.CreateDate).ToString("yyyy-MM-dd hh:mm:ss");
                        teamData.TeamName = packet.TeamName == null ? teamData.TeamName : packet.TeamName;
                        teamData.TeamInfo = packet.TeamInfo == null ? teamData.TeamInfo : packet.TeamInfo;
                        teamData.Avatar = packet.Avatar == null ? teamData.Avatar : packet.Avatar;
                        teamData.FrontCover = packet.FrontCover == null ? teamData.FrontCover : packet.FrontCover;
                        teamData.County = packet.County == 0 ? teamData.County : packet.County;
                        teamData.SearchStatus = packet.SearchStatus == 0 ? teamData.SearchStatus : packet.SearchStatus;
                        teamData.ExamineStatus = packet.ExamineStatus == 0 ? teamData.ExamineStatus : packet.ExamineStatus;

                        if (dbConnect.GetSql().Updateable<TeamData>(teamData).With(SqlSugar.SqlWith.RowLock).Where(it => it.TeamID == packet.TeamID).ExecuteCommand() > 0)
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
                TeamData teamData = dbConnect.GetSql().Queryable<TeamData>().With(SqlSugar.SqlWith.RowLock).Where(it => it.TeamID == packet.TeamID).Single();

                List<TeamData> LeaderList = dbConnect.GetSql().Queryable<TeamData>().With(SqlSugar.SqlWith.RowLock).Where(it => it.Leader == packet.MemberID).ToList();

                // 有找到車隊 
                if (teamData != null)
                {   
                    // 只有隊長有更改權限
                    if (teamData.Leader == packet.LeaderID)
                    {
                        // 新隊長不為其他車隊的隊長
                        if (LeaderList.Count() == 0)
                        {
                            JArray jsMemberList = JArray.Parse(teamData.TeamMemberIDs);
                            List<string> memberList = jsMemberList.ToObject<List<string>>();

                            JArray jsViceLeaderList = JArray.Parse(teamData.TeamViceLeaderIDs);
                            List<string> viceLeaderList = jsViceLeaderList.ToObject<List<string>>();

                            List<string> notifyTargetList = GetAllMemberID(teamData);

                            // 新隊長為車隊中的隊員 或 副隊長
                            if (memberList.Contains(packet.MemberID) || viceLeaderList.Contains(packet.MemberID))
                            {
                                string oldLeaderID = teamData.Leader;

                                teamData.Leader = packet.MemberID == null ? teamData.Leader : packet.MemberID;

                                if (dbConnect.GetSql().Updateable<TeamData>(teamData).With(SqlSugar.SqlWith.RowLock).Where(it => it.TeamID == packet.TeamID).ExecuteCommand() > 0)
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

                                    // 新隊長為車隊一般成員
                                    if (memberList.Contains(packet.MemberID))
                                    {
                                        // 新隊長從車隊隊員列表中移除
                                        UpdateTeamMemberList newLeader = new UpdateTeamMemberList
                                        {
                                            TeamID = packet.TeamID,
                                            Action = -1,
                                            MemberID = packet.MemberID
                                        };

                                        OnUpdateTeamMemberList(newLeader);

                                    }

                                    // 新隊長原為副隊長
                                    if (viceLeaderList.Contains(packet.MemberID))
                                    {
                                        UpdateViceLeaderList oldViceLeader = new UpdateViceLeaderList
                                        {
                                            TeamID = packet.TeamID,
                                            Action = -1,
                                            LeaderID = packet.MemberID,
                                            MemberID = packet.MemberID
                                        };

                                        OnUpdateViceLeaderList(oldViceLeader);
                                    }

                                    notifyTargetList.Remove(packet.MemberID);

                                    for (int idx = 0; idx < notifyTargetList.Count(); idx++)
                                    {
                                        string targetID = notifyTargetList[idx];

                                        UserAccount account = dbConnect.GetSql().Queryable<UserAccount>().With(SqlSugar.SqlWith.RowLock).Where(it => it.MemberID == targetID).Single();

                                        UserInfo oldLeaderInfo = dbConnect.GetSql().Queryable<UserInfo>().With(SqlSugar.SqlWith.RowLock).Where(it => it.MemberID == teamData.Leader).Single();
                                        UserInfo newLeaderInfo = dbConnect.GetSql().Queryable<UserInfo>().With(SqlSugar.SqlWith.RowLock).Where(it => it.MemberID == packet.MemberID).Single();

                                        if (account != null && oldLeaderInfo != null && newLeaderInfo != null)
                                        {
                                            string sTitle = $"系統公告";

                                            string sNotifyMsg = $"{teamData.TeamName} 隊長由 {oldLeaderInfo.NickName} 更換為 {newLeaderInfo.NickName} ";

                                            ntMsg.NotifyMsgToDevice(account.NotifyToken, sTitle, sNotifyMsg);
                                        }

                                    }
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

                                log.SaveLog($"[Warning] Controller::OnChangeLander, Member:{packet.MemberID} Not Tame:{packet.TeamID}'s Member");
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
                TeamData teamData = dbConnect.GetSql().Queryable<TeamData>().With(SqlSugar.SqlWith.RowLock).Where(it => it.TeamID == packet.TeamID).Single();

                // 有找到車隊
                if (teamData != null)
                {
                    // 只有隊長有更改權限
                    if (teamData.Leader == packet.LeaderID)
                    {
                        JArray jsMemberList = JArray.Parse(teamData.TeamMemberIDs);
                        List<string> memberList = jsMemberList.ToObject<List<string>>();

                        JArray jsData = JArray.Parse(teamData.TeamViceLeaderIDs);
                        List<string> idList = jsData.ToObject<List<string>>();

                        // 為一般隊員 或 副隊長
                        if (memberList.Contains(packet.MemberID) || idList.Contains(packet.MemberID))
                        {
                            // 新增
                            if (packet.Action == 1)
                            {
                                if (!idList.Contains(packet.MemberID))
                                {
                                    idList.Add(packet.MemberID);

                                    rData.Result = 1;

                                    log.SaveLog($"[Info] Controller::OnUpdateViceLeaderList, Add Vice Leader:{packet.TeamID} Success");

                                }
                                else
                                {
                                    rData.Result = 0;

                                    log.SaveLog($"[Info] Controller::OnUpdateViceLeaderList, Vice Leader:{packet.TeamID} Repeat");
                                }

                            }
                            // 刪除
                            else if (packet.Action == -1)
                            {
                                if (idList.Contains(packet.MemberID))
                                {
                                    idList.Remove(packet.MemberID);

                                    rData.Result = 1;

                                    log.SaveLog($"[Info] Controller::OnUpdateViceLeaderList, Remove Vice Leader:{packet.TeamID} Success");
                                }
                                else
                                {
                                    rData.Result = 0;

                                    log.SaveLog($"[Info] Controller::OnUpdateViceLeaderList, Can Not Find Vice Leader:{packet.TeamID}");

                                }
                            }

                            if (rData.Result == 1)
                            {
                                JArray jsNew = JArray.FromObject(idList);

                                if (dbConnect.GetSql().Updateable<TeamData>().SetColumns(it => new TeamData() { TeamViceLeaderIDs = jsNew.ToString() }).With(SqlSugar.SqlWith.RowLock).Where(it => it.TeamID == packet.TeamID).ExecuteCommand() > 0)
                                {
                                    rData.Result = 1;

                                    log.SaveLog($"[Info] Controller::OnUpdateViceLeaderList, Update Vice Leader:{packet.TeamID} Success");

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

                                    log.SaveLog($"[Info] Controller::OnUpdateViceLeaderList, Update Vice Leader:{packet.TeamID} Fail, Data Base Con Not Update");
                                }
                            }
                        }
                        else
                        {
                            rData.Result = 0;

                            log.SaveLog($"[Info] Controller::OnUpdateViceLeaderList, {packet.MemberID} Not Team:{packet.TeamID}'s Member");
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
                TeamData teamData = dbConnect.GetSql().Queryable<TeamData>().With(SqlSugar.SqlWith.RowLock).Where(it => it.TeamID == packet.TeamID).Single();

                // 有找到車隊
                if (teamData != null)
                {
                    JArray jsMemberData = JArray.Parse(teamData.TeamMemberIDs);
                    List<string> idMemberList = jsMemberData.ToObject<List<string>>();

                    JArray jsData = JArray.Parse(teamData.TeamViceLeaderIDs);
                    List<string> idList = jsData.ToObject<List<string>>();

                    // 新增
                    if (packet.Action == 1)
                    {
                        // 非一般隊員 且 非副隊長 且 非隊長
                        if (!idMemberList.Contains(packet.MemberID) && !idList.Contains(packet.MemberID) && teamData.Leader != packet.MemberID)
                        {
                            idMemberList.Add(packet.MemberID);

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
                        if (idMemberList.Contains(packet.MemberID))
                        {
                            idMemberList.Remove(packet.MemberID);

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
                        JArray jsNew = JArray.FromObject(idMemberList);

                        if (dbConnect.GetSql().Updateable<TeamData>().SetColumns(it => new TeamData() { TeamMemberIDs = jsNew.ToString() }).With(SqlSugar.SqlWith.RowLock).Where(it => it.TeamID == packet.TeamID).ExecuteCommand() > 0)
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

            TeamData teamData = dbConnect.GetSql().Queryable<TeamData>().With(SqlSugar.SqlWith.RowLock).Where(it => it.TeamID == packet.TeamID).Single();

            try
            {
                // 有找到車隊
                if (teamData != null)
                {
                    JArray jsData = JArray.Parse(teamData.ApplyJoinList);

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

                        if (dbConnect.GetSql().Updateable<TeamData>().SetColumns(it => new TeamData() { ApplyJoinList = jsNew.ToString() }).With(SqlSugar.SqlWith.RowLock).Where(it => it.TeamID == packet.TeamID).ExecuteCommand() > 0)
                        {
                            rData.Result = 1;

                            JArray jsViceLeader = JArray.Parse(teamData.TeamViceLeaderIDs);

                            List<string> notifyTargrtList = jsViceLeader.ToObject<List<string>>();
                            notifyTargrtList.Add(teamData.Leader);

                            for (int idx = 0; idx < notifyTargrtList.Count(); idx++)
                            {
                                string targetID = notifyTargrtList[idx];

                               UserAccount account= dbConnect.GetSql().Queryable<UserAccount>().With(SqlSugar.SqlWith.RowLock).Where(it => it.MemberID == targetID).Single();
                               UserInfo userInfo = dbConnect.GetSql().Queryable<UserInfo>().With(SqlSugar.SqlWith.RowLock).Where(it => it.MemberID == targetID).Single();

                                if (account != null && userInfo != null)
                                {
                                    string sTitle = $"系統公告";

                                    string sNotifyMsg = $"{userInfo.NickName} 申請加入 {teamData.TeamName}";

                                    ntMsg.NotifyMsgToDevice(account.NotifyToken, sTitle, sNotifyMsg);
                                }

                            }

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
         * 更新公告
         */
        private string OnUpdateBulletin(UpdateBulletin packet)
        {
            UpdateBulletinResult rData = new UpdateBulletinResult();
            rData.Action = packet.Action;
            rData.BulletinID = packet.BulletinID;

            UpdateBulletin rSendToNy = packet;

            try
            {
                TeamData teamData = null;

                TeamBulletin bulletin = null;

                // 新增
                if (packet.Action == 1)
                {
                    teamData = dbConnect.GetSql().Queryable<TeamData>().With(SqlSugar.SqlWith.RowLock).Where(it => it.TeamID == packet.TeamID).Single();
                }
                // 修改公告 或 刪除公告
                else if (packet.Action == 2 || packet.Action == -1)
                {
                    bulletin = dbConnect.GetSql().Queryable<TeamBulletin>().With(SqlSugar.SqlWith.RowLock).Where(it => it.BulletinID == packet.BulletinID).Single();

                    if (bulletin != null)
                    {
                        teamData = dbConnect.GetSql().Queryable<TeamData>().Where(it => it.TeamID == bulletin.TeamID).Single();
                    }
                    else
                    {
                        rData.Result = 0;

                        log.SaveLog($"[Info] Controller::OnUpdateBulletin, Update Team Bulletin Fail, BulletinID:{rData.BulletinID}");
                    }
                }

                // 有找到車隊
                if (teamData != null)
                {
                    // 取得副隊長列表
                    JArray jsViceLeader = JArray.Parse(teamData.TeamViceLeaderIDs);
                    List<string> viceLeaderList = jsViceLeader.ToObject<List<string>>();

                    // 檢查發公告的人是否為隊長或副隊長
                    if (packet.MemberID == teamData.Leader || viceLeaderList.Contains(packet.MemberID))
                    {
                        // 新增公告
                        if (packet.Action == 1)
                        {
                            string dateTime = DateTime.UtcNow.ToString("yyyy-MM-dd hh:mm:ss");

                            string guidAll = Guid.NewGuid().ToString();

                            string[] guidList = guidAll.Split('-');

                            TeamBulletin newBulletin = new TeamBulletin
                            {
                                BulletinID = "DbBu-" + guidList[0],        // 取GUID前8碼
                                TeamID = packet.TeamID,
                                MemberID = packet.MemberID,
                                CreateDate = dateTime,
                                Content = packet.Content,
                                Day = packet.Day
                            };

                            if (dbConnect.GetSql().Insertable(newBulletin).With(SqlSugar.SqlWith.TabLockX).ExecuteCommand() > 0)
                            {
                                rData.Result = 1;

                                rData.BulletinID = newBulletin.BulletinID;

                                log.SaveLog($"[Info] Controller::OnUpdateBulletin, Create Team Bulletin Success, BulletinID:{rData.BulletinID}");
                            
                                List<string> notifyTargetList = GetAllMemberID(teamData);
                                notifyTargetList.Remove(packet.MemberID);

                                for (int idx = 0; idx < notifyTargetList.Count(); idx++)
                                {
                                    string targetID = notifyTargetList[idx];

                                    UserAccount account = dbConnect.GetSql().Queryable<UserAccount>().With(SqlSugar.SqlWith.RowLock).Where(it => it.MemberID == targetID).Single();

                                    if (account != null)
                                    {
                                        string sTitle = $"車隊公告";

                                        string sNotifyMsg = packet.Content;

                                        ntMsg.NotifyMsgToDevice(account.NotifyToken, sTitle, sNotifyMsg);
                                    }

                                }

                            }
                            else
                            {
                                rData.Result = 0;

                                log.SaveLog($"[Warning] Controller::OnUpdateBulletin, Create Team Bulletin Fail, Data Base Can Not Inseart");
                            }
                        }
                        // 修改公告 或 刪除公告
                        else
                        {
                            // 有找到公告
                            if (bulletin != null)
                            {
                                // 刪除公告
                                if (packet.Action == -1)
                                {
                                    if (dbConnect.GetSql().Deleteable<TeamBulletin>().With(SqlSugar.SqlWith.TabLockX).Where(it => it.BulletinID == packet.BulletinID).ExecuteCommand() > 0)
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
                                    bulletin.MemberID = packet.MemberID == null ? bulletin.MemberID : packet.MemberID;
                                    bulletin.Content = packet.Content == null ? bulletin.Content : packet.Content;
                                    bulletin.Day = packet.Day == 0 ? bulletin.Day : packet.Day;

                                    if (dbConnect.GetSql().Updateable<TeamBulletin>(bulletin).With(SqlSugar.SqlWith.RowLock).Where(it => it.BulletinID == packet.BulletinID).ExecuteCommand() > 0)
                                    {
                                        rData.Result = 1;

                                        rSendToNy.Action = 2;
                                        rSendToNy.TeamID = bulletin.TeamID;
                                        rSendToNy.MemberID = bulletin.MemberID;
                                        rSendToNy.Content = bulletin.Content;
                                        rSendToNy.Day = bulletin.Day;

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

                                    log.SaveLog($"[Warning] Controller::OnUpdateBulletin, MemberID:{packet.MemberID} Action:0");
                                }
                            }
                            else
                            {
                                rData.Result = 0;

                                log.SaveLog($"[Info] Controller::OnUpdateBulletin, Can Not Find, BulletinID:{rData.BulletinID}");
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

                    log.SaveLog($"[Warning] Controller::OnUpdateBulletin, Can Not Find Team:{packet.TeamID}");
                }

            }
            catch (Exception ex)
            {
                log.SaveLog($"[Error] Controller::OnUpdateBulletin Catch Error, Msg:{ex.Message}");

                rData.Result = 0;
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

            UpdateActivity rSendToNy = packet;

            try
            {
                TeamData teamData = null;
                TeamActivity teamAct = null;

                // 新增活動
                if (packet.Action == 1 )
                {
                    teamData = dbConnect.GetSql().Queryable<TeamData>().With(SqlSugar.SqlWith.RowLock).Where(it => it.TeamID == packet.TeamID).Single();
                }
                // 修改活動 或 刪除活動
                else if (packet.Action == 2 || packet.Action == -1)
                {
                    teamAct = dbConnect.GetSql().Queryable<TeamActivity>().With(SqlSugar.SqlWith.RowLock).Where(it => it.ActID == packet.ActID).Single();

                    // 有找到活動
                    if (teamAct != null)
                    {
                        teamData = dbConnect.GetSql().Queryable<TeamData>().With(SqlSugar.SqlWith.RowLock).Where(it => it.TeamID == packet.TeamID).Single();
                    }
                    else 
                    {
                        rData.Result = 0;

                        log.SaveLog($"[Info] Controller::OnUpdateBulletin, Update Team Bulletin Fail, BulletinID:{rData.ActID}");
                    }
                }

                // 有找到車隊
                if (teamData != null)
                {
                    // 取得副隊長列表
                    JArray jsViceLeader = JArray.Parse(teamData.TeamViceLeaderIDs);
                    List<string> viceLeaderList = jsViceLeader.ToObject<List<string>>();

                    // 取得隊員列表
                    JArray jsMemberList = JArray.Parse(teamData.TeamMemberIDs);
                    List<string> memberList = jsMemberList.ToObject<List<string>>();

                    // 檢查發活動的人是否為車隊隊員
                    if (packet.MemberID == teamData.Leader || viceLeaderList.Contains(packet.MemberID) || memberList.Contains(packet.MemberID))
                    {
                        // 新增活動
                        if (packet.Action == 1)
                        {
                            string dateTime = DateTime.UtcNow.ToString("yyyy-MM-dd hh:mm:ss");

                            string guidAll = Guid.NewGuid().ToString();

                            string[] guidList = guidAll.Split('-');

                            TeamActivity newTeamAct = new TeamActivity
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
                                Route = packet.Route

                            };

                            if (dbConnect.GetSql().Insertable(newTeamAct).With(SqlSugar.SqlWith.TabLockX).ExecuteCommand() > 0)
                            {
                                rData.Result = 1;

                                rData.ActID = newTeamAct.ActID;

                                log.SaveLog($"[Info] Controller::OnUpdateActivity, Create Team Activity Success, ActID:{rData.ActID}");

                                List<string> notifyTargetList = GetAllMemberID(teamData);
                                notifyTargetList.Remove(packet.MemberID);

                                for (int idx = 0; idx < notifyTargetList.Count(); idx++)
                                {
                                    string targetID = notifyTargetList[idx];

                                    // 活動發起人
                                    UserInfo userInfo = dbConnect.GetSql().Queryable<UserInfo>().With(SqlSugar.SqlWith.RowLock).Where(it => it.MemberID == packet.MemberID).Single();
                                    
                                    // 收到公告的人
                                    UserAccount account = dbConnect.GetSql().Queryable<UserAccount>().With(SqlSugar.SqlWith.RowLock).Where(it => it.MemberID == targetID).Single();

                                    if (userInfo != null && account != null)
                                    {
                                        string sTitle = $"車隊活動";

                                        string sNotifyMsg = $"{userInfo.NickName} 建立活動 {packet.Title}";

                                        ntMsg.NotifyMsgToDevice(account.NotifyToken, sTitle, sNotifyMsg);
                                    }

                                }

                            }
                            else
                            {
                                rData.Result = 0;

                                log.SaveLog($"[Warning] Controller::OnUpdateActivity, Create Team Activity Fail, Data Base Can Not Inseart");
                            }
                        }
                        // 修改活動 或 刪除活動
                        else
                        {   
                            // 有找到活動
                            if (teamAct != null)
                            {
                                // 為活動發起人
                                if (teamAct.MemberID == packet.MemberID)
                                {
                                    // 刪除活動
                                    if (packet.Action == -1)
                                    {
                                        if (dbConnect.GetSql().Deleteable<TeamActivity>().With(SqlSugar.SqlWith.TabLockX).Where(it => it.ActID == packet.ActID).ExecuteCommand() > 0)
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
                                    // 修改活動
                                    else if (packet.Action == 2)
                                    {

                                        teamAct.MemberList = packet.MemberList == null ? teamAct.MemberList : packet.MemberList;
                                        teamAct.ActDate = packet.ActDate == null ? teamAct.ActDate : packet.ActDate;
                                        teamAct.Title = packet.Title == null ? teamAct.Title : packet.Title;
                                        teamAct.MeetTime = packet.MeetTime == null ? teamAct.MeetTime : packet.MeetTime;
                                        teamAct.TotalDistance = packet.TotalDistance == 0 ? teamAct.TotalDistance : packet.TotalDistance;
                                        teamAct.MaxAltitude = packet.MaxAltitude == 0 ? teamAct.MaxAltitude : packet.MaxAltitude;
                                        teamAct.Route = packet.Route == null ? teamAct.Route : packet.Route;

                                        if (dbConnect.GetSql().Updateable<TeamActivity>(teamData).With(SqlSugar.SqlWith.RowLock).Where(it => it.ActID == packet.ActID).ExecuteCommand() > 0)
                                        {
                                            rData.Result = 1;

                                            rSendToNy.Action = packet.Action;
                                            rSendToNy.TeamID = teamAct.TeamID;
                                            rSendToNy.MemberID = teamAct.MemberID;
                                            rSendToNy.MemberList = teamAct.MemberList;
                                            rSendToNy.MemberList = teamAct.MemberList;
                                            rSendToNy.ActDate = teamAct.ActDate;
                                            rSendToNy.Title = teamAct.Title;
                                            rSendToNy.MeetTime = teamAct.MeetTime;
                                            rSendToNy.TotalDistance = teamAct.TotalDistance;
                                            rSendToNy.MaxAltitude = teamAct.MaxAltitude;
                                            rSendToNy.Route = teamAct.Route;

                                            log.SaveLog($"[Info] Controller::OnUpdateActivity, Update Team Activity Success, ActID:{rData.ActID}");

                                            List<string> notifyTargetList = GetAllMemberID(teamData);
                                            notifyTargetList.Remove(packet.MemberID);

                                            for (int idx = 0; idx < notifyTargetList.Count(); idx++)
                                            {
                                                string targetID = notifyTargetList[idx];

                                                // 收到公告的人
                                                UserAccount account = dbConnect.GetSql().Queryable<UserAccount>().With(SqlSugar.SqlWith.RowLock).Where(it => it.MemberID == targetID).Single();

                                                if (account != null)
                                                {
                                                    string sTitle = $"車隊活動";

                                                    string sNotifyMsg = $"{teamAct.Title} 已更新內容";

                                                    ntMsg.NotifyMsgToDevice(account.NotifyToken, sTitle, sNotifyMsg);
                                                }

                                            }

                                        }
                                        else
                                        {
                                            rData.Result = 0;

                                            log.SaveLog($"[Info] Controller::OnUpdateActivity, Update Team Activity Fail, ActID:{rData.ActID}");
                                        }

                                    }
                                    else
                                    {
                                        rData.Result = 2;

                                        log.SaveLog($"[Info] Controller::OnUpdateActivity, {packet.MemberID} Action: 0");

                                    }
                                }
                                else
                                {
                                    rData.Result = 2;

                                    log.SaveLog($"[Info] Controller::OnUpdateActivity, {packet.MemberID} Can Not Update Act:{packet.ActID}");
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
                TeamData teamData = dbConnect.GetSql().Queryable<TeamData>().With(SqlSugar.SqlWith.RowLock).Where(it => it.TeamID == packet.TeamID).Single();

                // 有找到車隊
                if (teamData != null)
                {
                    // 檢查解散車隊的人是否為隊長
                    if (packet.MemberID == teamData.Leader)
                    {
                        // 移入暫時區, 帶時間到期後刪除
                        TeamDataStorageCache stroge = new TeamDataStorageCache();
                        stroge.TeamID = teamData.TeamID;
                        stroge.CreateDate = teamData.CreateDate;
                        stroge.Leader = teamData.Leader;
                        stroge.TeamViceLeaderIDs = teamData.TeamViceLeaderIDs;
                        stroge.TeamMemberIDs = teamData.TeamMemberIDs;
                        stroge.TeamName = teamData.TeamName;
                        stroge.TeamInfo = teamData.TeamInfo;
                        stroge.Avatar = teamData.Avatar;
                        stroge.FrontCover = teamData.FrontCover;
                        stroge.County = teamData.County;
                        stroge.SearchStatus = teamData.SearchStatus;
                        stroge.ExamineStatus = teamData.ExamineStatus;
                        stroge.ApplyJoinList = teamData.ApplyJoinList;
                        stroge.StorageDate = DateTime.UtcNow.ToString("yyyy-MM-dd");

                        if (dbConnect.GetSql().Insertable(stroge).With(SqlSugar.SqlWith.TabLockX).ExecuteCommand() > 0)
                        {
                            // 刪除車隊
                            if (dbConnect.GetSql().Deleteable<TeamData>().With(SqlSugar.SqlWith.TabLockX).Where(it => it.TeamID == packet.TeamID).ExecuteCommand() > 0)
                            {
                                // 變更車隊成員的車隊列表
                                JArray jsData = JArray.Parse(teamData.TeamMemberIDs);

                                List<string> idList = jsData.ToObject<List<string>>();

                                for (int idx = 0; idx < idList.Count(); idx++)
                                {
                                    // 更新UserInfo的車隊資料
                                    UpdateUserTeamList(idList[idx], teamData.TeamID, -1);
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
                TeamData teamData = dbConnect.GetSql().Queryable<TeamData>().With(SqlSugar.SqlWith.RowLock).Where(it => it.TeamID == packet.TeamID).Single();

                // 有找到車隊
                if (teamData != null)
                {
                    TeamActivity teamAct = dbConnect.GetSql().Queryable<TeamActivity>().Where(it => it.ActID == packet.ActID).Single();

                    // 有該活動
                    if (teamAct != null)
                    {   
                        // 隊員列表
                        JArray jsTeamMember = JArray.Parse(teamData.TeamMemberIDs);
                        List<string> memberList = jsTeamMember.ToObject<List<string>>();

                        // 副隊長列表
                        JArray jsViceLeader = JArray.Parse(teamData.TeamViceLeaderIDs);
                        List<string> viceLeaderList = jsViceLeader.ToObject<List<string>>();

                        // 加入或離開的人為該車隊的隊員
                        if (memberList.Contains(packet.MemberID) || viceLeaderList.Contains(packet.MemberID) || teamData.Leader == packet.MemberID)
                        {
                            JArray jsActMember = JArray.Parse(teamAct.MemberList);

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

                                if (dbConnect.GetSql().Updateable<TeamActivity>().SetColumns(it => new TeamActivity() { MemberList = jsNew.ToString() }).With(SqlSugar.SqlWith.RowLock).Where(it => it.ActID == packet.ActID).ExecuteCommand() > 0)
                                {
                                    rData.Result = 1;

                                    log.SaveLog($"[Info] Controller::OnJoinOrLeaveTeamActivity, Upsate {packet.ActID} To Member List Success");

                                    // 若離開的人車隊發起人, 則解散活動
                                    if (packet.Action == -1 && teamAct.MemberID == packet.MemberID)
                                    {
                                        log.SaveLog($"[Info] Controller::OnJoinOrLeaveTeamActivity, Leave Member: {packet.MemberID} is Activity Member: {teamAct.MemberID}");

                                        UpdateActivity updateAct = new UpdateActivity
                                        {
                                            Action = -1,
                                            ActID = teamAct.ActID,
                                            TeamID = teamAct.TeamID,
                                            MemberID = packet.MemberID
                                        };

                                        OnUpdateActivity(updateAct);
                                    }
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
                TeamData teamData = dbConnect.GetSql().Queryable<TeamData>().With(SqlSugar.SqlWith.RowLock).Where(it => it.TeamID == packet.TeamID).Single();

                // 有找到車隊
                if (teamData != null)
                {
                    // 加入或離開的玩家資訊
                    UserInfo userInfo = dbConnect.GetSql().Queryable<UserInfo>().With(SqlSugar.SqlWith.RowLock).Where(it => it.MemberID == packet.MemberID).Single();

                    if (packet.Action == 1)
                    {
                        JArray jsApplyList = JArray.Parse(teamData.ApplyJoinList);
                        List<string> ApplyList = jsApplyList.ToObject<List<string>>();

                        // 若包含在申請加入
                        if (ApplyList.Contains(packet.MemberID))
                        {
                            ApplyList.Remove(packet.MemberID);

                            if (dbConnect.GetSql().Updateable<TeamData>().SetColumns(it => new TeamData() { ApplyJoinList = JArray.FromObject(ApplyList).ToString() }).With(SqlSugar.SqlWith.RowLock).Where(it => it.TeamID == packet.TeamID).ExecuteCommand() > 0)
                            {
                                rData.Result = 1;

                                log.SaveLog($"[Info] Controller::OnJoinOrLeaveTeam, Remove Member:{packet.MemberID} From Team:{packet.TeamID}'s ApplyJoinList Success");
                            }
                            else
                            {
                                rData.Result = 0;

                                log.SaveLog($"[Info] Controller::OnJoinOrLeaveTeam, Remove Member:{packet.MemberID} From Team:{packet.TeamID}'s ApplyJoinList Fail");
                            }

                        }
                        else
                        {
                            rData.Result = 0;

                            log.SaveLog($"[Info] Controller::OnJoinOrLeaveTeam, Can Not Find Member:{packet.MemberID} From Team:{packet.TeamID}'s ApplyJoinList Or InviteJoinList");
                        }

                        if (rData.Result == 1)
                        {
                            JArray jsMemberList = JArray.Parse(teamData.TeamMemberIDs);
                            List<string> MemberList = jsMemberList.ToObject<List<string>>();

                            // 副隊長列表
                            JArray jsViceLeader = JArray.Parse(teamData.TeamViceLeaderIDs);
                            List<string> viceLeaderList = jsViceLeader.ToObject<List<string>>();

                            // 車隊成員列表未包含要加入的會員
                            if (!MemberList.Contains(packet.MemberID) && !viceLeaderList.Contains(packet.MemberID) && teamData.Leader != packet.MemberID)
                            {
                                MemberList.Add(packet.MemberID);

                                JArray jsNew = JArray.FromObject(MemberList);

                                if (dbConnect.GetSql().Updateable<TeamData>().SetColumns(it => new TeamData() { TeamMemberIDs = jsNew.ToString() }).With(SqlSugar.SqlWith.RowLock).Where(it => it.TeamID == packet.TeamID).ExecuteCommand() > 0)
                                {
                                    rData.Result = 1;

                                    log.SaveLog($"[Info] Controller::OnJoinOrLeaveTeam, Join Member:{packet.MemberID} To Team:{packet.TeamID}'s TeamMemberIDs Success");

                                    // 更新新加入會員的車隊列表
                                    UpdateUserTeamList(packet.MemberID, packet.TeamID, 1);

                                    List<string> notifyTargetList = GetAllMemberID(teamData);
                                    notifyTargetList.Remove(packet.MemberID);

                                    string userNickName = userInfo != null ? userInfo.NickName : "未定義";

                                    for (int idx = 0; idx < notifyTargetList.Count(); idx++)
                                    {
                                        string targetID = notifyTargetList[idx];

                                        // 收到公告的人
                                        UserAccount account = dbConnect.GetSql().Queryable<UserAccount>().With(SqlSugar.SqlWith.RowLock).Where(it => it.MemberID == targetID).Single();

                                        if (account != null)
                                        {
                                            string sTitle = $"車隊公告";

                                            string sNotifyMsg = $"{userNickName} 加入 {teamData.TeamName}";

                                            if (targetID == packet.MemberID)
                                            {
                                                sTitle = $"系統公告";

                                                sNotifyMsg = $"您已加入車隊: {teamData.TeamName}";
                                            }

                                            ntMsg.NotifyMsgToDevice(account.NotifyToken, sTitle, sNotifyMsg);
                                        }

                                    }

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

                                log.SaveLog($"[Warning] Controller::OnJoinOrLeaveTeam, Member:{packet.MemberID} Already Join Team");

                            }
                        }
                    }
                    else if (packet.Action == -1)
                    {
                        JArray jsMemberList = JArray.Parse(teamData.TeamMemberIDs);
                        List<string> MemberList = jsMemberList.ToObject<List<string>>();

                        // 車隊列表有該名會員
                        if (MemberList.Contains(packet.MemberID))
                        {
                            MemberList.Remove(packet.MemberID);

                            JArray jsNew = JArray.FromObject(MemberList);

                            if (dbConnect.GetSql().Updateable<TeamData>().SetColumns(it => new TeamData() { TeamMemberIDs = jsNew.ToString() }).With(SqlSugar.SqlWith.RowLock).Where(it => it.TeamID == packet.TeamID).ExecuteCommand() > 0)
                            {
                                rData.Result = 1;

                                log.SaveLog($"[Info] Controller::OnJoinOrLeaveTeam, Remove Member:{packet.MemberID} To Team:{packet.TeamID}'s TeamMemberIDs Success");

                                // 更新新加入會員的車隊列表
                                UpdateUserTeamList(packet.MemberID, packet.TeamID, -1);

                                List<string> notifyTargetList = GetAllMemberID(teamData);
                                notifyTargetList.Add(packet.MemberID);

                                string userNickName = userInfo != null ? userInfo.NickName : "未定義";

                                for (int idx = 0; idx < notifyTargetList.Count(); idx++)
                                {
                                    string targetID = notifyTargetList[idx];

                                    // 收到公告的人
                                    UserAccount account = dbConnect.GetSql().Queryable<UserAccount>().With(SqlSugar.SqlWith.RowLock).Where(it => it.MemberID == targetID).Single();

                                    if (account != null)
                                    {
                                        string sTitle = $"車隊公告";

                                        string sNotifyMsg = $"{userNickName} 離開 {teamData.TeamName}";

                                        if (targetID == packet.MemberID)
                                        {
                                            sTitle = $"系統公告";

                                            sNotifyMsg = $"您已離開車隊: {teamData.TeamName}";
                                        }

                                        ntMsg.NotifyMsgToDevice(account.NotifyToken, sTitle, sNotifyMsg);
                                    }

                                }

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
