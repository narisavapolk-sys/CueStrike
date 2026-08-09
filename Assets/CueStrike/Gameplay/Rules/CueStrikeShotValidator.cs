using System;
using System.Collections.Generic;
using UnityEngine;

namespace CueStrike.Gameplay.Rules
{
    /// <summary>
    /// Shot validation system for WPA rules compliance.
    /// Tracks ball contacts, cushion hits, and validates shot legality.
    /// </summary>
    public class CueStrikeShotValidator : MonoBehaviour
    {
        public static CueStrikeShotValidator Instance { get; private set; }

        // Events
        public event Action<List<int>> OnBallsContacted;
        public event Action<int> OnCueBallContactedObjectBall;
        public event Action<bool> OnBallHitCushion;
        public event Action<int, int> OnBallPotted; // ballId, pocketIndex
        public event Action<int> OnCueBallPotted;
        public event Action<int> OnBallOffTable;

        // Shot tracking state
        private List<int> _ballsContactedThisShot = new List<int>();
        private bool _cueBallHitObjectBall = false;
        private bool _anyBallHitCushion = false;
        private int _firstObjectBallContacted = -1;
        private HashSet<int> _pottedBallsThisShot = new HashSet<int>();
        private bool _cueBallPottedThisShot = false;
        private bool _shotInProgress = false;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        /// <summary>
        /// Starts tracking a new shot.
        /// </summary>
        public void BeginShot()
        {
            _ballsContactedThisShot.Clear();
            _cueBallHitObjectBall = false;
            _anyBallHitCushion = false;
            _firstObjectBallContacted = -1;
            _pottedBallsThisShot.Clear();
            _cueBallPottedThisShot = false;
            _shotInProgress = true;
        }

        /// <summary>
        /// Ends shot tracking and fires events.
        /// </summary>
        public void EndShot()
        {
            if (!_shotInProgress) return;

            _shotInProgress = false;

            // Fire contacted balls event
            if (_ballsContactedThisShot.Count > 0)
            {
                OnBallsContacted?.Invoke(new List<int>(_ballsContactedThisShot));
            }

            // Fire cushion event
            OnBallHitCushion?.Invoke(_anyBallHitCushion);

            // Fire potted balls events
            foreach (int ballId in _pottedBallsThisShot)
            {
                // Pocket index would need to be tracked separately in full implementation
                OnBallPotted?.Invoke(ballId, -1);
            }

            if (_cueBallPottedThisShot)
            {
                OnCueBallPotted?.Invoke(0);
            }
        }

        /// <summary>
        /// Records a ball-ball collision.
        /// </summary>
        public void RecordCollision(int ballIdA, int ballIdB)
        {
            if (!_shotInProgress) return;

            // Check if cue ball (0) hit an object ball
            if (ballIdA == 0 && ballIdB >= 1)
            {
                RecordCueBallContact(ballIdB);
            }
            else if (ballIdB == 0 && ballIdA >= 1)
            {
                RecordCueBallContact(ballIdA);
            }

            // Track all balls that made contact
            if (ballIdA >= 1 && !_ballsContactedThisShot.Contains(ballIdA))
            {
                _ballsContactedThisShot.Add(ballIdA);
            }
            if (ballIdB >= 1 && !_ballsContactedThisShot.Contains(ballIdB))
            {
                _ballsContactedThisShot.Add(ballIdB);
            }
        }

        /// <summary>
        /// Records cue ball contacting an object ball.
        /// </summary>
        private void RecordCueBallContact(int objectBallId)
        {
            if (!_cueBallHitObjectBall)
            {
                _cueBallHitObjectBall = true;
                _firstObjectBallContacted = objectBallId;
                OnCueBallContactedObjectBall?.Invoke(objectBallId);
            }
        }

        /// <summary>
        /// Records a ball hitting a cushion.
        /// </summary>
        public void RecordCushionHit(int ballId)
        {
            if (!_shotInProgress) return;

            if (!_anyBallHitCushion)
            {
                _anyBallHitCushion = true;
            }
        }

        /// <summary>
        /// Records a ball being potted.
        /// </summary>
        public void RecordBallPotted(int ballId, int pocketIndex = -1)
        {
            if (!_shotInProgress) return;

            if (ballId == 0)
            {
                _cueBallPottedThisShot = true;
            }
            else
            {
                _pottedBallsThisShot.Add(ballId);
            }
        }

        /// <summary>
        /// Records a ball going off the table.
        /// </summary>
        public void RecordBallOffTable(int ballId)
        {
            if (!_shotInProgress) return;
            OnBallOffTable?.Invoke(ballId);
        }

