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
            //"20180423T060419.000Z"
            var format = "";

            if (input.Contains("."))
                format = "yyyyMMddTHHmmss.fffZ";
            else if (input.Contains(":"))
                format = "yyyyMMddTHHmmss:fffZ";
            else
                Console.WriteLine(input);
            var time = DateTime.ParseExact(input, format, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
            return TimeZoneInfo.ConvertTime(time, TimeZoneInfo.Utc, TimeZoneInfo.Local);
        }
    }
}
