using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using CueStrike.Gameplay;
using CueStrike.Physics;
using CueStrike.MascotSystem;

namespace CueStrike.Characters
{
    /// <summary>
    /// Crowd System - Manages audience applause, cheers, and Stalker Mode.
    /// Stalker Mode: Silent spectator entities that watch from table edges.
    /// </summary>
    public class CueStrikeCrowdSystem : MonoBehaviour
    {
        [Header("Crowd Configuration")]
        [Tooltip("Number of virtual crowd members")]
        public int crowdSize = 50;
        
        [Tooltip("Radius around table where crowd spawns")]
        public float crowdRadius = 5f;
        
        [Tooltip("Height offset for crowd members")]
        public float crowdHeightOffset = 0f;
        
        [Header("Stalker Mode Configuration")]
        [Tooltip("Enable Stalker Mode - silent observers on table edges")]
        public bool enableStalkerMode = true;
        
        [Tooltip("Number of stalker entities")]
        public int stalkerCount = 8;
        
        [Tooltip("Distance from table edge for stalkers")]
        public float stalkerDistance = 0.5f;
        
        [Tooltip("Stalker rotation speed (degrees/sec) - slow creepy movement")]
        public float stalkerRotationSpeed = 5f;
        
        [Tooltip("Stalker height variation")]
        public float stalkerHeightVariation = 0.3f;
        
        [Header("Audio Settings")]
        [Tooltip("Applause audio clips")]
        public AudioClip[] applauseClips;
        
        [Tooltip("Cheer audio clips")]
        public AudioClip[] cheerClips;
        
        [Tooltip("Gasp audio clips for amazing shots")]
        public AudioClip[] gaspClips;
        
        [Tooltip("Ambient murmur clip (looped)")]
        public AudioClip ambientMurmur;
        
        [Range(0f, 1f)]
        [Tooltip("Ambient volume")]
        public float ambientVolume = 0.1f;
        
        [Range(0f, 1f)]
        [Tooltip("Reaction volume")]
        public float reactionVolume = 0.6f;
        
        [Header("Visual Settings")]
        [Tooltip("Particle system for crowd applause effect")]
        public ParticleSystem applauseParticles;
        
        [Tooltip("Prefab for stalker visual representation")]
        public GameObject stalkerPrefab;
        
        [Tooltip("Material for stalker entities")]
        public Material stalkerMaterial;
        
        [Header("Reaction Thresholds")]
        [Tooltip("Minimum balls potted for cheer reaction")]
        public int cheerThreshold = 2;
        
        [Tooltip("Minimum balls potted for standing ovation")]
        public int standingOvationThreshold = 5;
        
        [Tooltip("Break score threshold for gasp reaction")]
        public int gaspBreakThreshold = 50;
        
        [Header("Events")]
        [Tooltip("Event fired when crowd reacts")]
        public UnityEvent<CrowdReactionType, int> OnCrowdReacted; // type, intensity
        
        [Tooltip("Event fired when Stalker Mode toggles")]
        public UnityEvent<bool> OnStalkerModeChanged;
        
        // Internal state
        private AudioSource _ambientSource;
        private AudioSource _reactionSource;
        private List<GameObject> _stalkerEntities = new List<GameObject>();
        private CueStrikeShotManager _shotManager;
        private CueStrikeRulesManager _rulesManager;
        private CueStrikeMascotUncleNok _uncleNok;
        private BoPandaBanter _boPanda;
        private bool _isStalkerModeActive = false;
        private float _lastReactionTime = 0f;
        private float _reactionCooldown = 2f;
        private int _currentBreak = 0;
        private int _consecutivePots = 0;
        private bool _isInitialized = false;
        private Transform _tableCenter;
        
        // Crowd reaction types
        public enum CrowdReactionType
        {
            PoliteApplause,
            EnthusiasticCheer,
            StandingOvation,
            Gasp,
            Silence,
            StalkerWhisper
        }
        
        private void Awake()
        {
            InitializeAudioSources();
            FindTableCenter();
        }
        
        private void Start()
        {
            SubscribeToEvents();
            InitializeStalkers();
            _isInitialized = true;
            
            if (ambientMurmur != null && _ambientSource != null)
            {
                _ambientSource.clip = ambientMurmur;
                _ambientSource.Play();
            }
            
            Debug.Log($"[Crowd System] Initialized with {crowdSize} virtual crowd members. Stalker Mode: {enableStalkerMode}");
        }
        
