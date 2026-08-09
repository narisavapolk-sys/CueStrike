using UnityEngine;

namespace CueStrike.Characters.Bones
{
    /// <summary>
    /// Bones — X-Ray Vision ability.
    /// See optimal pocket path glowing green. Show spin direction preview.
    /// </summary>
    public class BonesXRayVision : MonoBehaviour, ICharacterAbility
    {
        [Header("X-Ray Settings")]
        public float maxPocketSearchDistance = 5f;
        public Color optimalPathColor = Color.green;
        public Color suboptimalPathColor = new Color(0.5f, 1f, 0.5f, 0.3f);

        [Header("Visual")]
        public LineRenderer pathLine;
        public GameObject xRayOverlay;
        public Light xRayLight;

        // State
        private bool _isActive = false;
        private Camera _playerCamera;
        private Vector3[] _pocketPositions;

        public string AbilityName => "X-Ray Vision";
        public string AbilityDescription => "See optimal pocket path in green. Spin direction preview. Find the best shot.";

        public void OnCharacterSpawned()
        {
            _isActive = true;
            _playerCamera = Camera.main;

            if (pathLine != null)
            {
                pathLine.startColor = optimalPathColor;
                pathLine.endColor = new Color(0f, 0.5f, 0f, 0.3f);
                pathLine.startWidth = 0.02f;
                pathLine.endWidth = 0.005f;
                pathLine.positionCount = 20;
                pathLine.enabled = true;
            }

            // Discover table pocket positions
            FindPocketPositions();

            if (xRayLight != null)
            {
                xRayLight.color = optimalPathColor;
                xRayLight.enabled = true;
            }

            Debug.Log("[Bones] X-Ray Vision active. I see the path...");
        }

        public float GetAccuracyModifier() => 0.05f; // Small accuracy bonus
        public float GetPowerModifier() => 1f;
        public float GetSpeedModifier() => 1f;
        public float GetVisibilityBonus() => 0.4f; // Major visibility
        public bool IsAbilityActive() => _isActive;

        void Update()
        {
            if (!_isActive) return;
            UpdatePathLine();
        }

        /// <summary>
        /// Find pocket positions in scene
        /// </summary>
        private void FindPocketPositions()
        {
            var pockets = GameObject.FindGameObjectsWithTag("Pocket");
            if (pockets.Length > 0)
            {
                _pocketPositions = new Vector3[pockets.Length];
                for (int i = 0; i < pockets.Length; i++)
                    _pocketPositions[i] = pockets[i].transform.position;
            }
            else
            {
                // Fallback: scan by name
                var all = FindObjectsOfType<Transform>();
                int count = 0;
                foreach (var t in all)
                {
                    if (t.name.ToLower().Contains("pocket"))
                        count++;
                }

                _pocketPositions = new Vector3[count];
                int idx = 0;
                foreach (var t in all)
                {
                    if (t.name.ToLower().Contains("pocket"))
                        _pocketPositions[idx++] = t.position;
                }
            }

            Debug.Log($"[Bones] Found {_pocketPositions.Length} pocket positions.");
        }

        /// <summary>
        /// Update path line showing optimal trajectory
        /// </summary>
        private void UpdatePathLine()
        {
            if (pathLine == null || !pathLine.enabled) return;

            if (_pocketPositions == null || _pocketPositions.Length == 0)
            {
                pathLine.enabled = false;
                return;
            }

            // Find nearest pocket
            Vector3 nearestPocket = FindNearestPocket();
            Vector3 start = transform.position + transform.forward * 0.3f;
            Vector3 end = nearestPocket;

            // Draw curved path
            Vector3 mid = Vector3.Lerp(start, end, 0.5f);
            mid.y += 0.3f; // Arc slightly up

            for (int i = 0; i < pathLine.positionCount; i++)
            {
                float t = i / (float)(pathLine.positionCount - 1);
                Vector3 p0 = Vector3.Lerp(start, mid, t);
                Vector3 p1 = Vector3.Lerp(mid, end, t);
                Vector3 pos = Vector3.Lerp(p0, p1, t);
                pathLine.SetPosition(i, pos);
            }

            // Pulse the line
            float pulse = 0.7f + Mathf.Sin(Time.time * 2f) * 0.3f;
            pathLine.startColor = new Color(0f, pulse, 0f, 0.8f);
        }

        /// <summary>
        /// Find nearest pocket to the player's aim direction
        /// </summary>
        private Vector3 FindNearestPocket()
        {
            if (_pocketPositions == null || _pocketPositions.Length == 0)
                return transform.position + transform.forward * 3f;

            Vector3 bestPocket = _pocketPositions[0];
            float bestScore = float.MaxValue;

            foreach (var pocket in _pocketPositions)
            {
                Vector3 dirToPocket = (pocket - transform.position).normalized;
                float dot = Vector3.Dot(transform.forward, dirToPocket);
                float dist = Vector3.Distance(transform.position, pocket);

                // Score: prefer pockets in line of sight and close
                float score = dist / Mathf.Max(dot, 0.1f);
                if (score < bestScore && dot > -0.1f)
                {
                    bestScore = score;
                    bestPocket = pocket;
                }
            }

            return bestPocket;
        }

        /// <summary>
        /// Show spin direction as arrow overlay
        /// </summary>
        public void ShowSpinPreview(Vector2 spinDirection)
        {
            if (xRayOverlay != null)
            {
                xRayOverlay.SetActive(true);
                // Rotate overlay to match spin
                float angle = Mathf.Atan2(spinDirection.y, spinDirection.x) * Mathf.Rad2Deg;
                xRayOverlay.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
            }
        }

        /// <summary>
        /// Hide spin preview
        /// </summary>
        public void HideSpinPreview()
        {
            if (xRayOverlay != null)
                xRayOverlay.SetActive(false);
        }
    }
}