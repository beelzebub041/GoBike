using System;
using System.Data;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;


namespace DataBaseHandler.src.DataBaseHandler
{
    class SqlOperator
    {
        /**
         * 資料庫連線字串
         * 
         * Data Source          伺服器地址 (本地端 = Local)
         * Initial Catalog      資料庫名稱
         * User ID              資料庫使用者名稱
         * Password             資料庫密碼
         */
        private const string connectionString = "Data Source=localhost;Pooling=False;Max Pool Size = 1024;Initial Catalog=GoBike;Persist Security Info=True;User ID=root;Password=123456";



        /// <summary>
        /// sqlHelp 的摘要說明:資料庫訪問助手類
        /// sqlHelper是從DAAB中提出的一個類，在這裡進行了簡化，DAAB是微軟Enterprise Library的一部分，該庫包含了大量大型應用程式
        /// 開發需要使用的庫類。
        /// </summary>


        public SqlOperator()
        {
            //無參建構函式
        }

        public static SqlConnection conn;

        //開啟資料庫連線
        public static void OpenConn()
        {
            string SqlCon = connectionString;//資料庫連線字串
            conn = new SqlConnection(SqlCon);
            if (conn.State.ToString().ToLower() == "open")
            {

            }
            else
            {
                conn.Open();
            }
        }

        //關閉資料庫連線
        public static void CloseConn()
        {
            if (conn.State.ToString().ToLower() == "open")
            {
                //連線開啟時
                conn.Close();
                conn.Dispose();
            }
        }


        // 讀取資料
        public static SqlDataReader GetDataReaderValue(string sql)
        {
            OpenConn();
            SqlCommand cmd = new SqlCommand(sql, conn);
            SqlDataReader dr = cmd.ExecuteReader();
            CloseConn();
            return dr;
        }


        // 返回DataSet
        public DataSet GetDataSetValue(string sql, string tableName)
        {
            OpenConn();
            SqlDataAdapter da;
            DataSet ds = new DataSet();
            da = new SqlDataAdapter(sql, conn);
            da.Fill(ds, tableName);
            CloseConn();
            return ds;
        }

        //  返回DataView
        public DataView GetDataViewValue(string sql)
        {
            OpenConn();
            SqlDataAdapter da;
            DataSet ds = new DataSet();
            da = new SqlDataAdapter(sql, conn);
            da.Fill(ds, "temp");
            CloseConn();
            return ds.Tables[0].DefaultView;
        }

        // 返回DataTable
        public DataTable GetDataTableValue(string sql)
        {
            OpenConn();
            DataTable dt = new DataTable();
            SqlDataAdapter da = new SqlDataAdapter(sql, conn);
            da.Fill(dt);
            CloseConn();
            return dt;
        }

        // 執行一個SQL操作:新增、刪除、更新操作
        public void ExecuteNonQuery(string sql)
        {
            OpenConn();
            SqlCommand cmd;
            cmd = new SqlCommand(sql, conn);
            cmd.ExecuteNonQuery();
            cmd.Dispose();
            CloseConn();
        }

        // 執行一個SQL操作:新增、刪除、更新操作，返回受影響的行
        public int ExecuteNonQueryCount(string sql)
        {
            OpenConn();
            SqlCommand cmd;
            cmd = new SqlCommand(sql, conn);
            int value = cmd.ExecuteNonQuery();
            return value;
        }

        //執行一條返回第一條記錄第一列的SqlCommand命令
        public object ExecuteScalar(string sql)
        {
            OpenConn();
            SqlCommand cmd;
            cmd = new SqlCommand(sql, conn);
            object value = cmd.ExecuteScalar();
            return value;
        }

        // 返回記錄數
        public int SqlServerRecordCount(string sql)
        {
            OpenConn();
            SqlCommand cmd = new SqlCommand();
            cmd.CommandText = sql;
            cmd.Connection = conn;
            SqlDataReader dr;
            dr = cmd.ExecuteReader();
            int RecordCount = 0;
            while (dr.Read())
            {
                RecordCount = RecordCount + 1;
            }
            CloseConn();
            return RecordCount;
        }


        ///<summary>
        ///一些常用的函式
        ///</summary>

        //判斷是否為數字
        public static bool IsNumber(string a)
        {
            if (string.IsNullOrEmpty(a))
                return false;
            foreach (char c in a)
            {
                if (!char.IsDigit(c))
                    return false;
            }
            return true;
        }

        // 過濾非法字元
        public static string GetSafeValue(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;
            value = Regex.Replace(value, @";", string.Empty);
            value = Regex.Replace(value, @"'", string.Empty);
            value = Regex.Replace(value, @"&", string.Empty);
            value = Regex.Replace(value, @"%20", string.Empty);
            value = Regex.Replace(value, @"--", string.Empty);
            value = Regex.Replace(value, @"==", string.Empty);
            value = Regex.Replace(value, @"<", string.Empty);
            value = Regex.Replace(value, @">", string.Empty);
            value = Regex.Replace(value, @"%", string.Empty);
            return value;
        }



    }
}
