﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using static TootTally.Twitch.Plugin;

namespace TootTally.Twitch
{
    public static class FileManager
    {
        public const string requestFileName = "TwitchRequests.json";
        public const string blockFileName = "BlockedRequests.json";
        public static void SaveRequestsQueueToFile(List<Request> requests)
        {
            string requestQueuePath = Path.Combine(BepInEx.Paths.ConfigPath, requestFileName);
            if (File.Exists(requestQueuePath))
                File.Delete(requestQueuePath);

            File.WriteAllText(requestQueuePath, JsonConvert.SerializeObject(requests));
        }

        public static List<Request> GetRequestsFromFile()
        {
            string requestQueuePath = Path.Combine(BepInEx.Paths.ConfigPath, requestFileName);
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

        public static void SaveBlockedRequestsToFile(List<BlockedRequests> requests)
        {
            string requestQueuePath = Path.Combine(BepInEx.Paths.ConfigPath, blockFileName);
            if (File.Exists(requestQueuePath))
                File.Delete(requestQueuePath);

            File.WriteAllText(requestQueuePath, JsonConvert.SerializeObject(requests));
        }

        public static List<BlockedRequests> GetBlockedRequestsFromFile()
        {
            string requestQueuePath = Path.Combine(BepInEx.Paths.ConfigPath, blockFileName);
            if (File.Exists(requestQueuePath))
                try
                {
                    return JsonConvert.DeserializeObject<List<BlockedRequests>>(File.ReadAllText(requestQueuePath));
                }
                catch (Exception e)
                {
                    Plugin.Instance.LogError("Couldn't parse block list file.");
                    Plugin.Instance.LogError(e.Message);
                    Plugin.Instance.LogError(e.StackTrace);
                }
            return new List<BlockedRequests>();
        }
    }
}
