using UnityEngine;
using CueStrike.Gameplay.SaveSystem;
using UnityEngine.UI;
using System.Collections.Generic;

namespace CueStrike.UI
{
    /// <summary>
    /// UI for building custom practice drills.
    /// STUB — implement full drill builder when ready.
    /// </summary>
    public class CustomDrillBuilderUI : MonoBehaviour
    {
        [Header("UI References")]
        public GameObject ballButtonPrefab;
        public Transform ballListParent;
        public Button saveButton;
        public Button loadButton;
        public Button clearButton;
        public InputField drillNameInput;

        [Header("Settings")]
        public int maxBalls = 21;

        // Use fully qualified names to avoid ambiguity with CueStrike.Gameplay.Practice
        private CueStrike.Gameplay.SaveSystem.DrillSettingsData _currentSettings = new CueStrike.Gameplay.SaveSystem.DrillSettingsData();
        private List<CueStrike.Gameplay.SaveSystem.BallPositionData> _ballPositions = new List<CueStrike.Gameplay.SaveSystem.BallPositionData>();
        
        // Reference to the save system integration (to be implemented)
        // private CueStrikeSaveSystemIntegration _saveSystem;

        void Start()
        {
            SetupButtons();
            Debug.Log("[CueStrike] CustomDrillBuilderUI initialized — STUB");
        }

        private void SetupButtons()
        {
            if (saveButton != null) saveButton.onClick.AddListener(SaveDrill);
            if (loadButton != null) loadButton.onClick.AddListener(LoadDrill);
            if (clearButton != null) clearButton.onClick.AddListener(ClearAll);
        }

        /// <summary>
        /// Saves current drill configuration.
        /// </summary>
        public void SaveDrill()
        {
            string drillName = drillNameInput != null ? drillNameInput.text : "Untitled";
            Debug.Log($"[CueStrike] Saving drill: {drillName} — STUB");

            var drill = new CustomDrillData
            {
                drillId = System.Guid.NewGuid().ToString(),
                drillName = drillName,
                ballPositions = _ballPositions.ConvertAll(bp => new CueStrike.Gameplay.SaveSystem.BallPositionData
                {
                    ballId = bp.ballId,
                    ballName = bp.ballName,
                    position = bp.position,
                    velocity = bp.velocity,
                    isActive = bp.isActive,
                    isPocketed = bp.isPocketed,
                    pocketIndex = bp.pocketIndex
                }),
                settings = new CueStrike.Gameplay.SaveSystem.DrillSettingsData
                {
                    timeLimitSeconds = _currentSettings.timeLimitSeconds,
                    targetScore = _currentSettings.targetScore,
                    maxFoulsAllowed = _currentSettings.maxFoulsAllowed,
                    requireCallShot = _currentSettings.requireCallShot,
                    allowBallInHand = _currentSettings.allowBallInHand,
                    difficultyLevel = _currentSettings.difficultyLevel,
                    tags = _currentSettings.tags,
                    isTimed = _currentSettings.isTimed,
                    maxShots = _currentSettings.maxShots,
                    requireAllBallsPotted = _currentSettings.requireAllBallsPotted
                }
            };

            CueStrikeSaveSystemIntegration.SaveCustomDrill(drill);
        }

        /// <summary>
        /// Loads a saved drill.
        /// </summary>
        public void LoadDrill()
        {
            Debug.Log("[CueStrike] Loading drill — STUB");
        }

        /// <summary>
        /// Clears all ball positions.
        /// </summary>
        public void ClearAll()
        {
            _ballPositions.Clear();
            Debug.Log("[CueStrike] Cleared all ball positions");
        }
    }
}