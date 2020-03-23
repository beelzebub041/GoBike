using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Net;
using System.Net.Sockets;
using System.Threading;

using System.Text.Json;

using Tools.Logger;

using DataBaseService.Handler;
using Packet.ServerToClient;
using Packet.ClientToServer;

namespace DataBaseService.Communication
{
    class ClientConnect
    {
        private Logger log = null;

        private Socket serverSocket = null;

        private Socket[] clientSocketList;

        private DbHandler dbHander;

        private int iClientSocketIdx;

        public ClientConnect(string sIp, int iPort, DbHandler dbh, Logger log)
        {
            this.log = log;
            
            iClientSocketIdx = 0;

            serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            serverSocket.Bind(new IPEndPoint(IPAddress.Parse(sIp), iPort));

            serverSocket.Listen(10);

            // 動態增加 ClientSocketList 長度
            Array.Resize(ref clientSocketList, 1);

            dbHander = dbh;

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
            log.saveLog("[Info] Start: Start Wait Accept");

            Thread waitAcceptThread = new Thread(WaitAccept);
            waitAcceptThread.Start();
        }

        public void Stop()
        {
            if (serverSocket != null)
            {
                log.saveLog("[Info] Stop: Stop Socket");

                try
                {
                   // serverSocket.Shutdown(SocketShutdown.Both);
                }
                finally
                {
                    serverSocket.Close();
                }
            }
            else
            {
                log.saveLog("[Info] Stop: Can Not Find Socket");
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
                        clientSocketList[iClientSocketIdx] = serverSocket.Accept();

                        Thread RecvMsgThread = new Thread(new ParameterizedThreadStart(RecvMsg));
                        RecvMsgThread.Start(iClientSocketIdx);

                        iClientSocketIdx++;

                        // 動態增加 ClientSocketList 長度
                        Array.Resize(ref clientSocketList, iClientSocketIdx + 1);
                    }
                    catch (SocketException e)
                    {
                        closed = true;

                        throw e;
                    }

                    Thread.Sleep(100);
                }
                else
                {
                    log.saveLog("[Info] WaitAccept: Socket Obiect is null");
                }
            }

        }

        /**
         * 接收訊息
         */
        void RecvMsg(object value)
        {
            int iClientID = Convert.ToInt32(value);

            bool clientClosed = false;

            while (clientSocketList[iClientID].Connected && !clientClosed)
            {
                try
                {
                    byte[] clientData = new byte[clientSocketList[iClientID].Available];

                    long IntAcceptData = clientSocketList[iClientID].Receive(clientData);

                    string msg = Encoding.UTF8.GetString(clientData);

                    MessageProcess(msg, iClientID);

                    Console.WriteLine(msg);
                }
                catch (SocketException e)
                {
                    clientClosed = true;

                    throw e;
                }

                Thread.Sleep(100);
            }


        }

        /**
         * 傳送訊息
         */
        private void SendMsg(string sMsg, int iClientIdx)
        {
            clientSocketList[iClientIdx].Send(Encoding.UTF8.GetBytes(sMsg));
        }

        private void MessageProcess(string msg, int iClientIdx)
        {
            //try
            if (msg != "")
            {
                JsonDocument jsonDoc = JsonDocument.Parse(msg);

                JsonElement jsMain = jsonDoc.RootElement;

                if (jsMain.TryGetProperty("Name", out JsonElement jsName))
                {
                    string packetName = jsName.GetString();

                    string packetData = jsMain.GetProperty("Data").GetString();

                    switch (packetName)
                    {
                        case "C2S_UserRegistered":
                            C2S_UserRegistered msgObject = JsonSerializer.Deserialize<C2S_UserRegistered>(packetData);

                            AddNewAccount(msgObject, iClientIdx);

                            break;
                    }

                }
            }
            //catch
            {
                Console.WriteLine("[Info][Warning] ClientConnect::MessageProcess Process Error");
            }
            


        }

        private void AddNewAccount(C2S_UserRegistered msgObject, int iClientIdx)
        {
            S2C_UserRegisteredResult rData = new S2C_UserRegisteredResult();

            if (msgObject.Password == msgObject.CheckPassword)
            {
                DateTime dt = DateTime.Now;

                string sqlCmd = "INSERT INTO userinfo.useraccount (ID, Account, Password, CREATE_DATE) VALUES(0, '" + msgObject.Account + "', '" + msgObject.Password + "', '" + dt.ToString("yyyy-MM-dd hh:mm:ss") + "')";

                dbHander.SqlCommandProcess(sqlCmd);

                rData.Result = 0;
            }
            else
            {
                rData.Result = 2;
            }

            string packet = JsonSerializer.Serialize<S2C_UserRegisteredResult>(rData);

            SendMsg(packet, iClientIdx);

        }
    }
}
