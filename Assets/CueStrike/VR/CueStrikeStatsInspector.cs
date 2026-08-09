using UnityEngine;
using UnityEngine.XR;

namespace CueStrike.VR
{
    /// <summary>
    /// Fires a laser raycast from the VR hand controller.
    /// Pointing at the opponent avatar collider projects a floating holographic stats card above their head.
    /// </summary>
    public class CueStrikeStatsInspector : MonoBehaviour
    {
        [Header("Laser Pointer Settings")]
        public XRNode handNode = XRNode.RightHand;
        public float maxRayDistance = 10f;
        public Color laserColor = new Color(0f, 1f, 0.5f, 0.6f); // Neon green

        private LineRenderer _lineRenderer;
        private GameObject _floatingCardGO;
        private TextMesh _cardText;
        private Transform _lastHitTransform;
        private float _cardAlpha = 0f;

        private void Start()
        {
            SetupLaserRenderer();
            SetupHoloStatsCard();
        }

        private void Update()
        {
            PerformLaserScan();
        }

        private void SetupLaserRenderer()
        {
            _lineRenderer = gameObject.AddComponent<LineRenderer>();
            _lineRenderer.startWidth = 0.005f;
            _lineRenderer.endWidth = 0.002f;
            _lineRenderer.material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            _lineRenderer.material.color = laserColor;
            _lineRenderer.positionCount = 2;
        }

        private void SetupHoloStatsCard()
        {
            _floatingCardGO = new GameObject("OpponentHoloStatsCard");
            _floatingCardGO.SetActive(false);

            _cardText = _floatingCardGO.AddComponent<TextMesh>();
            _cardText.fontSize = 32;
            _cardText.characterSize = 0.03f;
            _cardText.anchor = TextAnchor.MiddleCenter;
            _cardText.alignment = TextAlignment.Center;
            _cardText.color = new Color(0f, 1f, 0.7f, 1f); // Turquoise neon

            // Face camera automatically
            _floatingCardGO.AddComponent<CueStrikeBallLabels>();
        }

        private void PerformLaserScan()
        {
            var device = InputDevices.GetDeviceAtXRNode(handNode);
            if (!device.isValid)
            {
                _lineRenderer.enabled = false;
                HideCard();
                return;
            }

            _lineRenderer.enabled = true;
            Vector3 rayStart = transform.position;
            Vector3 rayDir = transform.forward;

            _lineRenderer.SetPosition(0, rayStart);

            if (UnityEngine.Physics.Raycast(rayStart, rayDir, out RaycastHit hit, maxRayDistance))
            {
                _lineRenderer.SetPosition(1, hit.point);

                // Detect opponent avatar collider (by layer or name check)
                string hitName = hit.collider.name.ToLower();
                if (hitName.Contains("avatar") || hitName.Contains("character") || hitName.Contains("opponent") || hitName.Contains("somchay") || hitName.Contains("gentleman") || hitName.Contains("meiling"))
                {
                    ShowCardAboveOpponent(hit.collider.transform);
                }
                else
                {
                    HideCard();
                }
            }
            else
            {
                _lineRenderer.SetPosition(1, rayStart + rayDir * maxRayDistance);
                HideCard();
            }
        }

        private void ShowCardAboveOpponent(Transform opponentT)
        {
            if (_floatingCardGO == null || _cardText == null) return;

            // Position card 0.5 meters above opponent avatar root/transform
            _floatingCardGO.transform.position = opponentT.position + Vector3.up * 1.8f;
            _floatingCardGO.SetActive(true);

            // Fetch or mock opponent stats
            if (opponentT != _lastHitTransform)
            {
                _lastHitTransform = opponentT;
                GenerateOpponentStatsCard(opponentT.name);
                // Trigger quick haptic pulse on hit
                CueStrikeHapticManager.SendHapticImpulse(handNode, 0.3f, 0.05f);
            }

            // Interpolate alpha to fade in
            _cardAlpha = Mathf.MoveTowards(_cardAlpha, 1f, Time.deltaTime * 4f);
            _cardText.color = new Color(_cardText.color.r, _cardText.color.g, _cardText.color.b, _cardAlpha);
        }

        private void HideCard()
        {
            if (_floatingCardGO == null) return;

            _cardAlpha = Mathf.MoveTowards(_cardAlpha, 0f, Time.deltaTime * 3f);
            if (_cardAlpha <= 0f)
            {
                _floatingCardGO.SetActive(false);
                _lastHitTransform = null;
            }
            else if (_cardText != null)
            {
                _cardText.color = new Color(_cardText.color.r, _cardText.color.g, _cardText.color.b, _cardAlpha);
            }
        }

        private void GenerateOpponentStatsCard(string opponentName)
        {
            if (_cardText == null) return;

            // Strip clone/prefab tag formatting for display
            string cleanName = opponentName.Replace("(Clone)", "").Replace("Prefab", "").Trim();
            if (string.IsNullOrEmpty(cleanName)) cleanName = "Opponent";

            // Generate mock stats based on opponent name to simulate network query
            int seed = cleanName.GetHashCode();
            Random.InitState(seed);

            int played = Random.Range(30, 250);
            int won = Mathf.RoundToInt(played * Random.Range(0.4f, 0.75f));
            int lost = played - won;
            int rageQuits = Random.Range(0, Mathf.RoundToInt(played * 0.05f));
            int maxBreak = Random.Range(20, 147);

            _cardText.text = $"<b>--- PLAYER PROFILE ---</b>\n" +
                             $"Name: <color=yellow>{cleanName.ToUpper()}</color>\n" +
                             $"Matches: {played}   |   W/L: {won}/{lost}\n" +
                             $"Rage Quits: <color=red>{rageQuits}</color>\n" +
                             $"Max Break: <color=gold>{maxBreak}</color>\n" +
                             $"-------------------------";
        }
    }
}
