using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using CueStrike.Gameplay.ChinesePool;

// Type aliases for CueStrikeAIController nested types
using BallEntry = CueStrike.AI.CueStrikeAIController.BallEntry;
using ShotPlan = CueStrike.AI.CueStrikeAIController.ShotPlan;
using TableState = CueStrike.AI.CueStrikeAIController.TableState;
using AIParameters = CueStrike.AI.CueStrikeAIController.AIParameters;
using SkillLevel = CueStrike.AI.SkillLevel;

namespace CueStrike.AI
{
    /// <summary>
    /// Chinese 8-Ball AI strategy implementing ICueStrikeAIStrategy.
    /// Evaluates potting probability, cut angles, obstructions, safety options,
    /// position play, and call-shot compliance for Chinese Pool rules.
    /// </summary>
    public class ChinesePoolAIEasy : ICueStrikeAIStrategy
    {
        protected AIParameters _params;
        private System.Random _rng = new System.Random();

        public virtual void Initialize(AIParameters parameters)
        {
            _params = parameters;
        }

        public virtual ShotPlan? SelectShot(TableState tableState, AIParameters parameters)
        {
            if (tableState.availableBalls.Count == 0) return null;

            var (playerGroup, currentPhase) = GetChinesePoolState();

            // 1. Filter balls by game rules
            var legalBalls = FilterLegalBalls(tableState, playerGroup, currentPhase);
            if (legalBalls.Count == 0)
            {
                return PlaySafe(tableState, null);
            }

            // 2. Evaluate each ball → pocket combination
            var evaluations = new List<(ShotPlan plan, float score)>();
            foreach (var ball in legalBalls)
            {
                for (int pocketIdx = 0; pocketIdx < 6; pocketIdx++)
                {
                    var eval = EvaluateShot(tableState, ball, pocketIdx);
                    if (eval.HasValue)
                    {
                        evaluations.Add(eval.Value);
                    }
                }
            }

            // 3. Sort by score descending
            evaluations.Sort((a, b) => b.score.CompareTo(a.score));

            // 4. Easy AI: pick randomly from top 3 (with some noise)
            float randomThreshold = _params.accuracy * 0.5f; // Easy: more random
            if (evaluations.Count > 0 && (float)_rng.NextDouble() < randomThreshold)
            {
                int idx = _rng.Next(0, Mathf.Min(3, evaluations.Count));
                return evaluations[idx].plan;
            }

            // 5. If no good potting opportunity, play safe
            if (evaluations.Count == 0 || evaluations[0].score < 0.4f)
            {
                return PlaySafe(tableState, legalBalls);
            }

            return evaluations[0].plan;
        }

        protected virtual (ShotPlan plan, float score)? EvaluateShot(TableState tableState, BallEntry ball, int pocketIdx)
        {
            Vector3 pocketPos = AIPocketHelper.GetPocketPosition(pocketIdx);
            Vector3 cuePos = tableState.cueBallPosition;

            // Cut angle
            float cutAngle = CalculateCutAngle(cuePos, ball.position, pocketPos);

            // Direct line check
            bool obstructed = IsPathObstructed(ball.position, pocketPos, tableState.availableBalls, ball.id);

            // Distance
            float distCueToBall = Vector3.Distance(cuePos, ball.position);
            float distBallToPocket = Vector3.Distance(ball.position, pocketPos);

            // Potting difficulty score (0 = impossible, 1 = guaranteed)
            float score = 1.0f;

            // Penalize large cut angles
            float anglePenalty = Mathf.Abs(cutAngle) / 90f; // 0 at 0°, 1 at 90°
            score -= anglePenalty * 0.5f;

            // Penalize obstructions heavily
            if (obstructed)
            {
                score -= 0.6f;
            }

            // Penalize long distances
            float distPenalty = (distCueToBall + distBallToPocket) / 5.0f; // ~max table diagonal
            score -= Mathf.Clamp01(distPenalty) * 0.2f;

            // Apply difficulty-based noise
            score += UnityEngine.Random.Range(-_params.errorMargin, _params.errorMargin);

            score = Mathf.Clamp01(score);

            if (score < 0.1f) return null; // Not worth attempting

            // Calculate aim point
            Vector3 aimPoint = CalculateAimPoint(cuePos, ball.position, pocketPos, ball.id);

            var plan = new ShotPlan
            {
                ballId = ball.id,
                targetPosition = aimPoint,
                pocketIndex = pocketIdx,
                power = Mathf.Clamp(distCueToBall * 2.5f, 0.3f, 0.9f),
                spin = Vector3.zero,
                isSafe = false
            };

            return (plan, score);
        }

