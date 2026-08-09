using System;
using System.Collections.Generic;
using UnityEngine;
using CueStrike.Managers;

namespace CueStrike.NoirMemory
{
    public class NoirMemoryPuzzleManager : MonoBehaviour
    {
        public static NoirMemoryPuzzleManager Instance { get; private set; }

        [Header("References")]
        [SerializeField] private CueStrikeRulesManager rulesManager;
        [SerializeField] private CueStrikePhysicsManager physicsManager;
        [SerializeField] private CueStrikeShotManager shotManager;

        [Header("Noir Material")]
        [SerializeField] private Material noirMaterial;
        [SerializeField] private Material cueBallNoirMaterial;

        [Header("Config")]
        [SerializeField] private NoirMemoryPuzzleConfig config = new NoirMemoryPuzzleConfig();

        private NoirMemoryPuzzleState state = new NoirMemoryPuzzleState();
        private Dictionary<int, Material> originalMaterials = new Dictionary<int, Material>();
        private Dictionary<int, bool> revealedBalls = new Dictionary<int, bool>();
        private List<NoirMemoryBallVisuals> ballVisuals = new List<NoirMemoryBallVisuals>();

        // Events
        public event Action OnRevealPhaseStarted;
        public event Action OnNoirPhaseStarted;
        public event Action<int> OnBallRevealed;
        public event Action OnCorrectBallPottedReward;
        public event Action<float> OnTimerUpdated;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            InitializeBallVisuals();
            SubscribeEvents();
        }

        private void OnDestroy()
        {
            UnsubscribeEvents();
        }

        // ==================== PUBLIC API ====================

        public void StartMemoryMode(float? customDuration = null)
        {
            float duration = customDuration ?? (float)config.revealDuration;
            config.revealDuration = (NoirMemoryTimer)duration;
            
            state.isMemoryModeActive = true;
            StartRevealPhase();
        }

        public void StopMemoryMode()
        {
            state.isMemoryModeActive = false;
            RevealAllBalls();
        }

        public bool IsMemoryModeActive() => state.isMemoryModeActive;
        public bool IsNoirPhase() => state.isNoirPhase;
        public NoirMemoryPuzzleState GetState() => state;

        // Called by NoirMemoryBallVisuals when cue ball hits a ball
        public void OnBallHitByCue(int ballId)
        {
            if (!state.isMemoryModeActive || !state.isNoirPhase) return;
            if (ballId <= 0) return; // Don't reveal cue ball

            RevealBall(ballId);
        }

        // Called when RulesManager reports a ball was potted
        public void OnBallPotted(int ballId, bool isCorrectBallForCurrentPlayer)
        {
            if (!state.isMemoryModeActive) return;

            // Reveal the potted ball regardless
            RevealBall(ballId);

            // If correct ball potted during noir phase -> reward: reveal all + restart timer
            if (isCorrectBallForCurrentPlayer && state.isNoirPhase)
            {
                OnCorrectBallPottedReward?.Invoke();
                StartRevealPhase();
            }
            else if (isCorrectBallForCurrentPlayer)
            {
                // Switch player turn
                state.currentPlayerIndex = (state.currentPlayerIndex + 1) % 2;
            }
        }

        // ==================== PHASE CONTROL ====================

        private void StartRevealPhase()
        {
            state.isRevealPhase = true;
            state.isNoirPhase = false;
            state.timerRemaining = (float)config.revealDuration;

            RevealAllBalls();
            OnRevealPhaseStarted?.Invoke();
            
            Debug.Log($"[NoirMemory] Reveal Phase Started: {state.timerRemaining}s");
        }

        private void StartNoirPhase()
        {
            state.isRevealPhase = false;
            state.isNoirPhase = true;
            revealedBalls.Clear();

            // Hide all balls as noir except cue ball (ballId = 0)
            for (int i = 1; i < GetBallCount(); i++)
            {
                SetBallNoir(i);
            }

            // Cue ball may be visible or use special material
            SetCueBallVisible();

            OnNoirPhaseStarted?.Invoke();
            Debug.Log("[NoirMemory] Noir Phase Started — Remember your balls!");
        }

