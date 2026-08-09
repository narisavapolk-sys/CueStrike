using UnityEngine;
using System.Collections.Generic;

namespace CueStrike.Gameplay.ChinesePool
{
    /// <summary>
    /// Stub component for Chinese 8-Ball ball spawning and rack setup.
    /// Attach to an empty GameObject in the scene and assign ball prefabs.
    /// This is a stub implementation — replace with full physics spawning logic.
    /// </summary>
    public class ChinesePoolBallSetup : MonoBehaviour
    {
        #region Inspector References
        [Header("Ball Prefabs (Assign in Inspector)")]
        [Tooltip("Prefab for the cue ball (white).")]
        public GameObject cueBallPrefab;

        [Tooltip("Prefab for red balls (1-7).")]
        public GameObject redBallPrefab;

        [Tooltip("Prefab for yellow balls (9-15).")]
        public GameObject yellowBallPrefab;

        [Tooltip("Prefab for the 8-ball (black).")]
        public GameObject blackBallPrefab;

        [Header("Spawn Settings")]
        [Tooltip("Parent transform for spawned balls. Auto-created if null.")]
        public Transform ballsParent;

        [Tooltip("Rack center position on table (world space).")]
        public Vector3 rackCenter = new Vector3(0f, 0.03f, 0.6f); // Foot spot area

        [Tooltip("Ball diameter in meters (standard 57.15mm).")]
        public float ballDiameter = 0.05715f;

        [Tooltip("Gap between balls in rack (meters).")]
        public float rackGap = 0.001f;
        #endregion

        #region Internal State
        private readonly List<GameObject> spawnedBalls = new List<GameObject>();
        private readonly Dictionary<int, GameObject> ballIdToObject = new Dictionary<int, GameObject>();
        #endregion

        #region Unity Lifecycle
        void Awake()
        {
            if (ballsParent == null)
            {
                ballsParent = new GameObject("ChinesePool_Balls").transform;
                ballsParent.SetParent(transform);
            }
        }

        void OnDestroy()
        {
            ClearRack();
        }
        #endregion

        #region Public API

        /// <summary>
        /// Sets up a full Chinese 8-Ball rack (15 object balls + cue ball).
        /// Call this at the start of each frame.
        /// </summary>
        public void SetupRack()
        {
            ClearRack();
            SpawnRack();
            PositionCueBall();
            Debug.Log("[CueStrike] ChinesePoolBallSetup: Rack created (stub).");
        }

        /// <summary>
        /// Clears all spawned balls from the scene.
        /// </summary>
        public void ClearRack()
        {
            foreach (var ball in spawnedBalls)
            {
                if (ball != null)
                    Destroy(ball);
            }
            spawnedBalls.Clear();
            ballIdToObject.Clear();
        }

        /// <summary>
        /// Gets the GameObject for a specific ball ID.
        /// Ball IDs: 0 = cue ball, 1-7 = red, 8 = black (8-ball), 9-15 = yellow.
        /// </summary>
        public GameObject GetBallById(int ballId)
        {
            if (ballIdToObject.TryGetValue(ballId, out var ball))
                return ball;
            return null;
        }

        /// <summary>
        /// Returns all ball IDs currently on the table.
        /// </summary>
        public int[] GetBallsOnTable()
        {
            return new List<int>(ballIdToObject.Keys).ToArray();
        }

        /// <summary>
        /// Removes a ball from the table (e.g., when potted).
        /// </summary>
        public void RemoveBall(int ballId)
        {
            if (ballIdToObject.TryGetValue(ballId, out var ball))
            {
                if (ball != null)
                    Destroy(ball);
                ballIdToObject.Remove(ballId);
                spawnedBalls.Remove(ball);
            }
        }

        /// <summary>
        /// Respawns a specific ball at a given position (e.g., after foul).
        /// </summary>
        public void RespawnBall(int ballId, Vector3 position)
        {
            RemoveBall(ballId);
            var ball = SpawnSingleBall(ballId, position);
            if (ball != null)
            {
                ballIdToObject[ballId] = ball;
                spawnedBalls.Add(ball);
            }
        }
        #endregion

