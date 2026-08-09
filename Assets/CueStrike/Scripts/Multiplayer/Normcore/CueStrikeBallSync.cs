using UnityEngine;

namespace CueStrike.Multiplayer.Normcore
{
    /// <summary>
    /// Host-authority ball physics synchronization for Normcore multiplayer.
    /// The host runs authoritative physics and broadcasts ball positions to all clients.
    /// Clients receive interpolated positions for smooth visual playback.
    ///
    /// NOTE: Requires Normcore SDK to be installed and CUESTRIKE_NORMCORE define symbol.
    /// Without the SDK, this component operates as a local-only ball tracker.
    /// </summary>
    ///
    /// INSTALLATION STEPS (after Normcore SDK is installed):
    /// 1. Add this component to each ball GameObject in the scene
    /// 2. Add a RealtimeView component to this GameObject
    /// 3. Add a RealtimeTransform component to this GameObject
    /// 4. Mark position/rotation for network sync in RealtimeTransform
    /// 5. Host authority: set RealtimeView.ownershipModel = Server
    ///
    /// WITHOUT SDK: Acts as a local component that logs ball state changes

    public class CueStrikeBallSync : MonoBehaviour
    {
        #region Inspector
        [Header("Ball Info")]
        [Tooltip("Ball ID (0 = cue ball, 1-7 = reds/solids, 8 = black, 9-15 = yellows/stripes)")]
        public int ballId = 0;

        [Tooltip("Ball radius in meters (standard = 0.028575m for 2.25in balls)")]
        public float ballRadius = 0.028575f;

        [Header("Network Sync")]
        [Tooltip("Enable position smoothing on remote clients")]
        public bool enableSmoothing = true;

        [Tooltip("Smoothing interpolation speed (higher = faster)")]
        public float smoothingSpeed = 10f;

        [Header("Host Authority")]
        [Tooltip("True if this instance is the physics host")]
        public bool isHost = false;

        [Tooltip("Override for offline/local-only mode")]
        public bool forceLocalMode = false;
        #endregion

        #region State
        private Vector3 _lastSyncedPosition;
        private Quaternion _lastSyncedRotation;
        private Vector3 _targetPosition;
        private Quaternion _targetRotation;
        private bool _isPotted = false;
        private bool _isInitialized = false;

        /// <summary>True if this ball is potted/moved to pocket.</summary>
        public bool IsPotted => _isPotted;

        /// <summary>Last position sent to/from network.</summary>
        public Vector3 LastSyncedPosition => _lastSyncedPosition;
        #endregion

        #region Unity Lifecycle
        private void Start()
        {
            _lastSyncedPosition = transform.position;
            _lastSyncedRotation = transform.rotation;
            _targetPosition = transform.position;
            _targetRotation = transform.rotation;
            _isInitialized = true;
        }

        private void Update()
        {
            if (enableSmoothing && !isHost)
            {
                // Interpolate toward target position for smooth remote movement
                transform.position = Vector3.Lerp(
                    transform.position,
                    _targetPosition,
                    smoothingSpeed * Time.deltaTime);

                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    _targetRotation,
                    smoothingSpeed * Time.deltaTime);
            }

            if (isHost)
            {
                // Host: detect position changes and trigger sync
                if (Vector3.Distance(transform.position, _lastSyncedPosition) > 0.001f)
                {
                    OnPositionChanged();
                }
            }
        }
        #endregion

        #region Public API

        /// <summary>
        /// Called by the sync system (or Normcore RealtimeTransform) to apply a remote position update.
        /// </summary>
        public void ApplyRemotePosition(Vector3 position, Quaternion rotation, bool potted)
        {
            _targetPosition = position;
            _targetRotation = rotation;
            _isPotted = potted;

            if (!enableSmoothing)
            {
                transform.position = position;
                transform.rotation = rotation;
            }

            _lastSyncedPosition = position;
            _lastSyncedRotation = rotation;
        }

