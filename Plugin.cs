using BaboonAPI.Hooks.Initializer;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.IO;
using TootTally.Utils;

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
        public string CurrentSong { get; internal set; }
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
            // Attempt to add this module to the TTModules page in TrombSettings
            if (TootTally.Plugin.Instance.moduleSettings != null) OptionalTrombSettings.Add(TootTally.Plugin.Instance.moduleSettings, ModuleConfigEnabled);
            TootTally.Plugin.AddModule(this);

            // Set CurrentSong to default value
            CurrentSong = "No song currently being played.";
        }

        public void LoadModule()
        {
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
            };

            var settingsPage = OptionalTrombSettings.GetConfigPage("Twitch");
            if (settingsPage != null) {
                // Use OptionalTrombSettings to add your config to TrombSettings
                // Example:
                // OptionalTrombSettings.Add(settingsPage, option.Unlimited);
                OptionalTrombSettings.Add(settingsPage, option.EnableRequestsCommand);
                OptionalTrombSettings.Add(settingsPage, option.EnableCurrentSongCommand);
                OptionalTrombSettings.Add(settingsPage, option.EnableProfileCommand);
            }

            Harmony.CreateAndPatchAll(typeof(TwitchPatches), PluginInfo.PLUGIN_GUID);
            LogInfo($"Module loaded!");
        }

        public void UnloadModule()
        {
            Harmony.UnpatchID(PluginInfo.PLUGIN_GUID);
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
            public ConfigEntry<string> TwitchOAuthToken { get; set; }
        }
    }
}