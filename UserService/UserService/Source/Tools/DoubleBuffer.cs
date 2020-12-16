using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Tools.DoubleQueue
{
    class DoubleQueue<T>
    {
        /// <summary>
        /// 佇列In
        /// </summary>
        private volatile Queue<T> inQ = null;

        /// <summary>
        /// 佇列 Out
        /// </summary>
        private volatile Queue<T> outQ = null;

        /// <summary>
        /// In Queue Lock
        /// </summary>
        private static object inLock = null;

        /// <summary>
        /// 建構式
        /// </summary>
        public DoubleQueue()
        {
            inQ = new Queue<T>();
            outQ = new Queue<T>();

            inLock = new object();
        }

        /// <summary>
        /// 解構式
        /// </summary>
        ~DoubleQueue()
        {

        }

        /// <summary>
        /// 加入Queue中
        /// </summary>
        /// <param name="value"> 加入的資料 </param>
        public void Enqueue(T value)
        {
            lock (inLock)
            {
                inQ.Enqueue(value);
            }

        }

        /// <summary>
        /// 取得Main Queue
        /// </summary>
        /// <returns> Main Queue </returns>
        public Queue<T> GetMainQueue()
        {
            return inQ;
        }

        /// <summary>
        /// 切換Queue
        /// </summary>
        public void Switch()
        {
            Queue<T> tempQ = inQ;
            inQ = outQ;
            outQ = tempQ;
        }

        /// <summary>
        /// MainQueue是否為空
        /// </summary>
        /// <returns> 是否為空 </returns>
        public bool Empty()
        {
            return !inQ.Any();
        }

        /// <summary>
        /// 清除Queue
        /// </summary>
        /// <returns> 是否成功清除 </returns>
        public bool ClearDoubleQueue()
        {
            bool ret = true;

            inQ.Clear();
            outQ.Clear();

            return ret;
        }

    }
}
