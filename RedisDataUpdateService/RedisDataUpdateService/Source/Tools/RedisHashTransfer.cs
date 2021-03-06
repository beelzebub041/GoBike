﻿using System;
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
