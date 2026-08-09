using UnityEngine;
using CueStrike.Gameplay.SaveSystem;

namespace CueStrike.Gameplay.Practice
{
    [CreateAssetMenu(fileName = "DrillSettings", menuName = "CueStrike/Gameplay/Drill Settings")]
    public class DrillSettingsData : ScriptableObject
    {
        // Identity
        public string drillId;
        public string displayName;
        public string description;
        public Sprite drillIcon;
        public DrillCategory category;

        // Difficulty & Progression
        public DifficultyLevel difficulty;
        public int unlockRequirement;
        public bool isTutorial;

        // Table Setup
        public TableType tableType;

        // Objectives
        public DrillObjective objectiveType;
        public int targetBallCount;
        public int[] targetBallIds;
        public PocketType targetPocket;

        // Constraints
        public float timeLimit;
        public int maxShots;
        public bool requireCueBallPosition;
        public bool allowBallInHand;

        // Scoring
        public int baseScore;
        public float timeBonusMultiplier;
        public float accuracyBonus;

        // Validation Rules
        public bool validatePocketOrder;
        public bool validateCueBallStop;
        public float maxCueBallSpeed;
        public bool allowFouls;

        // Additional required fields
        public int targetScore;
        public int maxFoulsAllowed;
        public bool requireCallShot;
        public int difficultyLevel;
        public System.Collections.Generic.List<string> tags = new System.Collections.Generic.List<string>();
    }

    // Enums – if not already defined elsewhere, define them here
    public enum DrillCategory { BreakAndRun, PositionPlay, Safety, KickShot, BankShot, Combo, Carom, Pattern }
    public enum DifficultyLevel { Beginner, Intermediate, Advanced, Pro, Master }
    public enum DrillObjective { PocketAll, PocketSpecific, PositionCueBall, AvoidFoul, ClearTable, RunOut }
    public enum TableType { Standard9Foot, Standard8Foot, BarBox }
    public enum PocketType { Any, CornerOnly, SideOnly, Specific }
}