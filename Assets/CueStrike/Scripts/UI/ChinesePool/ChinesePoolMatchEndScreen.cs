using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using CueStrike.Gameplay.ChinesePool;

namespace CueStrike.UI.ChinesePool
{
    /// <summary>
    /// R25 — WINNER screen shown when the match is over.
    /// Subscribes to ChinesePoolGameManager.OnMatchOver, displays the winner,
    /// and offers "เล่นอีกครั้ง" (same Best-of) / "กลับเมนู" (Title scene).
    ///
    /// Fail-safe: if GameManager is missing at start, it keeps trying each frame
    /// (bounded) so the screen can self-attach when the manager is created late.
    /// UI built in code (no prefab dependency).
    /// </summary>
    public class ChinesePoolMatchEndScreen : MonoBehaviour
    {
        [Header("Optional References")]
        [Tooltip("Canvas หลัก. ถ้า null จะหา Canvas ตัวแรกในฉาก")]
        public Canvas targetCanvas;

        [Tooltip("ฉากเมนูหลักที่ใช้กลับ (default: Title_NoksGrandHall)")]
        public string mainMenuSceneName = "Title_NoksGrandHall";

        private GameObject _panelRoot;
        private Text _winnerText;
        private int _lastBestOf = 5;
        private int _lookupAttempts;

        private void Start()
        {
            SubscribeToManager();
        }

        private void Update()
        {
            // Retry a few times in case the GameManager is created late (auto-create pattern).
            if (ChinesePoolGameManager.Instance == null && _lookupAttempts < 30)
            {
                _lookupAttempts++;
                return;
            }
            if (ChinesePoolGameManager.Instance != null && !IsSubscribed)
            {
                SubscribeToManager();
            }
        }

        private bool IsSubscribed { get; set; }

        private void SubscribeToManager()
        {
            var gm = ChinesePoolGameManager.Instance;
            if (gm == null)
            {
                Debug.LogWarning("[ChinesePoolMatchEndScreen] ChinesePoolGameManager not found — WINNER screen idle.");
                return;
            }
            gm.OnMatchOver -= HandleMatchOver; // prevent double-subscribe
            gm.OnMatchOver += HandleMatchOver;
            IsSubscribed = true;
            _lastBestOf = Mathf.Max(1, gm.maxFrames);
            Debug.Log("[ChinesePoolMatchEndScreen] Subscribed to OnMatchOver.");
        }

        private void OnDisable()
        {
            var gm = ChinesePoolGameManager.Instance;
            if (gm != null)
            {
                gm.OnMatchOver -= HandleMatchOver;
            }
            IsSubscribed = false;
        }

        private void OnDestroy()
        {
            var gm = ChinesePoolGameManager.Instance;
            if (gm != null)
            {
                gm.OnMatchOver -= HandleMatchOver;
            }
        }

        private void HandleMatchOver()
        {
            var gm = ChinesePoolGameManager.Instance;
            if (gm == null) return;

            int winnerIndex = (gm.framesWonPlayer1 > gm.framesWonPlayer2) ? 0 : 1;
            string winnerText = $"WINNER — Player {winnerIndex + 1}!";
            _lastBestOf = Mathf.Max(1, gm.maxFrames);

            ChinesePoolUIManager.Instance?.ShowMatchOver(winnerText);

            if (!TryEnsureReferences())
            {
                Debug.LogWarning("[ChinesePoolMatchEndScreen] Missing Canvas — cannot show WINNER screen.");
                return;
            }

            if (_winnerText != null) _winnerText.text = winnerText;
            _panelRoot.SetActive(true);
            Debug.Log($"[ChinesePoolMatchEndScreen] {winnerText} (Best of {_lastBestOf}).");
        }

        // ---- Button handlers ----

        public void OnRematchClicked()
        {
            _panelRoot?.SetActive(false);
            var gm = ChinesePoolGameManager.Instance;
            if (gm != null)
            {
                gm.StartNewMatch(_lastBestOf);
                ChinesePoolUIManager.Instance?.InitializeGame();
                Debug.Log($"[ChinesePoolMatchEndScreen] Rematch started — Best of {_lastBestOf}.");
            }
        }

        public void OnBackToMenuClicked()
        {
            Debug.Log($"[ChinesePoolMatchEndScreen] Loading menu scene: {mainMenuSceneName}");
            SceneManager.LoadScene(mainMenuSceneName);
        }

