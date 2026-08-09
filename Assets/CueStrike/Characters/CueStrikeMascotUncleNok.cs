using UnityEngine;
using UnityEngine.Events;
using CueStrike.Gameplay;

namespace CueStrike.Characters
{
    /// <summary>
    /// Uncle Nok - Elephant AI Referee Mascot.
    /// Calculates and announces scores, provides commentary on shots.
    /// </summary>
    public class CueStrikeMascotUncleNok : MonoBehaviour
    {
        [Header("Mascot Identity")]
        [Tooltip("Mascot display name")]
        public string mascotName = "Uncle Nok";
        
        [Header("Visual Components")]
        [Tooltip("Animator for elephant animations")]
        public Animator mascotAnimator;
        
        [Tooltip("Transform for mascot position near table")]
        public Transform homePosition;
        
        [Header("Commentary Settings")]
        [Tooltip("Enable voice commentary")]
        public bool enableCommentary = true;
        
        [Tooltip("Commentary cooldown between announcements (seconds)")]
        public float commentaryCooldown = 3.0f;
        
        [Tooltip("Array of positive commentary lines for good shots")]
        [TextArea(2, 5)]
        public string[] positiveCommentary = new string[]
        {
            "Excellent shot! A masterful display of cue control.",
            "Magnificent! The balls dance to your command.",
            "Superb positioning. You play with true elegance.",
            "A textbook shot. Uncle Nok approves!",
            "Brilliant! That's championship caliber play."
        };
        
        [Tooltip("Array of neutral commentary lines for standard shots")]
        [TextArea(2, 5)]
        public string[] neutralCommentary = new string[]
        {
            "Good shot. Steady progress.",
            "Well played. Keep your focus.",
            "Solid execution. The frame continues.",
            "Respectable. A professional's choice.",
            "Noted. The table respects your skill."
        };
        
        [Tooltip("Array of commentary lines for missed shots")]
        [TextArea(2, 5)]
        public string[] missCommentary = new string[]
        {
            "Unfortunate. The table giveth, and the table taketh away.",
            "A learning moment. Even masters miss.",
            "The pockets remain elusive this time.",
            "Composure, player. The frame is long.",
            "A rare miss. Uncle Nok expects better."
        };
        
        [Tooltip("Array of commentary lines for fouls")]
        [TextArea(2, 5)]
        public string[] foulCommentary = new string[]
        {
            "Foul called. Discipline is the mark of a champion.",
            "Rules are absolute. Learn from this.",
            "A foul. The advantage shifts to your opponent.",
            "Uncle Nok sees all. Foul confirmed.",
            "Penalty assessed. Maintain your honor."
        };
        
        [Tooltip("Array of commentary lines for frame/game victory")]
        [TextArea(2, 5)]
        public string[] victoryCommentary = new string[]
        {
            "Victory! A triumph worthy of the grand hall!",
            "Frame won! You have conquered the cloth!",
            "Magnificent victory! The elephant trumpets for you!",
            "Champion! Your name echoes in these halls!",
            "A masterful performance! Uncle Nok bows!"
        };
        
        [Header("Score Tracking")]
        [Tooltip("Current frame score for player 1")]
        public int player1Score = 0;
        
        [Tooltip("Current frame score for player 2")]
        public int player2Score = 0;
        
        [Tooltip("Current break score")]
        public int currentBreak = 0;
        
        [Tooltip("Highest break this session")]
        public int highestBreak = 0;
        
        [Header("Events")]
        [Tooltip("Event fired when Uncle Nok delivers commentary")]
        public UnityEvent<string> OnCommentaryDelivered;
        
        [Tooltip("Event fired when score is updated")]
        public UnityEvent<int, int> OnScoreUpdated; // player1, player2
        
        [Tooltip("Event fired when break changes")]
        public UnityEvent<int> OnBreakUpdated;
        
