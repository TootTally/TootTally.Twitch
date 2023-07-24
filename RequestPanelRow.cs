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
            GameObjectFactory.CreateSingleText(_requestRowContainer.transform, "SongName", _chart.name, GameTheme.themeColors.leaderboard.text);
            GameObjectFactory.CreateSingleText(_requestRowContainer.transform, "Charter", _chart.charter, GameTheme.themeColors.leaderboard.text);
            GameObjectFactory.CreateSingleText(_requestRowContainer.transform, "RequestedByName", request.requester, GameTheme.themeColors.leaderboard.text);
            GameObjectFactory.CreateSingleText(_requestRowContainer.transform, "Time", requestTime.ToString(), GameTheme.themeColors.leaderboard.text);
            GameObjectFactory.CreateCustomButton(_requestRowContainer.transform, Vector2.zero, new Vector2(120,60), "Play", "PlayButton", PlayOrDownloadChart);
            GameObjectFactory.CreateCustomButton(_requestRowContainer.transform, Vector2.zero, new Vector2(120,60), "Skip", "SkipButton", RemoveFromPanel);
            _requestRow.SetActive(true);
        }

        public void PlayOrDownloadChart()
        {
            // TODO: Check if the chart with this trackref has the same hash as what we're looking for
            var track = TrackLookup.tryLookup(_chart.track_ref);
            if (FSharpOption<TromboneTrack>.get_IsNone(track)) {
                // track is None, could not find track in the current track list
                // Redirect them to the website and have them download the chart for now
                Application.OpenURL($"https://toottally.com/song/{request.song_id}/");
            }
            else {
                // track is Some, found in current track list
                // TODO: Figure out how to either play the song from here or
                //       set the track in the song select to this specific track
                RequestPanelManager.SetTrackToTrackref(_chart.track_ref);
            }
        }

        public void RemoveFromPanel()
        {
            RequestPanelManager.Remove(this);
            GameObject.DestroyImmediate(_requestRow);
        }
    }
}
