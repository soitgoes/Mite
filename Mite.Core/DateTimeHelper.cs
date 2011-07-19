using System;

namespace Mite.Core
{
    public static class DateTimeHelper
    {
        public static string ToIso(DateTime input)
        {
            return input.ToString("yyyy-MM-dd") + "T" + input.ToString("mm-dd-hh") + "Z";
        }
    }
}