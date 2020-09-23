using System;
using Telegram.Bot.Types;

namespace JenkinsNotifier
{
    public static class Extensions
    {
        public static string ToUserString(this User user)
        {
            return $"({user.Id}) {user.Username}";
        }
        
        public static TimeSpan ToTimespan(this long? ms)
        {
            return TimeSpan.FromMilliseconds(ms ?? 0);
        }

        public static string ToHumanReadable(this TimeSpan timeSpan)
        {
            return $"{Math.Round(timeSpan.TotalMinutes):0}m {timeSpan.Seconds}s";
        }
    }
}