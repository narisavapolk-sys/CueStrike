#if CUESTRIKE_NORMCORE
using UnityEngine;
using Normal.Realtime;

namespace CueStrike.Multiplayer
{
    /// <summary>
    /// Synchronizes the 3D physics of the ball using Normcore's RealtimeTransform.
    /// Manages physics ownership dynamically based on whose turn it is.
    /// </summary>
    [RequireComponent(typeof(RealtimeTransform))]
    [RequireComponent(typeof(Rigidbody))]
    public class CueStrikeBallSync : MonoBehaviour
    {
        private RealtimeTransform _realtimeTransform;
        private Rigidbody _rigidbody;
        private CueStrikeRulesManager _rules;

        private void Awake()
        {
            _realtimeTransform = GetComponent<RealtimeTransform>();
            _rigidbody = GetComponent<Rigidbody>();
            _rules = FindFirstObjectByType<CueStrikeRulesManager>();
        }

        private void Update()
        {
            if (_realtimeTransform == null || _rules == null) return;

            // Check if it is the local player's turn (e.g., matching the local client index)
            // Replace with custom local player ID matching once matchmaking is fully wired
            bool isMyTurn = CheckIfMyTurn();

            if (isMyTurn)
            {
                // We have turn authority: We calculate physics and push state to network
                if (!_realtimeTransform.isOwnedLocally)
                {
                    _realtimeTransform.RequestOwnership();
                    _rigidbody.isKinematic = false;
                }
            }
            else
            {
                // Waiting player: Interpolate from network inputs, disable local gravity collisions
                if (_realtimeTransform.isOwnedLocally)
                {
                    _rigidbody.isKinematic = true; // Let network drive the positions
                }
            }
        }

        private bool CheckIfMyTurn()
        {
            if (_rules == null) return true;
            
            // Assume Player 1 is the room owner (index 0) and Player 2 is client
            // Can be expanded as matchmaking logic grows
            bool isRoomOwner = _realtimeTransform.realtime.clientID == 0;
            int myIndex = isRoomOwner ? 0 : 1;
            return _rules.currentPlayer == myIndex;
        }
    }
}
#else
using UnityEngine;

namespace CueStrike.Multiplayer
{
    /// <summary>
    /// Fallback script to explain Ball physics sync when Normcore SDK is not present.
    /// </summary>
    public class CueStrikeBallSync : MonoBehaviour
    {
        [Header("Normcore SDK Missing")]
        public string notice = "This component synchronizes ball physics and collisions over the internet when Normcore is present.";
    }
}
#endif
