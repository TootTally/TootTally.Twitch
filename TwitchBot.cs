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
using TwitchLib.Communication.Events;

namespace TootTally.Twitch
{
    class TwitchBot
    {
        internal TwitchClient client;
        public string ACCESS_TOKEN { private get; set; }
        public string CHANNEL { get; set; }
        public Stack<string> MessageStack { get; set; }

        public TwitchBot()
        {
            if (!Initialize()) return;
            Plugin.Instance.LogInfo($"Attempting connection with channel {CHANNEL}...");
            ConnectionCredentials credentials = new ConnectionCredentials(CHANNEL, ACCESS_TOKEN);
	        var clientOptions = new ClientOptions
            {
                MessagesAllowedInPeriod = 750,
                ThrottlingPeriod = TimeSpan.FromSeconds(30),
                ReconnectionPolicy = new ReconnectionPolicy(reconnectInterval: 5, maxAttempts: 3),
            };
            WebSocketClient customClient = new WebSocketClient(clientOptions);
            client = new TwitchClient(customClient);
            client.Initialize(credentials, CHANNEL);

            client.OnLog += Client_OnLog;
            client.OnJoinedChannel += Client_OnJoinedChannel;
            client.OnConnected += Client_OnConnected;
            client.OnChatCommandReceived += Client_HandleChatCommand;
            client.OnIncorrectLogin += Client_OnIncorrectLogin;
            client.OnError += Client_OnError;

            MessageStack = new Stack<string>();

            client.Connect();
        }

        public void Disconnect()
        {
            if (client != null && client.IsConnected) client.Disconnect();
            MessageStack.Clear();
            MessageStack = null;
        }

        private bool Initialize()
        {
            if (Plugin.Instance.option.TwitchAccessToken.Value == null || Plugin.Instance.option.TwitchAccessToken.Value == "") {
                Plugin.Instance.DisplayNotif("Twitch Access Token is empty. Please fill it in.", true);
                return false;
            }
            // TODO: Check if ACCESS_TOKEN actually works
            ACCESS_TOKEN = Plugin.Instance.option.TwitchAccessToken.Value;
            if (Plugin.Instance.option.TwitchUsername.Value == null || Plugin.Instance.option.TwitchUsername.Value == "") {
                Plugin.Instance.DisplayNotif("Twitch Username is empty. Please fill it in.", true);
                return false;
            }
            CHANNEL = Plugin.Instance.option.TwitchUsername.Value.ToLower();
            return true;
        }

        private void Client_OnError(object sender, OnErrorEventArgs args)
        {
            Plugin.Instance.LogError($"{args.Exception.ToString()}\n{args.Exception.StackTrace}");
        }

        private void Client_OnIncorrectLogin(object sender, OnIncorrectLoginArgs args)
        {
            Plugin.Instance.DisplayNotif("Login credentials incorrect. Please re-authorize and re-check your Twitch username.", true);
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
                            int song_id;
                            if (int.TryParse(cmd_args, out song_id)) {
                                Plugin.Instance.LogInfo($"Successfully parsed request for {song_id}, submitting to stack.");
                                Plugin.Instance.RequestSong(song_id, args.Command.ChatMessage.Username);
                            }
                            else {
                                Plugin.Instance.LogInfo("Could not parse request input, ignoring.");
                                client.SendMessage(CHANNEL, "Invalid song ID. Please try again.");
                            }
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
                    if (Plugin.Instance.option.EnableCurrentSongCommand.Value) {
                        client.SendMessage(CHANNEL, $"Current Song: {Plugin.Instance.CurrentSong}");
                    }
                    break;
                default:
                    break;
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
            Plugin.Instance.DisplayNotif("Twitch Integration successful!");
            Plugin.Instance.LogInfo("Twitch integration successfully attached to chat!");
            CHANNEL = e.Channel;
        }

        private void Client_OnDisconnected(object sender, OnDisconnectedArgs e)
        {
            Plugin.Instance.LogInfo("TwitchBot successfully disconnected from Twitch!");
            Plugin.Instance.DisplayNotif("Twitch bot disconnected!");
        }
    }
}