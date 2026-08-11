using System;
using UnityEngine;
using UnityEngine.UI;

namespace CueStrike.UI
{
    /// <summary>
    /// R26 — Mode selection panel (coach-approved: Snooker 15/10/6 เป็นโหมดหลัก).
    /// Self-building World-Space VR UI: 6 ปุ่ม (Snooker 15 / Snooker 10 / Snooker 6 / 8-Ball / 9-Ball / Chinese Pool).
    /// กดปุ่ม → CueStrikeGameModeSelector.SelectedMode → ApplyModeToScene → โหลดฉากห้องที่ถูกต้อง.
    ///
    /// วางบน Canvas ของ MainMenu (ผ่าน Editor tool หรือผูกด้วยมือ) — สร้าง panel ด้วยโค้ด
    /// (ไม่พึ่ง prefab) ตาม convention R24/R25. Fail-safe: หา Canvas ไม่เจอ → สร้างให้.
    /// </summary>
    public class CueStrikeModeSelectionPanel : MonoBehaviour
    {
        [Header("Optional References")]
        [Tooltip("Canvas หลัก (World-Space VR friendly). ถ้า null จะหา Canvas ตัวแรกในฉาก")]
        public Canvas targetCanvas;

        [Tooltip("ฉากเมนูหลักที่ใช้กลับ (default: Title_NoksGrandHall)")]
        public string backSceneName = "Title_NoksGrandHall";

        private GameObject _panelRoot;

        private void Start()
        {
            ShowPanel();
        }

        private void OnDestroy()
        {
            if (_panelRoot != null)
            {
                Destroy(_panelRoot);
            }
        }

        /// <summary>แสดง panel เลือกโหมด</summary>
        public void ShowPanel()
        {
            if (!TryEnsureReferences())
            {
                Debug.LogWarning("[CueStrikeModeSelectionPanel] Missing Canvas — cannot show mode panel.");
                return;
            }

            _panelRoot.SetActive(true);
            Debug.Log("[CueStrikeModeSelectionPanel] Mode selection panel shown.");
        }

        // ---- Button handlers ----

        private void OnModeSelected(CueStrikeGameModeSelector.GameMode mode)
        {
            CueStrikeGameModeSelector.SelectedMode = mode;
            string sceneName = CueStrikeGameModeSelector.ModeToSceneName(mode);
            Debug.Log($"[CueStrikeModeSelectionPanel] Mode '{CueStrikeGameModeSelector.GetModeLabel(mode)}' selected → loading '{sceneName}'.");

            CueStrikeGameModeSelector.ApplyModeToScene();
            CueStrike.VR.CueStrikeLoadingScreen.LoadScene(sceneName);
        }

        private void OnBackClicked()
        {
            Debug.Log($"[CueStrikeModeSelectionPanel] Back to {backSceneName}.");
            UnityEngine.SceneManagement.SceneManager.LoadScene(backSceneName);
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
                var canvasGO = new GameObject("ModeSelectionCanvas");
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
                var root = new GameObject("ModeSelection_Panel");
                root.transform.SetParent(parent, false);

                var rect = root.AddComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(640f, 680f);

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
                title.fontSize = 42;
                title.fontStyle = FontStyle.Bold;
                title.color = new Color(1f, 0.84f, 0.4f);
                title.alignment = TextAnchor.MiddleCenter;
                title.text = "เลือกโหมดเกม";
                var titleRect = title.rectTransform;
                titleRect.anchorMin = new Vector2(0f, 0.88f);
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
                sub.text = "โหมดหลัก: Snooker (15/10/6 ลูกแดง)\nเลือกโหมดเพื่อเข้าห้องแข่ง";
                var subRect = sub.rectTransform;
                subRect.anchorMin = new Vector2(0f, 0.78f);
                subRect.anchorMax = new Vector2(1f, 0.86f);
                subRect.offsetMin = new Vector2(30f, 0f);
                subRect.offsetMax = new Vector2(-30f, 0f);

                // Mode buttons — vertical stack (enum order)
                var modes = (CueStrikeGameModeSelector.GameMode[])Enum.GetValues(typeof(CueStrikeGameModeSelector.GameMode));
                float startY = 0.70f;
                float step = 0.105f;
                for (int i = 0; i < modes.Length; i++)
                {
                    var mode = modes[i];
                    string label = CueStrikeGameModeSelector.GetModeLabel(mode);
                    float y = startY - i * step;
                    CreateButton(root.transform, $"Mode_{i}", label, new Vector2(0.5f, y), () => OnModeSelected(mode));
                }

                // Back button
                CreateButton(root.transform, "BackButton", "กลับ", new Vector2(0.5f, 0.05f), OnBackClicked);

                return root;
            }
            catch (Exception e)
            {
                Debug.LogError($"[CueStrikeModeSelectionPanel] Failed to build panel: {e.Message}");
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
            rect.sizeDelta = new Vector2(440f, 62f);

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