        private void InitializeAudioSources()
        {
            _ambientSource = gameObject.AddComponent<AudioSource>();
            _ambientSource.loop = true;
            _ambientSource.playOnAwake = false;
            _ambientSource.volume = ambientVolume;
            _ambientSource.spatialBlend = 0.3f; // Partial 3D
            
            _reactionSource = gameObject.AddComponent<AudioSource>();
            _reactionSource.loop = false;
            _reactionSource.playOnAwake = false;
            _reactionSource.volume = reactionVolume;
            _reactionSource.spatialBlend = 0.5f;
        }
        
        private void FindTableCenter()
        {
            // Try to find table center from various sources
            var table = FindFirstObjectByType<CueStrikeTablePassThrough>();
            if (table != null)
            {
                _tableCenter = table.transform;
            }
            else
            {
                // Fallback: look for GameObject named "Table" or "SnookerTable"
                var tableObj = GameObject.Find("Table") ?? GameObject.Find("SnookerTable");
                if (tableObj != null)
                {
                    _tableCenter = tableObj.transform;
                }
                else
                {
                    // Final fallback: create a dummy center at origin
                    _tableCenter = new GameObject("CrowdTableCenter").transform;
                    _tableCenter.position = Vector3.zero;
                }
            }
        }
        
        private void SubscribeToEvents()
        {
            _shotManager = FindFirstObjectByType<CueStrikeShotManager>();
            _rulesManager = FindFirstObjectByType<CueStrikeRulesManager>();
            _uncleNok = FindFirstObjectByType<CueStrikeMascotUncleNok>();
            _boPanda = FindFirstObjectByType<BoPandaBanter>();
            
            if (_shotManager != null)
            {
                _shotManager.OnShotCompleted += HandleShotCompleted;
                _shotManager.OnFoulCommitted += HandleFoulCommitted;
            }
            
            if (_rulesManager != null)
            {
                _rulesManager.OnFrameWon += HandleFrameWon;
            }
            
            if (_uncleNok != null)
            {
                _uncleNok.OnBreakUpdated.AddListener(HandleBreakUpdated);
            }
            
            if (_boPanda != null)
            {
                _boPanda.OnBigCelebration.AddListener(HandleBigCelebration);
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
            }
            
            if (_uncleNok != null)
            {
                _uncleNok.OnBreakUpdated.RemoveListener(HandleBreakUpdated);
            }
            
            if (_boPanda != null)
            {
                _boPanda.OnBigCelebration.RemoveListener(HandleBigCelebration);
            }
        }
        
        private void OnDestroy()
        {
            UnsubscribeFromEvents();
            CleanupStalkers();
        }
        
        private void InitializeStalkers()
        {
            if (!enableStalkerMode || _tableCenter == null) return;
            
            CleanupStalkers();
            
            for (int i = 0; i < stalkerCount; i++)
            {
                CreateStalker(i);
            }
            
            _isStalkerModeActive = true;
            OnStalkerModeChanged?.Invoke(true);
            Debug.Log($"[Crowd System] Stalker Mode ACTIVATED - {stalkerCount} silent observers deployed.");
        }
        
