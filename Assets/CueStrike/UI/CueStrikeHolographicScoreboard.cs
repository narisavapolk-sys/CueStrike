using UnityEngine;
using CueStrike.Gameplay;

namespace CueStrike.UI
{
    /// <summary>
    /// Spawns a high-tech floor projector standing next to the table
    /// and projects a glowing, semi-transparent 3D neon holographic scoreboard in mid-air.
    /// Displays scores, active turns, and "Highest Break" statistics dynamically.
    /// </summary>
    public class CueStrikeHolographicScoreboard : MonoBehaviour
    {
        public static CueStrikeHolographicScoreboard Instance { get; private set; }

        [Header("Visual Colors")]
        public Color neonColor = new Color(0f, 0.8f, 1f, 0.7f); // Neon Cyan
        public Color goldColor = new Color(1f, 0.85f, 0f, 0.8f); // Gold

        private TextMesh _textProjector;
        private CueStrikeRulesManager _rules;

        // Cached ball status string — updated via static event (no polling)
        private string _ballStatusCache = "";

        private void Awake()
        {
            Instance = this;
            _rules = FindFirstObjectByType<CueStrikeRulesManager>();
        }

        private void OnEnable()
        {
            CueStrikePottedBallTracker.OnBallStatusChanged += OnBallStatusChanged;
        }

        private void OnDisable()
        {
            CueStrikePottedBallTracker.OnBallStatusChanged -= OnBallStatusChanged;
        }

        private void OnBallStatusChanged(string status)
        {
            _ballStatusCache = status;
        }

        private void Start()
        {
            BuildProceduralProjector();
        }

        private void Update()
        {
            UpdateScoreDisplay();
        }

        /// <summary>
        /// Assembles the physical sci-fi projector base stand and the floating TextMesh projector.
        /// </summary>
        private void BuildProceduralProjector()
        {
            // 1. Metal projector floor base stand
            GameObject baseGO = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            baseGO.name = "HoloBase";
            baseGO.transform.SetParent(transform, false);
            baseGO.transform.localPosition = new Vector3(0f, 0.2f, 0f);
            baseGO.transform.localScale = new Vector3(0.35f, 0.2f, 0.35f);

            var baseRend = baseGO.GetComponent<Renderer>();
            if (baseRend != null)
            {
                baseRend.material = new Material(Shader.Find("Universal RenderPipeline/Lit"));
                baseRend.material.color = new Color(0.12f, 0.15f, 0.18f); // Matte dark metal
                baseRend.material.SetFloat("_Metallic", 0.9f);
                baseRend.material.SetFloat("_Smoothness", 0.7f);
            }

            // 2. Floating hologram text mesh object
            GameObject textGO = new GameObject("HoloDisplay_Text");
            textGO.transform.SetParent(transform, false);
            textGO.transform.localPosition = new Vector3(0f, 1.4f, 0f); // Float at eye level

            _textProjector = textGO.AddComponent<TextMesh>();
            _textProjector.fontSize = 42;
            _textProjector.characterSize = 0.04f;
            _textProjector.anchor = TextAnchor.MiddleCenter;
            _textProjector.alignment = TextAlignment.Center;
            _textProjector.color = neonColor;

            // Make it billboard to face camera
            textGO.AddComponent<CueStrikeBallLabels>();
        }

        /// <summary>
        /// Reads scores, turn index, and break runs to output clean glowing scoreboard stats.
        /// </summary>
        private void UpdateScoreDisplay()
        {
            if (_textProjector == null || _rules == null) return;

            int tableStyle  = PlayerPrefs.GetInt("CueStrike_TableStyle", 0);
            bool isSnooker  = tableStyle == 0;
            bool is8Ball    = tableStyle == 1;
            bool is9Ball    = tableStyle == 2;

            // ── Header ──
            string gameLabel = isSnooker ? "SNOOKER" : is8Ball ? "8-BALL" : "9-BALL";
            string turnStr   = _rules.currentPlayer == 0
                ? $"{_rules.playerNames[0]} TURN"
                : $"{_rules.playerNames[1]} TURN";

            // ── Score row ──
            string scoreStr = $"P1: {_rules.scores[0]}  |  P2: {_rules.scores[1]}";

            // ── Break info (Snooker only) ──
            string breakStr = "";
            if (isSnooker)
            {
                int currentBreak = _rules.currentBreak;
                int highestBreak = PlayerPrefs.GetInt("CueStrike_HighestBreak", 0);
                breakStr = $"\n<color=yellow>Break: {currentBreak}</color>  |  <color=#FFD700>Best: {highestBreak}</color>";
            }

            // ── Ball tracker section (live from event cache) ──
            string ballSection = !string.IsNullOrEmpty(_ballStatusCache)
                ? $"\n{_ballStatusCache}"
                : "";

            _textProjector.text =
                $"── {gameLabel} ──\n" +
                $"{turnStr}\n" +
                $"{scoreStr}" +
                $"{breakStr}" +
                $"{ballSection}\n" +
                $"─────────────────";
        }
    }
}
