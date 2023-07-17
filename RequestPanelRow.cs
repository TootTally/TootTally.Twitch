using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

        public RequestPanelRow(Transform canvasTransform, string songName, string charter, DateTime requestTime) 
        {
            _songName = songName;
            _charter = charter;
            _requestTime = requestTime;
            _requestRow = GameObject.Instantiate(RequestPanelManager.requestRowPrefab, canvasTransform);
            _requestRow.name = $"Request{songName}";
            _requestRowContainer = _requestRow.transform.Find("LatencyFG/MainPage").gameObject;
            GameObject.DestroyImmediate(_requestRowContainer.transform.parent.Find("subtitle").gameObject);
            GameObject.DestroyImmediate(_requestRowContainer.transform.parent.Find("title").gameObject);
            _requestRow.SetActive(true);
        }
    }
}
