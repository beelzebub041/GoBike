using System;
using System.Collections.Generic;
using System.Text;

using Tools;

namespace Service.Interface
{
    interface IControlCenter
    {
        // 初始化呼叫
        bool Initialize(Logger logger);

        // 刪除物件前呼叫
        bool Destroy();

    }
}
