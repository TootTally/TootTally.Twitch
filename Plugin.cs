using BaboonAPI.Hooks.Initializer;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using TootTally.Utils;
using TootTally.Utils.TootTallySettings;
using TootTally.Graphics;
using UnityEngine;

namespace TootTally.Twitch
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    [BepInDependency("TootTally", BepInDependency.DependencyFlags.HardDependency)]
    public class Plugin : BaseUnityPlugin, ITootTallyModule
    {
        public static Plugin Instance;

        private const string CONFIG_NAME = "TwitchIntegration.cfg";
        private const string CONFIG_FIELD = "Twitch";
        public Options option;
        public ConfigEntry<bool> ModuleConfigEnabled { get; set; }
        public bool IsConfigInitialized { get; set; }
        public string Name { get => PluginInfo.PLUGIN_NAME; set => Name = value; }
        public ManualLogSource GetLogger { get => Logger; }
        public string CurrentSong { get; internal set; }
        public List<Request> Requests { get; set; } // real requests queue
        public List<string> RequesterBlacklist { get; set; }
        public List<int> SongIDBlacklist { get; set; }
        private TwitchBot Bot = null;
        private Stack<Notif> NotifStack;
        private Stack<UnprocessedRequest> RequestStack; // Unfinished request stack, only song ids here
        public void LogInfo(string msg) => Logger.LogInfo(msg);
        public void LogError(string msg) => Logger.LogError(msg);

        private void Awake()
        {
            if (Instance != null) return;
            Instance = this;
            
            GameInitializationEvent.Register(Info, TryInitialize);
        }

        private void TryInitialize()
        {
            // Bind to the TTModules Config for TootTally
            ModuleConfigEnabled = TootTally.Plugin.Instance.Config.Bind("Modules", "Twitch", true, "TootTally Twitch Integration");
            TootTally.Plugin.AddModule(this);

            // Set CurrentSong to default value
            CurrentSong = "No song currently being played.";
        }

        public void LoadModule()
        {
            string toottallyTwitchLink = "https://toottally.com/twitch/";
            string configPath = Path.Combine(Paths.BepInExRootPath, "config/");
            ConfigFile config = new ConfigFile(configPath + CONFIG_NAME, true);
            option = new Options()
            {
                // Set your config here by binding them to the related ConfigEntry in your Options class
                // Example:
                // Unlimited = config.Bind(CONFIG_FIELD, "Unlimited", DEFAULT_UNLISETTING)
                EnableRequestsCommand = config.Bind(CONFIG_FIELD, "Enable requests command (!ttr)", true),
                EnableCurrentSongCommand = config.Bind(CONFIG_FIELD, "Enable current song command (!song)", true),
                EnableProfileCommand = config.Bind(CONFIG_FIELD, "Enable profile command (!profile)", true),
                TwitchUsername = config.Bind(CONFIG_FIELD, "Twitch channel to attach to", ""),
                TwitchAccessToken = config.Bind(CONFIG_FIELD, "Twitch Access Token", "")
            };
            
            var settingsPage = TootTallySettingsManager.AddNewPage(CONFIG_FIELD, "Twitch Integration Settings", 40, new Color(.1f, .1f, .1f, .1f));
            if (settingsPage != null)
            {
                settingsPage.AddToggle("EnableRequestsCommand", option.EnableRequestsCommand, (value) => {} );
                settingsPage.AddToggle("EnableCurrentSongsCommand", option.EnableCurrentSongCommand, (value) => {});
                settingsPage.AddToggle("EnableProfileCommand", option.EnableProfileCommand, (value) => {});
                settingsPage.AddLabel("TwitchSpecificSettingsLabel", "Twitch Integration", 24); // 20 is the default size for text
                settingsPage.AddTextField("Twitch Username", new Vector2(350, 50), 20, option.TwitchUsername.Value, false, SetTwitchUsername);
                settingsPage.AddTextField("Twitch Access Token", new Vector2(350, 50), 20, option.TwitchAccessToken.Value, true, SetTwitchAccessToken);
                settingsPage.AddButton("AuthorizeTwitchButton", new Vector2(450, 50), "Authorize TootTally on Twitch", delegate() { Application.OpenURL(toottallyTwitchLink); });
                settingsPage.AddButton("GetAccessToken", new Vector2(450, 50), "Refresh Access Token", delegate () {
                    Plugin.Instance.StartCoroutine(TootTallyAPIService.GetValidTwitchAccessToken((token_info) => {
                        option.TwitchAccessToken.Value = token_info.access_token;
                        Plugin.Instance.DisplayNotif("Access token successfully obtained");
                    }));
                });
                settingsPage.AddLabel("TwitchBotButtons", "Twitch Bot Settings", 24);
                settingsPage.AddButton("ConnectDisconnectBot", new Vector2(350, 50), "Connect/Disconnect Bot", () => {
                    if (Plugin.Instance.Bot == null) {
                        Plugin.Instance.Bot = new TwitchBot(); // Start and connect the bot if no bot detected yet
                    }
                    else {
                        Plugin.Instance.Bot.Disconnect(); // Disconnect the current bot if it exists
                        Plugin.Instance.Bot = null;
                    }
                });
                settingsPage.AddLabel("TwitchBotInstruction", "Twitch bot will also automatically start when you enter the song select menu.", 16);
            }

            Plugin.Instance.NotifStack = new Stack<Notif>();
            Plugin.Instance.RequestStack = new Stack<UnprocessedRequest>();
            Plugin.Instance.StartCoroutine(NotifCoroutine());
            Plugin.Instance.StartCoroutine(RequestCoroutine());

            Harmony.CreateAndPatchAll(typeof(TwitchPatches), PluginInfo.PLUGIN_GUID);
            LogInfo($"Module loaded!");
        }

        private void SetTwitchUsername(string text)
        {
            option.TwitchUsername.Value = text;
            Plugin.Instance.DisplayNotif($"Twitch username is set to '{text}'");
        }

        private void SetTwitchAccessToken(string text)
        {
            option.TwitchAccessToken.Value = text;
        }

        public IEnumerator NotifCoroutine()
        {
            while (true) {
                Notif notif;
                if (Plugin.Instance.NotifStack.TryPop(out notif)) {
                    Plugin.Instance.LogInfo("Attempting to generate notification...");
                    PopUpNotifManager.DisplayNotif(notif.message, notif.color);
                }
                yield return null;
            }
        }

        public IEnumerator RequestCoroutine()
        {
            while (true) {
                UnprocessedRequest request;
                if (Plugin.Instance.RequestStack.TryPop(out request)) {
                    Plugin.Instance.LogInfo($"Attempting to get song data for ID {request.song_id}");
                    Plugin.Instance.StartCoroutine(TootTallyAPIService.GetSongDataFromDB(request.song_id, (songdata) => {
                        Plugin.Instance.LogInfo($"Obtained request by {request.requester} for song {songdata.author} - {songdata.name}");
                        Plugin.Instance.DisplayNotif($"Requested song by {request.requester}: {songdata.author} - {songdata.name}");
                        Plugin.Instance.Bot.client.SendMessage(Plugin.Instance.Bot.CHANNEL, $"Song ID {request.song_id} successfully requested.");
                        Request req = new Request();
                        req.requester = request.requester;
                        req.songData = songdata;
                        Plugin.Instance.Requests.Add(req);
                    }));
                }
                yield return null;
            }
        }

        public void DisplayNotif(string message, bool isError=false)
        {
            Color color = isError ? GameTheme.themeColors.notification.errorText : GameTheme.themeColors.notification.defaultText;
            Notif notif = new Notif();
            notif.message = message;
            notif.color = color;
            Plugin.Instance.NotifStack.Push(notif);
            // PopUpNotifManager.DisplayNotif(message, color);
        }
        
        public void RequestSong(int song_id, string requester)
        {
            UnprocessedRequest request = new UnprocessedRequest();
            if (!Plugin.Instance.RequesterBlacklist.Contains(requester) && !Plugin.Instance.SongIDBlacklist.Contains(song_id)) {
                Plugin.Instance.LogInfo($"Accepted request {song_id} by {requester}.");
                request.song_id = song_id;
                request.requester = requester;
                Plugin.Instance.RequestStack.Push(request);
            }
        }

        public void UnloadModule()
        {
            Harmony.UnpatchID(PluginInfo.PLUGIN_GUID);
            if (Plugin.Instance.Bot != null) {
                Plugin.Instance.Bot.Disconnect();
                Plugin.Instance.Bot = null;
            }
            if (Plugin.Instance.NotifStack != null) {
                Plugin.Instance.NotifStack.Clear();
                Plugin.Instance.NotifStack = null;
            }
            if (Plugin.Instance.RequestStack != null) {
                Plugin.Instance.RequestStack.Clear();
                Plugin.Instance.RequestStack = null;
            }
            Plugin.Instance.StopAllCoroutines();
            LogInfo($"Module unloaded!");
        }

        public static class TwitchPatches
        {
            // Apply your Trombone Champ patches here
            [HarmonyPatch(typeof(GameController), nameof(GameController.Start))]
            [HarmonyPostfix]
            public static void SetCurrentSong()
            {
                Plugin.Instance.CurrentSong = $"{GlobalVariables.chosen_track_data.artist} - {GlobalVariables.chosen_track_data.trackname_long}";
            }

            [HarmonyPatch(typeof(PointSceneController), nameof(PointSceneController.Start))]
            [HarmonyPostfix]
            public static void ResetCurrentSong()
            {
                Plugin.Instance.CurrentSong = "No song currently being played.";
            }
            
            [HarmonyPatch(typeof(LevelSelectController), nameof(LevelSelectController.Start))]
            [HarmonyPostfix]
            public static void StartBot()
            {
                if (Plugin.Instance.Bot == null) {
                    Plugin.Instance.Bot = new TwitchBot();
                }
            }
        }

        public class Options
        {
            // Fill this class up with ConfigEntry objects that define your configs
            // Example:
            // public ConfigEntry<bool> Unlimited { get; set; }
            public ConfigEntry<bool> EnableRequestsCommand { get; set; }
            public ConfigEntry<bool> EnableProfileCommand { get; set; }
            public ConfigEntry<bool> EnableCurrentSongCommand { get; set; }
            public ConfigEntry<string> TwitchUsername { get; set; }
            public ConfigEntry<string> TwitchAccessToken { get; set; }
        }

        public class Request
        {
            public string requester;
            public SerializableClass.SongDataFromDB songData;
        }

        public class UnprocessedRequest
        {
            public string requester;
            public int song_id;
        }

        public class Notif
        {
            public string message;
            public Color color;
        }
    }
}