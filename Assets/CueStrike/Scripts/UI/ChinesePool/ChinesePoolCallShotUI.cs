using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CueStrike.UI.ChinesePool
{
    /// <summary>
    /// Call Shot UI for Chinese 8-Ball.
    /// Player must declare which ball and which pocket before shooting.
    /// </summary>
    public class ChinesePoolCallShotUI : MonoBehaviour
    {
        #region Events
        public event System.Action<int, int> OnShotCalled; // ballNumber, pocketIndex
        public event System.Action OnCallShotCancelled;
        #endregion

        [Header("UI Elements")]
        [SerializeField] private GameObject _callShotPanel;
        [SerializeField] private Text _titleText;
        [SerializeField] private Text _instructionText;
        [SerializeField] private Transform _ballSelectionGrid;
        [SerializeField] private Transform _pocketSelectionGrid;
        [SerializeField] private Button _confirmButton;
        [SerializeField] private Button _cancelButton;
        [SerializeField] private Text _selectedBallText;
        [SerializeField] private Text _selectedPocketText;

        [Header("Visual")]
        [SerializeField] private Color _redGroupColor = new Color(0.9f, 0.1f, 0.1f, 1f);
        [SerializeField] private Color _yellowGroupColor = new Color(0.9f, 0.8f, 0.1f, 1f);
        [SerializeField] private Color _blackBallColor = Color.black;
        [SerializeField] private Color _selectedColor = new Color(0.2f, 0.8f, 0.2f, 1f);
        [SerializeField] private Color _unselectedColor = Color.white;

        private int _selectedBall = -1;
        private int _selectedPocket = -1;
        private bool _isOpenTable = true;
        private int _playerGroup = 0; // 0=unset, 1=red, 2=yellow

        private void Awake()
        {
            if (_confirmButton != null)
                _confirmButton.onClick.AddListener(ConfirmCallShot);
            if (_cancelButton != null)
                _cancelButton.onClick.AddListener(CancelCallShot);
        }

        public void ShowCallShot(bool isOpenTable, int playerGroup)
        {
            _isOpenTable = isOpenTable;
            _playerGroup = playerGroup;
            _selectedBall = -1;
            _selectedPocket = -1;

            if (_callShotPanel != null)
                _callShotPanel.SetActive(true);

            UpdateUI();
            BuildBallGrid();
            BuildPocketGrid();
        }

        public void HideCallShot()
        {
            if (_callShotPanel != null)
                _callShotPanel.SetActive(false);
        }

        private void UpdateUI()
        {
            if (_titleText != null)
            {
                _titleText.text = _isOpenTable ? "OPEN TABLE — Call Your Shot" : "CALL YOUR SHOT";
            }
            if (_instructionText != null)
            {
                if (_isOpenTable)
                    _instructionText.text = "Select ANY ball and pocket. Groups will be assigned after break.";
                else if (_playerGroup == 1)
                    _instructionText.text = "You are RED group. Select a RED ball and pocket.";
                else if (_playerGroup == 2)
                    _instructionText.text = "You are YELLOW group. Select a YELLOW ball and pocket.";
            }
            UpdateConfirmButton();
        }

        private void BuildBallGrid()
        {
            if (_ballSelectionGrid == null) return;

            foreach (Transform child in _ballSelectionGrid)
                Destroy(child.gameObject);

            for (int i = 1; i <= 15; i++)
            {
                bool isSelectable = IsBallSelectable(i);
                CreateBallButton(i, isSelectable);
            }
        }

        private void CreateBallButton(int ballNumber, bool isSelectable)
        {
            GameObject btn = new GameObject($"Ball_{ballNumber}");
            btn.transform.SetParent(_ballSelectionGrid, false);

            Button button = btn.AddComponent<Button>();
            Image img = btn.AddComponent<Image>();

            if (ballNumber <= 7) img.color = _redGroupColor;
            else if (ballNumber <= 14) img.color = _yellowGroupColor;
            else img.color = _blackBallColor;

            if (!isSelectable)
            {
                img.color *= 0.3f;
                button.interactable = false;
            }

            GameObject txtObj = new GameObject("Text");
            txtObj.transform.SetParent(btn.transform, false);
            Text txt = txtObj.AddComponent<Text>();
            txt.text = ballNumber.ToString();
            txt.color = (ballNumber == 15) ? Color.white : Color.black;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            if (isSelectable)
            {
                int ball = ballNumber;
                button.onClick.AddListener(() => SelectBall(ball));
            }
        }

        private void BuildPocketGrid()
        {
            if (_pocketSelectionGrid == null) return;

            foreach (Transform child in _pocketSelectionGrid)
                Destroy(child.gameObject);

            string[] pocketNames = { "Top-Left", "Top-Mid", "Top-Right", "Bot-Left", "Bot-Mid", "Bot-Right" };
            for (int i = 0; i < 6; i++)
            {
                GameObject btn = new GameObject($"Pocket_{i}");
                btn.transform.SetParent(_pocketSelectionGrid, false);

                Button button = btn.AddComponent<Button>();
                Image img = btn.AddComponent<Image>();
                img.color = _unselectedColor;

                GameObject txtObj = new GameObject("Text");
                txtObj.transform.SetParent(btn.transform, false);
                Text txt = txtObj.AddComponent<Text>();
                txt.text = pocketNames[i];
                txt.color = Color.black;
                txt.alignment = TextAnchor.MiddleCenter;
                txt.fontSize = 14;
                txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

                int pocket = i;
                button.onClick.AddListener(() => SelectPocket(pocket));
            }
        }

        private bool IsBallSelectable(int ballNumber)
        {
            if (_isOpenTable) return true;
            if (_playerGroup == 1) return ballNumber <= 7 || ballNumber == 15;
            if (_playerGroup == 2) return (ballNumber >= 8 && ballNumber <= 14) || ballNumber == 15;
            return true;
        }

        private void SelectBall(int ballNumber)
        {
            _selectedBall = ballNumber;
            if (_selectedBallText != null)
            {
                string group = ballNumber <= 7 ? "RED" : (ballNumber <= 14 ? "YELLOW" : "BLACK (8-Ball)");
                _selectedBallText.text = $"Ball: {ballNumber} ({group})";
            }
            UpdateConfirmButton();
            HighlightSelection(_ballSelectionGrid, ballNumber - 1);
        }

        private void SelectPocket(int pocketIndex)
        {
            _selectedPocket = pocketIndex;
            string[] names = { "Top-Left", "Top-Mid", "Top-Right", "Bot-Left", "Bot-Mid", "Bot-Right" };
            if (_selectedPocketText != null)
                _selectedPocketText.text = $"Pocket: {names[pocketIndex]}";
            UpdateConfirmButton();
            HighlightSelection(_pocketSelectionGrid, pocketIndex);
        }

        private void HighlightSelection(Transform grid, int selectedIndex)
        {
            for (int i = 0; i < grid.childCount; i++)
            {
                Image img = grid.GetChild(i).GetComponent<Image>();
                if (img != null)
                    img.color = (i == selectedIndex) ? _selectedColor : _unselectedColor;
            }
        }

        private void UpdateConfirmButton()
        {
            if (_confirmButton != null)
                _confirmButton.interactable = (_selectedBall >= 0 && _selectedPocket >= 0);
        }

        private void ConfirmCallShot()
        {
            if (_selectedBall < 0 || _selectedPocket < 0) return;
            Debug.Log($"[CallShotUI] Called: Ball {_selectedBall} → Pocket {_selectedPocket}");
            OnShotCalled?.Invoke(_selectedBall, _selectedPocket);
            HideCallShot();
        }

        private void CancelCallShot()
        {
            OnCallShotCancelled?.Invoke();
            HideCallShot();
        }

        #region Self-Test
        public bool RunSelfTest()
        {
            bool pass = true;
            if (_callShotPanel == null)
            {
                Debug.LogError("[Self-Test] CallShotUI: Panel not assigned.");
                pass = false;
            }
            if (_ballSelectionGrid == null)
            {
                Debug.LogError("[Self-Test] CallShotUI: Ball grid not assigned.");
                pass = false;
            }
            if (_pocketSelectionGrid == null)
            {
                Debug.LogError("[Self-Test] CallShotUI: Pocket grid not assigned.");
                pass = false;
            }
            Debug.Log($"[Self-Test] ChinesePoolCallShotUI: {(pass ? "PASS" : "FAIL")}");
            return pass;
        }
        #endregion
    }
}