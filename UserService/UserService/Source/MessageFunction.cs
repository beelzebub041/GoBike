using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Collections.Concurrent;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Tools;
using Tools.WeekProcess;
using Tools.RedisHashTransfer;
using Tools.FireBaseHandler;

using Service.Source.Define;
using DataBaseDef;
using Connect;
using SqlSugar;
using StackExchange.Redis;
using Tools.NotifyMessage;

using UserPacket.ClientToServer;
using UserPacket.ServerToClient;
using FirebaseAdmin.Auth;

using PostProto;

namespace Service.Source
{
    class MessageFunction
    {
        /// <summary>
        /// hash 轉換器
        /// </summary>
        private RedisHashTransfer hashTransfer = null;

        /// <summary>
        /// 週時間處理
        /// </summary>
        private WeekProcess weekProcess = null;

        /// <summary>
        /// 推播物件
        /// </summary>
        private NotifyMessage ntMsg = null;

        /// <summary>
        /// Logger 物件
        /// </summary>
        private Logger logger = null;

        /// <summary>
        /// 建構式
        /// </summary>
        public MessageFunction()
        {
            hashTransfer = new RedisHashTransfer();

            weekProcess = new WeekProcess();

            ntMsg = new NotifyMessage();
        }

        /// <summary>
        /// 解構式
        /// </summary>
        ~MessageFunction()
        {

        }

        /// <summary>
        ///  初始化
        /// </summary>
        /// <returns> 是否成功初始化 </returns>
        public bool Initialize(Logger logger)
        {
            bool ret = false;

            try
            {
                this.logger = logger;

                if (ntMsg.Initialize(logger))
                {
                    ret = true;

                    FireBaseHandler.Instance.SetLogger(logger);

                    SaveLog("[Info] MessageFcunction::Initialize, Initialize Success");
                }
                else
                {
                    SaveLog($"[Info] MessageFcunction::Initialize, Fail");
                }
            }
            catch (Exception ex)
            {
                SaveLog($"[Error] MessageFcunction::Initialize, Catch Error, Msg:{ex.Message}");
            }

            return ret;
        }

        /// <summary>
        /// 銷毀
        /// </summary>
        /// <returns> 是否成功銷毀 </returns>
        public bool Destory()
        {
            bool ret = true;

            return ret;

        }

        /// <summary>
        /// 儲存Log
        /// </summary>
        /// <param name="msg"> 訊息 </param>
        private void SaveLog(string msg)
        {
            if (logger != null)
            {
                logger.AddLog(msg);
            }
        }

        /// <summary>
        /// 取得 Sql物件
        /// </summary>
        /// <returns> Sql物件 </returns>
        private SqlSugarClient GetSql()
        {
            return DataBaseConnect.Instance.GetSql();
        }

        /// <summary>
        /// 取得Redis 物件
        /// </summary>
        /// <param name="idx"> Redis資料庫索引</param>
        /// <returns> Redis物件 </returns>
        private IDatabase GetRedis(int idx)
        {
            return RedisConnect.Instance.GetRedis(idx);
        }