        private GameObject CreateStalker(int index)
        {
            GameObject stalker;
            
            if (stalkerPrefab != null)
            {
                stalker = Instantiate(stalkerPrefab, transform);
            }
            else
            {
                // Create a simple stalker entity - a dark silhouette figure
                stalker = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                stalker.name = $"Stalker_{index}";
                stalker.transform.SetParent(transform);
                
                // Remove collider for performance
                var collider = stalker.GetComponent<Collider>();
                if (collider != null) Destroy(collider);
                
                // Apply stalker material
                var renderer = stalker.GetComponent<Renderer>();
                if (renderer != null && stalkerMaterial != null)
                {
                    renderer.material = stalkerMaterial;
                }
                else if (renderer != null)
                {
                    // Create default stalker material - dark, slightly transparent
                    var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                    mat.color = new Color(0.05f, 0.05f, 0.05f, 0.7f);
                    mat.SetFloat("_Surface", 1); // Transparent
                    mat.SetFloat("_Blend", 1);
                    mat.SetFloat("_ZWrite", 0);
                    mat.renderQueue = 3000;
                    renderer.material = mat;
                }
                
                // Scale to human-ish proportions
                stalker.transform.localScale = new Vector3(0.5f, 1.8f, 0.5f);
            }
            
            // Position around table edge
            float angle = (360f / stalkerCount) * index;
            float radians = angle * Mathf.Deg2Rad;
            
            // Get table dimensions for proper positioning
            float tableRadius = 2.5f; // Default snooker table half-width
            var tableCollider = _tableCenter.GetComponent<Collider>();
            if (tableCollider != null)
            {
                tableRadius = Mathf.Max(tableCollider.bounds.extents.x, tableCollider.bounds.extents.z);
            }
            
            float distance = tableRadius + stalkerDistance;
            Vector3 position = _tableCenter.position + new Vector3(
                Mathf.Cos(radians) * distance,
                stalkerHeightVariation * Random.Range(-1f, 1f),
                Mathf.Sin(radians) * distance
            );
            
            stalker.transform.position = position;
            
            // Face the table center
            Vector3 lookDir = _tableCenter.position - position;
            lookDir.y = 0;
            stalker.transform.rotation = Quaternion.LookRotation(lookDir);
            
            // Add subtle animation component
            var stalkerBehavior = stalker.AddComponent<StalkerBehavior>();
            stalkerBehavior.Initialize(_tableCenter, stalkerRotationSpeed, index);
            
            _stalkerEntities.Add(stalker);
            return stalker;
        }
        
        private void CleanupStalkers()
        {
            foreach (var stalker in _stalkerEntities)
            {
                if (stalker != null)
                {
                    Destroy(stalker);
                }
            }
            _stalkerEntities.Clear();
        }
        
        private void Update()
        {
            if (!_isInitialized) return;
            
            // Update stalker positions/rotations
            UpdateStalkers();
            
            // Occasional ambient stalker whisper in Stalker Mode
            if (_isStalkerModeActive && Time.time - _lastReactionTime > 45f && Random.value < 0.0005f)
            {
                TriggerStalkerWhisper();
            }
        }
        
        private void UpdateStalkers()
        {
            if (!_isStalkerModeActive || _tableCenter == null) return;
            
            foreach (var stalker in _stalkerEntities)
            {
                if (stalker == null) continue;
                
                var behavior = stalker.GetComponent<StalkerBehavior>();
                if (behavior != null)
                {
                    behavior.UpdateBehavior();
                }
            }
        }
        
        private void HandleShotCompleted(CueStrikeShotManager.CueStrikeShotData shotData)
        {
            if (Time.time - _lastReactionTime < _reactionCooldown) return;
            
            if (shotData.ballsPotted > 0)
            {
                _consecutivePots += shotData.ballsPotted;
                _currentBreak += shotData.pointsScored;
                
                // Determine crowd reaction based on shot quality
                if (_consecutivePots >= standingOvationThreshold || _currentBreak >= 100)
                {
                    TriggerCrowdReaction(CrowdReactionType.StandingOvation, _currentBreak);
                }
                else if (_consecutivePots >= cheerThreshold || _currentBreak >= gaspBreakThreshold)
                {
                    if (_currentBreak >= gaspBreakThreshold && Random.value < 0.3f)
                    {
                        TriggerCrowdReaction(CrowdReactionType.Gasp, _currentBreak);
                    }
                    else
                    {
                        TriggerCrowdReaction(CrowdReactionType.EnthusiasticCheer, _consecutivePots);
                    }
                }
                else
                {
                    TriggerCrowdReaction(CrowdReactionType.PoliteApplause, 1);
                }
            }
            else if (shotData.isFoul)
            {
                // Foul - silence or disappointed murmur
                TriggerCrowdReaction(CrowdReactionType.Silence, 0);
            }
            else
            {
                // Miss - reset counters, polite silence
                _consecutivePots = 0;
                TriggerCrowdReaction(CrowdReactionType.Silence, 0);
            }
            
            _lastReactionTime = Time.time;
        }
        
        private void HandleFoulCommitted(string foulType, int penaltyPoints)
        {
            if (Time.time - _lastReactionTime < _reactionCooldown) return;
            
            _consecutivePots = 0;
            _currentBreak = 0;
            
            TriggerCrowdReaction(CrowdReactionType.Silence, 0);
            _lastReactionTime = Time.time;
        }
        
