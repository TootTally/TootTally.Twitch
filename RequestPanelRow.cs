using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TootTally.Graphics;
using TootTally.Utils;
using UnityEngine;

namespace TootTally.Twitch
{
    public class RequestPanelRow
    {
        private GameObject _requestRowContainer;
        private GameObject _requestRow;

        private Plugin.Request _request;
        private SerializableClass.SongDataFromDB _chart;
        private DateTime _requestTime;

        public RequestPanelRow(Transform canvasTransform, Plugin.Request request, DateTime requestTime) 
        {
            _chart = request.songData;
            _request = request;
            _requestTime = requestTime;
            _requestRow = GameObject.Instantiate(RequestPanelManager.requestRowPrefab, canvasTransform);
            _requestRow.name = $"Request{_chart.name}";
            _requestRowContainer = _requestRow.transform.Find("LatencyFG/MainPage").gameObject;
            GameObjectFactory.CreateSingleText(_requestRowContainer.transform, "SongName", _chart.name, GameTheme.themeColors.leaderboard.text);
            GameObjectFactory.CreateSingleText(_requestRowContainer.transform, "Charter", _chart.charter, GameTheme.themeColors.leaderboard.text);
            GameObjectFactory.CreateSingleText(_requestRowContainer.transform, "RequestedByName", request.requester, GameTheme.themeColors.leaderboard.text);
            GameObjectFactory.CreateSingleText(_requestRowContainer.transform, "Time", requestTime.ToString(), GameTheme.themeColors.leaderboard.text);
            GameObjectFactory.CreateCustomButton(_requestRowContainer.transform, Vector2.zero, new Vector2(120,60), "Play", "PlayButton");
            GameObjectFactory.CreateCustomButton(_requestRowContainer.transform, Vector2.zero, new Vector2(120,60), "Skip", "SkipButton", RemoveFromPanel);
            _requestRow.SetActive(true);
        }

        public void RemoveFromPanel()
        {
            RequestPanelManager.Remove(this);
            GameObject.DestroyImmediate(_requestRow);
        }
    }
}
