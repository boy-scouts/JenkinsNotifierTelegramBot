using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace JenkinsNotifier
{
    public class Commands
    {
        [SuppressMessage("ReSharper", "StringLiteralTypo")] 
        public static readonly Dictionary<string, ConsoleCommand> All = new Dictionary<string, ConsoleCommand>()
        {
            { "quit", new ConsoleCommand((_) => true, "Stop bot and quit") },
            { "help", new ConsoleCommand((_) => ListCommands(), "List commands") },
            { "addchat", new ConsoleCommand(AddChat, "Add chat. Syntax: \"addchat [chat id]\"") },
            { "rmchat", new ConsoleCommand(RemoveChat, "Remove chat. Syntax: \"rmchat [chat id]\"") },
        };

        private static bool ListCommands()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("Available commands:\n");
            foreach (var consoleCommand in All)
            {
                sb.Append($"{consoleCommand.Key}: {consoleCommand.Value.Description}\n");
            }
            Console.WriteLine(sb);

            return false;
        }

        private static bool AddChat(string chatIdString)
        {
            if (long.TryParse(chatIdString, out var chatId))
            {
                Program.TryAddChatId(chatId);
            }
            else
            {
                Logger.Log("Can't parse chat id");
            }

            return false;
        }

        private static bool RemoveChat(string chatIdString)
        {
            if (long.TryParse(chatIdString, out var chatId))
            {
                Program.TryRemoveChatId(chatId);
            }
            else
            {
                Logger.Log("Can't parse chat id");
            }
            
            return false;
        }
    }
}