using System;
using System.Collections.Generic;
using System.Text;

namespace RedisDataUpdateService.Source.Interface
{
    interface IUpdater
    {
        // 初始化呼叫
        bool Initialize();

        // 刪除物件前呼叫
        bool Destroy();

        // 更新
        void Update();
    }
}
