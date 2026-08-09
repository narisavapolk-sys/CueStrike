using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;

namespace CueStrike.VR
{
    /// <summary>
    /// VR-Safe Canvas Fixer — auto-fixes every Canvas in the scene so it
    /// does NOT follow the HMD (no "UI glued to face" bug).
    ///
    /// Attach to any persistent GameObject (e.g. XR Origin or GameManager).
    ///
    /// Strategy per Canvas type:
    ///   - HUD / Game Canvases  → World Space, anchored 0.8 m in front of
    ///                            the player's INITIAL spawn position (fixed in world).
    ///   - Overlay / Screen UI  → World Space billboard that faces the camera
    ///                            but stays FIXED in world coords (not parented to camera).
    ///
    /// What we deliberately DO NOT do:
    ///   - We never parent any Canvas to the Camera/XR rig transform.
    ///   - We never use Screen Space – Camera render mode in XR
    ///     (it causes the "UI moves with head" bug on Quest / OpenXR).
    ///   - We never modify the XR Origin or Tracked Pose Driver.
    /// </summary>
    [DefaultExecutionOrder(-100)] // Run before any UI scripts
    public class CueStrikeVRCanvasFixer : MonoBehaviour
    {
        [Header("World-Space Canvas Settings")]
        [Tooltip("Distance from player spawn point to place fixed-world canvases (meters)")]
        public float uiDistance = 0.8f;

        [Tooltip("Height offset from player spawn point")]
        public float uiHeightOffset = 0.0f;

        [Tooltip("Scale for World Space canvas (smaller = sharper in VR)")]
        public float canvasScale = 0.002f;

        [Tooltip("If true, also fix any canvases that are children of XR Origin / Camera")]
        public bool fixCameraChildCanvases = true;

        // ── Runtime ────────────────────────────────────────────────────
        private Camera _vrCamera;
        private Vector3 _playerSpawnPos;

        private void Awake()
        {
            // Find the VR/main camera
            _vrCamera = Camera.main;
            if (_vrCamera == null)
                _vrCamera = FindFirstObjectByType<Camera>();

            // Capture spawn position BEFORE any movement
            _playerSpawnPos = _vrCamera != null
                ? _vrCamera.transform.position
                : Vector3.zero;
        }

        private void Start()
        {
            FixAllCanvases();
        }

        /// <summary>
        /// Iterates all active Canvases and ensures VR-safe render mode + placement.
        /// </summary>
        public void FixAllCanvases()
        {
            bool isVRActive = XRSettings.isDeviceActive;

            var allCanvases = FindObjectsByType<Canvas>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

            foreach (var canvas in allCanvases)
            {
                FixCanvas(canvas, isVRActive);
            }

            Debug.Log($"[CueStrikeVRCanvasFixer] Fixed {allCanvases.Length} canvas(es). VR active: {isVRActive}");
        }

        private void FixCanvas(Canvas canvas, bool isVR)
        {
            // Skip canvases explicitly tagged to be left alone
            if (canvas.CompareTag("VRCanvasFixed")) return;

            // ── Non-VR: Screen Space Overlay is fine ──────────────────
            if (!isVR)
            {
                if (canvas.renderMode == RenderMode.ScreenSpaceOverlay) return;
                // Only touch World Space canvases in non-VR — leave them be
                return;
            }

            // ── VR: All canvases must be World Space ──────────────────

            // 1. Detach from camera if accidentally parented
            if (fixCameraChildCanvases && IsChildOfCameraOrXROrigin(canvas.transform))
            {
                canvas.transform.SetParent(null, true); // worldPositionStays = true
                Debug.Log($"[VRCanvasFixer] Detached '{canvas.name}' from camera/XR hierarchy.");
            }

            // 2. Set World Space render mode
            if (canvas.renderMode != RenderMode.WorldSpace)
            {
                canvas.renderMode = RenderMode.WorldSpace;
            }

            // 3. Assign the event camera for raycasting (required for XR Interaction Toolkit)
            if (canvas.worldCamera == null && _vrCamera != null)
            {
                canvas.worldCamera = _vrCamera;
            }

            // 4. Ensure Graphic Raycaster exists (needed for XR UI Input Module)
            if (canvas.GetComponent<GraphicRaycaster>() == null)
            {
                canvas.gameObject.AddComponent<GraphicRaycaster>();
            }

            // 5. Position canvas FIXED in world — in front of spawn point, NOT camera
            //    This means it stays put while the player looks around
            var rt = canvas.GetComponent<RectTransform>();
            if (rt != null)
            {
                // Place the canvas in front of initial spawn position
                Vector3 forward = _vrCamera != null
                    ? Vector3.ProjectOnPlane(_vrCamera.transform.forward, Vector3.up).normalized
                    : Vector3.forward;

                // Only reposition if the canvas is at origin or suspiciously close to camera
                float distToCamera = _vrCamera != null
                    ? Vector3.Distance(canvas.transform.position, _vrCamera.transform.position)
                    : 999f;

                bool isStuckToCamera = distToCamera < 0.5f;

                if (isStuckToCamera || canvas.transform.position == Vector3.zero)
                {
                    canvas.transform.position = _playerSpawnPos
                        + forward * uiDistance
                        + Vector3.up * uiHeightOffset;

                    // Face the canvas toward the spawn position (player faces it)
                    canvas.transform.rotation = Quaternion.LookRotation(
                        canvas.transform.position - _playerSpawnPos
                    );
                }

                // 6. Set readable scale (1 unit = 1 meter is too big; 0.002 = ~2mm per pixel)
                if (rt.localScale.x > 0.05f || rt.localScale.x < 0.0001f)
                {
                    rt.localScale = Vector3.one * canvasScale;
                }
            }

            Debug.Log($"[VRCanvasFixer] '{canvas.name}' → WorldSpace, dist:{uiDistance}m, scale:{canvasScale}");
        }

        /// <summary>
        /// Checks if a transform is under the XR Origin, XR Rig, or any Camera.
        /// </summary>
        private bool IsChildOfCameraOrXROrigin(Transform t)
        {
            Transform current = t.parent;
            while (current != null)
            {
                if (current.GetComponent<Camera>() != null) return true;
                string n = current.name.ToLower();
                if (n.Contains("xr origin") || n.Contains("xr rig") ||
                    n.Contains("xrorigin")  || n.Contains("xrrig")  ||
                    n.Contains("camera rig"))
                    return true;
                current = current.parent;
            }
            return false;
        }
    }
}
