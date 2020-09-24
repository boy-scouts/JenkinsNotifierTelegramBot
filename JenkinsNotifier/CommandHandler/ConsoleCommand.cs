namespace JenkinsNotifier
{
    public class ConsoleCommand
    {
        public delegate bool CommandAction(string args);

        private readonly CommandAction _action;

        public ConsoleCommand(CommandAction action, string description)
        {
            _action = action;
            Description = description;
        }

        public string Description { get; }

        public bool Execute(string args = "")
        {
            return _action.Invoke(args);
        }
    }
}