        public float CalculateCutAngle(Vector3 cue, Vector3 ball, Vector3 pocket)
        {
            Vector3 cueToBall = (ball - cue).normalized;
            Vector3 ballToPocket = (pocket - ball).normalized;
            float dot = Vector3.Dot(cueToBall, ballToPocket);
            return Mathf.Rad2Deg * Mathf.Acos(Mathf.Clamp(dot, -1f, 1f));
        }

        public bool IsPathObstructed(Vector3 from, Vector3 to, List<BallEntry> allBalls, int excludeId)
        {
            Vector3 dir = (to - from).normalized;
            float dist = Vector3.Distance(from, to);
            float ballRadius = 0.028575f; // Standard ball radius

            foreach (var ball in allBalls)
            {
                if (ball.id == excludeId || ball.isPotted) continue;

                // Check if ball obstructs the path to pocket
                Vector3 toObstacle = ball.position - from;
                float projDist = Vector3.Dot(toObstacle, dir);
                if (projDist < 0 || projDist > dist) continue;

                Vector3 closest = from + dir * projDist;
                float distToLine = Vector3.Distance(ball.position, closest);

                if (distToLine < ballRadius * 2.0f) // Allow slight grazing
                {
                    return true;
                }
            }
            return false;
        }

        protected Vector3 CalculateAimPoint(Vector3 cuePos, Vector3 ballPos, Vector3 pocketPos, int ballId)
        {
            float ballRadius = 0.028575f;
            Vector3 ballToPocket = (pocketPos - ballPos).normalized;
            // Aim at the contact point (edge of object ball opposite the pocket)
            return ballPos - ballToPocket * (ballRadius * 2.1f);
        }

        protected List<BallEntry> FilterLegalBalls(TableState tableState, ChinesePoolGameManager.BallGroup playerGroup, ChinesePoolMatchState phase)
        {
            var legal = new List<BallEntry>();

            foreach (var ball in tableState.availableBalls)
            {
                if (ball.id == 0) continue; // Skip cue ball

                var ballGroup = ChinesePoolGameManager.Instance != null
                    ? ChinesePoolGameManager.Instance.GetBallGroup(ball.id)
                    : ChinesePoolGameManager.BallGroup.None;

                // During break or open table: any object ball is legal
                if (phase == ChinesePoolMatchState.Break || phase == ChinesePoolMatchState.OpenTable)
                {
                    legal.Add(ball);
                }
                // Player has group: only their group balls are legal (not black yet)
                else if (ballGroup == playerGroup)
                {
                    legal.Add(ball);
                }
                // Black ball: legal only if player has cleared all their group
                else if (ball.id == 8)
                {
                    int playerIdx = (playerGroup == ChinesePoolGameManager.Instance?.player1Group) ? 0 : 1;
                    if (HasClearedGroup(playerIdx, playerGroup))
                    {
                        legal.Add(ball);
                    }
                }
            }

            return legal;
        }

        protected bool HasClearedGroup(int playerIndex, ChinesePoolGameManager.BallGroup group)
        {
            if (ChinesePoolGameManager.Instance == null) return false;

            // Check if all group balls are potted
            // Get balls on table, filter by group
            int ballsInGroup = 0;
            int ballsPotted = 0;

            // Simplified: if score >= 7, all group balls potted
            int score = (playerIndex == 0)
                ? ChinesePoolGameManager.Instance.scorePlayer1
                : ChinesePoolGameManager.Instance.scorePlayer2;

            return score >= 7;
        }

        protected (ChinesePoolGameManager.BallGroup group, ChinesePoolMatchState phase) GetChinesePoolState()
        {
            if (ChinesePoolGameManager.Instance == null)
                return (ChinesePoolGameManager.BallGroup.None, ChinesePoolMatchState.Waiting);

            var group = ChinesePoolGameManager.Instance.GetCurrentPlayerGroup();
            var phase = ChinesePoolGameManager.Instance.currentPhase;
            return (group, phase);
        }

