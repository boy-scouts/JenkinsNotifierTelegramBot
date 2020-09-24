using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using JenkinsNET;
using JenkinsNET.Models;
using Newtonsoft.Json;

namespace JenkinsNotifier
{
    public class JobsHandler : IDisposable
    {
        public event Action<string, JenkinsBuildWithProgress> OnNewBuildStarted = (jobName, build) => { };
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

        public async Task<long> GetBuildProgress(string jobName, string buildNumber)
        {
            string url = _client.BaseUrl + $"job/{jobName}/{buildNumber}/api/json?tree=executor[progress]";
            var responseContent = await SendPostRequest(url);
            try
            {
                var jobProgressData = JsonConvert.DeserializeObject<JobProgressData>(responseContent);
                if (jobProgressData.Executor != null)
                {
                    return jobProgressData.Executor.Progress;
                }
            }
            catch (Exception e)
            {
                Logger.LogException(e);
            }

            return 0;
        }

        public async void AbortBuild(string jobName, string buildNumber)
        {
            string url = _client.BaseUrl + $"job/{jobName}/{buildNumber}/stop";
            var responseContent = await SendPostRequest(url);
        }

        private async Task<string> SendPostRequest(string url)
        {
            var httpRequestMessage = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri(url),
                Headers =
                {
                    {
                        HttpRequestHeader.Authorization.ToString(),
                        "Basic " + $"{_client.UserName}:{_client.ApiToken}".Base64Encode()
                    },
                    {"X-Version", "1"}
                },
                Content = new StringContent(string.Empty)
            };

            HttpResponseMessage response = await WebRequestHelper.Client.SendAsync(httpRequestMessage);
            var responseContent = await response.Content.ReadAsStringAsync();
            return responseContent;
        }

        public async Task<JenkinsBuildWithProgress> GetBuildDescription(string jobName, int? buildNumber)
        {
            var build = _client.Builds.Get<JenkinsBuildWithProgress>(jobName, buildNumber.ToString());
            build.Progress = await GetBuildProgress(jobName, buildNumber.ToString());
            return build;
        }

        private async void CheckJobs()
        {
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
                            var buildDescription = await GetBuildDescription(jobName, startedBuild);
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