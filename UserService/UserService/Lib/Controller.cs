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

using UserPacket.ClientToServer;
using UserPacket.ServerToClient;


namespace UserService
{
    class Controller
    {
        private Form1 fm1 = null;

        private Logger log = null;                      // Logger

        private DataBaseConnect dbConnect = null;

        private Server wsServer = null;                 // Web Socket Server

        private object msgLock = new object();

        private string version = "User025";


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

                log.SaveLog($"User Service Version: {version}");

                wsServer = new Server(log.SaveLog, MessageProcess);

                dbConnect = new DataBaseConnect(log);

                if (dbConnect.Initialize() && dbConnect.Connect() && wsServer.Initialize())
                {
                    bReturn = true;
                }
                else
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
                                    case (int)C2S_CmdID.emUserRegistered:

                                        UserRegistered registeredMsg = JsonConvert.DeserializeObject<UserRegistered>(packetData);

                                        sReturn = OnCreateNewAccount(registeredMsg);

                                        break;

                                    case (int)C2S_CmdID.emUserLogin:
                                        UserLogin loginMsg = JsonConvert.DeserializeObject<UserLogin>(packetData);

                                        sReturn = OnUserLogin(loginMsg);

                                        break;

                                    case (int)C2S_CmdID.emUpdateUserInfo:
                                        UpdateUserInfo updateMsg = JsonConvert.DeserializeObject<UpdateUserInfo>(packetData);

                                        sReturn = OnUpdateUserInfo(updateMsg);

                                        break;

                                    case (int)C2S_CmdID.emUpdatePassword:
                                        UpdatePassword passwordMsg = JsonConvert.DeserializeObject<UpdatePassword>(packetData);

                                        sReturn = OnUpdatePassword(passwordMsg);

                                        break;

                                    case (int)C2S_CmdID.emUpdateFriendList:
                                        UpdateFriendList friendMsg = JsonConvert.DeserializeObject<UpdateFriendList>(packetData);

                                        sReturn = OnUpdateFriendList(friendMsg);

                                        break;

                                    case (int)C2S_CmdID.emUpdateBlackList:
                                        UpdateBlackList blackMsg = JsonConvert.DeserializeObject<UpdateBlackList>(packetData);

                                        sReturn = OnUpdateBlackList(blackMsg);

                                        break;

                                    case (int)C2S_CmdID.emUpdateNotifyToken:
                                        UpdateNotifyToken tokenMsg = JsonConvert.DeserializeObject<UpdateNotifyToken>(packetData);

