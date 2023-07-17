using System;
using TootTally.Utils;
using TootTally.Utils.Helpers;
using TootTally.Graphics;
using TootTally.Graphics.Animation;
using UnityEngine;
using UnityEngine.PlayerLoop;
using UnityEngine.UI;

namespace TootTally.Twitch
{
    public static class RequestPanelManager
    {
        private static GameObject _overlayPanel;
        private static GameObject _overlayCanvas;
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

            GameObject.DontDestroyOnLoad(_overlayCanvas);


            _overlayPanel = GameObjectFactory.CreateOverlayPanel(_overlayCanvas.transform, Vector2.zero, scaler.referenceResolution, "TwitchOverlayPanel");
            _overlayPanel.SetActive(false);
            _isPanelActive = false;
            _isInitialized = true;
        }

        public static void Update()
        {
            if (!_isInitialized) return;

            if (Input.GetKeyDown(KeyCode.F8))
                TogglePanel();
        }

        public static void TogglePanel()
        {
            _isPanelActive = !_isPanelActive;
            _overlayPanel?.SetActive(_isPanelActive);
        }


        public static void Dispose()
        {
            if (!_isInitialized) return; //just in case

            GameObject.DestroyImmediate(_overlayCanvas);
            GameObject.DestroyImmediate(_overlayPanel);
        }
    }
}