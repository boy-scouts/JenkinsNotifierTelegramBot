using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types.Enums;
using File = System.IO.File;

namespace JenkinsNotifier
{
    static class Program
    {
        private static JobsHandler _jobsHandler;
        private static ITelegramBotClient _botClient;

        private static Dictionary<long, List<ProgressiveChatMessage>> _chatMessages =
            new Dictionary<long, List<ProgressiveChatMessage>>();

        private static bool _started;

        private static TimedSemaphore _messageTimedSemaphore;
        
        // ReSharper disable once UnusedParameter.Local
        static void Main(string[] args)
        {
            Start();

            while (!_started)
            {
                Thread.Sleep(100);
            }
            
            while (!CommandHandler.HandleCommand())
            {
                // Do nothing
            }
        }

        private static async void Start()
        {
            Logger.Log("Starting..");

            Config.Load();
            LoadState();

            _messageTimedSemaphore =
                new TimedSemaphore(Config.Current.TimeWindowSeconds, Config.Current.MaxHitsPerTimeWindow);
            
            _botClient = new TelegramBotClient(Config.Current.BotAccessToken);
            _botClient.OnMessage += Bot_OnMessage;
            _botClient.OnCallbackQuery += Bot_OnCallbackQuery;
            _botClient.OnUpdate += BotClient_OnUpdate;
            _botClient.StartReceiving();

            _jobsHandler = new JobsHandler();
            await _jobsHandler.Initialize(Config.Current.JenkinsBaseUrl, Config.Current.JenkinsUserName,
                Config.Current.JenkinsApiToken);
            _jobsHandler.OnNewBuildStarted += JobsHandler_OnNewBuildStarted;
            _jobsHandler.OnJobsChecked += JobsHandlerOnOnJobsChecked;
            _jobsHandler.StartPolling();

            _started = true;
            
            Logger.Log("Started!");
        }
        
        private static void SaveState()
        {
            File.WriteAllText(Config.Current.StateFile, JsonConvert.SerializeObject(_chatMessages, Formatting.Indented));
        }

        private static void LoadState()
        {
            if (File.Exists(Config.Current.StateFile))
            {
                _chatMessages =
                    JsonConvert.DeserializeObject<Dictionary<long, List<ProgressiveChatMessage>>>(
                        File.ReadAllText(Config.Current.StateFile));
            }

            foreach (var chatId in Config.Current.Chats)
            {
                TryAddChatId(chatId);
            }
        }

        public static void TryAddChatId(long chatId)
        {
            if (!_chatMessages.ContainsKey(chatId))
            {
                _chatMessages.Add(chatId, new List<ProgressiveChatMessage>());
                Logger.Log($"Chat added: {chatId}");
            }
            
            SaveState();
        }
        
        public  static void TryRemoveChatId(long chatId)
        {
            if (_chatMessages.ContainsKey(chatId))
            {
                _chatMessages.Remove(chatId);
                Logger.Log($"Chat removed: {chatId}");
            }
            
            SaveState();
        }

        private static async void JobsHandlerOnOnJobsChecked()
        {
            try
            {
                await UpdateMessages();
            }
            catch (Exception e)
            {
                Logger.LogException(e);
            }
        }

