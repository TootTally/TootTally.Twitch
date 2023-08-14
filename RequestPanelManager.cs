using TootTally.Utils;
using TootTally.Utils.Helpers;
using TootTally.Graphics;
using TootTally.Graphics.Animation;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using static TootTally.Twitch.Plugin;

namespace TootTally.Twitch
{
    public static class RequestPanelManager
    {
        private const float MIN_POS_Y = -120;
        public static GameObject requestRowPrefab;
        public static LevelSelectController songSelectInstance;
        public static int songIndex;
        public static string songTrackref;
        public static bool isPlaying;
        private static List<RequestPanelRow> _requestRowList;
        private static List<Request> _requestList;
        private static List<int> _songIDHistory;
        public static int currentSongID;


        private static RectTransform _containerRect;
        private static CustomAnimation _panelAnimationFG, _panelAnimationBG;

        private static GameObject _overlayPanel;
        private static GameObject _overlayCanvas;
        private static GameObject _overlayPanelContainer;
        private static bool _isPanelActive;
        private static bool _isInitialized;
        public static void Initialize()
        {
            if (_isInitialized) return;

            _overlayCanvas = new GameObject("TwitchOverlayCanvas");
            Canvas canvas = _overlayCanvas.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = _overlayCanvas.AddComponent<CanvasScaler>();
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            _requestRowList = new List<RequestPanelRow>();
            _requestList = new List<Request>();
            _songIDHistory = new List<int>();

            GameObject.DontDestroyOnLoad(_overlayCanvas);

            _overlayPanel = GameObjectFactory.CreateOverlayPanel(_overlayCanvas.transform, Vector2.zero, new Vector2(1700, 900), 20f, "TwitchOverlayPanel");
            _overlayPanelContainer = _overlayPanel.transform.Find("FSLatencyPanel/LatencyFG/MainPage").gameObject;
            _overlayPanel.transform.Find("FSLatencyPanel/LatencyFG").localScale = Vector2.zero;
            _overlayPanel.transform.Find("FSLatencyPanel/LatencyBG").localScale = Vector2.zero;
            _containerRect = _overlayPanelContainer.GetComponent<RectTransform>();
            _containerRect.anchoredPosition = Vector2.zero;
            _containerRect.sizeDelta = new Vector2(1700, 900);
            var verticalLayout = _overlayPanelContainer.GetComponent<VerticalLayoutGroup>();
            verticalLayout.padding = new RectOffset(20, 20, 20, 20);
            verticalLayout.spacing = 120f;
            verticalLayout.childAlignment = TextAnchor.UpperCenter;
            verticalLayout.childControlHeight = verticalLayout.childControlWidth = true;
            _overlayPanelContainer.transform.parent.gameObject.AddComponent<Mask>();
            GameObjectFactory.DestroyFromParent(_overlayPanelContainer.transform.parent.gameObject, "subtitle");
            GameObjectFactory.DestroyFromParent(_overlayPanelContainer.transform.parent.gameObject, "title");
            var text = GameObjectFactory.CreateSingleText(_overlayPanelContainer.transform, "title", "Twitch Requests", GameTheme.themeColors.leaderboard.text);
            text.fontSize = 60f;
            _overlayPanel.SetActive(false);
            SetRequestRowPrefab();

            _requestList = FileManager.GetRequestsFromFile();
            _requestList.ForEach(AddRowFromFile);

            _isPanelActive = false;
            _isInitialized = true;
            isPlaying = false;
        }

        public static void Update()
        {
            if (!_isInitialized) return; //just in case

            if (Input.GetKeyDown(KeyCode.F8))
                TogglePanel();

            if (_isPanelActive && Input.mouseScrollDelta.y != 0 && _requestRowList.Count >= 6)
                _containerRect.anchoredPosition = new Vector2(_containerRect.anchoredPosition.x, Mathf.Clamp(_containerRect.anchoredPosition.y + Input.mouseScrollDelta.y * 35f, MIN_POS_Y, ((_requestRowList.Count - 6f) * 120f) + 120f));
        }

        public static void TogglePanel()
        {
            _isPanelActive = !_isPanelActive;
            if (_overlayPanel != null)
            {
                _panelAnimationBG?.Dispose();
                _panelAnimationFG?.Dispose();
                var targetVector = _isPanelActive ? Vector2.one : Vector2.zero;
                var animationTime = _isPanelActive ? 1f : 0.45f;
                var secondDegreeAnimationFG = _isPanelActive ? new EasingHelper.SecondOrderDynamics(1.75f, 1f, 0f) : new EasingHelper.SecondOrderDynamics(3.2f, 1f, 0.25f);
                var secondDegreeAnimationBG = _isPanelActive ? new EasingHelper.SecondOrderDynamics(1.75f, 1f, 0f) : new EasingHelper.SecondOrderDynamics(3.2f, 1f, 0.25f);
                _panelAnimationFG = AnimationManager.AddNewScaleAnimation(_overlayPanel.transform.Find("FSLatencyPanel/LatencyFG").gameObject, targetVector, animationTime, secondDegreeAnimationFG);
                _panelAnimationBG = AnimationManager.AddNewScaleAnimation(_overlayPanel.transform.Find("FSLatencyPanel/LatencyBG").gameObject, targetVector, animationTime, secondDegreeAnimationBG, (sender) =>
                {
                    if (!_isPanelActive)
                        _overlayPanel.SetActive(_isPanelActive);
                });
                if (_isPanelActive)
                    _overlayPanel.SetActive(_isPanelActive);
            }
        }

