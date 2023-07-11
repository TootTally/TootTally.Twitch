using BaboonAPI.Hooks.Initializer;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System.IO;
using System.Collections;
using TootTally.Utils;
using TootTally.Utils.TootTallySettings;
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
        private TwitchBot Bot = null;
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
            string toottallyTwitchLink = "https://localhost/twitch/";
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
                settingsPage.AddLabel("TwitchSpecificSettingsLabel", "Twitch Integration", 20); // 20 is the default size
                var twitchUsernameTextField = settingsPage.AddTextField("Twitch Username", new Vector2(350, 50), 20, "");
                // twitchUsernameTextField.inputField.onValueChanged.AddListener(SetTwitchUsername);
                var twitchAccessTokenTextField = settingsPage.AddTextField("Twitch Access Token", new Vector2(350, 50), 20, "", true);
                // twitchAccessTokenTextField.inputField.onValueChanged.AddListener(SetTwitchAccessToken);
                settingsPage.AddButton("AuthorizeTwitchButton", new Vector2(450, 50), "Authorize TootTally on Twitch", delegate() { Application.OpenURL(toottallyTwitchLink); });
            }

            Harmony.CreateAndPatchAll(typeof(TwitchPatches), PluginInfo.PLUGIN_GUID);
            LogInfo($"Module loaded!");
        }

        public void SetTwitchUsername(string text)
        {
            option.TwitchUsername.Value = text;
        }

        public void SetTwitchAccessToken(string text)
        {
            option.TwitchAccessToken.Value = text;
        }

        public void UnloadModule()
        {
            Harmony.UnpatchID(PluginInfo.PLUGIN_GUID);
            if (Plugin.Instance.Bot != null) {
                Plugin.Instance.Bot.Disconnect();
                Plugin.Instance.Bot = null;
            }
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
    }
}