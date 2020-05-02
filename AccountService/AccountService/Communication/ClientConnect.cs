using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Net;
using System.Net.Sockets;
using System.Threading;

using System.Net.WebSockets;

using AccountService;
using Tools.Logger;

using System.Text.Json;


namespace AccountService.Communication
{
    class ClientConnect
    {
        private Logger log = null;

        private Socket serverSocket = null;

        private Socket clientSocket = null;

        private AccountService accountService = null;

        public ClientConnect(string sIp, int iPort, AccountService service, Logger log)
        {
            this.log = log;

            accountService = service;

            try
            {
                serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                serverSocket.Bind(new IPEndPoint(IPAddress.Parse(sIp), iPort));

                serverSocket.Listen(1);
            }
            catch (SocketException e)
            {

            }

        }

        ~ClientConnect()
        {
            if (serverSocket != null)
            {
                Stop();
            }
        }

        /**
         * 開啟通訊
         */
        public void Start()
        {
            Thread waitAcceptThread = new Thread(WaitAccept);
            waitAcceptThread.Start();
        }

        public void Stop()
        {
            if (serverSocket != null)
            {
                serverSocket.Close();
            }
        }

        /**
         * 等待連線
         */
        private void WaitAccept()
        {
            bool closed = false;

            while (!closed)
            {
                if (serverSocket != null)
                {
                    try
                    {
                        // 等待Client 端連線
                        clientSocket = serverSocket.Accept();

                        Thread RecvMsgThread = new Thread(new ParameterizedThreadStart(RecvMsg));
                        RecvMsgThread.Start();

                        string sLog = "Client Csonnected";

                        log.saveLog(sLog);

                        SendMsg(sLog);

                    }
                    catch (SocketException e)
                    {
                        closed = true;
                    }
                    
                }
                else
                {
                    Console.WriteLine("Socket Obiect is null");
                }

                Thread.Sleep(100);
            }

        }

        /**
         * 接收訊息
         */
        private void RecvMsg(object value)
        {
            int iClientID = Convert.ToInt32(value);

            bool clientClosed = false;

            try
            {
                while (clientSocket.Connected && !clientClosed)
                {
                
                    byte[] clientData = new byte[clientSocket.Available];

                    long IntAcceptData = clientSocket.Receive(clientData);

                    string msg = Encoding.UTF8.GetString(clientData);

                    accountService.DispatchMessage("ToDataBaseService", msg);

                    Console.WriteLine(msg);
                

                    Thread.Sleep(100);
                }
            }
            catch (SocketException e)
            {
                clientClosed = true;

                clientSocket = null;
            }

        }

        /**
         * 傳送訊息
         */
        public void SendMsg(string sMsg)
        {
            if (clientSocket != null)
            {
                clientSocket.Send(Encoding.UTF8.GetBytes(sMsg));
            }
        }

    }
}
