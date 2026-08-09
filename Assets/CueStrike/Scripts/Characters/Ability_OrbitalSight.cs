using UnityEngine;

namespace CueStrike.Characters
{
    /// <summary>
    /// MeiLing — Orbital Sight: Pink glowing trajectory line with resonant sweep sound.
    /// Toggle visual aid during aiming.
    /// </summary>
    public class Ability_OrbitalSight : CueStrikeCharacterAbility
    {
        [Header("Orbital Sight Settings")]
        [SerializeField] private LineRenderer _trajectoryLine;
        [SerializeField] private Color _lineColor = new Color(1f, 0.4f, 0.7f, 0.8f);
        [SerializeField] private int _trajectoryPoints = 50;
        [SerializeField] private float _trajectoryLength = 3f;

        public override string AbilityName => "Orbital Sight";
        public override string AbilityDescription => "Glowing pink trajectory line guides your shot with resonant audio feedback.";

        protected override void Awake()
        {
            base.Awake();
            if (_trajectoryLine == null)
            {
                _trajectoryLine = gameObject.AddComponent<LineRenderer>();
                _trajectoryLine.startWidth = 0.02f;
                _trajectoryLine.endWidth = 0.02f;
                _trajectoryLine.material = new Material(Shader.Find("Sprites/Default"));
                _trajectoryLine.startColor = _lineColor;
                _trajectoryLine.endColor = _lineColor;
                _trajectoryLine.positionCount = _trajectoryPoints;
            }
            _trajectoryLine.enabled = false;
        }

        protected override void HandleInput()
        {
            // Hold to show trajectory
            if (Input.GetKey(_activationKey))
            {
                ShowTrajectory();
            }
            else if (Input.GetKeyUp(_activationKey))
            {
                HideTrajectory();
            }
        }

        private void ShowTrajectory()
        {
            if (_trajectoryLine == null) return;
            _trajectoryLine.enabled = true;
            UpdateTrajectoryVisual();
        }

        private void HideTrajectory()
        {
            if (_trajectoryLine != null) _trajectoryLine.enabled = false;
        }

        private void UpdateTrajectoryVisual()
        {
            // Placeholder: would calculate actual ball path
            for (int i = 0; i < _trajectoryPoints; i++)
            {
                float t = (float)i / _trajectoryPoints;
                Vector3 pos = transform.position + transform.forward * _trajectoryLength * t;
                _trajectoryLine.SetPosition(i, pos);
            }
        }

        public override bool RunSelfTest()
        {
            bool pass = base.RunSelfTest();
            if (_trajectoryLine == null)
            {
                Debug.LogError("[Self-Test] OrbitalSight: LineRenderer missing.");
                pass = false;
            }
            return pass;
        }
    }
}