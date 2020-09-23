using System;

namespace JenkinsNotifier
{
    public static class DateTimeExtensions
    {
        static readonly DateTime EpochStart = new DateTime(1970, 1, 1, 0, 0, 0);

        public static long ToUnixTimestamp(this DateTime d)
        {
            var epoch = d - EpochStart;

            return (long)epoch.TotalSeconds;
        }

        public static DateTime FromUnixTimestamp(this long t)
        {
            var date = EpochStart + TimeSpan.FromSeconds(t);
            
            return date;
        }
        
        public static long ToUnixTimestampMs(this DateTime d)
        {
            var epoch = d - EpochStart;

            return (long)epoch.TotalMilliseconds;
        }

        public static DateTime FromUnixTimestampMs(this long t)
        {
            var date = EpochStart + TimeSpan.FromMilliseconds(t);
            
            return date;
        }
    }
}