using UnityEngine;
using UnityEngine.UI;
using CueStrike.MascotSystem;

namespace CueStrike.UI
{
    /// <summary>R43 — Lobby selector for Bo solo, Uncle solo, or duo referees.</summary>
    public class RefereeModeUI : MonoBehaviour
    {
        [SerializeField] private RefereeModeSwitcher _switcher;
        [SerializeField] private Canvas _canvas;
        private Button[] _buttons;
        private bool _built;

        private void Awake()
        {
            if (_switcher == null) _switcher = FindAnyObjectByType<RefereeModeSwitcher>();
        }

        private void Start() { BuildUI(); Refresh(); }

        private void Update()
        {
            if (_switcher == null) _switcher = FindAnyObjectByType<RefereeModeSwitcher>();
            if (_switcher != null) Refresh();
        }

        public void SelectBoSolo() { Select(RefereeModeSwitcher.Mode.BoSolo); }
        public void SelectUncleSolo() { Select(RefereeModeSwitcher.Mode.UncleSolo); }
        public void SelectDuo() { Select(RefereeModeSwitcher.Mode.Duo); }
        public RefereeModeSwitcher GetSwitcher() => _switcher;

        private void Select(RefereeModeSwitcher.Mode mode)
        {
            if (_switcher == null) return;
            _switcher.SelectMode(mode);
            Refresh();
        }

        private void BuildUI()
        {
            if (_built) return;
            _built = true;
            if (_canvas == null) _canvas = FindAnyObjectByType<Canvas>();
            if (_canvas == null)
            {
                var go = new GameObject("RefereeModeCanvas");
                go.transform.SetParent(transform, false);
                _canvas = go.AddComponent<Canvas>();
                _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                go.AddComponent<CanvasScaler>();
                go.AddComponent<GraphicRaycaster>();
            }
            var panel = new GameObject("RefereeModePanel");
            panel.transform.SetParent(_canvas.transform, false);
            var rect = panel.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(.5f, .82f); rect.anchorMax = new Vector2(.5f, .96f);
            rect.sizeDelta = new Vector2(760f, 110f);
            var image = panel.AddComponent<Image>(); image.color = new Color(.05f, .06f, .1f, .9f);
            var modes = new[] { RefereeModeSwitcher.Mode.BoSolo, RefereeModeSwitcher.Mode.UncleSolo, RefereeModeSwitcher.Mode.Duo };
            var labels = new[] { "Bo คนเดียว", "ลุงคนเดียว", "คู่กัน" };
            _buttons = new Button[modes.Length];
            for (int i = 0; i < modes.Length; i++)
            {
                int index = i;
                var go = new GameObject("Referee_" + modes[i]); go.transform.SetParent(panel.transform, false);
                var br = go.AddComponent<RectTransform>(); br.anchorMin = new Vector2(.08f + i*.31f, .22f); br.anchorMax = new Vector2(.08f + i*.31f, .78f); br.sizeDelta = new Vector2(190f, 55f);
                var bi = go.AddComponent<Image>(); bi.color = new Color(.28f, .38f, .6f, 1f);
                var button = go.AddComponent<Button>(); button.targetGraphic = bi;
                button.onClick.AddListener(() => Select(modes[index])); _buttons[i] = button;
                var textGo = new GameObject("Label"); textGo.transform.SetParent(go.transform, false);
                var text = textGo.AddComponent<Text>(); text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); text.text = labels[i]; text.fontSize = 22; text.alignment = TextAnchor.MiddleCenter; text.color = Color.white;
                text.rectTransform.anchorMin = Vector2.zero; text.rectTransform.anchorMax = Vector2.one; text.rectTransform.offsetMin = Vector2.zero; text.rectTransform.offsetMax = Vector2.zero;
            }
        }

        private void Refresh()
        {
            if (_buttons == null || _switcher == null) return;
            for (int i = 0; i < _buttons.Length; i++) _buttons[i].GetComponent<Image>().color = i == (int)_switcher.SelectedMode ? new Color(.55f,.75f,.4f,1f) : new Color(.28f,.38f,.6f,1f);
        }
    }
}
