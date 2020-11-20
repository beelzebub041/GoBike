using System;
using System.Collections.Generic;
using System.Text;

using Connect;

using DataBaseDef;

using RedisDataUpdateService.Source.Interface;

using Tools;
using Tools.RedisHashTransfer;
using Tools.WeekProcess;

namespace RedisDataUpdateService.Source.DataUpdater
{
    class RideServiceUpdater: IUpdater
    {
        // ==================== Delegate ==================== //

        public delegate bool UpdateFunction();

        private Dictionary<string, UpdateFunction> funcList = null;

        // ============================================ //


        private DataBaseConnect db = null;

        RedisConnect redis = null;

        RedisHashTransfer hashTransfer = null;

        private WeekProcess weekProcess = null;

        public RideServiceUpdater()
        {
            funcList = new Dictionary<string, UpdateFunction>();

            db = new DataBaseConnect(SaveLog);

            redis = new RedisConnect(SaveLog);

            hashTransfer = new RedisHashTransfer();

            weekProcess = new WeekProcess();
        }

        ~RideServiceUpdater()
        {
            Destroy();
        }

        // 初始化呼叫
        public bool Initialize()
        {
            bool success = false;

            if (db.Initialize() && redis.Initialize())
            {
                if (db.Connect() && redis.Connect())
                {
                    AddUpdateFunc("RideData", UpdateRideData);
                    AddUpdateFunc("RideRecord", UpdateRideRecord);
                    AddUpdateFunc("RideIdList", UpdateRideIdList);
                    AddUpdateFunc("CurWeekRideData", UpdateWeekRideData);

                    success = true;
                }
                else
                {
                    SaveLog($"[Error] Connect DB Or Redis Error");
                }
            }

            return success;
        }

        // 刪除物件前呼叫
        public bool Destroy()
        {
            if (db != null)
            {
                db.Disconnect();
            }

            if (redis != null)
            {
                redis.Disconnect();
            }

            return true;
        }

        // 更新
        public void Update()
        {
            foreach (KeyValuePair<string, UpdateFunction> func in funcList)
            {
                if (!func.Value()) {
                    SaveLog($"[Error] {func.Key} Update Error");
                }
            }
            
        }

        public void SaveLog(string msg)
        {
            Logger.Instance.SaveLog(msg);
        }

        // 新增UpdateFunc
        private void AddUpdateFunc(string name, UpdateFunction func)
        {
            if (!funcList.ContainsKey(name))
            {
                funcList.Add(name, func);
            }
            else
            {
                SaveLog($"[Warning] {name} Function Repeat");
            }
        }

        // 更新 RideData 資料
        private bool UpdateRideData()
        {
            bool ret = false;

            try
            {
                List<RideData> rideList = db.GetSql().Queryable<RideData>().With(SqlSugar.SqlWith.RowLock).ToList();

                SaveLog($"[Info] Update Ride Data , Total Count: {rideList.Count}");

                for (int idx = 0; idx < rideList.Count; idx++)
                {
                    RideData info = rideList[idx];

                    redis.GetRedis((int)Connect.RedisDB.emRedisDB_Ride).HashSet($"RideData_" + info.MemberID, hashTransfer.TransToHashEntryArray(info));

                    SaveLog($"[Info] Update Ride Data, User: {info.MemberID}");
                }

                ret = true;
            }
            catch (Exception ex)
            {
                SaveLog($"[Error] Update Ride Data Catch Error, Msg:{ex.Message}");
            }

            return ret;
        }

        // 更新 RideRecord 資料
        private bool UpdateRideRecord()
        {
            bool ret = false;

            try
            {
                List<RideRecord> recordList = db.GetSql().Queryable<RideRecord>().With(SqlSugar.SqlWith.RowLock).ToList();

                SaveLog($"[Info] Update Ride Record, Total Count: {recordList.Count}");

                for (int idx = 0; idx < recordList.Count; idx++)
                {
                    RideRecord info = recordList[idx];

                    redis.GetRedis((int)Connect.RedisDB.emRedisDB_Ride).HashSet($"RideRecord_" + info.RideID, hashTransfer.TransToHashEntryArray(info));

                    SaveLog($"[Info] Update Ride Record, User: {info.MemberID}");
                }

                ret = true;
            }
            catch (Exception ex)
            {
                SaveLog($"[Error] Update Ride Info Record Error, Msg:{ex.Message}");
            }

            return ret;
        }