        #region Private Spawning Logic

        void SpawnRack()
        {
            // Standard Chinese 8-Ball triangle rack positions
            // Apex ball on foot spot, 8-ball in center, reds/yellows alternating
            float radius = ballDiameter / 2f;
            float spacing = ballDiameter + rackGap;

            // Row 1: Apex (ball 1 - Red)
            SpawnBallInRack(1, 0, 0, spacing, radius);

            // Row 2: 2 balls (ball 2 Red, ball 3 Yellow)
            SpawnBallInRack(2, -1, 1, spacing, radius);
            SpawnBallInRack(3, 1, 1, spacing, radius);

            // Row 3: 3 balls (ball 4 Red, ball 5 Yellow, ball 6 Red)
            SpawnBallInRack(4, -2, 2, spacing, radius);
            SpawnBallInRack(5, 0, 2, spacing, radius); // Center of row 3
            SpawnBallInRack(6, 2, 2, spacing, radius);

            // Row 4: 4 balls (ball 7 Red, ball 8 Black, ball 9 Yellow, ball 10 Red)
            SpawnBallInRack(7, -3, 3, spacing, radius);
            SpawnBallInRack(8, -1, 3, spacing, radius); // 8-ball
            SpawnBallInRack(9, 1, 3, spacing, radius);
            SpawnBallInRack(10, 3, 3, spacing, radius);

            // Row 5: 5 balls (ball 11 Yellow, ball 12 Red, ball 13 Yellow, ball 14 Red, ball 15 Yellow)
            SpawnBallInRack(11, -4, 4, spacing, radius);
            SpawnBallInRack(12, -2, 4, spacing, radius);
            SpawnBallInRack(13, 0, 4, spacing, radius);
            SpawnBallInRack(14, 2, 4, spacing, radius);
            SpawnBallInRack(15, 4, 4, spacing, radius);

            Debug.Log($"[CueStrike] ChinesePoolBallSetup: Spawned {spawnedBalls.Count} object balls in rack.");
        }

        void SpawnBallInRack(int ballId, int col, int row, float spacing, float radius)
        {
            // Convert column/row to world offset
            // Triangle formation: each row offset by half spacing
            float xOffset = col * spacing * 0.5f;
            float zOffset = row * spacing * 0.866f; // sqrt(3)/2 for equilateral triangle spacing

            Vector3 position = rackCenter + new Vector3(xOffset, radius, zOffset);

            GameObject ball = SpawnSingleBall(ballId, position);
            if (ball != null)
            {
                ballIdToObject[ballId] = ball;
                spawnedBalls.Add(ball);
            }
        }

        GameObject SpawnSingleBall(int ballId, Vector3 position)
        {
            GameObject prefab = GetPrefabForBall(ballId);
            if (prefab == null)
            {
                Debug.LogWarning($"[CueStrike] ChinesePoolBallSetup: No prefab assigned for ball ID {ballId}. Using primitive sphere.");
                return CreatePrimitiveBall(ballId, position);
            }

            GameObject ball = Instantiate(prefab, position, Quaternion.identity, ballsParent);
            ball.name = $"Ball_{ballId}";
            SetupBallComponents(ball, ballId);
            return ball;
        }

        GameObject GetPrefabForBall(int ballId)
        {
            if (ballId == 0) return cueBallPrefab;
            if (ballId >= 1 && ballId <= 7) return redBallPrefab;
            if (ballId == 8) return blackBallPrefab;
            if (ballId >= 9 && ballId <= 15) return yellowBallPrefab;
            return null;
        }

