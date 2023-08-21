using BaboonAPI.Hooks.Initializer;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using TootTally.Utils;
using TootTally.Utils.APIServices;
using TootTally.Utils.TootTallySettings;
using TootTally.Graphics;
using UnityEngine;
using System;
using BaboonAPI.Hooks.Tracks;
using TootTally.Utils.Helpers;
using TrombLoader.CustomTracks;

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
        public TwitchBot Bot = null;
        private TootTallySettingPage _settingPage;
        public RequestController requestController;
        public void LogInfo(string msg) => Logger.LogInfo(msg);
        public void LogError(string msg) => Logger.LogError(msg);

        private void Awake()
        {
            if (Instance != null) return;
            Instance = this;

            GameInitializationEvent.Register(Info, TryInitialize);
        }

        private void Update()
        {
            RequestPanelManager.Update();
        }

        private void TryInitialize()
        {
            // Bind to the TTModules Config for TootTally
            ModuleConfigEnabled = TootTally.Plugin.Instance.Config.Bind("Modules", "Twitch", true, "TootTally Twitch Integration");
            TootTally.Plugin.AddModule(this);
        }

        public void LoadModule()
        {
            string configPath = Path.Combine(Paths.BepInExRootPath, "config/");
            string toottallyTwitchLink = "https://toottally.com/twitch/";
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

            _settingPage = TootTallySettingsManager.AddNewPage(CONFIG_FIELD, "Twitch Integration Settings", 40, new Color(.1f, .1f, .1f, .1f));
            if (_settingPage != null)
            {
                _settingPage.AddToggle("EnableRequestsCommand", option.EnableRequestsCommand, (value) => { });
                _settingPage.AddToggle("EnableCurrentSongsCommand", option.EnableCurrentSongCommand, (value) => { });
                _settingPage.AddToggle("EnableProfileCommand", option.EnableProfileCommand, (value) => { });
                _settingPage.AddLabel("TwitchSpecificSettingsLabel", "Twitch Integration", 24); // 20 is the default size for text
                _settingPage.AddTextField("Twitch Username", new Vector2(350, 50), 20, option.TwitchUsername.Value, false, SetTwitchUsername);
                _settingPage.AddTextField("Twitch Access Token", new Vector2(350, 50), 20, option.TwitchAccessToken.Value, true, SetTwitchAccessToken);
                _settingPage.AddButton("AuthorizeTwitchButton", new Vector2(450, 50), "Authorize TootTally on Twitch", delegate () { Application.OpenURL(toottallyTwitchLink); });
                _settingPage.AddButton("GetAccessToken", new Vector2(450, 50), "Refresh Access Token", delegate ()
                {
                    Instance.StartCoroutine(TootTallyAPIService.GetValidTwitchAccessToken((token_info) =>
                    {
                        option.TwitchAccessToken.Value = token_info.access_token;
                        PopUpNotifManager.DisplayNotif("Access token successfully refreshed", GameTheme.themeColors.notification.defaultText);
                    }));
                });
                _settingPage.AddLabel("TwitchBotButtons", "Twitch Bot Settings", 24);
                _settingPage.AddButton("ConnectDisconnectBot", new Vector2(350, 50), "Connect/Disconnect Bot", () =>
                {
                    if (Bot == null)
                    {
                        Instance.StartCoroutine(StartBotCoroutine()); // Start and connect the bot if no bot detected yet
                    }
                    else
                    {
                        Bot.Disconnect(); // Disconnect the current bot if it exists
                        Bot = null;
                    }
                });
                _settingPage.AddLabel("TwitchBotInstruction", "Twitch bot will also automatically start when you enter the song select menu.", 16);
            }

            requestController = gameObject.AddComponent<RequestController>();

            Harmony.CreateAndPatchAll(typeof(TwitchPatches), PluginInfo.PLUGIN_GUID);
            LogInfo($"Module loaded!");
        }

        private void SetTwitchUsername(string text)
        {
            option.TwitchUsername.Value = text;
            PopUpNotifManager.DisplayNotif($"Twitch username is set to '{text}'", GameTheme.themeColors.notification.defaultText);
        }

        public static void DisplayNotif(string text, bool isError = false) => Instance.requestController?.DisplayNotif(text, isError);

        private void SetTwitchAccessToken(string text)
        {
            option.TwitchAccessToken.Value = text;
        }

        public IEnumerator StartBotCoroutine()
        {
            if (Bot == null)
            {
                Instance.StartCoroutine(TootTallyAPIService.GetValidTwitchAccessToken((token_info) =>
                {
                    option.TwitchAccessToken.Value = token_info.access_token;
                    Bot = new TwitchBot();
                    PopUpNotifManager.DisplayNotif("Access token successfully refreshed", GameTheme.themeColors.notification.defaultText);
                }));
            }
            yield return null;
        }



        public void UnloadModule()
        {
            RequestPanelManager.Dispose();
            TootTallySettingsManager.RemovePage(_settingPage);
            Bot?.Disconnect();
            Bot = null;
            requestController?.Dispose();
            GameObject.DestroyImmediate(requestController);
            StopAllCoroutines();
            Harmony.UnpatchID(PluginInfo.PLUGIN_GUID);
            LogInfo($"Module unloaded!");
        }

        public static class TwitchPatches
        {
            private static string _selectedSongTrackRef;

            // Apply your Trombone Champ patches here
            [HarmonyPatch(typeof(GameObjectFactory), nameof(GameObjectFactory.OnHomeControllerInitialize))]
            [HarmonyPostfix]
            public static void InitializeRequestPanel()
            {
                RequestPanelManager.Initialize();
            }

            [HarmonyPatch(typeof(HomeController), nameof(HomeController.Start))]
            [HarmonyPostfix]
            public static void DeInitialize()
            {
                RequestPanelManager.songSelectInstance = null;
            }

            [HarmonyPatch(typeof(HomeController), nameof(HomeController.tryToSaveSettings))]
            [HarmonyPostfix]
            public static void InitializeRequestPanelOnSaveConfig() => InitializeRequestPanel();

            [HarmonyPatch(typeof(GameController), nameof(GameController.Start))]
            [HarmonyPostfix]
            public static void SetCurrentSong()
            {
                RequestPanelManager.songSelectInstance = null;
                RequestPanelManager.isPlaying = true;
                var track = TrackLookup.lookup(RequestPanelManager.songTrackref);
                var songHash = SongDataHelper.GetSongHash(track);
                Instance.StartCoroutine(TootTallyAPIService.GetHashInDB(songHash, track is CustomTrack, (id) => RequestPanelManager.currentSongID = id));
            }

            [HarmonyPatch(typeof(PointSceneController), nameof(PointSceneController.Start))]
            [HarmonyPostfix]
            public static void ResetCurrentSong()
            {
                RequestPanelManager.isPlaying = false;
                RequestPanelManager.Remove(GlobalVariables.chosen_track_data.trackref);
            }

            [HarmonyPatch(typeof(LevelSelectController), nameof(LevelSelectController.Start))]
            [HarmonyPostfix]
            public static void StartBot(LevelSelectController __instance, List<SingleTrackData> ___alltrackslist, int ___songindex)
            {
                RequestPanelManager.songSelectInstance = __instance;
                RequestPanelManager.songIndex = ___songindex;
                RequestPanelManager.songTrackref = ___alltrackslist[___songindex].trackref;
                RequestPanelManager.isPlaying = false;
                Instance.StartCoroutine(Instance.StartBotCoroutine());
            }

            [HarmonyPatch(typeof(LevelSelectController), nameof(LevelSelectController.advanceSongs))]
            [HarmonyPostfix]
            public static void UpdateInstance(LevelSelectController __instance, List<SingleTrackData> ___alltrackslist, int ___songindex)
            {
                RequestPanelManager.songSelectInstance = __instance;
                RequestPanelManager.songIndex = ___songindex;
                RequestPanelManager.songTrackref = ___alltrackslist[___songindex].trackref;
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

        [Serializable]
        public class Request
        {
            public string requester;
            public SerializableClass.SongDataFromDB songData;
            public int song_id;
            public string date;
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