        // 更新 RideIdList 資料
        private bool UpdateRideIdList()
        {
            bool ret = false;

            try
            {
                List<UserInfo> userList = db.GetSql().Queryable<UserInfo>().With(SqlSugar.SqlWith.RowLock).ToList();

                SaveLog($"[Info]  Update Ride ID List, Total User Count: {userList.Count}");

                for (int idx = 0; idx < userList.Count; idx++)
                {
                    UserInfo user = userList[idx];

                    List<RideRecord> recordList = db.GetSql().Queryable<RideRecord>().With(SqlSugar.SqlWith.RowLock).Where(it => it.MemberID == user.MemberID).ToList();

                    SaveLog($"[Info]  Update Ride ID List, User: {user.MemberID} , Total Ride Record Count: {recordList.Count}");

                    for (int subIdx = 0; subIdx < recordList.Count; subIdx++)
                    {
                        RideRecord record = recordList[subIdx];

                        redis.GetRedis((int)Connect.RedisDB.emRedisDB_Ride).HashSet($"RideIdList_" + user.MemberID, record.RideID, record.RideID);
                    }
                }

                ret = true;
            }
            catch (Exception ex)
            {
                SaveLog($"[Error] Update Ride ID List Catch Error, Msg:{ex.Message}");
            }

            return ret;
        }

        // 更新 WeekRideData 資料
        private bool UpdateWeekRideData()
        {
            bool ret = false;

            try
            {

                List<UserInfo> userList = db.GetSql().Queryable<UserInfo>().With(SqlSugar.SqlWith.RowLock).ToList();

                SaveLog($"[Info]  Update Week Ride Data, Total User Count: {userList.Count}");

                for (int idx = 0; idx < userList.Count; idx++)
                {
                    UserInfo user = userList[idx];


                    // 本週資料
                    {
                        // 取得本週的第一天 與 最後一天
                        string curWeek_FirsDay = weekProcess.GetWeekFirstDay(DateTime.UtcNow);
                        string curWeek_LastDay = weekProcess.GetWeekLastDay(DateTime.UtcNow);

                        WeekRideData curWeekRideData = db.GetSql().Queryable<WeekRideData>().With(SqlSugar.SqlWith.RowLock).Where(it => it.MemberID == user.MemberID && it.WeekFirstDay == curWeek_FirsDay && it.WeekLastDay == curWeek_LastDay).Single();

                        if (curWeekRideData == null)
                        {
                            curWeekRideData = new WeekRideData();
                            curWeekRideData.MemberID = user.MemberID;
                            curWeekRideData.WeekFirstDay = curWeek_FirsDay;
                            curWeekRideData.WeekLastDay = curWeek_LastDay;
                            curWeekRideData.WeekDistance = 0;
                        }

                        redis.GetRedis((int)Connect.RedisDB.emRedisDB_Ride).HashSet($"CurWeekRideData_" + user.MemberID, hashTransfer.TransToHashEntryArray(curWeekRideData));
                    
                        SaveLog($"[Info]  Update Week Ride Data, User: {user.MemberID}'s Cur Week Ride Data");
                    }

                    // 上週資料
                    {
                        // 取得上週的第一天 與 最後一天
                        string lastWeek_FirsDay = weekProcess.GetLastWeekFirstDay(DateTime.UtcNow);
                        string lastWeek_LastDay = weekProcess.GetLastWeekLastDay(DateTime.UtcNow);

                        WeekRideData lastWeekRideData = db.GetSql().Queryable<WeekRideData>().With(SqlSugar.SqlWith.RowLock).Where(it => it.MemberID == user.MemberID && it.WeekFirstDay == lastWeek_FirsDay && it.WeekLastDay == lastWeek_LastDay).Single();

                        if (lastWeekRideData == null)
                        {
                            lastWeekRideData = new WeekRideData();
                            lastWeekRideData.MemberID = user.MemberID;
                            lastWeekRideData.WeekFirstDay = lastWeek_FirsDay;
                            lastWeekRideData.WeekLastDay = lastWeek_LastDay;
                            lastWeekRideData.WeekDistance = 0;
                        }

                        redis.GetRedis((int)Connect.RedisDB.emRedisDB_Ride).HashSet($"LastWeekRideData_" + user.MemberID, hashTransfer.TransToHashEntryArray(lastWeekRideData));

                        SaveLog($"[Info]  Update Week Ride Data, User: {user.MemberID}'s Last Week Ride Data");

                    }
                }

                ret = true;
            }
            catch (Exception ex)
            {
                SaveLog($"[Error] Update Week Ride Data Catch Error, Msg:{ex.Message}");
            }

            return ret;
        }

    }
}