        public static void AddRow(Request request)
        {
            _requestList.Add(request);
            UpdateSaveRequestFile();
            _requestRowList.Add(new RequestPanelRow(_overlayPanelContainer.transform, request));
        }

        public static void AddRowFromFile(Request request) =>
            _requestRowList.Add(new RequestPanelRow(_overlayPanelContainer.transform, request));

        public static void Dispose()
        {
            if (!_isInitialized) return; //just in case too

            GameObject.DestroyImmediate(_overlayCanvas);
            GameObject.DestroyImmediate(_overlayPanel);
            _isInitialized = false;
        }

        public static void Remove(RequestPanelRow row)
        {
            _requestList.Remove(row.request);
            UpdateSaveRequestFile();
            _requestRowList.Remove(row);
        }

        public static void Remove(string trackref)
        {
            var request = _requestRowList.Find(r => r.request.songData.track_ref == trackref);
            if (request == null) return;

            request.RemoveFromPanel();
            RequestPanelManager.AddSongIDToHistory(request.request.song_id);
            PopUpNotifManager.DisplayNotif($"Fulfilled request from {request.request.requester}", GameTheme.themeColors.notification.defaultText);
        }

        public static void SetRequestRowPrefab()
        {

            var tempRow = GameObjectFactory.CreateOverlayPanel(_overlayCanvas.transform, Vector2.zero, new Vector2(1200, 84), 5f, $"TwitchRequestRowTemp").transform.Find("FSLatencyPanel").gameObject;
            requestRowPrefab = GameObject.Instantiate(tempRow);
            GameObject.DestroyImmediate(tempRow.gameObject);

            requestRowPrefab.name = "RequestRowPrefab";
            requestRowPrefab.transform.localScale = Vector3.one;

            requestRowPrefab.GetComponent<Image>().maskable = true;
            var container = requestRowPrefab.transform.Find("LatencyFG/MainPage").gameObject;
            container.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
            container.GetComponent<RectTransform>().sizeDelta = new Vector2(1200, 100);
            GameObject.DestroyImmediate(container.transform.parent.Find("subtitle").gameObject);
            GameObject.DestroyImmediate(container.transform.parent.Find("title").gameObject);
            GameObject.DestroyImmediate(container.GetComponent<VerticalLayoutGroup>());
            var horizontalLayoutGroup = container.AddComponent<HorizontalLayoutGroup>();
            horizontalLayoutGroup.padding = new RectOffset(20, 20, 20, 20);
            horizontalLayoutGroup.spacing = 20f;
            horizontalLayoutGroup.childAlignment = TextAnchor.MiddleLeft;
            horizontalLayoutGroup.childControlHeight = horizontalLayoutGroup.childControlWidth = false;
            horizontalLayoutGroup.childForceExpandHeight = horizontalLayoutGroup.childForceExpandWidth = false;
            requestRowPrefab.transform.Find("LatencyFG").GetComponent<Image>().maskable = true;
            requestRowPrefab.transform.Find("LatencyBG").GetComponent<Image>().maskable = true;

            GameObject.DontDestroyOnLoad(requestRowPrefab);
            requestRowPrefab.SetActive(false);
        }

        public static void SetTrackToTrackref(string trackref)
        {
            if (songSelectInstance == null) return;
            for (int i = 0; i < songSelectInstance.alltrackslist.Count; i++)
            {
                if (songSelectInstance.alltrackslist[i].trackref == trackref)
                {
                    if (i - songIndex != 0)
                    {
                        // Only advance songs if we're not on the same song already
                        songSelectInstance.advanceSongs(i - songIndex, true);
                    }
                    TogglePanel();
                    return;
                }
            }
        }

        public static void AddSongIDToHistory(int id) => _songIDHistory.Add(id);
        public static string GetSongIDHistoryString() => _songIDHistory.Count > 0 ? string.Join(", ", _songIDHistory) : "No songs history recorded";

        public static string GetSongQueueIDString() => _requestList.Count > 0 ? string.Join(", ", _requestList.Select(x => x.song_id)) : "No songs requested";
        public static string GetLastSongPlayed() => _songIDHistory.Count > 0 ? $"https://toottally.com/song/{_songIDHistory.Last()}" : "No song played";

        public static bool CheckDuplicate(UnprocessedRequest request)
        {
            foreach (var reqRow in _requestRowList)
            {
                if (reqRow.request.song_id == request.song_id) return true;
            }
            return false;
        }

        public static void UpdateSaveRequestFile()
        {
            FileManager.SaveToFile(_requestList);
        }
    }
}