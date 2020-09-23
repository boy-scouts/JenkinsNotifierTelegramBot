using System;
using System.Collections.Generic;
using System.Text;

namespace JenkinsNotifier
{
    public class CommandHandler
    {
        private static readonly Dictionary<string, ConsoleCommand> _commands = new Dictionary<string, ConsoleCommand>()
        {
            { "quit", new ConsoleCommand((_) => true, "Stop bot and quit") },
            { "help", new ConsoleCommand((_) => ListCommands(), "List commands") },
        };

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
                if (_commands.TryGetValue(cmd, out var commandAction))
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

        public static bool ListCommands()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("Available commands:\n");
            foreach (var consoleCommand in _commands)
            {
                sb.Append($"{consoleCommand.Key}: {consoleCommand.Value.Description}\n");
            }
            Console.WriteLine(sb);

            return false;
        }

        public class ConsoleCommand
        {
            public delegate bool CommandAction(string args);

            private readonly CommandAction _action;
            private readonly string _description;

            public ConsoleCommand(CommandAction action, string description)
            {
                _action = action;
                _description = description;
            }

            public string Description => _description;

            public bool Execute(string args = "")
            {
                return _action.Invoke(args);
            }
        }
    }
}