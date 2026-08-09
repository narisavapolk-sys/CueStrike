using UnityEngine;
using System.Collections.Generic;
using System;
using CueStrike.Gameplay.SaveSystem;

namespace CueStrike.Gameplay
{
    /// <summary>
    /// Practice routine types for solo practice and training.
    /// </summary>
    public enum PracticeRoutine
    {
        FreePlacement = 0,
        LineUp = 1,
        DZoneClearance = 2,
        CushionKiss = 3,
        AroundTheBlack = 4,
        SpiralCurve = 5,
        StraightIn = 6,
        CutShots = 7,
        FollowDraw = 8,
        SideSpin = 9,
        PositionPlay = 10,
        BreakPractice = 11,
        SafetyPlay = 12,
        PatternPlay = 13,
        PressureDrills = 14,
        CustomBuilder = 15
    }

    /// <summary>
    /// Coordinates the Solo Practice and Training Mode.
    /// Manages ball layouts for 16 training routines plus free placement.
    /// Supports dynamic table swapping between Snooker, 8-Ball Pool, and 9-Ball Pool.
    /// Integrates with SaveSystem for progress tracking.
    /// </summary>
    public class CueStrikePracticeManager : MonoBehaviour
    {
        public static CueStrikePracticeManager Instance { get; private set; }

        [Header("Settings")]
        public PracticeRoutine activeRoutine = PracticeRoutine.FreePlacement;
        public int tableType = 0; // 0 = Snooker, 1 = Pool 8-Ball, 2 = Pool 9-Ball
        
        // Public properties for external access
        public PracticeRoutine ActiveRoutine => activeRoutine;
        public int TableType => tableType;

        [Header("Prefabs (Optional)")]
        [Tooltip("The ball prefab to instantiate. If null, a default sphere will be created.")]
        public GameObject ballPrefab;

        [Header("Session Tracking")]
        [SerializeField] private float routineStartTime;
        [SerializeField] private int ballsPottedThisSession = 0;
        [SerializeField] private int foulsThisSession = 0;
        [SerializeField] private List<ShotMetricData> shotMetrics = new List<ShotMetricData>();

        // Events
        public event Action<PracticeRoutine> OnRoutineChanged;
        public event Action<int> OnTableTypeChanged;
        public event Action OnRoutineCompleted;
        public event Action<int, float, bool> OnRoutineResult; // score, time, success

        private bool _isSessionActive = false;
        private string _currentRoutineId;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            // Load last selected routine and table type from save system
            _currentRoutineId = CueStrikeSaveSystemIntegration.GetLastSelectedRoutine();
            if (!string.IsNullOrEmpty(_currentRoutineId) && Enum.TryParse(_currentRoutineId, out PracticeRoutine savedRoutine))
            {
                activeRoutine = savedRoutine;
            }

            tableType = CueStrikeSaveSystemIntegration.GetLastSelectedTableType();

            ApplyRoutine(activeRoutine);
        }

        /// <summary>
        /// Swaps the active table style and re-racks balls.
        /// </summary>
        public void SwapTable(int newTableType)
        {
            tableType = Mathf.Clamp(newTableType, 0, 2);
            string tableName = tableType == 0 ? "Snooker 12ft" : (tableType == 1 ? "8-Ball Pool" : "9-Ball Pool");
            Debug.Log($"[CueStrike Practice] Swapping table style to: {tableName}");

            // Toggle GameObject visibility in scene
            var tables = FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None);
            foreach (var table in tables)
            {
                string name = table.gameObject.name.ToLower();
                bool isSnooker = name.Contains("snooker") || name.Contains("12ft");
                bool isPool = name.Contains("pool") || name.Contains("8ball") || name.Contains("8ft") || name.Contains("9ft") || name.Contains("9ball");
                
                if (isSnooker)
                {
                    table.gameObject.SetActive(tableType == 0);
                }
                else if (isPool)
                {
                    table.gameObject.SetActive(tableType != 0);
                }
            }

            // Save table type
            CueStrikeSaveSystemIntegration.SetLastSelectedTableType(tableType);