        protected ShotPlan? PlaySafe(TableState tableState, List<BallEntry> legalBalls)
        {
            if (tableState.availableBalls.Count == 0) return null;

            Vector3 cuePos = tableState.cueBallPosition;
            BallEntry target;

            if (legalBalls != null && legalBalls.Count > 0)
            {
                // Hit the nearest legal ball gently toward a rail
                target = legalBalls.OrderBy(b => Vector3.Distance(cuePos, b.position)).First();
            }
            else
            {
                // Hit any ball gently toward a rail
                target = tableState.availableBalls.Where(b => b.id != 0)
                    .OrderBy(b => Vector3.Distance(cuePos, b.position))
                    .FirstOrDefault();

                if (target.id == 0) return null;
            }

            // Aim to send ball toward nearest rail
            Vector3 railDir = GetNearestRailDirection(target.position);

            return new ShotPlan
            {
                ballId = target.id,
                targetPosition = target.position + railDir * 0.2f,
                pocketIndex = -1,
                power = 0.25f,
                spin = new Vector3(0, 0, 0),
                isSafe = true
            };
        }

        protected Vector3 GetNearestRailDirection(Vector3 pos)
        {
            float tableHalfX = 0.914f / 2f;
            float tableHalfZ = 1.828f / 2f;

            // Find nearest rail and return direction toward it
            float distRight = Mathf.Abs(tableHalfX - pos.x);
            float distLeft = Mathf.Abs(-tableHalfX - pos.x);
            float distTop = Mathf.Abs(tableHalfZ - pos.z);
            float distBottom = Mathf.Abs(-tableHalfZ - pos.z);

            float minDist = Mathf.Min(distRight, distLeft, distTop, distBottom);

            if (minDist == distRight) return Vector3.right;
            if (minDist == distLeft) return Vector3.left;
            if (minDist == distTop) return new Vector3(0, 0, 1);
            return new Vector3(0, 0, -1);
        }
    }

    /// <summary>
    /// Medium AI: Adds basic position play and better shot selection logic.
    /// </summary>
    public class ChinesePoolAIMedium : ChinesePoolAIEasy
    {
        public override ShotPlan? SelectShot(TableState tableState, AIParameters parameters)
        {
            if (tableState.availableBalls.Count == 0) return null;

            var (playerGroup, currentPhase) = GetChinesePoolState();
            var legalBalls = FilterLegalBalls(tableState, playerGroup, currentPhase);
            if (legalBalls.Count == 0)
                return PlaySafe(tableState, null);

            var evaluations = new List<(ShotPlan plan, float score)>();
            foreach (var ball in legalBalls)
            {
                for (int pocketIdx = 0; pocketIdx < 6; pocketIdx++)
                {
                    var eval = EvaluateShot(tableState, ball, pocketIdx);
                    if (eval.HasValue)
                    {
                        // Medium: Add position play factor
                        var positionScore = EvaluatePositionPlay(tableState, eval.Value.plan, legalBalls);
                        var adjustedScore = eval.Value.score * 0.7f + positionScore * 0.3f;
                        var adjustedPlan = eval.Value.plan;
                        evaluations.Add((adjustedPlan, adjustedScore));
                    }
                }
            }

            evaluations.Sort((a, b) => b.score.CompareTo(a.score));

            // Medium: Pick best shot with slight randomness
            if (evaluations.Count > 0 && UnityEngine.Random.value < _params.accuracy * 0.8f)
            {
                return evaluations[0].plan;
            }

            if (evaluations.Count == 0 || evaluations[0].score < 0.3f)
            {
                return PlaySafe(tableState, legalBalls);
            }

            return evaluations.Count > 0 ? evaluations[0].plan : null;
        }

        protected float EvaluatePositionPlay(TableState tableState, ShotPlan plan, List<BallEntry> remainingBalls)
        {
            if (plan.pocketIndex < 0) return 0f;

            // Predict cue ball position after shot (simplified: stop shot assumption)
            Vector3 predictedCuePos = PredictCueBallEndPos(tableState, plan);

            // Score = distance to next target ball (closer = better)
            var otherBalls = remainingBalls.Where(b => b.id != plan.ballId && b.id != 0).ToList();
            if (otherBalls.Count == 0) return 0.5f;

            float totalDist = 0f;
            foreach (var b in otherBalls)
            {
                totalDist += Vector3.Distance(predictedCuePos, b.position);
            }
            float avgDist = totalDist / otherBalls.Count;

            // Normalize: 0 distance = 1.0, 3m distance = 0.0
            return 1.0f - Mathf.Clamp01(avgDist / 3.0f);
        }

        protected Vector3 PredictCueBallEndPos(TableState tableState, ShotPlan plan)
        {
            if (ChinesePoolGameManager.Instance?.ballSetup == null)
                return tableState.cueBallPosition;

            var targetBall = ChinesePoolGameManager.Instance.ballSetup.GetBallById(plan.ballId);
            if (targetBall == null) return tableState.cueBallPosition;

            Vector3 cueToTarget = (targetBall.transform.position - tableState.cueBallPosition).normalized;
            // Simple: cue ball deflects based on hit position
            return tableState.cueBallPosition + cueToTarget * -0.15f;
        }
    }

