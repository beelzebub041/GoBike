using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using Tools.Logger;

namespace AccountService.Main
{
    public partial class Form1 : Form
    {
        private AccountService accountService = null;

        private Logger log = null;


        public Form1()
        {
            InitializeComponent();

            // 添加Close事件
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form1_FormClosing);

            accountService = new AccountService();

            log = new Logger(ConsoleTextBox);

            if (accountService.Initialize())
            {
                log.saveLog("[Info][Error] Form1: Login Server Initialize Success");
            }
            else 
            {
                log.saveLog("[Info][Error] Form1: Login Server Initialize Fail");
            }
        }

        ~Form1()
        {
           if (accountService != null)
           {
                accountService.StopProcess();
           }

            if (log != null)
            {
                accountService.StopProcess();
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
                    accountService.StopProcess();
                }
            }
        }
    }
}
