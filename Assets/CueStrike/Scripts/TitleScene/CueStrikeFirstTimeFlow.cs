using System;
using UnityEngine;
using UnityEngine.UI;

namespace CueStrike.UI
{
    /// <summary>
    /// R24 — First-time onboarding for the Title scene (Lobby).
    ///
    /// แสดงสไลด์สอนเบื้องต้น (ยินดีต้อนรับ / จับไม้คิว + เล็ง / ยิง + เริ่มเล่น)
    /// เฉพาะผู้เล่นครั้งแรก (PlayerPrefs "CueStrike_FirstTimeTutorialDone" = 0)
    /// เคยเล่นแล้วหรือกด Skip → เข้า Lobby ได้ทันที
    ///
    /// Design (coach-approved): Title เป็นศูนย์กลาง — UI เป็น World-Space board ลอยหน้า
    /// ผู้เล่น, ไม่สลับฉาก. Standalone: ไม่พึ่ง CueStrikeTutorialManager (in-match validation).
    /// Fail-safe: ถ้า reference หาย / PlayerPrefs ล้มเหลว → ไม่บล็อกเมนู.
    /// </summary>
    public class CueStrikeFirstTimeFlow : MonoBehaviour
    {
        public const string PrefsKey = "CueStrike_FirstTimeTutorialDone";

        [Header("Optional References (ถ้าไม่ assign จะสร้างให้อัตโนมัติ)")]
        [Tooltip("Canvas หลักของ Lobby (World-Space VR friendly). ถ้า null จะสร้าง Canvas ใหม่")]
        public Canvas targetCanvas;

        [Header("Content")]
        [Tooltip("สไลด์สอน (title + body) — ถ้าไม่ assign จะใช้ค่า default")]
        public TutorialSlide[] slides;

        [Header("Behavior")]
        [Tooltip("เปิดใช้การตรวจครั้งแรก (ถ้าปิด = ข้าม tutorial เสมอ)")]
        public bool tutorialEnabled = true;

        // Runtime state
        private bool _showing = false;
        private int _currentSlide = 0;
        private GameObject _panelRoot;
        private Text _titleText;
        private Text _bodyText;
        private Button _nextButton;
        private Button _skipButton;
        private Canvas _runtimeCanvas;

        /// <summary>แสดง tutorial อยู่หรือไม่</summary>
        public bool IsShowing => _showing;

        /// <summary>เช็คว่าเคยผ่าน tutorial มาแล้วหรือยัง (static — ใช้ได้จาก Editor tool / self-test)</summary>
        public static bool IsTutorialDone()
        {
            try { return PlayerPrefs.GetInt(PrefsKey, 0) != 0; }
            catch (Exception) { return false; }
        }

        /// <summary>ตั้งค่า flag ว่า tutorial ผ่านแล้ว (static)</summary>
        public static void MarkTutorialDone()
        {
            try
            {
                PlayerPrefs.SetInt(PrefsKey, 1);
                PlayerPrefs.Save();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[CueStrikeFirstTimeFlow] Failed to save PlayerPrefs: {e.Message}");
            }
        }

        /// <summary>รีเซ็ต flag (สำหรับ test / debug)</summary>
        public static void ResetTutorialFlag()
        {
            try { PlayerPrefs.DeleteKey(PrefsKey); PlayerPrefs.Save(); }
            catch (Exception e) { Debug.LogWarning($"[CueStrikeFirstTimeFlow] Failed to reset PlayerPrefs: {e.Message}"); }
        }

        private void Start()
        {
            if (!tutorialEnabled)
            {
                Debug.Log("[CueStrikeFirstTimeFlow] Disabled — skipping first-time tutorial.");
                return;
            }

            if (IsTutorialDone())
            {
                // เคยเล่นแล้ว — ไม่ต้องรบกวน
                Debug.Log("[CueStrikeFirstTimeFlow] Tutorial already done — skipping.");
                return;
            }

            ShowOnboarding();
        }

        /// <summary>
        /// แสดง onboarding panel (เริ่มจากสไลด์แรก). Guard: ถ้าแสดงอยู่แล้ว return.
        /// </summary>
        public void ShowOnboarding()
        {
            if (_showing) return;

            if (!TryEnsureReferences())
            {
                Debug.LogWarning("[CueStrikeFirstTimeFlow] Missing references — skipping tutorial to avoid blocking the lobby.");
                return;
            }

            _showing = true;
            _currentSlide = 0;

            if (_panelRoot != null) _panelRoot.SetActive(true);
            RenderSlide();
            Debug.Log("[CueStrikeFirstTimeFlow] First-time tutorial shown (slide 1).");
        }

