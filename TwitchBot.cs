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
        public string ACCESS_TOKEN { private get; set; }
        public string CHANNEL { private get; set; }

        public TwitchBot()
        {
            if (!Initialize()) return;
            Plugin.Instance.LogInfo($"Attempting connection with channel {CHANNEL}...");
            ConnectionCredentials credentials = new ConnectionCredentials(CHANNEL, ACCESS_TOKEN);
	        var clientOptions = new ClientOptions
            {
                MessagesAllowedInPeriod = 750,
                ThrottlingPeriod = TimeSpan.FromSeconds(30)
            };
            WebSocketClient customClient = new WebSocketClient(clientOptions);
            client = new TwitchClient(customClient);
            client.Initialize(credentials, CHANNEL);

            client.OnLog += Client_OnLog;
            client.OnJoinedChannel += Client_OnJoinedChannel;
            client.OnConnected += Client_OnConnected;
            client.OnChatCommandReceived += Client_HandleChatCommand;
            client.OnIncorrectLogin += Client_OnIncorrectLogin;

            client.Connect();
        }

        public void Disconnect()
        {
            if (client.IsConnected) client.Disconnect();
        }

        private bool Initialize()
        {
            if (Plugin.Instance.option.TwitchAccessToken.Value == null || Plugin.Instance.option.TwitchAccessToken.Value == "") {
                PopUpNotifManager.DisplayNotif("Twitch Access Token is empty. Please fill it in.", GameTheme.themeColors.notification.errorText);
                return false;
            }
            // TODO: Check if ACCESS_TOKEN actually works
            ACCESS_TOKEN = Plugin.Instance.option.TwitchAccessToken.Value;
            if (Plugin.Instance.option.TwitchUsername.Value == null || Plugin.Instance.option.TwitchUsername.Value == "") {
                PopUpNotifManager.DisplayNotif("Twitch Username is empty. Please fill it in.", GameTheme.themeColors.notification.errorText);
                return false;
            }
            CHANNEL = Plugin.Instance.option.TwitchUsername.Value.ToLower();
            return true;
        }

        private void Client_OnIncorrectLogin(object sender, OnIncorrectLoginArgs args)
        {
            PopUpNotifManager.DisplayNotif("Login credentials incorrect. Please re-authorize and re-check your Twitch username.", GameTheme.themeColors.notification.errorText);
            client.Disconnect();
        }

        private void Client_HandleChatCommand(object sender, OnChatCommandReceivedArgs args)
        {
            string command = args.Command.CommandText;
            string cmd_args = args.Command.ArgumentsAsString;
            switch (command)
            {
                case "ttr": // Request a song
                    if (Plugin.Instance.option.EnableRequestsCommand.Value) {
                        if (args.Command.ArgumentsAsList.Count == 1) {
                            HandleRequestCommand(cmd_args, args.Command.ChatMessage.Username);
                        }
                        else {
                            client.SendMessage(CHANNEL, $"Use !ttr to request a chart use its TootTally Song ID! (Example: !ttr 3781)");
                        }
                    }
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

        private void HandleRequestCommand(string arg, string requester)
        {
            int song_id;
            if (int.TryParse(arg, out song_id)) {
                if (!Plugin.Instance.RequesterBlacklist.Contains(requester) && !Plugin.Instance.SongIDBlacklist.Contains(song_id)) {
                    Plugin.Instance.LogInfo($"Received request for {song_id}, waiting for TootTally API to respond.");
                    Plugin.Instance.StartCoroutine(TootTallyAPIService.GetSongDataFromDB(song_id, (songdata) => {
                        Plugin.Instance.LogInfo($"Obtained request by {requester} for song {songdata.author} - {songdata.name}");
                        PopUpNotifManager.DisplayNotif($"Requested song by {requester}: {songdata.author} - {songdata.name}", GameTheme.themeColors.notification.defaultText);
                        client.SendMessage(CHANNEL, $"Song ID {song_id} successfully requested.");
                        Plugin.Request req = new Plugin.Request();
                        req.requester = requester;
                        req.songData = songdata;
                        Plugin.Instance.Requests.Add(req);
                    }));
                }
                else {
                    Plugin.Instance.LogInfo($"Request rejected.");
                }
            }
            else {
                client.SendMessage(CHANNEL, "Invalid song ID. Please try again.");
            }
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
            client.SendMessage(e.Channel, "TootTally Twitch Integration ready!");
            PopUpNotifManager.DisplayNotif("Twitch Integration successful!", GameTheme.themeColors.notification.defaultText);
            Plugin.Instance.LogInfo("Twitch integration successfully attached to chat!");
            CHANNEL = e.Channel;
        }

        private void Client_OnDisconnected(object sender, OnDisconnectedArgs e)
        {
            Plugin.Instance.LogInfo("TwitchBot successfully disconnected from Twitch!");
            PopUpNotifManager.DisplayNotif("Twitch bot disconnected!", GameTheme.themeColors.notification.defaultText);
        }
    }
}