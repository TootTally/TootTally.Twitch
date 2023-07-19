using System;
using TootTally.Utils;
using TootTally.Utils.Helpers;
using TootTally.Graphics;
using TootTally.Graphics.Animation;
using UnityEngine;
using UnityEngine.PlayerLoop;
using UnityEngine.UI;
using System.Collections.Generic;

namespace TootTally.Twitch
{
    public static class RequestPanelManager
    {
        private const float MIN_POS_Y = -120;
        public static GameObject requestRowPrefab;
        private static List<RequestPanelRow> _requestRowList;
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

            _overlayCanvas = new GameObject("OverlayCanvas");
            Canvas canvas = _overlayCanvas.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = _overlayCanvas.AddComponent<CanvasScaler>();
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            _requestRowList = new List<RequestPanelRow>();

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

            _isPanelActive = false;
            _isInitialized = true;
        }

        public static void Update()
        {
            if (!_isInitialized) return; //just in case

            if (Input.GetKeyDown(KeyCode.F8))
                TogglePanel();

            if (Input.GetKeyUp(KeyCode.F7))
                AddRow("Test", "TestCharter", "Requested by grist");

            if (_isPanelActive && Input.mouseScrollDelta.y != 0 && _requestRowList.Count >= 6)
                _containerRect.anchoredPosition = new Vector2(_containerRect.anchoredPosition.x, Mathf.Clamp(_containerRect.anchoredPosition.y + Input.mouseScrollDelta.y * 35f, MIN_POS_Y, ((_requestRowList.Count - 6f) * 120f) + 120f));
        }

        public static void TogglePanel()
        {
            _isPanelActive = !_isPanelActive;
            if (_overlayPanel != null)
            {
                if (_panelAnimationBG != null)
                    _panelAnimationBG.Dispose();
                if (_panelAnimationFG != null)
                    _panelAnimationFG.Dispose();
                var targetVector = _isPanelActive ? Vector2.one : Vector2.zero;
                var animationTime = _isPanelActive ? 1f: 0.45f;
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

        public static void AddRow(string songName, string charterName, string requestedByName)
        {
            _requestRowList.Add(new RequestPanelRow(_overlayPanelContainer.transform, songName, charterName, requestedByName, DateTime.Now));
        }

        public static void Dispose()
        {
            if (!_isInitialized) return; //just in case too

            GameObject.DestroyImmediate(_overlayCanvas);
            GameObject.DestroyImmediate(_overlayPanel);
        }

        public static void Remove(RequestPanelRow row)
        {
            _requestRowList.Remove(row);
        }

        public static void SetRequestRowPrefab()
        {

            var tempRow = GameObjectFactory.CreateOverlayPanel(_overlayCanvas.transform, Vector2.zero, new Vector2(1200, 100), 5f, $"TwitchRequestRowTemp").transform.Find("FSLatencyPanel").gameObject;
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
            horizontalLayoutGroup.childAlignment = TextAnchor.UpperLeft;
            /*horizontalLayoutGroup.childControlHeight = horizontalLayoutGroup.childControlWidth = false;
            horizontalLayoutGroup.childForceExpandHeight = horizontalLayoutGroup.childForceExpandWidth = true;
            horizontalLayoutGroup.childScaleHeight = horizontalLayoutGroup.childScaleWidth = true;*/
            requestRowPrefab.transform.Find("LatencyFG").GetComponent<Image>().maskable = true;
            requestRowPrefab.transform.Find("LatencyBG").GetComponent<Image>().maskable = true;

            GameObject.DontDestroyOnLoad(requestRowPrefab);
            requestRowPrefab.SetActive(false);
        }

    }
}