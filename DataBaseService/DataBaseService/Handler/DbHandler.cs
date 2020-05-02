using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MySql.Data.MySqlClient;

namespace DataBaseService.Handler
{
    class DbHandler
    {
        private string connectStr = "";

        private MySqlConnection sql;

        public DbHandler()
        {
            /**
             * server       伺服器地址
             * port         Port號
             * database     資料庫名稱
             * user         資料庫使用者名稱
             * password     資料庫密碼
             */
            connectStr = "server=localhost;port=3306;user=root;password=123456; database=userinfo;";

            sql = new MySqlConnection(connectStr);
        }

        /**
         * 讀取Config
         */
        private bool LoadConfig()
        {
            bool bReturn = false;


            return bReturn;
        }

        /**
         * 建立連線
         */
        public bool ConnectSQL()
        {
            bool bReturn = false;

            try
            {
                // 建立連線
                sql.Open();

                bReturn = true;

                Console.WriteLine("已經建立連線");
            }
            catch (MySqlException ex)
            {
                Console.WriteLine(ex.Message);
            }


            return bReturn;
        }

        /**
         * 關閉連線
         */
        public bool DisconnectSQL()
        {
            bool bReturn = false;

            try
            {
                // 關閉連線
                sql.Close();

                bReturn = true;

                Console.WriteLine("已經關閉連線");
            }
            catch (MySqlException ex)
            {
                Console.WriteLine(ex.Message);
            }

            return bReturn;
        }

        /**
         * SQL 操作
         * 
         */
        public void SqlCommand_Process(string sqlCmd)
        {
            //lock (this)
            {
                MySqlCommand cmd = new MySqlCommand(sqlCmd, sql);

                cmd.ExecuteNonQuery();
            }

        }

        /**
         * SQL 查詢
         */
        public int SqlCommand_Search(string sqlCmd)
        {
            //lock (this)
            {
                MySqlCommand cmd = new MySqlCommand(sqlCmd, sql);


                int n = Convert.ToInt32(cmd.ExecuteScalar());

                return n;
            }
        }

    }


}