        /// <summary>
        /// Gets the list of balls contacted this shot (in order of contact).
        /// </summary>
        public List<int> GetBallsContacted() => new List<int>(_ballsContactedThisShot);

        /// <summary>
        /// Gets the first object ball contacted by cue ball.
        /// </summary>
        public int GetFirstObjectBallContacted() => _firstObjectBallContacted;

        /// <summary>
        /// Checks if cue ball hit an object ball.
        /// </summary>
        public bool DidCueBallHitObjectBall() => _cueBallHitObjectBall;

        /// <summary>
        /// Checks if any ball hit a cushion.
        /// </summary>
        public bool DidAnyBallHitCushion() => _anyBallHitCushion;

        /// <summary>
        /// Gets potted balls this shot.
        /// </summary>
        public HashSet<int> GetPottedBalls() => new HashSet<int>(_pottedBallsThisShot);

        /// <summary>
        /// Checks if cue ball was potted.
        /// </summary>
        public bool WasCueBallPotted() => _cueBallPottedThisShot;

        /// <summary>
        /// Checks if shot is currently being tracked.
        /// </summary>
        public bool IsShotInProgress() => _shotInProgress;

        /// <summary>
        /// Validates shot legality for 8-Ball.
        /// </summary>
        public CueStrikeEightBallWPARuleset.ShotResult ValidateEightBallShot(
            int playerIndex,
            bool isBreakShot,
            bool isOpenTable,
            CueStrikeEightBallWPARuleset.BallGroup playerGroup,
            int targetBallId,
            int pocketIndex)
        {
            // Cue ball potted = foul
            if (_cueBallPottedThisShot)
            {
                return CueStrikeEightBallWPARuleset.ShotResult.Foul;
            }

            // No object ball contacted = foul
            if (!_cueBallHitObjectBall)
            {
                return CueStrikeEightBallWPARuleset.ShotResult.Foul;
            }

            // Break shot validation
            if (isBreakShot)
            {
                // Legal break: 4 balls to cushion OR ball potted
                bool legalBreak = _anyBallHitCushion || _pottedBallsThisShot.Count > 0;
                if (!legalBreak)
                {
                    return CueStrikeEightBallWPARuleset.ShotResult.Foul;
                }
                return CueStrikeEightBallWPARuleset.ShotResult.Legal;
            }

            // Open table validation
            if (isOpenTable)
            {
                // 8-ball potted on open table = loss (handled by ruleset)
                if (targetBallId == 8)
                {
                    return CueStrikeEightBallWPARuleset.ShotResult.Loss;
                }

                // Must hit an object ball first (any)
                if (_firstObjectBallContacted < 1 || _firstObjectBallContacted > 15)
                {
                    return CueStrikeEightBallWPARuleset.ShotResult.Foul;
                }

                // Cushion rule
                if (!_anyBallHitCushion && _pottedBallsThisShot.Count == 0)
                {
                    return CueStrikeEightBallWPARuleset.ShotResult.Foul;
                }

                return CueStrikeEightBallWPARuleset.ShotResult.Legal;
            }

            // Normal play (groups assigned)
            // Must hit own group ball first (or 8-ball if group cleared)
            if (playerGroup != CueStrikeEightBallWPARuleset.BallGroup.Unassigned)
            {
                bool hitOwnGroup = IsBallInGroup(_firstObjectBallContacted, playerGroup);

                // If group cleared, can hit 8-ball first
                bool groupCleared = IsGroupCleared(playerGroup);
                if (groupCleared && _firstObjectBallContacted == 8)
                {
                    hitOwnGroup = true;
                }

                if (!hitOwnGroup)
                {
                    return CueStrikeEightBallWPARuleset.ShotResult.Foul;
                }
            }

            // Cushion rule
            if (!_anyBallHitCushion && _pottedBallsThisShot.Count == 0)
            {
                return CueStrikeEightBallWPARuleset.ShotResult.Foul;
            }

            // 8-ball pocketed validation
            if (_pottedBallsThisShot.Contains(8))
            {
                // Check if group cleared
                if (!IsGroupCleared(playerGroup))
                {
                    return CueStrikeEightBallWPARuleset.ShotResult.Loss;
                }

                // Check called pocket (simplified)
                // In full implementation: verify pocketIndex matches called pocket
                return CueStrikeEightBallWPARuleset.ShotResult.Win;
            }

            return CueStrikeEightBallWPARuleset.ShotResult.Legal;
        }

