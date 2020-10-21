using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using Service.Source;

namespace Service
{
    public partial class Form1 : Form
    {

        // 定義Dalegate(委派)方法 (參數部分需與帶入的方法所用之參數相同)
        public delegate void TextBoxDalegate(string s);

        public Form1()
        {
            InitializeComponent();

            // 添加Close事件
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form1_FormClosing);

            Initialize();
        }

        ~Form1()
        {

        }

        void Initialize()
        {
            ControlCenter ctrl = new ControlCenter(this);

            if (ctrl.Initialize())
            {

            }
            else
            {

            }
        }

        public void updateTextBox(string msg)
        {
            // 有調用需求
            if (this.InvokeRequired)
            {
                // 建立Dalegate 物件
                /*
                 建立委派物件(TextBoxDalegate)並委派使用"updateTextBox"方法
                 */
                TextBoxDalegate TBDalegate = new TextBoxDalegate(updateTextBox);

                // 調用委派的方法
                this.Invoke(TBDalegate, msg);

            }
            else
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

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            /// if the close button in the title bar is clicked
            if (e.CloseReason == CloseReason.UserClosing)
            {
                /// ask the user whether close or not
                if (MessageBox.Show("Leave?", "warning",
                        MessageBoxButtons.YesNo) ==
                    DialogResult.No)
                {
                    /// cancel the process of closing
                    e.Cancel = true;
                }
                else
                {
                    //accountService.StopProcess();
                }
            }
        }
    }
}
