// using NodaTime;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace LM_Service_Manager.Utils
{
    public static class TimeUtil
    {
        public static string UtcToLocalDateString(DateTimeOffset dt)
        {
            return dt.ToLocalTime().ToString("yyyy-MM-dd 00:00:00");
        }

        public static string UtcToLocalDateTimeString(DateTimeOffset dt)
        {
            return dt.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");
        }
        /// <summary>
        /// 获取当前月的第一天和最后一天
        /// </summary>
        /// <returns></returns>
        public static string[] MonthFirstLastDAY() => new[] { DateTime.Now.ToString("yyyy-MM-1 00:00:00"), DateTime.Now.AddMonths(1).ToString("yyyy-MM-1 00:00:00") };

        /// <summary>
        /// 获取当前时间是第几周
        /// </summary>
        /// <returns></returns>
        public static string GetTheWeekNum(DateTime dt)
        {

            //创建公历日历对象
            GregorianCalendar gregorianCalendar = new GregorianCalendar();

            //获取指定日期是周数 CalendarWeekRule指定 第一周开始于该年的第一天，DayOfWeek指定每周第一天是星期几
            int weekOfYear = gregorianCalendar.GetWeekOfYear(dt, CalendarWeekRule.FirstDay, DayOfWeek.Monday);

            return weekOfYear.ToString();

        }

        /// <summary>
        /// 获取搜索日期
        /// </summary>
        /// <returns></returns>
        public static string GetDateSearch()
        {
            DateTime now = DateTime.Now;
            var day = int.Parse(now.DayOfWeek.ToString("d"));
            day = day == 0 ? 7 : day;
            var monday = GetMonday(now);
            return $"{monday:yyyy-MM-dd}~{monday.AddDays(6):yyyy-MM-dd}";
        }

        /// <summary>
        /// 获取周一
        /// </summary>
        /// <returns></returns>
        public static DateTime GetMonday(DateTime now)
        {
            var day = int.Parse(now.DayOfWeek.ToString("d"));
            day = day == 0 ? 7 : day;
            return now.AddDays(1 - day);
        }

        /// <summary>
        /// 根据指定时间段计算工作日天数
        /// </summary>
        /// <param name="firstDay"></param>
        /// <param name="lastDay"></param>
        /// <param name="bankHolidays"></param>
        /// <returns></returns>
        public static async Task<int> BusinessDaysUntilAsync(DateTime firstDay, DateTime lastDay)
        {
            firstDay = firstDay.Date;
            lastDay = lastDay.Date;
            if (firstDay > lastDay)
                throw new ArgumentException("最后一天不正确" + lastDay);

            TimeSpan span = lastDay - firstDay;
            int businessDays = span.Days + 1;
            int fullWeekCount = businessDays / 7;
            if (businessDays > fullWeekCount * 7)
            {
                int firstDayOfWeek = firstDay.DayOfWeek == DayOfWeek.Sunday ? 7 : (int)firstDay.DayOfWeek;
                int lastDayOfWeek = lastDay.DayOfWeek == DayOfWeek.Sunday ? 7 : (int)lastDay.DayOfWeek;

                if (lastDayOfWeek < firstDayOfWeek)
                    lastDayOfWeek += 7;
                if (firstDayOfWeek <= 6)
                {
                    if (lastDayOfWeek >= 7)
                        businessDays -= 2;
                    else if (lastDayOfWeek >= 6)
                        businessDays -= 1;
                }
                else if (firstDayOfWeek <= 7 && lastDayOfWeek >= 7)
                    businessDays -= 1;
            }

            businessDays -= fullWeekCount + fullWeekCount;
            var bankHolidays = await getHolidayOfYearAsync(firstDay.Year.ToString());
            foreach (var bankHoliday in bankHolidays)
            {
                DateTime bh = bankHoliday.Date;
                if (firstDay <= bh && bh <= lastDay)
                    _ = bankHoliday.IsOffDay ? --businessDays : ++businessDays;
            }
            if (firstDay.Year != lastDay.Year)
            {
                bankHolidays = await getHolidayOfYearAsync(lastDay.Year.ToString());
                foreach (var bankHoliday in bankHolidays)
                {
                    DateTime bh = bankHoliday.Date;
                    if (firstDay <= bh && bh <= lastDay)
                        _ = bankHoliday.IsOffDay ? --businessDays : ++businessDays;
                }
            }

            return businessDays;
        }

        /// <summary>
        /// 判断当前日期是否为休息日
        /// </summary>
        /// <param name="dt"></param>
        /// <returns></returns>
        public static async Task<bool> IsHoliday(this DateTime dt)
        {
            var bankHolidays = await getHolidayOfYearAsync(dt.Year.ToString());
            foreach (var item in bankHolidays)
            {
                if (dt.Date == item.Date)
                {
                    return item.IsOffDay;
                }
            }
            if ((int)dt.DayOfWeek > 0 && (int)dt.DayOfWeek < 6)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        /// <summary>
        /// 获取指定年份节假日信息
        /// </summary>
        /// <param name="year"></param>
        /// <returns></returns>
        public static async Task<List<Holiday>> getHolidayOfYearAsync(String year)
        {
            String url = "https://cdn.jsdelivr.net/gh/NateScarlet/holiday-cn@master/" + year + ".json";
            String json = await HttpHelper.GetJsonAsyncForCUrl(url);
            JObject jobj = JObject.Parse(json);
            JArray jArray = (JArray)jobj.GetValue("days");
            var holidays = JsonConvert.DeserializeObject<List<Holiday>>(jArray.ToString());
            return holidays;
        }

        public class Holiday
        {
            public string Name { get; set; }
            public DateTime Date { get; set; }
            public bool IsOffDay { get; set; }
        }
    }
}