        // ---- UI building ----

        private bool TryEnsureReferences()
        {
            if (targetCanvas == null)
            {
                targetCanvas = FindAnyObjectByType<Canvas>();
            }

            if (targetCanvas == null)
            {
                var canvasGO = new GameObject("MatchEndCanvas");
                canvasGO.transform.SetParent(transform, false);
                targetCanvas = canvasGO.AddComponent<Canvas>();
                targetCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasGO.AddComponent<CanvasScaler>();
                canvasGO.AddComponent<GraphicRaycaster>();
            }

            if (_panelRoot == null)
            {
                _panelRoot = BuildPanel(targetCanvas.transform);
                if (_panelRoot == null) return false;
            }

            return true;
        }

        private GameObject BuildPanel(Transform parent)
        {
            try
            {
                var root = new GameObject("MatchEnd_Panel");
                root.transform.SetParent(parent, false);
                root.SetActive(false); // hidden until match over

                var rect = root.AddComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(560f, 420f);

                // Background
                var bg = new GameObject("Background");
                bg.transform.SetParent(root.transform, false);
                var bgImg = bg.AddComponent<Image>();
                bgImg.color = new Color(0.05f, 0.09f, 0.14f, 0.97f);
                var bgRect = bg.GetComponent<RectTransform>();
                bgRect.anchorMin = Vector2.zero;
                bgRect.anchorMax = Vector2.one;
                bgRect.offsetMin = Vector2.zero;
                bgRect.offsetMax = Vector2.zero;

                // WINNER text
                var winnerGO = new GameObject("WinnerText");
                winnerGO.transform.SetParent(root.transform, false);
                _winnerText = winnerGO.AddComponent<Text>();
                _winnerText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                _winnerText.fontSize = 52;
                _winnerText.fontStyle = FontStyle.Bold;
                _winnerText.color = new Color(1f, 0.84f, 0.4f);
                _winnerText.alignment = TextAnchor.MiddleCenter;
                _winnerText.text = "WINNER!";
                var winnerRect = _winnerText.rectTransform;
                winnerRect.anchorMin = new Vector2(0f, 0.68f);
                winnerRect.anchorMax = new Vector2(1f, 0.9f);
                winnerRect.offsetMin = new Vector2(30f, 0f);
                winnerRect.offsetMax = new Vector2(-30f, 0f);

                // Subtitle
                var subGO = new GameObject("Subtitle");
                subGO.transform.SetParent(root.transform, false);
                var sub = subGO.AddComponent<Text>();
                sub.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                sub.fontSize = 24;
                sub.color = new Color(0.85f, 0.85f, 0.9f);
                sub.alignment = TextAnchor.MiddleCenter;
                sub.text = "แมตช์จบแล้ว — เล่นใหม่หรือกลับเมนู";
                var subRect = sub.rectTransform;
                subRect.anchorMin = new Vector2(0f, 0.55f);
                subRect.anchorMax = new Vector2(1f, 0.64f);
                subRect.offsetMin = new Vector2(30f, 0f);
                subRect.offsetMax = new Vector2(-30f, 0f);

                // Buttons
                CreateButton(root.transform, "RematchButton", "เล่นอีกครั้ง", new Vector2(0.5f, 0.35f), OnRematchClicked);
                CreateButton(root.transform, "MenuButton", "กลับเมนู", new Vector2(0.5f, 0.18f), OnBackToMenuClicked);

                return root;
            }
            catch (Exception e)
            {
                Debug.LogError($"[ChinesePoolMatchEndScreen] Failed to build panel: {e.Message}");
                return null;
            }
        }

        private void CreateButton(Transform parent, string name, string label, Vector2 anchor, UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);

            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(360f, 68f);

            var img = go.AddComponent<Image>();
            img.color = new Color(0.25f, 0.35f, 0.55f, 1f);

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(onClick);

            var labelGO = new GameObject("Label");
            labelGO.transform.SetParent(go.transform, false);
            var labelText = labelGO.AddComponent<Text>();
            labelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            labelText.fontSize = 26;
            labelText.color = Color.white;
            labelText.alignment = TextAnchor.MiddleCenter;
            labelText.text = label;
            var labelRect = labelText.rectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
        }
    }
}