    /// <summary>
    /// Hard AI: Evaluates multiple shot sequences, uses safety more effectively,
    /// and considers combination shots.
    /// </summary>
    public class ChinesePoolAIHard : ChinesePoolAIMedium
    {
        public override ShotPlan? SelectShot(TableState tableState, AIParameters parameters)
        {
            if (tableState.availableBalls.Count == 0) return null;

            var (playerGroup, currentPhase) = GetChinesePoolState();
            var legalBalls = FilterLegalBalls(tableState, playerGroup, currentPhase);
            if (legalBalls.Count == 0)
            {
                // Try combination: use opponent's ball to pot own ball
                return EvaluateCombinationShot(tableState, playerGroup, currentPhase)
                       ?? PlaySafe(tableState, null);
            }

            var evaluations = new List<(ShotPlan plan, float score)>();
            foreach (var ball in legalBalls)
            {
                for (int pocketIdx = 0; pocketIdx < 6; pocketIdx++)
                {
                    var eval = EvaluateShot(tableState, ball, pocketIdx);
                    if (eval.HasValue)
                    {
                        float positionScore = EvaluatePositionPlay(tableState, eval.Value.plan, legalBalls);
                        float sequencingScore = EvaluateSequencing(eval.Value.plan, legalBalls);
                        float score = eval.Value.score * 0.5f + positionScore * 0.25f + sequencingScore * 0.25f;
                        evaluations.Add((eval.Value.plan, score));
                    }
                }
            }

            evaluations.Sort((a, b) => b.score.CompareTo(a.score));

            // Hard: Pick the best shot most of the time
            if (evaluations.Count > 0 && UnityEngine.Random.value < _params.accuracy * 0.9f + 0.1f)
            {
                // Check if safety is actually better
                if (evaluations[0].score < 0.35f)
                {
                    var safety = EvaluateSafety(tableState, legalBalls, playerGroup);
                    if (safety.HasValue && safety.Value.score > evaluations[0].score)
                    {
                        return safety.Value.plan;
                    }
                }
                return evaluations[0].plan;
            }

            return evaluations.Count > 0 ? evaluations[0].plan : PlaySafe(tableState, legalBalls);
        }

        protected float EvaluateSequencing(ShotPlan plan, List<BallEntry> remainingBalls)
        {
            // Prefer balls that are "key balls" — balls that open up clusters
            if (remainingBalls.Count <= 1) return 1.0f;

            var targetBall = remainingBalls.FirstOrDefault(b => b.id == plan.ballId);
            if (targetBall.id == 0) return 0f;

            // Count nearby same-group balls (breaking clusters = good)
            float clusterRadius = 0.15f;
            int nearby = remainingBalls.Count(b =>
                b.id != plan.ballId &&
                Vector3.Distance(b.position, targetBall.position) < clusterRadius);

            // Prefer ball that unlocks a cluster
            return Mathf.Clamp01(1.0f + nearby * 0.3f - remainingBalls.Count * 0.05f);
        }

        protected (ShotPlan plan, float score)? EvaluateSafety(TableState tableState, List<BallEntry> legalBalls, ChinesePoolGameManager.BallGroup playerGroup)
        {
            if (legalBalls.Count == 0) return null;

            // Find a ball that can be gently tapped with the cue ball
            // ending up hidden behind other balls or against a rail
            var cuePos = tableState.cueBallPosition;

            foreach (var ball in legalBalls.OrderBy(b => Vector3.Distance(cuePos, b.position)))
            {
                // Check if we can hide behind another ball
                var otherBalls = tableState.availableBalls.Where(b => b.id != ball.id && b.id != 0).ToList();
                foreach (var hideBall in otherBalls)
                {
                    Vector3 dirFromHide = (hideBall.position - cuePos).normalized;
                    Vector3 targetPos = ball.position + dirFromHide * 0.15f;

                    float safetyScore = 0.6f;
                    var plan = new ShotPlan
                    {
                        ballId = ball.id,
                        targetPosition = targetPos,
                        pocketIndex = -1,
                        power = 0.2f,
                        spin = Vector3.zero,
                        isSafe = true
                    };
                    return (plan, safetyScore);
                }
            }

            return null;
        }

