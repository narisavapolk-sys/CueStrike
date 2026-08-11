using System;
using UnityEngine;
using UnityEngine.UI;
using CueStrike.Gameplay.ChinesePool;

namespace CueStrike.UI.ChinesePool
{
    /// <summary>
    /// R25 — Match setup dialog (World-Space VR panel) shown before a match starts.
    /// Lets the player agree with the AI on the match condition:
    ///   Single Frame (= Best of 1) / Best of 3 / Best of 5 / Best of 7 / Practice (no end)
    /// Coach-approved: "UI Dialog หน้าต่างเลือกก่อนเริ่มเกม ... ผู้เล่นกดยืนยันเลือกได้เองผ่านปุ่ม VR"
    ///
    /// Fail-safe: no GameManager / no Canvas → log warning, never block the scene.
    /// UI is built in code (no prefab dependency), same convention as R24.
    /// </summary>
    public class ChinesePoolMatchSetupUI : MonoBehaviour
    {
        [Header("Optional References")]
        [Tooltip("Canvas หลัก (World-Space VR friendly). ถ้า null จะหา Canvas ตัวแรกในฉาก")]
        public Canvas targetCanvas;

        [Tooltip("Best-of values for the buttons. 0 = Practice. Edit in Inspector if needed.")]
        public int[] bestOfOptions = { 1, 3, 5, 7, 0 };

        [Tooltip("ป้ายชื่อปุ่ม ตามลำดับเดียวกับ bestOfOptions")]
        public string[] optionLabels = { "Single Frame", "Best of 3", "Best of 5", "Best of 7", "Practice" };

        private GameObject _panelRoot;
        private bool _shown;

        /// <summary>panel กำลังแสดงอยู่หรือไม่</summary>
        public bool IsShown => _shown;

        private void Start()
        {
            ShowSetup();
        }

        private void OnDestroy()
        {
            if (_panelRoot != null)
            {
                Destroy(_panelRoot);
            }
        }

        /// <summary>แสดง panel เลือกเงื่อนไข (guard: ถ้าแสดงอยู่แล้ว / เกมเริ่มแล้ว → ข้าม)</summary>
        public void ShowSetup()
        {
            if (_shown) return;

            var gm = ChinesePoolGameManager.Instance;
            if (gm != null && gm.currentPhase != ChinesePoolMatchState.Waiting)
            {
                // Match already in progress — don't hijack it.
                Debug.Log("[ChinesePoolMatchSetupUI] Match already started — skipping setup dialog.");
                return;
            }

            if (!TryEnsureReferences())
            {
                Debug.LogWarning("[ChinesePoolMatchSetupUI] Missing Canvas — skipping setup dialog (fail-safe).");
                return;
            }

            _shown = true;
            if (_panelRoot != null) _panelRoot.SetActive(true);
            Debug.Log("[ChinesePoolMatchSetupUI] Match setup dialog shown.");
        }

        /// <summary>ปิด panel (เรียกเมื่อเลือกเงื่อนไขแล้ว)</summary>
        public void HideSetup()
        {
            _shown = false;
            if (_panelRoot != null) _panelRoot.SetActive(false);
        }

        // ---- Button handlers ----

        private void OnOptionSelected(int bestOf)
        {
            HideSetup();

            var gm = ChinesePoolGameManager.Instance;
            if (gm == null)
            {
                Debug.LogWarning("[ChinesePoolMatchSetupUI] ChinesePoolGameManager not found — cannot start match.");
                return;
            }

            gm.StartNewMatch(bestOf);
            ChinesePoolUIManager.Instance?.InitializeGame();
            Debug.Log($"[ChinesePoolMatchSetupUI] Match started: {(bestOf == 0 ? "Practice" : $"Best of {bestOf}")}.");
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
                var canvasGO = new GameObject("MatchSetupCanvas");
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
                var root = new GameObject("MatchSetup_Panel");
                root.transform.SetParent(parent, false);

                var rect = root.AddComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(640f, 640f);

                // Background
                var bg = new GameObject("Background");
                bg.transform.SetParent(root.transform, false);
                var bgImg = bg.AddComponent<Image>();
                bgImg.color = new Color(0.06f, 0.07f, 0.11f, 0.97f);
                var bgRect = bg.GetComponent<RectTransform>();
                bgRect.anchorMin = Vector2.zero;
                bgRect.anchorMax = Vector2.one;
                bgRect.offsetMin = Vector2.zero;
                bgRect.offsetMax = Vector2.zero;

                // Title
                var titleGO = new GameObject("Title");
                titleGO.transform.SetParent(root.transform, false);
                var title = titleGO.AddComponent<Text>();
                title.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                title.fontSize = 40;
                title.fontStyle = FontStyle.Bold;
                title.color = new Color(1f, 0.84f, 0.4f);
                title.alignment = TextAnchor.MiddleCenter;
                title.text = "เลือกเงื่อนไขการเล่น";
                var titleRect = title.rectTransform;
                titleRect.anchorMin = new Vector2(0f, 0.85f);
                titleRect.anchorMax = new Vector2(1f, 0.97f);
                titleRect.offsetMin = new Vector2(30f, 0f);
                titleRect.offsetMax = new Vector2(-30f, 0f);

                // Subtitle
                var subGO = new GameObject("Subtitle");
                subGO.transform.SetParent(root.transform, false);
                var sub = subGO.AddComponent<Text>();
                sub.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                sub.fontSize = 22;
                sub.color = new Color(0.8f, 0.8f, 0.85f);
                sub.alignment = TextAnchor.MiddleCenter;
                sub.text = "ตกลงเงื่อนไขกับคู่แข่ง (AI) ก่อนเริ่ม\nPractice = เล่นไปเรื่อยๆ ไม่มีจบแมตช์";
                var subRect = sub.rectTransform;
                subRect.anchorMin = new Vector2(0f, 0.72f);
                subRect.anchorMax = new Vector2(1f, 0.82f);
                subRect.offsetMin = new Vector2(30f, 0f);
                subRect.offsetMax = new Vector2(-30f, 0f);

                // Option buttons — vertical stack
                int count = Mathf.Max(1, bestOfOptions != null ? bestOfOptions.Length : 1);
                float startY = 0.60f;
                float step = 0.115f;
                for (int i = 0; i < count; i++)
                {
                    int bestOf = (bestOfOptions != null && i < bestOfOptions.Length) ? bestOfOptions[i] : 5;
                    string label = (optionLabels != null && i < optionLabels.Length) ? optionLabels[i] : $"Best of {bestOf}";
                    float y = startY - i * step;
                    CreateButton(root.transform, $"Option_{i}", label, new Vector2(0.5f, y), () => OnOptionSelected(bestOf));
                }

                // Hint
                var hintGO = new GameObject("Hint");
                hintGO.transform.SetParent(root.transform, false);
                var hint = hintGO.AddComponent<Text>();
                hint.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                hint.fontSize = 18;
                hint.color = new Color(0.7f, 0.7f, 0.75f);
                hint.alignment = TextAnchor.MiddleCenter;
                hint.text = "เลือกเงื่อนไขเพื่อเริ่มแมตช์";
                var hintRect = hint.rectTransform;
                hintRect.anchorMin = new Vector2(0f, 0.03f);
                hintRect.anchorMax = new Vector2(1f, 0.09f);
                hintRect.offsetMin = Vector2.zero;
                hintRect.offsetMax = Vector2.zero;

                return root;
            }
            catch (Exception e)
            {
                Debug.LogError($"[ChinesePoolMatchSetupUI] Failed to build panel: {e.Message}");
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
            rect.sizeDelta = new Vector2(440f, 72f);

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
