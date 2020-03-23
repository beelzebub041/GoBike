using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace AccountService.Communication
{
    class DataBaseServiceConnect
    {
        private Socket clientSocket = null;

        private AccountService accountService = null;

        public DataBaseServiceConnect()
        {

        }

        ~DataBaseServiceConnect()
        {
            if (clientSocket != null)
            {
                Disconnect();
            }
        }

        /**
         * 
         * 
         */
        public void Connect(string sIP, int iPort, AccountService service)
        {
            try
            {
                accountService = service;

                clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                clientSocket.Connect(new IPEndPoint(IPAddress.Parse(sIP), iPort));

                Thread RecvMsgThread = new Thread(RecvMsg);
                RecvMsgThread.Start();

            }
            catch
            {

            }
        }

        public void Disconnect()
        {
            if (clientSocket != null)
            {
                clientSocket.Close();
            }
        }

        /**
         * 接收訊息
         */
        private void RecvMsg()
        {
            long IntAcceptData;

            bool closed = false;

            while (!closed)
            {

                try
                {
                    byte[] serverData = new byte[clientSocket.Available];

                    IntAcceptData = clientSocket.Receive(serverData);

                    string sMsg = Encoding.UTF8.GetString(serverData);

                    accountService.DispatchMessage("ToClient", sMsg);

                    Console.WriteLine(sMsg);
                }
                catch (SocketException e)
                {
                    closed = true;
                }
            }

        }

        /**
         * 送出訊息
         */
        public void SendMsg(string sMsg)
        {
            try
            {
                clientSocket.Send(Encoding.UTF8.GetBytes(sMsg));
            }
            catch
            {

            }
        }


    }
}
