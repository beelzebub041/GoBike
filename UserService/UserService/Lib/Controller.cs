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

using Packet.ClientToServer;
using Packet.ServerToClient;


namespace UserService
{
    class Controller
    {
        private Form1 fm1 = null;

        private Logger log = null;                      // Logger

        private DataBaseConnect dbConnect = null;

        private Server wsServer = null;                 // Web Socket Server

        private string ControllerVersion = "Version007";


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
            catch
            {


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
                            LoginDate = dateTime
                        };

                        dbConnect.GetSql().Insertable(account).ExecuteCommand();

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
                            Mobile = "",
                            Country = -1
                        };

                        dbConnect.GetSql().Insertable(info).ExecuteCommand();

                        // 新增騎乘資料
                        RideData data = new RideData
                        {
                            MemberID = account.MemberID,
                            TotalDistance = 0,
                            TotalAltitude = 0,
                            TotalRideTime = 0,
                        };

                        dbConnect.GetSql().Insertable(data).ExecuteCommand();

                        rData.Result = 1;
                    }
                    else
                    {
                        rData.Result = 2;
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

                        List<RideData> rideDataList = dbConnect.GetSql().Queryable<RideData>().Where(it => it.MemberID == accountList[0].MemberID).ToList();

                        if (infoList.Count() == 1 && rideDataList.Count() == 1)
                        {
                            rData.LoginData = new UserLoginData()
                            {
                                Email = accountList[0].Email,
                                FBToken = accountList[0].FBToken,
                                GoogleToken = accountList[0].GoogleToken,
                                LoginDate = DateTime.Parse(accountList[0].LoginDate).ToString("yyyy-MM-dd hh:mm:ss"),
                                RegisterDate = DateTime.Parse(accountList[0].RegisterDate).ToString("yyyy-MM-dd hh:mm:ss"),
                                RegisterSource = accountList[0].RegisterSource,

                                MemberID = infoList[0].MemberID,
                                Nickname = infoList[0].NickName,
                                Avatar = infoList[0].Avatar,
                                Birthday = DateTime.Parse(infoList[0].Birthday).ToString("yyyy-MM-dd hh:mm:ss"),
                                BodyHeight = infoList[0].BodyHeight,
                                BodyWeight = infoList[0].BodyWeight,
                                Country = infoList[0].Country,
                                FrontCover = infoList[0].FrontCover,
                                Gender = infoList[0].Gender,
                                Mobile = infoList[0].Mobile,
                                
                                TotalAltitude = rideDataList[0].TotalAltitude,
                                TotalDistance = rideDataList[0].TotalDistance,
                                TotalRideTime = rideDataList[0].TotalRideTime
                            };

                            rData.Result = 1;

                        }
                        else
                        {
                            log.SaveLog("[Error] Controller::OnUserLogin Can't Find User Info or Ride Data");

                            rData.Result = 0;
                        }


                    }
                    else
                    {
                        rData.Result = 3;
                    }
                }
                else
                {
                    rData.Result = 2;
                }
            }
            catch (Exception ex)
            {
                log.SaveLog("[Error] Controller::OnUserLogin Catch Error, Msg:" + ex.Message);

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
                    infoList[0].Mobile = packet.UpdateData.Mobile == null ? infoList[0].Mobile : packet.UpdateData.Mobile;
                    infoList[0].Country = packet.UpdateData.Country == 0 ? infoList[0].Country : packet.UpdateData.Country;

                    dbConnect.GetSql().Updateable<UserInfo>(infoList[0]).Where(it => it.MemberID == packet.MemberID).ExecuteCommand();

                    //dbConnect.GetSql().Updateable<UserAccount>().SetColumns(it => new UserAccount() { Password = packet.NewPassword }).Where(it => it.MemberID == packet.MemberID).ExecuteCommand();


                    rData.Result = 1;
                }
                else
                {
                    rData.Result = 0;
                }
            }
            catch (Exception ex)
            {
                log.SaveLog("[Error] Controller::OnUpdateUserInfo Catch Error, Msg:" + ex.Message);

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
                    if (accountList[0].Password == packet.Password)
                    {
                        dbConnect.GetSql().Updateable<UserAccount>().SetColumns(it => new UserAccount() { Password = packet.NewPassword }).Where(it => it.MemberID == packet.MemberID).ExecuteCommand();

                        rData.Result = 1;
                    }
                    else
                    {
                        rData.Result = 2;
                    }
                }
                else
                {
                    rData.Result = 0;
                }
            }
            catch (Exception ex)
            {
                log.SaveLog("[Error] Controller::OnUpdateUserInfo Catch Error, Msg:" + ex.Message);

                rData.Result = 0;
            }

            JObject jsMain = new JObject();
            jsMain.Add("CmdID", (int)S2C_CmdID.emUpdatePasswordResult);
            jsMain.Add("Data", JsonConvert.DeserializeObject<JObject>(JsonConvert.SerializeObject(rData)));

            return jsMain.ToString();
        }

    }

}
