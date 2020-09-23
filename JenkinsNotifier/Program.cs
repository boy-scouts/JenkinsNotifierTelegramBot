using System;
using System.Collections.Generic;
using System.Linq;
using JenkinsNET;
using JenkinsNET.Models;

namespace JenkinsNotifier
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Logging in..");
            var client = new JenkinsClient {
                BaseUrl = "http://192.168.0.252:8080/",
                UserName = "admin",
                ApiToken = "11aa6eb88577c4e8fd7a9b6a1b5c5ad279",
            };
            
            Console.WriteLine(client.ApiToken);

            
            Console.WriteLine("Jobs:");
            var jobs = client.Get().Jobs;
            foreach (var job in jobs)
            {
                Console.WriteLine(job.Name);
            }

            var testJob = client.Jobs.Get<JenkinsFreeStyleJob>("TestPipeline");
            Console.WriteLine(testJob.Builds.Count());
            
            var build = client.Builds.Get<JenkinsBuildBase>("TestPipeline", "3");
            Console.WriteLine(build.Duration);
            
            // var newJob = client.Jobs.BuildWithParameters("Differences_Android", new Dictionary<string, string>()
            // {
            //     // {"PROJECT_TITLE", "Difference Master (android)"}, 
            //     {"UNITY_PATH", "/Applications/Unity/Hub/Editor/2019.4.7f1/Unity.app/Contents/MacOS/Unity"}, 
            //     {"PROJECT_TITLE", "/Users/admin/Jenkins/Builds/Differences_Android/result.aab"}, 
            //     {"LOG_TELEGRAM", "curl -s -X POST https://api.telegram.org/bot1120291876:AAF5V0tAH6z3ZtraL2kjwWkIlxDKJ9xtzAo/sendMessage -d chat_id=-440765607"}, 
            // });
            // Console.WriteLine(newJob.QueueItemUrl);

            // var text = client.Builds.GetConsoleText("Differences_Pipeline", "16");
            // Console.WriteLine(text);
        }
    }
}