        // Internal state
        private float _lastCommentaryTime = 0f;
        private CueStrikeScoreManager _scoreManager;
        private CueStrikeShotManager _shotManager;
        private CueStrikeRulesManager _rulesManager;
        private bool _isInitialized = false;
        
        // Animation parameter hashes
        private static readonly int AnimTriggerSpeak = Animator.StringToHash("TriggerSpeak");
        private static readonly int AnimTriggerCelebrate = Animator.StringToHash("TriggerCelebrate");
        private static readonly int AnimTriggerDisappointed = Animator.StringToHash("TriggerDisappointed");
        private static readonly int AnimTriggerNeutral = Animator.StringToHash("TriggerNeutral");
        private static readonly int AnimBoolIsIdle = Animator.StringToHash("IsIdle");
        
        private void Awake()
        {
            InitializeReferences();
        }
        
        private void Start()
        {
            SubscribeToEvents();
            _isInitialized = true;
            
            if (homePosition != null)
            {
                transform.position = homePosition.position;
                transform.rotation = homePosition.rotation;
            }
            
            SetIdleAnimation(true);
            Debug.Log($"[Uncle Nok] {mascotName} the Elephant Referee is ready for duty!");
        }
        
        private void InitializeReferences()
        {
            _scoreManager = CueStrikeScoreManager.Instance;
            _shotManager = FindFirstObjectByType<CueStrikeShotManager>();
            _rulesManager = FindFirstObjectByType<CueStrikeRulesManager>();
            
            if (mascotAnimator == null)
            {
                mascotAnimator = GetComponentInChildren<Animator>();
            }
        }
        
        private void SubscribeToEvents()
        {
            if (_shotManager != null)
            {
                _shotManager.OnShotCompleted += HandleShotCompleted;
                _shotManager.OnFoulCommitted += HandleFoulCommitted;
            }
            
            if (_rulesManager != null)
            {
                _rulesManager.OnFrameWon += HandleFrameWon;
                _rulesManager.OnScoreChanged += HandleScoreChanged;
            }
        }
        
        private void UnsubscribeFromEvents()
        {
            if (_shotManager != null)
            {
                _shotManager.OnShotCompleted -= HandleShotCompleted;
                _shotManager.OnFoulCommitted -= HandleFoulCommitted;
            }
            
            if (_rulesManager != null)
            {
                _rulesManager.OnFrameWon -= HandleFrameWon;
                _rulesManager.OnScoreChanged -= HandleScoreChanged;
            }
        }
        
        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }
        
