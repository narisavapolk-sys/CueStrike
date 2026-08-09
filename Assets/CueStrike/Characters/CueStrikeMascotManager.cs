using UnityEngine;
using UnityEngine.Events;
using CueStrike.MascotSystem;

namespace CueStrike.Characters
{
    /// <summary>
    /// Central coordinator for all mascot and crowd systems.
    /// Manages Uncle Nok (AI Referee), Bo (Hype Panda), and Crowd System with Stalker Mode.
    /// Compatible with MascotGameplayIntegration expectations.
    /// </summary>
    public class CueStrikeMascotManager : MonoBehaviour
    {
        public static CueStrikeMascotManager Instance { get; private set; }
        
        [Header("Mascot References")]
        [Tooltip("Uncle Nok - Elephant AI Referee")]
        public CueStrikeMascotUncleNok uncleNok;
        
        [Tooltip("Bo - Panda Hype Mascot")]
        public BoPandaBanter boPanda;
        
        [Tooltip("Crowd System with Stalker Mode")]
        public CueStrikeCrowdSystem crowdSystem;
        
        [Header("Auto-Find Settings")]
        [Tooltip("Automatically find mascot components in scene on start")]
        public bool autoFindMascots = true;
        
        [Header("Global Settings")]
        [Tooltip("Master enable for all mascot systems")]
        public bool enableAllMascots = true;
        
        [Tooltip("Enable Uncle Nok commentary")]
        public bool enableUncleNok = true;
        
        [Tooltip("Enable Bo Panda reactions")]
        public bool enableBoPanda = true;
        
        [Tooltip("Enable Crowd System")]
        public bool enableCrowd = true;
        
        [Tooltip("Enable Stalker Mode")]
        public bool enableStalkerMode = true;
        
        [Header("Events")]
        [Tooltip("Event fired when any mascot delivers commentary/reaction")]
        public UnityEvent<string, string> OnMascotSpoke; // mascotName, message
        
        [Tooltip("Event fired when crowd reacts")]
        public UnityEvent<CueStrikeCrowdSystem.CrowdReactionType, int> OnCrowdReacted;
        
        // Internal state
        private bool _isInitialized = false;
        
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            if (autoFindMascots)
            {
                FindMascotComponents();
            }
        }
        
        private void Start()
        {
            InitializeSystems();
            SubscribeToEvents();
            _isInitialized = true;
            
            ApplyGlobalSettings();
            Debug.Log("[MascotManager] All mascot systems initialized and coordinated!");
        }
        
        private void FindMascotComponents()
        {
            if (uncleNok == null)
                uncleNok = FindFirstObjectByType<CueStrikeMascotUncleNok>();
            
            if (boPanda == null)
                boPanda = FindFirstObjectByType<BoPandaBanter>();
            
            if (crowdSystem == null)
                crowdSystem = FindFirstObjectByType<CueStrikeCrowdSystem>();
        }
        
        private void InitializeSystems()
        {
            // Ensure all systems are enabled/disabled based on global settings
            if (uncleNok != null)
                uncleNok.enabled = enableAllMascots && enableUncleNok;
            
            if (boPanda != null)
                boPanda.enabled = enableAllMascots && enableBoPanda;
            
            if (crowdSystem != null)
            {
                crowdSystem.enabled = enableAllMascots && enableCrowd;
                if (enableAllMascots && enableCrowd && enableStalkerMode)
                {
                    crowdSystem.SetStalkerMode(true);
                }
            }
        }
        
        private void SubscribeToEvents()
        {
            if (uncleNok != null)
            {
                uncleNok.OnCommentaryDelivered.AddListener(OnUncleNokCommentary);
            }
            
            if (boPanda != null)
            {
                boPanda.OnReactionDelivered.AddListener(OnBoPandaReaction);
            }
            
            if (crowdSystem != null)
            {
                crowdSystem.OnCrowdReacted.AddListener(OnCrowdReaction);
            }
        }
        