        private void Update()
        {
            if (!state.isMemoryModeActive || !state.isRevealPhase) return;

            state.timerRemaining -= Time.deltaTime;
            OnTimerUpdated?.Invoke(state.timerRemaining);

            if (state.timerRemaining <= 0)
            {
                StartNoirPhase();
            }
        }

        // ==================== BALL VISUALS ====================

        private void InitializeBallVisuals()
        {
            ballVisuals.Clear();
            int ballCount = GetBallCount();

            for (int i = 0; i < ballCount; i++)
            {
                var ball = physicsManager?.GetBallById(i);
                if (ball == null) continue;

                var visuals = ball.GetComponent<NoirMemoryBallVisuals>();
                if (visuals == null)
                {
                    visuals = ball.gameObject.AddComponent<NoirMemoryBallVisuals>();
                }
                visuals.Setup(i, this);
                ballVisuals.Add(visuals);

                // Store original material
                var renderer = ball.GetComponent<Renderer>();
                if (renderer != null && !originalMaterials.ContainsKey(i))
                {
                    originalMaterials[i] = renderer.material;
                }
            }
        }

        private void RevealBall(int ballId)
        {
            if (revealedBalls.ContainsKey(ballId) && revealedBalls[ballId]) return;

            var ball = physicsManager?.GetBallById(ballId);
            if (ball == null) return;

            var renderer = ball.GetComponent<Renderer>();
            if (renderer != null && originalMaterials.ContainsKey(ballId))
            {
                renderer.material = originalMaterials[ballId];
            }

            revealedBalls[ballId] = true;
            OnBallRevealed?.Invoke(ballId);
        }

        private void RevealAllBalls()
        {
            for (int i = 0; i < GetBallCount(); i++)
            {
                RevealBall(i);
            }
        }

        private void SetBallNoir(int ballId)
        {
            var ball = physicsManager?.GetBallById(ballId);
            if (ball == null) return;

            var renderer = ball.GetComponent<Renderer>();
            if (renderer == null) return;

            // Store original if not already
            if (!originalMaterials.ContainsKey(ballId))
            {
                originalMaterials[ballId] = renderer.material;
            }

            // Use noir material (fallback to black if not assigned)
            Material mat = noirMaterial;
            if (mat == null)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                mat.SetColor("_BaseColor", Color.black);
            }
            renderer.material = mat;
        }

        private void SetCueBallVisible()
        {
            var ball = physicsManager?.GetBallById(0);
            if (ball == null) return;

            var renderer = ball.GetComponent<Renderer>();
            if (renderer == null) return;

            Material mat = cueBallNoirMaterial;
            if (mat == null)
            {
                // Use original or slightly tinted
                if (originalMaterials.ContainsKey(0))
                {
                    mat = originalMaterials[0];
                }
                else
                {
                    mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                    mat.SetColor("_BaseColor", new Color(0.9f, 0.9f, 0.95f));
                }
            }
            renderer.material = mat;
        }

        private int GetBallCount()
        {
            return physicsManager?.GetBallCount() ?? 16;
        }

        // ==================== EVENT SUBSCRIPTION ====================

        private void SubscribeEvents()
        {
            if (rulesManager != null)
            {
                rulesManager.OnBallPottedEvent += OnBallPotted;
            }
            if (shotManager != null)
            {
                
            }
        }

        private void UnsubscribeEvents()
        {
            if (rulesManager != null)
            {
                rulesManager.OnBallPottedEvent -= OnBallPotted;
            }
            if (shotManager != null)
            {
                
            }
        }

        private void OnShotCompleted(bool wasFoul)
        {
            // Switch player turn
            if (state.isMemoryModeActive)
            {
                state.currentPlayerIndex = (state.currentPlayerIndex + 1) % 2;
            }
            else
            {
                // Switch player turn
                state.currentPlayerIndex = (state.currentPlayerIndex + 1) % 2;
            }
        }
    }
}