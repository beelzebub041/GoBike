using System;
using System.Collections.Generic;
using System.Text;

namespace Service.Interface
{
    interface IControlCenter
    {
        // 初始化呼叫
        bool Initialize();

        // 刪除物件前呼叫
        bool Destroy();

    }
}
