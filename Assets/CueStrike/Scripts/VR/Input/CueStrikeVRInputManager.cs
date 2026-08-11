using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using Unity.XR.CoreUtils;
using CueStrike.Core;

namespace CueStrike.VR.Input
{
    /// <summary>
    /// Central coordinator for all VR physical input systems.
    /// Singleton that wires PhysicalShotController, StanceController, AimOrbitController,
    /// and ShotHistory together with dominant hand detection.
    /// </summary>
    [DefaultExecutionOrder(-50)] // Run before other systems
    public class CueStrikeVRInputManager : MonoBehaviour
    {
        #region Singleton
        private static CueStrikeVRInputManager _instance;
        public static CueStrikeVRInputManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    // Edit Mode fallback: find existing instance in scene
#if UNITY_2023_1_OR_NEWER
                    _instance = FindAnyObjectByType<CueStrikeVRInputManager>();
#else
                    _instance = FindFirstObjectByType<CueStrikeVRInputManager>();
#endif

                    // Runtime fallback: create if still null and we're playing
                    if (_instance == null && Application.isPlaying)
                    {
                        var go = new GameObject("CueStrikeVRInputManager");
                        _instance = go.AddComponent<CueStrikeVRInputManager>();
                    }
                }
                return _instance;
            }
        }
        #endregion

        #region Enums
        public enum HandType
        {
            Right,
            Left
        }
        #endregion

        #region Serialized Fields
        [Header("Component References")]
        [SerializeField] private CueStrikeVRInputMapping inputMapping;
        [SerializeField] private CueStrikePhysicalShotController shotController;
        [SerializeField] private CueStrikeStanceController stanceController;
        [SerializeField] private CueStrikeAimOrbitController aimOrbitController;
        [SerializeField] private CueStrikeShotHistory shotHistory;

        [Header("XR References (assigned automatically or via setup)")]
        [SerializeField] private XROrigin xrOrigin;
        [SerializeField] private Transform cameraTransform;
        [SerializeField] private UnityEngine.XR.Interaction.Toolkit.Interactors.XRBaseInputInteractor dominantHandInteractor;
        [SerializeField] private Transform dominantHandTransform;
        [SerializeField] private UnityEngine.XR.Interaction.Toolkit.Interactors.XRBaseInputInteractor offHandInteractor;
        [SerializeField] private Transform offHandTransform;

        [Header("Settings")]
        [SerializeField] private HandType dominantHand = HandType.Right;

        [Header("Debug")]
        [SerializeField] private bool verboseLogging = false;
        #endregion

        #region Private State
        private bool _isInitialized;
        private bool _optionsActionPressed;
        private bool _undoActionPressed;
        #endregion

        #region Properties
        public HandType DominantHand
        {
            get => dominantHand;
            set
            {
                if (dominantHand != value)
                {
                    dominantHand = value;
                    OnDominantHandChanged?.Invoke(value);
                    ReassignHands();
                }
            }
        }

        public CueStrikePhysicalShotController ShotController => shotController;
        public CueStrikeStanceController StanceController => stanceController;
        public CueStrikeAimOrbitController AimOrbitController => aimOrbitController;
        public CueStrikeShotHistory ShotHistory => shotHistory;
        public CueStrikeVRInputMapping InputMapping => inputMapping;
        #endregion

        #region Events
        /// <summary>Fired when dominant hand changes.</summary>
        public event Action<HandType> OnDominantHandChanged;

        /// <summary>Fired when options button is pressed.</summary>
        public event Action OnOptionsPressed;

        /// <summary>Fired when undo button is pressed.</summary>
        public event Action OnUndoPressed;
        #endregion

        #region Unity Methods
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Debug.LogWarning("[VRInputManager] Duplicate instance destroyed.");
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);

            // Load input mapping if not assigned
            if (inputMapping == null)
                inputMapping = Resources.Load<CueStrikeVRInputMapping>("VRInputMapping");

            // Load dominant hand from settings
            LoadDominantHandFromSettings();

            // Auto-find XR Origin
            if (xrOrigin == null)
                xrOrigin = FindAnyObjectByType<XROrigin>();

            if (cameraTransform == null && xrOrigin != null)
                cameraTransform = xrOrigin.Camera?.transform;

            if (verboseLogging)
                Debug.Log("[VRInputManager] Awake complete.");
        }

        private void Start()
        {
            TryAutoWire();
        }

        private void Update()
        {
            if (!_isInitialized) return;
            if (inputMapping == null) return;

            // 1. Read Options button (X/A)
            bool optionsPressed = IsActionPressedThisFrame(inputMapping.optionsButtonAction);
            if (optionsPressed && !_optionsActionPressed)
            {
                OnOptionsPressed?.Invoke();
                if (verboseLogging)
                    Debug.Log("[VRInputManager] Options button pressed.");
            }
            _optionsActionPressed = optionsPressed;

            // 2. Read Undo button (Y/B)
            bool undoPressed = IsActionPressedThisFrame(inputMapping.undoButtonAction);
            if (undoPressed && !_undoActionPressed)
            {
                OnUndoPressed?.Invoke();
                if (shotHistory != null)
                {
                    shotHistory.UndoLastShot();
                }
                if (verboseLogging)
                    Debug.Log("[VRInputManager] Undo button pressed.");
            }
            _undoActionPressed = undoPressed;
        }

        private void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
        }
        #endregion

        #region Initialization
        /// <summary>
        /// Try to auto-wire all components and detect hands.
        /// Called on Start and can be called manually via Setup tool.
        /// </summary>
        public void TryAutoWire()
        {
            // Find XR Origin if not set
            if (xrOrigin == null)
                xrOrigin = FindAnyObjectByType<XROrigin>();

            if (xrOrigin != null && cameraTransform == null)
                cameraTransform = xrOrigin.Camera?.transform;

            // Find XR Controller Interactors
            var interactors = FindObjectsByType<UnityEngine.XR.Interaction.Toolkit.Interactors.XRBaseInputInteractor>(FindObjectsSortMode.None);
            if (interactors.Length >= 2)
            {
                // Attempt to determine which is dominant based on hand name
                foreach (var interactor in interactors)
                {
                    string name = interactor.gameObject.name.ToLowerInvariant();
                    bool isRight = name.Contains("right") || name.Contains("r hand");

                    if (dominantHand == HandType.Right && isRight)
                    {
                        dominantHandInteractor = interactor;
                        dominantHandTransform = interactor.transform;
                    }
                    else if (dominantHand == HandType.Left && !isRight)
                    {
                        dominantHandInteractor = interactor;
                        dominantHandTransform = interactor.transform;
                    }
                    else
                    {
                        offHandInteractor = interactor;
                        offHandTransform = interactor.transform;
                    }
                }
            }
            else if (interactors.Length == 1)
            {
                dominantHandInteractor = interactors[0];
                dominantHandTransform = interactors[0].transform;
            }

            // Wire controllers
            if (shotController != null)
            {
                shotController.AssignDominantHand(dominantHandInteractor, dominantHandTransform);
            }

            if (stanceController != null)
            {
                stanceController.AssignXROrigin(xrOrigin?.transform);
            }

            if (aimOrbitController != null)
            {
                aimOrbitController.AssignTransforms(xrOrigin?.transform, cameraTransform);
            }

            _isInitialized = true;
            Debug.Log("[VRInputManager] Auto-wire complete." +
                      $" Dominant: {(dominantHandInteractor != null ? dominantHandInteractor.gameObject.name : "null")}" +
                      $" | Off: {(offHandInteractor != null ? offHandInteractor.gameObject.name : "null")}");
        }

        private void ReassignHands()
        {
            // Swap dominant/off-hand interactors
            var tempInteractor = dominantHandInteractor;
            var tempTransform = dominantHandTransform;
            dominantHandInteractor = offHandInteractor;
            dominantHandTransform = offHandTransform;
            offHandInteractor = tempInteractor;
            offHandTransform = tempTransform;

            if (shotController != null)
                shotController.AssignDominantHand(dominantHandInteractor, dominantHandTransform);
        }

        private void LoadDominantHandFromSettings()
        {
            // Read from CueStrikeSettingsManager
            var settings = FindAnyObjectByType<CueStrikeSettingsManager>();
            if (settings != null)
            {
                dominantHand = (settings.dominantHand == 0) ? HandType.Right : HandType.Left;
            }
            else
            {
                // Fallback to PlayerPrefs
                int savedHand = PlayerPrefs.GetInt("CueStrike_DominantHand", 0);
                dominantHand = (savedHand == 0) ? HandType.Right : HandType.Left;
            }

            if (verboseLogging)
                Debug.Log($"[VRInputManager] Dominant hand loaded: {dominantHand}");
        }
        #endregion

        #region Public API
        /// <summary>
        /// Wire everything from a setup tool (Editor).
        /// </summary>
        public void WireComponents(
            CueStrikeVRInputMapping mapping,
            CueStrikePhysicalShotController shotCtrl,
            CueStrikeStanceController stanceCtrl,
            CueStrikeAimOrbitController aimCtrl,
            CueStrikeShotHistory history,
            XROrigin origin)
        {
            inputMapping = mapping;
            shotController = shotCtrl;
            stanceController = stanceCtrl;
            aimOrbitController = aimCtrl;
            shotHistory = history;
            xrOrigin = origin;

            if (xrOrigin != null)
                cameraTransform = xrOrigin.Camera?.transform;

            TryAutoWire();
        }

        /// <summary>
        /// Simulate options button press (for testing).
        /// </summary>
        public void SimulateOptionsPress()
        {
            OnOptionsPressed?.Invoke();
        }

        /// <summary>
        /// Simulate undo button press (for testing).
        /// </summary>
        public void SimulateUndoPress()
        {
            OnUndoPressed?.Invoke();
            if (shotHistory != null)
                shotHistory.UndoLastShot();
        }
        #endregion

        #region Haptic Helpers
        /// <summary>
        /// Send haptic impulse to the dominant hand controller.
        /// </summary>
        public void SendHapticImpulse(float amplitude, float duration)
        {
            if (dominantHandInteractor != null)
            {
                var controller = dominantHandInteractor.xrController;
                if (controller != null)
                {
                    controller.SendHapticImpulse(amplitude, duration);
                }
            }
        }

        /// <summary>
        /// Send haptic impulse to the off-hand controller.
        /// </summary>
        public void SendOffHandHapticImpulse(float amplitude, float duration)
        {
            if (offHandInteractor != null)
            {
                var controller = offHandInteractor.xrController;
                if (controller != null)
                {
                    controller.SendHapticImpulse(amplitude, duration);
                }
            }
        }
        #endregion

        #region Input Helpers
        private static bool IsActionPressedThisFrame(InputActionReference actionRef)
        {
            if (actionRef == null || actionRef.action == null) return false;
            try
            {
                return actionRef.action.WasPressedThisFrame();
            }
            catch
            {
                return false;
            }
        }
        #endregion
    }
}