        private static async Task UpdateMessages()
        {
            List<long> chatIds = _chatMessages.Keys.ToList();

            foreach (var chatId in chatIds)
            {
                for (var cm = 0; cm < _chatMessages[chatId].Count; cm++)
                {
                    try
                    {
                        var message = _chatMessages[chatId][cm];
                        if (message.Completed) continue;

                        var build = await _jobsHandler.GetBuildDescription(message.JobName, message.BuildNumber);

                        switch (build.Building)
                        {
                            case false when message.Completed:
                                continue;
                            case false when !message.Completed:
                                message.SetCompleted();
                                // await _botClient.SendTextMessageAsync(chatId, message.ToCompletedMessageString(build),
                                //     ParseMode.Markdown);
                                Logger.Log($"Build {message.ToBuildString()} has ended");
                                break;
                        }

                        message.Update(build);

                        await _messageTimedSemaphore.Hit();
                        
                        await _botClient.EditMessageTextAsync(chatId, message.MessageId,
                            message.GetDescriptionString(),
                            ParseMode.Markdown, replyMarkup: message.GetKeyboard(build));
                    }
                    catch (Exception ex)
                    {
                        RemoveChatIfNeeded(ex, chatId);
                        Logger.LogException(ex);
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
                var chatId = e?.Message?.Chat?.Id;
                if (chatId.HasValue)
                {
                    RemoveChatIfNeeded(ex, chatId.Value);
                }
                Logger.LogException(ex);
            }
        }
        
        private static void BotClient_OnUpdate(object sender, UpdateEventArgs e)
        {
            if (Config.Current.SelectedChatsOnly) return;
            
            var chatId = e.Update?.Message?.Chat?.Id;
            if (chatId.HasValue)
            {
                TryAddChatId(chatId.Value);
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
                        await AbortBuild(jobName, buildNum);
                        break;
                }

                await _messageTimedSemaphore.Hit();
                await _botClient.AnswerCallbackQueryAsync(
                    callbackQuery.Id,
                    replyText
                );
            }
            else
            {
                await _messageTimedSemaphore.Hit();
                await _botClient.AnswerCallbackQueryAsync(
                    callbackQuery.Id,
                    "Something went wrong"
                );
            }
        }

        private static async Task AbortBuild(string jobName, string buildNum)
        {
            _jobsHandler.AbortBuild(jobName, buildNum);
            foreach (var chatMessage in _chatMessages)
            {
                foreach (var progressiveChatMessage in chatMessage.Value)
                {
                    if (progressiveChatMessage.JobName == jobName &&
                        progressiveChatMessage.BuildNumber?.ToString() == buildNum)
                    {
                        progressiveChatMessage.IsAborting = true;
                    }
                }
            }

            await UpdateMessages();
        }

        private static async Task AnswerMessage(MessageEventArgs e)
        {
            var msg = e.Message;
            var chatId = msg.Chat.Id;
            //var fromId = msg.From.Id;

            async Task Reply(string text)
            {
                await _messageTimedSemaphore.Hit();
                await _botClient.SendTextMessageAsync(chatId, text, parseMode: ParseMode.Markdown);
            }

            Logger.Log($"Received a {msg.Type} {msg.Text} message from {msg.From.ToUserString()}");

            if (!_chatMessages.ContainsKey(chatId))
            {
                _chatMessages.Add(chatId, new List<ProgressiveChatMessage>());
            }

            await Reply("Hello there");
        }

        private static async void JobsHandler_OnNewBuildStarted(string jobName, JenkinsBuildWithProgress build)
        {
            Logger.Log($"Build {build.Number} started in job {jobName}");
            Logger.Log($"Notifying {_chatMessages.Count} chats");

            foreach (var chatMessage in _chatMessages)
            {
                var chatId = chatMessage.Key;

                try
                {
                    var progressiveMessage = new ProgressiveChatMessage {JobName = jobName, BuildNumber = build.Number};

                    progressiveMessage.Update(build);
                    await _messageTimedSemaphore.Hit();
                    var message = await _botClient.SendTextMessageAsync(chatId,
                        progressiveMessage.GetDescriptionString(),
                        ParseMode.Markdown, replyMarkup: progressiveMessage.GetKeyboard(build));

                    progressiveMessage.ChatId = message.Chat.Id;
                    progressiveMessage.MessageId = message.MessageId;
                    _chatMessages[chatId].Add(progressiveMessage);

                    Logger.Log($"Added progressive message to {chatId}");
                }
                catch (Exception e)
                {
                    RemoveChatIfNeeded(e, chatId);
                    Logger.LogException(e);
                }
            }

            SaveState();
        }

        private static void RemoveChatIfNeeded(Exception ex, long chatId)
        {
            if (ex is ApiRequestException apiRequestException)
            {
                Logger.Log($"API REQUEST EXCEPTION!\nError Code: {apiRequestException.ErrorCode}\n{apiRequestException}");
                
                if (apiRequestException is ChatNotFoundException)
                {
                    TryRemoveChatId(chatId);
                }
                else if (apiRequestException.ErrorCode == 403)
                {
                    TryRemoveChatId(chatId);
                }
            }
        }

        public static void ListChats()
        {
            foreach (var chatMessage in _chatMessages)
            {
                Console.WriteLine(chatMessage.Key);
            }
        }
    }
}