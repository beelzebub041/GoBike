using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Windows.Forms;
using System.Text.Json;

using Tools.Logger;

using UserService.Def;
using UserService.Connect;

using Packet.Base;
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
            string sReturn = string.Empty;

            // TODO Lock ??

            if (msg != string.Empty)
            {
                JsonDocument jsonDoc = JsonDocument.Parse(msg);

                JsonElement jsMain = jsonDoc.RootElement;

                if (jsMain.TryGetProperty("CmdID", out JsonElement jsName))
                {
                    int cmdID = jsName.GetInt32();

                    string packetData = jsMain.GetProperty("Data").GetString();

                    switch (cmdID)
                    {
                        case (int)C2S_CmdID.emUserRegistered:
                            UserRegistered registeredMsg = JsonSerializer.Deserialize<UserRegistered>(packetData);

                            sReturn = OnCreateNewAccount(registeredMsg);

                            break;

                        case (int)C2S_CmdID.emUserLogin:
                            UserLogin loginMsg = JsonSerializer.Deserialize<UserLogin>(packetData);

                            sReturn = OnUserLogin(loginMsg);

                            break;

                        case (int)C2S_CmdID.emUpdateUserInfo:
                            UpdateUserInfo updateMsg = JsonSerializer.Deserialize<UpdateUserInfo>(packetData);

                            sReturn = OnUpdateUserInfo(updateMsg);

                            break;

                    }

                }
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
                List<UserAccount> accountList = dbConnect.GetSql().Queryable<UserAccount>().Where(it => it.Email == packet.Email).ToList();

                // 未包含該Email
                if (accountList.Count() == 0)
                {
                    try
                    {
                        string dateTime = DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss");


                        string guidAll = Guid.NewGuid().ToString();

                        string[] guidList = guidAll.Split('-');

                        UserAccount account = new UserAccount
                        {
                            UserID = "Dblha-" + guidList[0],        // 取GUID前8碼
                            Email = packet.Email,
                            Password = packet.Password,
                            FBToken = "",
                            GoogleToken = "",
                            RegisterSource = -1,
                            RegisterDate = dateTime,
                            LoginDate = dateTime
                        };

                        dbConnect.GetSql().Insertable(account).ExecuteCommand();

                        UserInfo info = new UserInfo
                        {
                            Index = account.UserID,
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

                    }
                    catch (Exception ex)
                    {
                        log.SaveLog("[Error] Controller::OnCreateNewAccount Create Fail, Error Msg:" + ex.Message);
                    }

                    rData.Result = 0;
                }
                else
                {
                    rData.Result = 1;
                }

            }
            else
            {
                rData.Result = 2;
            }

            PacketBase rPacket = new PacketBase
            {
                CmdID = (int)S2C_CmdID.emUserRegisteredResult,
                Data = JsonSerializer.Serialize<UserRegisteredResult>(rData)
            };

            string sReturn = JsonSerializer.Serialize(rPacket);
            return sReturn;

        }

        /**
         * 使用者登入
         */
        private string OnUserLogin(UserLogin packet)
        {
            UserLoginResult rData = new UserLoginResult();

            List<UserAccount> accountList = dbConnect.GetSql().Queryable<UserAccount>().Where(it => it.Email == packet.Email && it.Password == packet.Password).ToList();

            // 有找到帳號
            if (accountList.Count() == 1)
            {
                if (accountList[0].Password == packet.Password)
                {
                    rData.Result = 0;
                }
                else
                {
                    rData.Result = 2;
                }
            }
            else
            {
                rData.Result = 1;
            }

            PacketBase rPacket = new PacketBase
            {
                CmdID = (int)S2C_CmdID.emUserLoginResult,
                Data = JsonSerializer.Serialize<UserLoginResult>(rData)
            };

            string sReturn = JsonSerializer.Serialize(rPacket);
            return sReturn;
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

            PacketBase rPacket = new PacketBase
            {
                CmdID = (int)S2C_CmdID.emUpdateUserInfoResult,
                Data = JsonSerializer.Serialize<UpdateUserInfoResult>(rData)
            };

            string sReturn = JsonSerializer.Serialize(rPacket);
            return sReturn;
        }

    }

}