        /// <summary>
        /// 使用者登入
        /// </summary>
        /// <param name="data"> 封包資料 </param>
        /// <returns> 結果 </returns>
        public string OnUserLogin(string data)
        {
            string ret = "";
            
            UserLogin packet = JsonConvert.DeserializeObject<UserLogin>(data);

            UserLoginResult rData = new UserLoginResult();

            try
            {
                string uID = FireBaseHandler.Instance.CheckFireBaseUserToken(packet.Token).Result;

                if (uID != "")
                {
                    UserAccount account = GetSql().Queryable<UserAccount>().Where(it => it.UID == uID).Single();

                    // 有找到帳號
                    if (account != null)
                    {
                        UserInfo userInfo = GetSql().Queryable<UserInfo>().Where(it => it.MemberID == account.MemberID).Single();

                        // 有找到會員
                        if (userInfo != null)
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
                        string dateTime = DateTime.UtcNow.ToString("yyyy-MM-dd hh:mm:ss");

                        string guidAll = Guid.NewGuid().ToString();

                        string[] guidList = guidAll.Split('-');

                        UserAccount newAccount = new UserAccount();

                        // 建立新帳號
                        newAccount.MemberID = "Dblha-" + guidList[0];        // 取GUID前8碼
                        newAccount.UID = uID;
                        newAccount.NotifyToken = "";
                        newAccount.RegisterSource = packet.LoginSource;
                        newAccount.RegisterDate = dateTime;

                        rData.MemberID = newAccount.MemberID;

                        UserInfo newInfo = new UserInfo();

                        // 新增使用者資訊
                        newInfo.MemberID = newAccount.MemberID;
                        newInfo.Email = packet.Email == null ? "" : packet.Email;
                        newInfo.NickName = packet.NickName == null ? "" : packet.NickName;
                        newInfo.Birthday = "";
                        newInfo.BodyHeight = 0;
                        newInfo.BodyWeight = 0;
                        newInfo.FrontCover = "";
                        newInfo.Avatar = packet.Avatar == null ? "" : packet.Avatar; ;
                        newInfo.Photo = "";
                        newInfo.Mobile = "";
                        newInfo.County = -1;
                        newInfo.TeamList = "[]";
                        newInfo.FriendList = "[]";
                        newInfo.BlackList = "[]";
                        newInfo.SpecificationModel = "";


                        RideData rideData = new RideData();

                        // 新增騎乘資料
                        rideData.MemberID = newAccount.MemberID;
                        rideData.TotalDistance = 0;
                        rideData.TotalAltitude = 0;
                        rideData.TotalRideTime = 0;

                        WeekRideData curWeek = new WeekRideData();

                        // 新增本週騎乘資料
                        string firsDay = weekProcess.GetWeekFirstDay(DateTime.UtcNow);
                        string lastDay = weekProcess.GetWeekLastDay(DateTime.UtcNow);

                        curWeek.MemberID = newAccount.MemberID;
                        curWeek.WeekFirstDay = firsDay;
                        curWeek.WeekLastDay = lastDay;
                        curWeek.WeekDistance = 0;

                        // 設定DB 交易的起始點
                        GetSql().BeginTran();

                        // 寫入資料庫
                        if (GetSql().Insertable(newAccount).With(SqlSugar.SqlWith.TabLockX).ExecuteCommand() > 0)
                        {

                            if (GetSql().Insertable(newInfo).With(SqlSugar.SqlWith.TabLockX).ExecuteCommand() > 0
                                && GetSql().Insertable(rideData).With(SqlSugar.SqlWith.TabLockX).ExecuteCommand() > 0
                                && GetSql().Insertable(curWeek).With(SqlSugar.SqlWith.TabLockX).ExecuteCommand() > 0)
                            {
                                rData.Result = (int)UserLoginResult.ResultDefine.emResult_Success;

                                GetRedis((int)Connect.RedisDB.emRedisDB_User).HashSet($"UserAccount_" + newAccount.MemberID, hashTransfer.TransToHashEntryArray(newAccount));
                                GetRedis((int)Connect.RedisDB.emRedisDB_User).HashSet($"UserInfo_" + newInfo.MemberID, hashTransfer.TransToHashEntryArray(newInfo));
                                GetRedis((int)Connect.RedisDB.emRedisDB_Ride).HashSet($"RideData_" + rideData.MemberID, hashTransfer.TransToHashEntryArray(rideData));
                                GetRedis((int)Connect.RedisDB.emRedisDB_Ride).HashSet($"RideData_" + rideData.MemberID, hashTransfer.TransToHashEntryArray(rideData));
                                GetRedis((int)Connect.RedisDB.emRedisDB_Ride).HashSet($"CurWeekRideData_" + newAccount.MemberID, hashTransfer.TransToHashEntryArray(curWeek));

                                SaveLog($"[Info] MessageFcunction::OnUserLogin Create New Account Success");

                                // DB 交易提交
                                GetSql().CommitTran();
                            }
                            else
                            {
                                rData.Result = (int)UserLoginResult.ResultDefine.emResult_Fail;

                                SaveLog($"[Warning] MessageFcunction::OnUserLogin Can Not Inseart User Info Or Ride Data");
                            }
                        }
                        else
                        {
                            rData.Result = (int)UserLoginResult.ResultDefine.emResult_Fail;

                            SaveLog($"[Warning] MessageFcunction::OnUserLogin UID: {newAccount.UID} Repeat");
                        }



                    }
                }
                else
                {
                    SaveLog("[Warning] MessageFcunction::OnUserLogin Fire Base Token Certification Error");

                    rData.Result = (int)UserLoginResult.ResultDefine.emResult_Fail;
                }
            }
            catch (Exception ex)
            {
                SaveLog($"[Error] MessageFcunction::OnUserLogin Catch Error, Msg:{ex.Message}");

                rData.Result = (int)UserLoginResult.ResultDefine.emResult_Fail;
            }

            // DB 交易失敗, 啟動Rollback
            if (rData.Result != (int)UserLoginResult.ResultDefine.emResult_Success)
            {
                GetSql().RollbackTran();
            }

            JObject jsMain = new JObject();
            jsMain.Add("CmdID", (int)S2C_CmdID.emUserLoginResult);
            jsMain.Add("Data", JsonConvert.DeserializeObject<JObject>(JsonConvert.SerializeObject(rData)));

            ret = jsMain.ToString();

            return ret;
        }

