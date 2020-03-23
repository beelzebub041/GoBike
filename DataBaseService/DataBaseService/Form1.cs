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
using DataBaseService;

using System.Diagnostics;

namespace DataBaseService.Main
{
    public partial class Form1 : Form
    {
        private DataBaseService dbh = null;

        private Logger log = null;


        public Form1()
        {
            InitializeComponent();

            // 添加Close事件
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form1_FormClosing);

            log = new Logger(ConsoleTextBox);

            dbh = new DataBaseService(log);


            if (dbh.Initialize())
            {
                log.saveLog("[Info] Form1: Data Base Handler Initialize Success");
            }
            else
            {
                log.saveLog("[Info][Error] Form1: Data Base Handler Initialize Fail");
            }

        }

        ~Form1()
        {
            if (dbh != null)
            {
                // TODO 刪除物件

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
                    dbh.StopProcess();
                }
            }
        }

    }
}
