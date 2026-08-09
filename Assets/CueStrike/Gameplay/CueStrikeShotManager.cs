using System;
using UnityEngine;
using CueStrike;
using CueStrike.Audio;

public class CueStrikeShotManager : MonoBehaviour
{
    public RCA rca;
    public CueStrikePhysicsManager physicsManager;
    public CueStrikeRulesManager rulesManager;

    [Header("Shot Settings")]
    public float minPullback = 0.1f;
    public float maxPullback = 1.5f;
    public float maxForce = 20f;
    public float maxSpin = 8f;
    public AnimationCurve forceCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    public AnimationCurve spinCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    public float miscuedThreshold = 0.15f;
    public float followThroughThreshold = 0.25f;
    public float settleSpeedThreshold = 0.05f;
    public float settleTime = 0.6f;

    [Header("Aim Helpers")]
    public Camera playerCamera;
    public bool useMouseAim = true;
    public bool useXR = false;

    private float lastPullback = 0f;
    private Vector3 aimAnchorScreen;
    private Vector3 aimDirection = Vector3.forward;
    private bool isAiming = false;
    private bool shotInProgress = false;
    private float settleTimer = 0f;
    private int initialBallCount;
    private Rigidbody cueBallRb;
    private bool shotFoul = false;
    private bool shotPotted = false;
    private bool shotMiscue = false;
    private float currentForce = 0f;
    private float currentSpin = 0f;
    private Vector3 contactPoint;
    private int _ballsPottedThisShot = 0;
    private int _pointsScoredThisShot = 0;

    public event Action OnShotStart;
    public event Action OnShotEnd;
    public event Action<float, float, bool> OnShotAimingUpdate;
    public event Action<bool, bool> OnShotResult;
    
    // New events for mascot/crowd systems
    public event Action<CueStrikeShotData> OnShotCompleted;
    public event Action<string, int> OnFoulCommitted;

    public Vector3 PredictedContactPoint => contactPoint;
    public float CurrentShotForce => currentForce;
    public float CurrentShotSpin => currentSpin;
    public bool IsMiscue => shotMiscue;

    /// <summary>
    /// Shot data structure for event communication
    /// </summary>
    public struct CueStrikeShotData
    {
        public int ballsPotted;
        public int pointsScored;
        public bool isFoul;
        public string foulType;
        public Vector3 cueBallPosition;
        public float shotPower;
    }

    void Awake()
    {
        if (physicsManager == null) physicsManager = CueStrikePhysicsManager.Instance;
        if (rulesManager == null) rulesManager = CueStrikeRulesManager.Instance;
        if (playerCamera == null) playerCamera = Camera.main;
    }

    void Start()
    {
        cueBallRb = FindCueBall();
        initialBallCount = GetActiveBallCount();
    }

    void Update()
    {
        if (rulesManager == null) rulesManager = CueStrikeRulesManager.Instance;
        if (physicsManager == null) physicsManager = CueStrikePhysicsManager.Instance;
        if (playerCamera == null) playerCamera = Camera.main;

        if (shotInProgress)
        {
            if (AreBallsSettled())
            {
                shotInProgress = false;
                FinishShot();
            }
            return;
        }

        if (useMouseAim && Input.GetMouseButtonDown(0))
        {
            StartAiming();
        }

        if (isAiming)
        {
            UpdateAiming();
            if (Input.GetMouseButtonUp(0))
            {
                Release();
            }
        }
    }

    public void StartAiming()
    {
        if (shotInProgress) return;

        cueBallRb = FindCueBall();
        if (cueBallRb == null) return;

        // Reset Power Shot on new shot
        var powerShot = cueBallRb.GetComponent<CueStrike.Physics.CueStrikePowerShot>();
        if (powerShot != null)
        {
            powerShot.ResetPowerShot();
        }

        isAiming = true;
        aimAnchorScreen = Input.mousePosition;
        lastPullback = 0f;
        aimDirection = DetermineAimDirection();
        shotFoul = false;
        shotPotted = false;
        shotMiscue = false;

        if (rulesManager != null)
        {
            rulesManager.BeginShot();
        }

        CueStrikeAudioManager.Instance?.PlayChalk();
        CueStrikeFXManager.Instance?.SpawnChalkDust(cueBallRb.position);

        initialBallCount = GetActiveBallCount();
        OnShotStart?.Invoke();
        UpdateAimingUI();
    }

    public void UpdateAiming()
    {
        if (!isAiming || cueBallRb == null) return;

        aimDirection = DetermineAimDirection();
        float dragDelta = (aimAnchorScreen.y - Input.mousePosition.y) / 300f;
        lastPullback = Mathf.Clamp(dragDelta, 0f, maxPullback);

        float normalized = Mathf.InverseLerp(minPullback, maxPullback, lastPullback);
        currentForce = forceCurve.Evaluate(normalized) * maxForce;
        currentSpin = spinCurve.Evaluate(normalized) * maxSpin;

        if (Input.GetKey(KeyCode.A)) currentSpin -= maxSpin * 0.55f;
        if (Input.GetKey(KeyCode.D)) currentSpin += maxSpin * 0.55f;

        contactPoint = cueBallRb.position - aimDirection * 0.5f;
        bool readyToShoot = normalized >= miscuedThreshold;
        OnShotAimingUpdate?.Invoke(currentForce, currentSpin, readyToShoot);
    }