        /// <summary>
        /// 更新使用者資料
        /// </summary>
        /// <param name="data"> 封包資料 </param>
        /// <returns> 結果 </returns>
        public string OnUpdateUserInfo(string data)
        {
            string ret = "";

            UpdateUserInfo packet = JsonConvert.DeserializeObject<UpdateUserInfo>(data);

            UpdateUserInfoResult rData = new UpdateUserInfoResult();

            UserInfo userInfo = null;

            try
            {
                userInfo = GetSql().Queryable<UserInfo>().With(SqlSugar.SqlWith.RowLock).Where(it => it.MemberID == packet.MemberID).Single();

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
                    GetSql().BeginTran();

                    if (GetSql().Updateable<UserInfo>(userInfo).With(SqlSugar.SqlWith.RowLock).Where(it => it.MemberID == packet.MemberID).ExecuteCommand() > 0)
                    {
                        rData.Result = (int)UpdateUserInfoResult.ResultDefine.emResult_Success;

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

            if (rData.Result == (int)UpdateUserInfoResult.ResultDefine.emResult_Success)
            {
                GetRedis((int)Connect.RedisDB.emRedisDB_User).HashSet($"UserInfo_" + userInfo.MemberID, hashTransfer.TransToHashEntryArray(userInfo));

                // DB 交易提交
                GetSql().CommitTran();
            }
            else
            {
                // DB 交易失敗, 啟動Rollback
                GetSql().RollbackTran();
            }

            JObject jsMain = new JObject();
            jsMain.Add("CmdID", (int)S2C_CmdID.emUpdateUserInfoResult);
            jsMain.Add("Data", JsonConvert.DeserializeObject<JObject>(JsonConvert.SerializeObject(rData)));

            ret = jsMain.ToString();

            return ret;
        }
        
        /// <summary>
        /// 更新密碼
        /// </summary>
        /// <param name="data"> 封包資料 </param>
        /// <returns> 結果 </returns>
        //public string OnUpdatePassword(string data)
        //{
        //    string ret = "";

        //    UpdatePassword packet = JsonConvert.DeserializeObject<UpdatePassword>(data);

        //    UpdatePasswordResult rData = new UpdatePasswordResult();
        //    rData.Action = packet.Action;

        //    try
        //    {
        //        UserAccount account = GetSql().Queryable<UserAccount>().With(SqlSugar.SqlWith.RowLock).Where(it => it.MemberID == packet.MemberID).Single();

        //        // 有找到帳號
        //        if (account != null)
        //        {
        //            bool canUpdate = false;

        //            SaveLog($"[Warning] MessageFcunction::OnUpdateUserInfo, Member:{packet.MemberID} Action:{packet.Action}");

        //            if (packet.Action == (int)UpdatePassword.ActionDefine.emAction_UpdatePwd)
        //            {
        //                // 舊密碼相同
        //                if (account.Password == packet.Password)
        //                {
        //                    canUpdate = true;
        //                }
        //                else
        //                {
        //                    canUpdate = false;

        //                    rData.Result = (int)UpdatePasswordResult.ResultDefine.emResult_OldPwdError;

        //                    SaveLog($"[Warning] MessageFcunction::OnUpdateUserInfo, Member:{packet.MemberID} Old Password Error");
        //                }
        //            }
        //            // 忘記密碼可直接修改
        //            else if (packet.Action == (int)UpdatePassword.ActionDefine.emAction_ForgetPwd)
        //            {
        //                canUpdate = true;
        //            }

        //            if (canUpdate)
        //            {
        //                if (GetSql().Updateable<UserAccount>().SetColumns(it => new UserAccount() { Password = packet.NewPassword }).With(SqlSugar.SqlWith.RowLock).Where(it => it.MemberID == packet.MemberID).ExecuteCommand() > 0)
        //                {
        //                    rData.Result = (int)UpdatePasswordResult.ResultDefine.emResult_Success;

        //                    account.Password = packet.NewPassword;
        //                    GetRedis((int)Connect.RedisDB.emRedisDB_User).HashSet($"UserAccount_" + account.Email, hashTransfer.TransToHashEntryArray(account));

        //                    SaveLog($"[Warning] MessageFcunction::OnUpdateUserInfo Member:{packet.MemberID} Update Password Success ");
        //                }
        //                else
        //                {
        //                    rData.Result = (int)UpdatePasswordResult.ResultDefine.emResult_Fail;

        //                    SaveLog($"[Warning] MessageFcunction::OnUpdateUserInfo Member:{packet.MemberID} Can Not Change Password");

        //                }

        //            }
        //            else
        //            {
        //                rData.Result = (int)UpdatePasswordResult.ResultDefine.emResult_Fail;

        //                SaveLog($"[Warning] MessageFcunction::OnUpdateUserInfo Member:{packet.MemberID} Update Fail");
        //            }
        //        }
        //        else
        //        {
        //            rData.Result = (int)UpdatePasswordResult.ResultDefine.emResult_Fail;

        //            SaveLog($"[Warning] MessageFcunction::OnUpdateUserInfo Can Not Find Account:{packet.MemberID}");
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        SaveLog($"[Error] MessageFcunction::OnUpdateUserInfo Catch Error, Msg:{ex.Message}");

        //        rData.Result = (int)UpdatePasswordResult.ResultDefine.emResult_Fail;
        //    }

        //    JObject jsMain = new JObject();
        //    jsMain.Add("CmdID", (int)S2C_CmdID.emUpdatePasswordResult);
        //    jsMain.Add("Data", JsonConvert.DeserializeObject<JObject>(JsonConvert.SerializeObject(rData)));

        //    ret = jsMain.ToString();

        //    return ret;
        //}
        
        /// <summary>
        /// 更新好友列表
        /// </summary>
        /// <param name="data"> 封包資料 </param>
        /// <returns> 結果 </returns>
        public string OnUpdateFriendList(string data)
        {
            string ret = "";

            UpdateFriendList packet = JsonConvert.DeserializeObject<UpdateFriendList>(data);

            UpdateFriendListResult rData = new UpdateFriendListResult();
            rData.Action = packet.Action;

            UserInfo userInfo_Invite = null;    // 邀請者
            UserInfo userInfo_Friend = null;    // 被邀請者

            try
            {
                // 不能為同一人
                if (packet.MemberID != packet.FriendID)
                {
                    userInfo_Invite = GetSql().Queryable<UserInfo>().With(SqlSugar.SqlWith.RowLock).Where(it => it.MemberID == packet.MemberID).Single();

                    // 有找到會員資料
                    if (userInfo_Invite != null)
                    {
                        userInfo_Friend = GetSql().Queryable<UserInfo>().With(SqlSugar.SqlWith.RowLock).Where(it => it.MemberID == packet.FriendID).Single();

                        // 有找到好友的會員資料
                        if (userInfo_Invite != null)
                        {
                            JArray jaData_User = JArray.Parse(userInfo_Invite.FriendList);
                            List<string> friendList_User = jaData_User.ToObject<List<string>>();

                            JArray jaData_Friend = JArray.Parse(userInfo_Friend.FriendList);
                            List<string> friendList_Friend = jaData_Friend.ToObject<List<string>>();

                            // 新增
                            if (packet.Action == (int)UpdateFriendList.ActionDefine.emAction_Add)
                            {
                                if (!friendList_User.Contains(packet.FriendID) && !friendList_Friend.Contains(packet.MemberID))
                                {
                                    friendList_User.Add(packet.FriendID);
                                    friendList_Friend.Add(packet.MemberID);

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
                                if (friendList_User.Contains(packet.FriendID) && friendList_Friend.Contains(packet.MemberID))
                                {
                                    friendList_User.Remove(packet.FriendID);
                                    friendList_Friend.Remove(packet.MemberID);

                                    rData.Result = (int)UpdateFriendListResult.ResultDefine.emResult_Success;
                                }
                                else
                                {
                                    rData.Result = (int)UpdateFriendListResult.ResultDefine.emResult_Fail;
                                }
                            }

                            if (rData.Result == (int)UpdateFriendListResult.ResultDefine.emResult_Success)
                            {
                                JArray jsUser = JArray.FromObject(friendList_User);
                                JArray jsFriend = JArray.FromObject(friendList_Friend);

                                // 設定DB 交易的起始點
                                GetSql().BeginTran();

                                if (GetSql().Updateable<UserInfo>().SetColumns(it => new UserInfo() { FriendList = jsUser.ToString() }).With(SqlSugar.SqlWith.RowLock).Where(it => it.MemberID == packet.MemberID).ExecuteCommand() > 0 &&
                                    GetSql().Updateable<UserInfo>().SetColumns(it => new UserInfo() { FriendList = jsFriend.ToString() }).With(SqlSugar.SqlWith.RowLock).Where(it => it.MemberID == packet.FriendID).ExecuteCommand() > 0)
                                {
                                    rData.Result = (int)UpdateFriendListResult.ResultDefine.emResult_Success;

                                    userInfo_Invite.FriendList = jsUser.ToString();

                                    userInfo_Friend.FriendList = jsFriend.ToString();

                                    SaveLog($"[Info] MessageFcunction::OnUpdateFriendList Member: {packet.MemberID} Update FriendList Success");

                                    // 發送推播通知
                                    {
                                        if (packet.Action == (int)UpdateFriendList.ActionDefine.emAction_Add)
                                        {
                                            UserAccount friendAccount = GetSql().Queryable<UserAccount>().With(SqlSugar.SqlWith.RowLock).Where(it => it.MemberID == packet.FriendID).Single();

                                            if (friendAccount != null)
                                            {
                                                string sTitle = $"好友通知";

                                                string sNotifyMsg = $"{userInfo_Invite.NickName} 將您加入好友";

                                                ntMsg.NotifyMsgToDevice(friendAccount.NotifyToken, sTitle, sNotifyMsg, (int)NotifyID.User_AddFriend);
                                            }
                                        }
                                    }
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

            if (rData.Result == (int)UpdateBlackListResult.ResultDefine.emResult_Success)
            {
                // DB 交易提交
                GetSql().CommitTran();

                GetRedis((int)Connect.RedisDB.emRedisDB_User).HashSet($"UserInfo_" + userInfo_Invite.MemberID, hashTransfer.TransToHashEntryArray(userInfo_Invite));
                GetRedis((int)Connect.RedisDB.emRedisDB_User).HashSet($"UserInfo_" + userInfo_Friend.MemberID, hashTransfer.TransToHashEntryArray(userInfo_Friend));

                // 更新Redis的新增好友列表
                {
                    // 新增/刪除好友的會員
                    {
                        string dateTime = DateTime.UtcNow.ToString("yyyy-MM-dd hh:mm:ss");

                        string sKey = $"NewFriendList_" + packet.MemberID;

                        string sNewFriendList = "[]";

                        if (GetRedis((int)Connect.RedisDB.emRedisDB_User).KeyExists(sKey))
                        {
                            sNewFriendList = GetRedis((int)Connect.RedisDB.emRedisDB_User).StringGet(sKey);
                        }

                        JArray jsNewFriendList = JArray.Parse(sNewFriendList);

                        JArray jsNewFriendInfo = new JArray();
                        jsNewFriendInfo.Add(packet.FriendID);
                        jsNewFriendInfo.Add(dateTime);

                        if (packet.Action == (int)UpdateFriendList.ActionDefine.emAction_Add)
                        {
                            jsNewFriendList.Add(jsNewFriendInfo);
                        }
                        else
                        {
                            foreach (JArray jsInfo in jsNewFriendList)
                            {
                                if (jsInfo[0].ToString() == packet.FriendID)
                                {
                                    jsNewFriendList.Remove(jsInfo);

                                    break;
                                }
                            }
                        }

                        GetRedis((int)Connect.RedisDB.emRedisDB_User).StringSet(sKey, jsNewFriendList.ToString());

                    }

                    // 被加好友的會員
                    {
                        string dateTime = DateTime.UtcNow.ToString("yyyy-MM-dd hh:mm:ss");

                        string sKey = $"NewFriendList_" + packet.FriendID;

                        string sNewFriendList = "[]";

                        if (GetRedis((int)Connect.RedisDB.emRedisDB_User).KeyExists(sKey))
                        {
                            sNewFriendList = GetRedis((int)Connect.RedisDB.emRedisDB_User).StringGet(sKey);
                        }

                        JArray jsNewFriendList = JArray.Parse(sNewFriendList);

                        JArray jsNewFriendInfo = new JArray();
                        jsNewFriendInfo.Add(packet.MemberID);
                        jsNewFriendInfo.Add(dateTime);

                        if (packet.Action == (int)UpdateFriendList.ActionDefine.emAction_Add)
                        {
                            jsNewFriendList.Add(jsNewFriendInfo);
                        }
                        else
                        {
                            foreach (JArray jsInfo in jsNewFriendList)
                            {
                                if (jsInfo[0].ToString() == packet.MemberID)
                                {
                                    jsNewFriendList.Remove(jsInfo);

                                    break;
                                }
                            }
                        }

                        GetRedis((int)Connect.RedisDB.emRedisDB_User).StringSet(sKey, jsNewFriendList.ToString());

                    }

                }

                try
                {
                    // 傳送資料到Post Service, 更新塗鴉牆
                    var postClient = GRPCClient.Instance.GetClient();

                    UpdateMemberPostShowList updateInfo = new UpdateMemberPostShowList();
                    updateInfo.MemberID = packet.MemberID;

                    if (postClient != null)
                    {
                        var reply = postClient.UpdatePostShowListFun(updateInfo);
                    }

                }
                catch (Exception postEx)
                {
                    SaveLog($"[Error] Controller::OnUpdateFriendList, Post GRPC Catch Error, Msg:{postEx.Message}");
                }
            }
            else
            {
                // DB 交易失敗, 啟動Rollback
                GetSql().RollbackTran();
            }

            JObject jsMain = new JObject();
            jsMain.Add("CmdID", (int)S2C_CmdID.emUpdateFriendListResult);
            jsMain.Add("Data", JsonConvert.DeserializeObject<JObject>(JsonConvert.SerializeObject(rData)));

            ret = jsMain.ToString();

            return ret;
        }

        /// <summary>
        /// 更新黑名單列表
        /// </summary>
        /// <param name="data"> 封包資料 </param>
        /// <returns> 結果 </returns>
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
                    userInfo = GetSql().Queryable<UserInfo>().With(SqlSugar.SqlWith.RowLock).Where(it => it.MemberID == packet.MemberID).Single();

                    // 有找到會員
                    if (userInfo != null)
                    {
                        UserInfo blackInfo = GetSql().Queryable<UserInfo>().With(SqlSugar.SqlWith.RowLock).Where(it => it.MemberID == packet.MemberID).Single();

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
                                GetSql().BeginTran();

                                if (GetSql().Updateable<UserInfo>().SetColumns(it => new UserInfo() { BlackList = jsNew.ToString() }).With(SqlSugar.SqlWith.RowLock).Where(it => it.MemberID == packet.MemberID).ExecuteCommand() > 0)
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

                                            if (GetSql().Updateable<UserInfo>().SetColumns(it => new UserInfo() { FriendList = jsFriendNew.ToString() }).With(SqlSugar.SqlWith.RowLock).Where(it => it.MemberID == packet.MemberID).ExecuteCommand() > 0)
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
                GetSql().CommitTran();

                GetRedis((int)Connect.RedisDB.emRedisDB_User).HashSet($"UserInfo_" + userInfo.MemberID, hashTransfer.TransToHashEntryArray(userInfo));
            }
            else
            {
                // DB 交易失敗, 啟動Rollback
                GetSql().RollbackTran();
            }

            JObject jsMain = new JObject();
            jsMain.Add("CmdID", (int)S2C_CmdID.emUpdateBlackListResult);
            jsMain.Add("Data", JsonConvert.DeserializeObject<JObject>(JsonConvert.SerializeObject(rData)));

            ret = jsMain.ToString();

            return ret;
        }

        /// <summary>
        /// 更新推播Token
        /// </summary>
        /// <param name="data"> 封包資料 </param>
        /// <returns> 結果 </returns>
        public string OnUpdateNotifyToken(string data)
        {
            string ret = "";

            UpdateNotifyToken packet = JsonConvert.DeserializeObject<UpdateNotifyToken>(data);

            UpdateNotifyTokenResult rData = new UpdateNotifyTokenResult();

            try
            {
                UserAccount account = GetSql().Queryable<UserAccount>().With(SqlSugar.SqlWith.RowLock).Where(it => it.MemberID == packet.MemberID).Single();

                // 有找到資料
                if (account != null)
                {
                    if (GetSql().Updateable<UserAccount>().SetColumns(it => new UserAccount() { NotifyToken = packet.NotifyToken }).With(SqlSugar.SqlWith.RowLock).Where(it => it.MemberID == packet.MemberID).ExecuteCommand() > 0)
                    {
                        rData.Result = (int)UpdateNotifyTokenResult.ResultDefine.emResult_Success;

                        account.NotifyToken = packet.NotifyToken;
                        GetRedis((int)Connect.RedisDB.emRedisDB_User).HashSet($"UserAccount_" + account.MemberID, hashTransfer.TransToHashEntryArray(account));

                        ////刪除其他相同的Token
                        { 
                            List<UserAccount> accountList = GetSql().Queryable<UserAccount>().With(SqlSugar.SqlWith.RowLock).Where(it => it.NotifyToken == packet.NotifyToken).ToList();

                            for (int idx = 0; idx < accountList.Count(); idx++)
                            {
                                if (accountList[idx].MemberID != account.MemberID)
                                {
                                    GetSql().Updateable<UserAccount>().SetColumns(it => new UserAccount() { NotifyToken = "" }).With(SqlSugar.SqlWith.RowLock).Where(it => it.MemberID == accountList[idx].MemberID).ExecuteCommand();

                                    accountList[idx].NotifyToken = "";
                                    GetRedis((int)Connect.RedisDB.emRedisDB_User).HashSet($"UserAccount_" + accountList[idx].MemberID, hashTransfer.TransToHashEntryArray(accountList[idx]));
                                }
                            }
                        }

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

        /// <summary>
        /// 取得新增好友列表
        /// </summary>
        /// <param name="data"> 封包資料 </param>
        /// <returns> 結果 </returns>
        public string onGetNewFriendList(string data)
        {
            string ret = "";

            GetNewFriendList packet = JsonConvert.DeserializeObject<GetNewFriendList>(data);

            GetNewFriendListResult rData = new GetNewFriendListResult();

            UserInfo userInfo = GetSql().Queryable<UserInfo>().With(SqlSugar.SqlWith.RowLock).Where(it => it.MemberID == packet.MemberID).Single(); 

            try
            {
                // 有找到會員資料
                if (userInfo != null)
                {
                    JArray jaNewFriendList = new JArray();
                    
                    string sKey = $"NewFriendList_" + packet.MemberID;

                    if (GetRedis((int)Connect.RedisDB.emRedisDB_User).KeyExists(sKey))
                    {
                        string sNewFriendList = GetRedis((int)Connect.RedisDB.emRedisDB_User).StringGet(sKey);

                        JArray jsNewFriendList = JArray.Parse(sNewFriendList);
                        JArray jsNewFriendList_New = JArray.Parse(sNewFriendList);

                        foreach (JArray jsInfo in jsNewFriendList)
                        {
                            string sFriendMemberID = jsInfo[0].ToString();
                            DateTime stAddTime = DateTime.Parse(jsInfo[1].ToString());

                            TimeSpan Diff_dates = DateTime.UtcNow.Subtract(stAddTime);

                            // 未超過6小時
                            if (Diff_dates.TotalHours < 6)
                            {
                                jaNewFriendList.Add(sFriendMemberID);

                                SaveLog($"[DebugInfo] add: {sFriendMemberID}");
                            }
                            // 超過6小時, 從Redis的紀錄中刪除
                            else
                            {
                                jsNewFriendList_New.Remove(jsInfo);

                                SaveLog($"[DebugInfo] Remove: {sFriendMemberID}");
                            }
                        }

                        GetRedis((int)Connect.RedisDB.emRedisDB_User).StringSet(sKey, jsNewFriendList_New.ToString());
                    }
                    else
                    {
                        SaveLog($"[Warning] MessageFcunction::onGetNewFriendList Can Not Find Member:{packet.MemberID} Redis Key");
                    }

                    rData.Result = (int)GetNewFriendListResult.ResultDefine.emResult_Success;

                    rData.FriendList = jaNewFriendList.ToString();

                }
                else
                {
                    rData.Result = (int)GetNewFriendListResult.ResultDefine.emResult_Fail;

                    SaveLog($"[Error] MessageFcunction::onGetNewFriendList Can Not Find Member:{packet.MemberID}");
                }

            }
            catch (Exception ex)
            {
                SaveLog($"[Error] MessageFcunction::onGetNewFriendList Catch Error, Msg:{ex.Message}");

                rData.Result = (int)GetNewFriendListResult.ResultDefine.emResult_Fail;
            }

            JObject jsMain = new JObject();
            jsMain.Add("CmdID", (int)S2C_CmdID.emGetNewFriendListResult);
            jsMain.Add("Data", JsonConvert.DeserializeObject<JObject>(JsonConvert.SerializeObject(rData)));

            ret = jsMain.ToString();

            return ret;
        }

    }

}
