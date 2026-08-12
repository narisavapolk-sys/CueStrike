using UnityEngine;
using UnityEngine.UI;
using CueStrike.AI;

namespace CueStrike.AI
{
    /// <summary>
    /// R41 — Snooker AI Difficulty Selector UI (Easy/Medium/Hard/Expert)
    /// ลอก R34 pattern (ChinesePoolMatchSetupUI) แต่ผูกกับ CueStrikeSnookerAIBridge.SetDifficulty
    ///
    /// สร้าง Canvas (ScreenSpaceOverlay) + panel + label + 4 ปุ่ม difficulty
    /// Highlight ปุ่มที่เลือก (สีต่าง) + PlayerPrefs จำค่า (bridge อ่านตอน Start)
    /// Fail-safe: bridge ยังไม่โหลด → retry หาใหม่ทุกเฟรมจนเจอ
    /// </summary>
    public class SnookerDifficultyUI : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private CueStrikeSnookerAIBridge _bridge;
        [SerializeField] private Canvas _canvas;

        [Header("Difficulty (R41)")]
        [Tooltip("ระดับ AI เริ่มต้น — Easy/Medium/Hard/Expert")]
        public SkillLevel selectedDifficulty = SkillLevel.Medium;

        private const string PrefsKey = "CueStrike_SnookerAIDifficulty";
        private Button[] _difficultyButtons;
        private bool _built;

        private void Awake()
        {
            if (_bridge == null)
            {
                _bridge = FindAnyObjectByType<CueStrikeSnookerAIBridge>();
            }

            // โหลดค่าที่เคยเลือกไว้
            int saved = PlayerPrefs.GetInt(PrefsKey, (int)selectedDifficulty);
            if (System.Enum.IsDefined(typeof(SkillLevel), saved))
            {
                selectedDifficulty = (SkillLevel)saved;
            }
        }

        private void Start()
        {
            BuildUI();
            ApplyToBridge();
            RefreshHighlight();
        }

        private void Update()
        {
            // retry — bridge อาจโหลดทีหลัง (เหมือน R31 fail-safe)
            if (_bridge == null)
            {
                _bridge = FindAnyObjectByType<CueStrikeSnookerAIBridge>();
                if (_bridge != null)
                {
                    ApplyToBridge();
                    RefreshHighlight();
                }
            }
        }

        // ============ Public API ============

        /// <summary>เลือกระดับ AI (เรียกจากปุ่ม)</summary>
        public void OnDifficultySelected(SkillLevel level)
        {
            selectedDifficulty = level;
            PlayerPrefs.SetInt(PrefsKey, (int)level);
            PlayerPrefs.Save();

            ApplyToBridge();
            RefreshHighlight();
            Debug.Log($"[SnookerDifficultyUI] AI difficulty selected: {level}.");
        }

        /// <summary>ระดับปัจจุบัน (สำหรับ self-test)</summary>
        public SkillLevel GetSelectedDifficulty() => selectedDifficulty;

        /// <summary>bridge ที่ผูก (สำหรับ self-test)</summary>
        public CueStrikeSnookerAIBridge GetBridge() => _bridge;

        // ============ Internals ============

        private void ApplyToBridge()
        {
            if (_bridge != null)
            {
                _bridge.SetDifficulty(selectedDifficulty);
            }
        }

        private void BuildUI()
        {
            if (_built) return;
            _built = true;

            // ---------- Canvas (สร้างถ้าไม่มี) ----------
            if (_canvas == null)
            {
                _canvas = FindAnyObjectByType<Canvas>();
            }
            if (_canvas == null)
            {
                var canvasGO = new GameObject("SnookerDifficultyCanvas");
                canvasGO.transform.SetParent(transform, false);
                _canvas = canvasGO.AddComponent<Canvas>();
                _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasGO.AddComponent<CanvasScaler>();
                canvasGO.AddComponent<GraphicRaycaster>();

                if (FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
                {
                    var esGO = new GameObject("EventSystem");
                    esGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
                    esGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                }
            }

            // ---------- Panel ----------
            var panel = new GameObject("DifficultyPanel");
            panel.transform.SetParent(_canvas.transform, false);
            var panelRect = panel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.02f);
            panelRect.anchorMax = new Vector2(0.5f, 0.14f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(900f, 120f);

            var panelImg = panel.AddComponent<Image>();
            panelImg.color = new Color(0.08f, 0.08f, 0.12f, 0.85f);

            // ---------- Label ----------
            var labelGO = new GameObject("AIDifficultyLabel");
            labelGO.transform.SetParent(panel.transform, false);
            var label = labelGO.AddComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 20;
            label.color = new Color(0.9f, 0.9f, 0.95f);
            label.alignment = TextAnchor.MiddleLeft;
            label.text = "ระดับ AI:";
            var labelRect = label.rectTransform;
            labelRect.anchorMin = new Vector2(0f, 0f);
            labelRect.anchorMax = new Vector2(0.2f, 1f);
            labelRect.offsetMin = new Vector2(20f, 0f);
            labelRect.offsetMax = new Vector2(0f, 0f);

            // ---------- 4 ปุ่ม difficulty ----------
            string[] labels = { "Easy", "Medium", "Hard", "Expert" };
            SkillLevel[] values = { SkillLevel.Easy, SkillLevel.Medium, SkillLevel.Hard, SkillLevel.Expert };
            _difficultyButtons = new Button[values.Length];

            float startX = 0.24f;
            float stepX = 0.19f;
            for (int i = 0; i < values.Length; i++)
            {
                int idx = i;
                _difficultyButtons[i] = CreateDifficultyButton(
                    panel.transform,
                    $"Diff_{values[i]}",
                    labels[i],
                    new Vector2(startX + idx * stepX, 0.5f),
                    () => OnDifficultySelected(values[idx]));
            }
        }

        private Button CreateDifficultyButton(Transform parent, string name, string label, Vector2 anchor, UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);

            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(140f, 52f);

            var img = go.AddComponent<Image>();
            img.color = new Color(0.35f, 0.45f, 0.65f, 1f);

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(onClick);

            var labelGO = new GameObject("Label");
            labelGO.transform.SetParent(go.transform, false);
            var labelText = labelGO.AddComponent<Text>();
            labelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            labelText.fontSize = 22;
            labelText.color = Color.white;
            labelText.alignment = TextAnchor.MiddleCenter;
            labelText.text = label;
            var labelRect = labelText.rectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            return btn;
        }

        private void RefreshHighlight()
        {
            if (_difficultyButtons == null) return;

            Color selected = new Color(0.55f, 0.75f, 0.4f, 1f);  // เขียว = เลือกอยู่
            Color normal = new Color(0.35f, 0.45f, 0.65f, 1f);

            for (int i = 0; i < _difficultyButtons.Length; i++)
            {
                var img = _difficultyButtons[i].GetComponent<Image>();
                if (img == null) continue;
                img.color = ((int)selectedDifficulty == i) ? selected : normal;
            }
        }
    }
}
