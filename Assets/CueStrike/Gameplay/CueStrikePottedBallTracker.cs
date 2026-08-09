using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace CueStrike.Gameplay
{
    /// <summary>
    /// Central tracker for potted balls during a match.
    /// Maintains potted ball history per player and computes remaining ball lists
    /// for 8-Ball, 9-Ball, and Snooker game modes.
    /// Exposes static events so Scoreboard and HUD can subscribe without tight coupling.
    /// </summary>
    public class CueStrikePottedBallTracker : MonoBehaviour
    {
        // ── Singleton ────────────────────────────────────────────────────
        public static CueStrikePottedBallTracker Instance { get; private set; }

        // ── Events ───────────────────────────────────────────────────────
        /// Fired whenever the potted list changes. Passes formatted summary string.
        public static event System.Action<string> OnBallStatusChanged;

        // ── Internal state ───────────────────────────────────────────────
        // Key = player index (0/1), Value = list of ball IDs potted by that player
        private readonly Dictionary<int, List<int>> _pottedByPlayer = new();

        // Which ball IDs exist at the start of each game mode
        // Snooker: 1 cue, 15 reds (id 1-15), 6 colours (id 16-21)
        // 8-Ball : 1 cue (0), solids 1-7, 8, stripes 9-15
        // 9-Ball : 1 cue (0), balls 1-9
        private static readonly HashSet<int> SnookerReds      = new() { 1,2,3,4,5,6,7,8,9,10,11,12,13,14,15 };
        private static readonly HashSet<int> SnookerColours   = new() { 16,17,18,19,20,21 };
        private static readonly HashSet<int> Pool8All         = new() { 1,2,3,4,5,6,7,8,9,10,11,12,13,14,15 };
        private static readonly HashSet<int> Pool9All         = new() { 1,2,3,4,5,6,7,8,9 };

        // Snooker colour names indexed by ball ID
        private static readonly Dictionary<int, string> SnookerColourNames = new()
        {
            { 16, "Yellow" }, { 17, "Green"  }, { 18, "Brown" },
            { 19, "Blue"   }, { 20, "Pink"   }, { 21, "Black" }
        };

        // 0 = Snooker, 1 = 8-Ball, 2 = 9-Ball  (matches TableStyle PlayerPref)
        private int _tableStyle;

        // 8-Ball assignment: 0 = unassigned, 1 = solids (1-7), 2 = stripes (9-15)
        private int[] _playerGroup = { 0, 0 };   // index by player

        // ── Lifecycle ────────────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void OnEnable()
        {
            // Hook into the rules manager ball-potted pipeline via Pocket.cs
            // Pocket.cs calls CueStrikeRulesManager.BallPotted, which we intercept here
            // by subscribing to OnPlayerScore (simplest compatible hook).
            var rules = CueStrikeRulesManager.Instance;
            if (rules != null)
                rules.OnPlayerScore += _ => BroadcastStatus();
        }

        private void OnDisable()
        {
            var rules = CueStrikeRulesManager.Instance;
            if (rules != null)
                rules.OnPlayerScore -= _ => BroadcastStatus();
        }

        /// <summary>
        /// Called every time a non-cue ball enters a pocket.
        /// Should be called from Pocket.cs (or intercepted via RulesManager).
        /// </summary>
        public void RegisterPotted(int ballId, int playerIndex)
        {
            if (ballId == 0) return; // cue ball – ignore

            _tableStyle = PlayerPrefs.GetInt("CueStrike_TableStyle", 0);

            if (!_pottedByPlayer.ContainsKey(playerIndex))
                _pottedByPlayer[playerIndex] = new List<int>();

            if (!_pottedByPlayer[playerIndex].Contains(ballId))
                _pottedByPlayer[playerIndex].Add(ballId);

            // 8-Ball group auto-assignment on first pot
            if (_tableStyle == 1)
            {
                if (_playerGroup[playerIndex] == 0)
                {
                    _playerGroup[playerIndex] = (ballId >= 1 && ballId <= 7) ? 1 : 2;
                    _playerGroup[1 - playerIndex] = (_playerGroup[playerIndex] == 1) ? 2 : 1;
                }
            }

            BroadcastStatus();
        }

        /// <summary>Reset tracker at the start of a new frame/game.</summary>
        public void ResetTracker()
        {
            _pottedByPlayer.Clear();
            _playerGroup[0] = 0;
            _playerGroup[1] = 0;
            BroadcastStatus();
        }

        // ── Public Query Methods ─────────────────────────────────────────

        /// <summary>Returns a multi-line display string summarising potted/remaining balls.</summary>
        public string GetStatusString()
        {
            _tableStyle = PlayerPrefs.GetInt("CueStrike_TableStyle", 0);
            return _tableStyle switch
            {
                1 => Build8BallStatus(),
                2 => Build9BallStatus(),
                _ => BuildSnookerStatus()
            };
        }

        // ── Private Builder Methods ──────────────────────────────────────

        private string BuildSnookerStatus()
        {
            var sb = new StringBuilder();
            sb.AppendLine("── SNOOKER ──");

            // Reds remaining
            var allPotted = GetAllPotted();
            int redsRemaining = 0;
            foreach (var id in SnookerReds)
                if (!allPotted.Contains(id)) redsRemaining++;

            sb.AppendLine($"Reds Left:  {redsRemaining} / 15");

            // Colours remaining
            var coloursLeft = new List<string>();
            foreach (var kv in SnookerColourNames)
                if (!allPotted.Contains(kv.Key)) coloursLeft.Add(kv.Value);

            sb.AppendLine($"Colours:  {(coloursLeft.Count > 0 ? string.Join(", ", coloursLeft) : "All Potted")}");
            return sb.ToString().TrimEnd();
        }

        private string Build8BallStatus()
        {
            var sb = new StringBuilder();
            sb.AppendLine("── 8-BALL ──");

            var rules = CueStrikeRulesManager.Instance;

            for (int p = 0; p < 2; p++)
            {
                string playerLabel = rules != null ? rules.playerNames[p] : $"P{p + 1}";
                string groupLabel  = _playerGroup[p] == 1 ? "Solids" :
                                     _playerGroup[p] == 2 ? "Stripes" : "TBD";

                var potted   = _pottedByPlayer.ContainsKey(p) ? _pottedByPlayer[p] : new List<int>();
                var pottedSet = new HashSet<int>(potted);

                // Balls still on the table for this player's group
                var remaining = new List<int>();
                if (_playerGroup[p] == 1)
                {
                    for (int si = 1; si <= 7; si++) if (!pottedSet.Contains(si)) remaining.Add(si);
                }
                else if (_playerGroup[p] == 2)
                {
                    for (int si = 9; si <= 15; si++) if (!pottedSet.Contains(si)) remaining.Add(si);
                }

            // Format: "P1 [Solids]: [1] [3] [5]  Left: 2,4,6,7"
            var pottedLabels   = potted.Count > 0 ? string.Join(" ", potted.ConvertAll(x => $"[{x}]")) : "--";
            var remainingLabel = remaining.Count > 0 ? string.Join(",", remaining) : (potted.Count > 0 ? "[Done!]" : "--");

                sb.AppendLine($"{playerLabel} [{groupLabel}]");
                sb.AppendLine($"  Potted: {pottedLabels}");
                sb.AppendLine($"  Left:   {remainingLabel}");
            }

            // 8-ball status
            var allPotted = GetAllPotted();
            sb.Append(allPotted.Contains(8) ? "  8-Ball: POTTED" : "  8-Ball: On Table");
            return sb.ToString().TrimEnd();
        }

        private string Build9BallStatus()
        {
            var sb = new StringBuilder();
            sb.AppendLine("── 9-BALL ──");

            var allPotted = GetAllPotted();

            var remaining = new List<int>();
            var potted    = new List<int>();

            for (int i = 1; i <= 9; i++)
            {
                if (allPotted.Contains(i)) potted.Add(i);
                else remaining.Add(i);
            }

            // Next legal ball = lowest numbered remaining
            string nextBall = remaining.Count > 0 ? $"Next: #{remaining[0]}" : "All Potted!";

            sb.AppendLine($"Potted:  {(potted.Count > 0 ? string.Join(" ", potted.ConvertAll(x => $"[{x}]")) : "–")}");
            sb.AppendLine($"Left:    {(remaining.Count > 0 ? string.Join(" ", remaining.ConvertAll(x => $"{x}")) : "Done!")}");
            sb.Append(nextBall);
            return sb.ToString().TrimEnd();
        }

        private HashSet<int> GetAllPotted()
        {
            var result = new HashSet<int>();
            foreach (var kv in _pottedByPlayer)
                foreach (var id in kv.Value) result.Add(id);
            return result;
        }

        private void BroadcastStatus()
        {
            OnBallStatusChanged?.Invoke(GetStatusString());
        }
    }
}