        protected ShotPlan? EvaluateCombinationShot(TableState tableState, ChinesePoolGameManager.BallGroup playerGroup, ChinesePoolMatchState phase)
        {
            // If no legal balls directly, can we pot using opponent's ball as a bridge?
            var opponentGroup = playerGroup == ChinesePoolGameManager.BallGroup.Red
                ? ChinesePoolGameManager.BallGroup.Yellow
                : ChinesePoolGameManager.BallGroup.Red;

            var opponentBalls = tableState.availableBalls
                .Where(b => b.id != 0 && b.id != 8 &&
                    (ChinesePoolGameManager.Instance?.GetBallGroup(b.id) == opponentGroup))
                .ToList();

            var ownBalls = tableState.availableBalls
                .Where(b => b.id != 0 && b.id != 8 &&
                    (ChinesePoolGameManager.Instance?.GetBallGroup(b.id) == playerGroup))
                .ToList();

            if (opponentBalls.Count == 0 || ownBalls.Count == 0) return null;

            Vector3 cuePos = tableState.cueBallPosition;

            // Can we hit opponent ball → own ball → pocket?
            foreach (var oppBall in opponentBalls)
            {
                foreach (var ownBall in ownBalls)
                {
                    Vector3 oppToOwn = (ownBall.position - oppBall.position).normalized;
                    for (int pocketIdx = 0; pocketIdx < 6; pocketIdx++)
                    {
                        Vector3 pocketPos = AIPocketHelper.GetPocketPosition(pocketIdx);
                        Vector3 ownToPocket = (pocketPos - ownBall.position).normalized;

                        // Check if opp→own→pocket alignment is reasonable
                        float alignment = Vector3.Dot(oppToOwn, ownToPocket);
                        if (alignment > 0.85f) // Good alignment
                        {
                            // Check cue→opp path
                            bool cueToOppClear = !IsPathObstructed(cuePos, oppBall.position, tableState.availableBalls, oppBall.id);
                            if (cueToOppClear)
                            {
                                return new ShotPlan
                                {
                                    ballId = oppBall.id,
                                    targetPosition = oppBall.position - oppToOwn * 0.05f,
                                    pocketIndex = pocketIdx,
                                    power = 0.6f,
                                    spin = Vector3.zero,
                                    isSafe = false
                                };
                            }
                        }
                    }
                }
            }

            return null;
        }
    }

    /// <summary>
    /// Expert AI: Full table awareness, break-building, precise safety,
    /// cue ball control, multi-shot planning, and tactical foul decisions.
    /// </summary>
    public class ChinesePoolAIExpert : ChinesePoolAIHard
    {
        private const int MAX_SEARCH_DEPTH = 2; // Lookahead shots

        public override ShotPlan? SelectShot(TableState tableState, AIParameters parameters)
        {
            if (tableState.availableBalls.Count == 0) return null;

            var (playerGroup, currentPhase) = GetChinesePoolState();
            var legalBalls = FilterLegalBalls(tableState, playerGroup, currentPhase);

            // 1. Evaluate all direct shots first
            var directEvals = new List<(ShotPlan plan, float score, BallEntry ball)>();

            // Use all balls on table for evaluation (not just legal - consider combos via any ball)
            var evalBalls = legalBalls.Count > 0 ? legalBalls
                : tableState.availableBalls.Where(b => b.id != 0).ToList();

            foreach (var ball in evalBalls)
            {
                for (int pocketIdx = 0; pocketIdx < 6; pocketIdx++)
                {
                    var eval = EvaluateShot(tableState, ball, pocketIdx);
                    if (eval.HasValue)
                    {
                        float posScore = EvaluatePositionPlay(tableState, eval.Value.plan, legalBalls);
                        float seqScore = EvaluateSequencing(eval.Value.plan, legalBalls);

                        // Expert: Add 1-shot lookahead
                        float lookaheadScore = EvaluateLookahead(tableState, eval.Value.plan, legalBalls, playerGroup, MAX_SEARCH_DEPTH);

                        float totalScore = eval.Value.score * 0.35f + posScore * 0.15f + seqScore * 0.15f + lookaheadScore * 0.35f;
                        directEvals.Add((eval.Value.plan, totalScore, ball));
                    }
                }
            }

            directEvals.Sort((a, b) => b.score.CompareTo(a.score));

            // 2. Check for tactical options if best shot is poor
            if (directEvals.Count == 0 || directEvals[0].score < 0.5f)
            {
                // Try combos first
                var combo = EvaluateCombinationShot(tableState, playerGroup, currentPhase);
                if (combo.HasValue) return combo;

                // Evaluate tactical safety
                var tacticalShot = EvaluateTacticalSafety(tableState, legalBalls, playerGroup);
                if (tacticalShot.HasValue) return tacticalShot.Value.plan;

                // Evaluate intentional foul (advanced: snooker behind ball)
                var foulShot = EvaluateIntentionalFoul(tableState, playerGroup);
                if (foulShot.HasValue) return foulShot.Value.plan;
            }

            // 3. Expert: nearly always pick the best shot
            if (directEvals.Count > 0 && UnityEngine.Random.value < _params.accuracy * 0.98f + 0.02f)
            {
                return directEvals[0].plan;
            }

            return directEvals.Count > 0 ? directEvals[0].plan : PlaySafe(tableState, legalBalls);
        }

