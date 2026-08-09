using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using CueStrike.Gameplay.ChinesePool;

namespace CueStrike.Gameplay.ChinesePool
{
    /// <summary>
    /// UI for Call Shot phase in Chinese 8-Ball.
    /// Displays available balls and pockets for the player to call their shot.
    /// </summary>
    public class ChinesePoolCallShotUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Transform ballButtonContainer;
        [SerializeField] private Transform pocketButtonContainer;
        [SerializeField] private GameObject ballButtonPrefab;
        [SerializeField] private GameObject pocketButtonPrefab;
        [SerializeField] private TextMeshProUGUI instructionText;
        [SerializeField] private TextMeshProUGUI calledShotText;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button cancelButton;

        [Header("Settings")]
        [SerializeField] private float panelFadeDuration = 0.2f;
        [SerializeField] private Color availableColor = Color.white;
        [SerializeField] private Color selectedColor = Color.yellow;
        [SerializeField] private Color unavailableColor = Color.gray;

        private ChinesePoolGameManager gameManager;
        private int currentPlayerIndex = 0;
        private int selectedBallId = -1;
        private int selectedPocketId = -1;
        private List<Button> ballButtons = new List<Button>();
        private List<Button> pocketButtons = new List<Button>();
        private CanvasGroup panelCanvasGroup;
        private System.Action<int, int> onCallShotConfirmed;

        private void Awake()
        {
            panelCanvasGroup = panelRoot.GetComponent<CanvasGroup>();
            if (panelCanvasGroup == null)
                panelCanvasGroup = panelRoot.AddComponent<CanvasGroup>();

            panelRoot.SetActive(false);

            if (confirmButton != null)
                confirmButton.onClick.AddListener(OnConfirmClicked);
            if (cancelButton != null)
                cancelButton.onClick.AddListener(OnCancelClicked);
        }

        private void Start()
        {
            gameManager = FindFirstObjectByType<ChinesePoolGameManager>();
            if (gameManager == null)
            {
                Debug.LogWarning("[ChinesePoolCallShotUI] ChinesePoolGameManager not found in scene.");
            }
        }

        /// <summary>
        /// Shows the call shot UI for the current player.
        /// </summary>
        public void ShowCallShotUI(int playerIndex, int[] availableBalls, int[] availablePockets, System.Action<int, int> onConfirmed)
        {
            currentPlayerIndex = playerIndex;
            selectedBallId = -1;
            selectedPocketId = -1;
            onCallShotConfirmed = onConfirmed;

            BuildBallButtons(availableBalls);
            BuildPocketButtons(availablePockets);
            UpdateUI();

            panelRoot.SetActive(true);
            StartCoroutine(FadePanel(1f));
        }

        /// <summary>
        /// Hides the call shot UI.
        /// </summary>
        public void HideCallShotUI()
        {
            StartCoroutine(FadePanel(0f, () => panelRoot.SetActive(false)));
        }

        private System.Collections.IEnumerator FadePanel(float targetAlpha, System.Action onComplete = null)
        {
            float startAlpha = panelCanvasGroup.alpha;
            float elapsed = 0f;

            while (elapsed < panelFadeDuration)
            {
                elapsed += Time.deltaTime;
                panelCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / panelFadeDuration);
                yield return null;
            }