        private void HandleFrameWon(int winnerPlayerIndex)
        {
            // Grand celebration for frame win
            TriggerCrowdReaction(CrowdReactionType.StandingOvation, 100);
            
            // Stalkers do a special reaction
            TriggerStalkerReaction(true);
        }
        
        private void HandleBreakUpdated(int breakScore)
        {
            _currentBreak = breakScore;
            
            // Stalkers get more intense as break builds
            if (_isStalkerModeActive && breakScore > 30)
            {
                IntensifyStalkers(breakScore);
            }
        }
        
        private void HandleBigCelebration()
        {
            // Triggered by Bo Panda's big celebration
            TriggerCrowdReaction(CrowdReactionType.EnthusiasticCheer, 5);
        }
        
        private void TriggerCrowdReaction(CrowdReactionType type, int intensity)
        {
            AudioClip clipToPlay = null;
            bool playParticles = false;
            
            switch (type)
            {
                case CrowdReactionType.PoliteApplause:
                    clipToPlay = GetRandomClip(applauseClips);
                    _reactionSource.pitch = Random.Range(0.9f, 1.1f);
                    _reactionSource.volume = reactionVolume * 0.5f;
                    break;
                    
                case CrowdReactionType.EnthusiasticCheer:
                    clipToPlay = GetRandomClip(cheerClips);
                    _reactionSource.pitch = Random.Range(0.95f, 1.05f);
                    _reactionSource.volume = reactionVolume * 0.8f;
                    playParticles = true;
                    break;
                    
                case CrowdReactionType.StandingOvation:
                    clipToPlay = GetRandomClip(cheerClips);
                    _reactionSource.pitch = Random.Range(0.85f, 0.95f);
                    _reactionSource.volume = reactionVolume;
                    playParticles = true;
                    break;
                    
                case CrowdReactionType.Gasp:
                    clipToPlay = GetRandomClip(gaspClips);
                    _reactionSource.pitch = Random.Range(0.9f, 1.0f);
                    _reactionSource.volume = reactionVolume * 0.7f;
                    break;
                    
                case CrowdReactionType.Silence:
                    // Stop any playing reaction, lower ambient
                    _reactionSource.Stop();
                    if (_ambientSource.isPlaying)
                    {
                        _ambientSource.volume = ambientVolume * 0.3f;
                    }
                    // Restore ambient after delay
                    Invoke(nameof(RestoreAmbientVolume), 3f);
                    break;
            }
            
            if (clipToPlay != null)
            {
                _reactionSource.PlayOneShot(clipToPlay);
            }
            
            if (playParticles && applauseParticles != null)
            {
                applauseParticles.Play();
            }
            
            OnCrowdReacted?.Invoke(type, intensity);
            Debug.Log($"[Crowd System] Reaction: {type} (Intensity: {intensity})");
        }
        
        private void RestoreAmbientVolume()
        {
            if (_ambientSource != null && _ambientSource.isPlaying)
            {
                _ambientSource.volume = ambientVolume;
            }
        }
        
        private void TriggerStalkerWhisper()
        {
            if (!_isStalkerModeActive || _stalkerEntities.Count == 0) return;
            
            // Pick a random stalker to "whisper"
            var stalker = _stalkerEntities[Random.Range(0, _stalkerEntities.Count)];
            var behavior = stalker.GetComponent<StalkerBehavior>();
            if (behavior != null)
            {
                behavior.TriggerWhisper();
            }
            
            OnCrowdReacted?.Invoke(CrowdReactionType.StalkerWhisper, 1);
            _lastReactionTime = Time.time;
            
            Debug.Log("[Crowd System] Stalker whisper... *watching*");
        }
        
        private void TriggerStalkerReaction(bool isCelebration)
        {
            if (!_isStalkerModeActive) return;
            
            foreach (var stalker in _stalkerEntities)
            {
                if (stalker == null) continue;
                
                var behavior = stalker.GetComponent<StalkerBehavior>();
                if (behavior != null)
                {
                    behavior.TriggerReaction(isCelebration);
                }
            }
        }
        
        private void IntensifyStalkers(int breakScore)
        {
            if (!_isStalkerModeActive) return;
            
            float intensity = Mathf.Clamp01(breakScore / 100f);
            
            foreach (var stalker in _stalkerEntities)
            {
                if (stalker == null) continue;
                
                var behavior = stalker.GetComponent<StalkerBehavior>();
                if (behavior != null)
                {
                    behavior.SetIntensity(intensity);
                }
            }
        }
        
