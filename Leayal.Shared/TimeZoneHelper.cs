using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Leayal.Shared
{
    public static class TimeZoneHelper
    {
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

        public static DateTime ConvertTimeToCustom(in DateTime dateTime)
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
    }
}
