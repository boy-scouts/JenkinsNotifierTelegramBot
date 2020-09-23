using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JenkinsNET.Models;
using Newtonsoft.Json;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using File = System.IO.File;

namespace JenkinsNotifier
{
    static class Program
    {
        private const string StateFile = "state.json";
        
        private static JobsHandler _jobsHandler;
        private static ITelegramBotClient _botClient;
        
        private static Dictionary<long, List<ProgressiveChatMessage>> _chatMessages = new Dictionary<long, List<ProgressiveChatMessage>>();

        // ReSharper disable once UnusedParameter.Local
        static void Main(string[] args)
        {
            Logger.Log("Starting..");

            Config.Load();
            LoadState();

            _botClient = new TelegramBotClient(Config.Current.BotAccessToken);
            _botClient.OnMessage += Bot_OnMessage;
            _botClient.OnCallbackQuery += Bot_OnCallbackQuery;
            _botClient.StartReceiving();
            
            _jobsHandler = new JobsHandler(Config.Current.JenkinsBaseUrl, Config.Current.JenkinsUserName, Config.Current.JenkinsApiToken);
            _jobsHandler.OnNewBuildStarted += JobsHandlerOnOnNewBuildStarted;
            _jobsHandler.OnJobsChecked += JobsHandlerOnOnJobsChecked;
            _jobsHandler.StartPolling();

            Logger.Log("Started!");
            
            while (!CommandHandler.HandleCommand())
            {
                // Do nothing
            }
        }

        private static void SaveState()
        {
            File.WriteAllText(StateFile, JsonConvert.SerializeObject(_chatMessages, Formatting.Indented));
        }

        private static void LoadState()
        {
            if (File.Exists(StateFile))
            {
                _chatMessages =
                    JsonConvert.DeserializeObject<Dictionary<long, List<ProgressiveChatMessage>>>(
                        File.ReadAllText(StateFile));
            }
        }

        private static void JobsHandlerOnOnJobsChecked()
        {
            UpdateMessages();
        }

        private static async void UpdateMessages()
        {
            List<long> chatIds = _chatMessages.Keys.ToList();

            foreach (var chatId in chatIds)
            {
                for (var cm = 0; cm < _chatMessages[chatId].Count; cm++)
                {
                    var message = _chatMessages[chatId][cm];
                    if (message.Completed) continue;
                    
                    var build = _jobsHandler.GetBuildDescription(message.JobName, message.BuildNumber);
                    
                    switch (build.Building)
                    {
                        case false when message.Completed:
                            continue;
                        case false when !message.Completed:
                            message.Completed = true;
                            await _botClient.SendTextMessageAsync(chatId, message.ToCompletedMessageString(build),
                                ParseMode.Markdown);
                            break;
                    }

                    try
                    {
                        await _botClient.EditMessageTextAsync(chatId, message.MessageId, message.ToMessageString(build),
                            ParseMode.Markdown, replyMarkup: message.GetKeyboard(build));
                    }
                    catch (Telegram.Bot.Exceptions.MessageIsNotModifiedException)
                    {
                    }
                }
            }

            SaveState();
        }

