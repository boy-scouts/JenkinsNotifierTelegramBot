using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JenkinsNET;
using JenkinsNET.Models;

namespace JenkinsNotifier
{
    public class JobsHandler
    {
        private static int CheckDelayMs => 5000;
        
        public static event Action<string, JenkinsBuildDescription> OnNewBuildStarted = (jobName, build) => { };
        public static event Action<string, JenkinsBuildDescription> OnBuildUpdated = (jobName, build) => { };
        public static event Action<string, JenkinsBuildDescription> OnBuildCompleted = (jobName, build) => { };
        public static event Action<string, JenkinsBuildDescription> OnBuildDeleted = (jobName, build) => { };

        public static bool Started { get; private set; }
        
        private static Dictionary<string, List<JenkinsBuildDescription>> _jobsBuilds = new Dictionary<string, List<JenkinsBuildDescription>>();
        private static JenkinsClient Client;
       
        
        public static void Initialize()
        {
            Client = new JenkinsClient {
                BaseUrl = "http://192.168.0.252:8080/",
                UserName = "admin",
                ApiToken = "11aa6eb88577c4e8fd7a9b6a1b5c5ad279",
            };
            
            _jobsBuilds = new Dictionary<string, List<JenkinsBuildDescription>>();
            
            CheckJobsLoop();
        }

        public static void DeInitialize()
        {
            Started = false;
        }

        private static async void CheckJobsLoop()
        {
            Started = true;
            while (Started)
            {
                CheckJobs();
                await Task.Delay(CheckDelayMs);
            }
        }

        private static void CheckJobs()
        {
            Dictionary<string, List<JenkinsBuildDescription>> newJobs = new Dictionary<string, List<JenkinsBuildDescription>>();

            var jobs = Client.Get().Jobs;
            foreach (var job in jobs)
            {
                var jobDescription = Client.Jobs.Get<JenkinsFreeStyleJob>(job.Name);
                newJobs.Add(job.Name, jobDescription.Builds.ToList());
            }

            if (_jobsBuilds.Keys.Count != 0)
            {
                foreach (var jobsBuild in _jobsBuilds)
                {
                    var jobName = jobsBuild.Key;
                    if (newJobs.TryGetValue(jobName, out var newBuilds))
                    {
                        var currentBuilds = _jobsBuilds[jobName];
                        var startedBuilds = newBuilds
                            .FindAll(x => !currentBuilds.Exists(b => b.Number == x.Number));


                        foreach (var startedBuild in startedBuilds)
                        {
                            _jobsBuilds[jobName].Add(startedBuild);
                            OnNewBuildStarted?.Invoke(jobName, startedBuild);
                        }

                        foreach (var newBuild in newBuilds)
                        {
                            OnBuildUpdated(jobName, newBuild);
                        }

                        foreach (var newBuild in newBuilds)
                        {
                            var currentBuild = _jobsBuilds[jobName].Find(x => x.Number == newBuild.Number);
                            if (currentBuild != null)
                            {
                                
                            }
                            else
                            {
                                // TODO: Handle deletion
                            }
                        }
                    }
                }
            }
            
            _jobsBuilds = newJobs;
        }
    }
}