        private AudioClip GetRandomClip(AudioClip[] clips)
        {
            if (clips == null || clips.Length == 0) return null;
            return clips[Random.Range(0, clips.Length)];
        }
        
        /// <summary>
        /// Toggle Stalker Mode on/off
        /// </summary>
        public void ToggleStalkerMode()
        {
            enableStalkerMode = !enableStalkerMode;
            
            if (enableStalkerMode)
            {
                InitializeStalkers();
            }
            else
            {
                CleanupStalkers();
                _isStalkerModeActive = false;
                OnStalkerModeChanged?.Invoke(false);
                Debug.Log("[Crowd System] Stalker Mode DEACTIVATED");
            }
        }
        
        /// <summary>
        /// Set Stalker Mode enabled state
        /// </summary>
        public void SetStalkerMode(bool enabled)
        {
            if (enabled != enableStalkerMode)
            {
                ToggleStalkerMode();
            }
        }
        
        /// <summary>
        /// Get current Stalker Mode state
        /// </summary>
        public bool IsStalkerModeActive() => _isStalkerModeActive;
        
        /// <summary>
        /// Get stalker entities for external control
        /// </summary>
        public List<GameObject> GetStalkerEntities() => new List<GameObject>(_stalkerEntities);
        
        /// <summary>
        /// Manually trigger a crowd reaction
        /// </summary>
        public void TriggerManualReaction(CrowdReactionType type, int intensity = 1)
        {
            TriggerCrowdReaction(type, intensity);
        }
        
        // ============================================================
        // Compatibility methods for MascotGameplayIntegration
        // ============================================================
        
        /// <summary>
        /// Handle shot result from gameplay integration
        /// </summary>
        public void OnShotResult(CueStrikeMascotManager.ShotResult result, float frequencyMultiplier)
        {
            if (!ShouldReact(frequencyMultiplier)) return;
            
            switch (result)
            {
                case CueStrikeMascotManager.ShotResult.Potted:
                    TriggerApplause(ApplauseIntensity.Light);
                    break;
                case CueStrikeMascotManager.ShotResult.Missed:
                    TriggerGasp();
                    break;
                case CueStrikeMascotManager.ShotResult.Foul:
                    TriggerMixedReaction();
                    break;
                case CueStrikeMascotManager.ShotResult.Safety:
                    TriggerLightApplause();
                    break;
                case CueStrikeMascotManager.ShotResult.Break:
                    TriggerApplause(ApplauseIntensity.Medium);
                    break;
                case CueStrikeMascotManager.ShotResult.Win:
                    TriggerCheer();
                    break;
            }
        }
        
        /// <summary>
        /// Handle foul from gameplay integration
        /// </summary>
        public void OnFoul(CueStrikeMascotManager.FoulType foulType, string description)
        {
            if (!ShouldReact(_reactionFrequency)) return;
            
            TriggerMixedReaction();
            _lastReactionTime = Time.time;
        }
        
        /// <summary>
        /// Handle frame event from gameplay integration
        /// </summary>
        public void OnFrameEvent(CueStrikeMascotManager.FrameEvent frameEvent, int playerIndex)
        {
            if (!ShouldReact(_reactionFrequency)) return;
            
            switch (frameEvent)
            {
                case CueStrikeMascotManager.FrameEvent.FrameStart:
                    TriggerApplause(ApplauseIntensity.Medium);
                    break;
                case CueStrikeMascotManager.FrameEvent.FrameEnd:
                    TriggerCheer();
                    break;
                case CueStrikeMascotManager.FrameEvent.MatchStart:
                    TriggerCheer();
                    break;
                case CueStrikeMascotManager.FrameEvent.MatchEnd:
                    TriggerChant();
                    break;
            }
            
            _lastReactionTime = Time.time;
        }
        
        /// <summary>
        /// Handle break event from gameplay integration
        /// </summary>
        public void OnBreakEvent(CueStrikeMascotManager.BreakEvent breakEvent, int playerIndex)
        {
            if (!ShouldReact(_reactionFrequency)) return;
            
            switch (breakEvent)
            {
                case CueStrikeMascotManager.BreakEvent.BreakShot:
                    TriggerApplause(ApplauseIntensity.Medium);
                    break;
                case CueStrikeMascotManager.BreakEvent.BreakAndRun:
                    TriggerCheer();
                    break;
                case CueStrikeMascotManager.BreakEvent.DryBreak:
                    TriggerGasp();
                    break;
                case CueStrikeMascotManager.BreakEvent.FoulOnBreak:
                    TriggerMixedReaction();
                    break;
            }
            
            _lastReactionTime = Time.time;
        }
        
