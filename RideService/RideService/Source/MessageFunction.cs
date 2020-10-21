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

using RidePacket.ClientToServer;
using RidePacket.ServerToClient;

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
         * 建立騎乘紀錄
         */
        public string OnCreateRideRecord(string data)
        {
            string ret = "";

            CreateRideRecord packet = JsonConvert.DeserializeObject<CreateRideRecord>(data);

            CreateRideRecordResult rData = new CreateRideRecordResult();

            try
            {
                UserAccount account = db.GetSql().Queryable<UserAccount>().With(SqlSugar.SqlWith.RowLock).Where(it => it.MemberID == packet.MemberID).Single();

                RideData rideData = db.GetSql().Queryable<RideData>().With(SqlSugar.SqlWith.RowLock).Where(it => it.MemberID == packet.MemberID).Single();

                // 有找到帳號 且 有找到 騎乘資料
                if (account != null && rideData != null)
                {
                    string dateTime = DateTime.UtcNow.ToString("yyyy-MM-dd hh:mm:ss");

                    string guidAll = Guid.NewGuid().ToString();

                    string[] guidList = guidAll.Split('-');

                    // ======================= 新增騎乘紀錄 =======================
                    RideRecord newRecord = new RideRecord
                    {
                        RideID = "DbRr-" + guidList[0] + "-" + DateTime.UtcNow.ToString("MMdd-hhmmss"),
                        MemberID = packet.MemberID,
                        CreateDate = dateTime,
                        Title = packet.Title,
                        Photo = packet.Photo,
                        Time = packet.Time,
                        Distance = packet.Distance,
                        Altitude = packet.Altitude,
                        Level = packet.Level,
                        County = packet.County,
                        Route = packet.Route,
                        ShareContent = packet.ShareContent,
                        SharedType = packet.SharedType
                    };

                    // 設定DB 交易的起始點
                    db.GetSql().BeginTran();

                    if (db.GetSql().Insertable(newRecord).With(SqlSugar.SqlWith.TabLockX).ExecuteCommand() > 0)
                    {
                        rData.Result = (int)CreateRideRecordResult.ResultDefine.emResult_Success;

                        SaveLog($"[Info] Controller::OnCreateRideRecord Create {packet.MemberID}'s Ride Record:{newRecord.RideID} Success");


                        // ======================= 更新騎乘資料 =======================
                        if (rData.Result == (int)CreateRideRecordResult.ResultDefine.emResult_Success)
                        {
                            rideData.TotalDistance += packet.Distance;
                            rideData.TotalAltitude += packet.Altitude;
                            rideData.TotalRideTime += packet.Time;

                            if (db.GetSql().Updateable<RideData>(rideData).With(SqlSugar.SqlWith.RowLock).Where(it => it.MemberID == packet.MemberID).ExecuteCommand() > 0)
                            {
                                rData.Result = (int)CreateRideRecordResult.ResultDefine.emResult_Success;

                                SaveLog($"[Info] Controller::OnCreateRideRecord Update MemberID:{packet.MemberID}'s Ride Data Success");
                            }
                            else
                            {
                                rData.Result = (int)CreateRideRecordResult.ResultDefine.emResult_Fail;

                                SaveLog($"[Warning] Controller::OnCreateRideRecord Update MemberID:{packet.MemberID}'s Ride Data Fail");
                            }
                        }

                        // ======================= 更新周騎乘資料 =======================
                        string firsDay = weekProcess.GetWeekFirstDay(DateTime.UtcNow);
                        string lastDay = weekProcess.GetWeekLastDay(DateTime.UtcNow);

                        WeekRideData curWeekRideData = db.GetSql().Queryable<WeekRideData>().With(SqlSugar.SqlWith.RowLock).Where(it => it.MemberID == packet.MemberID && it.WeekFirstDay == firsDay && it.WeekLastDay == lastDay).Single();

                        if (rData.Result == (int)CreateRideRecordResult.ResultDefine.emResult_Success)
                        {
                            // 有找到資料
                            if (curWeekRideData != null)
                            {
                                curWeekRideData.WeekDistance += packet.Distance;

                                if (db.GetSql().Updateable<WeekRideData>(curWeekRideData).With(SqlSugar.SqlWith.RowLock).Where(it => it.MemberID == packet.MemberID && it.WeekFirstDay == firsDay && it.WeekLastDay == lastDay).ExecuteCommand() > 0)
                                {
                                    rData.Result = (int)CreateRideRecordResult.ResultDefine.emResult_Success;

                                    SaveLog($"[Info] Controller::ODnCreateRideRecord Update MemberID:{packet.MemberID}'s Week({firsDay} To {lastDay}) Ride Data Success");
                                }
                                else
                                {
                                    rData.Result = (int)CreateRideRecordResult.ResultDefine.emResult_Fail;

                                    SaveLog($"[Warning] Controller::ODnCreateRideRecord Update MemberID:{packet.MemberID}'s Week({firsDay} To {lastDay}) Ride Data Fail");
                                }

                            }
                            else
                            {
                                WeekRideData updateWeek = new WeekRideData
                                {
                                    MemberID = packet.MemberID,
                                    WeekFirstDay = firsDay,
                                    WeekLastDay = lastDay,
                                    WeekDistance = packet.Distance
                                };

                                if (db.GetSql().Insertable(updateWeek).With(SqlSugar.SqlWith.TabLockX).ExecuteCommand() > 0)
                                {
                                    rData.Result = (int)CreateRideRecordResult.ResultDefine.emResult_Success;

                                    SaveLog($"[Info] Controller::ODnCreateRideRecord Create MemberID:{packet.MemberID}'s Week({firsDay} To {lastDay}) Ride Data Success");

                                }
                                else
                                {
                                    rData.Result = (int)CreateRideRecordResult.ResultDefine.emResult_Fail;

                                    SaveLog($"[Warning] Controller::ODnCreateRideRecord Create MemberID:{packet.MemberID}'s Week({firsDay} To {lastDay}) Ride Data Fail");
                                }
                            }
                        }

                        if (rData.Result == (int)CreateRideRecordResult.ResultDefine.emResult_Success)
                        {
                            redis.GetRedis((int)Connect.RedisDB.emRedisDB_Ride).HashSet($"RideRecord_" + newRecord.RideID, hashTransfer.TransToHashEntryArray(newRecord));
                            redis.GetRedis((int)Connect.RedisDB.emRedisDB_Ride).HashSet($"RideIdList_" + packet.MemberID, newRecord.RideID, newRecord.RideID);
                            redis.GetRedis((int)Connect.RedisDB.emRedisDB_Ride).HashSet($"RideData_" + packet.MemberID, hashTransfer.TransToHashEntryArray(rideData));

                            if (curWeekRideData != null)
                            {
                                redis.GetRedis((int)Connect.RedisDB.emRedisDB_Ride).HashSet($"CurWeekRideData_" + packet.MemberID, hashTransfer.TransToHashEntryArray(curWeekRideData));
                            }
                            else
                            {
                                var lastWeekRideData = redis.GetRedis((int)Connect.RedisDB.emRedisDB_Ride).HashGetAll($"LastWeekRideData_" + packet.MemberID);

                                redis.GetRedis((int)Connect.RedisDB.emRedisDB_Ride).HashSet($"LastWeekRideData_" + packet.MemberID, lastWeekRideData);

                                redis.GetRedis((int)Connect.RedisDB.emRedisDB_Ride).HashSet($"CurWeekRideData_" + packet.MemberID, hashTransfer.TransToHashEntryArray(curWeekRideData));
                            }

                            rData.TotalDistance = rideData.TotalDistance;
                            rData.TotalAltitude = rideData.TotalAltitude;
                            rData.TotalRideTime = rideData.TotalRideTime;

                            // DB 交易提交
                            db.GetSql().CommitTran();
                        }

                    }
                    else
                    {
                        rData.Result = (int)CreateRideRecordResult.ResultDefine.emResult_Fail;
                    }

                }
                else
                {
                    rData.Result = (int)CreateRideRecordResult.ResultDefine.emResult_Fail;
                }
            }
            catch (Exception ex)
            {
                SaveLog($"[Error] Controller::OnCreateRideRecord Catch Error, Msg:{ex.Message}");

                rData.Result = (int)CreateRideRecordResult.ResultDefine.emResult_Fail;
            }

            // DB 交易失敗, 啟動Rollback
            if (rData.Result != (int)CreateRideRecordResult.ResultDefine.emResult_Success)
            {
                db.GetSql().RollbackTran();
            }

            JObject jsMain = new JObject();
            jsMain.Add("CmdID", (int)S2C_CmdID.emCreateRideRecordResult);
            jsMain.Add("Data", JsonConvert.DeserializeObject<JObject>(JsonConvert.SerializeObject(rData)));

            ret = jsMain.ToString();

            return ret;
        }

        /**
         * 更新組隊騎乘
         */
        public string OnUpdateRideGroup(string data)
        {
            string ret = "";

            UpdateRideGroup packet = JsonConvert.DeserializeObject<UpdateRideGroup>(data);

            UpdateRideGroupResult rData = new UpdateRideGroupResult();

            try
            {
                UserAccount account = db.GetSql().Queryable<UserAccount>().With(SqlSugar.SqlWith.RowLock).Where(it => it.MemberID == packet.MemberID).Single();
                UserInfo userInfo = db.GetSql().Queryable<UserInfo>().With(SqlSugar.SqlWith.RowLock).Where(it => it.MemberID == packet.MemberID).Single();

                // 有找到帳號
                if (account != null && userInfo != null)
                {
                    string sRideGroupKey = $"RideGroup_{packet.MemberID}";

                    if (packet.Action == (int)UpdateRideGroup.ActionDefine.emAction_Add)
                    {
                        // 該組隊不存在
                        if (!redis.GetRedis(0).KeyExists(sRideGroupKey))
                        {
                            JArray jsInviteList = JArray.Parse(packet.InviteList);
                            List<string> InviteList = jsInviteList.ToObject<List<string>>();

                            List<string> MemberList = new List<string>();
                            MemberList.Add(packet.MemberID);

                            JObject jsGroupData = new JObject();
                            jsGroupData.Add("Leader", packet.MemberID);
                            jsGroupData.Add("InviteList", jsInviteList);
                            jsGroupData.Add("MemberList", JArray.FromObject(MemberList).ToString());

                            if (redis.GetRedis(0).StringSet(sRideGroupKey, jsGroupData.ToString()))
                            {
                                rData.Result = (int)UpdateRideGroupResult.ResultDefine.emResult_Success;

                                SaveLog($"[Info] Controller::OnUpdateRideGroup Create Ride Group: {sRideGroupKey} Success");

                                for (int idx = 0; idx < InviteList.Count(); idx++)
                                {
                                    string sMemberKey = $"GroupMember_{InviteList[idx]}";

                                    JObject jsMemberData = new JObject();
                                    jsMemberData.Add("RideGroupKey", sRideGroupKey);
                                    jsMemberData.Add("CoordinateX", "");
                                    jsMemberData.Add("CoordinateY", "");

                                    // 建立成員的Redis資料
                                    if (redis.GetRedis(0).StringSet(sMemberKey, jsMemberData.ToString()))
                                    {
                                        SaveLog($"[Info] Controller::OnUpdateRideGroup Create Ride Group: {sRideGroupKey}'s Member:{InviteList[idx]} Success");
                                    }
                                    else
                                    {
                                        SaveLog($"[Info] Controller::OnUpdateRideGroup Create Ride Group: {sRideGroupKey}'s Member:{InviteList[idx]} Fail");
                                    }

                                    UserAccount notifyAccount = db.GetSql().Queryable<UserAccount>().With(SqlSugar.SqlWith.RowLock).Where(it => it.MemberID == InviteList[idx]).Single();
                                    UserInfo notifyUserInfo = db.GetSql().Queryable<UserInfo>().With(SqlSugar.SqlWith.RowLock).Where(it => it.MemberID == InviteList[idx]).Single();

                                    // 有找對會員
                                    if (notifyAccount != null && notifyUserInfo != null)
                                    {
                                        // 發送推播通知
                                        string sTitle = $"騎乘邀請";

                                        string sNotifyMsg = $"{notifyUserInfo.NickName} 邀請您組隊";

                                        ntMsg.NotifyMsgToDevice(notifyAccount.NotifyToken, sTitle, sNotifyMsg);
                                    }
                                    else
                                    {
                                        SaveLog($"[Warning] Controller::OnUpdateRideGroup Can Not Find Notify Member: {account.MemberID}");
                                    }


                                }

                            }
                            else
                            {
                                rData.Result = (int)UpdateRideGroupResult.ResultDefine.emResult_Fail;

                                SaveLog($"[Warning] Controller::OnUpdateRideGroup Create Ride Group: {sRideGroupKey} Fail");
                            }

                        }
                        else
                        {
                            rData.Result = (int)UpdateRideGroupResult.ResultDefine.emResult_RideGroupRepeat;

                            SaveLog($"[Warning] Controller::OnUpdateRideGroup Create {packet.MemberID}'s Ride Group, The Group Repeat");
                        }
                    }
                    else if (packet.Action == (int)UpdateRideGroup.ActionDefine.emAction_Delete)
                    {
                        // 該組隊存在
                        if (redis.GetRedis(0).KeyExists(sRideGroupKey))
                        {
                            // 刪除Redis資料
                            if (redis.GetRedis(0).KeyDelete(sRideGroupKey))
                            {
                                rData.Result = (int)UpdateRideGroupResult.ResultDefine.emResult_Success;

                                SaveLog($"[Info] Controller::OnUpdateRideGroup Remove Ride Group: {sRideGroupKey} Success");
                            }
                            else
                            {
                                rData.Result = (int)UpdateRideGroupResult.ResultDefine.emResult_Fail;

                                SaveLog($"[Warning] Controller::OnUpdateRideGroup Remove Ride Group: {sRideGroupKey} Fail");
                            }
                        }
                        else
                        {
                            rData.Result = (int)UpdateRideGroupResult.ResultDefine.emResult_Fail;

                            SaveLog($"[Warning] Controller::OnUpdateRideGroup Can Not Find Ride Group: {sRideGroupKey}");
                        }
                    }
                    else
                    {
                        rData.Result = (int)UpdateRideGroupResult.ResultDefine.emResult_Fail;

                        SaveLog($"[Warning] Controller::OnUpdateRideGroup MemberID:{packet.MemberID}'s Action: 0");
                    }

                }
                else
                {
                    rData.Result = (int)UpdateRideGroupResult.ResultDefine.emResult_Fail;

                    SaveLog($"[Warning] Controller::OnUpdateRideGroup Can Not Find MemberID:{packet.MemberID}");

                }
            }
            catch (Exception ex)
            {
                SaveLog($"[Error] Controller::OnUpdateRideGroup Catch Error, Msg:{ex.Message}");

                rData.Result = (int)UpdateRideGroupResult.ResultDefine.emResult_Fail;
            }

            JObject jsMain = new JObject();
            jsMain.Add("CmdID", (int)S2C_CmdID.emUpdateRideGroupResult);
            jsMain.Add("Data", JsonConvert.DeserializeObject<JObject>(JsonConvert.SerializeObject(rData)));

            ret = jsMain.ToString();

            return ret;
        }

        /**
         * 回覆組隊騎乘
         */
        public string OnReplyRideGroup(string data)
        {
            string ret = "";

            ReplyRideGroup packet = JsonConvert.DeserializeObject<ReplyRideGroup>(data);

            ReplyRideGroupResult rData = new ReplyRideGroupResult();

            UserAccount leaderAccount = null;
            UserInfo leaderUserInfo = null;

            string replyMemberNickName = "";

            try
            {
                UserAccount account = db.GetSql().Queryable<UserAccount>().With(SqlSugar.SqlWith.RowLock).Where(it => it.MemberID == packet.MemberID).Single();
                UserInfo userInfo = db.GetSql().Queryable<UserInfo>().With(SqlSugar.SqlWith.RowLock).Where(it => it.MemberID == packet.MemberID).Single();

                // 有找到帳號
                if (account != null && userInfo != null)
                {
                    string sMemberKey = $"GroupMember_{packet.MemberID}";

                    replyMemberNickName = userInfo.NickName;

                    // 該組隊成員存在
                    if (redis.GetRedis(0).KeyExists(sMemberKey))
                    {
                        if (packet.Action == (int)ReplyRideGroup.ActionDefine.emAction_Add)
                        {
                            string MemberData = redis.GetRedis(0).StringGet(sMemberKey);

                            JObject jsMemberData = JObject.Parse(MemberData);

                            if (jsMemberData.ContainsKey("RideGroupKey"))
                            {
                                string sRideGroupKey = jsMemberData["RideGroupKey"].ToString();

                                if (redis.GetRedis(0).KeyExists(sRideGroupKey))
                                {
                                    string GroupData = redis.GetRedis(0).StringGet(sRideGroupKey);

                                    leaderAccount = db.GetSql().Queryable<UserAccount>().With(SqlSugar.SqlWith.RowLock).Where(it => it.MemberID == packet.MemberID).Single();
                                    leaderUserInfo = db.GetSql().Queryable<UserInfo>().With(SqlSugar.SqlWith.RowLock).Where(it => it.MemberID == packet.MemberID).Single();

                                    JObject jsGroupData = JObject.Parse(GroupData);

                                    JArray jsInviteList = JArray.Parse(jsGroupData["InviteList"].ToString());
                                    List<string> InviteList = jsInviteList.ToObject<List<string>>();

                                    if (InviteList.Contains(packet.MemberID))
                                    {
                                        InviteList.Remove(packet.MemberID);

                                        JArray jsMemberList = JArray.Parse(jsGroupData["MemberList"].ToString());
                                        List<string> MemberList = jsMemberList.ToObject<List<string>>();

                                        if (!MemberList.Contains(packet.MemberID))
                                        {
                                            MemberList.Add(packet.MemberID);

                                            jsGroupData["InviteList"] = JArray.FromObject(InviteList);
                                            jsGroupData["MemberList"] = JArray.FromObject(MemberList);

                                            GroupData = jsGroupData.ToString();

                                            if (redis.GetRedis(0).StringSet(sRideGroupKey, GroupData))
                                            {
                                                rData.Result = (int)ReplyRideGroupResult.ResultDefine.emResult_Success;

                                                SaveLog($"[Info] Controller::OnReplyRideGroup, Member:{packet.MemberID} Join Ride Group: {sRideGroupKey} Success");
                                            }
                                            else
                                            {
                                                rData.Result = (int)ReplyRideGroupResult.ResultDefine.emResult_Fail;

                                                SaveLog($"[Warning] Controller::OnReplyRideGroup, Member:{packet.MemberID} Join Ride Group: {sRideGroupKey} Fail");
                                            }
                                        }
                                        else
                                        {
                                            rData.Result = (int)ReplyRideGroupResult.ResultDefine.emResult_Fail;

                                            SaveLog($"[Warning] Controller::OnReplyRideGroup, Member:{packet.MemberID} already is Member");

                                        }

                                    }
                                    else
                                    {
                                        rData.Result = (int)ReplyRideGroupResult.ResultDefine.emResult_Fail;

                                        SaveLog($"[Warning] Controller::OnReplyRideGroup, Invite List Cant Not Find Member:{packet.MemberID}");

                                    }
                                }
                                else
                                {
                                    rData.Result = (int)ReplyRideGroupResult.ResultDefine.emResult_Fail;

                                    SaveLog($"[Warning] Controller::OnReplyRideGroup, Can Not Find Ride Group: {sRideGroupKey}");
                                }
                            }
                            else
                            {
                                rData.Result = (int)ReplyRideGroupResult.ResultDefine.emResult_Fail;

                                SaveLog($"[Warning] Controller::OnReplyRideGroup, Can Not Find Json Ride Group Key");

                            }

                        }
                        else if (packet.Action == (int)ReplyRideGroup.ActionDefine.emAction_Delete)
                        {
                            // 刪除Redis資料
                            if (redis.GetRedis(0).KeyDelete(sMemberKey))
                            {
                                rData.Result = (int)ReplyRideGroupResult.ResultDefine.emResult_Success;

                                SaveLog($"[Info] Controller::OnReplyRideGroup Remove Group Member Temp Data: {sMemberKey} Success");
                            }
                            else
                            {
                                rData.Result = (int)ReplyRideGroupResult.ResultDefine.emResult_Fail;

                                SaveLog($"[Warning] Controller::OnReplyRideGroup Remove Group Member Temp Data: {sMemberKey} Fail");
                            }
                        }
                        else
                        {
                            rData.Result = (int)ReplyRideGroupResult.ResultDefine.emResult_Fail;

                            SaveLog($"[Warning] Controller::OnUpdateRideGroup MemberID:{packet.MemberID}'s Action: 0");
                        }
                    }
                    else
                    {
                        rData.Result = (int)ReplyRideGroupResult.ResultDefine.emResult_Fail;

                        SaveLog($"[Warning] Controller::OnUpdateRideGroup Can Not Find Group Member Temp Data: {sMemberKey}");
                    }

                }
                else
                {
                    rData.Result = (int)ReplyRideGroupResult.ResultDefine.emResult_Fail;

                    SaveLog($"[Warning] Controller::OnUpdateRideGroup Can Not Find MemberID:{packet.MemberID}");

                }
            }
            catch (Exception ex)
            {
                SaveLog($"[Error] Controller::OnUpdateRideGroup Catch Error, Msg:{ex.Message}");

                rData.Result = (int)ReplyRideGroupResult.ResultDefine.emResult_Fail;
            }

            if (rData.Result == (int)ReplyRideGroupResult.ResultDefine.emResult_Success)
            {
                string sAction = packet.Action == 1 ? "加入" : packet.Action == -1 ? "拒絕" : "Error";

                if (leaderAccount != null && leaderUserInfo != null)
                {
                    // 發送推播通知
                    string sTitle = $"組隊回覆";

                    string sNotifyMsg = $"{replyMemberNickName} 已{sAction}組隊";

                    ntMsg.NotifyMsgToDevice(leaderAccount.NotifyToken, sTitle, sNotifyMsg);
                }
                else
                {
                    SaveLog($"[Error] Controller::OnReplyRideGroup, Can Not Notify");
                }
            }

            JObject jsMain = new JObject();
            jsMain.Add("CmdID", (int)S2C_CmdID.emReplyRideGroupResult);
            jsMain.Add("Data", JsonConvert.DeserializeObject<JObject>(JsonConvert.SerializeObject(rData)));

            ret = jsMain.ToString();

            return ret;
        }

        /**
         * 更新座標
         */
        public string OnUpdateCoordinate(string data)
        {
            string ret = "";

            UpdateCoordinate packet = JsonConvert.DeserializeObject<UpdateCoordinate>(data);

            UpdateCoordinateResult rData = new UpdateCoordinateResult();

            try
            {
                UserAccount account = db.GetSql().Queryable<UserAccount>().With(SqlSugar.SqlWith.RowLock).Where(it => it.MemberID == packet.MemberID).Single();

                // 有找到帳號
                if (account != null)
                {
                    string sMemberKey = $"GroupMember_{packet.MemberID}";

                    // 該組隊成員存在
                    if (redis.GetRedis(0).KeyExists(sMemberKey))
                    {
                        string MemberData = redis.GetRedis(0).StringGet(sMemberKey);

                        JObject jsMemberData = JObject.Parse(MemberData);

                        if (redis.GetRedis(0).KeyExists(jsMemberData["RideGroupKey"].ToString()))
                        {
                            jsMemberData["CoordinateX"] = packet.CoordinateX;
                            jsMemberData["CoordinateY"] = packet.CoordinateY;

                            if (redis.GetRedis(0).StringSet(sMemberKey, jsMemberData.ToString()))
                            {
                                rData.Result = (int)UpdateCoordinateResult.ResultDefine.emResult_Success;

                                SaveLog($"[Info] Controller::OnUpdateCoordinate, Update Member:{packet.MemberID}'s Coordinate: {packet.CoordinateX}, {packet.CoordinateY} Success");

                                string sGroupData = redis.GetRedis(0).StringGet("RideGroupKey");

                                JObject jsGroupData = JObject.Parse(sGroupData);

                                JArray jsMemberList = JArray.Parse(jsGroupData["MemberList"].ToString());
                                List<string> MemberList = jsMemberList.ToObject<List<string>>();

                                for (int idx = 0; idx < MemberList.Count(); idx++)
                                {
                                    if (MemberList[idx] != packet.MemberID)
                                    {
                                        List<UserAccount> notifyAccountList = db.GetSql().Queryable<UserAccount>().With(SqlSugar.SqlWith.RowLock).Where(it => it.MemberID == MemberList[idx]).ToList();
                                        List<UserInfo> notifyUserInfoList = db.GetSql().Queryable<UserInfo>().With(SqlSugar.SqlWith.RowLock).Where(it => it.MemberID == MemberList[idx]).ToList();

                                        if (notifyAccountList.Count() == 1 && notifyUserInfoList.Count() == 1)
                                        {
                                            // 發送推播通知
                                            string sTitle = $"更新座標";

                                            string sNotifyMsg = $"{packet.CoordinateX},{packet.CoordinateY}";

                                            ntMsg.NotifyMsgToDevice(notifyAccountList[0].NotifyToken, sTitle, sNotifyMsg);
                                        }
                                        else
                                        {
                                            SaveLog($"[Warning] Controller::OnUpdateCoordinate, Can Not Finf Notify Member:{MemberList[idx]}");
                                        }

                                    }
                                }
                            }
                            else
                            {
                                rData.Result = (int)UpdateCoordinateResult.ResultDefine.emResult_Fail;

                                SaveLog($"[Warning] Controller::OnUpdateCoordinate, Update Member:{packet.MemberID} 's Coordinate Fail");
                            }

                        }
                        else
                        {
                            rData.Result = (int)UpdateCoordinateResult.ResultDefine.emResult_Fail;

                            SaveLog($"[Error] Controller::OnUpdateCoordinate, Can Not Find Ride Group: {sMemberKey}");
                        }

                    }
                    else
                    {
                        rData.Result = (int)UpdateCoordinateResult.ResultDefine.emResult_Fail;

                        SaveLog($"[Warning] Controller::OnUpdateCoordinate Group Member: {packet.MemberID} Not Exist");
                    }

                }
                else
                {
                    rData.Result = (int)UpdateCoordinateResult.ResultDefine.emResult_Fail;

                    SaveLog($"[Warning] Controller::OnUpdateCoordinate Can Not Find MemberID:{packet.MemberID}");

                }
            }
            catch (Exception ex)
            {
                SaveLog($"[Error] Controller::OnUpdateCoordinate Catch Error, Msg:{ex.Message}");

                rData.Result = (int)UpdateCoordinateResult.ResultDefine.emResult_Fail;
            }

            JObject jsMain = new JObject();
            jsMain.Add("CmdID", (int)S2C_CmdID.emUpdateCoordinateResult);
            jsMain.Add("Data", JsonConvert.DeserializeObject<JObject>(JsonConvert.SerializeObject(rData)));

            ret = jsMain.ToString();

            return ret;
        }

        /**
         * 通知隊友
         */
        public string OnNotifyRideGroupMember(string data)
        {
            string ret = "";

            NotifyRideGroupMember packet = JsonConvert.DeserializeObject<NotifyRideGroupMember>(data);

            NotifyRideGroupMemberResult rData = new NotifyRideGroupMemberResult();

            try
            {
                UserAccount account = db.GetSql().Queryable<UserAccount>().With(SqlSugar.SqlWith.RowLock).Where(it => it.MemberID == packet.MemberID).Single();
                UserInfo userInfo = db.GetSql().Queryable<UserInfo>().With(SqlSugar.SqlWith.RowLock).Where(it => it.MemberID == packet.MemberID).Single();

                // 有找到帳號
                if (account != null && userInfo != null)
                {
                    string sMemberKey = $"GroupMember_{packet.MemberID}";

                    // 該組隊成員存在
                    if (redis.GetRedis(0).KeyExists(sMemberKey))
                    {
                        string MemberData = redis.GetRedis(0).StringGet(sMemberKey);

                        JObject jsMemberData = JObject.Parse(MemberData);

                        if (jsMemberData.ContainsKey("RideGroupKey"))
                        {
                            string sRideGroupKey = jsMemberData["RideGroupKey"].ToString();

                            // 該組隊騎乘存在
                            if (redis.GetRedis(0).KeyExists(sRideGroupKey))
                            {
                                string GroupData = redis.GetRedis(0).StringGet(sRideGroupKey);

                                JObject jsGroupData = JObject.Parse(GroupData);

                                JArray jsMemberList = JArray.Parse(jsGroupData["MemberList"].ToString());
                                List<string> MemberList = jsMemberList.ToObject<List<string>>();

                                for (int idx = 0; idx < MemberList.Count(); idx++)
                                {
                                    if (MemberList[idx] != packet.MemberID)
                                    {
                                        UserAccount notifyAccount = db.GetSql().Queryable<UserAccount>().With(SqlSugar.SqlWith.RowLock).Where(it => it.MemberID == MemberList[idx]).Single();
                                        UserInfo notifyUserInfo = db.GetSql().Queryable<UserInfo>().With(SqlSugar.SqlWith.RowLock).Where(it => it.MemberID == MemberList[idx]).Single();

                                        if (notifyAccount != null && notifyUserInfo != null)
                                        {
                                            rData.Result = (int)NotifyRideGroupMemberResult.ResultDefine.emResult_Success;

                                            SaveLog($"[Info] Controller::OnNotifyRideGroupMember MemberID:{packet.MemberID}'s Notify Warning To Group Member");

                                            string sAction = packet.Action == (int)NotifyRideGroupMember.ActionDefine.emAction_Add ? "發生" : packet.Action == (int)NotifyRideGroupMember.ActionDefine.emAction_Delete ? "取消" : "Error";

                                            // 發送推播通知
                                            string sTitle = $"緊急通知";

                                            string sNotifyMsg = $"{userInfo.NickName} {sAction} 緊急狀況";

                                            ntMsg.NotifyMsgToDevice(notifyAccount.NotifyToken, sTitle, sNotifyMsg);

                                            // 推播通知
                                            if (packet.Action == (int)NotifyRideGroupMember.ActionDefine.emAction_None)
                                            {
                                                rData.Result = (int)NotifyRideGroupMemberResult.ResultDefine.emResult_Fail;

                                                SaveLog($"[Warning] Controller::OnNotifyRideGroupMember MemberID:{packet.MemberID}'s Action: 0");

                                                break;
                                            }
                                        }
                                        else
                                        {
                                            SaveLog($"[Warning] Controller::OnNotifyRideGroupMember Can Not Find Notify MemberID:{MemberList[idx]}");

                                        }
                                    }
                                }
                            }
                            else
                            {
                                rData.Result = (int)NotifyRideGroupMemberResult.ResultDefine.emResult_Fail;

                                SaveLog($"[Warning] Controller::OnNotifyRideGroupMember, Can Not Find Ride Group: {sRideGroupKey}");
                            }
                        }
                        else
                        {
                            rData.Result = (int)NotifyRideGroupMemberResult.ResultDefine.emResult_Fail;

                            SaveLog($"[Warning] Controller::OnNotifyRideGroupMember, Can Not Find Json Ride Group Key");
                        }

                    }
                    else
                    {
                        rData.Result = (int)NotifyRideGroupMemberResult.ResultDefine.emResult_Fail;

                        SaveLog($"[Warning] Controller::OnNotifyRideGroupMember Group Member: {packet.MemberID} Not Exist");
                    }

                }
                else
                {
                    rData.Result = (int)NotifyRideGroupMemberResult.ResultDefine.emResult_Fail;

                    SaveLog($"[Warning] Controller::OnNotifyRideGroupMember Can Not Find MemberID:{packet.MemberID}");

                }
            }
            catch (Exception ex)
            {
                SaveLog($"[Error] Controller::OnNotifyRideGroupMember Catch Error, Msg:{ex.Message}");

                rData.Result = (int)NotifyRideGroupMemberResult.ResultDefine.emResult_Fail;
            }

            JObject jsMain = new JObject();
            jsMain.Add("CmdID", (int)S2C_CmdID.emNotifyRideGroupMemberResult);
            jsMain.Add("Data", JsonConvert.DeserializeObject<JObject>(JsonConvert.SerializeObject(rData)));

            ret = jsMain.ToString();

            return ret;
        }

    }

}
