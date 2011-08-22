using System;

namespace Mite.Core
{
    public static class DateTimeHelper
    {
        public static string ToIso(this DateTime input)
        {
            return input.ToUniversalTime().ToString("yyyy-MM-dd") + "T" + input.ToUniversalTime().ToString("hh-mm-ss") + "Z";
        }
    }
}