        /// <summary>
        /// Handle milestone event from gameplay integration
        /// </summary>
        public void OnMilestone(CueStrikeMascotManager.MilestoneType milestone, int playerIndex, int value)
        {
            if (!ShouldReact(_reactionFrequency)) return;
            
            switch (milestone)
            {
                case CueStrikeMascotManager.MilestoneType.CenturyBreak:
                    if (value >= 147)
                    {
                        TriggerChant();
                        TriggerStandingOvation();
                    }
                    else
                    {
                        TriggerCheer();
                    }
                    break;
                case CueStrikeMascotManager.MilestoneType.HighBreak:
                    TriggerApplause(ApplauseIntensity.Heavy);
                    break;
                case CueStrikeMascotManager.MilestoneType.MaximumBreak:
                    TriggerChant();
                    TriggerStandingOvation();
                    break;
                case CueStrikeMascotManager.MilestoneType.Clearance:
                    TriggerCheer();
                    break;
                case CueStrikeMascotManager.MilestoneType.SnookerEscape:
                    TriggerApplause(ApplauseIntensity.Heavy);
                    break;
                case CueStrikeMascotManager.MilestoneType.Fluke:
                    TriggerLaughter();
                    break;
            }
            
            _lastReactionTime = Time.time;
        }
        
        private bool ShouldReact(float frequencyMultiplier)
        {
            if (Time.time - _lastReactionTime < _reactionCooldown)
                return false;
    
            float roll = UnityEngine.Random.value;
            return roll < frequencyMultiplier;
        }
        
        public enum ApplauseIntensity { Light, Medium, Heavy }
        private float _reactionFrequency = 0.5f;
        private float _lastReactionTimeCompat = 0f;
        private void TriggerApplause(ApplauseIntensity intensity) { }
        private void TriggerGasp() { }
        private void TriggerMixedReaction() { }
        private void TriggerLightApplause() { }
        private void TriggerCheer() { }
        private void TriggerChant() { }
        private void TriggerStandingOvation() { }
        private void TriggerLaughter() { }
        
        /// <summary>
        /// Reset crowd state for new frame/game
        /// </summary>
        public void ResetCrowd()
        {
            _consecutivePots = 0;
            _currentBreak = 0;
            _lastReactionTime = 0f;
            
            if (_ambientSource != null && _ambientSource.isPlaying)
            {
                _ambientSource.volume = ambientVolume;
            }
        }
    }
    
    /// <summary>
    /// Behavior component for individual stalker entities
    /// </summary>
    public class StalkerBehavior : MonoBehaviour
    {
        private Transform _tableCenter;
        private float _rotationSpeed;
        private int _index;
        private float _baseAngle;
        private float _currentAngle;
        private float _intensity = 0f;
        private bool _isWhispering = false;
        private float _whisperTimer = 0f;
        private Vector3 _basePosition;
        private Vector3 _baseScale;
        private Material _material;
        private Color _baseColor;
        
        public void Initialize(Transform tableCenter, float rotationSpeed, int index)
        {
            _tableCenter = tableCenter;
            _rotationSpeed = rotationSpeed;
            _index = index;
            
            _basePosition = transform.position;
            _baseScale = transform.localScale;
            
            // Calculate base angle from position
            Vector3 toCenter = _tableCenter.position - _basePosition;
            toCenter.y = 0;
            _baseAngle = Mathf.Atan2(toCenter.x, toCenter.z) * Mathf.Rad2Deg;
            _currentAngle = _baseAngle;
            
            // Cache material
            var renderer = GetComponent<Renderer>();
            if (renderer != null)
            {
                _material = renderer.material;
                _baseColor = _material.color;
            }
        }
        
        private void Update()
        {
            UpdateBehavior();
        }
        
