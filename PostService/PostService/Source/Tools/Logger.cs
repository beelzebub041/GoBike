using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Timers;

using System.IO;
using System.Threading;

using Service;

namespace Tools
{
    class Logger
    {
        /// <summary>
        /// 訊息佇列
        /// </summary>
        private volatile Queue<string> msgQ = null;

        /// <summary>
        /// 儲存用的Timer
        /// </summary>
        private System.Timers.Timer saveTimer = null;

        /// <summary>
        /// Log文件的位置
        /// </summary>
        private string filePath = @"./Log/system.txt";

        /// <summary>
        /// Queue Lock
        /// </summary>
        private static object qLock = null;

        /// <summary>
        /// 視窗物件
        /// </summary>
        private Form1 form = null;

        /// <summary>
        /// 建構式
        /// </summary>
        public Logger()
        {
            qLock = new object();

            this.msgQ = new Queue<string>();

            this.saveTimer = new System.Timers.Timer();
            this.saveTimer.AutoReset = false;
            this.saveTimer.Interval = 100;
            this.saveTimer.Enabled = true;
            this.saveTimer.Elapsed += this.SaveLog;
            saveTimer.Start();

            string folder = @"./Log";

            // 資料夾不存在
            if (!Directory.Exists(folder))
            {
                // 建立資料夾
                Directory.CreateDirectory(folder);
            }

            // 檔案不存在
            if (!File.Exists(filePath))
            {   
                // 建立檔案
                File.Create(filePath);
            }

        }
        public void SetForm(Form1 form)
        {
            this.form = form;
        }

        /// <summary>
        /// 解構式
        /// </summary>
        ~Logger()
        {
            if (saveTimer != null)
            {
                saveTimer.Stop();
                saveTimer.Close();
            }
        }

        /// <summary>
        /// 將訊息加到Queue裡
        /// </summary>
        /// <param name="msg"></param>
        public void AddLog(string msg)
        {
            lock (qLock)
            {
                if (msgQ != null)
                {
                    msgQ.Enqueue(msg);
                }
            }
        }
        
        /// <summary>
        /// Timer 觸發事件, 儲存Msg
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SaveLog(Object sender, EventArgs e)
        {
            saveTimer.Stop();

            try
            {
                if (msgQ.Any())
                {
                    FileStream fs = new FileStream(filePath, FileMode.Append, FileAccess.Write);

                    for (int idx = 0; idx < msgQ.Count; idx++)
                    {
                        string msg = msgQ.Dequeue();

                        Console.WriteLine(msg);

                        if (form != null)
                        {
                            form.AddTextBoxQueue(msg);
                        }

                        StreamWriter swWriter = new StreamWriter(fs);
                        //寫入數據
                        swWriter.WriteLine("{0} {1}", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), msg);
                        swWriter.Close();
                    }

                    fs.Close();

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Error] Logger TimeElapsed Catch Error, Msg:{ex.Message}");
            }

            saveTimer.Start();

        }


    }

}
