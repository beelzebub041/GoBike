using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Windows.Forms;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Tools.Logger;
using Tools.WeekProcess;
using Tools.NotifyMessage;

using DataBaseDef;
using Connect;

using RidePacket.ClientToServer;
using RidePacket.ServerToClient;


namespace RideService
{
    class Controller
    {
        private Form1 fm1 = null;

        private Logger log = null;                      // Logger

        private WeekProcess weekProcess = null;         // WeekProcess

        private DataBaseConnect dbConnect = null;

        private NotifyMessage ntMsg = null;

        RedisConnect redis = null;

        private Server wsServer = null;                 // Web Socket Server

        private object msgLock = new object();

        private string version = "Ride008";


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

                weekProcess = new WeekProcess();

                log.SaveLog($"Controller Version: {version}");

                wsServer = new Server(log.SaveLog, MessageProcess);

                dbConnect = new DataBaseConnect(log);

                ntMsg = new NotifyMessage(log);

                redis = new RedisConnect(log);

                if (dbConnect.Initialize())
                {
                    if (dbConnect.Connect())
                    {
                        if (wsServer.Initialize())
                        {
                            if (redis.Initialize())
                            {
                                if (ntMsg.Initialize())
                                {
                                    bReturn = true;
                                }
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
                                    case (int)C2S_CmdID.emCreateRideRecord:
                                        CreateRideRecord CreateData = JsonConvert.DeserializeObject<CreateRideRecord>(packetData);

                                        sReturn = OnCreateRideRecord(CreateData);

                                        break;

                                    case (int)C2S_CmdID.emUpdateRideGroup:
                                        UpdateRideGroup UpdateGroup = JsonConvert.DeserializeObject<UpdateRideGroup>(packetData);

                                        sReturn = OnUpdateRideGroup(UpdateGroup);

                                        break;

                                    case (int)C2S_CmdID.emReplyRideGroup:
                                        ReplyRideGroup ReplyData = JsonConvert.DeserializeObject<ReplyRideGroup>(packetData);

                                        sReturn = OnReplyRideGroup(ReplyData);

                                        break;

                                    case (int)C2S_CmdID.emUpdateCoordinate:
                                        UpdateCoordinate CoordinateData = JsonConvert.DeserializeObject<UpdateCoordinate>(packetData);

                                        sReturn = OnUpdateCoordinate(CoordinateData);

                                        break;

                                    case (int)C2S_CmdID.emNotifyRideGroupMember:
                                        NotifyRideGroupMember NotifyData = JsonConvert.DeserializeObject<NotifyRideGroupMember>(packetData);

                                        sReturn = OnNotifyRideGroupMember(NotifyData);

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
                        log.SaveLog($"[Error] Controller::MessageProcess Process Error Msg:{ex.Message}");
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
         * 建立騎乘紀錄
         */
        private string OnCreateRideRecord(CreateRideRecord packet)
        {
            CreateRideRecordResult rData = new CreateRideRecordResult();

            try 
            {
                List<UserAccount> accountList = dbConnect.GetSql().Queryable<UserAccount>().With(SqlSugar.SqlWith.RowLock).Where(it => it.MemberID == packet.MemberID).ToList();

                List<RideData> rideDataList = dbConnect.GetSql().Queryable<RideData>().With(SqlSugar.SqlWith.RowLock).Where(it => it.MemberID == packet.MemberID).ToList();

                // 有找到帳號 且 有找到 騎乘資料
                if (accountList.Count() == 1 && rideDataList.Count() == 1)
                {
                    string dateTime = DateTime.UtcNow.ToString("yyyy-MM-dd hh:mm:ss");

                    string guidAll = Guid.NewGuid().ToString();

                    string[] guidList = guidAll.Split('-');

                    // ======================= 新增騎乘紀錄 =======================
                    RideRecord record = new RideRecord
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

                    if (dbConnect.GetSql().Insertable(record).With(SqlSugar.SqlWith.TabLockX).ExecuteCommand() > 0)
                    {
                        log.SaveLog($"[Info] Controller::OnCreateRideRecord Create {packet.MemberID}'s Ride Record:{record.RideID} Success");

                        // ======================= 更新騎乘資料 =======================
                        rideDataList[0].TotalDistance += packet.Distance;
                        rideDataList[0].TotalAltitude += packet.Altitude;
                        rideDataList[0].TotalRideTime += packet.Time;

                        if (dbConnect.GetSql().Updateable<RideData>(rideDataList[0]).With(SqlSugar.SqlWith.RowLock).Where(it => it.MemberID == packet.MemberID).ExecuteCommand() > 0)
                        {
                            log.SaveLog($"[Info] Controller::OnCreateRideRecord Update MemberID:{packet.MemberID}'s Ride Data Success");
                        }
                        else
                        {
                            log.SaveLog($"[Warning] Controller::OnCreateRideRecord Update MemberID:{packet.MemberID}'s Ride Data Fail");
                        }

                        // ======================= 更新周騎乘資料 =======================
                        string firsDay = weekProcess.GetWeekFirstDay(DateTime.UtcNow);
                        string lastDay = weekProcess.GetWeekLastDay(DateTime.UtcNow);

                        List<WeekRideData> wRideDataList = dbConnect.GetSql().Queryable<WeekRideData>().With(SqlSugar.SqlWith.RowLock).Where(it => it.MemberID == packet.MemberID && it.WeekFirstDay == firsDay && it.WeekLastDay == lastDay).ToList();

                        // 有找到資料
                        if (wRideDataList.Count() == 1)
                        {
                            wRideDataList[0].WeekDistance += packet.Distance;

                            if (dbConnect.GetSql().Updateable<WeekRideData>(wRideDataList[0]).With(SqlSugar.SqlWith.RowLock).Where(it => it.MemberID == packet.MemberID && it.WeekFirstDay == firsDay && it.WeekLastDay == lastDay).ExecuteCommand() > 0)
                            {
                                log.SaveLog($"[Info] Controller::ODnCreateRideRecord Update MemberID:{packet.MemberID}'s Week({firsDay} To {lastDay}) Ride Data Success");
                            }
                            else
                            {
                                log.SaveLog($"[Warning] Controller::ODnCreateRideRecord Update MemberID:{packet.MemberID}'s Week({firsDay} To {lastDay}) Ride Data Fail");
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

                            if (dbConnect.GetSql().Insertable(updateWeek).With(SqlSugar.SqlWith.TabLockX).ExecuteCommand() > 0)
                            {
                                log.SaveLog($"[Info] Controller::ODnCreateRideRecord Create MemberID:{packet.MemberID}'s Week({firsDay} To {lastDay}) Ride Data Success");

                            }
                            else
                            {
                                log.SaveLog($"[Warning] Controller::ODnCreateRideRecord Create MemberID:{packet.MemberID}'s Week({firsDay} To {lastDay}) Ride Data Fail");
                            }
                        }

                        rData.Result = 1;
                        rData.TotalDistance = rideDataList[0].TotalDistance;
                        rData.TotalAltitude = rideDataList[0].TotalAltitude;
                        rData.TotalRideTime = rideDataList[0].TotalRideTime;
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
                log.SaveLog($"[Error] Controller::OnCreateRideRecord Catch Error, Msg:{ex.Message}");

                rData.Result = 0;
            }

            JObject jsMain = new JObject();
            jsMain.Add("CmdID", (int)S2C_CmdID.emCreateRideRecordResult);
            jsMain.Add("Data", JsonConvert.DeserializeObject<JObject>(JsonConvert.SerializeObject(rData)));

            return jsMain.ToString();
        }

        /**
         * 更新組隊騎乘
         */
        private string OnUpdateRideGroup(UpdateRideGroup packet)
        {
            UpdateRideGroupResult rData = new UpdateRideGroupResult();

            try
            {
                List<UserAccount> accountList = dbConnect.GetSql().Queryable<UserAccount>().With(SqlSugar.SqlWith.RowLock).Where(it => it.MemberID == packet.MemberID).ToList();
                List<UserInfo> userInfoList = dbConnect.GetSql().Queryable<UserInfo>().With(SqlSugar.SqlWith.RowLock).Where(it => it.MemberID == packet.MemberID).ToList();

                // 有找到帳號
                if (accountList.Count() == 1 && userInfoList.Count() == 1)
                {
                    string sRideGroupKey = $"RideGroup_{packet.MemberID}";

                    if (packet.Action == 1)
                    {
                        // 該組隊不存在
                        if (!redis.GetRedis().KeyExists(sRideGroupKey))
                        {
                            JArray jsInviteList = JArray.Parse(packet.InviteList);
                            List<string> InviteList = jsInviteList.ToObject<List<string>>();

                            List<string> MemberList = new List<string>();
                            MemberList.Add(packet.MemberID);

                            JObject jsGroupData = new JObject();
                            jsGroupData.Add("Leader", packet.MemberID);
                            jsGroupData.Add("InviteList", jsInviteList);
                            jsGroupData.Add("MemberList", JArray.FromObject(MemberList).ToString());

                            if (redis.GetRedis().StringSet(sRideGroupKey, jsGroupData.ToString())) {

                                rData.Result = 1;

                                log.SaveLog($"[Info] Controller::OnUpdateRideGroup Create Ride Group: {sRideGroupKey} Success");

                                for (int idx =0; idx < InviteList.Count(); idx++)
                                {
                                    string sMemberKey = $"GroupMember_{InviteList[idx]}";

                                    JObject jsMemberData = new JObject();
                                    jsMemberData.Add("RideGroupKey", sRideGroupKey);
                                    jsMemberData.Add("CoordinateX", "");
                                    jsMemberData.Add("CoordinateY", "");

                                    // 建立成員的Redis資料
                                    if (redis.GetRedis().StringSet(sMemberKey, jsMemberData.ToString()))
                                    {
                                        log.SaveLog($"[Info] Controller::OnUpdateRideGroup Create Ride Group: {sRideGroupKey}'s Member:{InviteList[idx]} Success");
                                    }
                                    else
                                    {
                                        log.SaveLog($"[Info] Controller::OnUpdateRideGroup Create Ride Group: {sRideGroupKey}'s Member:{InviteList[idx]} Fail");
                                    }

                                    List<UserAccount> notifyAccounrList = dbConnect.GetSql().Queryable<UserAccount>().With(SqlSugar.SqlWith.RowLock).Where(it => it.MemberID == InviteList[idx]).ToList();
                                    List<UserInfo> notifyUserInfoList = dbConnect.GetSql().Queryable<UserInfo>().With(SqlSugar.SqlWith.RowLock).Where(it => it.MemberID == InviteList[idx]).ToList();

                                    // 有找對會員
                                    if (notifyAccounrList.Count() == 1 && notifyUserInfoList.Count() == 1)
                                    {
                                        // 發送推播通知
                                        string sTitle = $"騎乘邀請";

                                        string sNotifyMsg = $"{userInfoList[0].NickName} 邀請您組隊";

                                        ntMsg.NotifyMsgToDevice(notifyAccounrList[0].NotifyToken, sTitle, sNotifyMsg);
                                    }
                                    else
                                    {
                                        log.SaveLog($"[Warning] Controller::OnUpdateRideGroup Can Not Find Notify Member: {accountList[0].MemberID}");
                                    }


                                }

                            }
                            else
                            {
                                rData.Result = 0;

                                log.SaveLog($"[Warning] Controller::OnUpdateRideGroup Create Ride Group: {sRideGroupKey} Fail");
                            }

                        }
                        else
                        {
                            rData.Result = 2;

                            log.SaveLog($"[Warning] Controller::OnUpdateRideGroup Create {packet.MemberID}'s Ride Group, The Group Repeat");
                        }
                    }
                    else if (packet.Action == -1)
                    {
                        // 該組隊存在
                        if (redis.GetRedis().KeyExists(sRideGroupKey))
                        {
                            // 刪除Redis資料
                            if(redis.GetRedis().KeyDelete(sRideGroupKey))
                            {
                                rData.Result = 1;

                                log.SaveLog($"[Info] Controller::OnUpdateRideGroup Remove Ride Group: {sRideGroupKey} Success");
                            }
                            else
                            {
                                rData.Result = 0;

                                log.SaveLog($"[Warning] Controller::OnUpdateRideGroup Remove Ride Group: {sRideGroupKey} Fail");
                            }
                        }
                        else
                        {
                            rData.Result = 0;

                            log.SaveLog($"[Warning] Controller::OnUpdateRideGroup Can Not Find Ride Group: {sRideGroupKey}");
                        }
                    }
                    else
                    {
                        rData.Result = 0;

                        log.SaveLog($"[Warning] Controller::OnUpdateRideGroup MemberID:{packet.MemberID}'s Action: 0");
                    }

                }
                else
                {
                    rData.Result = 0;

                    log.SaveLog($"[Warning] Controller::OnUpdateRideGroup Can Not Find MemberID:{packet.MemberID}");

                }
            }
            catch (Exception ex)
            {
                log.SaveLog($"[Error] Controller::OnUpdateRideGroup Catch Error, Msg:{ex.Message}");

                rData.Result = 0;
            }

            JObject jsMain = new JObject();
            jsMain.Add("CmdID", (int)S2C_CmdID.emUpdateRideGroupResult);
            jsMain.Add("Data", JsonConvert.DeserializeObject<JObject>(JsonConvert.SerializeObject(rData)));

            return jsMain.ToString();
        }

        /**
         * 回覆組隊騎乘
         */
        private string OnReplyRideGroup(ReplyRideGroup packet)
        {
            ReplyRideGroupResult rData = new ReplyRideGroupResult();

            List<UserAccount> leaderAccountList = new List<UserAccount>();
            List<UserInfo> leaderUserInfoList = new List<UserInfo>();

            string replyMemberNickName = "";

            try
            {
                List<UserAccount> accountList = dbConnect.GetSql().Queryable<UserAccount>().With(SqlSugar.SqlWith.RowLock).Where(it => it.MemberID == packet.MemberID).ToList();
                List<UserInfo> userInfoList = dbConnect.GetSql().Queryable<UserInfo>().With(SqlSugar.SqlWith.RowLock).Where(it => it.MemberID == packet.MemberID).ToList();

                // 有找到帳號
                if (accountList.Count() == 1 && userInfoList.Count() == 1)
                {
                    string sMemberKey = $"GroupMember_{packet.MemberID}";

                    replyMemberNickName = userInfoList[0].NickName;

                    // 該組隊成員存在
                    if (redis.GetRedis().KeyExists(sMemberKey))
                    {
                        if (packet.Action == 1)
                        {
                            string MemberData = redis.GetRedis().StringGet(sMemberKey);

                            JObject jsMemberData = JObject.Parse(MemberData);

                            if (jsMemberData.ContainsKey("RideGroupKey"))
                            {
                                string sRideGroupKey = jsMemberData["RideGroupKey"].ToString();

                                if (redis.GetRedis().KeyExists(sRideGroupKey))
                                {
                                    string GroupData = redis.GetRedis().StringGet(sRideGroupKey);

                                    leaderAccountList = dbConnect.GetSql().Queryable<UserAccount>().With(SqlSugar.SqlWith.RowLock).Where(it => it.MemberID == packet.MemberID).ToList();
                                    leaderUserInfoList = dbConnect.GetSql().Queryable<UserInfo>().With(SqlSugar.SqlWith.RowLock).Where(it => it.MemberID == packet.MemberID).ToList();

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

                                            if (redis.GetRedis().StringSet(sRideGroupKey, GroupData))
                                            {
                                                rData.Result = 1;

                                                log.SaveLog($"[Info] Controller::OnReplyRideGroup, Member:{packet.MemberID} Join Ride Group: {sRideGroupKey} Success");
                                            }
                                            else
                                            {
                                                rData.Result = 0;

                                                log.SaveLog($"[Warning] Controller::OnReplyRideGroup, Member:{packet.MemberID} Join Ride Group: {sRideGroupKey} Fail");
                                            }
                                        }
                                        else
                                        {
                                            rData.Result = 0;

                                            log.SaveLog($"[Warning] Controller::OnReplyRideGroup, Member:{packet.MemberID} already is Member");

                                        }

                                    }
                                    else
                                    {
                                        rData.Result = 0;

                                        log.SaveLog($"[Warning] Controller::OnReplyRideGroup, Invite List Cant Not Find Member:{packet.MemberID}");

                                    }
                                }
                                else
                                {
                                    rData.Result = 0;

                                    log.SaveLog($"[Warning] Controller::OnReplyRideGroup, Can Not Find Ride Group: {sRideGroupKey}");
                                }
                            }
                            else
                            {
                                rData.Result = 0;

                                log.SaveLog($"[Warning] Controller::OnReplyRideGroup, Can Not Find Json Ride Group Key");

                            }

                        }
                        else if (packet.Action == -1)
                        {
                            // 刪除Redis資料
                            if (redis.GetRedis().KeyDelete(sMemberKey))
                            {
                                rData.Result = 1;

                                log.SaveLog($"[Info] Controller::OnReplyRideGroup Remove Group Member Temp Data: {sMemberKey} Success");
                            }
                            else
                            {
                                rData.Result = 0;

                                log.SaveLog($"[Warning] Controller::OnReplyRideGroup Remove Group Member Temp Data: {sMemberKey} Fail");
                            }
                        }
                        else
                        {
                            rData.Result = 0;

                            log.SaveLog($"[Warning] Controller::OnUpdateRideGroup MemberID:{packet.MemberID}'s Action: 0");
                        }
                    }
                    else
                    {
                        rData.Result = 0;

                        log.SaveLog($"[Warning] Controller::OnUpdateRideGroup Can Not Find Group Member Temp Data: {sMemberKey}");
                    }

                }
                else
                {
                    rData.Result = 0;

                    log.SaveLog($"[Warning] Controller::OnUpdateRideGroup Can Not Find MemberID:{packet.MemberID}");

                }
            }
            catch (Exception ex)
            {
                log.SaveLog($"[Error] Controller::OnUpdateRideGroup Catch Error, Msg:{ex.Message}");

                rData.Result = 0;
            }

            if (rData.Result == 1)
            {
                string sAction = packet.Action == 1 ? "加入" : packet.Action == -1 ? "拒絕" : "Error";

                if (leaderAccountList.Count() == 1 && leaderUserInfoList.Count() == 1)
                {
                    // 發送推播通知
                    string sTitle = $"組隊回覆";

                    string sNotifyMsg = $"{replyMemberNickName} 已{sAction}組隊";

                    ntMsg.NotifyMsgToDevice(leaderAccountList[0].NotifyToken, sTitle, sNotifyMsg);
                }
                else
                {
                    log.SaveLog($"[Error] Controller::OnReplyRideGroup, Can Not Notify");
                }
            }

            JObject jsMain = new JObject();
            jsMain.Add("CmdID", (int)S2C_CmdID.emReplyRideGroupResult);
            jsMain.Add("Data", JsonConvert.DeserializeObject<JObject>(JsonConvert.SerializeObject(rData)));

            return jsMain.ToString();
        }

        /**
         * 更新座標
         */
        private string OnUpdateCoordinate(UpdateCoordinate packet)
        {
            UpdateCoordinateResult rData = new UpdateCoordinateResult();

            try
            {
                List<UserAccount> accountList = dbConnect.GetSql().Queryable<UserAccount>().With(SqlSugar.SqlWith.RowLock).Where(it => it.MemberID == packet.MemberID).ToList();

                // 有找到帳號
                if (accountList.Count() == 1)
                {
                    string sMemberKey = $"GroupMember_{packet.MemberID}";

                    // 該組隊成員存在
                    if (redis.GetRedis().KeyExists(sMemberKey))
                    {
                        string MemberData = redis.GetRedis().StringGet(sMemberKey);

                        JObject jsMemberData = JObject.Parse(MemberData);

                        if (redis.GetRedis().KeyExists(jsMemberData["RideGroupKey"].ToString()))
                        {
                            jsMemberData["CoordinateX"] = packet.CoordinateX;
                            jsMemberData["CoordinateY"] = packet.CoordinateY;

                            if (redis.GetRedis().StringSet(sMemberKey, jsMemberData.ToString()))
                            {
                                rData.Result = 1;

                                log.SaveLog($"[Info] Controller::OnUpdateCoordinate, Update Member:{packet.MemberID}'s Coordinate: {packet.CoordinateX}, {packet.CoordinateY} Success");

                                string sGroupData = redis.GetRedis().StringGet("RideGroupKey");

                                JObject jsGroupData = JObject.Parse(sGroupData);

                                JArray jsMemberList = JArray.Parse(jsGroupData["MemberList"].ToString());
                                List<string> MemberList = jsMemberList.ToObject<List<string>>();

                                for (int idx = 0; idx < MemberList.Count(); idx++)
                                {
                                    if (MemberList[idx] != packet.MemberID)
                                    {
                                        List<UserAccount> notifyAccountList = dbConnect.GetSql().Queryable<UserAccount>().With(SqlSugar.SqlWith.RowLock).Where(it => it.MemberID == MemberList[idx]).ToList();
                                        List<UserInfo> notifyUserInfoList = dbConnect.GetSql().Queryable<UserInfo>().With(SqlSugar.SqlWith.RowLock).Where(it => it.MemberID == MemberList[idx]).ToList();

                                        if (notifyAccountList.Count() == 1 && notifyUserInfoList.Count() == 1)
                                        {
                                            // 發送推播通知
                                            string sTitle = $"更新座標";

                                            string sNotifyMsg = $"{packet.CoordinateX},{packet.CoordinateY}";

                                            ntMsg.NotifyMsgToDevice(notifyAccountList[0].NotifyToken, sTitle, sNotifyMsg);
                                        }
                                        else {
                                            log.SaveLog($"[Warning] Controller::OnUpdateCoordinate, Can Not Finf Notify Member:{MemberList[idx]}");
                                        }

                                    }
                                }
                            }
                            else
                            {
                                rData.Result = 0;

                                log.SaveLog($"[Warning] Controller::OnUpdateCoordinate, Update Member:{packet.MemberID} 's Coordinate Fail");
                            }

                        }
                        else
                        {
                            log.SaveLog($"[Error] Controller::OnUpdateCoordinate, Can Not Find Ride Group: {sMemberKey}");
                        }

                    }
                    else
                    {
                        rData.Result = 0;

                        log.SaveLog($"[Warning] Controller::OnUpdateCoordinate Group Member: {packet.MemberID} Not Exist");
                    }

                }
                else
                {
                    rData.Result = 0;

                    log.SaveLog($"[Warning] Controller::OnUpdateCoordinate Can Not Find MemberID:{packet.MemberID}");

                }
            }
            catch (Exception ex)
            {
                log.SaveLog($"[Error] Controller::OnUpdateCoordinate Catch Error, Msg:{ex.Message}");

                rData.Result = 0;
            }

            JObject jsMain = new JObject();
            jsMain.Add("CmdID", (int)S2C_CmdID.emUpdateCoordinateResult);
            jsMain.Add("Data", JsonConvert.DeserializeObject<JObject>(JsonConvert.SerializeObject(rData)));

            return jsMain.ToString();
        }

        /**
         * 通知隊友
         */
        private string OnNotifyRideGroupMember(NotifyRideGroupMember packet)
        {
            NotifyRideGroupMemberResult rData = new NotifyRideGroupMemberResult();

            try
            {
                List<UserAccount> accountList = dbConnect.GetSql().Queryable<UserAccount>().With(SqlSugar.SqlWith.RowLock).Where(it => it.MemberID == packet.MemberID).ToList();
                List<UserInfo> userInfoList = dbConnect.GetSql().Queryable<UserInfo>().With(SqlSugar.SqlWith.RowLock).Where(it => it.MemberID == packet.MemberID).ToList();

                // 有找到帳號
                if (accountList.Count() == 1 && userInfoList.Count() == 1)
                {
                    string sMemberKey = $"GroupMember_{packet.MemberID}";

                    // 該組隊成員存在
                    if (redis.GetRedis().KeyExists(sMemberKey))
                    {
                        string MemberData = redis.GetRedis().StringGet(sMemberKey);

                        JObject jsMemberData = JObject.Parse(MemberData);

                        if (jsMemberData.ContainsKey("RideGroupKey"))
                        {
                            string sRideGroupKey = jsMemberData["RideGroupKey"].ToString();

                            // 該組隊騎乘存在
                            if (redis.GetRedis().KeyExists(sRideGroupKey))
                            {
                                string GroupData = redis.GetRedis().StringGet(sRideGroupKey);

                                JObject jsGroupData = JObject.Parse(GroupData);

                                JArray jsMemberList = JArray.Parse(jsGroupData["MemberList"].ToString());
                                List<string> MemberList = jsMemberList.ToObject<List<string>>();

                                for (int idx = 0; idx < MemberList.Count(); idx++)
                                {
                                    if (MemberList[idx] != packet.MemberID)
                                    {
                                        List<UserAccount> notifyAccountList = dbConnect.GetSql().Queryable<UserAccount>().With(SqlSugar.SqlWith.RowLock).Where(it => it.MemberID == MemberList[idx]).ToList();
                                        List<UserInfo> notifyUserInfoList = dbConnect.GetSql().Queryable<UserInfo>().With(SqlSugar.SqlWith.RowLock).Where(it => it.MemberID == MemberList[idx]).ToList();

                                        if (notifyAccountList.Count() == 1 && notifyUserInfoList.Count() == 1)
                                        {
                                            rData.Result = 1;

                                            log.SaveLog($"[Info] Controller::OnNotifyRideGroupMember MemberID:{packet.MemberID}'s Notify Warning To Group Member");

                                            string sAction = packet.Action == 1 ? "發生" : packet.Action == -1 ? "取消" : "Error";

                                            // 發送推播通知
                                            string sTitle = $"緊急通知";

                                            string sNotifyMsg = $"{userInfoList[0].NickName} {sAction}緊急狀況";

                                            ntMsg.NotifyMsgToDevice(notifyAccountList[0].NotifyToken, sTitle, sNotifyMsg);

                                            // 推播通知
                                            if (packet.Action == 0)
                                            {
                                                rData.Result = 0;

                                                log.SaveLog($"[Warning] Controller::OnNotifyRideGroupMember MemberID:{packet.MemberID}'s Action: 0");

                                                break;
                                            }
                                        }
                                        else
                                        {
                                            log.SaveLog($"[Warning] Controller::OnNotifyRideGroupMember Can Not Find Notify MemberID:{MemberList[idx]}");

                                        }
                                    }
                                }
                            }
                            else
                            {
                                rData.Result = 0;

                                log.SaveLog($"[Warning] Controller::OnNotifyRideGroupMember, Can Not Find Ride Group: {sRideGroupKey}");
                            }
                        }
                        else
                        {
                            rData.Result = 0;

                            log.SaveLog($"[Warning] Controller::OnNotifyRideGroupMember, Can Not Find Json Ride Group Key");
                        }

                    }
                    else
                    {
                        rData.Result = 0;

                        log.SaveLog($"[Warning] Controller::OnNotifyRideGroupMember Group Member: {packet.MemberID} Not Exist");
                    }

                }
                else
                {
                    rData.Result = 0;

                    log.SaveLog($"[Warning] Controller::OnNotifyRideGroupMember Can Not Find MemberID:{packet.MemberID}");

                }
            }
            catch (Exception ex)
            {
                log.SaveLog($"[Error] Controller::OnNotifyRideGroupMember Catch Error, Msg:{ex.Message}");

                rData.Result = 0;
            }

            JObject jsMain = new JObject();
            jsMain.Add("CmdID", (int)S2C_CmdID.emNotifyRideGroupMemberResult);
            jsMain.Add("Data", JsonConvert.DeserializeObject<JObject>(JsonConvert.SerializeObject(rData)));

            return jsMain.ToString();
        }

    }

}