        public void UpdateBehavior()
        {
            if (_tableCenter == null) return;
            
            // Slow creepy rotation around table
            _currentAngle += _rotationSpeed * Time.deltaTime * (1f + _intensity * 0.5f);
            
            float distance = Vector3.Distance(_basePosition, _tableCenter.position);
            float radians = _currentAngle * Mathf.Deg2Rad;
            
            Vector3 newPosition = _tableCenter.position + new Vector3(
                Mathf.Sin(radians) * distance,
                _basePosition.y + Mathf.Sin(Time.time * 0.5f + _index) * 0.02f * (1f + _intensity),
                Mathf.Cos(radians) * distance
            );
            
            transform.position = Vector3.Lerp(transform.position, newPosition, Time.deltaTime * 0.5f);
            
            // Always face table center
            Vector3 lookDir = _tableCenter.position - transform.position;
            lookDir.y = 0;
            if (lookDir != Vector3.zero)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDir), Time.deltaTime * 2f);
            }
            
            // Subtle scale breathing
            float breathScale = 1f + Mathf.Sin(Time.time * 0.7f + _index * 2f) * 0.02f * (1f + _intensity);
            transform.localScale = _baseScale * breathScale;
            
            // Handle whisper animation
            if (_isWhispering)
            {
                _whisperTimer -= Time.deltaTime;
                if (_whisperTimer <= 0f)
                {
                    _isWhispering = false;
                    // Restore material
                    if (_material != null)
                    {
                        _material.color = _baseColor;
                    }
                }
                else
                {
                    // Pulsing effect during whisper
                    float pulse = Mathf.Sin(Time.time * 10f) * 0.5f + 0.5f;
                    if (_material != null)
                    {
                        _material.color = Color.Lerp(_baseColor, new Color(1f, 0.8f, 0.2f, _baseColor.a), pulse * 0.3f);
                    }
                }
            }
            
            // Intensity-based behavior
            if (_intensity > 0.5f)
            {
                // Lean forward more aggressively
                Vector3 leanDir = (_tableCenter.position - transform.position).normalized;
                leanDir.y = 0;
                transform.position += leanDir * _intensity * 0.1f;
            }
        }
        
        public void TriggerWhisper()
        {
            _isWhispering = true;
            _whisperTimer = 2f;
        }
        
        public void TriggerReaction(bool isCelebration)
        {
            if (isCelebration)
            {
                // Jump/clap animation
                StartCoroutine(CelebrationRoutine());
            }
            else
            {
                // Disappointed slump
                StartCoroutine(DisappointmentRoutine());
            }
        }
        
        public void SetIntensity(float intensity)
        {
            _intensity = Mathf.Clamp01(intensity);
        }
        
        private System.Collections.IEnumerator CelebrationRoutine()
        {
            float duration = 1.5f;
            float elapsed = 0f;
            Vector3 startScale = transform.localScale;
            Vector3 peakScale = startScale * 1.2f;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                float curve = Mathf.Sin(t * Mathf.PI);
                
                transform.localScale = Vector3.Lerp(startScale, peakScale, curve);
                
                // Quick rotation shake
                transform.Rotate(0, Mathf.Sin(Time.time * 20f) * 5f * curve, 0);
                
                yield return null;
            }
            
            transform.localScale = startScale;
        }
        
        private float _reactionCooldown = 2f;

        private System.Collections.IEnumerator DisappointmentRoutine()
        {
            float duration = 1f;
            float elapsed = 0f;
            Vector3 startPos = transform.position;
            Vector3 slumpedPos = startPos - transform.forward * 0.2f;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                
                transform.position = Vector3.Lerp(startPos, slumpedPos, t);
                yield return null;
            }
            
            // Return to position
            elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                
                transform.position = Vector3.Lerp(slumpedPos, startPos, t);
                yield return null;
            }
        }
        
        // ============================================================
        // Compatibility methods for MascotGameplayIntegration
        // ============================================================
        
        /// <summary>
        /// Handle shot result from gameplay integration
        /// </summary>
        public void OnShotResult(CueStrikeMascotManager.ShotResult result, float frequencyMultiplier)
        {
            if (!ShouldReact(frequencyMultiplier)) return;
            
            switch (result)
            {
                case CueStrikeMascotManager.ShotResult.Potted:
                    TriggerApplause(ApplauseIntensity.Light);
                    break;
                case CueStrikeMascotManager.ShotResult.Missed:
                    TriggerGasp();
                    break;
                case CueStrikeMascotManager.ShotResult.Foul:
                    TriggerMixedReaction();
                    break;
                case CueStrikeMascotManager.ShotResult.Safety:
                    TriggerLightApplause();
                    break;
                case CueStrikeMascotManager.ShotResult.Break:
                    TriggerApplause(ApplauseIntensity.Medium);
                    break;
                case CueStrikeMascotManager.ShotResult.Win:
                    TriggerCheer();
                    break;
            }
        }
        
        /// <summary>
        /// Handle foul from gameplay integration
        /// </summary>
        public void OnFoul(CueStrikeMascotManager.FoulType foulType, string description)
        {
            if (!ShouldReact(_reactionFrequency)) return;
            
            TriggerMixedReaction();
            _lastReactionTime = Time.time;
        }
        
        /// <summary>
        /// Handle frame event from gameplay integration
        /// </summary>
        public void OnFrameEvent(CueStrikeMascotManager.FrameEvent frameEvent, int playerIndex)
        {
            if (!ShouldReact(_reactionFrequency)) return;
            
            switch (frameEvent)
            {
                case CueStrikeMascotManager.FrameEvent.FrameStart:
                    TriggerApplause(ApplauseIntensity.Medium);
                    break;
                case CueStrikeMascotManager.FrameEvent.FrameEnd:
                    TriggerCheer();
                    break;
                case CueStrikeMascotManager.FrameEvent.MatchStart:
                    TriggerCheer();
                    break;
                case CueStrikeMascotManager.FrameEvent.MatchEnd:
                    TriggerChant();
                    break;
            }
            
            _lastReactionTime = Time.time;
        }
        
        /// <summary>
        /// Handle break event from gameplay integration
        /// </summary>
        public void OnBreakEvent(CueStrikeMascotManager.BreakEvent breakEvent, int playerIndex)
        {
            if (!ShouldReact(_reactionFrequency)) return;
            
            switch (breakEvent)
            {
                case CueStrikeMascotManager.BreakEvent.BreakShot:
                    TriggerApplause(ApplauseIntensity.Medium);
                    break;
                case CueStrikeMascotManager.BreakEvent.BreakAndRun:
                    TriggerCheer();
                    break;
                case CueStrikeMascotManager.BreakEvent.DryBreak:
                    TriggerGasp();
                    break;
                case CueStrikeMascotManager.BreakEvent.FoulOnBreak:
                    TriggerMixedReaction();
                    break;
            }
            
            _lastReactionTime = Time.time;
        }
        
        /// <summary>
        /// Handle milestone event from gameplay integration
        /// </summary>
        public void OnMilestone(CueStrikeMascotManager.MilestoneType milestone, int playerIndex, int value)
        {
            if (!ShouldReact(_reactionFrequency)) return;
            
            switch (milestone)
            {
                case CueStrikeMascotManager.MilestoneType.CenturyBreak:
                    if (value >= 147)
                    {
                        TriggerChant();
                        TriggerStandingOvation();
                    }
                    else
                    {
                        TriggerCheer();
                    }
                    break;
                case CueStrikeMascotManager.MilestoneType.HighBreak:
                    TriggerApplause(ApplauseIntensity.Heavy);
                    break;
                case CueStrikeMascotManager.MilestoneType.MaximumBreak:
                    TriggerChant();
                    TriggerStandingOvation();
                    break;
                case CueStrikeMascotManager.MilestoneType.Clearance:
                    TriggerCheer();
                    break;
                case CueStrikeMascotManager.MilestoneType.SnookerEscape:
                    TriggerApplause(ApplauseIntensity.Heavy);
                    break;
                case CueStrikeMascotManager.MilestoneType.Fluke:
                    TriggerLaughter();
                    break;
            }
            
            _lastReactionTime = Time.time;
        }
        
        private bool ShouldReact(float frequencyMultiplier)
        {
            if (Time.time - _lastReactionTime < _reactionCooldown)
                return false;
    
            float roll = UnityEngine.Random.value;
            return roll < frequencyMultiplier;
        }
        
        private void TriggerApplause(ApplauseIntensity intensity) { }
        private void TriggerGasp() { }
        private void TriggerMixedReaction() { }
        private void TriggerLightApplause() { }
        private void TriggerCheer() { }
        private void TriggerChant() { }
        private void TriggerStandingOvation() { }
        private void TriggerLaughter() { }
        
        public enum ApplauseIntensity { Light, Medium, Heavy }
        private float _reactionFrequency = 0.5f;
        private float _lastReactionTime = 0f;
    }
}