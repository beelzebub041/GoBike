using System;
using System.Collections.Generic;
using System.Text;

using Connect;

using DataBaseDef;

using CheckDateTimeService.Source.Interface;

using Tools;
using Tools.RedisHashTransfer;
using Tools.WeekProcess;

namespace CheckDateTimeService.Source.DataChecker
{
    class RideServiceChecker: IChecker
    {
        // ==================== Delegate ==================== //

        public delegate bool CheckFunction();

        private Dictionary<string, CheckFunction> funcList = null;

        // ============================================ //


        private DataBaseConnect db = null;

        RedisConnect redis = null;

        RedisHashTransfer hashTransfer = null;

        public RideServiceChecker()
        {
            funcList = new Dictionary<string, CheckFunction>();

            db = new DataBaseConnect(SaveLog);

            redis = new RedisConnect(SaveLog);

            hashTransfer = new RedisHashTransfer();

        }

        ~RideServiceChecker()
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


    }
}
