using UnityEngine;

namespace CueStrike.Characters.Phantom
{
    /// <summary>
    /// Phantom — Spectral Sight ability.
    /// See through obstructing balls, reflected trajectory lines, ghost walk.
    /// </summary>
    public class PhantomSpectralSight : MonoBehaviour, ICharacterAbility
    {
        [Header("Spectral Settings")]
        public float seeThroughRange = 5f;
        public float reflectionBounceCount = 3;
        public LayerMask ballLayer = -1;

        [Header("Visual")]
        public Material ghostCueBallMaterial;
        public LineRenderer trajectoryLine;
        public GameObject seeThroughIndicator;

        // State
        private bool _isActive = false;
        private Camera _playerCamera;

        public string AbilityName => "Spectral Sight";
        public string AbilityDescription => "See through obstructing balls, reflected trajectory, ghost movement.";

        public void OnCharacterSpawned()
        {
            _isActive = true;
            _playerCamera = Camera.main;

            if (trajectoryLine != null)
            {
                trajectoryLine.startColor = new Color(0.7f, 0.2f, 1f, 0.6f);
                trajectoryLine.endColor = new Color(0.4f, 0.1f, 0.8f, 0.2f);
                trajectoryLine.startWidth = 0.015f;
                trajectoryLine.endWidth = 0.005f;
                trajectoryLine.enabled = true;
            }

            Debug.Log("[Phantom] Spectral Sight active. I see through balls...");
        }

        public float GetAccuracyModifier() => 0f;
        public float GetPowerModifier() => 1f;
        public float GetSpeedModifier() => 1f;
        public float GetVisibilityBonus() => 0.5f; // Major visibility bonus
        public bool IsAbilityActive() => _isActive;

        void Update()
        {
            if (!_isActive) return;
            UpdateSeeThrough();
            UpdateTrajectory();
        }

        /// <summary>
        /// Update see-through effect — make obstructing balls transparent
        /// </summary>
        private void UpdateSeeThrough()
        {
            if (_playerCamera == null) return;

            Ray ray = new Ray(_playerCamera.transform.position, _playerCamera.transform.forward);
            RaycastHit[] hits = UnityEngine.Physics.SphereCastAll(ray, 0.1f, seeThroughRange, ballLayer);

            foreach (var hit in hits)
            {
                Renderer r = hit.collider.GetComponent<Renderer>();
                if (r != null && r.material != null)
                {
                    Color c = r.material.GetColor("_BaseColor");
                    c.a = Mathf.Lerp(c.a, 0.3f, Time.deltaTime * 5f);
                    r.material.SetColor("_BaseColor", c);
                    r.material.SetFloat("_Surface", 1f); // Transparent
                }
            }
        }

        /// <summary>
        /// Draw reflected trajectory line showing bank shots
        /// </summary>
        private void UpdateTrajectory()
        {
            if (trajectoryLine == null || !trajectoryLine.enabled) return;

            // Simplified trajectory — draw a curved line showing potential reflection path
            Vector3 start = transform.position + transform.forward * 0.5f;
            Vector3 end = start + transform.forward * 3f;
            Vector3 reflection = Vector3.Reflect(transform.forward, Vector3.right);

            trajectoryLine.positionCount = 10;
            for (int i = 0; i < 10; i++)
            {
                float t = i / 9f;
                Vector3 pos;
                if (t < 0.5f)
                {
                    // First half: direct path
                    float p = t * 2f;
                    pos = Vector3.Lerp(start, end, p);
                }
                else
                {
                    // Second half: reflected path
                    float p = (t - 0.5f) * 2f;
                    pos = Vector3.Lerp(end, end + reflection * 2f * p, p);
                }
                trajectoryLine.SetPosition(i, pos);
            }
        }

        /// <summary>
        /// Toggle ghost walk — make character semi-transparent
        /// </summary>
        public void ToggleGhostWalk()
        {
            var renderers = GetComponentsInChildren<Renderer>();
            foreach (var r in renderers)
            {
                foreach (var mat in r.materials)
                {
                    Color c = mat.GetColor("_BaseColor");
                    if (mat.HasProperty("_BaseColor"))
                    {
                        c.a = c.a > 0.5f ? 0.3f : 1f;
                        mat.SetColor("_BaseColor", c);
                    }
                    mat.SetFloat("_Surface", c.a < 1f ? 1f : 0f);
                }
            }

            Debug.Log("[Phantom] Ghost walk toggled.");
        }

        /// <summary>
        /// Reset ball transparency after shot
        /// </summary>
        public void ResetBallVisibility()
        {
            var balls = FindObjectsOfType<Rigidbody>();
            foreach (var ball in balls)
            {
                Renderer r = ball.GetComponent<Renderer>();
                if (r != null && r.material != null)
                {
                    Color c = r.material.GetColor("_BaseColor");
                    c.a = 1f;
                    r.material.SetColor("_BaseColor", c);
                }
            }
        }
    }
}