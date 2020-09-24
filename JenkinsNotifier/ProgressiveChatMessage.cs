using System;
using JenkinsNET.Models;
using Telegram.Bot.Types.ReplyMarkups;

namespace JenkinsNotifier
{
    public class ProgressiveChatMessage
    {
        public long ChatId;
        public int MessageId;
        public string JobName;
        public int? BuildNumber;
        public bool Completed;
        public bool? IsBuilding;
        public long BuildProgress;
        public string BuildResult;
        public long? BuildEstimatedDuration;
        public long? BuildTimeStamp;
        public long BuildCompletionTimeStamp;

        private TimeSpan GetDuration()
        {
            if (BuildTimeStamp != null)
            {
                var diff = DateTime.Now - BuildTimeStamp.Value.FromUnixTimestampMs().ToLocalTime();
                return diff;
            }
            
            return TimeSpan.Zero;
        }

        public void Update(JenkinsBuildWithProgress build)
        {
            IsBuilding = build.Building;
            BuildProgress = build.Progress > BuildProgress ? build.Progress : BuildProgress;
            BuildResult = build.Result;
            BuildEstimatedDuration = build.EstimatedDuration;
            BuildTimeStamp = build.TimeStamp;
        }

        public string GetDescriptionString()
        {
            string progressBar = "Not available";

            //var duration = GetDuration(build).TotalMilliseconds;
            if (BuildEstimatedDuration != null)
            {
                const int barLength = 15;
                progressBar = "";

                for (int i = 0; i < barLength; i++)
                {
                    //double? bt = duration / (double) build.EstimatedDuration;
                    float bt = BuildProgress / 100f;
                    float t = i / (float) barLength;
                    progressBar += t < bt ? "▓" : "░";
                }
            }

            var status = "Not available";
            if (Completed && BuildResult != null)
            {
                status = BuildResult;
            }
            else if (IsBuilding != null)
            {
                status = IsBuilding.Value
                    ? "Building"
                    : "Not building";
            }
            
            var buildNumber = BuildNumber ?? -1;
            var timestamp = BuildTimeStamp ?? 0;
            return $"*{JobName} #{buildNumber}*\n" +
                   $"Status: *{status}*\n" +
                   $"Build started at: *{timestamp.FromUnixTimestampMs().ToLocalTime():G}*\n" +
                   $"Duration: *~{GetDuration().ToHumanReadable()}*\n" +
                   $"Estimated build time: *~{BuildEstimatedDuration.ToTimespan().ToHumanReadable()}*\n" +
                   $"Progress: {progressBar}\n" +
                   (Completed ? $"Completed at: *{BuildCompletionTimeStamp.FromUnixTimestamp():G}*\n" : "\n") +
                   $"\n_Updated at:_ {DateTime.Now:G}\n";
        }

        public string ToBuildString()
        {
            return $"{JobName} #{BuildNumber}";
        }

        public string ToCompletedMessageString(JenkinsBuildBase jenkinsBuildBase)
        {
            return $"Build {ToBuildString()} ZALETEL";
        }
        
        public InlineKeyboardMarkup GetKeyboard(JenkinsBuildBase build)
        {
            if (build?.Building == true)
            {
                var inlineKeyboard = new InlineKeyboardMarkup(new[]
                {
                    new[]
                    {
                        new InlineKeyboardButton()
                        {
                            Text = $"Abort",
                            CallbackData = $"abort:{JobName}:{build.Number}"
                        },
                    },
                });
                return inlineKeyboard;
            }
            else
            {
                return null;
            }
        }

        public void SetCompleted()
        {
            if (Completed) return;

            Completed = true;
            BuildCompletionTimeStamp = DateTime.Now.ToUnixTimestamp();
        }
    }
}