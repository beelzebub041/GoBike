using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using StackExchange.Redis;
using System.Reflection;

namespace Tools.RedisHashTransfer
{
    class RedisHashTransfer
    {
        /// <summary>
        /// 建構式
        /// </summary>
        public RedisHashTransfer()
        {

        }

        /// <summary>
        /// 解構式
        /// </summary>
        ~RedisHashTransfer()
        {

        }

        /// <summary>
        /// 將物件轉換成 Hash Entry Array
        /// </summary>
        /// <param name="hashObject"> hash Object </param>
        /// <returns> Hash Entry Array </returns>
        public HashEntry[] TransToHashEntryArray(object hashObject)
        {
            PropertyInfo[] properties = hashObject.GetType().GetProperties();

            int count = 0;

            HashEntry[] hashEntryList = new HashEntry[properties.Length];

            foreach (PropertyInfo property in properties)
            {
                string value = "";

                if (property.GetValue(hashObject) != null)
                {
                    value = property.GetValue(hashObject).ToString();
                }

                hashEntryList.SetValue(new HashEntry(property.Name, value), count);

                count++;
            }

            return hashEntryList;

        }
    }
}
