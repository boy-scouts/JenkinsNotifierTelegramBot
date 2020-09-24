using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace JenkinsNotifier
{
    public class CommandHandler
    {
        public static bool HandleCommand()
        {
            var input = Console.ReadLine();
            Logger.Log($">> {input}");

            try
            {
                return HandleCommandInput(input);
            }
            catch (Exception e)
            {
                Logger.Log($"Exception while handling command:");
                Logger.Log(e);
            }

            return false;
        }

        private static bool HandleCommandInput(string input)
        {
            var split = input?.Split(' ', 2);
            var cmd = input;
            if (split != null && split.Length > 0)
            {
                cmd = split[0];
                if (Commands.All.TryGetValue(cmd, out var commandAction))
                {
                    var args = split.Length > 1 ? split[1] : string.Empty;
                    return commandAction.Execute(args);
                }
            }

            if (!string.IsNullOrEmpty(cmd))
            {
                Logger.Log($"Command {cmd} not found");
            }

            return false;
        }
    }
}