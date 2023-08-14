using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using static TootTally.Twitch.Plugin;

namespace TootTally.Twitch
{
    public static class FileManager
    {
        public const string fileName = "TwitchRequests.json";
        public static void SaveToFile(List<Request> requests)
        {
            string requestQueuePath = Path.Combine(BepInEx.Paths.ConfigPath, fileName);
            if (File.Exists(requestQueuePath))
                File.Delete(requestQueuePath);

            File.WriteAllText(requestQueuePath, JsonConvert.SerializeObject(requests));
        }

        public static List<Request> GetRequestsFromFile()
        {
            string requestQueuePath = Path.Combine(BepInEx.Paths.ConfigPath, fileName);
            if (File.Exists(requestQueuePath))
                try
                {
                    return JsonConvert.DeserializeObject<List<Request>>(File.ReadAllText(requestQueuePath));
                }
                catch (Exception e)
                {
                    Plugin.Instance.LogError("Couldn't parse request queue file.");
                    Plugin.Instance.LogError(e.Message);
                    Plugin.Instance.LogError(e.StackTrace);
                }
            return new List<Request>();
        }
    }
}