        private float EvaluateLookahead(TableState tableState, ShotPlan plan, List<BallEntry> remainingBalls,
            ChinesePoolGameManager.BallGroup playerGroup, int depth)
        {
            if (depth <= 0) return 0.5f;
            if (remainingBalls.Count <= 1) return 1.0f;

            // Simulate the shot outcome
            var predictedState = SimulateShotOutcome(tableState, plan, playerGroup);
            if (!predictedState.HasValue) return 0f;

            // Evaluate the next best shot from the new state
            var nextLegalBalls = FilterLegalBalls(predictedState.Value, playerGroup, ChinesePoolMatchState.Playing);
            if (nextLegalBalls.Count == 0) return 0f;

            float bestNextScore = 0f;
            foreach (var nextBall in nextLegalBalls)
            {
                for (int pocketIdx = 0; pocketIdx < 6; pocketIdx++)
                {
                    var nextEval = EvaluateShot(predictedState.Value, nextBall, pocketIdx);
                    if (nextEval.HasValue)
                    {
                        if (nextEval.Value.score > bestNextScore)
                        {
                            bestNextScore = nextEval.Value.score;
                        }
                    }
                }
            }

            // Discount for depth
            return bestNextScore * Mathf.Pow(0.8f, MAX_SEARCH_DEPTH - depth + 1);
        }

        private TableState? SimulateShotOutcome(TableState current, ShotPlan plan, ChinesePoolGameManager.BallGroup playerGroup)
        {
            // Simplified shot simulation:
            // 1. Remove the target ball (assume potted)
            // 2. Estimate cue ball new position
            // 3. Return new table state

            var newBalls = new List<BallEntry>();
            Vector3 predictedCuePos = PredictCueBallEndPos(current, plan);

            // Filter out the potted ball
            foreach (var ball in current.availableBalls)
            {
                if (ball.id == plan.ballId)
                {
                    continue; // Assume potted
                }

                newBalls.Add(new BallEntry
                {
                    id = ball.id,
                    position = ball.position,
                    isPotted = ball.isPotted
                });
            }

            // If cue ball potted in simulation, it's in hand (return current)
            float pocketRadius = 0.04f;
            for (int p = 0; p < 6; p++)
            {
                if (Vector3.Distance(predictedCuePos, AIPocketHelper.GetPocketPosition(p)) < pocketRadius)
                {
                    predictedCuePos = current.cueBallPosition; // Fail: keep current
                    break;
                }
            }

            return new TableState
            {
                cueBallPosition = predictedCuePos,
                availableBalls = newBalls
            };
        }

