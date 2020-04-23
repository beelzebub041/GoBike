using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tools.WeekProcess
{
    class WeekProcess
    {
        public WeekProcess()
        {

        }

        ~WeekProcess()
        {

        }

       /**
        * 取得本周第一天(星期天)
        */
        public string GetWeekFirstDay(DateTime dateTime)
        {
            int weekNow = Convert.ToInt32(dateTime.DayOfWeek);
            int dayDiff = (-1) * weekNow;

            return dateTime.AddDays(dayDiff).ToString("yyyy-MM-dd");
        }

        /**
        * 取得本周最後一天(星期六)
        */
        public string GetWeekLastDay(DateTime dateTime)
        {
            int weekNow = Convert.ToInt32(dateTime.DayOfWeek);
            int dayDiff = (7 - weekNow) - 1;

            return dateTime.AddDays(dayDiff).ToString("yyyy-MM-dd");
        }
    }
}
