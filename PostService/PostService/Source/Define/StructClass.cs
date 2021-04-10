using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Source.Define.StructClass
{
    public class MsgInfo
    {
        public string msg { get; set; }

        public delegate void SendToClient(string msg);

        private SendToClient send = null;

        public MsgInfo(string msg, SendToClient send)
        {
            this.msg = msg;

            this.send = send;
        }

        public void Send(string msg)
        {
            send?.Invoke(msg);
        }

    }
}
