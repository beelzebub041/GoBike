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
         * 取得本週第一天(星期天)
         */
        public string GetWeekFirstDay(DateTime dateTime)
        {
            int weekNow = Convert.ToInt32(dateTime.DayOfWeek);
            int dayDiff = (-1) * weekNow;

            string firstDay = dateTime.AddDays(dayDiff).ToString("yyyy-MM-dd");

            return firstDay;
        }

        /**
        * 取得本週最後一天(星期六)
        */
        public string GetWeekLastDay(DateTime dateTime)
        {
            int weekNow = Convert.ToInt32(dateTime.DayOfWeek);
            int dayDiff = (7 - weekNow) - 1;

            string LastDay = dateTime.AddDays(dayDiff).ToString("yyyy-MM-dd");
            return LastDay;
        }

        /**
        * 取得上週第一天(星期天)
        */
        public string GetLastWeekFirstDay(DateTime dateTime)
        {
            int weekNow = Convert.ToInt32(dateTime.DayOfWeek);
            int dayDiff = (-1) * weekNow;

            string firstDay = dateTime.AddDays(dayDiff-7).ToString("yyyy-MM-dd");

            return firstDay;
        }

        /**
        * 取得本週最後一天(星期六)
        */
        public string GetLastWeekLastDay(DateTime dateTime)
        {
            int weekNow = Convert.ToInt32(dateTime.DayOfWeek);
            int dayDiff = (7 - weekNow) - 1;

            string LastDay = dateTime.AddDays(dayDiff-7).ToString("yyyy-MM-dd");
            return LastDay;
        }
    }
}
