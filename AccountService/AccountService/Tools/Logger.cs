using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;
using System.Windows.Forms;


namespace Tools.Logger
{
    class Logger
    {

        private TextBox ConsoleTextBox;

        public Logger(TextBox textBox)
        {
            ConsoleTextBox = textBox;

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

        public void saveLog(string msg)
        {
            try
            {
                updateTextBox(msg);

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

        private void updateTextBox(string msg)
        {
            int lineCount = ConsoleTextBox.GetLineFromCharIndex(ConsoleTextBox.Text.Length);

            if (lineCount >= 100)
            {
                ConsoleTextBox.Text = ConsoleTextBox.Text.Remove(0, (ConsoleTextBox.Lines[0].Length + Environment.NewLine.Length));
            }

            ConsoleTextBox.Text += msg;
            ConsoleTextBox.Text += Environment.NewLine;
            ConsoleTextBox.SelectionStart = ConsoleTextBox.Text.Length;
            ConsoleTextBox.ScrollToCaret();

        }
    }


}