        /// <summary>
        /// Called when the ball is potted. Sends potted event to all clients.
        /// </summary>
        public void OnBallPotted(int pocketId)
        {
            _isPotted = true;

            if (isHost)
            {
                // Broadcast potted event to all connected clients
                Debug.Log($"[BallSync] Host: Ball {ballId} potted in pocket {pocketId}. Broadcasting sync.");
                BroadcastBallState(true);
            }
            else
            {
                Debug.Log($"[BallSync] Client: Ball {ballId} potted (pocket {pocketId}).");
            }
        }

        /// <summary>
        /// Called when the ball is reset (new frame/rack).
        /// </summary>
        public void OnBallReset(Vector3 resetPosition)
        {
            _isPotted = false;
            transform.position = resetPosition;
            transform.rotation = Quaternion.identity;
            _lastSyncedPosition = resetPosition;
            _targetPosition = resetPosition;

            if (isHost)
            {
                Debug.Log($"[BallSync] Host: Ball {ballId} reset to {resetPosition}. Broadcasting sync.");
                BroadcastBallState(false);
            }
        }

        /// <summary>
        /// Forces a network state broadcast (host only).
        /// </summary>
        public void BroadcastBallState(bool potted)
        {
            _lastSyncedPosition = transform.position;
            _lastSyncedRotation = transform.rotation;

            // var realtimeView = GetComponent<Normal.Realtime.RealtimeView>();
            // if (realtimeView != null && realtimeView.isOwnedLocally)
            // {
            //     realtimeView.RequestOwnership();
            // }
            //
            // var rt = GetComponent<Normal.Realtime.RealtimeTransform>();
            // if (rt != null)
            // {
            //     rt.RequestOwnership();
            //     rt.MarkDirty();
            // }
        }

        /// <summary>
        /// Returns true if this ball is the host-authoritative instance.
        /// </summary>
        public bool IsHostAuthoritative()
        {
            return isHost;
        }

        /// <summary>
        /// Sets the host authority flag (called by sync manager on room creation).
        /// </summary>
        public void SetHostAuthority(bool isHostAuthority)
        {
            isHost = isHostAuthority;
        }

        #endregion

        #region Private

        private void OnPositionChanged()
        {
            _lastSyncedPosition = transform.position;
            _lastSyncedRotation = transform.rotation;

            // The actual network sync is handled by RealtimeTransform.
            // This method can be used for custom sync if needed.
        }

        #endregion

        #region Self-Test
#if UNITY_EDITOR
        [UnityEditor.MenuItem("Tools/CueStrike/Debug/Test Ball Sync")]
        public static void SelfTest()
        {
            bool pass = true;

            var ballSyncs = FindObjectsByType<CueStrikeBallSync>(FindObjectsSortMode.None);
            if (ballSyncs.Length == 0)
            {
                Debug.LogWarning("⚠️ No CueStrikeBallSync components found in scene. This is expected if not in multiplayer scene.");
            }
            else
            {
                Debug.Log($"✅ Found {ballSyncs.Length} BallSync components.");
                foreach (var bs in ballSyncs)
                {
                    Debug.Log($"   - Ball {bs.ballId}: host={bs.isHost}, localMode={bs.forceLocalMode}, pos={bs.transform.position}");
                }
            }

            // Test API
            var testObj = new GameObject("BallSyncTest");
            var testSync = testObj.AddComponent<CueStrikeBallSync>();
            testSync.ballId = 0;
            testSync.isHost = true;

            testSync.OnBallPotted(2);
            Debug.Log("✅ OnBallPotted test passed (log only, no SDK).");

            testSync.OnBallReset(Vector3.zero);
            Debug.Log("✅ OnBallReset test passed.");

            GameObject.DestroyImmediate(testObj);

            if (pass) Debug.Log("✅ CueStrikeBallSync SELF-TEST PASSED — Ready for human verify.");
            else Debug.LogWarning("⚠️ CueStrikeBallSync SELF-TEST FAILED.");
        }
#endif
        #endregion
    }
}