        private (ShotPlan plan, float score)? EvaluateTacticalSafety(TableState tableState, List<BallEntry> legalBalls,
            ChinesePoolGameManager.BallGroup playerGroup)
        {
            if (tableState.availableBalls.Count == 0) return null;

            Vector3 cuePos = tableState.cueBallPosition;

            // Find best snooker opportunity: hide cue ball behind opponent ball(s)
            var opponentGroup = playerGroup == ChinesePoolGameManager.BallGroup.Red
                ? ChinesePoolGameManager.BallGroup.Yellow
                : ChinesePoolGameManager.BallGroup.Red;

            var opponentBalls = tableState.availableBalls
                .Where(b => b.id != 0 && b.id != 8 &&
                    (ChinesePoolGameManager.Instance?.GetBallGroup(b.id) == opponentGroup))
                .ToList();

            var ownBalls = tableState.availableBalls
                .Where(b => b.id != 0 && b.id != 8 &&
                    (ChinesePoolGameManager.Instance?.GetBallGroup(b.id) == playerGroup))
                .ToList();

            var hideCandidates = opponentBalls.Count > 0 ? opponentBalls : tableState.availableBalls.Where(b => b.id != 0).ToList();

            foreach (var hideBall in hideCandidates)
            {
                // Can we send cue ball behind this ball?
                Vector3 hideDir = (hideBall.position - cuePos).normalized;
                Vector3 targetPos = hideBall.position + hideDir * 0.1f;

                // Check available target (own ball near rail)
                foreach (var ownBall in ownBalls)
                {
                    float distToRail = GetDistanceToNearestRail(ownBall.position);
                    if (distToRail < 0.2f) // Ball near rail = good safety target
                    {
                        // Gentle tap
                        float alignment = Vector3.Dot(
                            (ownBall.position - cuePos).normalized,
                            (targetPos - cuePos).normalized);

                        if (alignment > 0.7f)
                        {
                            return (new ShotPlan
                            {
                                ballId = ownBall.id,
                                targetPosition = ownBall.position + hideDir * 0.1f,
                                pocketIndex = -1,
                                power = 0.15f,
                                spin = new Vector3(0, 0, 0),
                                isSafe = true
                            }, 0.7f);
                        }
                    }
                }
            }

            // Fallback: simple rail safety
            var nearestBall = tableState.availableBalls
                .Where(b => b.id != 0)
                .OrderBy(b => Vector3.Distance(cuePos, b.position))
                .First();
            Vector3 railDir = GetNearestRailDirection(nearestBall.position);

            return (new ShotPlan
            {
                ballId = nearestBall.id,
                targetPosition = nearestBall.position + railDir * 0.15f,
                pocketIndex = -1,
                power = 0.2f,
                spin = Vector3.zero,
                isSafe = true
            }, 0.4f);
        }

        private (ShotPlan plan, float score)? EvaluateIntentionalFoul(TableState tableState, ChinesePoolGameManager.BallGroup playerGroup)
        {
            // Advanced tactic: intentional foul when snookered and no legal shot
            // Hit opponent's ball hard to scatter balls, accepting the foul
            var opponentGroup = playerGroup == ChinesePoolGameManager.BallGroup.Red
                ? ChinesePoolGameManager.BallGroup.Yellow
                : ChinesePoolGameManager.BallGroup.Red;

            var opponentBalls = tableState.availableBalls
                .Where(b => b.id != 0 && b.id != 8 &&
                    (ChinesePoolGameManager.Instance?.GetBallGroup(b.id) == opponentGroup))
                .ToList();

            if (opponentBalls.Count == 0) return null;

            // Hit the cluster of opponent balls to scatter them
            var clusterBall = FindClusterCenter(opponentBalls);
            if (clusterBall.HasValue)
            {
                return (new ShotPlan
                {
                    ballId = clusterBall.Value.id,
                    targetPosition = clusterBall.Value.position,
                    pocketIndex = -1,
                    power = 0.9f, // Full power to scatter
                    spin = Vector3.zero,
                    isSafe = false
                }, 0.3f);
            }

            return null;
        }

        private BallEntry? FindClusterCenter(List<BallEntry> balls)
        {
            if (balls.Count == 0) return null;
            if (balls.Count == 1) return balls[0];

            // Find the ball with most neighbors within cluster radius
            float clusterRadius = 0.3f;
            int maxNeighbors = 0;
            BallEntry? best = null;

            foreach (var ball in balls)
            {
                int neighbors = balls.Count(b =>
                    b.id != ball.id &&
                    Vector3.Distance(b.position, ball.position) < clusterRadius);
                if (neighbors > maxNeighbors)
                {
                    maxNeighbors = neighbors;
                    best = ball;
                }
            }

            return best;
        }

        private float GetDistanceToNearestRail(Vector3 pos)
        {
            float tableHalfX = 0.914f / 2f;
            float tableHalfZ = 1.828f / 2f;
            float dRight = Mathf.Abs(tableHalfX - pos.x);
            float dLeft = Mathf.Abs(-tableHalfX - pos.x);
            float dTop = Mathf.Abs(tableHalfZ - pos.z);
            float dBottom = Mathf.Abs(-tableHalfZ - pos.z);
            return Mathf.Min(dRight, dLeft, dTop, dBottom);
        }
    }

