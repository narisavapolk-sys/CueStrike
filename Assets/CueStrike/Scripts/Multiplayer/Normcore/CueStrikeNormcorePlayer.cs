using UnityEngine;

namespace CueStrike.Multiplayer.Normcore
{
    /// <summary>
    /// Represents a networked player in a Normcore multiplayer session.
    /// Handles position/rotation sync, ready state, and score tracking.
    /// In offline mode, acts as a local player representation.
    /// </summary>
    public class CueStrikeNormcorePlayer : MonoBehaviour
    {
        #region Properties
        [Header("Player Info")]
        [SerializeField] private int _playerId = -1;
        [SerializeField] private string _playerName = "Unknown";
        [SerializeField] private bool _isReady = false;
        [SerializeField] private int _score = 0;

        /// <summary>Unique player ID assigned by NormcoreManager.</summary>
        public int PlayerId
        {
            get => _playerId;
            set => _playerId = value;
        }

        /// <summary>Display name of the player.</summary>
        public string PlayerName
        {
            get => _playerName;
            set => _playerName = value;
        }

        /// <summary>Whether the player has signalled ready.</summary>
        public bool IsReady
        {
            get => _isReady;
            set
            {
                _isReady = value;
                UpdateReadyVisual();
            }
        }

        /// <summary>Current score for this player.</summary>
        public int Score
        {
            get => _score;
            set => _score = Mathf.Max(0, value);
        }

        /// <summary>The transform used for avatar positioning.</summary>
        public Transform AvatarTransform { get; set; }
        #endregion

        #region Lifecycle
        private void Start()
        {
            AvatarTransform = transform;
            UpdateReadyVisual();
        }
        #endregion

        #region Public Methods

        /// <summary>
        /// Updates the player's world position and rotation.
        /// In a networked context, this is called from the sync system.
        /// </summary>
        public void UpdatePosition(Vector3 position, Quaternion rotation)
        {
            transform.position = position;
            transform.rotation = rotation;
        }

        /// <summary>
        /// Sets the player's ready state. Usually called from lobby UI.
        /// </summary>
        public void SetReady(bool ready)
        {
            IsReady = ready;

            var mgr = CueStrikeNormcoreManager.Instance;
            if (mgr != null && mgr.LocalPlayerId == _playerId)
            {
                mgr.SetReady(ready);
            }

            Debug.Log($"[NormcorePlayer] {_playerName} ready={ready}");
        }

        /// <summary>
        /// Adds points to the player's score.
        /// </summary>
        public void AddScore(int points)
        {
            _score += Mathf.Abs(points);
            Debug.Log($"[NormcorePlayer] {_playerName} score += {points} (total: {_score})");
        }

        /// <summary>
        /// Resets the player for a new frame/match.
        /// </summary>
        public void ResetForNewFrame()
        {
            _score = 0;
            _isReady = false;
            UpdateReadyVisual();
            Debug.Log($"[NormcorePlayer] {_playerName} reset for new frame.");
        }

        #endregion

        #region Private

        private void UpdateReadyVisual()
        {
            // In a full implementation, toggle a visual indicator (e.g., checkmark, color change)
            // For now, we log the state change.
        }

        #endregion
    }
}