                                        sReturn = OnUpdateNotifyToken(tokenMsg);

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
         * 建立新帳號
         */
        private string OnCreateNewAccount(UserRegistered packet)
        {
            UserRegisteredResult rData = new UserRegisteredResult();

            if (packet.Password == packet.CheckPassword)
            {
                try
                {
                    UserAccount account = dbConnect.GetSql().Queryable<UserAccount>().With(SqlSugar.SqlWith.RowLock).Where(it => it.Email == packet.Email).Single();

                    // 未包含該Email
                    if (account == null)
                    {
                        string dateTime = DateTime.UtcNow.ToString("yyyy-MM-dd hh:mm:ss");

                        string guidAll = Guid.NewGuid().ToString();

                        string[] guidList = guidAll.Split('-');

                        // 建立新帳號
                        UserAccount newAccount = new UserAccount
                        {
                            MemberID = "Dblha-" + guidList[0],        // 取GUID前8碼
                            Email = packet.Email,
                            Password = packet.Password,
                            FBToken = packet.FBToken,
                            GoogleToken = packet.GoogleToken,
                            NotifyToken = "",
                            RegisterSource = packet.RegisterSource,
                            RegisterDate = dateTime,
                        };

                        // 新增使用者資訊
                        UserInfo info = new UserInfo
                        {
                            MemberID = newAccount.MemberID,
                            NickName = "",
                            Birthday = "",
                            BodyHeight = 0,
                            BodyWeight = 0,
                            FrontCover = "",
                            Avatar = "",
                            Photo = "",
                            Mobile = "",
                            County = -1,
                            TeamList = "[]",
                            FriendList = "[]",
                            BlackList = "[]",
                            SpecificationModel = ""
                        };

                        // 新增騎乘資料
                        RideData data = new RideData
                        {
                            MemberID = newAccount.MemberID,
                            TotalDistance = 0,
                            TotalAltitude = 0,
                            TotalRideTime = 0,
                        };

                        // 寫入資料庫
                        if (dbConnect.GetSql().Insertable(newAccount).With(SqlSugar.SqlWith.TabLockX).ExecuteCommand() > 0)
                        {
                            
                            if (dbConnect.GetSql().Insertable(info).With(SqlSugar.SqlWith.TabLockX).ExecuteCommand() > 0 &&
                                dbConnect.GetSql().Insertable(data).With(SqlSugar.SqlWith.TabLockX).ExecuteCommand() > 0)
                            {
                                rData.Result = 1;

                                log.SaveLog($"[Info] Controller::OnCreateNewAccount Create New Account Success");
                            }
                            else
                            {
                                rData.Result = 0;

                                log.SaveLog($"[Warning] Controller::OnCreateNewAccount Can Not Inseart User Info Or Ride Data");
                            }
                        }
                        else
                        {
                            rData.Result = 0;

                            log.SaveLog($"[Warning] Controller::OnCreateNewAccount Email: {packet.Email} Repeat");
                        }

                    }
                    else
                    {
                        rData.Result = 2;

                        log.SaveLog($"[Info] Controller::OnCreateNewAccount Email Repeat:");

                    }

                }
                catch (Exception ex)
                {
                    rData.Result = 0;

                    log.SaveLog($"[Error] Controller::OnCreateNewAccount Create Error Msg:{ex.Message}");
                }

            }
            else
            {
                rData.Result = 3;

                log.SaveLog($"[Info] Controller::OnCreateNewAccount Check Password Error");

            }

            JObject jsMain = new JObject();
            jsMain.Add("CmdID", (int)S2C_CmdID.emUserRegisteredResult);
            jsMain.Add("Data", JsonConvert.DeserializeObject<JObject>(JsonConvert.SerializeObject(rData)));

            return jsMain.ToString();

        }

        /**
         * 使用者登入
         */
        private string OnUserLogin(UserLogin packet)
        {
            UserLoginResult rData = new UserLoginResult();

            try
            {
                UserAccount account = dbConnect.GetSql().Queryable<UserAccount>().Where(it => it.Email == packet.Email && it.Password == packet.Password).Single();

                // 有找到帳號
                if (account != null)
                {
                    if (account.Password == packet.Password)
                    {
                        List<UserInfo> infoList = dbConnect.GetSql().Queryable<UserInfo>().Where(it => it.MemberID == account.MemberID).ToList();

                        // 有找到會員
                        if (infoList.Count() == 1)
                        {
                            rData.Result = 1;
                            rData.MemberID = account.MemberID;
                        }
                        else
                        {
                            log.SaveLog("[Warning] Controller::OnUserLogin Can't Find User Info");

                            rData.Result = 0;
                        }
                    }
                    else
                    {
                        rData.Result = 3;

                        log.SaveLog("[Info] Controller::OnUserLogin Can't Password Error");
                    }
                }
                else
                {
                    rData.Result = 2;
                }
            }
            catch (Exception ex)
            {
                log.SaveLog($"[Error] Controller::OnUserLogin Catch Error, Msg:{ex.Message}");

                rData.Result = 0;
            }

            JObject jsMain = new JObject();
            jsMain.Add("CmdID", (int)S2C_CmdID.emUserLoginResult);
            jsMain.Add("Data", JsonConvert.DeserializeObject<JObject>(JsonConvert.SerializeObject(rData)));

            return jsMain.ToString();
        }