        private void UnsubscribeFromEvents()
        {
            if (uncleNok != null)
            {
                uncleNok.OnCommentaryDelivered.RemoveListener(OnUncleNokCommentary);
            }
            
            if (boPanda != null)
            {
                boPanda.OnReactionDelivered.RemoveListener(OnBoPandaReaction);
            }
            
            if (crowdSystem != null)
            {
                crowdSystem.OnCrowdReacted.RemoveListener(OnCrowdReaction);
            }
        }
        
        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }
        
        private void OnUncleNokCommentary(string commentary)
        {
            OnMascotSpoke?.Invoke("Uncle Nok", commentary);
        }
        
        private void OnBoPandaReaction(string reaction)
        {
            OnMascotSpoke?.Invoke("Bo", reaction);
        }
        
        private void OnCrowdReaction(CueStrikeCrowdSystem.CrowdReactionType type, int intensity)
        {
            OnCrowdReacted?.Invoke(type, intensity);
        }
        
        private void ApplyGlobalSettings()
        {
            SetAllMascotsEnabled(enableAllMascots);
            SetUncleNokEnabled(enableUncleNok);
            SetBoPandaEnabled(enableBoPanda);
            SetCrowdEnabled(enableCrowd);
            SetStalkerModeEnabled(enableStalkerMode);
        }
        
        /// <summary>
        /// Master toggle for all mascot systems
        /// </summary>
        public void SetAllMascotsEnabled(bool enabled)
        {
            enableAllMascots = enabled;
            
            if (uncleNok != null) uncleNok.enabled = enabled && enableUncleNok;
            if (boPanda != null) boPanda.enabled = enabled && enableBoPanda;
            if (crowdSystem != null) crowdSystem.enabled = enabled && enableCrowd;
            
            Debug.Log($"[MascotManager] All mascot systems {(enabled ? "ENABLED" : "DISABLED")}");
        }
        
        /// <summary>
        /// Toggle Uncle Nok (AI Referee)
        /// </summary>
        public void SetUncleNokEnabled(bool enabled)
        {
            enableUncleNok = enabled;
            if (uncleNok != null)
                uncleNok.enabled = enableAllMascots && enabled;
        }
        
        /// <summary>
        /// Toggle Bo Panda (Hype Mascot)
        /// </summary>
        public void SetBoPandaEnabled(bool enabled)
        {
            enableBoPanda = enabled;
            if (boPanda != null)
                boPanda.enabled = enableAllMascots && enabled;
        }
        
        /// <summary>
        /// Toggle Crowd System
        /// </summary>
        public void SetCrowdEnabled(bool enabled)
        {
            enableCrowd = enabled;
            if (crowdSystem != null)
                crowdSystem.enabled = enableAllMascots && enabled;
        }
        
        /// <summary>
        /// Toggle Stalker Mode
        /// </summary>
        public void SetStalkerModeEnabled(bool enabled)
        {
            enableStalkerMode = enabled;
            if (crowdSystem != null && crowdSystem.enabled)
                crowdSystem.SetStalkerMode(enabled);
        }
        
        /// <summary>
        /// Trigger a coordinated celebration across all systems
        /// </summary>
        public void TriggerGrandCelebration(string reason = "Victory!")
        {
            if (!enableAllMascots) return;
            
            if (uncleNok != null && enableUncleNok)
                uncleNok.TriggerCommentary(CommentaryType.Victory);
            
            if (boPanda != null && enableBoPanda)
                boPanda.TriggerReaction(BoPandaBanter.ReactionType.Victory);
            
            if (crowdSystem != null && enableCrowd)
                crowdSystem.TriggerManualReaction(CueStrikeCrowdSystem.CrowdReactionType.StandingOvation, 100);
            
            Debug.Log($"[MascotManager] GRAND CELEBRATION TRIGGERED: {reason}");
        }
        
        /// <summary>
        /// Trigger coordinated reaction for a great shot
        /// </summary>
        public void TriggerGreatShotReaction(int ballsPotted, int breakScore)
        {
            if (!enableAllMascots) return;
            
            // Uncle Nok gives technical commentary
            if (uncleNok != null && enableUncleNok)
            {
                if (breakScore >= 50)
                    uncleNok.TriggerCommentary(CommentaryType.Positive);
                else
                    uncleNok.TriggerCommentary(CommentaryType.Neutral);
            }
            
            // Bo gets hyped
            if (boPanda != null && enableBoPanda)
            {
                if (ballsPotted >= 3 || breakScore >= 50)
                    boPanda.TriggerReaction(BoPandaBanter.ReactionType.Clutch);
                else
                    boPanda.TriggerReaction(BoPandaBanter.ReactionType.SinglePot);
            }
            
            // Crowd reacts
            if (crowdSystem != null && enableCrowd)
            {
                if (breakScore >= 100)
                    crowdSystem.TriggerManualReaction(CueStrikeCrowdSystem.CrowdReactionType.StandingOvation, breakScore);
                else if (breakScore >= 50)
                    crowdSystem.TriggerManualReaction(CueStrikeCrowdSystem.CrowdReactionType.Gasp, breakScore);
                else if (ballsPotted >= 2)
                    crowdSystem.TriggerManualReaction(CueStrikeCrowdSystem.CrowdReactionType.EnthusiasticCheer, ballsPotted);
                else
                    crowdSystem.TriggerManualReaction(CueStrikeCrowdSystem.CrowdReactionType.PoliteApplause, 1);
            }
        }
        
        /// <summary>
        /// Trigger coordinated reaction for a missed shot
        /// </summary>
        public void TriggerMissReaction()
        {
            if (!enableAllMascots) return;
            
            if (uncleNok != null && enableUncleNok)
                uncleNok.TriggerCommentary(CommentaryType.Miss);
            
            if (boPanda != null && enableBoPanda)
                boPanda.TriggerReaction(BoPandaBanter.ReactionType.Miss);
            
            if (crowdSystem != null && enableCrowd)
                crowdSystem.TriggerManualReaction(CueStrikeCrowdSystem.CrowdReactionType.Silence, 0);
        }
        
        /// <summary>
        /// Trigger coordinated reaction for a foul
        /// </summary>
        public void TriggerFoulReaction(string foulType)
        {
            if (!enableAllMascots) return;
            
            if (uncleNok != null && enableUncleNok)
                uncleNok.TriggerCommentary(CommentaryType.Foul);
            
            if (boPanda != null && enableBoPanda)
                boPanda.TriggerReaction(BoPandaBanter.ReactionType.Foul);
            
            if (crowdSystem != null && enableCrowd)
                crowdSystem.TriggerManualReaction(CueStrikeCrowdSystem.CrowdReactionType.Silence, 0);
        }
        
        /// <summary>
        /// Reset all mascot systems for new frame/game
        /// </summary>
        public void ResetAllSystems()
        {
            if (uncleNok != null)
                uncleNok.ResetScores();
            
            if (boPanda != null)
                boPanda.ResetCounters();
            
            if (crowdSystem != null)
                crowdSystem.ResetCrowd();
            
            Debug.Log("[MascotManager] All mascot systems reset for new frame/game");
        }
        
        /// <summary>
        /// Get Uncle Nok component
        /// </summary>
        public CueStrikeMascotUncleNok GetUncleNok() => uncleNok;
        
        /// <summary>
        /// Get Bo Panda component
        /// </summary>
        public BoPandaBanter GetBoPanda() => boPanda;
        
        /// <summary>
        /// Get Crowd System component
        /// </summary>
        public CueStrikeCrowdSystem GetCrowdSystem() => crowdSystem;
        
        /// <summary>
        /// Check if all systems are active
        /// </summary>
        public bool AreAllSystemsActive()
        {
            bool uncleActive = uncleNok != null && uncleNok.enabled;
            bool boActive = boPanda != null && boPanda.enabled;
            bool crowdActive = crowdSystem != null && crowdSystem.enabled;
            
            return uncleActive && boActive && crowdActive;
        }
        
        // ============================================================
        // Types and Methods for MascotGameplayIntegration Compatibility
        // ============================================================
        
        /// <summary>
        /// Shot result enumeration for mascot reactions.
        /// </summary>
        public enum ShotResult
        {
            Potted = 0,
            Missed = 1,
            Foul = 2,
            Safety = 3,
            Break = 4,
            Win = 5
        }
        
        /// <summary>
        /// Frame event types for announcements.
        /// </summary>
        public enum FrameEvent
        {
            FrameStart = 0,
            FrameEnd = 1,
            MatchStart = 2,
            MatchEnd = 3,
            PlayerTurnStart = 4,
            PlayerTurnEnd = 5
        }
        
        /// <summary>
        /// Break event types.
        /// </summary>
        public enum BreakEvent
        {
            BreakShot = 0,
            BreakAndRun = 1,
            DryBreak = 2,
            FoulOnBreak = 3
        }
        
        /// <summary>
        /// Milestone types for achievements.
        /// </summary>
        public enum MilestoneType
        {
            CenturyBreak = 0,
            HighBreak = 1,
            MaximumBreak = 2,
            Clearance = 3,
            SnookerEscape = 4,
            Fluke = 5
        }
        
        /// <summary>
        /// Foul type enumeration for referee announcements.
        /// </summary>
        public enum FoulType
        {
            None = 0,
            CueBallPotted = 1,
            NoBallContacted = 2,
            WrongBallFirst = 3,
            NoCushionAfterContact = 4,
            EightBallEarly = 5,
            EightBallWrongPocket = 6,
            BallOffTable = 7,
            DoubleHit = 8,
            PushShot = 9,
            Miscue = 10,
            TimeViolation = 11,
            TouchingBall = 12,
            PlayingOutOfTurn = 13,
            UnintentionalContact = 14,
            IllegalSnooker = 15
        }
        
        // Methods expected by MascotGameplayIntegration
        
        /// <summary>
        /// Trigger shot reaction for mascots
        /// </summary>
        public void TriggerShotReaction(ShotResult result, float shotQuality)
        {
            if (!enableAllMascots) return;
            
            // Route to Uncle Nok
            if (uncleNok != null && enableUncleNok)
            {
                uncleNok.OnShotResult(result, shotQuality);
            }
            
            // Route to Bo Panda
            if (boPanda != null && enableBoPanda)
            {
                boPanda.OnShotResult(result, shotQuality);
            }
            
            // Route to Crowd System
            if (crowdSystem != null && enableCrowd)
            {
                crowdSystem.OnShotResult(result, shotQuality);
            }
        }
        
        /// <summary>
        /// Trigger frame event for mascots
        /// </summary>
        public void TriggerFrameEvent(FrameEvent frameEvent, int playerIndex)
        {
            if (!enableAllMascots) return;
            
            if (uncleNok != null && enableUncleNok)
                uncleNok.OnFrameEvent(frameEvent, playerIndex);
            
            if (boPanda != null && enableBoPanda)
                boPanda.OnFrameEvent(frameEvent, playerIndex);
            
            if (crowdSystem != null && enableCrowd)
                crowdSystem.OnFrameEvent(frameEvent, playerIndex);
        }
        
        /// <summary>
        /// Trigger milestone event for mascots
        /// </summary>
        public void TriggerMilestoneEvent(MilestoneType milestone, int playerIndex, int value)
        {
            if (!enableAllMascots) return;
            
            if (uncleNok != null && enableUncleNok)
                uncleNok.OnMilestone(milestone, playerIndex, value);
            
            if (boPanda != null && enableBoPanda)
                boPanda.OnMilestone(milestone, playerIndex, value);
            
            if (crowdSystem != null && enableCrowd)
                crowdSystem.OnMilestone(milestone, playerIndex, value);
        }
        
        /// <summary>
        /// Trigger foul reaction for mascots
        /// </summary>
        public void TriggerFoulReaction(FoulType foulType, string description)
        {
            if (!enableAllMascots) return;
            
            if (uncleNok != null && enableUncleNok)
                uncleNok.OnFoul(foulType, description);
            
            if (boPanda != null && enableBoPanda)
                boPanda.OnFoul(foulType, description);
            
            if (crowdSystem != null && enableCrowd)
                crowdSystem.OnFoul(foulType, description);
        }
        
        /// <summary>
        /// Trigger break event for mascots
        /// </summary>
        public void TriggerBreakEvent(BreakEvent breakEvent, int playerIndex)
        {
            if (!enableAllMascots) return;
            
            if (uncleNok != null && enableUncleNok)
                uncleNok.OnBreakEvent(breakEvent, playerIndex);
            
            if (boPanda != null && enableBoPanda)
                boPanda.OnBreakEvent(breakEvent, playerIndex);
            
            if (crowdSystem != null && enableCrowd)
                crowdSystem.OnBreakEvent(breakEvent, playerIndex);
        }
    }
}