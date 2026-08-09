#if CUESTRIKE_NORMCORE
using UnityEngine;
using Normal.Realtime;

namespace CueStrike.Multiplayer
{
    /// <summary>
    /// Synchronizes the VR player avatar (head, hands) and the cue stick aiming positions in real-time.
    /// Implements 'Invisible Opponent Mode' (Ghost Mode) to prevent blocking the local player's view/pockets.
    /// </summary>
    public class CueStrikePlayerSync : RealtimeComponent<RealtimeTransformModel>
    {
        [Header("Avatar Components")]
        public Transform headTransform;
        public Transform leftHandTransform;
        public Transform rightHandTransform;
        public Transform cueStickTransform;

        private CueStrikeRulesManager _rules;
        private Renderer[] _renderers;
        private bool _wasHidden = false;

        private void Start()
        {
            _rules = FindFirstObjectByType<CueStrikeRulesManager>();
            // Gather all avatar mesh renderers
            _renderers = GetComponentsInChildren<Renderer>(true);
        }

        private void Update()
        {
            if (realtimeView.isOwnedLocally)
            {
                // Update local headset/hand coordinates to the network
                if (headTransform != null && Camera.main != null)
                {
                    headTransform.position = Camera.main.transform.position;
                    headTransform.rotation = Camera.main.transform.rotation;
                }

                // Sync cue stick if pointing
                var localCue = FindFirstObjectByType<CueStrikeCue>();
                if (localCue != null && cueStickTransform != null)
                {
                    cueStickTransform.position = localCue.transform.position;
                    cueStickTransform.rotation = localCue.transform.rotation;
                }
            }
            else
            {
                // Opponent player: handle Invisible Opponent / Ghost Mode check
                HandleGhostModeVisibility();
            }
        }

        /// <summary>
        /// Hide opponent's avatar renderers during the local player's turn to prevent obstructing pockets.
        /// </summary>
        private void HandleGhostModeVisibility()
        {
            if (_rules == null || _renderers == null) return;

            bool isGhostModeActive = PlayerPrefs.GetInt("CueStrike_InvisibleOpponent", 0) == 1;
            bool isMyTurn = CheckIfLocalTurn();

            // Hide opponent only when Ghost Mode is active AND it is the local player's turn to shoot
            bool shouldHideOpponent = isGhostModeActive && isMyTurn;

            if (shouldHideOpponent != _wasHidden)
            {
                _wasHidden = shouldHideOpponent;
                foreach (var rend in _renderers)
                {
                    if (rend != null) rend.enabled = !shouldHideOpponent;
                }
                Debug.Log($"[CueStrike VR] Opponent avatar visibility toggled: {!shouldHideOpponent}");
            }
        }

        private bool CheckIfLocalTurn()
        {
            if (_rules == null) return true;
            // Matches local client room ownership logic
            bool isRoomOwner = realtimeView.realtime.clientID == 0;
            int localPlayerIndex = isRoomOwner ? 0 : 1;
            return _rules.currentPlayer == localPlayerIndex;
        }
    }
}
#else
using UnityEngine;

namespace CueStrike.Multiplayer
{
    /// <summary>
    /// Fallback script to explain VR Avatar and Cue stick syncing when Normcore SDK is not present.
    /// </summary>
    public class CueStrikePlayerSync : MonoBehaviour
    {
        [Header("Normcore SDK Missing")]
        public string notice = "This component synchronizes VR Avatars and cue sticks over the network when Normcore is present.";
    }
}
#endif