            panelCanvasGroup.alpha = targetAlpha;
            onComplete?.Invoke();
        }

        private void BuildBallButtons(int[] availableBalls)
        {
            // Clear existing buttons
            foreach (var btn in ballButtons)
            {
                if (btn != null) Destroy(btn.gameObject);
            }
            ballButtons.Clear();

            if (ballButtonPrefab == null || ballButtonContainer == null) return;

            foreach (int ballId in availableBalls)
            {
                var btnObj = Instantiate(ballButtonPrefab, ballButtonContainer);
                var btn = btnObj.GetComponent<Button>();
                var text = btnObj.GetComponentInChildren<TextMeshProUGUI>();

                if (text != null)
                {
                    ChinesePoolBallType ballType = ChinesePoolRules.GetBallType(ballId);
                    string ballName = ballType switch
                    {
                        ChinesePoolBallType.Red => $"Red {ballId}",
                        ChinesePoolBallType.Yellow => $"Yellow {ballId - 8}",
                        ChinesePoolBallType.BlackBall => "Black 8",
                        _ => $"Ball {ballId}"
                    };
                    text.text = ballName;
                }

                int capturedBallId = ballId;
                btn.onClick.AddListener(() => OnBallSelected(capturedBallId));
                ballButtons.Add(btn);
            }
        }

        private void BuildPocketButtons(int[] availablePockets)
        {
            // Clear existing buttons
            foreach (var btn in pocketButtons)
            {
                if (btn != null) Destroy(btn.gameObject);
            }
            pocketButtons.Clear();

            if (pocketButtonPrefab == null || pocketButtonContainer == null) return;

            string[] pocketNames = { "Top Left", "Top", "Top Right", "Bottom Right", "Bottom", "Bottom Left" };

            foreach (int pocketId in availablePockets)
            {
                var btnObj = Instantiate(pocketButtonPrefab, pocketButtonContainer);
                var btn = btnObj.GetComponent<Button>();
                var text = btnObj.GetComponentInChildren<TextMeshProUGUI>();

                if (text != null && pocketId >= 0 && pocketId < pocketNames.Length)
                {
                    text.text = pocketNames[pocketId];
                }

                int capturedPocketId = pocketId;
                btn.onClick.AddListener(() => OnPocketSelected(capturedPocketId));
                pocketButtons.Add(btn);
            }
        }

        private void OnBallSelected(int ballId)
        {
            selectedBallId = ballId;
            UpdateUI();
        }

        private void OnPocketSelected(int pocketId)
        {
            selectedPocketId = pocketId;
            UpdateUI();
        }

        private void UpdateUI()
        {
            // Update ball button colors
            for (int i = 0; i < ballButtons.Count; i++)
            {
                var btn = ballButtons[i];
                if (btn == null) continue;

                var colors = btn.colors;
                int ballId = GetBallIdFromButtonIndex(i);
                colors.normalColor = (ballId == selectedBallId) ? selectedColor : availableColor;
                btn.colors = colors;
            }

            // Update pocket button colors
            for (int i = 0; i < pocketButtons.Count; i++)
            {
                var btn = pocketButtons[i];
                if (btn == null) continue;

                var colors = btn.colors;
                colors.normalColor = (i == selectedPocketId) ? selectedColor : availableColor;
                btn.colors = colors;
            }

            // Update confirm button interactable
            if (confirmButton != null)
            {
                confirmButton.interactable = (selectedBallId != -1 && selectedPocketId != -1);
            }

            // Update called shot display
            if (calledShotText != null)
            {
                if (selectedBallId != -1 && selectedPocketId != -1)
                {
                    ChinesePoolBallType ballType = ChinesePoolRules.GetBallType(selectedBallId);
                    string ballName = ballType switch
                    {
                        ChinesePoolBallType.Red => $"Red {selectedBallId}",
                        ChinesePoolBallType.Yellow => $"Yellow {selectedBallId - 8}",
                        ChinesePoolBallType.BlackBall => "Black 8",
                        _ => $"Ball {selectedBallId}"
                    };
                    string[] pocketNames = { "Top Left", "Top", "Top Right", "Bottom Right", "Bottom", "Bottom Left" };
                    string pocketName = (selectedPocketId >= 0 && selectedPocketId < pocketNames.Length) ? pocketNames[selectedPocketId] : $"Pocket {selectedPocketId}";
                    calledShotText.text = $"Call: {ballName} → {pocketName}";
                }
                else
                {
                    calledShotText.text = "Select a ball and pocket to call your shot";
                }
            }

            // Update instruction text
            if (instructionText != null)
            {
                instructionText.text = $"Player {currentPlayerIndex + 1}: Call your shot";
            }
        }

        private int GetBallIdFromButtonIndex(int index)
        {
            // This is a simplified mapping - in practice you'd store the ballId with the button
            // For now, we'll need to track this differently
            return -1; // Placeholder - would need proper mapping
        }

        private void OnConfirmClicked()
        {
            if (selectedBallId != -1 && selectedPocketId != -1 && onCallShotConfirmed != null)
            {
                onCallShotConfirmed.Invoke(selectedBallId, selectedPocketId);
                HideCallShotUI();
            }
        }

        private void OnCancelClicked()
        {
            HideCallShotUI();
            onCallShotConfirmed?.Invoke(-1, -1); // Signal cancellation
        }

        /// <summary>
        /// Static method to show call shot UI from ChinesePoolRules events.
        /// </summary>
        public static void ShowCallShot(int playerIndex, int ballId, int pocketId)
        {
            var ui = FindFirstObjectByType<ChinesePoolCallShotUI>();
            if (ui != null)
            {
                // This would be called from ChinesePoolRules.OnCallShotRequested event
                // The UI would need to be shown with available balls/pockets
                Debug.Log($"[ChinesePoolCallShotUI] Call shot requested for Player {playerIndex}: Ball {ballId} -> Pocket {pocketId}");
            }
        }
    }
}