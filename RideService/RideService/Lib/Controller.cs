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


namespace RideService
{
    class Controller
    {
        private Form1 fm1 = null;

        private Logger log = null;                      // Logger

        private DataBaseConnect dbConnect = null;

        private Server wsServer = null;                 // Web Socket Server

        private string ControllerVersion = "Version001";


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
                                case (int)C2S_CmdID.UpdateRideData:

                                    UpdateRideData UpdateData = JsonConvert.DeserializeObject<UpdateRideData>(packetData);

                                    sReturn = OnUpdateRideData(UpdateData);

                                    break;

                                case (int)C2S_CmdID.CreateRideRecord:
                                    CreateRideRecord CreateData = JsonConvert.DeserializeObject<CreateRideRecord>(packetData);

                                    sReturn = OnCreateRideRecord(CreateData);

                                    break;

                                //case (int)C2S_CmdID.GetRideRecordIdList:
                                //    GetRideRecordIdList dataList = JsonConvert.DeserializeObject<GetRideRecordIdList>(packetData);

                                //    sReturn = OnGetRideRecordIdList(dataList);

                                //    break;
                                //case (int)C2S_CmdID.GetRideRecord:
                                //    GetRideRecord recordData = JsonConvert.DeserializeObject<GetRideRecord>(packetData);

                                //    sReturn = OnGetRideRecord(recordData);

                                //    break;

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
         * 更新騎乘資料
         */
        private string OnUpdateRideData(UpdateRideData packet)
        {
            UpdateRideDataResult rData = new UpdateRideDataResult();

            try 
            {
                List<UserAccount> accountList = dbConnect.GetSql().Queryable<UserAccount>().Where(it => it.UserID == packet.UserID).ToList();

                // 有找到帳號
                if (accountList.Count() == 1)
                {
                    RideData info = new RideData
                    {
                        UserID = packet.UserID,
                        TotalDistance = packet.TotalDistance,
                        TotalAltitude = packet.TotalAltitude,
                        TotalRideTime = packet.TotalRideTime,
                    };

                    dbConnect.GetSql().Updateable(info).ExecuteCommand();

                    rData.Result = 1;
                }
                else
                {
                    rData.Result = 0;
                }
            }
            catch (Exception ex)
            {
                log.SaveLog("[Error] Controller::OnUpdateRideData Catch Error, Msg:" + ex.Message);

                rData.Result = 0;
            }

            JObject jsMain = new JObject();
            jsMain.Add("CmdID", (int)S2C_CmdID.emUpdateRideDataResult);
            jsMain.Add("Data", JsonConvert.DeserializeObject<JObject>(JsonConvert.SerializeObject(rData)));

            return jsMain.ToString();
        }

        /**
         * 建立騎乘紀錄
         */
        private string OnCreateRideRecord(CreateRideRecord packet)
        {
            CreateRideRecordResult rData = new CreateRideRecordResult();

            try 
            {
                List<UserAccount> accountList = dbConnect.GetSql().Queryable<UserAccount>().Where(it => it.UserID == packet.UserID).ToList();

                // 有找到帳號
                if (accountList.Count() == 1)
                {
                    string dateTime = DateTime.Now.ToString("yyyyMMdd-hhmmss");

                    string guidAll = Guid.NewGuid().ToString();

                    string[] guidList = guidAll.Split('-');

                    RideRecord record = new RideRecord
                    {
                        RideID = "Dblha-" + guidList[0] + "-" + dateTime,
                        UserID = packet.UserID,
                        CreateDate = packet.CreateDate,
                        Title = packet.Title,
                        Photo = packet.Photo,
                        Time = packet.Time,
                        Distance = packet.Distance,
                        Altitude = packet.Altitude,
                        Level = packet.Level,
                        CountyID = packet.CountyID,
                        Route = packet.Route,
                        ShareContent = packet.ShareContent,
                        SharedType = packet.SharedType
                    };

                    dbConnect.GetSql().Insertable(record).ExecuteCommand();

                    rData.Result = 1;

                }
                else
                {
                    rData.Result = 0;
                }
            }
            catch (Exception ex)
            {
                log.SaveLog("[Error] Controller::OnCreateRideRecord Catch Error, Msg:" + ex.Message);

                rData.Result = 0;
            }

            JObject jsMain = new JObject();
            jsMain.Add("CmdID", (int)S2C_CmdID.emCreateRideRecordResult);
            jsMain.Add("Data", JsonConvert.DeserializeObject<JObject>(JsonConvert.SerializeObject(rData)));

            return jsMain.ToString();
        }

        ///**
        // * 取得騎乘紀錄ID列表
        // */
        //private string OnGetRideRecordIdList(GetRideRecordIdList packet)
        //{
        //    RespondRideRecordIdList rData = new RespondRideRecordIdList();

        //    JObject jsMain = new JObject();
        //    jsMain.Add("CmdID", (int)S2C_CmdID.emRespondRideRecordIdList);
        //    jsMain.Add("Data", JsonConvert.DeserializeObject<JObject>(JsonConvert.SerializeObject(rData)));

        //    return jsMain.ToString();
        //}

        ///**
        // * 取得騎乘紀錄ID列表
        // */
        //private string OnGetRideRecord(GetRideRecord packet)
        //{
        //    RespondRideRecord rData = new RespondRideRecord();

        //    JObject jsMain = new JObject();
        //    jsMain.Add("CmdID", (int)S2C_CmdID.emRespondRideRecord);
        //    jsMain.Add("Data", JsonConvert.DeserializeObject<JObject>(JsonConvert.SerializeObject(rData)));

        //    return jsMain.ToString();
        //}



    }

}
