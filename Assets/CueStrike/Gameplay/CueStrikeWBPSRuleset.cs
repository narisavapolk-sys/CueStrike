using System;
using UnityEngine;

/// <summary>
/// CueStrikeWBPSRuleset - International Snooker Ruleset (WBPS / WPBSA)
/// Created by Nari for P'Mong | 2026-07-19
///
/// Ball Points (WBPS Standard):
/// - Red    = 1 point
/// - Yellow = 2 points
/// - Green  = 3 points
/// - Brown  = 4 points
/// - Blue   = 5 points
/// - Pink   = 6 points
/// - Black  = 7 points
///
/// Basic Snooker Rules (WBPS):
/// - Shot alternates Red -> Color until all reds are cleared.
/// - After all reds are cleared, colors are potted in order:
///   Yellow -> Green -> Brown -> Blue -> Pink -> Black.
/// - Fouls: wrong ball struck, cue ball potted, no ball hit, illegal pot.
/// - Foul = lose points equal to the highest-value ball involved (minimum 4 points).
/// </summary>
public class CueStrikeWBPSRuleset : MonoBehaviour
{
    public static CueStrikeWBPSRuleset Instance { get; private set; }

    /// <summary>
    /// Snooker ball types per WBPS standard
    /// </summary>
    public enum SnookerBallType
    {
        Red = 0,      // 1 point
        Yellow = 1,   // 2 points
        Green = 2,    // 3 points
        Brown = 3,    // 4 points
        Blue = 4,     // 5 points
        Pink = 5,     // 6 points
        Black = 6,    // 7 points
        CueBall = 7   // Cue ball
    }

    /// <summary>
    /// Points per ball type per WBPS rules
    /// </summary>
    public static int GetBallPoints(SnookerBallType ballType)
    {
        switch (ballType)
        {
            case SnookerBallType.Red:    return 1;
            case SnookerBallType.Yellow: return 2;
            case SnookerBallType.Green:  return 3;
            case SnookerBallType.Brown:  return 4;
            case SnookerBallType.Blue:   return 5;
            case SnookerBallType.Pink:   return 6;
            case SnookerBallType.Black:  return 7;
            default: return 0;
        }
    }

    /// <summary>
    /// Maps prefab ballId to SnookerBallType
    /// (Mapping: 1=Red01..15=Red15, 16=Yellow, 17=Green, 18=Brown, 19=Blue, 20=Pink, 21=Black, 0=CueBall)
    /// </summary>
    public static SnookerBallType GetBallTypeFromId(int ballId)
    {
        if (ballId == 0) return SnookerBallType.CueBall;
        if (ballId >= 1 && ballId <= 15) return SnookerBallType.Red;
        if (ballId == 16) return SnookerBallType.Yellow;
        if (ballId == 17) return SnookerBallType.Green;
        if (ballId == 18) return SnookerBallType.Brown;
        if (ballId == 19) return SnookerBallType.Blue;
        if (ballId == 20) return SnookerBallType.Pink;
        if (ballId == 21) return SnookerBallType.Black;
        return SnookerBallType.CueBall;
    }

    /// <summary>
    /// True if the ball type is one of the six colors (Yellow..Black).
    /// </summary>
    public static bool IsColorBall(SnookerBallType ballType)
    {
        return ballType >= SnookerBallType.Yellow && ballType <= SnookerBallType.Black;
    }

    [Header("Frame Setup (WBPS Standard)")]
    public int totalRedBalls = 15;
    public int minFoulPoints = 4;
    public int maxFoulPoints = 7;

    [Header("Game State")]
    public int redsRemaining = 15;
    public bool isColorPhase = false; // Start color phase after reds cleared

    [Header("Shot Sequence State")]
    [Tooltip("True after a red is potted: the next shot must strike a color.")]
    public bool awaitingRespotColor = false;
    [Tooltip("Color phase sequence index: 0=Yellow, 1=Green, 2=Brown, 3=Blue, 4=Pink, 5=Black.")]
    [SerializeField] private int _colorSequenceIndex = 0;

    public event Action<int> OnBallPotted;
    public event Action<int, string> OnFoulCommitted;
    public event Action OnFrameWon;

