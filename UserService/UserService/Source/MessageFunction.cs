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

using Tools.WeekProcess;
using Tools.RedisHashTransfer;

using DataBaseDef;
using Connect;

using UserPacket.ClientToServer;
using UserPacket.ServerToClient;

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

        WeekProcess weekProcess = null;

        public MessageFunction(LogDelegate log)
        {
            this.log = log;

            db = new DataBaseConnect(SaveLog);

            redis = new RedisConnect(SaveLog);

            hashTransfer = new RedisHashTransfer();

            weekProcess = new WeekProcess();

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
                && redis.Initialize() && redis.Connect())
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

        public string OnCreateNewAccount(string data)
        {
            string ret = "";

            UserRegistered packet = JsonConvert.DeserializeObject<UserRegistered>(data);

            UserRegisteredResult rData = new UserRegisteredResult();

            UserAccount newAccount = new UserAccount();
            UserInfo info = new UserInfo();
            RideData rideData = new RideData();
            WeekRideData curWeek = new WeekRideData();

            if (packet.Password == packet.CheckPassword)
            {
                try
                {
                    UserAccount account = db.GetSql().Queryable<UserAccount>().With(SqlSugar.SqlWith.RowLock).Where(it => it.Email == packet.Email).Single();

                    // 未包含該Email
                    if (account == null)
                    {
                        string dateTime = DateTime.UtcNow.ToString("yyyy-MM-dd hh:mm:ss");

                        string guidAll = Guid.NewGuid().ToString();

                        string[] guidList = guidAll.Split('-');

                        // 建立新帳號
                        newAccount.MemberID = "Dblha-" + guidList[0];        // 取GUID前8碼
                        newAccount.Email = packet.Email;
                        newAccount.Password = packet.Password;
                        newAccount.FBToken = packet.FBToken != null ? packet.FBToken : "";
                        newAccount.GoogleToken = packet.GoogleToken != null ? packet.GoogleToken : "";
                        newAccount.NotifyToken = "";
                        newAccount.RegisterSource = packet.RegisterSource;
                        newAccount.RegisterDate = dateTime;

                        // 新增使用者資訊
                        info.MemberID = newAccount.MemberID;
                        info.NickName = "";
                        info.Birthday = "";
                        info.BodyHeight = 0;
                        info.BodyWeight = 0;
                        info.FrontCover = "";
                        info.Avatar = "";
                        info.Photo = "";
                        info.Mobile = "";
                        info.County = -1;
                        info.TeamList = "[]";
                        info.FriendList = "[]";
                        info.BlackList = "[]";
                        info.SpecificationModel = "";

                        // 新增騎乘資料
                        rideData.MemberID = newAccount.MemberID;
                        rideData.TotalDistance = 0;
                        rideData.TotalAltitude = 0;
                        rideData.TotalRideTime = 0;

                        // 新增本週騎乘資料
                        string firsDay = weekProcess.GetWeekFirstDay(DateTime.UtcNow);
                        string lastDay = weekProcess.GetWeekLastDay(DateTime.UtcNow);

                        curWeek.MemberID = newAccount.MemberID;
                        curWeek.WeekFirstDay = firsDay;
                        curWeek.WeekLastDay = lastDay;
                        curWeek.WeekDistance = 0;

                        // 設定DB 交易的起始點
                        db.GetSql().BeginTran();

                        // 寫入資料庫
                        if (db.GetSql().Insertable(newAccount).With(SqlSugar.SqlWith.TabLockX).ExecuteCommand() > 0)
                        {

                            if (db.GetSql().Insertable(info).With(SqlSugar.SqlWith.TabLockX).ExecuteCommand() > 0 
                                && db.GetSql().Insertable(rideData).With(SqlSugar.SqlWith.TabLockX).ExecuteCommand() > 0
                                && db.GetSql().Insertable(curWeek).With(SqlSugar.SqlWith.TabLockX).ExecuteCommand() > 0)
                            {
                                rData.Result = (int)UserRegisteredResult.ResultDefine.emResult_Success;

                                SaveLog($"[Info] MessageFcunction::OnCreateNewAccount Create New Account Success");
                            }
                            else
                            {
                                rData.Result = (int)UserRegisteredResult.ResultDefine.emResult_Fail;

                                SaveLog($"[Warning] MessageFcunction::OnCreateNewAccount Can Not Inseart User Info Or Ride Data");
                            }
                        }
                        else
                        {
                            rData.Result = (int)UserRegisteredResult.ResultDefine.emResult_AccountRepeat;

                            SaveLog($"[Warning] MessageFcunction::OnCreateNewAccount Email: {packet.Email} Repeat");
                        }

                    }
                    else
                    {
                        rData.Result = (int)UserRegisteredResult.ResultDefine.emResult_AccountRepeat;

                        SaveLog($"[Info] MessageFcunction::OnCreateNewAccount Email Repeat:");

                    }

                }
                catch (Exception ex)
                {
                    rData.Result = (int)UserRegisteredResult.ResultDefine.emResult_Fail;

                    SaveLog($"[Error] MessageFcunction::OnCreateNewAccount Catch Error Msg:{ex.Message}");
                }

            }
            else
            {
                rData.Result = (int)UserRegisteredResult.ResultDefine.emResult_PwdError;

                SaveLog($"[Info] MessageFcunction::OnCreateNewAccount Check Password Error");

            }

            if (rData.Result == (int)UserRegisteredResult.ResultDefine.emResult_Success)
            {
                redis.GetRedis((int)Connect.RedisDB.emRedisDB_User).HashSet($"UserAccount_" + newAccount.Email, hashTransfer.TransToHashEntryArray(newAccount));
                redis.GetRedis((int)Connect.RedisDB.emRedisDB_User).HashSet($"UserInfo_" + info.MemberID, hashTransfer.TransToHashEntryArray(info));
                redis.GetRedis((int)Connect.RedisDB.emRedisDB_Ride).HashSet($"RideData_" + rideData.MemberID, hashTransfer.TransToHashEntryArray(rideData));
                redis.GetRedis((int)Connect.RedisDB.emRedisDB_Ride).HashSet($"RideData_" + rideData.MemberID, hashTransfer.TransToHashEntryArray(rideData));
                redis.GetRedis((int)Connect.RedisDB.emRedisDB_Ride).HashSet($"CurWeekRideData_" + newAccount.MemberID, hashTransfer.TransToHashEntryArray(curWeek));

                // DB 交易提交
                db.GetSql().CommitTran();
            }
            else
            {
                // DB 交易失敗, 啟動Rollback
                db.GetSql().RollbackTran();
            }

            JObject jsMain = new JObject();
            jsMain.Add("CmdID", (int)S2C_CmdID.emUserRegisteredResult);
            jsMain.Add("Data", JsonConvert.DeserializeObject<JObject>(JsonConvert.SerializeObject(rData)));

            ret = jsMain.ToString();

            return ret;
        }

        public string OnUserLogin(string data)
        {
            string ret = "";

            UserLogin packet = JsonConvert.DeserializeObject<UserLogin>(data);

            UserLoginResult rData = new UserLoginResult();

            try
            {
                UserAccount account = db.GetSql().Queryable<UserAccount>().Where(it => it.Email == packet.Email && it.Password == packet.Password).Single();

                // 有找到帳號
                if (account != null)
                {
                    if (account.Password == packet.Password)
                    {
                        List<UserInfo> infoList = db.GetSql().Queryable<UserInfo>().Where(it => it.MemberID == account.MemberID).ToList();

                        // 有找到會員
                        if (infoList.Count() == 1)
                        {
                            rData.Result = (int)UserLoginResult.ResultDefine.emResult_Success;
                            rData.MemberID = account.MemberID;
                        }
                        else
                        {
                            SaveLog("[Warning] MessageFcunction::OnUserLogin Can't Find User Info");

                            rData.Result = (int)UserLoginResult.ResultDefine.emResult_Fail;
                        }
                    }
                    else
                    {
                        rData.Result = (int)UserLoginResult.ResultDefine.emResult_PwdError;

                        SaveLog("[Info] MessageFcunction::OnUserLogin Can't Password Error");
                    }
                }
                else
                {
                    rData.Result = (int)UserLoginResult.ResultDefine.emResult_AccountError;
                }
            }
            catch (Exception ex)
            {
                SaveLog($"[Error] MessageFcunction::OnUserLogin Catch Error, Msg:{ex.Message}");

                rData.Result = (int)UserLoginResult.ResultDefine.emResult_Fail;
            }

            JObject jsMain = new JObject();
            jsMain.Add("CmdID", (int)S2C_CmdID.emUserLoginResult);
            jsMain.Add("Data", JsonConvert.DeserializeObject<JObject>(JsonConvert.SerializeObject(rData)));

            ret = jsMain.ToString();

            return ret;
        }

        public string OnUpdateUserInfo(string data)
        {
            string ret = "";

            UpdateUserInfo packet = JsonConvert.DeserializeObject<UpdateUserInfo>(data);

            UpdateUserInfoResult rData = new UpdateUserInfoResult();

            try
            {
                UserInfo userInfo = db.GetSql().Queryable<UserInfo>().With(SqlSugar.SqlWith.RowLock).Where(it => it.MemberID == packet.MemberID).Single();

                // 有找到資料
                if (userInfo != null)
                {
                    userInfo.NickName = packet.UpdateData.NickName == null ? userInfo.NickName : packet.UpdateData.NickName;
                    userInfo.Birthday = packet.UpdateData.Birthday == null ? userInfo.Birthday : packet.UpdateData.Birthday;
                    userInfo.BodyHeight = packet.UpdateData.BodyHeight == 0 ? userInfo.BodyHeight : packet.UpdateData.BodyHeight;
                    userInfo.BodyWeight = packet.UpdateData.BodyWeight == 0 ? userInfo.BodyWeight : packet.UpdateData.BodyWeight;
                    userInfo.FrontCover = packet.UpdateData.FrontCover == null ? userInfo.FrontCover : packet.UpdateData.FrontCover;
                    userInfo.Avatar = packet.UpdateData.Avatar == null ? userInfo.Avatar : packet.UpdateData.Avatar;
                    userInfo.Photo = packet.UpdateData.Photo == null ? userInfo.Photo : packet.UpdateData.Photo;
                    userInfo.Mobile = packet.UpdateData.Mobile == null ? userInfo.Mobile : packet.UpdateData.Mobile;
                    userInfo.Gender = packet.UpdateData.Gender == 0 ? userInfo.Gender : packet.UpdateData.Gender;
                    userInfo.County = packet.UpdateData.Country == 0 ? userInfo.County : packet.UpdateData.Country;
                    userInfo.SpecificationModel = packet.UpdateData.SpecificationModel == null ? userInfo.SpecificationModel : packet.UpdateData.SpecificationModel;

                    // 設定DB 交易的起始點
                    db.GetSql().BeginTran();

                    if (db.GetSql().Updateable<UserInfo>(userInfo).With(SqlSugar.SqlWith.RowLock).Where(it => it.MemberID == packet.MemberID).ExecuteCommand() > 0)
                    {
                        rData.Result = (int)UpdateUserInfoResult.ResultDefine.emResult_Success;

                        redis.GetRedis((int)Connect.RedisDB.emRedisDB_User).HashSet($"UserInfo_" + userInfo.MemberID, hashTransfer.TransToHashEntryArray(userInfo));

                        // DB 交易提交
                        db.GetSql().CommitTran();

                        SaveLog($"[Info] MessageFcunction::OnUpdateUserInfo Update User: {packet.MemberID} Info Success");
                    }
                    else
                    {
                        rData.Result = (int)UpdateUserInfoResult.ResultDefine.emResult_Fail;

                        SaveLog($"[Info] MessageFcunction::OnUpdateUserInfo Update User: {packet.MemberID} Info Fail");

                    }
                }
                else
                {
                    rData.Result = (int)UpdateUserInfoResult.ResultDefine.emResult_Fail;

                    SaveLog($"[Info] MessageFcunction::OnUpdateUserInfo Can Not Find User: {packet.MemberID}");

                }
            }
            catch (Exception ex)
            {
                SaveLog($"[Error] MessageFcunction::OnUpdateUserInfo Catch Error, Msg: {ex.Message}");

                rData.Result = (int)UpdateUserInfoResult.ResultDefine.emResult_Fail;
            }

            // DB 交易失敗, 啟動Rollback
            if (rData.Result != (int)UpdateUserInfoResult.ResultDefine.emResult_Success)
            {
                db.GetSql().RollbackTran();
            }

            JObject jsMain = new JObject();
            jsMain.Add("CmdID", (int)S2C_CmdID.emUpdateUserInfoResult);
            jsMain.Add("Data", JsonConvert.DeserializeObject<JObject>(JsonConvert.SerializeObject(rData)));

            ret = jsMain.ToString();

            return ret;
        }
        
        public string OnUpdatePassword(string data)
        {
            string ret = "";

            UpdatePassword packet = JsonConvert.DeserializeObject<UpdatePassword>(data);

            UpdatePasswordResult rData = new UpdatePasswordResult();
            rData.Action = packet.Action;

            try
            {
                UserAccount account = db.GetSql().Queryable<UserAccount>().With(SqlSugar.SqlWith.RowLock).Where(it => it.MemberID == packet.MemberID).Single();

                // 有找到帳號
                if (account != null)
                {
                    bool canUpdate = false;

                    SaveLog($"[Warning] MessageFcunction::OnUpdateUserInfo, Member:{packet.MemberID} Action:{packet.Action}");

                    if (packet.Action == (int)UpdatePassword.ActionDefine.emAction_UpdatePwd)
                    {
                        // 舊密碼相同
                        if (account.Password == packet.Password)
                        {
                            canUpdate = true;
                        }
                        else
                        {
                            canUpdate = false;

                            rData.Result = (int)UpdatePasswordResult.ResultDefine.emResult_OldPwdError;

                            SaveLog($"[Warning] MessageFcunction::OnUpdateUserInfo, Member:{packet.MemberID} Old Password Error");
                        }
                    }
                    // 忘記密碼可直接修改
                    else if (packet.Action == (int)UpdatePassword.ActionDefine.emAction_ForgetPwd)
                    {
                        canUpdate = true;
                    }

                    if (canUpdate)
                    {
                        if (db.GetSql().Updateable<UserAccount>().SetColumns(it => new UserAccount() { Password = packet.NewPassword }).With(SqlSugar.SqlWith.RowLock).Where(it => it.MemberID == packet.MemberID).ExecuteCommand() > 0)
                        {
                            rData.Result = (int)UpdatePasswordResult.ResultDefine.emResult_Success;

                            account.Password = packet.NewPassword;
                            redis.GetRedis((int)Connect.RedisDB.emRedisDB_User).HashSet($"UserAccount_" + account.Email, hashTransfer.TransToHashEntryArray(account));

                            SaveLog($"[Warning] MessageFcunction::OnUpdateUserInfo Member:{packet.MemberID} Update Password Success ");
                        }
                        else
                        {
                            rData.Result = (int)UpdatePasswordResult.ResultDefine.emResult_Fail;

                            SaveLog($"[Warning] MessageFcunction::OnUpdateUserInfo Member:{packet.MemberID} Can Not Change Password");

                        }

                    }
                    else
                    {
                        rData.Result = (int)UpdatePasswordResult.ResultDefine.emResult_Fail;

                        SaveLog($"[Warning] MessageFcunction::OnUpdateUserInfo Member:{packet.MemberID} Update Fail");
                    }
                }
                else
                {
                    rData.Result = (int)UpdatePasswordResult.ResultDefine.emResult_Fail;

                    SaveLog($"[Warning] MessageFcunction::OnUpdateUserInfo Can Not Find Account:{packet.MemberID}");
                }
            }
            catch (Exception ex)
            {
                SaveLog($"[Error] MessageFcunction::OnUpdateUserInfo Catch Error, Msg:{ex.Message}");

                rData.Result = (int)UpdatePasswordResult.ResultDefine.emResult_Fail;
            }

            JObject jsMain = new JObject();
            jsMain.Add("CmdID", (int)S2C_CmdID.emUpdatePasswordResult);
            jsMain.Add("Data", JsonConvert.DeserializeObject<JObject>(JsonConvert.SerializeObject(rData)));

            ret = jsMain.ToString();

            return ret;
        }
        
        public string OnUpdateFriendList(string data)
        {
            string ret = "";

            UpdateFriendList packet = JsonConvert.DeserializeObject<UpdateFriendList>(data);

            UpdateFriendListResult rData = new UpdateFriendListResult();
            rData.Action = packet.Action;

            try
            {
                // 不能為同一人
                if (packet.MemberID != packet.FriendID)
                {
                    UserInfo userInfo = db.GetSql().Queryable<UserInfo>().With(SqlSugar.SqlWith.RowLock).Where(it => it.MemberID == packet.MemberID).Single();

                    // 有找到會員資料
                    if (userInfo != null)
                    {
                        UserInfo friendInfo = db.GetSql().Queryable<UserInfo>().With(SqlSugar.SqlWith.RowLock).Where(it => it.MemberID == packet.FriendID).Single();

                        // 有找到好友的會員資料
                        if (friendInfo != null)
                        {
                            JArray jaData = JArray.Parse(userInfo.FriendList);

                            List<string> idList = jaData.ToObject<List<string>>();

                            // 新增
                            if (packet.Action == (int)UpdateFriendList.ActionDefine.emAction_Add)
                            {
                                if (!idList.Contains(packet.FriendID))
                                {
                                    idList.Add(packet.FriendID);

                                    rData.Result = (int)UpdateFriendListResult.ResultDefine.emResult_Success;

                                }
                                else
                                {
                                    rData.Result = (int)UpdateFriendListResult.ResultDefine.emResult_Fail;
                                }

                            }
                            // 刪除
                            else if (packet.Action == (int)UpdateFriendList.ActionDefine.emAction_Delete)
                            {
                                if (idList.Contains(packet.FriendID))
                                {
                                    idList.Remove(packet.FriendID);

                                    rData.Result = (int)UpdateFriendListResult.ResultDefine.emResult_Success;
                                }
                                else
                                {
                                    rData.Result = (int)UpdateFriendListResult.ResultDefine.emResult_Fail;
                                }
                            }

                            if (rData.Result == (int)UpdateFriendListResult.ResultDefine.emResult_Success)
                            {
                                JArray jsNew = JArray.FromObject(idList);

                                if (db.GetSql().Updateable<UserInfo>().SetColumns(it => new UserInfo() { FriendList = jsNew.ToString() }).With(SqlSugar.SqlWith.RowLock).Where(it => it.MemberID == packet.MemberID).ExecuteCommand() > 0)
                                {
                                    rData.Result = (int)UpdateFriendListResult.ResultDefine.emResult_Success;

                                    userInfo.FriendList = jsNew.ToString();
                                    redis.GetRedis((int)Connect.RedisDB.emRedisDB_User).HashSet($"UserInfo_" + userInfo.MemberID, hashTransfer.TransToHashEntryArray(userInfo));

                                    SaveLog($"[Error] MessageFcunction::OnUpdateFriendList Member: {packet.MemberID} Update FriendList Success");

                                }
                                else
                                {
                                    rData.Result = (int)UpdateFriendListResult.ResultDefine.emResult_Fail;

                                    SaveLog($"[Error] MessageFcunction::OnUpdateFriendList Member: {packet.MemberID} Update FriendList Fail");
                                }
                            }

                        }
                        else
                        {
                            rData.Result = (int)UpdateFriendListResult.ResultDefine.emResult_Fail;

                            SaveLog($"[Error] MessageFcunction::OnUpdateFriendList Can Not Find  Firend Member:{packet.FriendID}");
                        }
                    }
                    else
                    {
                        rData.Result = (int)UpdateFriendListResult.ResultDefine.emResult_Fail;

                        SaveLog($"[Error] MessageFcunction::OnUpdateFriendList Can Not Find Member:{packet.MemberID}");
                    }
                }
                else
                {
                    rData.Result = (int)UpdateFriendListResult.ResultDefine.emResult_Fail;

                    SaveLog($"[Error] MessageFcunction::OnUpdateFriendList Member: {packet.MemberID} Is Same With Friend Member: {packet.FriendID}");
                }

            }
            catch (Exception ex)
            {
                SaveLog($"[Error] MessageFcunction::OnUpdateFriendList Catch Error, Msg:{ex.Message}");

                rData.Result = (int)UpdateFriendListResult.ResultDefine.emResult_Fail;
            }

            JObject jsMain = new JObject();
            jsMain.Add("CmdID", (int)S2C_CmdID.emUpdateFriendListResult);
            jsMain.Add("Data", JsonConvert.DeserializeObject<JObject>(JsonConvert.SerializeObject(rData)));

            ret = jsMain.ToString();

            return ret;
        }
        
        public string OnUpdateBlackList(string data)
        {
            string ret = "";

            UpdateBlackList packet = JsonConvert.DeserializeObject<UpdateBlackList>(data);

            UpdateBlackListResult rData = new UpdateBlackListResult();
            rData.Action = packet.Action;

            UserInfo userInfo = null;

            try
            {
                if (packet.MemberID != packet.BlackID)
                {
                    userInfo = db.GetSql().Queryable<UserInfo>().With(SqlSugar.SqlWith.RowLock).Where(it => it.MemberID == packet.MemberID).Single();

                    // 有找到會員
                    if (userInfo != null)
                    {
                        UserInfo blackInfo = db.GetSql().Queryable<UserInfo>().With(SqlSugar.SqlWith.RowLock).Where(it => it.MemberID == packet.MemberID).Single();

                        if (blackInfo != null)
                        {
                            JArray jsData = JArray.Parse(userInfo.BlackList);

                            List<string> idList = jsData.ToObject<List<string>>();

                            // 新增
                            if (packet.Action == (int)UpdateBlackList.ActionDefine.emAction_Add)
                            {
                                if (!idList.Contains(packet.BlackID))
                                {
                                    idList.Add(packet.BlackID);

                                    rData.Result = (int)UpdateBlackListResult.ResultDefine.emResult_Success;
                                }
                                else
                                {
                                    rData.Result = (int)UpdateBlackListResult.ResultDefine.emResult_Fail;
                                }

                            }
                            // 刪除
                            else if (packet.Action == (int)UpdateBlackList.ActionDefine.emAction_Delete)
                            {
                                if (idList.Contains(packet.BlackID))
                                {
                                    idList.Remove(packet.BlackID);

                                    rData.Result = (int)UpdateBlackListResult.ResultDefine.emResult_Success;
                                }
                                else
                                {
                                    rData.Result = (int)UpdateBlackListResult.ResultDefine.emResult_Fail;
                                }
                            }

                            if (rData.Result == (int)UpdateBlackListResult.ResultDefine.emResult_Success)
                            {
                                JArray jsNew = JArray.FromObject(idList);

                                // 設定DB 交易的起始點
                                db.GetSql().BeginTran();

                                if (db.GetSql().Updateable<UserInfo>().SetColumns(it => new UserInfo() { BlackList = jsNew.ToString() }).With(SqlSugar.SqlWith.RowLock).Where(it => it.MemberID == packet.MemberID).ExecuteCommand() > 0)
                                {
                                    rData.Result = (int)UpdateBlackListResult.ResultDefine.emResult_Success;

                                    userInfo.BlackList = jsNew.ToString();

                                    SaveLog($"[Info] MessageFcunction::OnUpdateBlackList Member: {packet.MemberID} Update BlackList Success");

                                    // 檢查是否有在好友名單中
                                    {
                                        JArray jsFriendData = JArray.Parse(userInfo.FriendList);

                                        List<string> friendList = jsFriendData.ToObject<List<string>>();

                                        if (friendList.Contains(packet.BlackID))
                                        {
                                            friendList.Remove(packet.BlackID);

                                            JArray jsFriendNew = JArray.FromObject(friendList);

                                            if (db.GetSql().Updateable<UserInfo>().SetColumns(it => new UserInfo() { FriendList = jsFriendNew.ToString() }).With(SqlSugar.SqlWith.RowLock).Where(it => it.MemberID == packet.MemberID).ExecuteCommand() > 0)
                                            {
                                                rData.Result = (int)UpdateBlackListResult.ResultDefine.emResult_Success;

                                                userInfo.FriendList = jsFriendNew.ToString();

                                                SaveLog($"[Info] MessageFcunction::OnUpdateBlackList Remove MemberID:{packet.MemberID} From Friend List");
                                            }
                                            else
                                            {
                                                rData.Result = (int)UpdateBlackListResult.ResultDefine.emResult_Fail;

                                                SaveLog($"[Error] MessageFcunction::OnUpdateBlackList Can Not Remove MemberID:{packet.MemberID} From Friend List");
                                            }
                                        }
                                    }

                                }
                                else
                                {
                                    rData.Result = (int)UpdateBlackListResult.ResultDefine.emResult_Fail;

                                    SaveLog($"[Error] MessageFcunction::OnUpdateBlackList Member: {packet.MemberID} Update BlackList Success");

                                }
                            }
                        }
                        else
                        {
                            rData.Result = (int)UpdateBlackListResult.ResultDefine.emResult_Fail;

                            SaveLog($"[Error] MessageFcunction::OnUpdateBlackList Can Not Find Black Member:{packet.BlackID}");
                        }

                    }
                    else
                    {
                        rData.Result = (int)UpdateBlackListResult.ResultDefine.emResult_Fail;

                        SaveLog($"[Error] MessageFcunction::OnUpdateBlackList Can Not Find Member:{packet.MemberID}");

                    }
                }
                else
                {
                    rData.Result = (int)UpdateBlackListResult.ResultDefine.emResult_Fail;

                    SaveLog($"[Error] MessageFcunction::OnUpdateBlackList Member: {packet.MemberID} Is Same With Black Member: {packet.BlackID}");
                }

            }
            catch (Exception ex)
            {
                SaveLog($"[Error] MessageFcunction::OnUpdateBlackList Catch Error, Msg:{ex.Message}");

                rData.Result = (int)UpdateBlackListResult.ResultDefine.emResult_Fail;
            }

            if (rData.Result == (int)UpdateBlackListResult.ResultDefine.emResult_Success)
            {
                // DB 交易提交
                db.GetSql().CommitTran();

                redis.GetRedis((int)Connect.RedisDB.emRedisDB_User).HashSet($"UserInfo_" + userInfo.MemberID, hashTransfer.TransToHashEntryArray(userInfo));
            }
            else
            {
                // DB 交易失敗, 啟動Rollback
                db.GetSql().RollbackTran();
            }

            JObject jsMain = new JObject();
            jsMain.Add("CmdID", (int)S2C_CmdID.emUpdateBlackListResult);
            jsMain.Add("Data", JsonConvert.DeserializeObject<JObject>(JsonConvert.SerializeObject(rData)));

            ret = jsMain.ToString();

            return ret;
        }

        public string OnUpdateNotifyToken(string data)
        {
            string ret = "";

            UpdateNotifyToken packet = JsonConvert.DeserializeObject<UpdateNotifyToken>(data);

            UpdateNotifyTokenResult rData = new UpdateNotifyTokenResult();

            try
            {
                UserAccount account = db.GetSql().Queryable<UserAccount>().With(SqlSugar.SqlWith.RowLock).Where(it => it.MemberID == packet.MemberID).Single();

                // 有找到資料
                if (account != null)
                {
                    if (db.GetSql().Updateable<UserAccount>().SetColumns(it => new UserAccount() { NotifyToken = packet.NotifyToken }).With(SqlSugar.SqlWith.RowLock).Where(it => it.MemberID == packet.MemberID).ExecuteCommand() > 0)
                    {
                        rData.Result = (int)UpdateNotifyTokenResult.ResultDefine.emResult_Success;

                        account.NotifyToken = packet.NotifyToken;
                        redis.GetRedis((int)Connect.RedisDB.emRedisDB_User).HashSet($"UserAccount_" + account.MemberID, hashTransfer.TransToHashEntryArray(account));

                        SaveLog($"[Info] MessageFcunction::OnUpdateNotifyToken Update User: {packet.MemberID} Info Success");

                    }
                    else
                    {
                        rData.Result = (int)UpdateNotifyTokenResult.ResultDefine.emResult_Fail;

                        SaveLog($"[Info] MessageFcunction::OnUpdateNotifyToken Update User: {packet.MemberID} Info Fail");

                    }
                }
                else
                {
                    rData.Result = (int)UpdateNotifyTokenResult.ResultDefine.emResult_Fail;

                    SaveLog($"[Info] MessageFcunction::OnUpdateNotifyToken Can Not Find User: {packet.MemberID}");

                }
            }
            catch (Exception ex)
            {
                SaveLog($"[Error] MessageFcunction::OnUpdateNotifyToken Catch Error, Msg: {ex.Message}");

                rData.Result = (int)UpdateNotifyTokenResult.ResultDefine.emResult_Fail;
            }

            JObject jsMain = new JObject();
            jsMain.Add("CmdID", (int)S2C_CmdID.emUpdateNotifyTokenResult);
            jsMain.Add("Data", JsonConvert.DeserializeObject<JObject>(JsonConvert.SerializeObject(rData)));

            ret = jsMain.ToString();

            return ret;
        }

    }

}
