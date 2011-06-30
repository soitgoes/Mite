using System;

namespace Mite.Core
{
    public static class DateTimeExtensionMethods
    {
        public static string ToIso(this DateTime input)
        {
            return input.ToString("yyyy-MM-dd") + "T" + input.ToString("mm-dd-hh") + "Z";
        }
    }
}