using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Runtime.InteropServices;
using System.Net;
using System.IO;
using System.Web.Script.Serialization;

namespace Tools.NotifyMessage
{
    class NotifyMessage
    {
        private Logger.Logger log = null;

        // ============================================ //
        string serverKey = "";

        string senderId = "";

        [DllImport("kernel32")]
        private static extern int GetPrivateProfileString(string section, string key, string def, StringBuilder retVal, int size, string filePath);

        public NotifyMessage(Logger.Logger log)
        {
            this.log = log;
        }

        ~NotifyMessage()
        {

        }

        /**
         * 初始化
         */
        public bool Initialize()
        {
            bool bReturn = true;

            try
            {
                if (LoadingConfig())
                {
                    bReturn = true;

                    log.SaveLog($"[Info] NotifyMessage::Initialize, Initialize Success");
                }
                else
                {
                    log.SaveLog("[Error] NotifyMessage::Initialize, LoadingConfig Fail");
                }

            }
            catch (Exception ex)
            {
                log.SaveLog($"[Error] NotifyMessage::Initialize, Catch Error Msg:{ex.Message}");

            }

            return bReturn;
        }

        /**
         * 讀取設定檔
         */
        private bool LoadingConfig()
        {
            bool bReturn = true;

            try
            {
                string configPath = @"./Config/NotifySetting.ini";

                StringBuilder temp = new StringBuilder(1024);

                // Server Key
                if (bReturn && GetPrivateProfileString("CONNECT", "ServerKey", "", temp, 1024, configPath) > 0)
                {
                    serverKey = temp.ToString();
                }
                else
                {
                    bReturn = false;
                }

                // Sender Id
                if (bReturn && GetPrivateProfileString("CONNECT", "SenderId", "", temp, 1024, configPath) > 0)
                {
                    senderId = temp.ToString();
                }
                else
                {
                    bReturn = false;
                }

            }
            catch
            {
                bReturn = false;

                log.SaveLog($"[Error] NotifyMessage::LoadConfig, Config Parameter Error");
            }

            return bReturn;
        }

        /**
         * 推播訊息至裝置
         */
        public void NotifyMsgToDevice(string deviceId, string tit, string msg)
        {
            if (serverKey != "" && senderId != "")
            {
                try
                {
                    WebRequest tRequest = WebRequest.Create("https://fcm.googleapis.com/fcm/send");
                    tRequest.Method = "post";
                    tRequest.ContentType = "application/json";
                    var data = new
                    {
                        to = deviceId,
                        notification = new
                        {
                            body = msg,
                            title = tit,
                            sound = "Enabled"

                        }
                    };
                    var serializer = new JavaScriptSerializer();
                    var json = serializer.Serialize(data);
                    Byte[] byteArray = Encoding.UTF8.GetBytes(json);
                    tRequest.Headers.Add(string.Format("Authorization: key={0}", serverKey));
                    tRequest.Headers.Add(string.Format("Sender: id={0}", senderId));
                    tRequest.ContentLength = byteArray.Length;
                    using (Stream dataStream = tRequest.GetRequestStream())
                    {
                        dataStream.Write(byteArray, 0, byteArray.Length);
                        using (WebResponse tResponse = tRequest.GetResponse())
                        {
                            using (Stream dataStreamResponse = tResponse.GetResponseStream())
                            {
                                using (StreamReader tReader = new StreamReader(dataStreamResponse))
                                {
                                    String sResponseFromServer = tReader.ReadToEnd();
                                    string str = sResponseFromServer;
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    log.SaveLog($"[Error] NotifyMessage::SendToDevice, Catch Error Msg: {ex.Message}");
                }

            }
            else
            {
                log.SaveLog($"[Error] NotifyMessage::SendToDevice, serverKey or senderId is Empty String");
            }

        }
    }
}
