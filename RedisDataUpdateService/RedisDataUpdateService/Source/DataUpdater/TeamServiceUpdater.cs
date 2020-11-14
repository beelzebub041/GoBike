using System;
using System.Collections.Generic;
using System.Text;

using Connect;

using DataBaseDef;

using DataUpdateService.Source.Interface;

using Tools;
using Tools.RedisHashTransfer;

namespace RedisDataUpdateService.Source.DataUpdater
{
    class TeamServiceUpdater: IUpdater
    {
        // ==================== Delegate ==================== //

        public delegate bool UpdateFunction();

        private Dictionary<string, UpdateFunction> funcList = null;

        // ============================================ //


        private DataBaseConnect db = null;

        RedisConnect redis = null;

        RedisHashTransfer hashTransfer = null;


        public TeamServiceUpdater()
        {
            funcList = new Dictionary<string, UpdateFunction>();

            db = new DataBaseConnect(SaveLog);

            redis = new RedisConnect(SaveLog);

            hashTransfer = new RedisHashTransfer();
        }

        ~TeamServiceUpdater()
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
                    AddUpdateFunc("TeamData", UpdateTeamData);
                    AddUpdateFunc("TeamBulletin", UpdateTeamBulletin);
                    AddUpdateFunc("TeamActivity", UpdateTeamActivity);

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

        // 更新 TeamData 資料
        private bool UpdateTeamData()
        {
            bool ret = false;

            try
            {
                List<TeamData> teamList = db.GetSql().Queryable<TeamData>().With(SqlSugar.SqlWith.RowLock).ToList();

                SaveLog($"[Info] Update Team Data , Total Count: {teamList.Count}");

                for (int idx = 0; idx < teamList.Count; idx++)
                {
                    TeamData info = teamList[idx];

                    redis.GetRedis((int)Connect.RedisDB.emRedisDB_Team).HashSet($"TeamData_" + info.TeamID, hashTransfer.TransToHashEntryArray(info));
                
                    SaveLog($"[Info] Update Team Data , Team: {info.TeamID}");
                }

                ret = true;
            }
            catch (Exception ex)
            {
                SaveLog($"[Error] Update Team Data Catch Error, Msg:{ex.Message}");
            }

            return ret;
        }

        // 更新 TeamBulletin 資料
        private bool UpdateTeamBulletin()
        {
            bool ret = false;

            try
            {
                List<TeamBulletin> bulletinList = db.GetSql().Queryable<TeamBulletin>().With(SqlSugar.SqlWith.RowLock).ToList();

                SaveLog($"[Info] Update Team Bulletin , Total Count: {bulletinList.Count}");

                for (int idx = 0; idx < bulletinList.Count; idx++)
                {
                    TeamBulletin info = bulletinList[idx];

                    redis.GetRedis((int)Connect.RedisDB.emRedisDB_Team).HashSet($"TeamBulletin_" + info.BulletinID, hashTransfer.TransToHashEntryArray(info));

                    redis.GetRedis((int)Connect.RedisDB.emRedisDB_Team).HashSet($"BulletinIdList_" + info.TeamID, info.BulletinID, info.BulletinID);

                    SaveLog($"[Info] Update Team Bulletin , Bulletin: {info.BulletinID}");
                }

                ret = true;
            }
            catch (Exception ex)
            {
                SaveLog($"[Error] Update Team Bulletin Catch Error, Msg:{ex.Message}");
            }

            return ret;
        }

        // 更新 TeamActivity 資料
        private bool UpdateTeamActivity()
        {
            bool ret = false;

            try
            {
                List<TeamActivity> actList = db.GetSql().Queryable<TeamActivity>().With(SqlSugar.SqlWith.RowLock).ToList();

                SaveLog($"[Info] Update Team Activity , Total Count: {actList.Count}");

                for (int idx = 0; idx < actList.Count; idx++)
                {
                    TeamActivity info = actList[idx];

                    redis.GetRedis((int)Connect.RedisDB.emRedisDB_Team).HashSet($"TeamActivity_" + info.ActID, hashTransfer.TransToHashEntryArray(info));

                    redis.GetRedis((int)Connect.RedisDB.emRedisDB_Team).HashSet($"ActIdList_" + info.TeamID, info.ActID, info.ActID);

                    SaveLog($"[Info] Update Team Activity , Activity: {info.ActID}");
                }

                ret = true;
            }
            catch (Exception ex)
            {
                SaveLog($"[Error] Update Team Activity Catch Error, Msg:{ex.Message}");
            }

            return ret;
        }

    }
}