            // Re-apply the active routine to fit the new table dimensions
            ApplyRoutine(activeRoutine);
            OnTableTypeChanged?.Invoke(tableType);
        }

        /// <summary>
        /// Clear active balls in the scene and set up the selected routine.
        /// </summary>
        public void ApplyRoutine(PracticeRoutine routine)
        {
            activeRoutine = routine;
            ClearCurrentBalls();

            float ballRadius = GetBallRadius();
            float tableY = 0.85f; // Standard table play surface height

            Vector3 cueBallPos = new Vector3(0f, tableY + ballRadius, -1.0f);
            List<Vector3> targetBallPositions = new List<Vector3>();
            List<int> targetBallIds = new List<int>(); // For color assignment

            _currentRoutineId = routine.ToString().ToLower();
            CueStrikeSaveSystemIntegration.SetLastSelectedRoutine(_currentRoutineId);

            switch (routine)
            {
                case PracticeRoutine.FreePlacement:
                    cueBallPos = new Vector3(0f, tableY + ballRadius, -1.0f);
                    for (int i = 0; i < 5; i++)
                    {
                        targetBallPositions.Add(new Vector3(-0.4f + i * 0.2f, tableY + ballRadius, 0.5f));
                        targetBallIds.Add(i + 1);
                    }
                    break;

                case PracticeRoutine.LineUp:
                    cueBallPos = new Vector3(0f, tableY + ballRadius, -1.5f);
                    int redCount = tableType == 0 ? 15 : 10;
                    for (int i = 0; i < redCount; i++)
                    {
                        targetBallPositions.Add(new Vector3(0f, tableY + ballRadius, -0.8f + (i * (ballRadius * 2.2f))));
                        targetBallIds.Add(i + 1);
                    }
                    break;

                case PracticeRoutine.DZoneClearance:
                    cueBallPos = new Vector3(0f, tableY + ballRadius, -0.5f);
                    float dRadius = 0.29f;
                    float centerZ = -0.89f;
                    int colorCount = 6;
                    for (int i = 0; i < colorCount; i++)
                    {
                        float angle = Mathf.PI + (i * Mathf.PI / (colorCount - 1));
                        targetBallPositions.Add(new Vector3(Mathf.Cos(angle) * dRadius, tableY + ballRadius, centerZ + Mathf.Sin(angle) * dRadius));
                        targetBallIds.Add(16 + i); // Color balls
                    }
                    break;

                case PracticeRoutine.CushionKiss:
                    cueBallPos = new Vector3(0f, tableY + ballRadius, 0f);
                    float railX = tableType == 0 ? 0.84f : 0.60f;
                    for (int i = 0; i < 6; i++)
                    {
                        float zPos = -1.2f + (i * 0.5f);
                        targetBallPositions.Add(new Vector3(railX - ballRadius, tableY + ballRadius, zPos));
                        targetBallIds.Add(i * 2 + 1);
                        targetBallPositions.Add(new Vector3(-railX + ballRadius, tableY + ballRadius, zPos));
                        targetBallIds.Add(i * 2 + 2);
                    }
                    break;

                case PracticeRoutine.AroundTheBlack:
                    float blackSpotZ = tableType == 0 ? 1.42f : 1.0f;
                    cueBallPos = new Vector3(0f, tableY + ballRadius, -0.2f);
                    int clusterCount = 8;
                    float clusterRadius = ballRadius * 3.5f;
                    for (int i = 0; i < clusterCount; i++)
                    {
                        float angle = i * Mathf.PI * 2f / clusterCount;
                        targetBallPositions.Add(new Vector3(Mathf.Cos(angle) * clusterRadius, tableY + ballRadius, blackSpotZ + Mathf.Sin(angle) * clusterRadius));
                        targetBallIds.Add(i + 1);
                    }
                    targetBallPositions.Add(new Vector3(0f, tableY + ballRadius, blackSpotZ));
                    targetBallIds.Add(tableType == 0 ? 21 : 8); // Black or 8-ball
                    break;

                case PracticeRoutine.SpiralCurve:
                    cueBallPos = new Vector3(0f, tableY + ballRadius, -1.2f);
                    int spiralCount = 12;
                    for (int i = 0; i < spiralCount; i++)
                    {
                        float theta = i * 0.6f;
                        float r = 0.1f + (i * 0.06f);
                        targetBallPositions.Add(new Vector3(Mathf.Cos(theta) * r, tableY + ballRadius, -0.4f + Mathf.Sin(theta) * r * 1.5f));
                        targetBallIds.Add(i + 1);
                    }
                    break;

                // Phase 4 New Routines
                case PracticeRoutine.StraightIn:
                    SetupStraightInRoutine(ref cueBallPos, ref targetBallPositions, ref targetBallIds, ballRadius, tableY);
                    break;
                case PracticeRoutine.CutShots:
                    SetupCutShotsRoutine(ref cueBallPos, ref targetBallPositions, ref targetBallIds, ballRadius, tableY);
                    break;
                case PracticeRoutine.FollowDraw:
                    SetupFollowDrawRoutine(ref cueBallPos, ref targetBallPositions, ref targetBallIds, ballRadius, tableY);
                    break;
                case PracticeRoutine.SideSpin:
                    SetupSideSpinRoutine(ref cueBallPos, ref targetBallPositions, ref targetBallIds, ballRadius, tableY);
                    break;
                case PracticeRoutine.PositionPlay:
                    SetupPositionPlayRoutine(ref cueBallPos, ref targetBallPositions, ref targetBallIds, ballRadius, tableY);
                    break;
                case PracticeRoutine.BreakPractice:
                    SetupBreakPracticeRoutine(ref cueBallPos, ref targetBallPositions, ref targetBallIds, ballRadius, tableY);
                    break;
                case PracticeRoutine.SafetyPlay:
                    SetupSafetyPlayRoutine(ref cueBallPos, ref targetBallPositions, ref targetBallIds, ballRadius, tableY);
                    break;
                case PracticeRoutine.PatternPlay:
                    SetupPatternPlayRoutine(ref cueBallPos, ref targetBallPositions, ref targetBallIds, ballRadius, tableY);
                    break;
                case PracticeRoutine.PressureDrills:
                    SetupPressureDrillsRoutine(ref cueBallPos, ref targetBallPositions, ref targetBallIds, ballRadius, tableY);
                    break;
                case PracticeRoutine.CustomBuilder:
                    LoadCustomDrill();
                    return; // Custom builder loads from save data
            }

            // Spawn Cue Ball
            SpawnBall(cueBallPos, 0, "Cue Ball", true);

            // Spawn Target Balls
            for (int k = 0; k < targetBallPositions.Count; k++)
            {
                int ballId = k < targetBallIds.Count ? targetBallIds[k] : k + 1;
                string ballName = GetBallName(ballId);
                SpawnBall(targetBallPositions[k], ballId, ballName, false);
            }

            StartSession();
            Debug.Log($"[CueStrike Practice] Loaded Routine: {routine} with {targetBallPositions.Count} target balls.");
            OnRoutineChanged?.Invoke(routine);
        }

        private void SetupStraightInRoutine(ref Vector3 cueBallPos, ref List<Vector3> targetBallPositions, ref List<int> targetBallIds, float ballRadius, float tableY)
        {
            // Straight-in shots at various distances
            cueBallPos = new Vector3(0f, tableY + ballRadius, -1.5f);
            
            float[] distances = { 0.3f, 0.5f, 0.8f, 1.2f, 1.5f };
            for (int i = 0; i < distances.Length; i++)
            {
                targetBallPositions.Add(new Vector3(0f, tableY + ballRadius, distances[i]));
                targetBallIds.Add(i + 1);
            }
        }

        private void SetupCutShotsRoutine(ref Vector3 cueBallPos, ref List<Vector3> targetBallPositions, ref List<int> targetBallIds, float ballRadius, float tableY)
        {
            // Cut shots at various angles
            cueBallPos = new Vector3(-0.5f, tableY + ballRadius, -1.0f);
            
            float[] angles = { 15f, 30f, 45f, 60f, 75f };
            float distance = 0.8f;
            for (int i = 0; i < angles.Length; i++)
            {
                float rad = angles[i] * Mathf.Deg2Rad;
                targetBallPositions.Add(new Vector3(Mathf.Sin(rad) * distance, tableY + ballRadius, Mathf.Cos(rad) * distance));
                targetBallIds.Add(i + 1);
            }
        }

        private void SetupFollowDrawRoutine(ref Vector3 cueBallPos, ref List<Vector3> targetBallPositions, ref List<int> targetBallIds, float ballRadius, float tableY)
        {
            // Follow and draw shots
            cueBallPos = new Vector3(0f, tableY + ballRadius, -1.0f);
            
            // Target ball positions
            for (int i = 0; i < 5; i++)
            {
                float z = 0.2f + i * 0.3f;
                targetBallPositions.Add(new Vector3(0f, tableY + ballRadius, z));
                targetBallIds.Add(i + 1);
            }
        }

        private void SetupSideSpinRoutine(ref Vector3 cueBallPos, ref List<Vector3> targetBallPositions, ref List<int> targetBallIds, float ballRadius, float tableY)
        {
            // Side spin shots
            cueBallPos = new Vector3(-0.3f, tableY + ballRadius, -0.8f);
            
            for (int i = 0; i < 6; i++)
            {
                float x = -0.6f + i * 0.25f;
                float z = 0.5f;
                targetBallPositions.Add(new Vector3(x, tableY + ballRadius, z));
                targetBallIds.Add(i + 1);
            }
        }

        private void SetupPositionPlayRoutine(ref Vector3 cueBallPos, ref List<Vector3> targetBallPositions, ref List<int> targetBallIds, float ballRadius, float tableY)
        {
            // Position play - multiple balls for pattern
            cueBallPos = new Vector3(0f, tableY + ballRadius, -1.2f);
            
            // Create a pattern of 6 balls
            Vector3[] positions = new Vector3[]
            {
                new Vector3(-0.3f, tableY + ballRadius, 0.2f),
                new Vector3(0.4f, tableY + ballRadius, 0.5f),
                new Vector3(-0.2f, tableY + ballRadius, 0.9f),
                new Vector3(0.5f, tableY + ballRadius, 1.2f),
                new Vector3(-0.4f, tableY + ballRadius, 1.5f),
                new Vector3(0f, tableY + ballRadius, 1.8f)
            };
            
            foreach (var pos in positions)
            {
                targetBallPositions.Add(pos);
                targetBallIds.Add(targetBallIds.Count + 1);
            }
        }

        private void SetupBreakPracticeRoutine(ref Vector3 cueBallPos, ref List<Vector3> targetBallPositions, ref List<int> targetBallIds, float ballRadius, float tableY)
        {
            // Break practice - full rack
            cueBallPos = new Vector3(0f, tableY + ballRadius, -1.8f);
            
            int ballCount = tableType == 0 ? 15 : (tableType == 1 ? 15 : 9);
            float rackStartZ = tableType == 0 ? 0.8f : 0.5f;
            
            // Triangle rack formation
            int row = 0;
            int count = 0;
            for (int i = 0; i < ballCount; i++)
            {
                if (count >= row + 1)
                {
                    row++;
                    count = 0;
                }
                
                float x = (count - row * 0.5f) * ballRadius * 2.1f;
                float z = rackStartZ + row * ballRadius * 2.1f;
                
                targetBallPositions.Add(new Vector3(x, tableY + ballRadius, z));
                targetBallIds.Add(i + 1);
                count++;
            }
        }

        private void SetupSafetyPlayRoutine(ref Vector3 cueBallPos, ref List<Vector3> targetBallPositions, ref List<int> targetBallIds, float ballRadius, float tableY)
        {
            // Safety play - balls near cushions
            cueBallPos = new Vector3(0f, tableY + ballRadius, 0f);
            float railX = tableType == 0 ? 0.84f : 0.60f;
            
            for (int i = 0; i < 8; i++)
            {
                float z = -1.0f + i * 0.3f;
                targetBallPositions.Add(new Vector3(railX - ballRadius * 2, tableY + ballRadius, z));
                targetBallIds.Add(i + 1);
                targetBallPositions.Add(new Vector3(-railX + ballRadius * 2, tableY + ballRadius, z));
                targetBallIds.Add(i + 9);
            }
        }

        private void SetupPatternPlayRoutine(ref Vector3 cueBallPos, ref List<Vector3> targetBallPositions, ref List<int> targetBallIds, float ballRadius, float tableY)
        {
            // Pattern play - clear all balls in order
            cueBallPos = new Vector3(0f, tableY + ballRadius, -1.0f);
            
            int ballCount = tableType == 0 ? 10 : 7;
            for (int i = 0; i < ballCount; i++)
            {
                float angle = i * Mathf.PI * 2f / ballCount;
                float radius = 0.3f + (i % 3) * 0.2f;
                targetBallPositions.Add(new Vector3(Mathf.Cos(angle) * radius, tableY + ballRadius, Mathf.Sin(angle) * radius));
                targetBallIds.Add(i + 1);
            }
        }

        private void SetupPressureDrillsRoutine(ref Vector3 cueBallPos, ref List<Vector3> targetBallPositions, ref List<int> targetBallIds, float ballRadius, float tableY)
        {
            // Pressure drills - time-sensitive setups
            cueBallPos = new Vector3(0f, tableY + ballRadius, -1.0f);
            
            // 5 balls in a line - must clear quickly
            for (int i = 0; i < 5; i++)
            {
                targetBallPositions.Add(new Vector3(0f, tableY + ballRadius, 0.3f + i * 0.4f));
                targetBallIds.Add(i + 1);
            }
        }

        private void LoadCustomDrill()
        {
            // Load from custom drill save data
            string lastRoutine = CueStrikeSaveSystemIntegration.GetLastSelectedRoutine();
            if (lastRoutine.StartsWith("custom_"))
            {
                string drillId = lastRoutine.Substring(7);
                var drill = CueStrikeSaveSystemIntegration.GetCustomDrill(drillId);
                if (drill != null)
                {
                    ApplyCustomDrill(drill);
                    return;
                }
            }
            
            // Fallback to free placement
            activeRoutine = PracticeRoutine.FreePlacement;
            ApplyRoutine(PracticeRoutine.FreePlacement);
        }

        private void ApplyCustomDrill(CustomDrillData drill)
        {
            ClearCurrentBalls();
            float ballRadius = GetBallRadius();
            float tableY = 0.85f;

            foreach (var ballData in drill.ballPositions)
            {
                if (ballData.isActive && !ballData.isPocketed)
                {
                    Vector3 pos = ballData.position.ToVector3();
                    if (ballData.ballId == 0)
                    {
                        SpawnBall(pos, 0, "Cue Ball", true);
                    }
                    else
                    {
                        SpawnBall(pos, ballData.ballId, ballData.ballName, false);
                    }
                }
            }

            StartSession();
        }

        private void SpawnBall(Vector3 position, int ballId, string ballName, bool isCueBall)
        {
            GameObject ballObj;
            if (ballPrefab != null)
            {
                ballObj = Instantiate(ballPrefab, position, Quaternion.identity);
            }
            else
            {
                float radius = GetBallRadius();
                ballObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                ballObj.transform.position = position;
                ballObj.transform.localScale = Vector3.one * (radius * 2f);
                
                var rb = ballObj.AddComponent<Rigidbody>();
                rb.mass = 0.17f;
                rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

                var collider = ballObj.GetComponent<SphereCollider>();
                if (collider != null)
                {
                    collider.material = new PhysicsMaterial
                    {
                        bounciness = 0.85f,
                        frictionCombine = PhysicsMaterialCombine.Minimum,
                        bounceCombine = PhysicsMaterialCombine.Maximum
                    };
                }
            }

            ballObj.tag = "Ball";
            ballObj.name = isCueBall ? "CueBall" : $"TargetBall_{ballId}";

            // Add ball identifier component
            var ballComponent = ballObj.AddComponent<CueStrikeBall>();
            ballComponent.BallId = ballId;
            ballComponent.BallName = ballName;
            ballComponent.Type = isCueBall ? CueStrikeBall.BallType.CueBall : CueStrikeBall.BallType.ObjectBall;

            var rend = ballObj.GetComponent<Renderer>();
            if (rend != null)
            {
                if (isCueBall)
                {
                    rend.material.color = Color.white;
                }
                else
                {
                    rend.material.color = GetBallColor(ballId);
                }
            }
        }

        private float GetBallRadius()
        {
            return tableType == 0 ? 0.02625f : 0.028575f;
        }

        private Color GetBallColor(int ballId)
        {
            if (tableType == 0) // Snooker
            {
                if (ballId <= 15) return Color.red; // Reds
                switch (ballId)
                {
                    case 16: return Color.yellow; // Yellow
                    case 17: return Color.green; // Green
                    case 18: return new Color(0.6f, 0.2f, 0.8f); // Brown
                    case 19: return Color.blue; // Blue
                    case 20: return new Color(0.6f, 0.2f, 0.8f); // Pink
                    case 21: return Color.black; // Black
                }
            }
            else if (tableType == 1) // 8-Ball Pool
            {
                if (ballId == 8) return Color.black;
                if (ballId <= 7) return new Color(0.8f, 0.6f, 0.2f); // Solids
                return new Color(0.9f, 0.9f, 0.9f); // Stripes base
            }
            else // 9-Ball Pool
            {
                if (ballId == 9) return new Color(0.8f, 0.6f, 0.2f);
                return new Color(0.9f, 0.9f, 0.9f);
            }
            return Color.white;
        }

        private string GetBallName(int ballId)
        {
            if (tableType == 0) // Snooker
            {
                if (ballId <= 15) return $"Red {ballId}";
                string[] colorNames = { "Yellow", "Green", "Brown", "Blue", "Pink", "Black" };
                if (ballId >= 16 && ballId <= 21) return colorNames[ballId - 16];
            }
            else if (tableType == 1) // 8-Ball
            {
                if (ballId == 8) return "8-Ball";
                if (ballId <= 7) return $"Solid {ballId}";
                return $"Stripe {ballId - 8}";
            }
            else // 9-Ball
            {
                return $"Ball {ballId}";
            }
            return $"Ball {ballId}";
        }

        private void ClearCurrentBalls()
        {
            var balls = GameObject.FindGameObjectsWithTag("Ball");
            foreach (var ball in balls)
            {
                Destroy(ball);
            }
        }

        private void StartSession()
        {
            _isSessionActive = true;
            routineStartTime = Time.time;
            ballsPottedThisSession = 0;
            foulsThisSession = 0;
            shotMetrics.Clear();
        }

        /// <summary>
        /// Call when a ball is potted during practice.
        /// </summary>
        public void OnBallPotted(int ballId, string ballName)
        {
            if (!_isSessionActive) return;
            ballsPottedThisSession++;
            
            // Record shot metric
            shotMetrics.Add(new ShotMetricData
            {
                timestamp = Time.time - routineStartTime,
                eventType = "potted",
                ballId = ballId,
                ballName = ballName
            });
        }

        /// <summary>
        /// Call when a foul occurs during practice.
        /// </summary>
        public void OnFoul()
        {
            if (!_isSessionActive) return;
            foulsThisSession++;
            
            shotMetrics.Add(new ShotMetricData
            {
                timestamp = Time.time - routineStartTime,
                eventType = "foul"
            });
        }

        /// <summary>
        /// Call when a shot is taken (for accuracy tracking).
        /// </summary>
        public void OnShotTaken(float accuracy, float power, float spin)
        {
            if (!_isSessionActive) return;
            
            shotMetrics.Add(new ShotMetricData
            {
                timestamp = Time.time - routineStartTime,
                eventType = "shot",
                accuracy = accuracy,
                power = power,
                spin = spin
            });
        }

        /// <summary>
        /// Complete the current routine and save progress.
        /// </summary>
        public void CompleteRoutine(bool success, int score = 0)
        {
            if (!_isSessionActive) return;
            
            float timeSeconds = Time.time - routineStartTime;
            _isSessionActive = false;

            // Calculate accuracy
            float accuracy = 0f;
            var shots = shotMetrics.FindAll(m => m.eventType == "shot");
            if (shots.Count > 0)
            {
                float totalAccuracy = 0f;
                foreach (var shot in shots) totalAccuracy += shot.accuracy;
                accuracy = totalAccuracy / shots.Count;
            }

            // Record to save system
            int difficulty = CueStrikeSaveSystemIntegration.GetRoutineDifficulty(_currentRoutineId);
            var metricsDict = new Dictionary<string, float>
            {
                { "avgAccuracy", accuracy },
                { "totalShots", shots.Count },
                { "ballsPotted", ballsPottedThisSession },
                { "fouls", foulsThisSession }
            };

            CueStrikeSaveSystemIntegration.RecordPracticeRoutineComplete(
                _currentRoutineId, score, timeSeconds, success,
                ballsPottedThisSession, foulsThisSession, accuracy, difficulty, metricsDict);

            OnRoutineResult?.Invoke(score, timeSeconds, success);
            OnRoutineCompleted?.Invoke();
        }

        /// <summary>
        /// Get current routine ID.
        /// </summary>
        public string GetCurrentRoutineId() => _currentRoutineId;

        /// <summary>
        /// Get session time elapsed.
        /// </summary>
        public float GetSessionTime() => _isSessionActive ? Time.time - routineStartTime : 0f;

        /// <summary>
        /// Get balls potted this session.
        /// </summary>
        public int GetBallsPotted() => ballsPottedThisSession;

        /// <summary>
        /// Get fouls this session.
        /// </summary>
        public int GetFouls() => foulsThisSession;
    }

    /// <summary>
    /// Shot metric data for detailed tracking.
    /// </summary>
    [Serializable]
    public class ShotMetricData
    {
        public float timestamp;
        public string eventType; // "shot", "potted", "foul"
        public int ballId;
        public string ballName;
        public float accuracy;
        public float power;
        public float spin;
    }

    /// <summary>
    /// Helper component to attach to balls for identification.
    /// </summary>
    public class CueStrikeBall : MonoBehaviour
    {
        public int BallId;
        public string BallName;
        public BallType Type = BallType.ObjectBall;

        public enum BallType
        {
            CueBall = 0,
            ObjectBall = 1,
            ColorBall = 2
        }
    }
}