        /// <summary>ปิด onboarding + ตั้ง flag (กด Skip หรือจบสไลด์สุดท้าย)</summary>
        public void DismissOnboarding(bool markDone)
        {
            if (markDone) MarkTutorialDone();

            _showing = false;
            if (_panelRoot != null) _panelRoot.SetActive(false);

            Debug.Log(markDone
                ? "[CueStrikeFirstTimeFlow] Tutorial dismissed — marked as done."
                : "[CueStrikeFirstTimeFlow] Tutorial dismissed.");
        }

        // ---- UI handlers (ผูกกับปุ่ม) ----

        public void OnNextClicked()
        {
            if (!_showing) return;

            if (_currentSlide < GetSlideCount() - 1)
            {
                _currentSlide++;
                RenderSlide();
            }
            else
            {
                // สไลด์สุดท้าย → จบ tutorial
                DismissOnboarding(true);
            }
        }

        public void OnSkipClicked()
        {
            if (!_showing) return;
            DismissOnboarding(true);
        }

        // ---- Internals ----

        private int GetSlideCount()
        {
            return slides != null && slides.Length > 0 ? slides.Length : DefaultSlides().Length;
        }

        private TutorialSlide[] GetActiveSlides()
        {
            return slides != null && slides.Length > 0 ? slides : DefaultSlides();
        }

        private void RenderSlide()
        {
            if (_panelRoot == null) return;

            var active = GetActiveSlides();
            if (active == null || active.Length == 0 || _currentSlide >= active.Length)
            {
                DismissOnboarding(true);
                return;
            }

            var slide = active[_currentSlide];
            if (_titleText != null) _titleText.text = slide.title;
            if (_bodyText != null) _bodyText.text = slide.body;

            // ปุ่มสุดท้ายเปลี่ยน label เป็น "เริ่มเล่น"
            bool isLast = _currentSlide >= active.Length - 1;
            if (_nextButton != null)
            {
                var label = _nextButton.GetComponentInChildren<Text>();
                if (label != null) label.text = isLast ? "เริ่มเล่น" : "ถัดไป";
            }
        }

