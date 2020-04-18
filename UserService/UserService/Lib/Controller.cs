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
using UserService.Connect;

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

        private string ControllerVersion = "Version003";


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
                        string dateTime = DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss");

                        string guidAll = Guid.NewGuid().ToString();

                        string[] guidList = guidAll.Split('-');

                        UserAccount account = new UserAccount
                        {
                            UserID = "Dblha-" + guidList[0],        // 取GUID前8碼
                            Email = packet.Email,
                            Password = packet.Password,
                            FBToken = packet.FBToken,
                            GoogleToken = packet.GoogleToken,
                            RegisterSource = packet.RegisterSource,
                            RegisterDate = dateTime,
                            LoginDate = dateTime
                        };

                        dbConnect.GetSql().Insertable(account).ExecuteCommand();

                        UserInfo info = new UserInfo
                        {
                            UserID = account.UserID,
                            NickName = "",
                            Birthday = dateTime,
                            BodyHeight = 0,
                            BodyWeight = 0,
                            FrontCoverUrl = "",
                            PhotoUrl = "",
                            Mobile = "",
                            Country = -1
                        };

                        dbConnect.GetSql().Insertable(info).ExecuteCommand();

                        RideData data = new RideData
                        {
                            UserID = account.UserID,
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
                        rData.Result = 1;
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

            List<UserAccount> accountList = dbConnect.GetSql().Queryable<UserAccount>().Where(it => it.Email == packet.Email).ToList();

            // 有找到帳號
            if (accountList.Count() == 1)
            {
                UpdateUserInfo info = new UpdateUserInfo
                {
                    NickName = packet.NickName,
                    Birthday = packet.Birthday,
                    BodyHeight = packet.BodyHeight,
                    BodyWeight = packet.BodyWeight,
                    FrontCoverUrl = packet.FrontCoverUrl,
                    PhotoUrl = packet.PhotoUrl,
                    Mobile = packet.Mobile,
                    Country = packet.Country
                };

                dbConnect.GetSql().Updateable(info).ExecuteCommand();

                rData.Result = 0;
            }
            else
            {
                rData.Result = 1;
            }

            JObject jsMain = new JObject();
            jsMain.Add("CmdID", (int)S2C_CmdID.emUpdateUserInfoResult);
            jsMain.Add("Data", JsonConvert.DeserializeObject<JObject>(JsonConvert.SerializeObject(rData)));

            return jsMain.ToString();
        }

    }

}
