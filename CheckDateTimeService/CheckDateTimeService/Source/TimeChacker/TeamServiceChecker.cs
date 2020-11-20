using System;
using System.Collections.Generic;
using System.Text;

using Connect;

using DataBaseDef;

using CheckDateTimeService.Source.Interface;

using Tools;
using Tools.RedisHashTransfer;

namespace CheckDateTimeService.Source.DataChecker
{
    class TeamServiceChecker: IChecker
    {
        // ==================== Delegate ==================== //

        public delegate bool CheckFunction();

        private Dictionary<string, CheckFunction> funcList = null;

        // ============================================ //


        private DataBaseConnect db = null;

        RedisConnect redis = null;

        RedisHashTransfer hashTransfer = null;


        public TeamServiceChecker()
        {
            funcList = new Dictionary<string, CheckFunction>();

            db = new DataBaseConnect(SaveLog);

            redis = new RedisConnect(SaveLog);

            hashTransfer = new RedisHashTransfer();
        }

        ~TeamServiceChecker()
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
                    AddUpdateFunc("TeamBulletin", CheckTeamBulletin);

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
        public void Check()
        {
            foreach (KeyValuePair<string, CheckFunction> func in funcList)
            {
                if (!func.Value()) {
                    SaveLog($"[Error] {func.Key} Check Error");
                }
            }
            
        }

        public void SaveLog(string msg)
        {
            Logger.Instance.SaveLog(msg);
        }

        // 新增UpdateFunc
        private void AddUpdateFunc(string name, CheckFunction func)
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

        // 檢查 TeamBulletin 資料
        private bool CheckTeamBulletin()
        {
            bool ret = false;

            try
            {
                List<TeamBulletin> bulletinList = db.GetSql().Queryable<TeamBulletin>().With(SqlSugar.SqlWith.RowLock).ToList();

                SaveLog($"[Info] Check Team Bulletin , Total Count: {bulletinList.Count}");

                for (int idx = 0; idx < bulletinList.Count; idx++)
                {
                    TeamBulletin info = bulletinList[idx];

                    DateTime createDate = DateTime.Parse(info.CreateDate);
                    DateTime curDate = DateTime.UtcNow;
                    DateTime limitDate = createDate.AddDays(info.Day);

                    // 超過時間
                    if (curDate > limitDate)
                    {
                        // 刪除DB的資料
                        if (db.GetSql().Deleteable<TeamBulletin>().With(SqlSugar.SqlWith.TabLockX).Where(it => it.BulletinID == info.BulletinID).ExecuteCommand() > 0)
                        {
                            // 刪除Redis中的資料
                            if (redis.GetRedis((int)Connect.RedisDB.emRedisDB_Team).KeyExists($"TeamBulletin_" + info.BulletinID))
                            {
                                redis.GetRedis((int)Connect.RedisDB.emRedisDB_Team).KeyDelete($"TeamBulletin_" + info.BulletinID);

                                redis.GetRedis((int)Connect.RedisDB.emRedisDB_Team).HashDelete($"BulletinIdList_" + info.TeamID, info.BulletinID);

                                SaveLog($"[Info] Check Team Bulletin , Delete Bulletin: {info.BulletinID}");
                            }
                            else
                            {
                                SaveLog($"[Error] Check Team Bulletin , Delete Bulletin: {info.BulletinID} Fail, Can Not Find Redis Data");
                            }
                        }
                        else
                        {
                            SaveLog($"[Error] Check Team Bulletin , Delete Bulletin: {info.BulletinID} From DB Fail");

                        }
                    }
                }

                ret = true;
            }
            catch (Exception ex)
            {
                SaveLog($"[Error] Check Team Bulletin Catch Error, Msg:{ex.Message}");
            }


            return ret;
        }
    }
}
