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

using Tools;

namespace Service
{
    public partial class Form1 : Form
    {
        /// <summary>
        /// TextBox的委派函式
        /// </summary>
        /// <param name="s"></param>
        public delegate void TextBoxDalegate(string s);

        /// <summary>
        /// TextBox的委派物件
        /// </summary>
        public TextBoxDalegate tbDalegate = null;

        /// <summary>
        /// Queue Lock 
        /// </summary>
        private static object qLock = null;                  

        /// <summary>
        /// Process Queue的Tmer
        /// </summary>
        private System.Timers.Timer timer = null;

        /// <summary>
        /// Msg Queue
        /// </summary>
        private Queue<string> msgQ = null;

        /// <summary>
        /// Logger物件
        /// </summary>
        private Logger logger = null;

        /// <summary>
        /// 建構式
        /// </summary>
        public Form1()
        {
            InitializeComponent();

            // 添加Close事件
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form1_FormClosing);

            // 添加讀取完成事件
            this.Load += new EventHandler(Form1_Load);

            qLock = new object(); 

        }

        /// <summary>
        /// 解構式
        /// </summary>
        ~Form1()
        {
            if (timer != null)
            {
                timer.Stop();
                timer.Close();
            }

            ControlCenter.Instance.Destroy();
        }

        /// <summary>
        /// 初始化
        /// </summary>
        void Initialize()
        {
            this.tbDalegate = new TextBoxDalegate(UpdateTextBox);

            this.msgQ = new Queue<string>();

            this.timer = new System.Timers.Timer();
            this.timer.AutoReset = false;
            this.timer.Interval = 100;
            this.timer.Enabled = true;
            this.timer.Elapsed += ProcessQueue;

            logger = new Logger();
            logger.SetForm(this);

            ControlCenter.Instance.Initialize(logger);

        }

        /// <summary>
        /// 處理Queue中的資料
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ProcessQueue(Object sender, EventArgs e)
        {
            timer.Stop();
            
            if (msgQ.Any())
            {
                try
                {
                    this.Invoke(tbDalegate, msgQ.Dequeue());
                }
                catch (Exception ex)
                {
                    this.Invoke(tbDalegate, $"[Erorr] Form1 Timer_Elapsed, Catch Error, msg:{ex.Message}");
                }

            }

            timer.Start();
        }

        /// <summary>
        /// 新增訊息至TextBox的Queue中
        /// </summary>
        /// <param name="msg"></param>
        public void AddTextBoxQueue(string msg)
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
        /// 更新 TextBox
        /// </summary>
        /// <param name="msg"></param>
        private void UpdateTextBox(string msg)
        {
            int lineCount = this.ConsoleTextBox.GetLineFromCharIndex(ConsoleTextBox.Text.Length);

            // 大於100行
            if (lineCount > 100)
            {
                this.ConsoleTextBox.Text = this.ConsoleTextBox.Text.Remove(0, (this.ConsoleTextBox.Lines[0].Length + Environment.NewLine.Length));
            }

            this.ConsoleTextBox.Text += msg + Environment.NewLine;
            this.ConsoleTextBox.SelectionStart = this.ConsoleTextBox.Text.Length;
            this.ConsoleTextBox.ScrollToCaret();
        }

        /// <summary>
        /// 視窗關閉前的事件處理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
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

        /// <summary>
        /// 視窗讀取完畢後的事件處理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Form1_Load(object sender, EventArgs e)
        {
            Initialize();
        }
    }
}
