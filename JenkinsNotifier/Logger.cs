using System;
using System.IO;

namespace JenkinsNotifier
{
    public static class Logger
    {
        static string LogPath = "bot.log";

        public static void Log(object o)
        {
            Console.WriteLine(o);
            AppendWithDateToFile(o);
        }

        public static void LogException(Exception e)
        {
            Log($"Exception occured: {e}");
        }
        
        public static void AppendWithDateToFile(object o)
        {
            AppendToFile($"{DateTime.Now:G}: {o}\n");
        }

        private static void AppendToFile(string text)
        {
            File.AppendAllText(LogPath, text);
        }
    }
}