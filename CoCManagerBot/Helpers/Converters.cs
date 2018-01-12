using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoCManagerBot.Helpers
{
    internal static class Converters
    {
        internal static DateTime GetTime(string input)
        {
            var time = DateTime.ParseExact(input, "yyyyMMddTHHmmss:fffZ", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
            return TimeZoneInfo.ConvertTime(time, TimeZoneInfo.Utc, TimeZoneInfo.Local);
        }
    }
}