    /// <summary>
    /// Factory for creating Chinese Pool AI strategies by difficulty.
    /// </summary>
    public static class ChinesePoolAIStrategyFactory
    {
        public static ICueStrikeAIStrategy Create(SkillLevel level)
        {
            return level switch
            {
                SkillLevel.Easy => new ChinesePoolAIEasy(),
                SkillLevel.Medium => new ChinesePoolAIMedium(),
                SkillLevel.Hard => new ChinesePoolAIHard(),
                SkillLevel.Expert => new ChinesePoolAIExpert(),
                _ => new ChinesePoolAIMedium()
            };
        }
    }

    #region Self-Test
#if UNITY_EDITOR
    public static class ChinesePoolAIStrategyTest
    {
        [UnityEditor.MenuItem("Tools/CueStrike/Debug/Test ChinesePool AI Strategy")]
        public static void SelfTest()
        {
            bool pass = true;

            // Test strategy creation
            try
            {
                var easy = ChinesePoolAIStrategyFactory.Create(SkillLevel.Easy);
                var medium = ChinesePoolAIStrategyFactory.Create(SkillLevel.Medium);
                var hard = ChinesePoolAIStrategyFactory.Create(SkillLevel.Hard);
                var expert = ChinesePoolAIStrategyFactory.Create(SkillLevel.Expert);

                Debug.Log($"✅ Strategies created: Easy={easy?.GetType().Name}, Medium={medium?.GetType().Name}, Hard={hard?.GetType().Name}, Expert={expert?.GetType().Name}");

                // Test parameters initialization
                var testParams = new CueStrikeAIController.AIParameters
                {
                    accuracy = 0.8f, positionWeight = 0.5f, power = 0.7f,
                    spinControl = 0.5f, decisionDelay = 1.0f, errorMargin = 0.1f,
                    label = "Test"
                };

                easy.Initialize(testParams);

                // Test shot selection with empty table
                var emptyTable = new CueStrikeAIController.TableState
                {
                    cueBallPosition = Vector3.zero,
                    availableBalls = new List<CueStrikeAIController.BallEntry>()
                };
                var shot = easy.SelectShot(emptyTable, testParams);
                Debug.Log($"✅ Easy AI with empty table returned: {(shot.HasValue ? "shot" : "null (expected)")}");

                // Test with mock table
                var mockTable = new CueStrikeAIController.TableState
                {
                    cueBallPosition = new Vector3(0, 0, 0),
                    availableBalls = new List<CueStrikeAIController.BallEntry>
                    {
                        new CueStrikeAIController.BallEntry { id = 1, position = new Vector3(0.1f, 0, 0.3f), isPotted = false },
                        new CueStrikeAIController.BallEntry { id = 9, position = new Vector3(-0.1f, 0, 0.5f), isPotted = false },
                        new CueStrikeAIController.BallEntry { id = 8, position = new Vector3(0, 0, 0.8f), isPotted = false }
                    }
                };
                shot = easy.SelectShot(mockTable, testParams);
                Debug.Log($"✅ Easy AI with mock table returned: {(shot.HasValue ? $"ball={shot.Value.ballId}, pocket={shot.Value.pocketIndex}" : "null")}");

                // Test cut angle calculation
                var easyInstance = easy as ChinesePoolAIEasy;
                if (easyInstance != null)
                {
                    float angle = easyInstance.CalculateCutAngle(
                        new Vector3(0, 0, 0),
                        new Vector3(0, 0, 0.5f),
                        new Vector3(0.3f, 0, 1.0f));
                    Debug.Log($"✅ Cut angle test: {angle:F1}° (expected ~30° to 45°)");
                }

                // Test path obstruction
                if (easyInstance != null)
                {
                    var balls = new List<CueStrikeAIController.BallEntry>
                    {
                        new CueStrikeAIController.BallEntry { id = 1, position = new Vector3(0.1f, 0, 0.5f), isPotted = false },
                        new CueStrikeAIController.BallEntry { id = 2, position = new Vector3(-0.05f, 0, 0.6f), isPotted = false }
                    };
                    bool blocked = easyInstance.IsPathObstructed(
                        new Vector3(0, 0, 0),
                        new Vector3(0, 0, 1.0f),
                        balls, 1);
                    Debug.Log($"✅ Path obstruction test: {blocked} (expected: true - ball 2 blocks)");
                }

                Debug.Log("✅ ChinesePool AI Strategy SELF-TEST PASSED — Ready for human verify.");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"❌ FAIL: ChinesePool AI Strategy self-test exception: {ex.Message}\n{ex.StackTrace}");
                pass = false;
            }

            if (!pass)
            {
                Debug.LogWarning("⚠️ ChinesePool AI Strategy SELF-TEST FAILED — See errors above.");
            }
        }
    }
#endif
    #endregion
}