        /**
         * 更新使用者資訊
         */
        private string OnUpdateUserInfo(UpdateUserInfo packet)
        {
            UpdateUserInfoResult rData = new UpdateUserInfoResult();

            try
            {
                UserInfo userInfo = dbConnect.GetSql().Queryable<UserInfo>().With(SqlSugar.SqlWith.RowLock).Where(it => it.MemberID == packet.MemberID).Single();

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
                    userInfo.SpecificationModel = packet.UpdateData.SpecificationModel == "" ? userInfo.SpecificationModel : packet.UpdateData.SpecificationModel;

                    if (dbConnect.GetSql().Updateable<UserInfo>(userInfo).With(SqlSugar.SqlWith.RowLock).Where(it => it.MemberID == packet.MemberID).ExecuteCommand() > 0)
                    {
                        rData.Result = 1;
                        
                        log.SaveLog($"[Info] Controller::OnUpdateUserInfo Update User: {packet.MemberID} Info Success");

                    }
                    else
                    {
                        rData.Result = 0;

                        log.SaveLog($"[Info] Controller::OnUpdateUserInfo Update User: {packet.MemberID} Info Fail");

                    }
                }
                else
                {
                    rData.Result = 0;

                    log.SaveLog($"[Info] Controller::OnUpdateUserInfo Can Not Find User: {packet.MemberID}");

                }
            }
            catch (Exception ex)
            {
                log.SaveLog($"[Error] Controller::OnUpdateUserInfo Catch Error, Msg: {ex.Message}");

                rData.Result = 0;
            }

            JObject jsMain = new JObject();
            jsMain.Add("CmdID", (int)S2C_CmdID.emUpdateUserInfoResult);
            jsMain.Add("Data", JsonConvert.DeserializeObject<JObject>(JsonConvert.SerializeObject(rData)));

            return jsMain.ToString();
        }

        /**
         * 更新密碼
         */
        private string OnUpdatePassword(UpdatePassword packet)
        {
            UpdatePasswordResult rData = new UpdatePasswordResult();
            rData.Action = packet.Action;

            try
            {
                UserAccount account = dbConnect.GetSql().Queryable<UserAccount>().With(SqlSugar.SqlWith.RowLock).Where(it => it.MemberID == packet.MemberID).Single();

                // 有找到帳號
                if (account != null)
                {
                    bool canUpdate = false;

                    log.SaveLog($"[Warning] Controller::OnUpdateUserInfo, Member:{packet.MemberID} Action:{packet.Action}");

                    if (packet.Action == 1)
                    {   
                        // 舊密碼相同
                        if (account.Password == packet.Password)
                        {
                            canUpdate = true;
                        }
                        else
                        {
                            canUpdate = false;

                            log.SaveLog($"[Warning] Controller::OnUpdateUserInfo, Member:{packet.MemberID} Old Password Error");
                        }
                    }
                    // 忘記密碼可直接修改
                    else if (packet.Action == 2)
                    {
                        canUpdate = true;
                    }

                    if (canUpdate)
                    {
                        if (dbConnect.GetSql().Updateable<UserAccount>().SetColumns(it => new UserAccount() { Password = packet.NewPassword }).With(SqlSugar.SqlWith.RowLock).Where(it => it.MemberID == packet.MemberID).ExecuteCommand() > 0)
                        {
                            rData.Result = 1;

                            log.SaveLog($"[Warning] Controller::OnUpdateUserInfo Member:{packet.MemberID} Update Password Success ");
                        }
                        else
                        {
                            rData.Result = 0;

                            log.SaveLog($"[Warning] Controller::OnUpdateUserInfo Member:{packet.MemberID} Can Not Change Password");

                        }
                    }
                    else
                    {
                        rData.Result = 0;

                        log.SaveLog($"[Warning] Controller::OnUpdateUserInfo Member:{packet.MemberID} Update Fail");
                    }
                }
                else
                {
                    rData.Result = 0;

                    log.SaveLog($"[Warning] Controller::OnUpdateUserInfo Can Not Find Account:{packet.MemberID}");
                }
            }
            catch (Exception ex)
            {
                log.SaveLog($"[Error] Controller::OnUpdateUserInfo Catch Error, Msg:{ex.Message}");

                rData.Result = 0;
            }

            JObject jsMain = new JObject();
            jsMain.Add("CmdID", (int)S2C_CmdID.emUpdatePasswordResult);
            jsMain.Add("Data", JsonConvert.DeserializeObject<JObject>(JsonConvert.SerializeObject(rData)));

            return jsMain.ToString();
        }

