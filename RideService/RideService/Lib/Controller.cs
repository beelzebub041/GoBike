using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Windows.Forms;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Tools.Logger;
using Tools.WeekProcess;

using DataBaseDef;
using Connect;

using RidePacket.ClientToServer;
using RidePacket.ServerToClient;


namespace RideService
{
    class Controller
    {
        private Form1 fm1 = null;

        private Logger log = null;                      // Logger

        private WeekProcess weekProcess = null;         // WeekProcess

        private DataBaseConnect dbConnect = null;

        private Server wsServer = null;                 // Web Socket Server

        private object msgLock = new object();

        private string version = "Ride006";


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

                weekProcess = new WeekProcess();

                log.SaveLog($"Controller Version: {version}");

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
                                    case (int)C2S_CmdID.CreateRideRecord:
                                        CreateRideRecord CreateData = JsonConvert.DeserializeObject<CreateRideRecord>(packetData);

                                        sReturn = OnCreateRideRecord(CreateData);

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
         * 建立騎乘紀錄
         */
        private string OnCreateRideRecord(CreateRideRecord packet)
        {
            CreateRideRecordResult rData = new CreateRideRecordResult();

            try 
            {
                List<UserAccount> accountList = dbConnect.GetSql().Queryable<UserAccount>().With(SqlSugar.SqlWith.RowLock).Where(it => it.MemberID == packet.MemberID).ToList();

                List<RideData> rideDataList = dbConnect.GetSql().Queryable<RideData>().With(SqlSugar.SqlWith.RowLock).Where(it => it.MemberID == packet.MemberID).ToList();

                // 有找到帳號 且 有找到 騎乘資料
                if (accountList.Count() == 1 && rideDataList.Count() == 1)
                {
                    string dateTime = DateTime.UtcNow.ToString("yyyy-MM-dd hh:mm:ss");

                    string guidAll = Guid.NewGuid().ToString();

                    string[] guidList = guidAll.Split('-');

                    // ======================= 新增騎乘紀錄 =======================
                    RideRecord record = new RideRecord
                    {
                        RideID = "DbRr-" + guidList[0] + "-" + DateTime.UtcNow.ToString("MMdd-hhmmss"),
                        MemberID = packet.MemberID,
                        CreateDate = dateTime,
                        Title = packet.Title,
                        Photo = packet.Photo,
                        Time = packet.Time,
                        Distance = packet.Distance,
                        Altitude = packet.Altitude,
                        Level = packet.Level,
                        County = packet.County,
                        Route = packet.Route,
                        ShareContent = packet.ShareContent,
                        SharedType = packet.SharedType
                    };

                    if (dbConnect.GetSql().Insertable(record).With(SqlSugar.SqlWith.TabLockX).ExecuteCommand() > 0)
                    {
                        // ======================= 更新騎乘資料 =======================
                        rideDataList[0].TotalDistance += packet.Distance;
                        rideDataList[0].TotalAltitude += packet.Altitude;
                        rideDataList[0].TotalRideTime += packet.Time;

                        dbConnect.GetSql().Updateable<RideData>(rideDataList[0]).With(SqlSugar.SqlWith.RowLock).Where(it => it.MemberID == packet.MemberID).ExecuteCommand();


                        // ======================= 更新周騎乘資料 =======================
                        string firsDay = weekProcess.GetWeekFirstDay(DateTime.UtcNow);
                        string lastDay = weekProcess.GetWeekLastDay(DateTime.UtcNow);

                        List<WeekRideData> wRideDataList = dbConnect.GetSql().Queryable<WeekRideData>().With(SqlSugar.SqlWith.RowLock).Where(it => it.MemberID == packet.MemberID && it.WeekFirstDay == firsDay && it.WeekLastDay == lastDay).ToList();

                        // 有找到資料
                        if (wRideDataList.Count() == 1)
                        {
                            wRideDataList[0].WeekDistance += packet.Distance;

                            dbConnect.GetSql().Updateable<WeekRideData>(wRideDataList[0]).With(SqlSugar.SqlWith.RowLock).Where(it => it.MemberID == packet.MemberID && it.WeekFirstDay == firsDay && it.WeekLastDay == lastDay).ExecuteCommand();
                        }
                        else
                        {
                            WeekRideData updateWeek = new WeekRideData
                            {
                                MemberID = packet.MemberID,
                                WeekFirstDay = firsDay,
                                WeekLastDay = lastDay,
                                WeekDistance = packet.Distance
                            };

                            dbConnect.GetSql().Insertable(updateWeek).With(SqlSugar.SqlWith.TabLockX).ExecuteCommand();

                        }

                        rData.Result = 1;
                        rData.TotalDistance = rideDataList[0].TotalDistance;
                        rData.TotalAltitude = rideDataList[0].TotalAltitude;
                        rData.TotalRideTime = rideDataList[0].TotalRideTime;
                    }
                    else
                    {
                        rData.Result = 0;
                    }

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

    }

}
