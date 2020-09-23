using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JenkinsNET;
using JenkinsNET.Models;

namespace JenkinsNotifier
{
    public class JobsHandler : IDisposable
    {
        public event Action<string, JenkinsBuildBase> OnNewBuildStarted = (jobName, build) => { };
        public event Action OnJobsChecked = () => { };

        private bool Started { get; set; }
        
        private Dictionary<string, List<int?>> _jobsBuilds;
        private readonly JenkinsClient _client;

        public JobsHandler(string baseUrl, string userName, string apiToken)
        {
            Logger.Log("Initializing JobsHandler..");

            _client = new JenkinsClient {
                BaseUrl = baseUrl,
                UserName = userName,
                ApiToken = apiToken,
            };
            
            _jobsBuilds = new Dictionary<string, List<int?>>();
        }

        public async void StartPolling()
        {
            if(Started) return;

            Started = true;
            while (Started)
            {
                CheckJobs();
                await Task.Delay(Config.Current.CheckJobsDelayMs);
            }
        }

        public async void AbortBuild(string jobName, string buildNumber)
        {
            await Task.Delay(1);
        }

        public JenkinsBuildBase GetBuildDescription(string jobName, int? buildNumber)
        {
            var build = _client.Builds.Get<JenkinsBuildBase>(jobName, buildNumber.ToString());
            return build;
        }

        private void CheckJobs()
        {
            Logger.Log("Checking jobs");
            
            Dictionary<string, List<int?>> updateJobs = new Dictionary<string, List<int?>>();

            var jobs = _client.Get().Jobs;
            foreach (var job in jobs)
            {
                var jobDescription = _client.Jobs.Get<JenkinsFreeStyleJob>(job.Name);
                updateJobs.Add(job.Name, jobDescription.Builds.Select(x => x.Number).ToList());
            }

            if (_jobsBuilds.Keys.Count != 0)
            {
                foreach (var jobsBuild in _jobsBuilds)
                {
                    var jobName = jobsBuild.Key;
                    if (updateJobs.TryGetValue(jobName, out var updateBuilds))
                    {
                        var currentBuilds = _jobsBuilds[jobName];
                        var newBuilds = updateBuilds
                            .FindAll(x => !currentBuilds.Exists(b => b == x));

                        foreach (var startedBuild in newBuilds)
                        {
                            var buildDescription = GetBuildDescription(jobName, startedBuild);
                            OnNewBuildStarted?.Invoke(jobName, buildDescription);
                        }
                    }
                    else
                    {
                        Logger.Log($"JOB {jobName} WAS DELETED!");
                    }
                }
            }

            _jobsBuilds = updateJobs;
            
            OnJobsChecked?.Invoke();
        }
        
        public void Dispose()
        {
            Started = false;
        }
    }
}