        /// <summary>
        /// หา canvas / สร้าง panel UI. Fail-safe: ถ้า Canvas component หาไม่เจอ → สร้างใหม่.
        /// </summary>
        private bool TryEnsureReferences()
        {
            // 1. Canvas
            if (targetCanvas == null)
            {
                targetCanvas = FindAnyObjectByType<Canvas>();
            }

            if (targetCanvas == null)
            {
                var canvasGO = new GameObject("FirstTimeTutorialCanvas");
                canvasGO.transform.SetParent(transform, false);
                targetCanvas = canvasGO.AddComponent<Canvas>();
                targetCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasGO.AddComponent<CanvasScaler>();
                canvasGO.AddComponent<GraphicRaycaster>();
                _runtimeCanvas = targetCanvas;
            }

            // 2. Panel UI (สร้างให้ถ้ายังไม่มี)
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
                var root = new GameObject("FirstTimeTutorial_Panel");
                root.transform.SetParent(parent, false);

                var rect = root.AddComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(900f, 520f);

                // Background
                var bg = new GameObject("Background");
                bg.transform.SetParent(root.transform, false);
                var bgImg = bg.AddComponent<Image>();
                bgImg.color = new Color(0.08f, 0.08f, 0.12f, 0.96f);
                var bgRect = bg.GetComponent<RectTransform>();
                bgRect.anchorMin = Vector2.zero;
                bgRect.anchorMax = Vector2.one;
                bgRect.offsetMin = Vector2.zero;
                bgRect.offsetMax = Vector2.zero;

                // Title
                var titleGO = new GameObject("Title");
                titleGO.transform.SetParent(root.transform, false);
                _titleText = titleGO.AddComponent<Text>();
                _titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                _titleText.fontSize = 44;
                _titleText.fontStyle = FontStyle.Bold;
                _titleText.color = new Color(1f, 0.84f, 0.4f);
                _titleText.alignment = TextAnchor.MiddleCenter;
                var titleRect = _titleText.rectTransform;
                titleRect.anchorMin = new Vector2(0f, 0.78f);
                titleRect.anchorMax = new Vector2(1f, 0.95f);
                titleRect.offsetMin = new Vector2(40f, 0f);
                titleRect.offsetMax = new Vector2(-40f, 0f);

                // Body
                var bodyGO = new GameObject("Body");
                bodyGO.transform.SetParent(root.transform, false);
                _bodyText = bodyGO.AddComponent<Text>();
                _bodyText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                _bodyText.fontSize = 26;
                _bodyText.color = new Color(0.92f, 0.92f, 0.96f);
                _bodyText.alignment = TextAnchor.MiddleCenter;
                _bodyText.horizontalOverflow = HorizontalWrapMode.Wrap;
                _bodyText.verticalOverflow = VerticalWrapMode.Truncate;
                var bodyRect = _bodyText.rectTransform;
                bodyRect.anchorMin = new Vector2(0.05f, 0.28f);
                bodyRect.anchorMax = new Vector2(0.95f, 0.74f);
                bodyRect.offsetMin = Vector2.zero;
                bodyRect.offsetMax = Vector2.zero;

                // Buttons row
                _skipButton = CreateButton(root.transform, "SkipButton", "ข้าม Tutorial", new Vector2(0.22f, 0.12f), new Vector2(0.5f, 0.5f), OnSkipClicked);
                _nextButton = CreateButton(root.transform, "NextButton", "ถัดไป", new Vector2(0.78f, 0.12f), new Vector2(0.5f, 0.5f), OnNextClicked);

                // Hint
                var hintGO = new GameObject("Hint");
                hintGO.transform.SetParent(root.transform, false);
                var hint = hintGO.AddComponent<Text>();
                hint.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                hint.fontSize = 18;
                hint.color = new Color(0.7f, 0.7f, 0.75f);
                hint.alignment = TextAnchor.MiddleCenter;
                hint.text = "ใช้ปุ่มถัดไปเพื่ออ่าน และปุ่มข้ามเพื่อเข้าห้องหลัก";
                var hintRect = hint.rectTransform;
                hintRect.anchorMin = new Vector2(0f, 0.04f);
                hintRect.anchorMax = new Vector2(1f, 0.1f);
                hintRect.offsetMin = Vector2.zero;
                hintRect.offsetMax = Vector2.zero;

                return root;
            }
            catch (Exception e)
            {
                Debug.LogError($"[CueStrikeFirstTimeFlow] Failed to build panel: {e.Message}");
                return null;
            }
        }

        private Button CreateButton(Transform parent, string name, string label, Vector2 anchor, Vector2 pivot, UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);

            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.sizeDelta = new Vector2(220f, 64f);

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

            return btn;
        }

        private void OnDestroy()
        {
            if (_runtimeCanvas != null)
            {
                Destroy(_runtimeCanvas.gameObject);
            }
        }

        /// <summary>สไลด์เริ่มต้น (ภาษาไทย, VR-friendly)</summary>
        private static TutorialSlide[] DefaultSlides()
        {
            return new[]
            {
                new TutorialSlide
                {
                    title = "ยินดีต้อนรับสู่ CueStrike! 🎱",
                    body = "นี่คือคลับบิลเลียด VR ของคุณ — ลุงโน๊กกับโบรออยู่!\n\n" +
                           "ครั้งแรก เราจะสอนพื้นฐานสั้นๆ ก่อนเริ่มเล่น\n" +
                           "กด \"ถัดไป\" เพื่อเริ่ม"
                },
                new TutorialSlide
                {
                    title = "จับไม้คิว & เล็ง 🎯",
                    body = "จับไม้คิวด้วยมือข้างถนัด ใช้มืออีกข้างเป็น Bridge (พักไม้)\n\n" +
                           "เล็งโดยให้แนวไม้คิวตรงกับลูกเป้าหมาย\n" +
                           "ดูเส้นนำทาง (aim line) ที่แสดงบนโต๊ะ"
                },
                new TutorialSlide
                {
                    title = "ยิง & เริ่มเล่น 💥",
                    body = "ดึงไม้คิวกลับเพื่อเพิ่มแรง แล้วแทงไปข้างหน้า\n\n" +
                           "เลือกโหมดเกมและเริ่มเฟรมแรกของคุณได้เลย\n" +
                           "ขอให้สนุกกับการเล่น!"
                }
            };
        }

        /// <summary>ข้อมูลสไลด์ — serializable สำหรับ Inspector</summary>
        [Serializable]
        public class TutorialSlide
        {
            public string title;
            public string body;
        }
    }
}
