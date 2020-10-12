using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.IO;
using System.Windows.Forms;

namespace Tools
{
    class Logger
    {

        // 建構式
        public Logger()
        {
            string path = @"./Log";
            string file = @"./Log/system.txt";

            // 資料夾不存在
            if (!Directory.Exists(path))
            {
                // 建立資料夾
                Directory.CreateDirectory(path);
            }

            // 檔案不存在
            if (!File.Exists(file))
            {   
                // 建立檔案
                File.Create(file);
            }

        }

        // 解構式
        ~Logger()
        {

        }

        // 儲存Log
        public void SaveLog(string msg)
        {   
            try
            {
                Console.WriteLine(msg);

                FileStream fs = new FileStream(@"./Log/system.txt", FileMode.Append, FileAccess.Write);

                StreamWriter swWriter = new StreamWriter(fs);
                //寫入數據
                swWriter.WriteLine("{0} {1}", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), msg);
                swWriter.Close();

                fs.Close();

            }
            catch (Exception e)
            {
                throw e;
            }
            
        }
        
    }

}
