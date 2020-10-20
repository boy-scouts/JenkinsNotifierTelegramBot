using System;
using System.Diagnostics;

namespace JenkinsNotifier
{
    public static class Utilities
    {
        public static string Base64Encode(this string plainText) {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            return System.Convert.ToBase64String(plainTextBytes);
        }

        public static string Base64Decode(this string base64EncodedData) {
            var base64EncodedBytes = System.Convert.FromBase64String(base64EncodedData);
            return System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
        }

        public static void OpenWithDefaultProgram(string path)
        {
            Process opener = new Process {StartInfo = {FileName = "explorer", Arguments = "\"" + path + "\""}};
            opener.Start();
        }
    }
}