        private static async void Bot_OnMessage(object sender, MessageEventArgs e)
        {
            try
            {
                await AnswerMessage(e);
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }
        
        private static async void Bot_OnCallbackQuery(object sender, CallbackQueryEventArgs e)
        {
            try
            {
                await AnswerCallbackQuery(e);
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }

        private static async Task AnswerCallbackQuery(CallbackQueryEventArgs e)
        {
            var callbackQuery = e.CallbackQuery;

            Logger.Log($"Received a callback query {callbackQuery.Data} from {callbackQuery.From.ToUserString()}");

            if (callbackQuery.Data.Contains(":"))
            {
                var replyText = "Wtf";
                
                var split = callbackQuery.Data.Split(':');
                var command = split[0];
                switch (command)
                {
                    case "abort":
                        var jobName = split[1];
                        var buildNum = split[2];
                        replyText = $"Aborting {jobName} #{buildNum}";
                        _jobsHandler.AbortBuild(jobName, buildNum);
                        break;
                }
                
                await _botClient.AnswerCallbackQueryAsync(
                    callbackQuery.Id,
                    replyText
                );
            }
            else
            {
                await _botClient.AnswerCallbackQueryAsync(
                    callbackQuery.Id,
                    "Something went wrong"
                );
            }
        }

        private static async Task AnswerMessage(MessageEventArgs e)
        {
            var msg = e.Message;
            var chatId = msg.Chat.Id;
            //var fromId = msg.From.Id;

            async Task Reply(string text)
            {
                await _botClient.SendTextMessageAsync(chatId, text, parseMode: ParseMode.Markdown);
            }

            Logger.Log($"Received a {msg.Type} {msg.Text} message from {msg.From.ToUserString()}");

            if (!_chatMessages.ContainsKey(chatId))
            {
                _chatMessages.Add(chatId, new List<ProgressiveChatMessage>());
            }

            await Reply("Hello there");
        }

        private static async void JobsHandlerOnOnNewBuildStarted(string jobName, JenkinsBuildBase build)
        {
            Logger.Log($"Build {build.Number} started in job {jobName}");

            foreach (var chatMessage in _chatMessages)
            {
                var progressiveMessage = new ProgressiveChatMessage {JobName = jobName, BuildNumber = build.Number};

                var chatId = chatMessage.Key;
                var message = await _botClient.SendTextMessageAsync(chatId, progressiveMessage.ToMessageString(build),
                    ParseMode.Markdown, replyMarkup: progressiveMessage.GetKeyboard(build));

                progressiveMessage.ChatId = message.Chat.Id;
                progressiveMessage.MessageId = message.MessageId;
                _chatMessages[chatId].Add(progressiveMessage);

                Logger.Log($"Added progressive message to {chatId}");
            }

            SaveState();
        }
    }

    public class ProgressiveChatMessage
    {
        public long ChatId;
        public int MessageId;
        public string JobName;
        public int? BuildNumber;
        public bool Completed;

        public TimeSpan GetDuration(JenkinsBuildBase build)
        {
            if (build.TimeStamp != null)
            {
                var diff = DateTime.Now - build.TimeStamp.Value.FromUnixTimestampMs().ToLocalTime();
                return diff;
            }
            
            Logger.Log(build.TimeStamp);

            return TimeSpan.Zero;
        }
        
        public string ToMessageString(JenkinsBuildBase build)
        {
            if (build == null) return "Wtf";
            
            string progressBar = "Not available";

            var duration = GetDuration(build).TotalMilliseconds;
            if (build.EstimatedDuration != null)
            {
                const int barLength = 15;
                progressBar = "";

                for (int i = 0; i < barLength; i++)
                {
                    double? bt = duration / (double) build.EstimatedDuration;
                    float t = i / (float) barLength;
                    progressBar += t < bt ? "▓" : "░";
                }
            }

            var status = build.Building != null 
                                ? build.Building.Value 
                                    ? "Building" 
                                    : "Not building" 
                                : "Not available";


            var buildNumber = build.Number ?? -1;
            var timestamp = build.TimeStamp ?? 0;
            return $"*{JobName} #{buildNumber}*\n" +
                   $"_Status_: {status}\n" +
                   $"_Build started at_: {timestamp.FromUnixTimestampMs().ToLocalTime():G}\n" +
                   $"_Duration_: ~{GetDuration(build).ToHumanReadable()}\n" +
                   $"_Estimated build time_: ~{build.EstimatedDuration.ToTimespan().ToHumanReadable()}\n" +
                   $"_Progress:_ {progressBar}\n" +
                   $"\n_Updated at:_ {DateTime.Now:G}\n";
        }

        public string ToCompletedMessageString(JenkinsBuildBase jenkinsBuildBase)
        {
            return $"{JobName} #{BuildNumber} ZALETEL";
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
    }
}