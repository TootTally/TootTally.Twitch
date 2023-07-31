using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TootTally.Graphics;
using TootTally.Utils.APIServices;
using Microsoft.FSharp.Core;
using BaboonAPI.Hooks.Tracks;
using UnityEngine;
using TootTally.Utils;
using TwitchLib.Api.Helix.Models.Soundtrack;
using System.Security.Cryptography;

namespace TootTally.Twitch
{
    public class RequestPanelRow
    {
        private GameObject _requestRowContainer;
        private GameObject _requestRow;

        public Plugin.Request request { get; private set; }
        private SerializableClass.SongDataFromDB _chart;
        private DateTime _requestTime;

        public RequestPanelRow(Transform canvasTransform, Plugin.Request request, DateTime requestTime)
        {
            _chart = request.songData;
            this.request = request;
            _requestTime = requestTime;
            _requestRow = GameObject.Instantiate(RequestPanelManager.requestRowPrefab, canvasTransform);
            _requestRow.name = $"Request{_chart.name}";
            _requestRowContainer = _requestRow.transform.Find("LatencyFG/MainPage").gameObject;
            var t1 = GameObjectFactory.CreateSingleText(_requestRowContainer.transform, "SongName", _chart.name, GameTheme.themeColors.leaderboard.text);
            var t2 = GameObjectFactory.CreateSingleText(_requestRowContainer.transform, "Charter", (_chart.charter ?? "Unknown"), GameTheme.themeColors.leaderboard.text);
            var t3 = GameObjectFactory.CreateSingleText(_requestRowContainer.transform, "RequestedByName", request.requester, GameTheme.themeColors.leaderboard.text);
            var t4 = GameObjectFactory.CreateSingleText(_requestRowContainer.transform, "Time", requestTime.ToString(), GameTheme.themeColors.leaderboard.text);
            //fuck that shit :skull:
            t1.GetComponent<RectTransform>().sizeDelta = new Vector2(250, 64);
            t2.GetComponent<RectTransform>().sizeDelta = t3.GetComponent<RectTransform>().sizeDelta = new Vector2(200, 64);
            t4.GetComponent<RectTransform>().sizeDelta = new Vector2(150, 64);
            t1.overflowMode = t2.overflowMode = t3.overflowMode = t4.overflowMode = TMPro.TextOverflowModes.Ellipsis;

            if (FSharpOption<TromboneTrack>.get_IsNone(TrackLookup.tryLookup(_chart.track_ref)))
                GameObjectFactory.CreateCustomButton(_requestRowContainer.transform, Vector2.zero, new Vector2(68, 68), AssetManager.GetSprite("Download64.png"), "DownloadButton", DownloadChart);
            else
                GameObjectFactory.CreateCustomButton(_requestRowContainer.transform, Vector2.zero, new Vector2(68, 68), AssetManager.GetSprite("Check64.png"), "PlayButton", PlayChart);

            GameObjectFactory.CreateCustomButton(_requestRowContainer.transform, Vector2.zero, new Vector2(68, 68), AssetManager.GetSprite("Close64.png"), "SkipButton", RemoveFromPanel);
            GameObjectFactory.CreateCustomButton(_requestRowContainer.transform, Vector2.zero, new Vector2(68, 68), AssetManager.GetSprite("Block64.png"), "BlockButton", BlockChart);
            _requestRow.SetActive(true);
        }

        public void PlayChart()
        {
            // track is Some, found in current track list
            // TODO: Figure out how to either play the song from here or
            //       set the track in the song select to this specific track
            RequestPanelManager.SetTrackToTrackref(_chart.track_ref);
        }

        public void DownloadChart()
        {
            // track is None, could not find track in the current track list
            // Redirect them to the website and have them download the chart for now
            Application.OpenURL($"https://toottally.com/song/{request.song_id}/");
        }

        public void RemoveFromPanel()
        {
            RequestPanelManager.Remove(this);
            GameObject.DestroyImmediate(_requestRow);
        }

        public void BlockChart()
        {

        }
    }
}
