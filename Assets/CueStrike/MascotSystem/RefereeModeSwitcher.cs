using UnityEngine;

namespace CueStrike.MascotSystem
{
    /// <summary>R43 — applies the lobby referee selection to Bo and Uncle bridges.</summary>
    public class RefereeModeSwitcher : MonoBehaviour
    {
        public enum Mode { BoSolo, UncleSolo, Duo }
        private const string PrefsKey = "CueStrike_RefereeMode";
        [SerializeField] private BoRefereeEventBridge _boBridge;
        [SerializeField] private Mode _selectedMode = Mode.BoSolo;

        public Mode SelectedMode => _selectedMode;
        public BoRefereeEventBridge BoBridge => _boBridge;

        private void Awake()
        {
            int saved = PlayerPrefs.GetInt(PrefsKey, (int)Mode.BoSolo);
            if (System.Enum.IsDefined(typeof(Mode), saved)) _selectedMode = (Mode)saved;
            ResolveBridge();
        }

        private void Update()
        {
            if (_boBridge == null) ResolveBridge();
            ApplyMode();
        }

        public void SelectMode(Mode mode)
        {
            _selectedMode = mode;
            PlayerPrefs.SetInt(PrefsKey, (int)mode);
            PlayerPrefs.Save();
            ApplyMode();
            Debug.Log($"[RefereeModeSwitcher] Selected {_selectedMode}.");
        }

        public void ApplyMode()
        {
            if (_boBridge == null) return;
            _boBridge.enabled = _selectedMode != Mode.UncleSolo;
            _boBridge.refereeMode = _selectedMode == Mode.Duo
                ? BoRefereeEventBridge.RefereeMode.DuoWithUncle
                : BoRefereeEventBridge.RefereeMode.ReplaceUncle;
            // UncleSolo is handled by Bo bridge's mode application after it is disabled;
            // explicitly set the scene Uncle bridge for deterministic menu behavior.
            var uncle = FindAnyObjectByType<UncleNokRefereeEventBridge>();
            if (uncle != null) uncle.enabled = _selectedMode != Mode.BoSolo;
        }

        public void SetBoSolo() => SelectMode(Mode.BoSolo);
        public void SetUncleSolo() => SelectMode(Mode.UncleSolo);
        public void SetDuo() => SelectMode(Mode.Duo);

        private void ResolveBridge()
        {
            if (_boBridge == null) _boBridge = FindAnyObjectByType<BoRefereeEventBridge>(FindObjectsInactive.Include);
        }
    }
}
