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

using Tools.RedisHashTransfer;
using Tools.WeekProcess;
using Tools.NotifyMessage;

using DataBaseDef;
using Connect;

using TeamPacket.ClientToServer;
using TeamPacket.ServerToClient;

namespace Service.Source
{
    class MessageFunction
    {
        // ==================== Delegate ==================== //

        public delegate void LogDelegate(string msg);

        private LogDelegate log = null;

        // ============================================ //

        private DataBaseConnect db = null;

        RedisConnect redis = null;

        RedisHashTransfer hashTransfer = null;

        private WeekProcess weekProcess = null;

        private NotifyMessage ntMsg = null;

        public MessageFunction(LogDelegate log)
        {
            this.log = log;

            db = new DataBaseConnect(SaveLog);

            redis = new RedisConnect(SaveLog);

            hashTransfer = new RedisHashTransfer();

            weekProcess = new WeekProcess();

            ntMsg = new NotifyMessage(SaveLog);

        }

        ~MessageFunction()
        {

        }

        public void SaveLog(string msg)
        {
            log?.Invoke(msg);
        }

        public bool Initialize()
        {
            bool ret = true;

            if (db.Initialize() && db.Connect() 
                && redis.Initialize() && redis.Connect()
                && ntMsg.Initialize())
            {
                ret = true;

                SaveLog("[Info] MessageFcunction::Initialize, Initialize Success");
            }
            else
            {
                SaveLog("[Error] MessageFcunction::Initialize, Initialize Fail");
            }

            return ret;
        }

        public bool Destory()
        {
            bool ret = true;

            if (db != null)
            {
                db.Disconnect();
            }

            if (redis != null)
            {
                redis.Disconnect();
            }

            return ret;

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
        private bool UpdateUserTeamList(string memberID, string teamID, int action)
        {
            bool success = false;

            // 更新UserInfo的車隊資料
            UserInfo userInfo = db.GetSql().Queryable<UserInfo>().With(SqlSugar.SqlWith.RowLock).Where(it => it.MemberID == memberID).Single();

            // 有找到會員
            if (userInfo != null)
            {
                JArray jsData = JArray.Parse(userInfo.TeamList);

                List<string> idList = jsData.ToObject<List<string>>();


                // 新增
                if (action == 1)
                {
                    if (!idList.Contains(teamID))
                    {
                        idList.Add(teamID);

                        success = true;

                        SaveLog($"[Info] MessageFunction::UpdateUserTeamList, Add User:{memberID} Team List Success");
                    }
                    else
                    {
                        SaveLog($"[Warning] MessageFunction::UpdateUserTeamList, Add User:{memberID} Team List Fail, User Repeat");
                    }
                }
                // 刪除
                else if (action == -1)
                {
                    if (idList.Contains(teamID))
                    {
                        idList.Remove(teamID);

                        success = true;

                        SaveLog($"[Info] MessageFunction::UpdateUserTeamList, Remove User:{memberID} Team List Success");
                    }
                    else
                    {
                        SaveLog($"[Warning] MessageFunction::UpdateUserTeamList, Remove User:{memberID} Team List Fail, Can Not Find User");
                    }
                }

                if (success)
                {
                    JArray jsNew = JArray.FromObject(idList);

                    if (db.GetSql().Updateable<UserInfo>().SetColumns(it => new UserInfo() { TeamList = jsNew.ToString() }).With(SqlSugar.SqlWith.RowLock).Where(it => it.MemberID == memberID).ExecuteCommand() > 0)
                    {
                        userInfo.TeamList = jsNew.ToString();
                        redis.GetRedis((int)Connect.RedisDB.emRedisDB_User).HashSet($"UserInfo_" + userInfo.MemberID, hashTransfer.TransToHashEntryArray(userInfo));

                        SaveLog($"[Warning] MessageFunction::UpdateUserTeamList, Update User:{memberID} Team List Success");
                    }
                    else
                    {
                        SaveLog($"[Warning] MessageFunction::UpdateUserTeamList, Update User:{memberID} Team List Fail");
                    }
                }

            }
            else
            {
                SaveLog($"[Warning] MessageFunction::UpdateUserTeamList, Can Not Find User:{memberID}");
            }

            return success;
        }

        /**
         * 更新車隊成員資訊
         */
        private bool UpdateTeamMemberList(string memberID, string teamID, int action, bool updateUserInfo = true)
        {
            bool success = false;

            try
            {
                TeamData teamData = db.GetSql().Queryable<TeamData>().With(SqlSugar.SqlWith.RowLock).Where(it => it.TeamID == teamID).Single();

                // 有找到車隊
                if (teamData != null)
                {
                    JArray jsMemberData = JArray.Parse(teamData.TeamMemberIDs);
                    List<string> idMemberList = jsMemberData.ToObject<List<string>>();

                    JArray jsData = JArray.Parse(teamData.TeamViceLeaderIDs);
                    List<string> idList = jsData.ToObject<List<string>>();

                    // 新增
                    if (action == 1)
                    {
                        // 非一般隊員 且 非副隊長 且 非隊長
                        if (!idMemberList.Contains(memberID) && !idList.Contains(memberID) && teamData.Leader != memberID)
                        {
                            success = true;

                            idMemberList.Add(memberID);

                            SaveLog($"[Info] MessageFunction::UpdateTeamMemberList, Add Team Member:{memberID} Success");

                        }
                        else
                        {
                            success = false;

                            SaveLog($"[Warning] MessageFunction::OnUpdateTeamMemberList, Add Team Member:{memberID} Fail, User Repeat");
                        }

                    }
                    // 刪除
                    else if (action == -1)
                    {
                        if (idMemberList.Contains(memberID))
                        {
                            idMemberList.Remove(memberID);

                            success = true;

                            SaveLog($"[Info] MessageFunction::OnUpdateTeamMemberList, Remove Team Member:{memberID} Success");

                        }
                        else
                        {
                            success = false;

                            SaveLog($"[Warning] MessageFunction::OnUpdateTeamMemberList, Remove Team Member:{memberID} Fail, Can Not Find User");

                        }
                    }

                    if (success)
                    {
                        JArray jsNew = JArray.FromObject(idMemberList);

                        if (db.GetSql().Updateable<TeamData>().SetColumns(it => new TeamData() { TeamMemberIDs = jsNew.ToString() }).With(SqlSugar.SqlWith.RowLock).Where(it => it.TeamID == teamID).ExecuteCommand() > 0)
                        {
                            success = true;

                            teamData.TeamMemberIDs = jsNew.ToString();
                            redis.GetRedis((int)Connect.RedisDB.emRedisDB_Team).HashSet($"TeamData_" + teamData.TeamID, hashTransfer.TransToHashEntryArray(teamData));

                            SaveLog($"[Info] MessageFunction::OnUpdateTeamMemberList, Update Team Member:{memberID} Success");

                            if (updateUserInfo)
                            {
                                if (!UpdateUserTeamList(memberID, teamID, action))
                                {
                                    success = false;
                                }
                            }

                        }
                        else
                        {
                            success = false;

                            SaveLog($"[Info] MessageFunction::OnUpdateTeamMemberList, Update Team Member:{memberID} Fail, Data Base Con Not Update");
                        }
                    }

                }
                else
                {
                    success = false;

                    SaveLog($"[Warning] MessageFunction::OnUpdateTeamMemberList, Can Not Find Tram:{teamID}");
                }

            }
            catch (Exception ex)
            {
                SaveLog($"[Error] MessageFunction::OnUpdateTeamMemberList Catch Error, Msg:{ex.Message}");

                success = false;
            }

            return success;
        }

        /**
         * 更新副隊長列表
         */
        private bool UpdateViceLeaderList(string leaderID, string memberID, string teamID, int action, bool kick = false)
        {
            bool success = false;

            try
            {
                TeamData teamData = db.GetSql().Queryable<TeamData>().With(SqlSugar.SqlWith.RowLock).Where(it => it.TeamID == teamID).Single();

                // 有找到車隊
                if (teamData != null)
                {
                    // 只有隊長有更改權限
                    if (teamData.Leader == leaderID)
                    {
                        JArray jsMemberList = JArray.Parse(teamData.TeamMemberIDs);
                        List<string> memberList = jsMemberList.ToObject<List<string>>();

                        JArray jsData = JArray.Parse(teamData.TeamViceLeaderIDs);
                        List<string> idList = jsData.ToObject<List<string>>();

                        // 為一般隊員 或 副隊長
                        if (memberList.Contains(memberID) || idList.Contains(memberID))
                        {
                            // 新增
                            if (action == 1)
                            {
                                if (!idList.Contains(memberID))
                                {
                                    idList.Add(memberID);

                                    success = true;

                                    SaveLog($"[Info] MessageFunction::UpdateViceLeaderList, Add Vice Leader:{teamID} Success");

                                }
                                else
                                {
                                    success = false;

                                    SaveLog($"[Info] MessageFunction::UpdateViceLeaderList, Vice Leader:{teamID} Repeat");
                                }

                            }
                            // 刪除
                            else if (action == -1)
                            {
                                if (idList.Contains(memberID))
                                {
                                    idList.Remove(memberID);

                                    success = true;

                                    SaveLog($"[Info] MessageFunction::UpdateViceLeaderList, Remove Vice Leader:{memberID} Success");
                                }
                                else
                                {
                                    success = false;

                                    SaveLog($"[Info] MessageFunction::UpdateViceLeaderList, Can Not Find Vice Leader:{memberID}");

                                }
                            }

                            if (success)
                            {
                                JArray jsNew = JArray.FromObject(idList);

                                if (db.GetSql().Updateable<TeamData>().SetColumns(it => new TeamData() { TeamViceLeaderIDs = jsNew.ToString() }).With(SqlSugar.SqlWith.RowLock).Where(it => it.TeamID == teamID).ExecuteCommand() > 0)
                                {
                                    success = true;

                                    teamData.TeamViceLeaderIDs = jsNew.ToString();
                                    redis.GetRedis((int)Connect.RedisDB.emRedisDB_Team).HashSet($"TeamData_" + teamData.TeamID, hashTransfer.TransToHashEntryArray(teamData));

                                    SaveLog($"[Info] MessageFunction::OnUpdateViceLeaderList, Update Vice Leader:{memberID} Success");

                                    if (action == 1)
                                    {
                                        // 新副隊長從車隊隊員列表中移除
                                        UpdateTeamMemberList(memberID, teamID, -1, false);
                                    }
                                    else if (action == -1 && !kick)
                                    {
                                        // 舊副隊長加入車隊隊員列表中
                                        UpdateTeamMemberList(memberID, teamID, 1, false);
                                    }
                                }
                                else
                                {
                                    success = false;

                                    SaveLog($"[Info] MessageFunction::UpdateViceLeaderList, Update Vice Leader:{teamID} Fail, Data Base Con Not Update");
                                }
                            }
                        }
                        else
                        {
                            success = false;

                            SaveLog($"[Info] MessageFunction::UpdateViceLeaderList, {memberID} Not Team:{teamID}'s Member");
                        }

                    }
                    else
                    {
                        success = false;

                        SaveLog($"[Info] MessageFunction::UpdateViceLeaderList, {leaderID} Can Not Update");
                    }

                }
                else
                {

                    success = false;

                    SaveLog($"[Warning] MessageFunction::UpdateViceLeaderList, Can Not Find Team:{teamID}");
                }
            }
            catch (Exception ex)
            {
                SaveLog("[Error] MessageFunction::UpdateViceLeaderList Catch Error, Msg:" + ex.Message);

                success = false;
            }

            return success;
        }

        /**
         * 建立新車隊
         */
        public string OnCreateNewTeam(string data)
        {
            string ret = "";

            CreateNewTeam packet = JsonConvert.DeserializeObject<CreateNewTeam>(data);

            CreateNewTeamResult rData = new CreateNewTeamResult();

            TeamData newTeamData = new TeamData();

            try
            {
                TeamData teamData = db.GetSql().Queryable<TeamData>().With(SqlSugar.SqlWith.RowLock).Where(it => it.TeamName == packet.TeamName).Single();

                List<TeamData> sameLeaderTeamList = db.GetSql().Queryable<TeamData>().With(SqlSugar.SqlWith.RowLock).Where(it => it.Leader == packet.MemberID).ToList();

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
                        newTeamData.TeamID = "DbTeam-" + guidList[0];        // 取GUID前8碼
                        newTeamData.CreateDate = dateTime;
                        newTeamData.Leader = packet.MemberID;
                        newTeamData.TeamViceLeaderIDs = "[]";
                        newTeamData.TeamMemberIDs = "[]";
                        newTeamData.TeamName = packet.TeamName;
                        newTeamData.TeamInfo = packet.TeamInfo;
                        newTeamData.Avatar = packet.Avatar;
                        newTeamData.FrontCover = packet.FrontCover;
                        newTeamData.County = packet.County;
                        newTeamData.SearchStatus = packet.SearchStatus;
                        newTeamData.ExamineStatus = packet.ExamineStatus;
                        newTeamData.ApplyJoinList = "[]";

                        // 設定DB 交易的起始點
                        db.GetSql().BeginTran();

                        if (db.GetSql().Insertable(newTeamData).With(SqlSugar.SqlWith.RowLock).ExecuteCommand() > 0)
                        {
                            SaveLog($"[Info] MessageFunction::OnCreateNewTeam, Create Team Success, TeamID:{rData.TeamID}");

                            rData.Result = (int)CreateNewTeamResult.ResultDefine.emResult_Success;

                            rData.TeamID = newTeamData.TeamID;

                            if (!UpdateUserTeamList(packet.MemberID, rData.TeamID, 1))
                            {
                                rData.Result = (int)CreateNewTeamResult.ResultDefine.emResult_Fail;

                                SaveLog($"[Warning] MessageFunction::OnCreateNewTeam, Update User:{packet.MemberID} Team List Fail");
                            }

                        }
                        else
                        {
                            rData.Result = (int)CreateNewTeamResult.ResultDefine.emResult_Fail;

                            SaveLog($"[Warning] MessageFunction::OnCreateNewTeam, Create Team Fail, Data Base Can Not Inseart");
                        }

                    }
                    else
                    {
                        rData.Result = (int)CreateNewTeamResult.ResultDefine.emResult_LeaderRepeat;

                        SaveLog($"[Info] MessageFunction::OnCreateNewTeam, Leader:{packet.MemberID} Repeat");
                    }
                }
                else
                {
                    rData.Result = (int)CreateNewTeamResult.ResultDefine.emResult_Fail;

                    SaveLog($"[Info] MessageFunction::OnCreateNewTeam, Team Name:{packet.TeamName} Repeat");
                }

                if (rData.Result == (int)CreateNewTeamResult.ResultDefine.emResult_Success)
                {
                    
                }

            }
            catch (Exception ex)
            {
                rData.Result = (int)CreateNewTeamResult.ResultDefine.emResult_Fail;

                SaveLog($"[Error] MessageFunction::OnCreateNewTeam Create Error Msg:{ex.Message}");
            }