        private void Update()
        {
            // Keep mascot facing the table center
            if (homePosition != null)
            {
                Vector3 lookDirection = homePosition.position - transform.position;
                lookDirection.y = 0;
                if (lookDirection != Vector3.zero)
                {
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDirection), Time.deltaTime * 2f);
                }
            }
        }
        
        private void HandleShotCompleted(CueStrikeShotManager.CueStrikeShotData shotData)
        {
            if (!enableCommentary || Time.time - _lastCommentaryTime < commentaryCooldown) return;
            
            if (shotData.ballsPotted > 0)
            {
                // Successful pot(s)
                currentBreak += shotData.pointsScored;
                if (currentBreak > highestBreak) highestBreak = currentBreak;
                OnBreakUpdated?.Invoke(currentBreak);
                
                DeliverCommentary(GetRandomLine(positiveCommentary));
                TriggerAnimation(AnimTriggerCelebrate);
            }
            else if (shotData.isFoul)
            {
                // Foul handled separately
            }
            else
            {
                // Missed shot
                currentBreak = 0;
                OnBreakUpdated?.Invoke(currentBreak);
                
                DeliverCommentary(GetRandomLine(missCommentary));
                TriggerAnimation(AnimTriggerDisappointed);
            }
            
            _lastCommentaryTime = Time.time;
        }
        
        private void HandleFoulCommitted(string foulType, int penaltyPoints)
        {
            if (!enableCommentary || Time.time - _lastCommentaryTime < commentaryCooldown) return;
            
            currentBreak = 0;
            OnBreakUpdated?.Invoke(currentBreak);
            
            DeliverCommentary(GetRandomLine(foulCommentary));
            TriggerAnimation(AnimTriggerDisappointed);
            _lastCommentaryTime = Time.time;
        }
        
        private void HandleScoreChanged(int playerIndex, int newScore)
        {
            if (playerIndex == 0)
                player1Score = newScore;
            else if (playerIndex == 1)
                player2Score = newScore;
            
            OnScoreUpdated?.Invoke(player1Score, player2Score);
        }
        
        private void HandleFrameWon(int winnerPlayerIndex)
        {
            DeliverCommentary(GetRandomLine(victoryCommentary));
            TriggerAnimation(AnimTriggerCelebrate);
            
            // Reset break for next frame
            currentBreak = 0;
            OnBreakUpdated?.Invoke(currentBreak);
        }
        
        private string GetRandomLine(string[] lines)
        {
            if (lines == null || lines.Length == 0) return "";
            return lines[Random.Range(0, lines.Length)];
        }
        
        private void DeliverCommentary(string commentary)
        {
            if (string.IsNullOrEmpty(commentary)) return;
            
            string fullCommentary = $"{mascotName}: \"{commentary}\"";
            OnCommentaryDelivered?.Invoke(fullCommentary);
            Debug.Log($"[Uncle Nok] {fullCommentary}");
            
            TriggerAnimation(AnimTriggerSpeak);
        }
        
        private void TriggerAnimation(int triggerHash)
        {
            if (mascotAnimator != null)
            {
                mascotAnimator.SetTrigger(triggerHash);
            }
        }
        
        private void SetIdleAnimation(bool isIdle)
        {
            if (mascotAnimator != null)
            {
                mascotAnimator.SetBool(AnimBoolIsIdle, isIdle);
            }
        }
        
        /// <summary>
        /// Manually trigger a specific commentary type
        /// </summary>
        public void TriggerCommentary(CommentaryType type)
        {
            if (!enableCommentary) return;
            
            string line = type switch
            {
                CommentaryType.Positive => GetRandomLine(positiveCommentary),
                CommentaryType.Neutral => GetRandomLine(neutralCommentary),
                CommentaryType.Miss => GetRandomLine(missCommentary),
                CommentaryType.Foul => GetRandomLine(foulCommentary),
                CommentaryType.Victory => GetRandomLine(victoryCommentary),
                _ => GetRandomLine(neutralCommentary)
            };
            
            DeliverCommentary(line);
        }
        
        /// <summary>
        /// Set the mascot's home position (call when table position changes)
        /// </summary>
        public void SetHomePosition(Transform newHomePosition)
        {
            homePosition = newHomePosition;
            if (homePosition != null)
            {
                transform.position = homePosition.position;
                transform.rotation = homePosition.rotation;
            }
        }
        
        /// <summary>
        /// Get current break score
        /// </summary>
        public int GetCurrentBreak() => currentBreak;
        
        /// <summary>
        /// Get highest break this session
        /// </summary>
        public int GetHighestBreak() => highestBreak;
        
        /// <summary>
        /// Reset scores for new game
        /// </summary>
        public void ResetScores()
        {
            player1Score = 0;
            player2Score = 0;
            currentBreak = 0;
            highestBreak = 0;
            OnScoreUpdated?.Invoke(0, 0);
            OnBreakUpdated?.Invoke(0);
        }
        
        // ============================================================
        // Compatibility methods for MascotGameplayIntegration
        // ============================================================
        
        /// <summary>
        /// Handle shot result from gameplay integration
        /// </summary>
        public void OnShotResult(CueStrikeMascotManager.ShotResult result, float frequencyMultiplier)
        {
            if (!enableCommentary || Time.time - _lastCommentaryTime < commentaryCooldown) return;
            
            switch (result)
            {
                case CueStrikeMascotManager.ShotResult.Potted:
                    currentBreak += 1; // Approximate, actual points from shot manager
                    if (currentBreak > highestBreak) highestBreak = currentBreak;
                    OnBreakUpdated?.Invoke(currentBreak);
                    DeliverCommentary(GetRandomLine(positiveCommentary));
                    TriggerAnimation(AnimTriggerCelebrate);
                    break;
                    
                case CueStrikeMascotManager.ShotResult.Missed:
                    currentBreak = 0;
                    OnBreakUpdated?.Invoke(currentBreak);
                    DeliverCommentary(GetRandomLine(missCommentary));
                    TriggerAnimation(AnimTriggerDisappointed);
                    break;
                    
                case CueStrikeMascotManager.ShotResult.Foul:
                    // Handled via OnFoul
                    break;
                    
                case CueStrikeMascotManager.ShotResult.Safety:
                    DeliverCommentary(GetRandomLine(neutralCommentary));
                    TriggerAnimation(AnimTriggerNeutral);
                    break;
                    
                case CueStrikeMascotManager.ShotResult.Break:
                    DeliverCommentary(GetRandomLine(positiveCommentary));
                    TriggerAnimation(AnimTriggerSpeak);
                    break;
                    
                case CueStrikeMascotManager.ShotResult.Win:
                    DeliverCommentary(GetRandomLine(victoryCommentary));
                    TriggerAnimation(AnimTriggerCelebrate);
                    break;
            }
            
            _lastCommentaryTime = Time.time;
        }
        
        /// <summary>
        /// Handle foul from gameplay integration
        /// </summary>
        public void OnFoul(CueStrikeMascotManager.FoulType foulType, string description)
        {
            if (!enableCommentary || Time.time - _lastCommentaryTime < commentaryCooldown) return;
            
            currentBreak = 0;
            OnBreakUpdated?.Invoke(currentBreak);
            
            DeliverCommentary(GetRandomLine(foulCommentary));
            TriggerAnimation(AnimTriggerDisappointed);
            _lastCommentaryTime = Time.time;
        }
        
        /// <summary>
        /// Handle frame event from gameplay integration
        /// </summary>
        public void OnFrameEvent(CueStrikeMascotManager.FrameEvent frameEvent, int playerIndex)
        {
            if (!enableCommentary || Time.time - _lastCommentaryTime < commentaryCooldown) return;
            
            string playerName = playerIndex == 0 ? "Player One" : "Player Two";
            
            switch (frameEvent)
            {
                case CueStrikeMascotManager.FrameEvent.FrameStart:
                    currentBreak = 0;
                    OnBreakUpdated?.Invoke(currentBreak);
                    DeliverCommentary($"Frame start. {playerName} to break.");
                    TriggerAnimation(AnimTriggerSpeak);
                    break;
                    
                case CueStrikeMascotManager.FrameEvent.FrameEnd:
                    DeliverCommentary($"Frame over. {playerName} wins the frame.");
                    TriggerAnimation(AnimTriggerCelebrate);
                    break;
                    
                case CueStrikeMascotManager.FrameEvent.MatchStart:
                    highestBreak = 0;
                    DeliverCommentary("Match start. Best of luck to both players.");
                    TriggerAnimation(AnimTriggerSpeak);
                    break;
                    
                case CueStrikeMascotManager.FrameEvent.MatchEnd:
                    DeliverCommentary($"Match over. {playerName} wins the match.");
                    TriggerAnimation(AnimTriggerCelebrate);
                    break;
                    
                case CueStrikeMascotManager.FrameEvent.PlayerTurnStart:
                    DeliverCommentary($"{playerName} at the table.");
                    TriggerAnimation(AnimTriggerSpeak);
                    break;
                    
                case CueStrikeMascotManager.FrameEvent.PlayerTurnEnd:
                    currentBreak = 0;
                    OnBreakUpdated?.Invoke(currentBreak);
                    break;
            }
            
            _lastCommentaryTime = Time.time;
        }
        
        /// <summary>
        /// Handle break event from gameplay integration
        /// </summary>
        public void OnBreakEvent(CueStrikeMascotManager.BreakEvent breakEvent, int playerIndex)
        {
            if (!enableCommentary || Time.time - _lastCommentaryTime < commentaryCooldown) return;
            
            string playerName = playerIndex == 0 ? "Player One" : "Player Two";
            
            switch (breakEvent)
            {
                case CueStrikeMascotManager.BreakEvent.BreakShot:
                    DeliverCommentary($"{playerName} breaks.");
                    TriggerAnimation(AnimTriggerSpeak);
                    break;
                    
                case CueStrikeMascotManager.BreakEvent.BreakAndRun:
                    DeliverCommentary($"Break and run! Magnificent from {playerName}.");
                    TriggerAnimation(AnimTriggerCelebrate);
                    break;
                    
                case CueStrikeMascotManager.BreakEvent.DryBreak:
                    DeliverCommentary("Dry break. No balls potted.");
                    TriggerAnimation(AnimTriggerDisappointed);
                    break;
                    
                case CueStrikeMascotManager.BreakEvent.FoulOnBreak:
                    DeliverCommentary("Foul on the break.");
                    TriggerAnimation(AnimTriggerDisappointed);
                    break;
            }
            
            _lastCommentaryTime = Time.time;
        }
        
        /// <summary>
        /// Handle milestone event from gameplay integration
        /// </summary>
        public void OnMilestone(CueStrikeMascotManager.MilestoneType milestone, int playerIndex, int value)
        {
            if (!enableCommentary || Time.time - _lastCommentaryTime < commentaryCooldown) return;
            
            string playerName = playerIndex == 0 ? "Player One" : "Player Two";
            
            switch (milestone)
            {
                case CueStrikeMascotManager.MilestoneType.CenturyBreak:
                    currentBreak = value;
                    if (value >= 147)
                    {
                        DeliverCommentary($"{playerName} makes a maximum break! One hundred and forty-seven!");
                        TriggerAnimation(AnimTriggerCelebrate);
                    }
                    else
                    {
                        DeliverCommentary($"{playerName} reaches a century! {value} and counting.");
                        TriggerAnimation(AnimTriggerCelebrate);
                    }
                    highestBreak = Mathf.Max(highestBreak, value);
                    break;
                    
                case CueStrikeMascotManager.MilestoneType.HighBreak:
                    currentBreak = value;
                    if (value > highestBreak)
                    {
                        DeliverCommentary($"{playerName} sets a new high break of {value}.");
                        TriggerAnimation(AnimTriggerCelebrate);
                    }
                    highestBreak = Mathf.Max(highestBreak, value);
                    break;
                    
                case CueStrikeMascotManager.MilestoneType.MaximumBreak:
                    DeliverCommentary($"{playerName} achieves the perfect clearance! Maximum break!");
                    TriggerAnimation(AnimTriggerCelebrate);
                    break;
                    
                case CueStrikeMascotManager.MilestoneType.Clearance:
                    DeliverCommentary($"{playerName} clears the table! A total clearance of {value}.");
                    TriggerAnimation(AnimTriggerCelebrate);
                    break;
                    
                case CueStrikeMascotManager.MilestoneType.SnookerEscape:
                    DeliverCommentary("Escaped the snooker! Superb recovery.");
                    TriggerAnimation(AnimTriggerSpeak);
                    break;
                    
                case CueStrikeMascotManager.MilestoneType.Fluke:
                    DeliverCommentary("A fortunate fluke! The balls have a mind of their own.");
                    TriggerAnimation(AnimTriggerNeutral);
                    break;
            }
            
            _lastCommentaryTime = Time.time;
        }
    }
    
    /// <summary>
    /// Types of commentary Uncle Nok can deliver
    /// </summary>
    public enum CommentaryType
    {
        Positive,
        Neutral,
        Miss,
        Foul,
        Victory
    }
}
