﻿using TootTally.Graphics;
using TootTally.Utils.APIServices;
using Microsoft.FSharp.Core;
using BaboonAPI.Hooks.Tracks;
using UnityEngine;
using TootTally.Utils;
using BepInEx;
using System.IO;
using TootTally.Utils.Helpers;
using Microsoft.FSharp.Collections;

namespace TootTally.Twitch
{
    public class RequestPanelRow
    {
        private string _downloadLink;
        private GameObject _requestRowContainer;
        private GameObject _requestRow;
        private GameObject _downloadButton;
        private ProgressBar _progressBar;
        private TracksLoaderListener _reloadListener;
        public Plugin.Request request { get; private set; }
        private SerializableClass.SongDataFromDB _chart;

        public RequestPanelRow(Transform canvasTransform, Plugin.Request request)
        {
            _chart = request.songData;
            this.request = request;
            _requestRow = GameObject.Instantiate(RequestPanelManager.requestRowPrefab, canvasTransform);
            _requestRow.name = $"Request{_chart.name}";
            _requestRowContainer = _requestRow.transform.Find("LatencyFG/MainPage").gameObject;
            var t1 = GameObjectFactory.CreateSingleText(_requestRowContainer.transform, "SongName", _chart.name, GameTheme.themeColors.leaderboard.text);
            var t2 = GameObjectFactory.CreateSingleText(_requestRowContainer.transform, "Charter", (_chart.charter ?? "Unknown"), GameTheme.themeColors.leaderboard.text);
            var t3 = GameObjectFactory.CreateSingleText(_requestRowContainer.transform, "RequestedByName", request.requester, GameTheme.themeColors.leaderboard.text);
            var t4 = GameObjectFactory.CreateSingleText(_requestRowContainer.transform, "Time", request.date, GameTheme.themeColors.leaderboard.text);
            //fuck that shit :skull:
            t1.GetComponent<RectTransform>().sizeDelta = new Vector2(250, 64);
            t2.GetComponent<RectTransform>().sizeDelta = t3.GetComponent<RectTransform>().sizeDelta = new Vector2(200, 64);
            t4.GetComponent<RectTransform>().sizeDelta = new Vector2(150, 64);
            t1.overflowMode = t2.overflowMode = t3.overflowMode = t4.overflowMode = TMPro.TextOverflowModes.Ellipsis;

            if (FSharpOption<TromboneTrack>.get_IsNone(TrackLookup.tryLookup(_chart.track_ref)))
            {
                _downloadLink = FileHelper.GetDownloadLinkFromSongData(_chart);
                if (_downloadLink != null)
                {
                    _downloadButton = GameObjectFactory.CreateCustomButton(_requestRowContainer.transform, Vector2.zero, new Vector2(68, 68), AssetManager.GetSprite("Download64.png"), "DownloadButton", DownloadChart).gameObject;
                    _progressBar = GameObjectFactory.CreateProgressBar(_requestRow.transform.Find("LatencyFG"), Vector2.zero, new Vector2(900, 20), false, "ProgressBar");
                }
                else
                    _downloadButton = GameObjectFactory.CreateCustomButton(_requestRowContainer.transform, Vector2.zero, new Vector2(68, 68), AssetManager.GetSprite("global64.png"), "OpenLeaderboardButton", () => Application.OpenURL($"https://toottally.com/song/{request.songData.id}/")).gameObject;

            }
            else
                GameObjectFactory.CreateCustomButton(_requestRowContainer.transform, Vector2.zero, new Vector2(68, 68), AssetManager.GetSprite("Check64.png"), "PlayButton", PlayChart);

            GameObjectFactory.CreateCustomButton(_requestRowContainer.transform, Vector2.zero, new Vector2(68, 68), AssetManager.GetSprite("Close64.png"), "SkipButton", RemoveFromPanel);
            GameObjectFactory.CreateCustomButton(_requestRowContainer.transform, Vector2.zero, new Vector2(68, 68), AssetManager.GetSprite("Block64.png"), "BlockButton", BlockChart);
            _requestRow.SetActive(true);
        }

        public void PlayChart()
        {
            RequestPanelManager.currentSongID = request.song_id;
            RequestPanelManager.SetTrackToTrackref(_chart.track_ref);
        }

        public void DownloadChart()
        {
            _downloadButton.SetActive(false);
            Plugin.Instance.StartCoroutine(TootTallyAPIService.DownloadZipFromServer(_downloadLink, _progressBar, data =>
            {
                if (data != null)
                {
                    string downloadDir = Path.Combine(Path.GetDirectoryName(Plugin.Instance.Info.Location), "Downloads/");
                    string fileName = $"{_chart.id}.zip";
                    FileHelper.WriteBytesToFile(downloadDir, fileName, data);

                    string source = Path.Combine(downloadDir, fileName);
                    string destination = Path.Combine(Paths.BepInExRootPath, "CustomSongs/");
                    FileHelper.ExtractZipToDirectory(source, destination);

                    FileHelper.DeleteFile(downloadDir, fileName);

                    _reloadListener = new TracksLoaderListener(this);
                    TracksLoadedEvent.EVENT.Register(_reloadListener);

                    PopUpNotifManager.DisplayNotif("Reloading Songs...");
                    TootTally.Plugin.Instance.Invoke("ReloadTracks", .5f);

                }
                else
                {
                    PopUpNotifManager.DisplayNotif("Download not available.");
                    _downloadButton.SetActive(true);
                }
            }));
        }

        public class TracksLoaderListener : TracksLoadedEvent.Listener
        {
            private RequestPanelRow _row;
            public TracksLoaderListener(RequestPanelRow row)
            {
                _row = row;
            }

            public void OnTracksLoaded(FSharpList<TromboneTrack> value)
            {
                //Dunno why I have to call this twice 
                //_row.PlayChart();
                //_row.PlayChart();
                _row.UnsubscribeLoaderEvent();
            }
        }

        public void UnsubscribeLoaderEvent()
        {
            if (_reloadListener != null)
                TracksLoadedEvent.EVENT.Unregister(_reloadListener);
            _reloadListener = null;
        }

        public void RemoveFromPanel()
        {
            RequestPanelManager.Remove(this);
            GameObject.DestroyImmediate(_requestRow);
        }

        public void BlockChart()
        {
            RequestPanelManager.AddToBlockList(_chart.id);
            RemoveFromPanel();
        }
    }
}
