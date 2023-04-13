using System;
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
        private string CHANNEL = "";

        public TwitchBot()
        {
            ConnectionCredentials credentials = new ConnectionCredentials("twitch_username", "access_token");
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
            Console.WriteLine($"{e.DateTime.ToString()}: {e.BotUsername} - {e.Data}");
        }
  
        private void Client_OnConnected(object sender, OnConnectedArgs e)
        {
            Console.WriteLine($"Connected to {e.AutoJoinChannel}");
        }
  
        private void Client_OnJoinedChannel(object sender, OnJoinedChannelArgs e)
        {
            client.SendMessage(e.Channel, "TootTally Twitch Integration successful!");
            CHANNEL = e.Channel;
        }
    }
}