using System;
using System.Collections.Generic;
using UnityEngine;
using CueStrike;

public class CueStrikePhysicsManager : MonoBehaviour
{
    public static CueStrikePhysicsManager Instance { get; private set; }

    public PhysicsMaterial ballMaterial;
    public PhysicsMaterial tableFeltMaterial;
    public PhysicsMaterial cushionMaterial;

    public GameObject ballPrefab;
    public GameObject tablePrefab; // optional reference for table-spawned racks

    private readonly List<GameObject> balls = new List<GameObject>();

    // Ghost Replay integration
    public event Action OnBallsSettled;
    private bool wasSettled = false;

    void Awake()
    {
        if (Instance != null && Instance != this) Destroy(this);
        Instance = this;
    }

    void Update()
    {
        if (AreBallsSettled() && !wasSettled)
        {
            wasSettled = true;
            OnBallsSettled?.Invoke();
        }
        else if (!AreBallsSettled())
        {
            wasSettled = false;
        }
    }

    public GameObject SpawnBall(Vector3 position, int id = 0)
    {
        if (ballPrefab == null)
        {
            Debug.LogError("Ball prefab not set in CueStrikePhysicsManager");
            return null;
        }

        var go = Instantiate(ballPrefab, position, Quaternion.identity);
        go.name = "CueStrikeBall_" + id;
        var sphere = go.GetComponent<SphereCollider>();
        if (sphere != null && ballMaterial != null) sphere.material = ballMaterial;

        var phys = go.GetComponent<CueStrikeBallPhysics>();
        if (phys == null) phys = go.AddComponent<CueStrikeBallPhysics>();

        var rb = go.GetComponent<Rigidbody>();
        if (rb != null) rb.mass = 0.17f; // default ball mass (kg)

        go.tag = "Ball";

        var idHolder = go.GetComponent<BallIdentity>();
        if (idHolder == null) idHolder = go.AddComponent<BallIdentity>();
        idHolder.ballId = id;

        // Dynamic AAA Ball Material setup
        var rend = go.GetComponent<Renderer>();
        if (rend != null)
        {
            Material ballMat = null;
            bool isSnooker = (tablePrefab != null && tablePrefab.name.ToLower().Contains("snooker")) || 
                             (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name.ToLower().Contains("snooker"));
            
            if (!isSnooker)
            {
                // Pool mode: Load numbered ball material (0 to 15)
                int clampedId = Mathf.Clamp(id, 0, 15);
                ballMat = Resources.Load<Material>("CueStrike/BallMaterials/Ball_" + clampedId);
            }
            else
            {
                // Snooker mode: Load corresponding solid color materials
                if (id == 0) ballMat = Resources.Load<Material>("CueStrike/BallMaterials/Ball_0"); // cue ball
                else if (id == 1) ballMat = Resources.Load<Material>("CueStrike/BallMaterials/Ball_1"); // yellow
                else if (id == 2) ballMat = Resources.Load<Material>("CueStrike/BallMaterials/Ball_6"); // green
                else if (id == 3) ballMat = Resources.Load<Material>("CueStrike/BallMaterials/Ball_7"); // brown
                else if (id == 4) ballMat = Resources.Load<Material>("CueStrike/BallMaterials/Ball_Blue"); // blue
                else if (id == 5) ballMat = Resources.Load<Material>("CueStrike/BallMaterials/Ball_Pink"); // pink
                else if (id == 6) ballMat = Resources.Load<Material>("CueStrike/BallMaterials/Ball_Black"); // black
                else ballMat = Resources.Load<Material>("CueStrike/BallMaterials/Ball_Red"); // red balls
            }

            if (ballMat != null)
            {
                rend.sharedMaterial = ballMat;
            }
        }

        balls.Add(go);
        return go;
    }

    public void ResetBalls()
    {
        foreach (var b in balls) Destroy(b);
        balls.Clear();
    }

    // Runtime helpers used by AI and tools
    public System.Collections.Generic.List<Transform> GetAllBalls()
    {
        var list = new System.Collections.Generic.List<Transform>();
        var gos = GameObject.FindGameObjectsWithTag("Ball");
        foreach (var g in gos) list.Add(g.transform);
        return list;
    }

    public Transform GetNearestPocket(Vector3 position)
    {
        var pockets = GameObject.FindGameObjectsWithTag("Pocket");
        if (pockets == null || pockets.Length == 0) return null;
        Transform best = null;
        float bestDist = float.MaxValue;
        foreach (var p in pockets)
        {
            float d = Vector3.Distance(position, p.transform.position);
            if (d < bestDist)
            {
                bestDist = d;
                best = p.transform;
            }
        }
        return best;
    }

    // Check if all balls have settled (for Ghost Replay integration)
    public bool AreBallsSettled(float settleThreshold = 0.05f, float settleTime = 0.6f)
    {
        bool anyMoving = false;
        foreach (var ball in balls)
        {
            if (ball == null) continue;
            var rb = ball.GetComponent<Rigidbody>();
            if (rb != null && rb.linearVelocity.sqrMagnitude > settleThreshold * settleThreshold)
            {
                anyMoving = true;
                break;
            }
        }

        if (!anyMoving)
        {
            return true;
        }

        return false;
    }

    // Get ball by ID for Ghost Replay tracking
    public Transform GetBallById(int ballId)
    {
        foreach (var ball in balls)
        {
            if (ball == null) continue;
            var identity = ball.GetComponent<BallIdentity>();
            if (identity != null && identity.ballId == ballId)
            {
                return ball.transform;
            }
        }
        return null;
    }

    // Get ball count for Ghost Replay recording
    public int GetBallCount()
    {
        return balls.Count;
    }
}