            // DB 交易成功, 提交交易
            db.GetSql().CommitTran();
            if (rData.Result == (int)CreateNewTeamResult.ResultDefine.emResult_Success)
            {
                redis.GetRedis((int)Connect.RedisDB.emRedisDB_Team).HashSet($"TeamData_" + newTeamData.TeamID, hashTransfer.TransToHashEntryArray(newTeamData));
            }
            // DB 交易失敗, 啟動Rollback
            else
            {
                db.GetSql().RollbackTran();
            }

            JObject jsMain = new JObject();
            jsMain.Add("CmdID", (int)S2C_CmdID.emCreateNewTeamResult);
            jsMain.Add("Data", JsonConvert.DeserializeObject<JObject>(JsonConvert.SerializeObject(rData)));

            ret = jsMain.ToString();

            return ret;
        }

        /**
         * 更新車隊資料
         */
        public string OnUpdateTeamData(string data)
        {
            string ret = "";

            UpdateTeamData packet = JsonConvert.DeserializeObject<UpdateTeamData>(data);

            UpdateTeamDataResult rData = new UpdateTeamDataResult();

            TeamData teamData = null;

            try
            {
                teamData = db.GetSql().Queryable<TeamData>().With(SqlSugar.SqlWith.RowLock).Where(it => it.TeamID == packet.TeamID).Single();

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
                        teamData.SearchStatus = packet.SearchStatus == 0 ? teamData.SearchStatus : packet.SearchStatus;
                        teamData.ExamineStatus = packet.ExamineStatus == 0 ? teamData.ExamineStatus : packet.ExamineStatus;

                        // 設定DB 交易的起始點
                        db.GetSql().BeginTran();

                        if (db.GetSql().Updateable<TeamData>(teamData).With(SqlSugar.SqlWith.RowLock).Where(it => it.TeamID == packet.TeamID).ExecuteCommand() > 0)
                        {
                            rData.Result = (int)UpdateTeamDataResult.ResultDefine.emResult_Success;


                        }
                        else
                        {
                            rData.Result = (int)UpdateTeamDataResult.ResultDefine.emResult_Fail;
                        }

                    }
                    else
                    {
                        rData.Result = (int)UpdateTeamDataResult.ResultDefine.emResult_Fail;
                    }
                }
                else
                {
                    rData.Result = (int)UpdateTeamDataResult.ResultDefine.emResult_Fail;
                }
            }
            catch (Exception ex)
            {
                SaveLog("[Error] MessageFunction::OnUpdateTeamData Catch Error, Msg:" + ex.Message);

                rData.Result = (int)UpdateTeamDataResult.ResultDefine.emResult_Fail;
            }
            
            if (rData.Result == (int)UpdateTeamDataResult.ResultDefine.emResult_Success)
            {
                redis.GetRedis((int)Connect.RedisDB.emRedisDB_Team).HashSet($"TeamData_" + teamData.TeamID, hashTransfer.TransToHashEntryArray(teamData));

                // DB 交易提交
                db.GetSql().CommitTran();
            }
            else
            {
                // DB 交易失敗, 啟動Rollback
                db.GetSql().RollbackTran();
            }

            JObject jsMain = new JObject();
            jsMain.Add("CmdID", (int)S2C_CmdID.emUpdateTeamDataResult);
            jsMain.Add("Data", JsonConvert.DeserializeObject<JObject>(JsonConvert.SerializeObject(rData)));

            ret = jsMain.ToString();

            return ret;
        }

        /**
         * 更換隊長
         */
        public string OnChangeLander(string data)
        {
            string ret = "";

            ChangeLander packet = JsonConvert.DeserializeObject<ChangeLander>(data);

            ChangeLanderResult rData = new ChangeLanderResult();

            TeamData teamData = null;

            List<string> notifyTargetList = new List<string>();

            try
            {
                teamData = db.GetSql().Queryable<TeamData>().With(SqlSugar.SqlWith.RowLock).Where(it => it.TeamID == packet.TeamID).Single();

                List<TeamData> LeaderList = db.GetSql().Queryable<TeamData>().With(SqlSugar.SqlWith.RowLock).Where(it => it.Leader == packet.MemberID).ToList();

                // 有找到車隊 
                if (teamData != null)
                {
                    notifyTargetList = GetAllMemberID(teamData);

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

                            // 新隊長為車隊中的隊員 或 副隊長
                            if (memberList.Contains(packet.MemberID) || viceLeaderList.Contains(packet.MemberID))
                            {
                                string oldLeaderID = teamData.Leader;

                                // 舊隊長加入車隊成員列表
                                if (!memberList.Contains(teamData.Leader))
                                {
                                    memberList.Add(teamData.Leader);

                                    // 新隊長若為車隊成員, 則從列表中刪除
                                    if (memberList.Contains(packet.MemberID))
                                    {
                                        memberList.Remove(packet.MemberID);
                                    }

                                    // 新隊長若為副隊長, 則從副隊長列表中刪除
                                    if (viceLeaderList.Contains(packet.MemberID))
                                    {
                                        viceLeaderList.Remove(packet.MemberID);
                                    }

                                    // 設定新隊長
                                    if (packet.MemberID != null || packet.MemberID != "")
                                    {
                                        teamData.Leader = packet.MemberID;

                                        teamData.TeamViceLeaderIDs = JArray.FromObject(viceLeaderList).ToString();

                                        teamData.TeamMemberIDs = JArray.FromObject(memberList).ToString();

                                        if (db.GetSql().Updateable<TeamData>(teamData).With(SqlSugar.SqlWith.RowLock).Where(it => it.TeamID == packet.TeamID).ExecuteCommand() > 0)
                                        {
                                            rData.Result = (int)ChangeLanderResult.ResultDefine.emResult_Success;

                                            notifyTargetList.Remove(packet.MemberID);

                                            SaveLog($"[Info] MessageFunction::OnChangeLander, Change New Leader:{packet.MemberID} Success");
                                        }
                                        else
                                        {
                                            rData.Result = (int)ChangeLanderResult.ResultDefine.emResult_Fail;

                                            SaveLog($"[Warning] MessageFunction::OnChangeLander, Change New Leader:{packet.MemberID} Data Base Can Not Update");
                                        }
                                    }
                                    else
                                    {
                                        rData.Result = (int)ChangeLanderResult.ResultDefine.emResult_Fail;
                                            
                                    }
                                }
                                else
                                {
                                    rData.Result = (int)ChangeLanderResult.ResultDefine.emResult_Fail;
                                }

                            }
                            else
                            {
                                rData.Result = (int)ChangeLanderResult.ResultDefine.emResult_Fail;

                                SaveLog($"[Warning] MessageFunction::OnChangeLander, Member:{packet.MemberID} Not Tame:{packet.TeamID}'s Member");
                            }

                        }
                        else
                        {
                            rData.Result = (int)ChangeLanderResult.ResultDefine.emResult_Fail;

                            SaveLog($"[Info] MessageFunction::OnChangeLander, New Leader:{packet.MemberID} That Is Other Team Leader");
                        }

                    }
                    else
                    {
                        rData.Result = (int)ChangeLanderResult.ResultDefine.emResult_InsufficientPermissions;

                        SaveLog($"[Info] MessageFunction::OnChangeLander, {packet.LeaderID} Can Not Change Leader");
                    }

                }
                else
                {
                    rData.Result = (int)ChangeLanderResult.ResultDefine.emResult_Fail;

                    SaveLog($"[Warning] MessageFunction::OnChangeLander, Can Not Find Team:{packet.TeamID}");
                }
            }
            catch (Exception ex)
            {
                rData.Result = (int)ChangeLanderResult.ResultDefine.emResult_Fail;

                SaveLog($"[Error] MessageFunction::OnChangeLander Catch Error, Msg:{ex.Message}");
            }

            // 有找到車隊資料 且 判斷成功
            if (teamData != null && rData.Result == (int)ChangeLanderResult.ResultDefine.emResult_Success)
            {
                redis.GetRedis((int)Connect.RedisDB.emRedisDB_Team).HashSet($"TeamData_" + teamData.TeamID, hashTransfer.TransToHashEntryArray(teamData));

                for (int idx = 0; idx < notifyTargetList.Count(); idx++)
                {
                    string targetID = notifyTargetList[idx];

                    UserAccount account = db.GetSql().Queryable<UserAccount>().With(SqlSugar.SqlWith.RowLock).Where(it => it.MemberID == targetID).Single();

                    UserInfo oldLeaderInfo = db.GetSql().Queryable<UserInfo>().With(SqlSugar.SqlWith.RowLock).Where(it => it.MemberID == teamData.Leader).Single();
                    UserInfo newLeaderInfo = db.GetSql().Queryable<UserInfo>().With(SqlSugar.SqlWith.RowLock).Where(it => it.MemberID == packet.MemberID).Single();

                    if (account != null && oldLeaderInfo != null && newLeaderInfo != null)
                    {
                        string sTitle = $"系統公告";

                        string sNotifyMsg = $"{teamData.TeamName} 隊長由 {oldLeaderInfo.NickName} 更換為 {newLeaderInfo.NickName} ";

                        ntMsg.NotifyMsgToDevice(targetID, account.NotifyToken, sTitle, sNotifyMsg);
                    }

                }
            }
            else
            {
                db.GetSql().RollbackTran();

                SaveLog($"[Warning] MessageFunction::OnChangeLander, Change Fail, Result:{rData.Result}");

            }

            JObject jsMain = new JObject();
            jsMain.Add("CmdID", (int)S2C_CmdID.emChangeLanderResult);
            jsMain.Add("Data", JsonConvert.DeserializeObject<JObject>(JsonConvert.SerializeObject(rData)));

            ret = jsMain.ToString();

            return ret;
        }

        /**
         * 更新副隊長列表
         */
        public string OnUpdateViceLeaderList(string data)
        {
            string ret = "";

            UpdateViceLeaderList packet = JsonConvert.DeserializeObject<UpdateViceLeaderList>(data);

            UpdateViceLeaderListResult rData = new UpdateViceLeaderListResult();
            rData.Action = packet.Action;

            try
            {
                TeamData teamData = db.GetSql().Queryable<TeamData>().With(SqlSugar.SqlWith.RowLock).Where(it => it.TeamID == packet.TeamID).Single();

                // 有找到車隊
                if (teamData != null)
                {
                    // 只有隊長有更改權限
                    if (teamData.Leader == packet.LeaderID)
                    {
                        // 設定DB 交易的起始點
                        db.GetSql().BeginTran();

                        if (UpdateViceLeaderList(packet.LeaderID, packet.MemberID, packet.TeamID, packet.Action))
                        {
                            SaveLog($"[Info] MessageFunction::OnUpdateViceLeaderList, UpdateViceLeaderList Success");

                            rData.Result = (int)UpdateViceLeaderListResult.ResultDefine.emResult_Success;

                            // DB 交易提交
                            db.GetSql().CommitTran();
                        }
                        else
                        {
                            SaveLog($"[Warning] MessageFunction::OnUpdateViceLeaderList, UpdateViceLeaderList Fail");

                            rData.Result = (int)UpdateViceLeaderListResult.ResultDefine.emResult_Fail;
                        }

                    }
                    else
                    {
                        rData.Result = (int)UpdateViceLeaderListResult.ResultDefine.emResult_InsufficientPermissions;

                        SaveLog($"[Info] MessageFunction::OnUpdateViceLeaderList, {packet.LeaderID} Can Not Update");
                    }

                }
                else
                {
                    rData.Result = (int)UpdateViceLeaderListResult.ResultDefine.emResult_Fail;

                    SaveLog($"[Warning] MessageFunction::OnChangeLander, Can Not Find Team:{packet.TeamID}");
                }
            }
            catch (Exception ex)
            {
                rData.Result = (int)UpdateViceLeaderListResult.ResultDefine.emResult_Fail;

                SaveLog("[Error] MessageFunction::OnUpdateViceLeaderList Catch Error, Msg:" + ex.Message);
            }

            // DB 交易失敗, 啟動Rollback
            if (rData.Result != (int)UpdateViceLeaderListResult.ResultDefine.emResult_Success)
            {
                db.GetSql().RollbackTran();
            }

            JObject jsMain = new JObject();
            jsMain.Add("CmdID", (int)S2C_CmdID.emUpdateViceLeaderListResult);
            jsMain.Add("Data", JsonConvert.DeserializeObject<JObject>(JsonConvert.SerializeObject(rData)));

            ret = jsMain.ToString();

            return ret;
        }

        ///**
        // * 更新車隊隊員列表 (不使用)
        // */
        //public string OnUpdateTeamMemberList(string data)
        //{
        //    string ret = "";

        //    UpdateTeamMemberList packet = JsonConvert.DeserializeObject<UpdateTeamMemberList>(data);

        //    UpdateTeamMemberListResult rData = new UpdateTeamMemberListResult();
        //    rData.Action = packet.Action;

        //    try
        //    {
        //        TeamData teamData = db.GetSql().Queryable<TeamData>().With(SqlSugar.SqlWith.RowLock).Where(it => it.TeamID == packet.TeamID).Single();

        //        // 有找到車隊
        //        if (teamData != null)
        //        {
        //            // 設定DB 交易的起始點
        //            db.GetSql().BeginTran();

        //            if (UpdateTeamMemberList(packet.MemberID, packet.TeamID, packet.Action))
        //            {
        //                SaveLog($"[Info] MessageFunction::OnUpdateTeamMemberList, UpdateTeamMemberList Success");

        //                rData.Result = (int)UpdateTeamMemberListResult.ResultDefine.emResult_Success;

        //                // DB 交易提交
        //                db.GetSql().CommitTran();
        //            }
        //            else
        //            {
        //                SaveLog($"[Warning] MessageFunction::OnUpdateTeamMemberList, UpdateTeamMemberList Fail");

        //                rData.Result = (int)UpdateTeamMemberListResult.ResultDefine.emResult_Fail;
        //            }

        //        }
        //        else
        //        {
        //            rData.Result = (int)UpdateTeamMemberListResult.ResultDefine.emResult_Fail;

        //            SaveLog($"[Warning] MessageFunction::OnUpdateTeamMemberList, Can Not Find Tram:{packet.TeamID}");
        //        }

        //    }
        //    catch (Exception ex)
        //    {
        //        rData.Result = (int)UpdateTeamMemberListResult.ResultDefine.emResult_Fail;

        //        SaveLog($"[Error] MessageFunction::OnUpdateTeamMemberList Catch Error, Msg:{ex.Message}");
        //    }

        //    // DB 交易失敗, 啟動Rollback
        //    if (rData.Result != 1)
        //    {
        //        db.GetSql().RollbackTran();
        //    }

        //    JObject jsMain = new JObject();
        //    jsMain.Add("CmdID", (int)S2C_CmdID.emUpdateTeamMemberListResult);
        //    jsMain.Add("Data", JsonConvert.DeserializeObject<JObject>(JsonConvert.SerializeObject(rData)));

        //    ret = jsMain.ToString();

        //    return ret;
        //}

        /**
         * 更新申請加入車隊列表
         */
        public string OnUpdateApplyJoinList(string data)
        {
            string ret = "";

            UpdateApplyJoinList packet = JsonConvert.DeserializeObject<UpdateApplyJoinList>(data);

            UpdateApplyJoinListResult rData = new UpdateApplyJoinListResult();
            rData.Action = packet.Action;

            TeamData teamData = db.GetSql().Queryable<TeamData>().With(SqlSugar.SqlWith.RowLock).Where(it => it.TeamID == packet.TeamID).Single();

            try
            {
                // 有找到車隊
                if (teamData != null)
                {
                    // 設定DB 交易的起始點
                    db.GetSql().BeginTran();

                    // 有開啟審核
                    if (teamData.ExamineStatus == 1)
                    {
                        JArray jsData = JArray.Parse(teamData.ApplyJoinList);

                        List<string> applyJoinList = jsData.ToObject<List<string>>();

                        // 新增
                        if (packet.Action == (int)UpdateApplyJoinList.ActionDefine.emResult_Add)
                        {
                            if (!applyJoinList.Contains(packet.MemberID))
                            {
                                applyJoinList.Add(packet.MemberID);

                                rData.Result = (int)UpdateApplyJoinListResult.ResultDefine.emResult_Success;

                                SaveLog($"[Info] MessageFunction::OnUpdateApplyJoinList, Add {packet.MemberID} to Apply List Success");
                            }
                            else
                            {
                                rData.Result = (int)UpdateApplyJoinListResult.ResultDefine.emResult_Fail;

                                SaveLog($"[Warning] MessageFunction::OnUpdateApplyJoinList, Add {packet.MemberID} to Apply List Fail, User Repeat");
                            }
                        }
                        // 刪除
                        else if (packet.Action == (int)UpdateApplyJoinList.ActionDefine.emResult_Delete)
                        {
                            if (applyJoinList.Contains(packet.MemberID))
                            {
                                applyJoinList.Remove(packet.MemberID);

                                rData.Result = (int)UpdateApplyJoinListResult.ResultDefine.emResult_Success;

                                SaveLog($"[Info] MessageFunction::OnUpdateApplyJoinList, Remove {packet.MemberID} From Apply List Success");
                            }
                            else
                            {
                                rData.Result = (int)UpdateApplyJoinListResult.ResultDefine.emResult_Fail;

                                SaveLog($"[Info] MessageFunction::OnUpdateApplyJoinList, Remove {packet.MemberID} From Apply List Fail, Can Not Find User");
                            }
                        }

                        if (rData.Result == (int)UpdateApplyJoinListResult.ResultDefine.emResult_Success)
                        {
                            JArray jsNew = JArray.FromObject(applyJoinList);

                            if (db.GetSql().Updateable<TeamData>().SetColumns(it => new TeamData() { ApplyJoinList = jsNew.ToString() }).With(SqlSugar.SqlWith.RowLock).Where(it => it.TeamID == packet.TeamID).ExecuteCommand() > 0)
                            {
                                rData.Result = (int)UpdateApplyJoinListResult.ResultDefine.emResult_Success;

                                teamData.ApplyJoinList = jsNew.ToString();

                                SaveLog($"[Info] MessageFunction::OnUpdateApplyJoinList, {packet.MemberID} Update Apply List Success");
                            }
                            else
                            {
                                rData.Result = (int)UpdateApplyJoinListResult.ResultDefine.emResult_Fail;

                                SaveLog($"[Info] MessageFunction::OnUpdateApplyJoinList, {packet.MemberID} Update Apply List Fail");
                            }
                        }
                    }
                    else
                    {   
                        // 新增
                        if (packet.Action == (int)UpdateApplyJoinList.ActionDefine.emResult_Add)
                        {
                            // 直接加入車隊
                            if (UpdateTeamMemberList(packet.MemberID, packet.TeamID, 1))
                            {
                                SaveLog($"[Info] MessageFunction::OnUpdateApplyJoinList, UpdateTeamMemberList Success");

                                JArray jsData = JArray.Parse(teamData.TeamMemberIDs);

                                List<string> memberList = jsData.ToObject<List<string>>();

                                memberList.Add(packet.MemberID);

                                teamData.TeamMemberIDs = JArray.FromObject(memberList).ToString();

                                rData.Result = (int)UpdateApplyJoinListResult.ResultDefine.emResult_Success;
                            }
                            else
                            {
                                SaveLog($"[Warning] MessageFunction::OnUpdateApplyJoinList, UpdateTeamMemberList Fail");

                                rData.Result = (int)UpdateApplyJoinListResult.ResultDefine.emResult_Fail;
                            }
                        }
                        else
                        {
                            SaveLog($"[Warning] MessageFunction::OnUpdateApplyJoinList, Warning Setting");

                            rData.Result = (int)UpdateApplyJoinListResult.ResultDefine.emResult_Fail;
                        }

                    }

                }
                else
                {
                    rData.Result = (int)UpdateApplyJoinListResult.ResultDefine.emResult_Fail;

                    SaveLog($"[Info] MessageFunction::OnUpdateApplyJoinList, Can Not Find Team:{packet.TeamID}");
                }

            }
            catch (Exception ex)
            {
                SaveLog($"[Error] MessageFunction::OnUpdateApplyJoinList Catch Error, Msg:{ex.Message}");

                rData.Result = (int)UpdateApplyJoinListResult.ResultDefine.emResult_Fail;
            }

            if (rData.Result == (int)UpdateApplyJoinListResult.ResultDefine.emResult_Success)
            {
                // DB 交易提交
                db.GetSql().CommitTran();

                redis.GetRedis((int)Connect.RedisDB.emRedisDB_Team).HashSet($"TeamData_" + teamData.TeamID, hashTransfer.TransToHashEntryArray(teamData));

                JArray jsViceLeader = JArray.Parse(teamData.TeamViceLeaderIDs);

                List<string> notifyTargrtList = jsViceLeader.ToObject<List<string>>();
                notifyTargrtList.Add(teamData.Leader);

                for (int idx = 0; idx < notifyTargrtList.Count(); idx++)
                {
                    string targetID = notifyTargrtList[idx];

                    UserAccount account = db.GetSql().Queryable<UserAccount>().With(SqlSugar.SqlWith.RowLock).Where(it => it.MemberID == targetID).Single();
                    UserInfo userInfo = db.GetSql().Queryable<UserInfo>().With(SqlSugar.SqlWith.RowLock).Where(it => it.MemberID == targetID).Single();

                    if (account != null && userInfo != null)
                    {
                        string action = teamData.ExamineStatus == 1 ? "申請加入" : "加入";

                        string sTitle = $"系統公告";

                        string sNotifyMsg = $"{userInfo.NickName} {action} {teamData.TeamName}";

                        ntMsg.NotifyMsgToDevice(targetID, account.NotifyToken, sTitle, sNotifyMsg);
                    }

                }

            }
            else
            {
                // DB 交易失敗, 啟動Rollback
                db.GetSql().RollbackTran();
            }

            JObject jsMain = new JObject();
            jsMain.Add("CmdID", (int)S2C_CmdID.emUpdateApplyJoinListResult);
            jsMain.Add("Data", JsonConvert.DeserializeObject<JObject>(JsonConvert.SerializeObject(rData)));

            ret = jsMain.ToString();

            return ret;
        }

        /**
         * 更新公告
         */
        public string OnUpdateBulletin(string data)
        {
            string ret = "";

            UpdateBulletin packet = JsonConvert.DeserializeObject<UpdateBulletin>(data);

            UpdateBulletinResult rData = new UpdateBulletinResult();
            rData.Action = packet.Action;
            rData.BulletinID = packet.BulletinID;

            TeamData teamData = null;

            TeamBulletin newBulletin = null;

            TeamBulletin bulletin = null;

            try
            {
                // 新增
                if (packet.Action == (int)UpdateBulletin.ActionDefine.emResult_Add)
                {
                    teamData = db.GetSql().Queryable<TeamData>().With(SqlSugar.SqlWith.RowLock).Where(it => it.TeamID == packet.TeamID).Single();
                }
                // 修改公告 或 刪除公告
                else if (packet.Action == (int)UpdateBulletin.ActionDefine.emResult_Modify 
                    || packet.Action == (int)UpdateBulletin.ActionDefine.emResult_Delete)
                {
                    bulletin = db.GetSql().Queryable<TeamBulletin>().With(SqlSugar.SqlWith.RowLock).Where(it => it.BulletinID == packet.BulletinID).Single();

                    if (bulletin != null)
                    {
                        teamData = db.GetSql().Queryable<TeamData>().Where(it => it.TeamID == bulletin.TeamID).Single();
                    }
                    else
                    {
                        rData.Result = (int)UpdateBulletinResult.ResultDefine.emResult_Fail;

                        SaveLog($"[Info] MessageFunction::OnUpdateBulletin, Update Team Bulletin Fail, BulletinID:{rData.BulletinID}");
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
                        if (packet.Action == (int)UpdateBulletin.ActionDefine.emResult_Add)
                        {
                            string guidAll = Guid.NewGuid().ToString();

                            string[] guidList = guidAll.Split('-');

                            newBulletin = new TeamBulletin();
                            newBulletin.BulletinID = "DbBu-" + guidList[0];        // 取GUID前8碼
                            newBulletin.TeamID = packet.TeamID;
                            newBulletin.MemberID = packet.MemberID;
                            newBulletin.CreateDate = DateTime.UtcNow.ToString("yyyy-MM-dd hh:mm:ss");
                            newBulletin.Content = packet.Content;
                            newBulletin.Day = packet.Day;

                            if (db.GetSql().Insertable(newBulletin).With(SqlSugar.SqlWith.TabLockX).ExecuteCommand() > 0)
                            {
                                rData.Result = (int)UpdateBulletinResult.ResultDefine.emResult_Success;

                                rData.BulletinID = newBulletin.BulletinID;

                                SaveLog($"[Info] MessageFunction::OnUpdateBulletin, Create Team Bulletin Success, BulletinID:{rData.BulletinID}");
                            }
                            else
                            {
                                rData.Result = (int)UpdateBulletinResult.ResultDefine.emResult_Fail;

                                SaveLog($"[Warning] MessageFunction::OnUpdateBulletin, Create Team Bulletin Fail, Data Base Can Not Inseart");
                            }
                        }
                        // 修改公告 或 刪除公告
                        else
                        {
                            // 有找到公告
                            if (bulletin != null)
                            {
                                // 刪除公告
                                if (packet.Action == (int)UpdateBulletin.ActionDefine.emResult_Delete)
                                {
                                    if (db.GetSql().Deleteable<TeamBulletin>().With(SqlSugar.SqlWith.TabLockX).Where(it => it.BulletinID == packet.BulletinID).ExecuteCommand() > 0)
                                    {
                                        rData.Result = (int)UpdateBulletinResult.ResultDefine.emResult_Success;

                                        SaveLog($"[Info] MessageFunction::OnUpdateBulletin, Remove Team Bulletin Success, BulletinID:{rData.BulletinID}");
                                    }
                                    else
                                    {
                                        rData.Result = (int)UpdateBulletinResult.ResultDefine.emResult_Fail;

                                        SaveLog($"[Info] MessageFunction::OnUpdateBulletin, Remove Team Bulletin Fail, BulletinID:{rData.BulletinID}");
                                    }
                                }
                                // 修改公告
                                else if (packet.Action == (int)UpdateBulletin.ActionDefine.emResult_Modify)
                                {
                                    bulletin.MemberID = packet.MemberID == null ? bulletin.MemberID : packet.MemberID;
                                    bulletin.Content = packet.Content == null ? bulletin.Content : packet.Content;
                                    bulletin.Day = packet.Day == 0 ? bulletin.Day : packet.Day;

                                    if (db.GetSql().Updateable<TeamBulletin>(bulletin).With(SqlSugar.SqlWith.RowLock).Where(it => it.BulletinID == packet.BulletinID).ExecuteCommand() > 0)
                                    {
                                        rData.Result = (int)UpdateBulletinResult.ResultDefine.emResult_Success;

                                        SaveLog($"[Info] MessageFunction::OnUpdateBulletin, Update Team Bulletin Success, BulletinID:{rData.BulletinID}");
                                    }
                                    else
                                    {
                                        rData.Result = (int)UpdateBulletinResult.ResultDefine.emResult_Fail;

                                        SaveLog($"[Info] MessageFunction::OnUpdateBulletin, Update Team Bulletin Fail, BulletinID:{rData.BulletinID}");
                                    }

                                }
                                else
                                {
                                    rData.Result = (int)UpdateBulletinResult.ResultDefine.emResult_Fail;

                                    SaveLog($"[Warning] MessageFunction::OnUpdateBulletin, MemberID:{packet.MemberID} Action:0");
                                }
                            }
                            else
                            {
                                rData.Result = (int)UpdateBulletinResult.ResultDefine.emResult_Fail;

                                SaveLog($"[Info] MessageFunction::OnUpdateBulletin, Can Not Find, BulletinID:{rData.BulletinID}");
                            }

                        }
                    }
                    else
                    {
                        rData.Result = (int)UpdateBulletinResult.ResultDefine.emResult_InsufficientPermissions;

                        SaveLog($"[Warning] MessageFunction::OnUpdateBulletin, {packet.MemberID} Not Leader or Vice Leader");
                    }

                }
                else
                {
                    rData.Result = (int)UpdateBulletinResult.ResultDefine.emResult_Fail;

                    SaveLog($"[Warning] MessageFunction::OnUpdateBulletin, Can Not Find Team:{packet.TeamID}");
                }

            }
            catch (Exception ex)
            {
                SaveLog($"[Error] MessageFunction::OnUpdateBulletin Catch Error, Msg:{ex.Message}");

                rData.Result = (int)UpdateBulletinResult.ResultDefine.emResult_Fail;
            }

            if (rData.Result == (int)UpdateBulletinResult.ResultDefine.emResult_Success)
            {
                if (newBulletin != null && rData.Action == (int)UpdateBulletin.ActionDefine.emResult_Add)
                {
                    redis.GetRedis((int)Connect.RedisDB.emRedisDB_Team).HashSet($"TeamBulletin_" + newBulletin.BulletinID, hashTransfer.TransToHashEntryArray(newBulletin));

                    redis.GetRedis((int)Connect.RedisDB.emRedisDB_Team).HashSet($"BulletinIdList_" + packet.TeamID, newBulletin.BulletinID, newBulletin.BulletinID);

                    List<string> notifyTargetList = GetAllMemberID(teamData);
                    notifyTargetList.Remove(packet.MemberID);

                    for (int idx = 0; idx < notifyTargetList.Count(); idx++)
                    {
                        string targetID = notifyTargetList[idx];

                        UserAccount account = db.GetSql().Queryable<UserAccount>().With(SqlSugar.SqlWith.RowLock).Where(it => it.MemberID == targetID).Single();

                        if (account != null)
                        {
                            string sTitle = $"車隊公告";

                            string sNotifyMsg = packet.Content;

                            ntMsg.NotifyMsgToDevice(targetID, account.NotifyToken, sTitle, sNotifyMsg);
                        }

                    }
                }
                else if (rData.Action == (int)UpdateBulletin.ActionDefine.emResult_Delete)
                {
                    if (redis.GetRedis((int)Connect.RedisDB.emRedisDB_Team).KeyExists($"TeamBulletin_" + packet.BulletinID))
                    {
                        redis.GetRedis((int)Connect.RedisDB.emRedisDB_Team).KeyDelete($"TeamBulletin_" + packet.BulletinID);

                        redis.GetRedis((int)Connect.RedisDB.emRedisDB_Team).HashDelete($"BulletinIdList_" + packet.TeamID, packet.BulletinID);
                    }
                }
                else if (bulletin != null && rData.Action == (int)UpdateBulletin.ActionDefine.emResult_Modify)
                {
                    redis.GetRedis((int)Connect.RedisDB.emRedisDB_Team).HashSet($"TeamBulletin_" + bulletin.BulletinID, hashTransfer.TransToHashEntryArray(bulletin));
                }
                else
                {
                    SaveLog($"[Error] MessageFunction::OnUpdateBulletin, Update Bulletin Success But Can Not Action Judge Fail Action:{rData.Action}");
                }
            }

            JObject jsMain = new JObject();
            jsMain.Add("CmdID", (int)S2C_CmdID.emUpdateBulletinResult);
            jsMain.Add("Data", JsonConvert.DeserializeObject<JObject>(JsonConvert.SerializeObject(rData)));

            ret = jsMain.ToString();

            return ret;
        }

        /**
         * 更新活動
         */
        public string OnUpdateActivity(string data)
        {
            string ret = "";

            UpdateActivity packet = JsonConvert.DeserializeObject<UpdateActivity>(data);

            UpdateActivityResult rData = new UpdateActivityResult();
            rData.Action = packet.Action;
            rData.ActID = packet.ActID;

            TeamData teamData = null;

            TeamActivity teamAct = null;

            TeamActivity newTeamAct = null;

            try
            {
                // 新增活動
                if (packet.Action == (int)UpdateActivity.ActionDefine.emResult_Add)
                {
                    teamData = db.GetSql().Queryable<TeamData>().With(SqlSugar.SqlWith.RowLock).Where(it => it.TeamID == packet.TeamID).Single();
                }
                // 修改活動 或 刪除活動
                else if (packet.Action == (int)UpdateActivity.ActionDefine.emResult_Modify 
                    || packet.Action == (int)UpdateActivity.ActionDefine.emResult_Delete)
                {
                    teamAct = db.GetSql().Queryable<TeamActivity>().With(SqlSugar.SqlWith.RowLock).Where(it => it.ActID == packet.ActID).Single();

                    // 有找到活動
                    if (teamAct != null)
                    {
                        teamData = db.GetSql().Queryable<TeamData>().With(SqlSugar.SqlWith.RowLock).Where(it => it.TeamID == packet.TeamID).Single();
                    }
                    else
                    {
                        rData.Result = (int)UpdateActivityResult.ResultDefine.emResult_Fail;

                        SaveLog($"[Info] MessageFunction::OnUpdateBulletin, Update Team Bulletin Fail, BulletinID:{rData.ActID}");
                    }
                }

                // 有找到車隊
                if (teamData != null)
                {
                    List<string> memberIdList = GetAllMemberID(teamData);

                    // 檢查發活動的人是否為車隊隊員
                    if (memberIdList.Contains(packet.MemberID))
                    {
                        // 新增活動
                        if (packet.Action == (int)UpdateActivity.ActionDefine.emResult_Add)
                        {
                            string dateTime = DateTime.UtcNow.ToString("yyyy-MM-dd hh:mm:ss");

                            string guidAll = Guid.NewGuid().ToString();

                            string[] guidList = guidAll.Split('-');

                            newTeamAct = new TeamActivity();
                            newTeamAct.ActID = "DbAct-" + guidList[0];        // 取GUID前8碼,
                            newTeamAct.CreateDate = dateTime;
                            newTeamAct.TeamID = packet.TeamID;
                            newTeamAct.MemberID = packet.MemberID;
                            newTeamAct.MemberList = packet.MemberList == null ? "[]" : packet.MemberList;
                            newTeamAct.ActDate = packet.ActDate;
                            newTeamAct.Title = packet.Title;
                            newTeamAct.MeetTime = packet.MeetTime;
                            newTeamAct.TotalDistance = packet.TotalDistance;
                            newTeamAct.MaxAltitude = packet.MaxAltitude;
                            newTeamAct.Route = packet.Route;

                            if (db.GetSql().Insertable(newTeamAct).With(SqlSugar.SqlWith.TabLockX).ExecuteCommand() > 0)
                            {
                                rData.Result = (int)UpdateActivityResult.ResultDefine.emResult_Success;

                                rData.ActID = newTeamAct.ActID;

                                SaveLog($"[Info] MessageFunction::OnUpdateActivity, Create Team Activity Success, ActID:{rData.ActID}");

                            }
                            else
                            {
                                rData.Result = (int)UpdateActivityResult.ResultDefine.emResult_Fail;

                                SaveLog($"[Warning] MessageFunction::OnUpdateActivity, Create Team Activity Fail, Data Base Can Not Inseart");
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
                                    if (packet.Action == (int)UpdateActivity.ActionDefine.emResult_Delete)
                                    {
                                        if (db.GetSql().Deleteable<TeamActivity>().With(SqlSugar.SqlWith.TabLockX).Where(it => it.ActID == packet.ActID).ExecuteCommand() > 0)
                                        {
                                            rData.Result = (int)UpdateActivityResult.ResultDefine.emResult_Success;

                                            SaveLog($"[Info] MessageFunction::OnUpdateActivity, Remove Team Activity Success, BulletinID:{rData.ActID}");
                                        }
                                        else
                                        {
                                            rData.Result = (int)UpdateActivityResult.ResultDefine.emResult_Fail;

                                            SaveLog($"[Info] MessageFunction::OnUpdateActivity, Remove Team Activity Fail, BulletinID:{rData.ActID}");
                                        }
                                    }
                                    // 修改活動
                                    else if (packet.Action == (int)UpdateActivity.ActionDefine.emResult_Modify)
                                    {

                                        teamAct.MemberList = packet.MemberList == null ? teamAct.MemberList : packet.MemberList;
                                        teamAct.ActDate = packet.ActDate == null ? teamAct.ActDate : packet.ActDate;
                                        teamAct.Title = packet.Title == null ? teamAct.Title : packet.Title;
                                        teamAct.MeetTime = packet.MeetTime == null ? teamAct.MeetTime : packet.MeetTime;
                                        teamAct.TotalDistance = packet.TotalDistance == 0 ? teamAct.TotalDistance : packet.TotalDistance;
                                        teamAct.MaxAltitude = packet.MaxAltitude == 0 ? teamAct.MaxAltitude : packet.MaxAltitude;
                                        teamAct.Route = packet.Route == null ? teamAct.Route : packet.Route;

                                        if (db.GetSql().Updateable<TeamActivity>(teamAct).With(SqlSugar.SqlWith.RowLock).Where(it => it.ActID == packet.ActID).ExecuteCommand() > 0)
                                        {
                                            rData.Result = (int)UpdateActivityResult.ResultDefine.emResult_Success;

                                            SaveLog($"[Info] MessageFunction::OnUpdateActivity, Update Team Activity Success, ActID:{rData.ActID}");

                                        }
                                        else
                                        {
                                            rData.Result = (int)UpdateActivityResult.ResultDefine.emResult_Fail;

                                            SaveLog($"[Info] MessageFunction::OnUpdateActivity, Update Team Activity Fail, ActID:{rData.ActID}");
                                        }

                                    }
                                    else
                                    {
                                        rData.Result = (int)UpdateActivityResult.ResultDefine.emResult_InsufficientPermissions;

                                        SaveLog($"[Info] MessageFunction::OnUpdateActivity, {packet.MemberID} Action: 0");

                                    }
                                }
                                else
                                {
                                    rData.Result = (int)UpdateActivityResult.ResultDefine.emResult_InsufficientPermissions;

                                    SaveLog($"[Info] MessageFunction::OnUpdateActivity, {packet.MemberID} Can Not Update Act:{packet.ActID}");
                                }
                            }
                            else
                            {
                                rData.Result = (int)UpdateActivityResult.ResultDefine.emResult_Fail;

                                SaveLog($"[Info] MessageFunction::OnUpdateActivity, Update Team Bulletin Fail, ActID:{rData.ActID}");

                            }

                        }

                    }
                    else
                    {
                        rData.Result = (int)UpdateActivityResult.ResultDefine.emResult_InsufficientPermissions;

                        SaveLog($"[Warning] MessageFunction::OnUpdateActivity, {packet.MemberID} Not Leader or Vice Leader");
                    }

                }
                else
                {
                    rData.Result = (int)UpdateActivityResult.ResultDefine.emResult_Fail;

                    SaveLog($"[Warning] MessageFunction::OnUpdateActivity, Can Not Find Team:{packet.TeamID}");
                }

            }
            catch (Exception ex)
            {
                SaveLog($"[Error] MessageFunction::OnUpdateActivity Catch Error, Msg:{ex.Message}");

                rData.Result = (int)UpdateActivityResult.ResultDefine.emResult_Fail;
            }

            if (rData.Result == (int)UpdateActivityResult.ResultDefine.emResult_Success)
            {
                if (newTeamAct != null && packet.Action == (int)UpdateActivity.ActionDefine.emResult_Add)
                {
                    redis.GetRedis((int)Connect.RedisDB.emRedisDB_Team).HashSet($"TeamActivity_" + newTeamAct.ActID, hashTransfer.TransToHashEntryArray(newTeamAct));

                    redis.GetRedis((int)Connect.RedisDB.emRedisDB_Team).HashSet($"ActIdList_" + packet.TeamID, newTeamAct.ActID, newTeamAct.ActID);

                    List<string> notifyTargetList = GetAllMemberID(teamData);
                    notifyTargetList.Remove(packet.MemberID);

                    for (int idx = 0; idx < notifyTargetList.Count(); idx++)
                    {
                        string targetID = notifyTargetList[idx];

                        // 活動發起人
                        UserInfo userInfo = db.GetSql().Queryable<UserInfo>().With(SqlSugar.SqlWith.RowLock).Where(it => it.MemberID == packet.MemberID).Single();

                        // 收到公告的人
                        UserAccount account = db.GetSql().Queryable<UserAccount>().With(SqlSugar.SqlWith.RowLock).Where(it => it.MemberID == targetID).Single();

                        if (userInfo != null && account != null)
                        {
                            string sTitle = $"車隊活動";

                            string sNotifyMsg = $"{userInfo.NickName} 建立活動 {packet.Title}";

                            ntMsg.NotifyMsgToDevice(targetID, account.NotifyToken, sTitle, sNotifyMsg);
                        }

                    }
                }
                else if (packet.Action == (int)UpdateActivity.ActionDefine.emResult_Delete)
                {
                    if (redis.GetRedis((int)Connect.RedisDB.emRedisDB_Team).KeyExists($"TeamActivity_" + packet.ActID))
                    {
                        redis.GetRedis((int)Connect.RedisDB.emRedisDB_Team).KeyDelete($"TeamActivity_" + packet.ActID);

                        redis.GetRedis((int)Connect.RedisDB.emRedisDB_Team).HashDelete($"ActIdList_" + packet.TeamID, packet.ActID);
                    }
                }
                else if (teamAct != null && packet.Action == (int)UpdateActivity.ActionDefine.emResult_Modify)
                {
                    redis.GetRedis((int)Connect.RedisDB.emRedisDB_Team).HashSet($"TeamActivity_" + teamAct.ActID, hashTransfer.TransToHashEntryArray(teamAct));

                    List<string> notifyTargetList = GetAllMemberID(teamData);
                    notifyTargetList.Remove(packet.MemberID);

                    for (int idx = 0; idx < notifyTargetList.Count(); idx++)
                    {
                        string targetID = notifyTargetList[idx];

                        // 收到公告的人
                        UserAccount account = db.GetSql().Queryable<UserAccount>().With(SqlSugar.SqlWith.RowLock).Where(it => it.MemberID == targetID).Single();

                        if (account != null)
                        {
                            string sTitle = $"車隊活動";

                            string sNotifyMsg = $"{teamAct.Title} 已更新內容";

                            ntMsg.NotifyMsgToDevice(targetID, account.NotifyToken, sTitle, sNotifyMsg);
                        }

                    }
                }
                else
                {
                    SaveLog($"[Error] MessageFunction::OnUpdateActivity, Update Activity Success But Can Not Action Judge Fail Action:{rData.Action}");
                }
            }

            JObject jsMain = new JObject();
            jsMain.Add("CmdID", (int)S2C_CmdID.emUpdateActivityResult);
            jsMain.Add("Data", JsonConvert.DeserializeObject<JObject>(JsonConvert.SerializeObject(rData)));

            ret = jsMain.ToString();

            return ret;
        }

        /**
         * 解散車隊
         */
        public string OnDeleteTeam(string data)
        {
            string ret = "";

            DeleteTeam packet = JsonConvert.DeserializeObject<DeleteTeam>(data);

            DeleteTeamResult rData = new DeleteTeamResult();

            try
            {
                TeamData teamData = db.GetSql().Queryable<TeamData>().With(SqlSugar.SqlWith.RowLock).Where(it => it.TeamID == packet.TeamID).Single();

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

                        // 設定DB 交易的起始點
                        db.GetSql().BeginTran();

                        if (db.GetSql().Insertable(stroge).With(SqlSugar.SqlWith.TabLockX).ExecuteCommand() > 0)
                        {
                            // 刪除車隊
                            if (db.GetSql().Deleteable<TeamData>().With(SqlSugar.SqlWith.TabLockX).Where(it => it.TeamID == packet.TeamID).ExecuteCommand() > 0)
                            {
                                redis.GetRedis((int)Connect.RedisDB.emRedisDB_Team).KeyDelete($"TeamData_" + packet.TeamID);

                                // 變更車隊成員的車隊列表
                                List<string> idList = GetAllMemberID(teamData);

                                for (int idx = 0; idx < idList.Count(); idx++)
                                {
                                    // 更新UserInfo的車隊資料
                                    UpdateUserTeamList(idList[idx], teamData.TeamID, -1);
                                }

                                rData.Result = (int)DeleteTeamResult.ResultDefine.emResult_Success;

                                // DB 交易提交
                                db.GetSql().CommitTran();

                                SaveLog($"[Info] MessageFunction::OnDeleteTeam, Remove Team:{packet.TeamID} Success");
                            }
                            else
                            {
                                rData.Result = (int)DeleteTeamResult.ResultDefine.emResult_Fail;

                                SaveLog($"[Info] MessageFunction::OnDeleteTeam, Remove Team:{packet.TeamID} Fail");
                            }
                        }
                        else
                        {
                            rData.Result = (int)DeleteTeamResult.ResultDefine.emResult_Fail;

                            SaveLog($"[Info] MessageFunction::OnDeleteTeam, Back Up Team Data Fail, Team ID:{packet.TeamID}");
                        }
                    }
                    else
                    {
                        rData.Result = (int)DeleteTeamResult.ResultDefine.emResult_InsufficientPermissions;

                        SaveLog($"[Warning] MessageFunction::OnDeleteTeam, {packet.MemberID} Not Leader, Can Not Delete Team");
                    }

                }
                else
                {
                    rData.Result = (int)DeleteTeamResult.ResultDefine.emResult_Fail;

                    SaveLog($"[Warning] MessageFunction::OnDeleteTeam, Can Not Find Team:{packet.TeamID}");
                }

            }
            catch (Exception ex)
            {
                SaveLog($"[Error] MessageFunction::OnDeleteTeam Catch Error, Msg:{ex.Message}");

                rData.Result = (int)DeleteTeamResult.ResultDefine.emResult_Fail;
            }

            // DB 交易失敗, 啟動Rollback
            if (rData.Result != (int)DeleteTeamResult.ResultDefine.emResult_Success)
            {
                db.GetSql().RollbackTran();
            }

            JObject jsMain = new JObject();
            jsMain.Add("CmdID", (int)S2C_CmdID.emDeleteTeamResult);
            jsMain.Add("Data", JsonConvert.DeserializeObject<JObject>(JsonConvert.SerializeObject(rData)));

            ret = jsMain.ToString();

            return ret;
        }

        /**
         * 加入或離開車隊活動
         */
        public string OnJoinOrLeaveTeamActivity(string data)
        {
            string ret = "";

            JoinOrLeaveTeamActivity packet = JsonConvert.DeserializeObject<JoinOrLeaveTeamActivity>(data);

            JoinOrLeaveTeamActivityResult rData = new JoinOrLeaveTeamActivityResult();
            rData.Action = packet.Action;

            TeamActivity teamAct = null;

            try
            {
                TeamData teamData = db.GetSql().Queryable<TeamData>().With(SqlSugar.SqlWith.RowLock).Where(it => it.TeamID == packet.TeamID).Single();

                // 有找到車隊
                if (teamData != null)
                {
                    teamAct = db.GetSql().Queryable<TeamActivity>().Where(it => it.ActID == packet.ActID).Single();

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
                            if (packet.Action == (int)JoinOrLeaveTeamActivity.ActionDefine.emResult_Delete)
                            {
                                if (actMemberList.Contains(packet.MemberID))
                                {
                                    actMemberList.Remove(packet.MemberID);

                                    rData.Result = (int)JoinOrLeaveTeamActivityResult.ResultDefine.emResult_Success;

                                    SaveLog($"[Info] MessageFunction::OnJoinOrLeaveTeamActivity, {packet.MemberID} Remove Team:{packet.TeamID} Activity:{packet.ActID} Success");
                                }
                                else
                                {
                                    rData.Result = (int)JoinOrLeaveTeamActivityResult.ResultDefine.emResult_Fail;

                                    SaveLog($"[Warning] MessageFunction::OnJoinOrLeaveTeamActivity, {packet.MemberID} Remove Team:{packet.TeamID} Activity:{packet.ActID} Fail, Can Not Find Member In Activity Member List");
                                }
                            }
                            // 加入
                            else if (packet.Action == (int)JoinOrLeaveTeamActivity.ActionDefine.emResult_Add)
                            {
                                if (!actMemberList.Contains(packet.MemberID))
                                {
                                    actMemberList.Add(packet.MemberID);

                                    rData.Result = (int)JoinOrLeaveTeamActivityResult.ResultDefine.emResult_Success;

                                    SaveLog($"[Info] MessageFunction::OnJoinOrLeaveTeamActivity, {packet.MemberID} Add To Team:{packet.TeamID} Activity:{packet.ActID} Success");
                                }
                                else
                                {
                                    rData.Result = (int)JoinOrLeaveTeamActivityResult.ResultDefine.emResult_Fail;

                                    SaveLog($"[Warning] MessageFunction::OnJoinOrLeaveTeamActivity, {packet.MemberID} Add To Team:{packet.TeamID} Activity:{packet.ActID} Fail, Member Repeat");
                                }
                            }
                            else
                            {
                                rData.Result = (int)JoinOrLeaveTeamActivityResult.ResultDefine.emResult_Fail;

                                SaveLog($"[Warning] MessageFunction::OnJoinOrLeaveTeamActivity, {packet.MemberID} Action = 0");
                            }

                            if (rData.Result == (int)JoinOrLeaveTeamActivityResult.ResultDefine.emResult_Success)
                            {
                                JArray jsNew = JArray.FromObject(actMemberList);

                                // 設定DB 交易的起始點
                                db.GetSql().BeginTran();

                                if (db.GetSql().Updateable<TeamActivity>().SetColumns(it => new TeamActivity() { MemberList = jsNew.ToString() }).With(SqlSugar.SqlWith.RowLock).Where(it => it.ActID == packet.ActID).ExecuteCommand() > 0)
                                {
                                    rData.Result = (int)JoinOrLeaveTeamActivityResult.ResultDefine.emResult_Success;

                                    teamAct.MemberList = jsNew.ToString();

                                    SaveLog($"[Info] MessageFunction::OnJoinOrLeaveTeamActivity, Upsate {packet.ActID} To Member List Success");

                                    // 若離開的人車隊發起人, 則解散活動
                                    if (packet.Action == -1 && teamAct.MemberID == packet.MemberID)
                                    {
                                        SaveLog($"[Info] MessageFunction::OnJoinOrLeaveTeamActivity, Leave Member: {packet.MemberID} is Activity Member: {teamAct.MemberID}");

                                        // 刪除活動
                                        if (db.GetSql().Deleteable<TeamActivity>().With(SqlSugar.SqlWith.TabLockX).Where(it => it.ActID == packet.ActID).ExecuteCommand() > 0)
                                        {
                                            rData.Result = (int)JoinOrLeaveTeamActivityResult.ResultDefine.emResult_Success;

                                            SaveLog($"[Info] MessageFunction::OnUpdateActivity, Remove Team Activity Success, BulletinID:{packet.ActID}");
                                        }
                                        else
                                        {
                                            rData.Result = (int)JoinOrLeaveTeamActivityResult.ResultDefine.emResult_Fail;

                                            SaveLog($"[Info] MessageFunction::OnUpdateActivity, Remove Team Activity Fail, BulletinID:{packet.ActID}");
                                        }

                                    }
                                }
                                else
                                {
                                    rData.Result = (int)JoinOrLeaveTeamActivityResult.ResultDefine.emResult_Fail;

                                    SaveLog($"[Warning] MessageFunction::OnJoinOrLeaveTeamActivity, Upsate {packet.ActID} To Member List Fail");
                                }
                            }
                        }
                        else
                        {
                            rData.Result = (int)JoinOrLeaveTeamActivityResult.ResultDefine.emResult_Fail;

                            SaveLog($"[Info] MessageFunction::OnJoinOrLeaveTeamActivity, {packet.MemberID} Not Team Member = 0");
                        }

                    }
                    else
                    {
                        rData.Result = (int)JoinOrLeaveTeamActivityResult.ResultDefine.emResult_Fail;

                        SaveLog($"[Warning] MessageFunction::OnDeleteTeam, Can Not Find Actovoty:{packet.ActID}");
                    }

                }
                else
                {
                    rData.Result = (int)JoinOrLeaveTeamActivityResult.ResultDefine.emResult_Fail;

                    SaveLog($"[Warning] MessageFunction::OnDeleteTeam, Can Not Find Team:{packet.TeamID}");
                }

            }
            catch (Exception ex)
            {
                SaveLog($"[Error] MessageFunction::OnDeleteTeam Catch Error, Msg:{ex.Message}");

                rData.Result = (int)JoinOrLeaveTeamActivityResult.ResultDefine.emResult_Fail;
            }

            if (teamAct != null && rData.Result == (int)JoinOrLeaveTeamActivityResult.ResultDefine.emResult_Success)
            {
                redis.GetRedis((int)Connect.RedisDB.emRedisDB_Team).HashSet($"TeamActivity_" + teamAct.ActID, hashTransfer.TransToHashEntryArray(teamAct));

                // DB 交易提交
                db.GetSql().CommitTran();
            }
            // DB 交易失敗, 啟動Rollback
            else
            {
                db.GetSql().RollbackTran();
            }

            JObject jsMain = new JObject();
            jsMain.Add("CmdID", (int)S2C_CmdID.emDeleteTeamResult);
            jsMain.Add("Data", JsonConvert.DeserializeObject<JObject>(JsonConvert.SerializeObject(rData)));

            ret = jsMain.ToString();

            return ret;
        }

        /**
         * 加入或離開車隊
         */
        public string OnJoinOrLeaveTeam(string data)
        {
            string ret = "";

            JoinOrLeaveTeam packet = JsonConvert.DeserializeObject<JoinOrLeaveTeam>(data);

            JoinOrLeaveTeamResult rData = new JoinOrLeaveTeamResult();
            rData.Action = packet.Action;

            TeamData teamData = null;

            UserInfo userInfo = null;

            try
            {
                teamData = db.GetSql().Queryable<TeamData>().With(SqlSugar.SqlWith.RowLock).Where(it => it.TeamID == packet.TeamID).Single();

                // 加入或離開的玩家資訊
                userInfo = db.GetSql().Queryable<UserInfo>().With(SqlSugar.SqlWith.RowLock).Where(it => it.MemberID == packet.MemberID).Single();

                // 有找到車隊
                if (teamData != null)
                {
                    // 設定DB 交易的起始點
                    db.GetSql().BeginTran();

                    if (packet.Action == (int)JoinOrLeaveTeam.ActionDefine.emResult_Add)
                    {
                        JArray jsApplyList = JArray.Parse(teamData.ApplyJoinList);
                        List<string> ApplyList = jsApplyList.ToObject<List<string>>();

                        // 若包含在申請加入
                        if (ApplyList.Contains(packet.MemberID))
                        {
                            ApplyList.Remove(packet.MemberID);

                            if (db.GetSql().Updateable<TeamData>().SetColumns(it => new TeamData() { ApplyJoinList = JArray.FromObject(ApplyList).ToString() }).With(SqlSugar.SqlWith.RowLock).Where(it => it.TeamID == packet.TeamID).ExecuteCommand() > 0)
                            {
                                rData.Result = (int)JoinOrLeaveTeamResult.ResultDefine.emResult_Success;

                                teamData.ApplyJoinList = JArray.FromObject(ApplyList).ToString();

                                SaveLog($"[Info] MessageFunction::OnJoinOrLeaveTeam, Remove Member:{packet.MemberID} From Team:{packet.TeamID}'s ApplyJoinList Success");
                            }
                            else
                            {
                                rData.Result = (int)JoinOrLeaveTeamResult.ResultDefine.emResult_Fail;

                                SaveLog($"[Info] MessageFunction::OnJoinOrLeaveTeam, Remove Member:{packet.MemberID} From Team:{packet.TeamID}'s ApplyJoinList Fail");
                            }

                        }
                        else
                        {
                            rData.Result = (int)JoinOrLeaveTeamResult.ResultDefine.emResult_Fail;

                            SaveLog($"[Info] MessageFunction::OnJoinOrLeaveTeam, Can Not Find Member:{packet.MemberID} From Team:{packet.TeamID}'s ApplyJoinList Or InviteJoinList");
                        }

                        if (rData.Result == (int)JoinOrLeaveTeamResult.ResultDefine.emResult_Success)
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

                                if (db.GetSql().Updateable<TeamData>().SetColumns(it => new TeamData() { TeamMemberIDs = jsNew.ToString() }).With(SqlSugar.SqlWith.RowLock).Where(it => it.TeamID == packet.TeamID).ExecuteCommand() > 0)
                                {
                                    teamData.TeamMemberIDs = jsNew.ToString();

                                    // 更新新加入會員的車隊列表
                                    if (UpdateUserTeamList(packet.MemberID, packet.TeamID, 1))
                                    {
                                        rData.Result = (int)JoinOrLeaveTeamResult.ResultDefine.emResult_Success;

                                        SaveLog($"[Info] MessageFunction::OnJoinOrLeaveTeam, Join Member:{packet.MemberID} To Team:{packet.TeamID}'s TeamMemberIDs Success");
                                    }
                                    else
                                    {
                                        rData.Result = (int)JoinOrLeaveTeamResult.ResultDefine.emResult_Fail;

                                        SaveLog($"[Info] MessageFunction::OnJoinOrLeaveTeam, Update User:{packet.MemberID} Team List Fail");
                                    }

                                }
                                else
                                {
                                    rData.Result = (int)JoinOrLeaveTeamResult.ResultDefine.emResult_Fail;

                                    SaveLog($"[Info] MessageFunction::OnJoinOrLeaveTeam, Join Member:{packet.MemberID} To Team:{packet.TeamID}'s TeamMemberIDs Fail");
                                }
                            }
                            else
                            {
                                rData.Result = (int)JoinOrLeaveTeamResult.ResultDefine.emResult_Fail;

                                SaveLog($"[Warning] MessageFunction::OnJoinOrLeaveTeam, Member:{packet.MemberID} Already Join Team");

                            }
                        }
                    }
                    else if (packet.Action == (int)JoinOrLeaveTeam.ActionDefine.emResult_Delete)
                    {
                        JArray jsMemberList = JArray.Parse(teamData.TeamMemberIDs);
                        List<string> MemberList = jsMemberList.ToObject<List<string>>();

                        // 車隊列表有該名會員
                        if (MemberList.Contains(packet.MemberID))
                        {
                            MemberList.Remove(packet.MemberID);

                            JArray jsNew = JArray.FromObject(MemberList);

                            if (db.GetSql().Updateable<TeamData>().SetColumns(it => new TeamData() { TeamMemberIDs = jsNew.ToString() }).With(SqlSugar.SqlWith.RowLock).Where(it => it.TeamID == packet.TeamID).ExecuteCommand() > 0)
                            {
                                rData.Result = (int)JoinOrLeaveTeamResult.ResultDefine.emResult_Success;

                                teamData.TeamMemberIDs = jsNew.ToString();

                                // 更新新加入會員的車隊列表
                                if (UpdateUserTeamList(packet.MemberID, packet.TeamID, -1))
                                {
                                    rData.Result = (int)JoinOrLeaveTeamResult.ResultDefine.emResult_Success;

                                    SaveLog($"[Info] MessageFunction::OnJoinOrLeaveTeam, Remove Member:{packet.MemberID} To Team:{packet.TeamID}'s TeamMemberIDs Success");
                                }
                                else
                                {
                                    rData.Result = (int)JoinOrLeaveTeamResult.ResultDefine.emResult_Fail;

                                    SaveLog($"[Info] MessageFunction::OnJoinOrLeaveTeam, Update User:{packet.MemberID} Team List Fail");
                                }
                            }
                            else
                            {
                                rData.Result = (int)JoinOrLeaveTeamResult.ResultDefine.emResult_Fail;

                                SaveLog($"[Info] MessageFunction::OnJoinOrLeaveTeam, Remove Member:{packet.MemberID} To Team:{packet.TeamID}'s TeamMemberIDs Fail");
                            }
                        }
                        else
                        {
                            rData.Result = (int)JoinOrLeaveTeamResult.ResultDefine.emResult_Fail;

                            SaveLog($"[Warning] MessageFunction::OnJoinOrLeaveTeam, Can Not Find Member:{packet.MemberID}");
                        }
                    }
                    else
                    {
                        SaveLog($"[Warning] MessageFunction::OnJoinOrLeaveTeam, Join Member:{packet.MemberID} To Team:{packet.TeamID}'s TeamMemberIDs Fail");
                    }

                }
                else
                {
                    rData.Result = (int)JoinOrLeaveTeamResult.ResultDefine.emResult_Fail;

                    SaveLog($"[Warning] MessageFunction::OnJoinOrLeaveTeam, Can Not Find Team:{packet.TeamID}");
                }

            }
            catch (Exception ex)
            {
                SaveLog($"[Error] MessageFunction::OnJoinOrLeaveTeam Catch Error, Msg:{ex.Message}");

                rData.Result = (int)JoinOrLeaveTeamResult.ResultDefine.emResult_Fail;
            }

            if (rData.Result == (int)JoinOrLeaveTeamResult.ResultDefine.emResult_Success)
            {
                // DB 交易提交
                db.GetSql().CommitTran();

                redis.GetRedis((int)Connect.RedisDB.emRedisDB_Team).HashSet($"TeamData_" + teamData.TeamID, hashTransfer.TransToHashEntryArray(teamData));

                if (packet.Action == (int)JoinOrLeaveTeam.ActionDefine.emResult_Add)
                {

                    List<string> notifyTargetList = GetAllMemberID(teamData);
                    notifyTargetList.Remove(packet.MemberID);

                    string userNickName = userInfo != null ? userInfo.NickName : "未定義";

                    for (int idx = 0; idx < notifyTargetList.Count(); idx++)
                    {
                        string targetID = notifyTargetList[idx];

                        // 收到公告的人
                        UserAccount account = db.GetSql().Queryable<UserAccount>().With(SqlSugar.SqlWith.RowLock).Where(it => it.MemberID == targetID).Single();

                        if (account != null)
                        {
                            string sTitle = $"車隊公告";

                            string sNotifyMsg = $"{userNickName} 加入 {teamData.TeamName}";

                            if (targetID == packet.MemberID)
                            {
                                sTitle = $"系統公告";

                                sNotifyMsg = $"您已加入車隊: {teamData.TeamName}";
                            }

                            ntMsg.NotifyMsgToDevice(targetID, account.NotifyToken, sTitle, sNotifyMsg);
                        }

                    }
                }
                else if (teamData != null && packet.Action == (int)JoinOrLeaveTeam.ActionDefine.emResult_Delete)
                {
                    List<string> notifyTargetList = GetAllMemberID(teamData);
                    notifyTargetList.Add(packet.MemberID);

                    string userNickName = userInfo != null ? userInfo.NickName : "未定義";

                    for (int idx = 0; idx < notifyTargetList.Count(); idx++)
                    {
                        string targetID = notifyTargetList[idx];

                        // 收到公告的人
                        UserAccount account = db.GetSql().Queryable<UserAccount>().With(SqlSugar.SqlWith.RowLock).Where(it => it.MemberID == targetID).Single();

                        if (account != null)
                        {
                            string sTitle = $"車隊公告";

                            string sNotifyMsg = $"{userNickName} 離開 {teamData.TeamName}";

                            if (targetID == packet.MemberID)
                            {
                                sTitle = $"系統公告";

                                sNotifyMsg = $"您已離開車隊: {teamData.TeamName}";
                            }

                            ntMsg.NotifyMsgToDevice(targetID, account.NotifyToken, sTitle, sNotifyMsg);
                        }

                    }
                }
            }
            else
            {
                // DB 交易失敗, 啟動Rollback
                db.GetSql().RollbackTran();
            }

            JObject jsMain = new JObject();
            jsMain.Add("CmdID", (int)S2C_CmdID.emJoinOrLeaveTeamResult);
            jsMain.Add("Data", JsonConvert.DeserializeObject<JObject>(JsonConvert.SerializeObject(rData)));

            ret = jsMain.ToString();

            return ret;
        }

        /**
         * 踢離車隊成員
         */
        public string OnKickTeamMember(string data)
        {
            string ret = "";

            KickTeamMember packet = JsonConvert.DeserializeObject<KickTeamMember>(data);

            KickTeamMemberResult rData = new KickTeamMemberResult();

            TeamData teamData = null;

            JArray jsData = JArray.Parse(packet.KickIdList);
            List<string> idList = jsData.ToObject<List<string>>();

            try
            {
                teamData = db.GetSql().Queryable<TeamData>().With(SqlSugar.SqlWith.RowLock).Where(it => it.TeamID == packet.TeamID).Single();

                // 有找到車隊
                if (teamData != null)
                {
                    JArray jsViceLeaderList = JArray.Parse(teamData.TeamViceLeaderIDs);
                    List<string> viceLeaderList = jsViceLeaderList.ToObject<List<string>>();

                    JArray jsMemberList = JArray.Parse(teamData.TeamMemberIDs);
                    List<string> memberList = jsMemberList.ToObject<List<string>>();

                    // 踢人的人為隊長
                    if (packet.MemberID == teamData.Leader)
                    {
                        bool checkSuccess = true;

                        // 先做列表檢查
                        for (int idx = 0; idx < idList.Count(); idx++)
                        {
                            // 被踢成員若為隊長 或 自己踢自己 或被踢的人非車隊成員
                            if (idList[idx] == teamData.Leader
                                || idList[idx] == packet.MemberID
                                || (!viceLeaderList.Contains(idList[idx]) && !memberList.Contains(idList[idx]) && idList[idx] != teamData.Leader))
                            {
                                checkSuccess = false;

                                SaveLog($"[Warning] MessageFunction::OnKickTeamMember, Kick Member:{idList[idx]} Fail");

                                break;
                            }
                        }

                        // 檢查成功
                        if (checkSuccess)
                        {
                            // 設定DB 交易的起始點
                            db.GetSql().BeginTran();

                            for (int idx = 0; idx < idList.Count(); idx++)
                            {
                                if (viceLeaderList.Contains(idList[idx]))
                                {
                                    viceLeaderList.Remove(idList[idx]);

                                    // 更新新加入會員的車隊列表
                                    if (UpdateUserTeamList(idList[idx], packet.TeamID, -1))
                                    {
                                        rData.Result = (int)KickTeamMemberResult.ResultDefine.emResult_Success;

                                        SaveLog($"[Info] MessageFunction::OnKickTeamMember, Remove Member:{idList[idx]} From Team:{packet.TeamID}'s TeamMemberIDs Success");
                                    }
                                    else
                                    {
                                        rData.Result = (int)KickTeamMemberResult.ResultDefine.emResult_Fail;

                                        SaveLog($"[Info] MessageFunction::OnKickTeamMember, Update User:{idList[idx]} Team List Fail");

                                        break;
                                    }
                                }
                                else if (memberList.Contains(idList[idx]))
                                {
                                    memberList.Remove(idList[idx]);

                                    // 更新新加入會員的車隊列表
                                    if (UpdateUserTeamList(idList[idx], packet.TeamID, -1))
                                    {
                                        rData.Result = (int)KickTeamMemberResult.ResultDefine.emResult_Success;

                                        SaveLog($"[Info] MessageFunction::OnKickTeamMember, Remove Member:{idList[idx]} From Team:{packet.TeamID}'s TeamMemberIDs Success");
                                    }
                                    else
                                    {
                                        rData.Result = (int)KickTeamMemberResult.ResultDefine.emResult_Fail;

                                        SaveLog($"[Info] MessageFunction::OnKickTeamMember, Update User:{idList[idx]} Team List Fail");

                                        break;
                                    }
                                }
                                else
                                {
                                    SaveLog($"[Warning] MessageFunction::OnKickTeamMember, Can Not Find Member:{idList[idx]}");
                                }
                            }

                            teamData.TeamViceLeaderIDs = JArray.FromObject(viceLeaderList).ToString();
                            teamData.TeamMemberIDs = JArray.FromObject(memberList).ToString();

                            if (db.GetSql().Updateable<TeamData>(teamData).With(SqlSugar.SqlWith.RowLock).Where(it => it.TeamID == packet.TeamID).ExecuteCommand() > 0)
                            {
                                rData.Result = (int)KickTeamMemberResult.ResultDefine.emResult_Success;
                            }
                            else
                            {
                                rData.Result = (int)KickTeamMemberResult.ResultDefine.emResult_Fail;

                                SaveLog($"[Warning] MessageFunction::OnKickTeamMember, Update Team: {teamData.TeamID} Data Fail");
                            }
                        }
                        else
                        {
                            rData.Result = (int)KickTeamMemberResult.ResultDefine.emResult_Fail;

                            SaveLog($"[Warning] MessageFunction::OnKickTeamMember, Check Kick List Fail");
                        }

                    }
                    else
                    {
                        rData.Result = (int)KickTeamMemberResult.ResultDefine.emResult_InsufficientPermissions;

                        SaveLog($"[Warning] MessageFunction::OnKickTeamMember, Member:{packet.MemberID} Not Leader Or Vice Leader");

                    }
                }
                else
                {
                    rData.Result = (int)KickTeamMemberResult.ResultDefine.emResult_Fail;

                    SaveLog($"[Warning] MessageFunction::OnKickTeamMember, Can Not Find Team:{packet.TeamID}");
                }
            }
            catch (Exception ex)
            {
                SaveLog("[Error] MessageFunction::OnKickTeamMember Catch Error, Msg:" + ex.Message);

                rData.Result = (int)KickTeamMemberResult.ResultDefine.emResult_Fail;
            }

            if (rData.Result == (int)KickTeamMemberResult.ResultDefine.emResult_Success)
            {
                // DB 交易提交
                db.GetSql().CommitTran();

                redis.GetRedis((int)Connect.RedisDB.emRedisDB_Team).HashSet($"TeamData_" + teamData.TeamID, hashTransfer.TransToHashEntryArray(teamData));

                for (int idx = 0; idx < idList.Count(); idx++)
                {
                    string targetID = idList[idx];

                    // 收到公告的人
                    UserInfo info = db.GetSql().Queryable<UserInfo>().With(SqlSugar.SqlWith.RowLock).Where(it => it.MemberID == targetID).Single();

                    UserAccount account = db.GetSql().Queryable<UserAccount>().With(SqlSugar.SqlWith.RowLock).Where(it => it.MemberID == targetID).Single();

                    if (info!= null && account != null)
                    {
                        string sTitle = $"車隊公告";

                        string sNotifyMsg = $"您已離開車隊: {teamData.TeamName}";

                        ntMsg.NotifyMsgToDevice(targetID, account.NotifyToken, sTitle, sNotifyMsg);
                    }
                }
                
            }
            else
            {
                // DB 交易失敗, 啟動Rollback
                db.GetSql().RollbackTran();
            }

            JObject jsMain = new JObject();
            jsMain.Add("CmdID", (int)S2C_CmdID.emKickTeamMemberResult);
            jsMain.Add("Data", JsonConvert.DeserializeObject<JObject>(JsonConvert.SerializeObject(rData)));

            ret = jsMain.ToString();

            return ret;
        }

    }

}
