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
    class UserServiceUpdater: IUpdater
    {
        // ==================== Delegate ==================== //

        public delegate bool UpdateFunction();

        private Dictionary<string, UpdateFunction> funcList = null;

        // ============================================ //


        private DataBaseConnect db = null;

        RedisConnect redis = null;

        RedisHashTransfer hashTransfer = null;


        public UserServiceUpdater()
        {
            funcList = new Dictionary<string, UpdateFunction>();

            db = new DataBaseConnect(SaveLog);

            redis = new RedisConnect(SaveLog);

            hashTransfer = new RedisHashTransfer();
        }

        ~UserServiceUpdater()
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
                    AddUpdateFunc("UserAccount", UpdateUserAccount);
                    AddUpdateFunc("UserInfo", UpdateUserInfo);

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

        // 更新 UserAccount 資料
        private bool UpdateUserAccount()
        {
            bool ret = false;

            try
            {
                List<UserAccount> accountList = db.GetSql().Queryable<UserAccount>().With(SqlSugar.SqlWith.RowLock).ToList();

                SaveLog($"[Info] Update User Account , Total Count: {accountList.Count}");

                for (int idx = 0; idx < accountList.Count; idx++)
                {
                    UserAccount info = accountList[idx];

                    redis.GetRedis((int)Connect.RedisDB.emRedisDB_User).HashSet($"UserAccount_" + info.Email, hashTransfer.TransToHashEntryArray(info));
                
                    SaveLog($"[Info] Update User Account , User: {info.Email}");
                }

                ret = true;
            }
            catch (Exception ex)
            {
                SaveLog($"[Error] Update User Account Catch Error, Msg:{ex.Message}");
            }

            return ret;
        }

        // 更新 UserInfo 資料
        private bool UpdateUserInfo()
        {
            bool ret = false;

            try
            {
                List<UserInfo> infoList = db.GetSql().Queryable<UserInfo>().With(SqlSugar.SqlWith.RowLock).ToList();

                SaveLog($"[Info] Update User Info , Total Count: {infoList.Count}");

                for (int idx = 0; idx < infoList.Count; idx++)
                {
                    UserInfo info = infoList[idx];

                    redis.GetRedis((int)Connect.RedisDB.emRedisDB_User).HashSet($"UserInfo_" + info.MemberID, hashTransfer.TransToHashEntryArray(info));

                    SaveLog($"[Info] Update User Info , User: {info.MemberID}");
                }

                ret = true;
            }
            catch (Exception ex)
            {
                SaveLog($"[Error] Update User Info Catch Error, Msg:{ex.Message}");
            }

            return ret;
        }

    }
}