    void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        Instance = this;
        ResetFrame();
    }

    /// <summary>
    /// Registers a potted ball per snooker rules and updates shot sequence state.
    /// </summary>
    /// <returns>Points scored (0 = foul)</returns>
    public int RegisterPot(int ballId, bool isBreak = false)
    {
        var ballType = GetBallTypeFromId(ballId);

        // Cue ball potted = foul
        if (ballType == SnookerBallType.CueBall)
        {
            CommitFoul("Cue ball potted", 4);
            return 0;
        }

        if (!isColorPhase)
        {
            // == Red phase ==========================================================
            if (ballType == SnookerBallType.Red)
            {
                redsRemaining--;
                awaitingRespotColor = true; // next shot must strike a color
                int pts = GetBallPoints(ballType);
                OnBallPotted?.Invoke(pts);
                if (redsRemaining <= 0)
                {
                    isColorPhase = true;
                    _colorSequenceIndex = 0;
                }
                return pts;
            }
            else if (IsColorBall(ballType))
            {
                // Color potted is legal only when we are awaiting a color (after a red)
                if (awaitingRespotColor)
                {
                    awaitingRespotColor = false; // back to red
                    int pts = GetBallPoints(ballType);
                    OnBallPotted?.Invoke(pts);
                    return pts;
                }

                // Color potted while a red was "on" = foul
                CommitFoul($"Illegal color pot before clearing reds: {ballType}",
                    Mathf.Max(minFoulPoints, GetBallPoints(ballType)));
                return 0;
            }

            return 0;
        }
        else
        {
            // == Color phase ========================================================
            // Colors must be potted in order: Yellow -> Green -> Brown -> Blue -> Pink -> Black
            var expectedType = (SnookerBallType)(_colorSequenceIndex + 1);
            if (ballType != expectedType || !IsColorBall(ballType))
            {
                CommitFoul($"Wrong color potted in color phase: expected {expectedType}, got {ballType}",
                    Mathf.Max(minFoulPoints, GetBallPoints(expectedType)));
                return 0;
            }

            int pts = GetBallPoints(ballType);
            OnBallPotted?.Invoke(pts);
            _colorSequenceIndex++;

            // Black potted as final ball = frame won
            if (ballType == SnookerBallType.Black && _colorSequenceIndex > 5)
            {
                OnFrameWon?.Invoke();
            }
            return pts;
        }
    }

    /// <summary>
    /// Checks if a color ball should be spotted (returned to table).
    /// Colors potted during the red phase and the color phase (before frame end) are spotted.
    /// Reds are never spotted.
    /// </summary>
    public bool ShouldSpotColor(int ballId)
    {
        var ballType = GetBallTypeFromId(ballId);
        return IsColorBall(ballType);
    }

    /// <summary>
    /// Registers a basic foul
    /// </summary>
    public void CommitFoul(string reason, int penalty = 4)
    {
        int foulPts = Mathf.Clamp(penalty, minFoulPoints, maxFoulPoints);
        Debug.Log($"[WBPS] Foul: {reason} - Penalty: {foulPts}");
        OnFoulCommitted?.Invoke(foulPts, reason);
    }

    // ============================================================
    //  Shot validation (complete foul detection)
    // ============================================================

    /// <summary>
    /// Validates a shot per WBPS snooker rules.
    /// Supports red->color alternation, color phase sequence, and basic fouls.
    /// </summary>
    /// <param name="cueBallHitSomething">True if the cue ball contacted any object ball.</param>
    /// <param name="firstHitBallId">Ball id of the first ball struck (0 = none).</param>
    /// <param name="pottedBallId">Ball id potted this stroke (0 = none).</param>
    public void ValidateShot(bool cueBallHitSomething, int firstHitBallId, int pottedBallId)
        => ValidateShotFull(cueBallHitSomething, firstHitBallId, false, pottedBallId, null);

    /// <summary>
    /// Extended validation supporting cue-ball-potted detection and multiple potted balls.
    /// </summary>
    /// <param name="cueBallHitSomething">True if the cue ball contacted any object ball.</param>
    /// <param name="firstHitBallId">Ball id of the first ball struck (0 = none).</param>
    /// <param name="cueBallPotted">True if the cue ball was potted this stroke.</param>
    /// <param name="pottedBallId">Primary ball id potted this stroke (0 = none).</param>
    /// <param name="additionalPottedBallIds">Any additional balls potted in the same stroke (may be null).</param>
    public void ValidateShotFull(bool cueBallHitSomething, int firstHitBallId, bool cueBallPotted,
        int pottedBallId, System.Collections.Generic.List<int> additionalPottedBallIds)
    {
        // Foul 1: Cue ball did not hit any ball
        if (!cueBallHitSomething)
        {
            CommitFoul("Cue ball did not hit any ball", 4);
            return;
        }

        var firstHitType = GetBallTypeFromId(firstHitBallId);

        // Foul 2: Cue ball potted
        if (cueBallPotted)
        {
            CommitFoul("Cue ball potted", 4);
            return;
        }

        // Foul 3: First ball struck must be the correct type per phase
        var (requiredType, anyColorAllowed) = GetRequiredFirstBall();
        if (!IsLegalFirstBall(firstHitType, requiredType, anyColorAllowed))
        {
            int foulPts = Mathf.Max(minFoulPoints,
                anyColorAllowed ? minFoulPoints : GetBallPoints(requiredType));
            string expected = anyColorAllowed ? "a color" : requiredType.ToString();
            CommitFoul($"Wrong first ball struck: expected {expected}, hit {firstHitType}", foulPts);
            return;
        }

        // Foul 4: Primary potted ball must be legal for the current phase
        if (pottedBallId != 0)
        {
            var pottedType = GetBallTypeFromId(pottedBallId);
            if (!IsLegalPot(pottedType))
            {
                int foulPts = Mathf.Max(minFoulPoints,
                    Mathf.Max(GetBallPoints(requiredType), GetBallPoints(pottedType)));
                CommitFoul($"Illegal pot: {pottedType} (required first: {DescribeRequired(requiredType, anyColorAllowed)})", foulPts);
                return;
            }
            ApplyLegalPot(pottedType);
        }

        // Foul 5: Additional potted balls (multi-ball pot)
        if (additionalPottedBallIds != null)
        {
            foreach (int extraId in additionalPottedBallIds)
            {
                if (extraId == 0) continue; // cue ball already handled above
                var extraType = GetBallTypeFromId(extraId);
                if (!IsLegalPot(extraType))
                {
                    int foulPts = Mathf.Max(minFoulPoints,
                        Mathf.Max(GetBallPoints(requiredType), GetBallPoints(extraType)));
                    CommitFoul($"Illegal multi-ball pot: {extraType}", foulPts);
                    return;
                }
                ApplyLegalPot(extraType);
            }
        }

        // No foul
        string pottedDesc = pottedBallId == 0 ? "none" : GetBallTypeFromId(pottedBallId).ToString();
        Debug.Log($"[WBPS] Shot valid: first={firstHitType}, potted={pottedDesc}");
    }

    /// <summary>
    /// Determines which ball must be struck first for the current phase.
    /// </summary>
    /// <returns>
    /// requiredType = the specific ball to strike (Red or the next color in sequence).
    /// anyColorAllowed = true when, after a red pot, any of the six colors may be struck.
    /// </returns>
    private (SnookerBallType requiredType, bool anyColorAllowed) GetRequiredFirstBall()
    {
        if (!isColorPhase)
        {
            if (awaitingRespotColor)
                return (SnookerBallType.Yellow, true); // any color is "on" after a red pot
            return (SnookerBallType.Red, false);
        }

        // Color phase: strictly the next color in sequence
        return ((SnookerBallType)(_colorSequenceIndex + 1), false);
    }

    private static bool IsLegalFirstBall(SnookerBallType firstHitType, SnookerBallType requiredType, bool anyColorAllowed)
    {
        // Cue-ball/cue-ball type recorded as a strike is never legal
        if (firstHitType == SnookerBallType.CueBall) return false;

        // After a red pot, any color (Yellow..Black) is legal
        if (anyColorAllowed) return IsColorBall(firstHitType);

        return firstHitType == requiredType;
    }

    /// <summary>
    /// Checks whether potting this ball is legal for the current phase.
    /// </summary>
    private bool IsLegalPot(SnookerBallType pottedType)
    {
        if (pottedType == SnookerBallType.CueBall) return false;

        if (!isColorPhase)
        {
            // Red phase:
            // - Reds may be potted only when a red is "on" (not awaiting a color).
            // - A color may be potted only when we are awaiting a color (after a red pot).
            if (pottedType == SnookerBallType.Red)
                return !awaitingRespotColor;
            return IsColorBall(pottedType) && awaitingRespotColor;
        }

        // Color phase: only the next color in sequence may be potted
        var expectedType = (SnookerBallType)(_colorSequenceIndex + 1);
        return pottedType == expectedType;
    }

    /// <summary>
    /// Applies the state change produced by a legal pot.
    /// </summary>
    private void ApplyLegalPot(SnookerBallType pottedType)
    {
        if (!isColorPhase)
        {
            if (pottedType == SnookerBallType.Red)
            {
                redsRemaining--;
                awaitingRespotColor = true; // next shot must strike a color
                if (redsRemaining <= 0)
                {
                    isColorPhase = true;
                    _colorSequenceIndex = 0;
                }
            }
            else if (IsColorBall(pottedType))
            {
                awaitingRespotColor = false; // back to red
            }
            return;
        }

        // Color phase: advance to next color in sequence
        _colorSequenceIndex++;
        if (pottedType == SnookerBallType.Black && _colorSequenceIndex > 5)
        {
            OnFrameWon?.Invoke();
        }
    }

    private static string DescribeRequired(SnookerBallType requiredType, bool anyColorAllowed)
    {
        return anyColorAllowed ? "a color" : requiredType.ToString();
    }

    /// <summary>
    /// Resets frame to initial state
    /// </summary>
    public void ResetFrame()
    {
        redsRemaining = totalRedBalls;
        isColorPhase = false;
        awaitingRespotColor = false;
        _colorSequenceIndex = 0;
                Debug.Log("[WBPS] Frame reset - 15 reds, 6 colors on table");
    }

    /// <summary>
    /// Checks if color phase should start (reds cleared)
    /// </summary>
    public bool CheckColorPhaseStart()
    {
        if (redsRemaining <= 0 && !isColorPhase)
        {
            isColorPhase = true;
            _colorSequenceIndex = 0;
            Debug.Log("[WBPS] All reds cleared - Color phase begins (Yellow -> Black sequence)");
            return true;
        }
        return false;
    }
}
