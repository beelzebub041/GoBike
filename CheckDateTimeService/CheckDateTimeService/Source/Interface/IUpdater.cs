using System;
using System.Collections.Generic;
using System.Text;

namespace CheckDateTimeService.Source.Interface
{
    interface IChecker
    {
        // 初始化呼叫
        bool Initialize();

        // 刪除物件前呼叫
        bool Destroy();

        // 更新
        void Check();
    }
}
