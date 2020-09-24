using System;
using System.IO;
using System.Threading;

namespace JenkinsNotifier
{
    public static class Logger
    {
        static string LogPath = "bot.log";
        private static SemaphoreSlim SemaphoreSlim = new SemaphoreSlim(1, 1);
        
        public static async void Log(object o)
        {
            await SemaphoreSlim.WaitAsync();
            
            Console.WriteLine(o);
            AppendWithDateToFile(o);

            SemaphoreSlim.Release();
        }

        public static void LogException(Exception e)
        {
            Log($"Exception occured: {e}");
        }
        
        public static void AppendWithDateToFile(object o)
        {
            AppendToFile($"[{DateTime.Now:yyyy/MM/dd hh:mm:ss}] {o}\n");
        }

        private static void AppendToFile(string text)
        {
            File.AppendAllText(LogPath, text);
        }
    }
}