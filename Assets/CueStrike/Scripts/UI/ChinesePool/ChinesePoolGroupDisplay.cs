using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CueStrike.UI.ChinesePool
{
    /// <summary>
    /// Displays group assignment (Red/Yellow) and remaining balls for Chinese 8-Ball.
    /// Shows open table status and 8-ball warning.
    /// Auto-creates UI elements if not assigned in Inspector.
    /// </summary>
    public class ChinesePoolGroupDisplay : MonoBehaviour
    {
        [Header("Group Panels")]
        [SerializeField] private GameObject _redGroupPanel;
        [SerializeField] private GameObject _yellowGroupPanel;
        [SerializeField] private Image _redGroupBackground;
        [SerializeField] private Image _yellowGroupBackground;

        [Header("Ball Displays")]
        [SerializeField] private Transform _redBallsContainer;
        [SerializeField] private Transform _yellowBallsContainer;
        [SerializeField] private GameObject _ballIconPrefab;

        [Header("Status")]
        [SerializeField] private Text _openTableText;
        [SerializeField] private Text _playerGroupText;
        [SerializeField] private Text _remainingCountText;
        [SerializeField] private GameObject _eightBallWarning;

        [Header("Colors")]
        [SerializeField] private Color _redColor = new Color(0.9f, 0.1f, 0.1f, 1f);
        [SerializeField] private Color _yellowColor = new Color(0.9f, 0.8f, 0.1f, 1f);
        [SerializeField] private Color _activeGroupColor = new Color(1f, 1f, 1f, 1f);
        [SerializeField] private Color _inactiveGroupColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);

        private HashSet<int> _redBallsRemaining = new HashSet<int> { 1, 2, 3, 4, 5, 6, 7 };
        private HashSet<int> _yellowBallsRemaining = new HashSet<int> { 8, 9, 10, 11, 12, 13, 14 };
        private bool _isOpenTable = true;
        private int _playerGroup = 0; // 0=unset, 1=red, 2=yellow

        private void Awake()
        {
            EnsureUIExists();
        }

        public void Initialize()
        {
            _redBallsRemaining = new HashSet<int> { 1, 2, 3, 4, 5, 6, 7 };
            _yellowBallsRemaining = new HashSet<int> { 8, 9, 10, 11, 12, 13, 14 };
            _isOpenTable = true;
            _playerGroup = 0;
            UpdateDisplay();
        }

        private void EnsureUIExists()
        {
            // Auto-create minimal UI if not assigned
            if (_redGroupPanel == null || _yellowGroupPanel == null)
            {
                CreateMinimalUI();
            }
        }

        private void CreateMinimalUI()
        {
            Debug.Log("[ChinesePoolGroupDisplay] Auto-creating UI elements...");

            // Red group panel
            if (_redGroupPanel == null)
            {
                _redGroupPanel = new GameObject("RedGroupPanel");
                _redGroupPanel.transform.SetParent(transform, false);
                _redGroupBackground = _redGroupPanel.AddComponent<Image>();
                _redGroupBackground.color = new Color(0.9f, 0.1f, 0.1f, 0.3f);
                RectTransform rt = _redGroupPanel.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 1f);
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
            }

            // Yellow group panel
            if (_yellowGroupPanel == null)
            {
                _yellowGroupPanel = new GameObject("YellowGroupPanel");
                _yellowGroupPanel.transform.SetParent(transform, false);
                _yellowGroupBackground = _yellowGroupPanel.AddComponent<Image>();
                _yellowGroupBackground.color = new Color(0.9f, 0.8f, 0.1f, 0.3f);
                RectTransform rt = _yellowGroupPanel.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(1f, 1f);
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
            }

            // Ball containers
            if (_redBallsContainer == null)
            {
                GameObject container = new GameObject("RedBallsContainer");
                container.transform.SetParent(_redGroupPanel.transform, false);
                _redBallsContainer = container.transform;
            }
            if (_yellowBallsContainer == null)
            {
                GameObject container = new GameObject("YellowBallsContainer");
                container.transform.SetParent(_yellowGroupPanel.transform, false);
                _yellowBallsContainer = container.transform;
            }

            // Status text
            if (_playerGroupText == null)
            {
                GameObject txtObj = new GameObject("PlayerGroupText");
                txtObj.transform.SetParent(transform, false);
                _playerGroupText = txtObj.AddComponent<Text>();
                _playerGroupText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                _playerGroupText.fontSize = 24;
                _playerGroupText.color = Color.white;
                _playerGroupText.alignment = TextAnchor.MiddleCenter;
                RectTransform rt = txtObj.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0, 0.3f);
                rt.anchorMax = new Vector2(1f, 0.5f);
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
            }

            if (_remainingCountText == null)
            {
                GameObject txtObj = new GameObject("RemainingCountText");
                txtObj.transform.SetParent(transform, false);
                _remainingCountText = txtObj.AddComponent<Text>();
                _remainingCountText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                _remainingCountText.fontSize = 20;
                _remainingCountText.color = Color.white;
                _remainingCountText.alignment = TextAnchor.MiddleCenter;
                RectTransform rt = txtObj.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0, 0.1f);
                rt.anchorMax = new Vector2(1f, 0.3f);
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
            }

            // 8-ball warning
            if (_eightBallWarning == null)
            {
                GameObject warnObj = new GameObject("EightBallWarning");
                warnObj.transform.SetParent(transform, false);
                Text warnText = warnObj.AddComponent<Text>();
                warnText.text = "⚠ 8-BALL TIME!";
                warnText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                warnText.fontSize = 28;
                warnText.color = Color.red;
                warnText.alignment = TextAnchor.MiddleCenter;
                RectTransform rt = warnObj.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0, 0);
                rt.anchorMax = new Vector2(1f, 0.1f);
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                _eightBallWarning = warnObj;
                _eightBallWarning.SetActive(false);
            }

            Debug.Log("[ChinesePoolGroupDisplay] Auto-create complete.");
        }

        public void SetGroupAssignment(int player1Group, int player2Group)
        {
            _isOpenTable = false;
            if (_openTableText != null)
                _openTableText.gameObject.SetActive(false);
            UpdateDisplay();
        }

        public void SetPlayerGroup(int playerNumber, int group)
        {
            if (playerNumber == 1) _playerGroup = group;
            UpdateDisplay();
        }

        public void OnBallPotted(int ballNumber)
        {
            if (ballNumber >= 1 && ballNumber <= 7)
                _redBallsRemaining.Remove(ballNumber);
            else if (ballNumber >= 8 && ballNumber <= 14)
                _yellowBallsRemaining.Remove(ballNumber);

            UpdateDisplay();
            CheckEightBallWarning();
        }

        private void UpdateDisplay()
        {
            if (_redGroupPanel != null)
            {
                bool isPlayerRed = (_playerGroup == 1);
                var img = _redGroupPanel.GetComponent<Image>();
                if (img != null) img.color = isPlayerRed ? _activeGroupColor : _inactiveGroupColor;
            }
            if (_yellowGroupPanel != null)
            {
                bool isPlayerYellow = (_playerGroup == 2);
                var img = _yellowGroupPanel.GetComponent<Image>();
                if (img != null) img.color = isPlayerYellow ? _activeGroupColor : _inactiveGroupColor;
            }

            UpdateBallDisplay(_redBallsContainer, _redBallsRemaining, _redColor);
            UpdateBallDisplay(_yellowBallsContainer, _yellowBallsRemaining, _yellowColor);

            if (_remainingCountText != null)
            {
                int myRemaining = (_playerGroup == 1) ? _redBallsRemaining.Count :
                                  (_playerGroup == 2) ? _yellowBallsRemaining.Count : 0;
                _remainingCountText.text = $"Remaining: {myRemaining}";
            }

            if (_playerGroupText != null)
            {
                if (_playerGroup == 1) _playerGroupText.text = "YOUR GROUP: RED";
                else if (_playerGroup == 2) _playerGroupText.text = "YOUR GROUP: YELLOW";
                else _playerGroupText.text = "OPEN TABLE";
                _playerGroupText.color = (_playerGroup == 1) ? _redColor : (_playerGroup == 2) ? _yellowColor : Color.white;
            }
        }

        private void UpdateBallDisplay(Transform container, HashSet<int> remainingBalls, Color ballColor)
        {
            if (container == null) return;

            foreach (Transform child in container)
                Destroy(child.gameObject);

            if (_ballIconPrefab == null)
            {
                // Create simple text fallback
                foreach (int ball in remainingBalls)
                {
                    GameObject txtObj = new GameObject($"Ball_{ball}");
                    txtObj.transform.SetParent(container, false);
                    Text txt = txtObj.AddComponent<Text>();
                    txt.text = ball.ToString();
                    txt.color = ballColor;
                    txt.fontSize = 16;
                    txt.alignment = TextAnchor.MiddleCenter;
                    txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                }
                return;
            }

            foreach (int ball in remainingBalls)
            {
                GameObject icon = Instantiate(_ballIconPrefab, container);
                Image img = icon.GetComponent<Image>();
                if (img != null) img.color = ballColor;
                Text txt = icon.GetComponentInChildren<Text>();
                if (txt != null) txt.text = ball.ToString();
            }
        }

        private void CheckEightBallWarning()
        {
            if (_eightBallWarning == null) return;

            int myRemaining = (_playerGroup == 1) ? _redBallsRemaining.Count :
                              (_playerGroup == 2) ? _yellowBallsRemaining.Count : 99;

            _eightBallWarning.SetActive(myRemaining == 0 && !_isOpenTable);
            if (myRemaining == 0 && !_isOpenTable)
            {
                Debug.Log("[GroupDisplay] 8-BALL WARNING: Pot the black to win!");
            }
        }

        public bool IsOpenTable() => _isOpenTable;
        public bool IsEightBallTime() => (_playerGroup == 1 ? _redBallsRemaining.Count : _yellowBallsRemaining.Count) == 0;

        #region Self-Test
        public bool RunSelfTest()
        {
            EnsureUIExists();
            bool pass = true;
            if (_redGroupPanel == null || _yellowGroupPanel == null)
            {
                Debug.LogError("[Self-Test] GroupDisplay: Group panels could not be created.");
                pass = false;
            }
            if (_playerGroupText == null)
            {
                Debug.LogError("[Self-Test] GroupDisplay: Status text missing.");
                pass = false;
            }
            Debug.Log($"[Self-Test] ChinesePoolGroupDisplay: {(pass ? "PASS" : "FAIL")}");
            return pass;
        }
        #endregion
    }
}