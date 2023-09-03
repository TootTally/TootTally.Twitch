using System;
using System.Collections.Generic;
using System.Linq;
using TootTally.Graphics;
using TootTally.Utils;
using UnityEngine;
using static TootTally.Twitch.Plugin;

namespace TootTally.Twitch
{
    public class RequestController : MonoBehaviour
    {
        public List<string> RequesterBlacklist { get; set; }
        private Stack<Notif> NotifStack;
        private Stack<UnprocessedRequest> RequestStack; // Unfinished request stack, only song ids here

        public void Awake()
        {
            NotifStack = new Stack<Notif>();
            RequestStack = new Stack<UnprocessedRequest>();
            RequesterBlacklist = new List<string>();
        }

        public void Update()
        {
            if (RequestPanelManager.isPlaying) return;

            if (RequestStack.TryPop(out UnprocessedRequest request))
            {
                Instance.LogInfo($"Attempting to get song data for ID {request.song_id}");
                Instance.StartCoroutine(TootTallyAPIService.GetSongDataFromDB(request.song_id, (songdata) =>
                {
                    Instance.LogInfo($"Obtained request by {request.requester} for song {songdata.author} - {songdata.name}");
                    DisplayNotif($"Requested song by {request.requester}: {songdata.author} - {songdata.name}");
                    var processed_request = new Request
                    {
                        requester = request.requester,
                        songData = songdata,
                        song_id = request.song_id,
                        date = DateTime.Now.ToString()
                    };
                    RequestPanelManager.AddRow(processed_request);
                }));
            }

            if (NotifStack.TryPop(out Notif notif))
            {
                Instance.LogInfo("Attempting to generate notification...");
                PopUpNotifManager.DisplayNotif(notif.message, notif.color);
            }
        }

        public void DisplayNotif(string message, bool isError = false)
        {
            Color color = isError ? GameTheme.themeColors.notification.errorText : GameTheme.themeColors.notification.defaultText;
            Notif notif = new Notif
            {
                message = message,
                color = color
            };
            NotifStack.Push(notif);
        }

        public void RequestSong(int song_id, string requester)
        {
            
            if (!RequesterBlacklist.Contains(requester))
            {
                if (RequestPanelManager.IsBlocked(song_id))
                {
                    Instance.Bot.client.SendMessage(Instance.Bot.CHANNEL, $"!Song #{song_id} is blocked.");
                    return;
                }
                else if (RequestPanelManager.IsDuplicate(song_id) && !RequestStack.Any(x => x.song_id == song_id))
                {
                    Instance.Bot.client.SendMessage(Instance.Bot.CHANNEL, $"!Song #{song_id} already requested.");
                    return;
                }
                UnprocessedRequest request = new UnprocessedRequest();
                request.song_id = song_id;
                request.requester = requester;
                Instance.LogInfo($"Accepted request {song_id} by {requester}.");
                Instance.Bot.client.SendMessage(Instance.Bot.CHANNEL, $"!Song #{song_id} successfully requested.");
                RequestStack.Push(request);
            }
        }

        public void Dispose()
        {
            NotifStack?.Clear();
            NotifStack = null;
            RequestStack?.Clear();
            RequestStack = null;
            RequesterBlacklist?.Clear();
            RequesterBlacklist = null;
        }

    }
}
