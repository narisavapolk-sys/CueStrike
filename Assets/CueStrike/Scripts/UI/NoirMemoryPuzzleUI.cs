using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CueStrike.NoirMemory;

namespace CueStrike.UI
{
    public class NoirMemoryPuzzleUI : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] private GameObject memoryModePanel;
        [SerializeField] private GameObject timerDisplay;

        [Header("Timer UI")]
        [SerializeField] private TextMeshProUGUI timerText;
        [SerializeField] private Image timerFillImage;
        [SerializeField] private Color revealColor = new Color(0.2f, 0.8f, 0.2f); // Green = memorize
        [SerializeField] private Color noirColor = new Color(0.8f, 0.1f, 0.1f);   // Red = noir

        [Header("Settings")]
        [SerializeField] private Button fiveSecButton;
        [SerializeField] private Button eightSecButton;
        [SerializeField] private Button startButton;
        [SerializeField] private Button stopButton;

        [Header("Indicators")]
        [SerializeField] private TextMeshProUGUI phaseText; // "REVEAL" / "NOIR"
        [SerializeField] private GameObject noirIndicator; // Blinking light during noir

        private NoirMemoryPuzzleManager manager;
        private float maxDuration = 5f;

        private void Start()
        {
            manager = NoirMemoryPuzzleManager.Instance;
            if (manager != null)
            {
                manager.OnRevealPhaseStarted += OnRevealStarted;
                manager.OnNoirPhaseStarted += OnNoirStarted;
                manager.OnTimerUpdated += OnTimerUpdated;
                manager.OnBallRevealed += OnBallRevealed;
            }

            SetupButtons();
        }

        private void OnDestroy()
        {
            if (manager != null)
            {
                manager.OnRevealPhaseStarted -= OnRevealStarted;
                manager.OnNoirPhaseStarted -= OnNoirStarted;
                manager.OnTimerUpdated -= OnTimerUpdated;
                manager.OnBallRevealed -= OnBallRevealed;
            }
        }

        private void SetupButtons()
        {
            if (fiveSecButton != null)
                fiveSecButton.onClick.AddListener(() => SelectDuration(5f));
            if (eightSecButton != null)
                eightSecButton.onClick.AddListener(() => SelectDuration(8f));
            if (startButton != null)
                startButton.onClick.AddListener(() => manager?.StartMemoryMode());
            if (stopButton != null)
                stopButton.onClick.AddListener(() => manager?.StopMemoryMode());
        }

        private void SelectDuration(float seconds)
        {
            maxDuration = seconds;
            // Visual feedback for selection
        }

        private void OnRevealStarted()
        {
            phaseText.text = "REVEAL — MEMORIZE!";
            phaseText.color = revealColor;
            timerFillImage.color = revealColor;
            noirIndicator.SetActive(false);
        }

        private void OnNoirStarted()
        {
            phaseText.text = "NOIR — SHOOT!";
            phaseText.color = noirColor;
            timerFillImage.color = noirColor;
            noirIndicator.SetActive(true);
        }

        private void OnTimerUpdated(float remaining)
        {
            timerText.text = remaining.ToString("F1");
            if (maxDuration > 0)
                timerFillImage.fillAmount = remaining / maxDuration;
        }

        private void OnBallRevealed(int ballId)
        {
            // Optional: show small effect when ball is revealed
            Debug.Log($"[NoirUI] Ball {ballId} revealed!");
        }
    }
}