using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Runtime.InteropServices;
using System.Net;
using System.IO;
using System.Web.Script.Serialization;

using Tools;

namespace Tools.NotifyMessage
{
    class NotifyMessage
    {
        /// <summary>
        /// 推播 Server的 Key
        /// </summary>
        private string serverKey = "";

        /// <summary>
        /// Sender ID
        /// </summary>
        private string senderId = "";

        /// <summary>
        /// Logger 物件
        /// </summary>
        private Tools.Logger logger = null;

        [DllImport("kernel32")]
        private static extern int GetPrivateProfileString(string section, string key, string def, StringBuilder retVal, int size, string filePath);

        /// <summary>
        /// 建構式
        /// </summary>
        public NotifyMessage()
        {

        }

        ~NotifyMessage()
        {

        }

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="logger"> Logger物件 </param>
        /// <returns> 是否成功初始化 </returns>
        public bool Initialize(Logger logger)
        {
            bool bReturn = true;

            try
            {
                this.logger = logger;

                if (LoadingConfig())
                {
                    bReturn = true;

                    SaveLog($"[Info] NotifyMessage::Initialize, Initialize Success");
                }
                else
                {
                    SaveLog("[Error] NotifyMessage::Initialize, LoadingConfig Fail");
                }

            }
            catch (Exception ex)
            {
                SaveLog($"[Error] NotifyMessage::Initialize, Catch Error Msg:{ex.Message}");

            }

            return bReturn;
        }

        /// <summary>
        /// 讀取設定檔
        /// </summary>
        /// <returns> 是否成功讀取 </returns>
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

                SaveLog($"[Error] NotifyMessage::LoadConfig, Config Parameter Error");
            }

            return bReturn;
        }

        /// <summary>
        /// 儲存Log
        /// </summary>
        /// <param name="msg"> 訊息 </param>
        public void SaveLog(string msg)
        {
            if (logger != null)
            {
                logger.AddLog(msg);
            }
        }

        /// <summary>
        /// 推播訊息至裝置
        /// </summary>
        /// <param name="deviceId"> 裝置 Token </param>
        /// <param name="tit"> 標題 </param>
        /// <param name="msg"> 訊息 </param>
        public void NotifyMsgToDevice(string deviceId, string tit, string msg, int id)
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
                        },
                        data = new
                        {
                            id = Convert.ToString(id)
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
                    SaveLog($"[Error] NotifyMessage::SendToDevice, Catch Error Msg: {ex.Message}");
                }

            }
            else
            {
                SaveLog($"[Error] NotifyMessage::SendToDevice, serverKey or senderId is Empty String");
            }

        }
    }
}
