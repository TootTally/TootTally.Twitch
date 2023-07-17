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
            _containerRect = _overlayPanelContainer.GetComponent<RectTransform>();
            var verticalLayout = _overlayPanelContainer.GetComponent<VerticalLayoutGroup>();
            verticalLayout.padding = new RectOffset(20, 20, 20, 20);
            verticalLayout.spacing = 120f;
            verticalLayout.childAlignment = TextAnchor.UpperCenter;
            verticalLayout.childControlHeight = verticalLayout.childControlWidth = true;
            _overlayPanelContainer.transform.parent.gameObject.AddComponent<Mask>();
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
                _requestRowList.Add(new RequestPanelRow(_overlayPanelContainer.transform, "Test", "TestCharter", DateTime.Now));

            if (_isPanelActive && Input.mouseScrollDelta.y != 0 && _requestRowList.Count >= 7)
                _containerRect.anchoredPosition = new Vector2(_containerRect.anchoredPosition.x, Mathf.Clamp(_containerRect.anchoredPosition.y + Input.mouseScrollDelta.y * 25f, MIN_POS_Y, (_requestRowList.Count - 7f) * 120f));
        }

        public static void TogglePanel()
        {
            _isPanelActive = !_isPanelActive;
            _overlayPanel?.SetActive(_isPanelActive);
        }


        public static void Dispose()
        {
            if (!_isInitialized) return; //just in case too

            GameObject.DestroyImmediate(_overlayCanvas);
            GameObject.DestroyImmediate(_overlayPanel);
        }

        public static void SetRequestRowPrefab()
        {
            
            var tempRow = GameObjectFactory.CreateOverlayPanel(_overlayCanvas.transform, Vector2.zero, new Vector2(900,100), 5f, $"TwitchRequestRowTemp").transform.Find("FSLatencyPanel").gameObject;
            requestRowPrefab = GameObject.Instantiate(tempRow);
            GameObject.DestroyImmediate(tempRow.gameObject);

            requestRowPrefab.name = "RequestRowPrefab";
            requestRowPrefab.transform.localScale = Vector3.one;

            requestRowPrefab.GetComponent<Image>().maskable = true;
            var container = requestRowPrefab.transform.Find("LatencyFG/MainPage").gameObject;
            GameObject.DestroyImmediate(container.GetComponent<VerticalLayoutGroup>());
            container.AddComponent<HorizontalLayoutGroup>();
            requestRowPrefab.transform.Find("LatencyFG").GetComponent<Image>().maskable = true;
            requestRowPrefab.transform.Find("LatencyBG").GetComponent<Image>().maskable = true;

            GameObject.DontDestroyOnLoad(requestRowPrefab);
            requestRowPrefab.SetActive(false);
        }

    }
}