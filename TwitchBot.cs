using System;
using System.Collections.Generic;
using TootTally.Utils;
using TootTally.Graphics;
using TwitchLib.Api.Auth;
using TwitchLib.Client;
using TwitchLib.Client.Enums;
using TwitchLib.Client.Events;
using TwitchLib.Client.Extensions;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Clients;
using TwitchLib.Communication.Models;

namespace TootTally.Twitch
{
    class TwitchBot
    {
        private TwitchClient client;
        private const string CLIENT_ID = "4vfaarn1dtizogde5e36rsk6qx787e";
        private static List<string> SCOPES = new List<string> { "chat:read", "chat:edit" };
        public string ACCESS_TOKEN { private get; set; }
        // private string REFRESH_TOKEN = ""; // do we need this?
        public string CHANNEL { private get; set; }

        public TwitchBot()
        {
            if (!Initialize()) return;
            Plugin.Instance.LogInfo($"Attempting connection with channel {CHANNEL} using token {ACCESS_TOKEN}");
            ConnectionCredentials credentials = new ConnectionCredentials(CHANNEL, ACCESS_TOKEN);
	        var clientOptions = new ClientOptions
            {
                MessagesAllowedInPeriod = 750,
                ThrottlingPeriod = TimeSpan.FromSeconds(30)
            };
            WebSocketClient customClient = new WebSocketClient(clientOptions);
            client = new TwitchClient(customClient);
            client.Initialize(credentials, "channel");

            client.OnLog += Client_OnLog;
            client.OnJoinedChannel += Client_OnJoinedChannel;
            client.OnConnected += Client_OnConnected;
            client.OnChatCommandReceived += Client_HandleChatCommand;

            client.Connect();
        }

        public void Disconnect()
        {
            if (client.IsConnected) client.Disconnect();
        }

        private bool Initialize()
        {
            if (Plugin.Instance.option.TwitchAccessToken.Value == null || Plugin.Instance.option.TwitchAccessToken.Value == "") {
                PopUpNotifManager.DisplayNotif("Twitch Access Token is empty. Please fill it in.", GameTheme.themeColors.notification.defaultText);
                return false;
            }
            // TODO: Check if ACCESS_TOKEN actually works
            ACCESS_TOKEN = Plugin.Instance.option.TwitchAccessToken.Value;
            CHANNEL = Plugin.Instance.option.TwitchUsername.Value;
            return true;
        }

        private void Client_HandleChatCommand(object sender, OnChatCommandReceivedArgs args)
        {
            string command = args.Command.CommandText;
            string cmd_args = args.Command.ArgumentsAsString;
            switch (command)
            {
                case "ttr": // Request a song
                    if (Plugin.Instance.option.EnableRequestsCommand.Value)
                        HandleRequestCommand(cmd_args);
                    break;
                case "profile": // Get profile
                    if (Plugin.Instance.option.EnableProfileCommand.Value && TootTally.Plugin.userInfo.id > 0)
                        client.SendMessage(CHANNEL, $"TootTally Profile: https://toottally.com/profile/{TootTally.Plugin.userInfo.id}");
                    break;
                case "song": // Get current song
                    if (Plugin.Instance.option.EnableCurrentSongCommand.Value)
                        client.SendMessage(CHANNEL, $"Current Song: {Plugin.Instance.CurrentSong}");
                    break;
                default:
                    break;
            }
        }

        private void HandleRequestCommand(string arg)
        {
            return;
        }

        private void Client_OnLog(object sender, OnLogArgs e)
        {
            Plugin.Instance.LogInfo($"{e.DateTime.ToString()}: {e.BotUsername} - {e.Data}");
        }
  
        private void Client_OnConnected(object sender, OnConnectedArgs e)
        {
            Plugin.Instance.LogInfo($"Connected to {e.AutoJoinChannel}");
        }
  
        private void Client_OnJoinedChannel(object sender, OnJoinedChannelArgs e)
        {
            client.SendMessage(e.Channel, "TootTally Twitch Integration successful!");
            Plugin.Instance.LogInfo("Twitch integration successfully attached to chat!");
            CHANNEL = e.Channel;
        }

        private void Client_OnDisconnected(object sender, OnDisconnectedArgs e)
        {
            Plugin.Instance.LogInfo("TwitchBot successfully disconnected from Twitch!");
        }
    }
}