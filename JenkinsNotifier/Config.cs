using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace JenkinsNotifier
{
    public static class Config
    {
        private const string ConfigPath = "./config.json";
        
        private static ConfigData _current;
        public static ConfigData Current
        {
            get
            {
                if (_current == null)
                {
                    Load();
                }
                
                return _current;
            }
        }

        public static void Load()
        {
            bool loaded = false;

            if (File.Exists(ConfigPath))
            {
                var jsonString = File.ReadAllText(ConfigPath);

                if (!string.IsNullOrEmpty(jsonString))
                {
                    try
                    {
                        _current = JsonConvert.DeserializeObject<ConfigData>(jsonString);
                        Save();
                        loaded = true;
                    }
                    catch (JsonException jsonException)
                    {
                        Logger.Log($"Failed to deserialize config file! Exception:\n{jsonException}");
                    }
                }
            }

            if (!loaded)
            {
                Logger.Log("Creating config file..");
                _current = new ConfigData();
                Save();
                PerformInitialSetup();
            }
        }

        public static void Save()
        {
            File.WriteAllText(ConfigPath, JsonConvert.SerializeObject(_current, Formatting.Indented));
        }

        public static void PerformInitialSetup()
        {
            Logger.Log("Config file is empty, fill it first!");
            Environment.Exit(0);
        }

        public class ConfigData
        {
            [JsonProperty("botAccessToken")] 
            public string BotAccessToken { get; private set; } = string.Empty;

            [JsonProperty("jenkinsBaseUrl")]
            public string JenkinsBaseUrl { get; private set; } = string.Empty;
            [JsonProperty("jenkinsUserName")]
            public string JenkinsUserName { get; private set; } = string.Empty;
            [JsonProperty("jenkinsAccessToken")]
            public string JenkinsApiToken { get; private set; } = string.Empty;
            
            [JsonProperty("adminsTelegramIds")]
            public List<long> AdminsIds { get; private set; } = new List<long>();

            [JsonProperty("checkJobsDelayMs")]
            public int CheckJobsDelayMs { get; private set; } = 5000;
            
            [JsonProperty("stateFile")]
            public string StateFile { get; private set; } = "state.json";
            
            [JsonProperty("chats")]
            public List<long> Chats { get; private set; } = new List<long>();
        }

    }
}