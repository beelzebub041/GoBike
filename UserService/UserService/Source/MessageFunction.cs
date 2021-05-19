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
        /// 建立新帳號
        /// </summary>
        /// <param name="data"> 封包資料 </param>
        /// <returns> 結果 </returns>
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
                    UserAccount account = GetSql().Queryable<UserAccount>().With(SqlSugar.SqlWith.RowLock).Where(it => it.Email == packet.Email).Single();

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
                        GetSql().BeginTran();

                        // 寫入資料庫
                        if (GetSql().Insertable(newAccount).With(SqlSugar.SqlWith.TabLockX).ExecuteCommand() > 0)
                        {

                            if (GetSql().Insertable(info).With(SqlSugar.SqlWith.TabLockX).ExecuteCommand() > 0 
                                && GetSql().Insertable(rideData).With(SqlSugar.SqlWith.TabLockX).ExecuteCommand() > 0
                                && GetSql().Insertable(curWeek).With(SqlSugar.SqlWith.TabLockX).ExecuteCommand() > 0)
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
                GetRedis((int)Connect.RedisDB.emRedisDB_User).HashSet($"UserAccount_" + newAccount.Email, hashTransfer.TransToHashEntryArray(newAccount));
                GetRedis((int)Connect.RedisDB.emRedisDB_User).HashSet($"UserInfo_" + info.MemberID, hashTransfer.TransToHashEntryArray(info));
                GetRedis((int)Connect.RedisDB.emRedisDB_Ride).HashSet($"RideData_" + rideData.MemberID, hashTransfer.TransToHashEntryArray(rideData));
                GetRedis((int)Connect.RedisDB.emRedisDB_Ride).HashSet($"RideData_" + rideData.MemberID, hashTransfer.TransToHashEntryArray(rideData));
                GetRedis((int)Connect.RedisDB.emRedisDB_Ride).HashSet($"CurWeekRideData_" + newAccount.MemberID, hashTransfer.TransToHashEntryArray(curWeek));

                // DB 交易提交
                GetSql().CommitTran();
            }
            else
            {
                // DB 交易失敗, 啟動Rollback
                GetSql().RollbackTran();
            }

            JObject jsMain = new JObject();
            jsMain.Add("CmdID", (int)S2C_CmdID.emUserRegisteredResult);
            jsMain.Add("Data", JsonConvert.DeserializeObject<JObject>(JsonConvert.SerializeObject(rData)));

            ret = jsMain.ToString();

            return ret;
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
                if (packet.Email == "token")
                {
                    rData = OnUserLogin_Temp(data);
                }
                else
                {
                    UserAccount account = GetSql().Queryable<UserAccount>().Where(it => it.Email == packet.Email && it.Password == packet.Password).Single();

                    // 有找到帳號
                    if (account != null)
                    {
                        if (account.Password == packet.Password)
                        {
                            List<UserInfo> infoList = GetSql().Queryable<UserInfo>().Where(it => it.MemberID == account.MemberID).ToList();

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


        public string OnUserLogin_New(string data)
        {
            string ret = "";

            UserLogin packet = JsonConvert.DeserializeObject<UserLogin>(data);

            UserLoginResult rData = new UserLoginResult();

            try
            {
                string uUID = FireBaseHandler.Instance.CheckFireBaseUserToken(packet.Password).Result;

                if (uUID != "")
                {
                    UserAccount_New account = GetSql().Queryable<UserAccount_New>().Where(it => it.UUID == uUID).Single();

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
                        //rData.Result = (int)UserLoginResult.ResultDefine.emResult_AccountError;

                        string dateTime = DateTime.UtcNow.ToString("yyyy-MM-dd hh:mm:ss");

                        string guidAll = Guid.NewGuid().ToString();

                        string[] guidList = guidAll.Split('-');

                        UserAccount_New newAccount = new UserAccount_New();

                        // 建立新帳號
                        newAccount.UUID = uUID;
                        newAccount.MemberID = "Dblha-" + guidList[0];        // 取GUID前8碼
                        newAccount.Token = packet.Password;
                        newAccount.NotifyToken = "";
                        newAccount.RegisterDate = dateTime;

                        UserInfo newInfo = new UserInfo();

                        // 新增使用者資訊
                        newInfo.MemberID = newAccount.MemberID;
                        newInfo.NickName = "";
                        newInfo.Birthday = "";
                        newInfo.BodyHeight = 0;
                        newInfo.BodyWeight = 0;
                        newInfo.FrontCover = "";
                        newInfo.Avatar = "";
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

                                GetRedis((int)Connect.RedisDB.emRedisDB_User).HashSet($"UserAccount_" + newAccount.UUID, hashTransfer.TransToHashEntryArray(newAccount));
                                GetRedis((int)Connect.RedisDB.emRedisDB_User).HashSet($"UserInfo_" + newInfo.MemberID, hashTransfer.TransToHashEntryArray(newInfo));
                                GetRedis((int)Connect.RedisDB.emRedisDB_Ride).HashSet($"RideData_" + rideData.MemberID, hashTransfer.TransToHashEntryArray(rideData));
                                GetRedis((int)Connect.RedisDB.emRedisDB_Ride).HashSet($"RideData_" + rideData.MemberID, hashTransfer.TransToHashEntryArray(rideData));
                                GetRedis((int)Connect.RedisDB.emRedisDB_Ride).HashSet($"CurWeekRideData_" + newAccount.MemberID, hashTransfer.TransToHashEntryArray(curWeek));

                                SaveLog($"[Info] MessageFcunction::OnCreateNewAccount Create New Account Success");

                                // DB 交易提交
                                GetSql().CommitTran();
                            }
                            else
                            {
                                rData.Result = (int)UserLoginResult.ResultDefine.emResult_Fail;

                                SaveLog($"[Warning] MessageFcunction::OnCreateNewAccount Can Not Inseart User Info Or Ride Data");
                            }
                        }
                        else
                        {
                            rData.Result = (int)UserLoginResult.ResultDefine.emResult_Fail;

                            SaveLog($"[Warning] MessageFcunction::OnCreateNewAccount Email: {packet.Email} Repeat");
                        }



                    }
                }
                else
                {

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

        public UserLoginResult OnUserLogin_Temp(string data)
        {
            UserLogin packet = JsonConvert.DeserializeObject<UserLogin>(data);

            UserLoginResult rData = new UserLoginResult();

            try
            {
                string uUID = FireBaseHandler.Instance.CheckFireBaseUserToken(packet.Password).Result;

                if (uUID != "")
                {
                    UserAccount account = GetSql().Queryable<UserAccount>().Where(it => it.GoogleToken == uUID).Single();

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
                            SaveLog("[Warning] MessageFcunction::OnUserLogin_Temp Can't Find User Info");

                            rData.Result = (int)UserLoginResult.ResultDefine.emResult_Fail;
                        }

                    }
                    else
                    {
                        //rData.Result = (int)UserLoginResult.ResultDefine.emResult_AccountError;

                        string dateTime = DateTime.UtcNow.ToString("yyyy-MM-dd hh:mm:ss");

                        string guidAll = Guid.NewGuid().ToString();

                        string[] guidList = guidAll.Split('-');

                        UserAccount newAccount = new UserAccount();

                        // 建立新帳號
                        newAccount.MemberID = "Dblha-" + guidList[0];        // 取GUID前8碼
                        newAccount.Email = DateTime.UtcNow.ToString("hhmmss") + "@hotmail.com";
                        newAccount.Password = "";
                        newAccount.FBToken = "";
                        newAccount.GoogleToken = uUID;
                        newAccount.NotifyToken = "";
                        newAccount.RegisterSource = 0;
                        newAccount.RegisterDate = dateTime;

                        rData.MemberID = newAccount.MemberID;

                        UserInfo newInfo = new UserInfo();

                        // 新增使用者資訊
                        newInfo.MemberID = newAccount.MemberID;
                        newInfo.NickName = "";
                        newInfo.Birthday = "";
                        newInfo.BodyHeight = 0;
                        newInfo.BodyWeight = 0;
                        newInfo.FrontCover = "";
                        newInfo.Avatar = "";
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

                                GetRedis((int)Connect.RedisDB.emRedisDB_User).HashSet($"UserAccount_" + newAccount.Email, hashTransfer.TransToHashEntryArray(newAccount));
                                GetRedis((int)Connect.RedisDB.emRedisDB_User).HashSet($"UserInfo_" + newInfo.MemberID, hashTransfer.TransToHashEntryArray(newInfo));
                                GetRedis((int)Connect.RedisDB.emRedisDB_Ride).HashSet($"RideData_" + rideData.MemberID, hashTransfer.TransToHashEntryArray(rideData));
                                GetRedis((int)Connect.RedisDB.emRedisDB_Ride).HashSet($"RideData_" + rideData.MemberID, hashTransfer.TransToHashEntryArray(rideData));
                                GetRedis((int)Connect.RedisDB.emRedisDB_Ride).HashSet($"CurWeekRideData_" + newAccount.MemberID, hashTransfer.TransToHashEntryArray(curWeek));

                                SaveLog($"[Info] MessageFcunction::OnUserLogin_Temp Create New Account Success");

                                // DB 交易提交
                                GetSql().CommitTran();
                            }
                            else
                            {
                                rData.Result = (int)UserLoginResult.ResultDefine.emResult_Fail;

                                SaveLog($"[Warning] MessageFcunction::OnUserLogin_Temp Can Not Inseart User Info Or Ride Data");
                            }
                        }
                        else
                        {
                            rData.Result = (int)UserLoginResult.ResultDefine.emResult_Fail;

                            SaveLog($"[Warning] MessageFcunction::OnUserLogin_Temp Email: {newAccount.Email} Repeat");
                        }



                    }
                }
                else
                {

                }
            }
            catch (Exception ex)
            {
                SaveLog($"[Error] MessageFcunction::OnUserLogin_Temp Catch Error, Msg:{ex.Message}");

                rData.Result = (int)UserLoginResult.ResultDefine.emResult_Fail;
            }

            // DB 交易失敗, 啟動Rollback
            if (rData.Result != (int)UserLoginResult.ResultDefine.emResult_Success)
            {
                GetSql().RollbackTran();
            }

            //JObject jsMain = new JObject();
            //jsMain.Add("CmdID", (int)S2C_CmdID.emUserLoginResult);
            //jsMain.Add("Data", JsonConvert.DeserializeObject<JObject>(JsonConvert.SerializeObject(rData)));

            //ret = jsMain.ToString();

            return rData;
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
        public string OnUpdatePassword(string data)
        {
            string ret = "";

            UpdatePassword packet = JsonConvert.DeserializeObject<UpdatePassword>(data);

            UpdatePasswordResult rData = new UpdatePasswordResult();
            rData.Action = packet.Action;

            try
            {
                UserAccount account = GetSql().Queryable<UserAccount>().With(SqlSugar.SqlWith.RowLock).Where(it => it.MemberID == packet.MemberID).Single();

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
                        if (GetSql().Updateable<UserAccount>().SetColumns(it => new UserAccount() { Password = packet.NewPassword }).With(SqlSugar.SqlWith.RowLock).Where(it => it.MemberID == packet.MemberID).ExecuteCommand() > 0)
                        {
                            rData.Result = (int)UpdatePasswordResult.ResultDefine.emResult_Success;

                            account.Password = packet.NewPassword;
                            GetRedis((int)Connect.RedisDB.emRedisDB_User).HashSet($"UserAccount_" + account.Email, hashTransfer.TransToHashEntryArray(account));

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

                try
                {
                    // 傳送資料到Post Service, 更新塗鴉牆
                    var postClient = GRPCClient.Instance.GetClient();

                    UpdateMemberPostShowList updateInfo = new UpdateMemberPostShowList();
                    updateInfo.MemberID = packet.MemberID;

                    var reply = postClient.UpdatePostShowListFun(updateInfo);
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

    }

}
