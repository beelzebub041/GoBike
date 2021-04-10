using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tools.WeekProcess
{
    class WeekProcess
    {   
        /// <summary>
        /// 建構式
        /// </summary>
        public WeekProcess()
        {

        }

        /// <summary>
        /// 解構式
        /// </summary>
        ~WeekProcess()
        {

        }

        /// <summary>
        /// 取得本周第一天(星期天)
        /// </summary>
        /// <param name="dateTime"> Date Time </param>
        /// <returns> 本周第一天 </returns>
        public string GetWeekFirstDay(DateTime dateTime)
        {
            int weekNow = Convert.ToInt32(dateTime.DayOfWeek);
            int dayDiff = (-1) * weekNow;

            return dateTime.AddDays(dayDiff).ToString("yyyy-MM-dd");
        }

        /// <summary>
        /// 取得本周最後一天(星期六)
        /// </summary>
        /// <param name="dateTime"> Date Time </param>
        /// <returns> 本周最後一天 </returns>
        public string GetWeekLastDay(DateTime dateTime)
        {
            int weekNow = Convert.ToInt32(dateTime.DayOfWeek);
            int dayDiff = (7 - weekNow) - 1;

            return dateTime.AddDays(dayDiff).ToString("yyyy-MM-dd");
        }
    }
}
