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

                if (jsMain.TryGetProperty("cmdID", out JsonElement jsName))
                {
                    int cmdID = jsName.GetInt32();

                    string packetData = jsMain.GetProperty("data").GetString();

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

        private string OnCreateNewAccount(UserRegistered packet)
        {
            UserRegisteredResult rData = new UserRegisteredResult();

            if (packet.password == packet.checkPassword)
            {
                List<UserInfo> accountList = dbConnect.GetSql().Queryable<UserInfo>().Where(it => it.Email == packet.email).ToList();
                   
                // 未包含該Email
                if (accountList.Count() == 0 )
                {
                    try

                    {
                        UserInfo info = new UserInfo();
                        info.Email = packet.email;
                        info.Password = packet.password;
                        info.CREATE_DATE = DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss");

                        int ID = dbConnect.GetSql().Insertable(info).ExecuteReturnIdentity();

                        Console.WriteLine(ID);
                    }
                    catch (Exception ex)
                    {
                        log.SaveLog("[Error] Controller::OnCreateNewAccount Create Fail, Error Msg:" + ex.Message);
                    }

                    rData.result = 0;
                }
                else
                {
                    rData.result = 1;
                }

            }
            else
            {
                rData.result = 2;
            }

            PacketBase rPacket = new PacketBase
            {
                cmdID = (int)S2C_CmdID.emUserRegisteredResult,
                data = JsonSerializer.Serialize<UserRegisteredResult>(rData)
            };

            string sReturn = JsonSerializer.Serialize(rPacket);
            return sReturn;

        }

        private string OnUserLogin(UserLogin packet)
        {
            UserLoginResult rData = new UserLoginResult();

            List<UserInfo> accountList = dbConnect.GetSql().Queryable<UserInfo>().Where(it => it.Email == packet.email && it.Password == packet.password).ToList();

            // TODO 需新增帳號錯誤 或 密碼錯誤
            if (accountList.Count() == 1)
            {
                rData.result = 1;
            }
            else
            {
                rData.result = 0;
            }

            PacketBase rPacket = new PacketBase
            {
                cmdID = (int)S2C_CmdID.emUserLoginResult,
                data = JsonSerializer.Serialize<UserLoginResult>(rData)
            };

            string sReturn = JsonSerializer.Serialize(rPacket);
            return sReturn;
        }
    }

}
