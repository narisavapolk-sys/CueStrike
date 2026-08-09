using UnityEngine;

namespace CueStrike.VR
{
    /// <summary>
    /// Renders an optional 3D Aim Guide line and cue ball contact point.
    /// Toggleable via PlayerPrefs (default: OFF to preserve realism).
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public class CueStrikeAimAssist : MonoBehaviour
    {
        public static CueStrikeAimAssist Instance { get; private set; }

        [Header("Settings")]
        public Transform cueTip;
        public LayerMask targetLayers; // Cushion and Ball layers
        public float maxRayDistance = 4.0f;
        public float ballRadius = 0.02625f; // Standard Snooker radius default

        [Header("Laser Pointer Visuals")]
        public Color lineColor = new Color(1f, 0.75f, 0f, 0.8f); // Golden yellow
        public float lineWidth = 0.005f;

        private LineRenderer _lineRenderer;
        private bool _isAssistEnabled = false;

        private void Awake()
        {
            Instance = this;
            _lineRenderer = GetComponent<LineRenderer>();
            _lineRenderer.startWidth = lineWidth;
            _lineRenderer.endWidth = lineWidth;
            _lineRenderer.positionCount = 3;
            _lineRenderer.material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            _lineRenderer.material.color = lineColor;
            _lineRenderer.material.EnableKeyword("_EMISSION");
            _lineRenderer.material.SetColor("_EmissionColor", lineColor * 1.5f);

            // Read preference (default is 0/OFF)
            _isAssistEnabled = PlayerPrefs.GetInt("CueStrike_EnableAimAssist", 0) == 1;
        }

        private void Start()
        {
            // Auto detect cue tip in scene if not assigned
            if (cueTip == null)
            {
                var cue = FindFirstObjectByType<CueStrikeCue>();
                if (cue != null && cue.tipTransform != null)
                {
                    cueTip = cue.tipTransform;
                }
            }
        }

        private void LateUpdate()
        {
            if (!_isAssistEnabled || cueTip == null)
            {
                _lineRenderer.enabled = false;
                return;
            }

            _lineRenderer.enabled = true;
            Vector3 startPos = cueTip.position;
            Vector3 direction = cueTip.forward;

            // Perform spherecast to simulate cue ball path projection
            if (UnityEngine.Physics.SphereCast(startPos, ballRadius, direction, out RaycastHit hit, maxRayDistance, targetLayers))
            {
                _lineRenderer.SetPosition(0, startPos);
                _lineRenderer.SetPosition(1, hit.point);

                // If hit a cushion (represented by layer or name), show reflection
                if (hit.collider.name.ToLower().Contains("cushion") || hit.collider.name.ToLower().Contains("wall"))
                {
                    Vector3 reflectedDir = Vector3.Reflect(direction, hit.normal);
                    _lineRenderer.SetPosition(2, hit.point + reflectedDir * 0.8f);
                }
                else
                {
                    // If hit another ball, point straight to hit point
                    _lineRenderer.SetPosition(2, hit.point);
                }
            }
            else
            {
                _lineRenderer.SetPosition(0, startPos);
                _lineRenderer.SetPosition(1, startPos + direction * maxRayDistance);
                _lineRenderer.SetPosition(2, startPos + direction * maxRayDistance);
            }
        }

        /// <summary>
        /// Public method to toggle the aim assist state and save preference.
        /// </summary>
        public void ToggleAimAssist(bool enabledState)
        {
            _isAssistEnabled = enabledState;
            PlayerPrefs.SetInt("CueStrike_EnableAimAssist", enabledState ? 1 : 0);
            PlayerPrefs.Save();
            Debug.Log($"[CueStrike AimAssist] Set state to: {enabledState}");
        }
    }
}