    public void Release()
    {
        if (!isAiming || cueBallRb == null) return;

        isAiming = false;
        float normalized = Mathf.InverseLerp(minPullback, maxPullback, lastPullback);
        currentForce = forceCurve.Evaluate(normalized) * maxForce;
        currentSpin = spinCurve.Evaluate(normalized) * maxSpin;

        shotMiscue = normalized < miscuedThreshold || lastPullback < followThroughThreshold;
        float appliedForce = currentForce * (shotMiscue ? 0.35f : 1f);
        float appliedSpin = currentSpin * (shotMiscue ? 0.25f : 1f);

        Vector3 direction = aimDirection.normalized;
        contactPoint = cueBallRb.position - direction * 0.5f;

        if (cueBallRb.TryGetComponent<CueStrikeBallPhysics>(out var phys))
        {
            phys.ApplyCueImpact(direction, appliedForce, appliedSpin);
        }
        else
        {
            cueBallRb.AddForce(direction * appliedForce, ForceMode.Impulse);
        }

        if (shotMiscue)
        {
            CueStrikeAudioManager.Instance?.PlayMiscue();
            if (rulesManager != null) rulesManager.RecordFoul("Miscue");
        }

        shotFoul = shotMiscue;
        shotPotted = false;
        shotInProgress = true;
        settleTimer = 0f;
        initialBallCount = GetActiveBallCount();

        OnShotAimingUpdate?.Invoke(currentForce, currentSpin, false);
    }

    public void ExecuteShot(Rigidbody cueBallRb, Vector3 direction, float force, float spin)
    {
        if (cueBallRb == null) return;
        OnShotStart?.Invoke();

        if (cueBallRb.TryGetComponent<CueStrikeBallPhysics>(out var phys))
        {
            phys.ApplyCueImpact(direction.normalized, force, spin);
        }
        else
        {
            cueBallRb.AddForce(direction.normalized * force, ForceMode.Impulse);
        }

        shotInProgress = true;
        settleTimer = 0f;
        initialBallCount = GetActiveBallCount();
    }

    bool AreBallsSettled()
    {
        if (physicsManager == null) return false;

        bool anyMoving = false;
        foreach (var ball in physicsManager.GetAllBalls())
        {
            if (ball == null) continue;
            var rb = ball.GetComponent<Rigidbody>();
            if (rb != null && rb.linearVelocity.sqrMagnitude > settleSpeedThreshold * settleSpeedThreshold)
            {
                anyMoving = true;
                break;
            }
        }

        if (!anyMoving)
        {
            settleTimer += Time.deltaTime;
            return settleTimer >= settleTime;
        }

        settleTimer = 0f;
        return false;
    }

    void FinishShot()
    {
        bool hasLessBalls = GetActiveBallCount() < initialBallCount;
        shotPotted = shotPotted || hasLessBalls;

        if (rulesManager != null)
        {
            rulesManager.ResolveShot();
        }

        OnShotResult?.Invoke(shotPotted, shotFoul);
        OnShotEnd?.Invoke();
        cueBallRb = FindCueBall();
    }

    Rigidbody FindCueBall()
    {
        if (physicsManager == null) return null;
        foreach (var t in physicsManager.GetAllBalls())
        {
            if (t == null) continue;
            var identity = t.GetComponent<BallIdentity>();
            if (identity != null && identity.ballId == 0)
            {
                return t.GetComponent<Rigidbody>();
            }
        }

        var balls = physicsManager.GetAllBalls();
        if (balls.Count > 0) return balls[0].GetComponent<Rigidbody>();
        return null;
    }

    int GetActiveBallCount()
    {
        return physicsManager != null ? physicsManager.GetAllBalls().Count : 0;
    }

    Vector3 DetermineAimDirection()
    {
        if (useXR && rca != null && rca.cueTip != null)
        {
            return Vector3.ProjectOnPlane(rca.cueTip.forward, Vector3.up).normalized;
        }

        if (playerCamera != null)
        {
            var ray = playerCamera.ScreenPointToRay(Input.mousePosition);
            return Vector3.ProjectOnPlane(ray.direction, Vector3.up).normalized;
        }

        return Vector3.forward;
    }

    void UpdateAimingUI()
    {
        float normalized = Mathf.InverseLerp(minPullback, maxPullback, lastPullback);
        bool readyToShoot = normalized >= miscuedThreshold;
        OnShotAimingUpdate?.Invoke(currentForce, currentSpin, readyToShoot);
    }
}
