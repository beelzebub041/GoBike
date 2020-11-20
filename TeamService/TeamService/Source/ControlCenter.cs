using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Threading;

using Service.Interface;

using Tools;

namespace Service.Source
{
    class ControlCenter: IControlCenter
    {
        private Form1 form = null;                      // 視窗物件

        private Logger log = null;                      // Log物件

        private object logLock = new object();          // Log Lock

        private ClientHandler clientHandler = null;     // ClientHandler物件

        private MessageProcessor msgProcessor = null;   // MessageProcessor物件

        private readonly string version = "Team036";    // 版本號

        public ControlCenter(Form1 form)
        {
            this.form = form;

            log = new Logger();

            msgProcessor = new MessageProcessor(SaveLog);

            clientHandler = new ClientHandler(SaveLog, msgProcessor.MessageDispatch);

        }

        ~ControlCenter()
        {
            
        }

        // 初始化
        public bool Initialize()
        {
            SaveLog($"Ride Service Version: {version}");

            bool result = false;

            try
            {
                if (msgProcessor.Initialize() && clientHandler.Initialize())
                {
                    result = true;

                    SaveLog($"[Info] ControlCenter::Initialize, Success");
                }
                else
                {
                    SaveLog($"[Info] ControlCenter::Initialize, Fail");
                }
            }
            catch (Exception ex)
            {
                SaveLog($"[Error] ControlCenter::Initialize, Catch Error, Msg:{ex.Message}");
            }

            return result;
        }

        // 銷毀
        public bool Destroy()
        {
            bool result = true;

            return result;
        }

        // 儲存Log
        public void SaveLog(string msg)
        {
            lock (logLock)
            {
                log.SaveLog(msg);

                form.updateTextBox(msg);
            }

        }
    }
}