        /**
         * 更新好友列表
         */
        private string OnUpdateFriendList(UpdateFriendList packet)
        {
            UpdateFriendListResult rData = new UpdateFriendListResult();
            rData.Action = packet.Action;

            try
            {
                UserInfo userInfo = dbConnect.GetSql().Queryable<UserInfo>().With(SqlSugar.SqlWith.RowLock).Where(it => it.MemberID == packet.MemberID).Single();

                // 有找到會員
                if (userInfo != null)
                {
                    JArray jaData = JArray.Parse(userInfo.FriendList);

                    List<string> idList = jaData.ToObject<List<string>>();

                    // 新增
                    if (packet.Action == 1)
                    {
                        if (!idList.Contains(packet.FriendID))
                        {
                            idList.Add(packet.FriendID);

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
                        if (idList.Contains(packet.FriendID))
                        {
                            idList.Remove(packet.FriendID);

                            rData.Result = 1;
                        }
                        else
                        {
                            rData.Result = 0;
                        }
                    }

                    if (rData.Result == 1)
                    {
                        JArray jsNew = JArray.FromObject(idList);

                        if (dbConnect.GetSql().Updateable<UserInfo>().SetColumns(it => new UserInfo() { FriendList = jsNew.ToString() }).With(SqlSugar.SqlWith.RowLock).Where(it => it.MemberID == packet.MemberID).ExecuteCommand() > 0)
                        {
                            rData.Result = 1;

                            log.SaveLog($"[Error] Controller::OnUpdateFriendList Member: {packet.MemberID} Update FriendList Success");

                        }
                        else
                        {
                            rData.Result = 0;

                            log.SaveLog($"[Error] Controller::OnUpdateFriendList Member: {packet.MemberID} Update FriendList Fail");
                        }
                    }

                }
                else
                {
                    rData.Result = 0;

                    log.SaveLog($"[Error] Controller::OnUpdateFriendList Can Not Find Member:{packet.MemberID}");
                }
            }
            catch (Exception ex)
            {
                log.SaveLog($"[Error] Controller::OnUpdateFriendList Catch Error, Msg:{ex.Message}");

                rData.Result = 0;
            }

            JObject jsMain = new JObject();
            jsMain.Add("CmdID", (int)S2C_CmdID.emUpdateFriendListResult);
            jsMain.Add("Data", JsonConvert.DeserializeObject<JObject>(JsonConvert.SerializeObject(rData)));

            return jsMain.ToString();
        }

        /**
         * 更新黑名單列表
         */
        private string OnUpdateBlackList(UpdateBlackList packet)
        {
            UpdateBlackListResult rData = new UpdateBlackListResult();
            rData.Action = packet.Action;

            try
            {
                UserInfo userInfo = dbConnect.GetSql().Queryable<UserInfo>().With(SqlSugar.SqlWith.RowLock).Where(it => it.MemberID == packet.MemberID).Single();

                // 有找到會員
                if (userInfo != null)
                {
                    JArray jsData = JArray.Parse(userInfo.BlackList);

                    List<string> idList = jsData.ToObject<List<string>>();

                    // 新增
                    if (packet.Action == 1)
                    {
                        if (!idList.Contains(packet.BlackID))
                        {
                            idList.Add(packet.BlackID);

                            rData.Result = 1;

                            // 檢查是否有在好友名單中
                            JArray jsFriendData = JArray.Parse(userInfo.FriendList);

                            List<string> friendList = jsFriendData.ToObject<List<string>>();

                            if (friendList.Contains(packet.BlackID))
                            {
                                friendList.Remove(packet.BlackID);

                                JArray jsFriendNew = JArray.FromObject(friendList);

                                if (dbConnect.GetSql().Updateable<UserInfo>().SetColumns(it => new UserInfo() { FriendList = jsFriendNew.ToString() }).With(SqlSugar.SqlWith.RowLock).Where(it => it.MemberID == packet.MemberID).ExecuteCommand() > 0)
                                {
                                    log.SaveLog($"[Info] Controller::OnUpdateBlackList Remove MemberID:{packet.MemberID} From Friend List");
                                }
                                else
                                {
                                    log.SaveLog($"[Warning] Controller::OnUpdateBlackList Can Not Remove MemberID:{packet.MemberID} From Friend List");
                                }

                            }

                        }
                        else
                        {
                            rData.Result = 0;
                        }

                    }
                    // 刪除
                    else if (packet.Action == -1)
                    {
                        if (idList.Contains(packet.BlackID))
                        {
                            idList.Remove(packet.BlackID);

                            rData.Result = 1;
                        }
                        else
                        {
                            rData.Result = 0;
                        }
                    }

                    if (rData.Result == 1)
                    {
                        JArray jsNew = JArray.FromObject(idList);

                        if (dbConnect.GetSql().Updateable<UserInfo>().SetColumns(it => new UserInfo() { BlackList = jsNew.ToString() }).With(SqlSugar.SqlWith.RowLock).Where(it => it.MemberID == packet.MemberID).ExecuteCommand() > 0)
                        {
                            rData.Result = 1;

                            log.SaveLog($"[Error] Controller::OnUpdateBlackList Member: {packet.MemberID} Update BlackList Success");
                        }
                        else
                        {
                            rData.Result = 0;

                            log.SaveLog($"[Error] Controller::OnUpdateBlackList Member: {packet.MemberID} Update BlackList Success");

                        }
                    }

                }
                else
                {
                    rData.Result = 0;
                }

            }
            catch (Exception ex)
            {
                log.SaveLog($"[Error] Controller::OnUpdateBlackList Catch Error, Msg:{ex.Message}");

                rData.Result = 0;
            }

            JObject jsMain = new JObject();
            jsMain.Add("CmdID", (int)S2C_CmdID.emUpdateBlackListResult);
            jsMain.Add("Data", JsonConvert.DeserializeObject<JObject>(JsonConvert.SerializeObject(rData)));

            return jsMain.ToString();
        }

        /**
         * 更新推播Token
         */
        private string OnUpdateNotifyToken(UpdateNotifyToken packet)
        {
            UpdateNotifyTokenResult rData = new UpdateNotifyTokenResult();

            try
            {
                UserAccount account = dbConnect.GetSql().Queryable<UserAccount>().With(SqlSugar.SqlWith.RowLock).Where(it => it.MemberID == packet.MemberID).Single();

                // 有找到資料
                if (account != null)
                {
                    if (dbConnect.GetSql().Updateable<UserAccount>().SetColumns(it => new UserAccount() { NotifyToken = packet.NotifyToken }).With(SqlSugar.SqlWith.RowLock).Where(it => it.MemberID == packet.MemberID).ExecuteCommand() > 0)
                    {
                        rData.Result = 1;

                        log.SaveLog($"[Info] Controller::OnUpdateNotifyToken Update User: {packet.MemberID} Info Success");

                    }
                    else
                    {
                        rData.Result = 0;

                        log.SaveLog($"[Info] Controller::OnUpdateNotifyToken Update User: {packet.MemberID} Info Fail");

                    }
                }
                else
                {
                    rData.Result = 0;

                    log.SaveLog($"[Info] Controller::OnUpdateNotifyToken Can Not Find User: {packet.MemberID}");

                }
            }
            catch (Exception ex)
            {
                log.SaveLog($"[Error] Controller::OnUpdateNotifyToken Catch Error, Msg: {ex.Message}");

                rData.Result = 0;
            }

            JObject jsMain = new JObject();
            jsMain.Add("CmdID", (int)S2C_CmdID.emUpdateNotifyTokenResult);
            jsMain.Add("Data", JsonConvert.DeserializeObject<JObject>(JsonConvert.SerializeObject(rData)));

            return jsMain.ToString();
        }
    }

}
