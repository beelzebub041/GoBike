using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;
using System.Windows.Forms;

using TeamService;

namespace Tools.Logger
{
    class Logger
    {
        private Form1 fm = null;

        public Logger(Form1 fm)
        {
            this.fm = fm;

            string path = @"./Log";

            if (!Directory.Exists(path))
            {
                //新增資料夾
                Directory.CreateDirectory(path);
            }

            string file = @"./Log/system.txt";

            if (!File.Exists(file))
            {
                File.Create(file);
            }

        }

        ~Logger()
        {


        }

        public void SaveLog(string msg)
        {
            try
            {
                Console.WriteLine(msg);

                fm.updateTextBox(msg);

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
