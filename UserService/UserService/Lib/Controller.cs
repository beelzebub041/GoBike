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

        private string ControllerVersion = "Version015";


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

                                case (int)C2S_CmdID.emUpdateTeamList:
                                    UpdateTeamList teamMsg = JsonConvert.DeserializeObject<UpdateTeamList>(packetData);

                                    sReturn = OnUpdateTeamList(teamMsg);

                                    break;

                                case (int)C2S_CmdID.emUpdateFriendList:
                                    UpdateFriendList friendMsg = JsonConvert.DeserializeObject<UpdateFriendList>(packetData);

                                    sReturn = OnUpdateFriendList(friendMsg);

                                    break;

                                case (int)C2S_CmdID.emUpdateBlackList:
                                    UpdateBlackList blackMsg = JsonConvert.DeserializeObject<UpdateBlackList>(packetData);

                                    sReturn = OnUpdateBlackList(blackMsg);

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
         * 建立新帳號
         */
        private string OnCreateNewAccount(UserRegistered packet)
        {
            UserRegisteredResult rData = new UserRegisteredResult();

            if (packet.Password == packet.CheckPassword)
            {
                try
                {
                    List<UserAccount> accountList = dbConnect.GetSql().Queryable<UserAccount>().Where(it => it.Email == packet.Email).ToList();
                
                    // 未包含該Email
                    if (accountList.Count() == 0)
                    {
                        string dateTime = DateTime.UtcNow.ToString("yyyy-MM-dd hh:mm:ss");

                        string guidAll = Guid.NewGuid().ToString();

                        string[] guidList = guidAll.Split('-');

                        // 建立新帳號
                        UserAccount account = new UserAccount
                        {
                            MemberID = "Dblha-" + guidList[0],        // 取GUID前8碼
                            Email = packet.Email,
                            Password = packet.Password,
                            FBToken = packet.FBToken,
                            GoogleToken = packet.GoogleToken,
                            RegisterSource = packet.RegisterSource,
                            RegisterDate = dateTime,
                        };

                        // 新增使用者資訊
                        UserInfo info = new UserInfo
                        {
                            MemberID = account.MemberID,
                            NickName = "",
                            Birthday = dateTime,
                            BodyHeight = 0,
                            BodyWeight = 0,
                            FrontCover = "",
                            Avatar = "",
                            Photo = "",
                            Mobile = "",
                            County = -1,
                            TeamList = "{\"TeamList\":[]}",
                            FriendList = "{\"FriendList\":[]}",
                            BlackList = "{\"BlackList\":[]}"
                        };

                        // 新增騎乘資料
                        RideData data = new RideData
                        {
                            MemberID = account.MemberID,
                            TotalDistance = 0,
                            TotalAltitude = 0,
                            TotalRideTime = 0,
                        };

                        // 資料有寫入資料庫
                        if (dbConnect.GetSql().Insertable(account).ExecuteCommand() > 0 &&
                            dbConnect.GetSql().Insertable(info).ExecuteCommand() > 0 &&
                            dbConnect.GetSql().Insertable(data).ExecuteCommand() > 0)
                        {
                            rData.Result = 1;

                            log.SaveLog("[Info] Controller::OnCreateNewAccount Create New Account Success");
                        }
                        else
                        {
                            rData.Result = 0;

                            log.SaveLog("[Warning] Controller::OnCreateNewAccount Email Repeat:");
                        }

                    }
                    else
                    {
                        rData.Result = 2;

                        log.SaveLog("[Info] Controller::OnCreateNewAccount Email Repeat:");

                    }

                }
                catch (Exception ex)
                {
                    rData.Result = 0;

                    log.SaveLog("[Error] Controller::OnCreateNewAccount Create Error Msg:" + ex.Message);
                }

            }
            else
            {
                rData.Result = 3;

                log.SaveLog("[Info] Controller::OnCreateNewAccount Check Password Error:");

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
                List<UserAccount> accountList = dbConnect.GetSql().Queryable<UserAccount>().Where(it => it.Email == packet.Email && it.Password == packet.Password).ToList();

                // 有找到帳號
                if (accountList.Count() == 1)
                {
                    if (accountList[0].Password == packet.Password)
                    {
                        List<UserInfo> infoList = dbConnect.GetSql().Queryable<UserInfo>().Where(it => it.MemberID == accountList[0].MemberID).ToList();

                        // 有找到會員
                        if (infoList.Count() == 1)
                        {
                            rData.Result = 1;
                            rData.MemberID = accountList[0].MemberID;
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
                List<UserInfo> infoList = dbConnect.GetSql().Queryable<UserInfo>().Where(it => it.MemberID == packet.MemberID).ToList();

                // 有找到資料
                if (infoList.Count() == 1)
                {
                    infoList[0].NickName = packet.UpdateData.NickName == null ? infoList[0].NickName : packet.UpdateData.NickName;
                    infoList[0].Birthday = packet.UpdateData.Birthday == null ? DateTime.Parse(infoList[0].Birthday).ToString("yyyy-MM-dd hh:mm:ss") : packet.UpdateData.Birthday;
                    infoList[0].BodyHeight = packet.UpdateData.BodyHeight == 0 ? infoList[0].BodyHeight : packet.UpdateData.BodyHeight;
                    infoList[0].BodyWeight = packet.UpdateData.BodyWeight == 0 ? infoList[0].BodyWeight : packet.UpdateData.BodyWeight;
                    infoList[0].FrontCover = packet.UpdateData.FrontCover == null ? infoList[0].FrontCover : packet.UpdateData.FrontCover;
                    infoList[0].Avatar = packet.UpdateData.Avatar == null ? infoList[0].Avatar : packet.UpdateData.Avatar;
                    infoList[0].Photo = packet.UpdateData.Photo == null ? infoList[0].Photo : packet.UpdateData.Photo;
                    infoList[0].Mobile = packet.UpdateData.Mobile == null ? infoList[0].Mobile : packet.UpdateData.Mobile;
                    infoList[0].Gender = packet.UpdateData.Gender == 0 ? infoList[0].Gender : packet.UpdateData.Gender;
                    infoList[0].County = packet.UpdateData.Country == 0 ? infoList[0].County : packet.UpdateData.Country;

                    if (dbConnect.GetSql().Updateable<UserInfo>(infoList[0]).Where(it => it.MemberID == packet.MemberID).ExecuteCommand() > 0)
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

            try
            {
                List<UserAccount> accountList = dbConnect.GetSql().Queryable<UserAccount>().Where(it => it.MemberID == packet.MemberID).ToList();

                // 有找到帳號
                if (accountList.Count() == 1)
                {
                    if (dbConnect.GetSql().Updateable<UserAccount>().SetColumns(it => new UserAccount() { Password = packet.NewPassword }).Where(it => it.MemberID == packet.MemberID).ExecuteCommand() > 0)
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
         * 更新車隊列表
         */
        private string OnUpdateTeamList(UpdateTeamList packet)
        {
            UpdateTeamListResult rData = new UpdateTeamListResult();
            rData.Action = packet.Action;

            try
            {
                List<UserInfo> userList = dbConnect.GetSql().Queryable<UserInfo>().Where(it => it.MemberID == packet.MemberID).ToList();

                // 有找到會員
                if (userList.Count() == 1)
                {
                    JObject jsData = JObject.Parse(userList[0].TeamList);

                    if (jsData.ContainsKey("TeamList"))
                    {
                        JArray jsArray = jsData["TeamList"] as JArray;

                        List<string> idList = jsArray.ToObject<List<string>>();

                        // 新增
                        if (packet.Action == 1)
                        {
                            if (!idList.Contains(packet.TeamID))
                            {
                                idList.Add(packet.TeamID);

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
                            if (idList.Contains(packet.TeamID))
                            {
                                idList.Remove(packet.TeamID);

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
                            jsNew.Add("TeamList", JArray.FromObject(idList));

                            if (dbConnect.GetSql().Updateable<UserInfo>().SetColumns(it => new UserInfo() { TeamList = jsNew.ToString() }).Where(it => it.MemberID == packet.MemberID).ExecuteCommand() > 0)
                            {
                                rData.Result = 1;

                                log.SaveLog($"[Error] Controller::OnUpdateTeamList Member: {packet.MemberID} Update TeamList Success");

                            }
                            else
                            {
                                rData.Result = 0;

                                log.SaveLog($"[Error] Controller::OnUpdateTeamList Member: {packet.MemberID} Update TeamList Fail");

                            }
                        }

                    }
                    else
                    {
                        rData.Result = 0;

                        log.SaveLog($"[Error] Controller::OnUpdateTeamList Can Not Find Json \"TeamList\" Member");
                    }

                }
                else
                {
                    rData.Result = 0;

                    log.SaveLog($"[Error] Controller::OnUpdateTeamList Can Not Find Member:{packet.MemberID}");

                }
            }
            catch (Exception ex)
            {
                log.SaveLog($"[Error] Controller::OnUpdateTeamList Catch Error, Msg:{ex.Message}");

                rData.Result = 0;
            }

            JObject jsMain = new JObject();
            jsMain.Add("CmdID", (int)S2C_CmdID.emUpdateTeamListResult);
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
                List<UserInfo> userList = dbConnect.GetSql().Queryable<UserInfo>().Where(it => it.MemberID == packet.MemberID).ToList();

                // 有找到會員
                if (userList.Count() == 1)
                {
                    JObject jsData = JObject.Parse(userList[0].FriendList);

                    if (jsData.ContainsKey("FriendList"))
                    {
                        JArray jsArray = jsData["FriendList"] as JArray;

                        List<string> idList = jsArray.ToObject<List<string>>();

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
                            JObject jsNew = new JObject();
                            jsNew.Add("FriendList", JArray.FromObject(idList));

                            if (dbConnect.GetSql().Updateable<UserInfo>().SetColumns(it => new UserInfo() { FriendList = jsNew.ToString() }).Where(it => it.MemberID == packet.MemberID).ExecuteCommand() > 0)
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

                        log.SaveLog($"[Error] Controller::OnUpdateFriendList Can Not Find Json \"FriendList\" Member");

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
                List<UserInfo> userList = dbConnect.GetSql().Queryable<UserInfo>().Where(it => it.MemberID == packet.MemberID).ToList();

                // 有找到會員
                if (userList.Count() == 1)
                {
                    JObject jsData = JObject.Parse(userList[0].BlackList);

                    if (jsData.ContainsKey("BlackList"))
                    {
                        JArray jsArray = jsData["BlackList"] as JArray;

                        List<string> idList = jsArray.ToObject<List<string>>();

                        // 新增
                        if (packet.Action == 1)
                        {
                            if (!idList.Contains(packet.BlackID))
                            {
                                idList.Add(packet.BlackID);

                                rData.Result = 1;

                                // 檢查是否有在好友名單中
                                JObject jsFriendData = JObject.Parse(userList[0].FriendList);

                                if (jsFriendData.ContainsKey("FriendList"))
                                {
                                    JArray jsFriendArray = jsFriendData["FriendList"] as JArray;

                                    List<string> friendList = jsFriendArray.ToObject<List<string>>();

                                    if (friendList.Contains(packet.BlackID))
                                    {
                                        friendList.Remove(packet.BlackID);

                                        JObject jsFriendNew = new JObject();
                                        jsFriendNew.Add("FriendList", JArray.FromObject(friendList));


                                        if (dbConnect.GetSql().Updateable<UserInfo>().SetColumns(it => new UserInfo() { FriendList = jsFriendNew.ToString() }).Where(it => it.MemberID == packet.MemberID).ExecuteCommand() > 0)
                                        {
                                            log.SaveLog($"[Info] Controller::OnUpdateBlackList Remove MemberID:{packet.MemberID} From Friend List");
                                        }
                                        else
                                        {
                                            log.SaveLog($"[Warning] Controller::OnUpdateBlackList Can Not Remove MemberID:{packet.MemberID} From Friend List");
                                        }

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
                            JObject jsNew = new JObject();
                            jsNew.Add("BlackList", JArray.FromObject(idList));

                            if (dbConnect.GetSql().Updateable<UserInfo>().SetColumns(it => new UserInfo() { BlackList = jsNew.ToString() }).Where(it => it.MemberID == packet.MemberID).ExecuteCommand() > 0)
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
                else
                {
                    rData.Result = 0;

                    log.SaveLog($"[Error] Controller::OnUpdateBlackList Can Not Find Member:{packet.MemberID}");

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

    }

}
