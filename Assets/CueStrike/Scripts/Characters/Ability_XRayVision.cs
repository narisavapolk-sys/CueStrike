using UnityEngine;

namespace CueStrike.Characters
{
    /// <summary>
    /// Bones — X-Ray Vision: See optimal pocket path in green glow.
    /// Spin preview arrows. X-Ray skeleton visible.
    /// </summary>
    public class Ability_XRayVision : CueStrikeCharacterAbility
    {
        [Header("X-Ray Vision Settings")]
        [SerializeField] private LineRenderer _optimalPathLine;
        [SerializeField] private Color _pathColor = new Color(0f, 1f, 0.2f, 0.5f);
        [SerializeField] private GameObject _skeletonVisual;
        [SerializeField] private GameObject _spinArrowPrefab;

        public override string AbilityName => "X-Ray Vision";
        public override string AbilityDescription => "Hold to see the optimal pocket path (green glow) and spin direction preview.";

        protected override void Awake()
        {
            base.Awake();
            if (_optimalPathLine == null)
            {
                _optimalPathLine = gameObject.AddComponent<LineRenderer>();
                _optimalPathLine.startWidth = 0.03f;
                _optimalPathLine.endWidth = 0.03f;
                _optimalPathLine.material = new Material(Shader.Find("Sprites/Default"));
                _optimalPathLine.startColor = _pathColor;
                _optimalPathLine.endColor = _pathColor;
                _optimalPathLine.positionCount = 20;
                _optimalPathLine.enabled = false;
            }
            if (_skeletonVisual != null) _skeletonVisual.SetActive(false);
        }

        protected override void HandleInput()
        {
            if (Input.GetKey(_activationKey) && !_isOnCooldown)
            {
                ShowXRay();
            }
            else if (Input.GetKeyUp(_activationKey))
            {
                HideXRay();
            }
        }

        private void ShowXRay()
        {
            if (_optimalPathLine != null)
            {
                _optimalPathLine.enabled = true;
                UpdateOptimalPath();
            }
            if (_skeletonVisual != null) _skeletonVisual.SetActive(true);
        }

        private void HideXRay()
        {
            if (_optimalPathLine != null) _optimalPathLine.enabled = false;
            if (_skeletonVisual != null) _skeletonVisual.SetActive(false);
        }

        private void UpdateOptimalPath()
        {
            // Placeholder: would calculate optimal path from cue ball to pocket
            for (int i = 0; i < 20; i++)
            {
                float t = i / 19f;
                Vector3 pos = transform.position + transform.forward * t * 4f;
                _optimalPathLine.SetPosition(i, pos);
            }
        }

        public override bool RunSelfTest()
        {
            bool pass = base.RunSelfTest();
            if (_optimalPathLine == null)
            {
                Debug.LogError("[Self-Test] XRayVision: Optimal path LineRenderer missing.");
                pass = false;
            }
            return pass;
        }
    }
}