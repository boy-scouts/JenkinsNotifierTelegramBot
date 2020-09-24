namespace JenkinsNotifier
{
    public partial class JobProgressData
    {
        public Executor Executor { get; set; }
    }

    public partial class Executor
    {
        public long Progress { get; set; }
    }
}