        /// <summary>
        /// Validates shot legality for 9-Ball.
        /// </summary>
        public CueStrikeNineBallWPARuleset.ShotResult ValidateNineBallShot(
            int playerIndex,
            bool isBreakShot,
            CueStrikeNineBallWPARuleset.PushOutState pushOutState,
            int lowestBallOnTable,
            int targetBallId,
            int pocketIndex)
        {
            // Cue ball potted = foul
            if (_cueBallPottedThisShot)
            {
                return CueStrikeNineBallWPARuleset.ShotResult.Foul;
            }

            // No object ball contacted = foul
            if (!_cueBallHitObjectBall)
            {
                return CueStrikeNineBallWPARuleset.ShotResult.Foul;
            }

            // Break shot validation
            if (isBreakShot)
            {
                // Legal break: 4 balls to cushion OR ball potted
                bool legalBreak = _anyBallHitCushion || _pottedBallsThisShot.Count > 0;

                // 9-ball on break
                if (_pottedBallsThisShot.Contains(9))
                {
                    if (legalBreak)
                    {
                        return CueStrikeNineBallWPARuleset.ShotResult.Win;
                    }
                    else
                    {
                        return CueStrikeNineBallWPARuleset.ShotResult.Foul;
                    }
                }

                if (!legalBreak)
                {
                    return CueStrikeNineBallWPARuleset.ShotResult.Foul;
                }

                return CueStrikeNineBallWPARuleset.ShotResult.Legal;
            }

            // Push-out validation
            if (pushOutState == CueStrikeNineBallWPARuleset.PushOutState.Available)
            {
                // During push-out: no requirement to hit lowest ball
                // No cushion requirement
                // But cue ball potted or ball off table still fouls (handled above)

                // 9-ball potted on push-out = spotted (not win)
                if (_pottedBallsThisShot.Contains(9))
                {
                    // Handled by ruleset - ball spotted
                }

                return CueStrikeNineBallWPARuleset.ShotResult.Legal;
            }

            // Normal play: must hit lowest numbered ball first
            if (_firstObjectBallContacted != lowestBallOnTable)
            {
                // 9-ball potted on foul = spotted
                if (_pottedBallsThisShot.Contains(9))
                {
                    return CueStrikeNineBallWPARuleset.ShotResult.Foul;
                }
                return CueStrikeNineBallWPARuleset.ShotResult.Foul;
            }

            // Cushion rule
            if (!_anyBallHitCushion && _pottedBallsThisShot.Count == 0)
            {
                // 9-ball potted but no cushion = foul, 9-ball spotted
                if (_pottedBallsThisShot.Contains(9))
                {
                    return CueStrikeNineBallWPARuleset.ShotResult.Foul;
                }
                return CueStrikeNineBallWPARuleset.ShotResult.Foul;
            }

            // 9-ball legally potted = WIN
            if (_pottedBallsThisShot.Contains(9))
            {
                return CueStrikeNineBallWPARuleset.ShotResult.Win;
            }

            return CueStrikeNineBallWPARuleset.ShotResult.Legal;
        }

        /// <summary>
        /// Checks if a ball belongs to a group (8-Ball).
        /// </summary>
        private bool IsBallInGroup(int ballId, CueStrikeEightBallWPARuleset.BallGroup group)
        {
            if (group == CueStrikeEightBallWPARuleset.BallGroup.Solids)
                return ballId >= 1 && ballId <= 7;
            if (group == CueStrikeEightBallWPARuleset.BallGroup.Stripes)
                return ballId >= 9 && ballId <= 15;
            return false;
        }

        /// <summary>
        /// Checks if a group is completely cleared (simplified).
        /// </summary>
        private bool IsGroupCleared(CueStrikeEightBallWPARuleset.BallGroup group)
        {
            // In full implementation, would check against potted ball tracker
            return true; // Simplified
        }

        /// <summary>
        /// Gets detailed shot validation info for UI/debugging.
        /// </summary>
        public ShotValidationInfo GetValidationInfo()
        {
            return new ShotValidationInfo
            {
                ballsContacted = new List<int>(_ballsContactedThisShot),
                firstObjectBallContacted = _firstObjectBallContacted,
                cueBallHitObjectBall = _cueBallHitObjectBall,
                anyBallHitCushion = _anyBallHitCushion,
                pottedBalls = new HashSet<int>(_pottedBallsThisShot),
                cueBallPotted = _cueBallPottedThisShot,
                shotInProgress = _shotInProgress
            };
        }

        /// <summary>
        /// Shot validation info data structure.
        /// </summary>
        public class ShotValidationInfo
        {
            public List<int> ballsContacted;
            public int firstObjectBallContacted;
            public bool cueBallHitObjectBall;
            public bool anyBallHitCushion;
            public HashSet<int> pottedBalls;
            public bool cueBallPotted;
            public bool shotInProgress;
        }
    }
}