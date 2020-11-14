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
        public RedisHashTransfer()
        {

        }

        // 將物件轉換成 Hash Entry Array
        public HashEntry[] TransToHashEntryArray(object hashObject)
        {
            PropertyInfo[] properties = hashObject.GetType().GetProperties();

            try
            {
                return properties.Select(property => new HashEntry(property.Name, property.GetValue(hashObject).ToString())).ToArray();
            }
            catch
            {
                Console.WriteLine($"[Error] RedisHashTransfer::TransToHashEntryArray, Transfer Error");

                return new HashEntry[1];
            }

        }
    }
}
