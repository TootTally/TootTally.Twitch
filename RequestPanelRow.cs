using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TootTally.Graphics;
using UnityEngine;

namespace TootTally.Twitch
{
    public class RequestPanelRow
    {
        private GameObject _requestRowContainer;
        private GameObject _requestRow;

        private string _songName;
        private string _charter;
        private DateTime _requestTime;

        public RequestPanelRow(Transform canvasTransform, string songName, string charter, string requestedByName, DateTime requestTime) 
        {
            _songName = songName;
            _charter = charter;
            _requestTime = requestTime;
            _requestRow = GameObject.Instantiate(RequestPanelManager.requestRowPrefab, canvasTransform);
            _requestRow.name = $"Request{songName}";
            _requestRowContainer = _requestRow.transform.Find("LatencyFG/MainPage").gameObject;
            GameObjectFactory.CreateSingleText(_requestRowContainer.transform, "SongName", songName, GameTheme.themeColors.leaderboard.text);
            GameObjectFactory.CreateSingleText(_requestRowContainer.transform, "Charter", charter, GameTheme.themeColors.leaderboard.text);
            GameObjectFactory.CreateSingleText(_requestRowContainer.transform, "RequestedByName", requestedByName, GameTheme.themeColors.leaderboard.text);
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
