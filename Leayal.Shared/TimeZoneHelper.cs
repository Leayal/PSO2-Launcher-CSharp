using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Leayal.Shared
{
    public static class TimeZoneHelper
    {
        /// <summary>The reset time in local (JST time). Please note that that this is the fixed hour of the day.</summary>
        public static readonly TimeOnly DailyResetTime = new TimeOnly(4, 0);

        public static readonly TimeZoneInfo? JapanTimeZone;

        static TimeZoneHelper()
        {
            var zones = TimeZoneInfo.GetSystemTimeZones();
            foreach (var zone in zones)
            {
                if (string.Equals(zone.Id, "Tokyo Standard Time", StringComparison.OrdinalIgnoreCase) || string.Equals(zone.Id, "Japan Standard Time", StringComparison.OrdinalIgnoreCase))
                {
                    JapanTimeZone = zone;
                    break;
                }
            }
            if (JapanTimeZone == null)
            {
                foreach (var zone in zones)
                {
                    if (zone.Id.Contains("Tokyo", StringComparison.OrdinalIgnoreCase) || zone.Id.Contains("Japan", StringComparison.OrdinalIgnoreCase))
                    {
                        JapanTimeZone = zone;
                        break;
                    }
                }
            }
        }

        /// <summary>This will convert the time (local or UTC) to JST local time.</summary>
        /// <param name="dateTime">The time to convert to JST. It can be either UTC or local time. Unspecific time may yield wrong result.</param>
        /// <returns>A <seealso cref="DateTime"/> which is in JST local time.</returns>
        public static DateTime ConvertTimeToLocalJST(in DateTime dateTime)
        {
            if (JapanTimeZone is not null)
            {
                if (dateTime.Kind == DateTimeKind.Utc)
                {
                    return TimeZoneInfo.ConvertTimeFromUtc(dateTime, JapanTimeZone);
                }
                else
                {
                    return TimeZoneInfo.ConvertTime(dateTime, JapanTimeZone);
                }
            }
            else
            {
                if (dateTime.Kind == DateTimeKind.Utc)
                {
                    return dateTime.AddHours(9);
                }
                else
                {
                    return dateTime.ToUniversalTime().AddHours(9);
                }
            }
        }

        /// <summary>Adjust the <seealso cref="DateTime"/> according to the PSO2 game's daily reset.</summary>
        /// <remarks>This just simply push the date back to previous day if the time is still before 4AM.</remarks>
        /// <param name="datetime">The time to adjust. This <seealso cref="DateTime"/> must be in JST or it will definitely yield wrong result.</param>
        /// <returns>The adjusted <seealso cref="DateTime"/> if it's before daily reset. Otherwise <paramref name="datetime"/> is returned as-is.</returns>
        public static DateTime AdjustToPSO2GameResetTime(in DateTime datetime)
        {
            if (IsBeforePSO2GameResetTime(in datetime))
            {
                return datetime.AddDays(-1);
            }
            else
            {
                return datetime;
            }
        }

        /// <summary>Check whether the given time is still before daily reset.</summary>
        /// <param name="time">The time in JST to check for.</param>
        /// <returns>A boolean which will be true if the time is still before daily reset. Otherwise false.</returns>
        public static bool IsBeforePSO2GameResetTime(in DateTime datetime) => (TimeOnly.FromDateTime(datetime) < DailyResetTime);

        /// <summary>Check whether the given time is still before daily reset.</summary>
        /// <param name="time">The time in JST to check for.</param>
        /// <returns>A boolean which will be true if the time is still before daily reset. Otherwise false.</returns>
        public static bool IsBeforePSO2GameResetTime(in TimeOnly time) => (time < DailyResetTime);
    }
}