        GameObject CreatePrimitiveBall(int ballId, Vector3 position)
        {
            GameObject ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ball.transform.position = position;
            ball.transform.localScale = Vector3.one * ballDiameter;
            ball.transform.SetParent(ballsParent);
            ball.name = $"Ball_{ballId}";

            // Color by type
            var renderer = ball.GetComponent<Renderer>();
            if (renderer != null)
            {
                if (ballId == 0) renderer.material.color = Color.white;
                else if (ballId >= 1 && ballId <= 7) renderer.material.color = Color.red;
                else if (ballId == 8) renderer.material.color = Color.black;
                else renderer.material.color = Color.yellow;
            }

            SetupBallComponents(ball, ballId);
            return ball;
        }

        void SetupBallComponents(GameObject ball, int ballId)
        {
            // Add Rigidbody if missing
            var rb = ball.GetComponent<Rigidbody>();
            if (rb == null)
                rb = ball.AddComponent<Rigidbody>();

            rb.mass = 0.17f; // Standard billiard ball mass ~170g
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.interpolation = RigidbodyInterpolation.Interpolate;

            // Add SphereCollider if missing
            var col = ball.GetComponent<SphereCollider>();
            if (col == null)
                col = ball.AddComponent<SphereCollider>();

            col.radius = 0.5f; // Local radius (scaled by transform)

            // Add ball identifier component
            var identifier = ball.GetComponent<ChinesePoolBallIdentifier>();
            if (identifier == null)
                identifier = ball.AddComponent<ChinesePoolBallIdentifier>();
            identifier.ballId = ballId;
        }

        void PositionCueBall()
        {
            // Cue ball starts in "D" area (baulk) — standard position for Chinese 8-ball
            Vector3 cueBallPos = new Vector3(0f, ballDiameter / 2f, -1.2f); // Behind baulk line
            GameObject cueBall = SpawnSingleBall(0, cueBallPos);
            if (cueBall != null)
            {
                ballIdToObject[0] = cueBall;
                spawnedBalls.Add(cueBall);
            }
        }
        #endregion

        #region Self-Test
#if UNITY_EDITOR
        [UnityEditor.MenuItem("Tools/CueStrike/Debug/Test ChinesePool BallSetup")]
        public static void SelfTest()
        {
            bool pass = true;

            var setup = FindFirstObjectByType<ChinesePoolBallSetup>();
            if (setup == null)
            {
                Debug.LogError("❌ FAIL: ChinesePoolBallSetup not found in scene!");
                pass = false;
            }
            else
            {
                if (setup.cueBallPrefab == null)
                    Debug.LogWarning("⚠️ Cue ball prefab not assigned.");
                if (setup.redBallPrefab == null)
                    Debug.LogWarning("⚠️ Red ball prefab not assigned.");
                if (setup.yellowBallPrefab == null)
                    Debug.LogWarning("⚠️ Yellow ball prefab not assigned.");
                if (setup.blackBallPrefab == null)
                    Debug.LogWarning("⚠️ Black ball (8-ball) prefab not assigned.");

                setup.SetupRack();

                if (setup.GetBallById(0) == null)
                {
                    Debug.LogError("❌ FAIL: Cue ball not spawned.");
                    pass = false;
                }
                if (setup.GetBallById(8) == null)
                {
                    Debug.LogError("❌ FAIL: 8-ball not spawned.");
                    pass = false;
                }
                if (setup.GetBallsOnTable().Length != 16)
                {
                    Debug.LogError($"❌ FAIL: Expected 16 balls, got {setup.GetBallsOnTable().Length}.");
                    pass = false;
                }
            }

            if (pass) Debug.Log("✅ ChinesePoolBallSetup SELF-TEST PASSED — Ready for human verify.");
            else Debug.LogWarning("⚠️ ChinesePoolBallSetup SELF-TEST FAILED — Fix missing prefabs/references before proceeding.");
        }
#endif
        #endregion
    }

    /// <summary>
    /// Lightweight component to identify ball ID on physics objects.
    /// </summary>
    public class ChinesePoolBallIdentifier : MonoBehaviour
    {
        public int ballId = -1;
    }
}