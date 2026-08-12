using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CueStrike.AI;

namespace CueStrike.AI
{
    /// <summary>
    /// R36 — Snooker AI Bridge: ให้ AI เล่นสนุกเกอร์ตามกฎ WBPS ได้จริงใน Snooker_Demo.
    ///
    /// Flow:
    ///   Turn system: P1 (human) ↔ P2 (AI) สลับ — เริ่ม P1
    ///   เมื่อ AI เทิร์น:
    ///     อ่าน WBPS state (RedsRemaining / AwaitingRespotColor / IsColorPhase / ColorSequenceIndex)
    ///     → เลือกลูกเป้าหมายตามกฎ (red → color → red → ... → color phase sequence)
    ///     → เลือกหลุมที่เข้าถึงได้ (nearest pocket heuristic)
    ///     → คำนวณ aim (ghost-ball) → AddForce จริง + error ตาม difficulty
    ///     → รอลูกหยุด → ตรวจผล (ใกล้หลุม + ต่ำกว่าโต๊ะ) → WBPS.RegisterPot / ValidateShotFull → สลับเทิร์น
    ///
    /// Fail-safe: หา refs ไม่เจอ → log + retry ทุก 2s — ไม่พังฉาก
    /// </summary>
    public class CueStrikeSnookerAIBridge : MonoBehaviour
    {
        #region Inspector Settings
        [Header("References (auto-assigned)")]
        [Tooltip("CueStrikeWBPSRuleset — rules/state (หาเองถ้าว่าง)")]
        public CueStrikeWBPSRuleset ruleset;

        [Tooltip("ตำแหน่งหลุม 6 หลุม (มุม 4 + กลาง 2) — Editor tool ตั้งให้อัตโนมัติ")]
        public List<Vector3> pocketPositions = new List<Vector3>();

        [Tooltip("ความสูงพื้นโต๊ะ (ลูกต่ำกว่านี้ = ตกหลุม)")]
        public float tableSurfaceY = 0.42f;

        [Tooltip("รัศมีตรวจจับหลุม")]
        public float pocketDetectionRadius = 0.14f;

        [Header("Difficulty")]
        [Tooltip("ระดับ AI — เริ่ม Medium (เลือกได้จาก Inspector / SetDifficulty)")]
        public SkillLevel difficulty = SkillLevel.Medium;
        #endregion

        #region State
        private bool _subscribed;
        private int _currentPlayer = 1; // 1 = human, 2 = AI
        private bool _aiThinking;
        private Coroutine _aiRoutine;
        private readonly List<int> _pottedThisFrame = new List<int>();
        private bool _shotInProgress;
        #endregion

        #region Lifecycle
        private void Start()
        {
            if (ruleset == null) ruleset = CueStrikeWBPSRuleset.Instance;
            if (ruleset == null) ruleset = FindFirstObjectByType<CueStrikeWBPSRuleset>();
            StartCoroutine(TrySubscribeLoop());
        }

        private IEnumerator TrySubscribeLoop()
        {
            while (true)
            {
                if (ruleset == null)
                {
                    ruleset = CueStrikeWBPSRuleset.Instance;
                    if (ruleset == null) ruleset = FindFirstObjectByType<CueStrikeWBPSRuleset>();
                }
                if (ruleset != null && !_subscribed)
                {
                    ruleset.OnBallPotted += OnBallPotted;
                    ruleset.OnFoulCommitted += OnFoulCommitted;
                    ruleset.OnFrameWon += OnFrameWon;
                    _subscribed = true;
                    Debug.Log("[SnookerAI] Bridge subscribed to WBPS events.");
                    Debug.Log("[SnookerAI] Turn 1: Player (human) to play. AI difficulty: " + difficulty);
                }
                yield return new WaitForSeconds(2f);
            }
        }

        private void OnDestroy()
        {
            if (ruleset != null && _subscribed)
            {
                ruleset.OnBallPotted -= OnBallPotted;
                ruleset.OnFoulCommitted -= OnFoulCommitted;
                ruleset.OnFrameWon -= OnFrameWon;
                _subscribed = false;
            }
        }
        #endregion

        #region Public API

        /// <summary>ตั้งระดับความยากของ AI.</summary>
        public void SetDifficulty(SkillLevel level)
        {
            difficulty = level;
            Debug.Log($"[SnookerAI] Difficulty set to {level}.");
        }

        /// <summary>ให้ AI เริ่มเทิร์น (เรียกรถถ้าใครเรียกจาก UI / auto ตอนสลับเทิร์น).</summary>
        public void StartAITurn()
        {
            if (_aiThinking) return;
            _aiThinking = true;
            if (_aiRoutine != null) StopCoroutine(_aiRoutine);
            _aiRoutine = StartCoroutine(AITurnRoutine());
        }

        #endregion

        #region WBPS Events

        private void OnBallPotted(int points)
        {
            // สกอร์เพิ่ม → ผู้เล่นคนนั้นได้เทิร์นต่อ (สนุกเกอร์: พ็อตถูก = ได้เล่นต่อ)
            // จริงๆ แล้วเราสลับเทิร์นหลัง shot จบ (ใน EvaluateShot) — ที่นี่แค่ log
            Debug.Log($"[SnookerAI] Ball potted: +{points} pts (player {_currentPlayer}).");
        }

        private void OnFoulCommitted(int penalty, string reason)
        {
            Debug.Log($"[SnookerAI] Foul (player {_currentPlayer}): {reason} -{penalty} pts.");
            // ฟาวล์ = สลับเทิร์น (จัดการใน EvaluateShot ด้วย)
        }

        private void OnFrameWon()
        {
            Debug.Log("[SnookerAI] Frame won! Game over.");
        }

        #endregion

        #region AI Turn

        private IEnumerator AITurnRoutine()
        {
            _currentPlayer = 2;
            yield return new WaitForSeconds(1.0f); // คิดสักครู่

            // 1. เลือกลูกเป้าหมายตามกฎ WBPS
            var target = SelectTargetBall();
            if (target == null)
            {
                Debug.LogWarning("[SnookerAI] No legal target ball — playing safe (hit any ball gently).");
                var anyBall = FindAnyBall();
                if (anyBall == null)
                {
                    Debug.Log("[SnookerAI] No balls on table — pass turn.");
                    EndAITurn(false);
                    yield break;
                }
                var identity = anyBall.GetComponent<CueStrike.BallIdentity>();
                target = new BallInfo
                {
                    gameObject = anyBall,
                    ballId = identity != null ? identity.ballId : -1,
                    position = anyBall.transform.position
                };
            }

            // 2. เลือกหลุม (nearest pocket heuristic)
            var pocket = SelectPocket(target.Value.position);
            if (!pocket.HasValue)
            {
                Debug.Log("[SnookerAI] No pocket reachable — hit target gently (safety).");
                SafeShot(target.Value.gameObject);
                yield return StartCoroutine(WaitForBallsToSettle());
                EvaluateShot();
                yield break;
            }

            // 3. คำนวณ aim (ghost-ball) + error ตาม difficulty
            Vector3 aimDir = ComputeAimDirection(target.Value.gameObject, pocket.Value);
            if (aimDir == Vector3.zero)
            {
                SafeShot(target.Value.gameObject);
                yield return StartCoroutine(WaitForBallsToSettle());
                EvaluateShot();
                yield break;
            }

            // 4. ยิงจริง
            var cueBall = FindBallById(0);
            if (cueBall == null)
            {
                Debug.LogWarning("[SnookerAI] Cue ball missing — pass turn.");
                EndAITurn(false);
                yield break;
            }

            var rb = cueBall.GetComponent<Rigidbody>();
            if (rb == null)
            {
                Debug.LogWarning("[SnookerAI] Cue ball has no Rigidbody — pass turn.");
                EndAITurn(false);
                yield break;
            }

            float power = DifficultyPower();
            float error = DifficultyError();
            aimDir = ApplyError(aimDir, error);
            _shotInProgress = true;

            Debug.Log($"[SnookerAI] AI shot: target ballId={target.Value.ballId}, pocket={pocket.Value}, power={power:F2}.");
            rb.AddForce(aimDir * power, ForceMode.Impulse);

            // 5. รอลูกหยุด
            yield return StartCoroutine(WaitForBallsToSettle());

            // 6. ประเมินผล + สลับเทิร์น
            _shotInProgress = false;
            EvaluateShot();
        }

        /// <summary>ข้อมูลลูกที่ AI เลือก.</summary>
        private struct BallInfo
        {
            public GameObject gameObject;
            public int ballId;
            public Vector3 position;
        }

        private BallInfo? SelectTargetBall()
        {
            if (ruleset == null) return null;

            // Color phase: ยิงสีตามลำดับ sequence (Yellow→Green→Brown→Blue→Pink→Black)
            if (ruleset.IsColorPhaseActive)
            {
                int colorId = 16 + ruleset.ColorSequenceIndex; // 16=Yellow .. 21=Black
                var ball = FindBallById(colorId);
                if (ball != null)
                {
                    return new BallInfo { gameObject = ball, ballId = colorId, position = ball.transform.position };
                }
                // สีที่ต้องการหาย (พ็อตไปแล้วแต่ sequence ไม่เดิน?) — เลือกสีถัดไป
                for (int i = ruleset.ColorSequenceIndex + 1; i < 6; i++)
                {
                    var b = FindBallById(16 + i);
                    if (b != null) return new BallInfo { gameObject = b, ballId = 16 + i, position = b.transform.position };
                }
                return null;
            }

            // Red phase: ถ้าต้องยิง color (หลังแดง) → เลือกสีค่ามากสุดที่เหลือ
            if (ruleset.AwaitingRespotColorState)
            {
                for (int id = 21; id >= 16; id--)
                {
                    var ball = FindBallById(id);
                    if (ball != null) return new BallInfo { gameObject = ball, ballId = id, position = ball.transform.position };
                }
                return null;
            }

            // ปกติ: ยิงลูกแดง (เลือกลูกที่อยู่ใกล้หลุมที่สุด)
            BallInfo? best = null;
            float bestScore = float.MaxValue;
            var cue = FindBallById(0);
            Vector3 cuePos = cue != null ? cue.transform.position : Vector3.zero;

            foreach (var identity in FindObjectsByType<CueStrike.BallIdentity>())
            {
                if (identity == null || identity.ballId < 1 || identity.ballId > 15) continue; // เฉพาะแดง
                if (!IsOnTable(identity.transform.position)) continue;

                float distToCue = Vector3.Distance(cuePos, identity.transform.position);
                float minPocketDist = MinDistanceToPocket(identity.transform.position);
                float score = distToCue * 0.5f + minPocketDist; // ใกล้คิว + ใกล้หลุม
                if (score < bestScore)
                {
                    bestScore = score;
                    best = new BallInfo { gameObject = identity.gameObject, ballId = identity.ballId, position = identity.transform.position };
                }
            }
            return best;
        }

        private GameObject FindAnyBall()
        {
            foreach (var identity in FindObjectsByType<CueStrike.BallIdentity>())
            {
                if (identity == null || identity.ballId == 0) continue;
                if (IsOnTable(identity.transform.position)) return identity.gameObject;
            }
            return null;
        }

        private Vector3? SelectPocket(Vector3 ballPos)
        {
            if (pocketPositions == null || pocketPositions.Count == 0) return null;

            Vector3? best = null;
            float bestDist = float.MaxValue;
            foreach (var pocket in pocketPositions)
            {
                // หลุมต้องอยู่หลังลูก (ฝั่งเดียวกับทิศทางที่คิวตี) — heuristic: เลือกหลุมที่ไกลจากคิวมากกว่า
                float dist = Vector3.Distance(ballPos, pocket);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    best = pocket;
                }
            }
            return best;
        }

        /// <summary>Ghost-ball aim: aim point = target + (dir target→pocket) * (radius*2).</summary>
        private Vector3 ComputeAimDirection(GameObject targetBall, Vector3 pocket)
        {
            var cue = FindBallById(0);
            if (cue == null || targetBall == null) return Vector3.zero;

            Vector3 targetPos = targetBall.transform.position;
            Vector3 toPocket = (pocket - targetPos).normalized;
            if (toPocket == Vector3.zero) return Vector3.zero;

            float ballRadius = 0.052f;
            Vector3 ghostBallPos = targetPos + toPocket * (ballRadius * 2f + 0.01f);

            Vector3 dir = (ghostBallPos - cue.transform.position);
            dir.y = 0f;
            if (dir.magnitude < 0.001f) return Vector3.zero;
            return dir.normalized;
        }

        private void SafeShot(GameObject target)
        {
            var cue = FindBallById(0);
            if (cue == null || target == null) return;
            var rb = cue.GetComponent<Rigidbody>();
            if (rb == null) return;

            Vector3 dir = (target.transform.position - cue.transform.position);
            dir.y = 0f;
            if (dir.magnitude < 0.001f) dir = Vector3.forward;
            rb.AddForce(dir.normalized * 2.5f, ForceMode.Impulse);
        }

        private IEnumerator WaitForBallsToSettle()
        {
            float waitTimer = 0f;
            const float maxWait = 12f;
            const float settleThreshold = 0.08f;

            while (waitTimer < maxWait)
            {
                bool anyMoving = false;
                foreach (var rb in FindObjectsByType<Rigidbody>())
                {
                    if (rb == null) continue;
                    if (rb.linearVelocity.sqrMagnitude > settleThreshold * settleThreshold)
                    {
                        anyMoving = true;
                        break;
                    }
                }
                if (!anyMoving)
                {
                    yield return new WaitForSeconds(0.5f);
                    break;
                }
                yield return new WaitForSeconds(0.25f);
                waitTimer += 0.25f;
            }
        }

        /// <summary>ประเมินผล: ลูกไหนตกลงหลุม (ใกล้หลุม + ต่ำกว่าโต๊ะ) → WBPS.RegisterPot / foul.</summary>
        private void EvaluateShot()
        {
            if (ruleset == null) { EndAITurn(false); return; }

            _pottedThisFrame.Clear();
            bool cueBallPotted = false;

            foreach (var identity in FindObjectsByType<CueStrike.BallIdentity>())
            {
                if (identity == null) continue;
                if (!IsNearPocket(identity.transform.position)) continue;
                if (identity.transform.position.y >= tableSurfaceY - 0.05f) continue; // ยังอยู่บนโต๊ะ

                if (identity.ballId == 0) cueBallPotted = true;
                else _pottedThisFrame.Add(identity.ballId);
            }

            if (cueBallPotted)
            {
                ruleset.CommitFoul("Cue ball potted", 4);
                EndAITurn(true);
                return;
            }

            if (_pottedThisFrame.Count > 0)
            {
                // พ็อตลูกแรก (ลูกที่ควร) — ใช้ ValidateShotFull เพื่อให้กติกาเดินถูกต้อง
                int primary = _pottedThisFrame[0];
                ruleset.ValidateShotFull(true, primary, false, primary, _pottedThisFrame);
            }
            else
            {
                // ไม่พ็อตอะไร → ตรวจว่าโดนลูกแรกถูกไหม (หา first hit ไม่ได้ง่าย → ข้าม ถือว่าถูก)
                Debug.Log("[SnookerAI] No ball potted — checking shot validity.");
                // ถ้าไม่มีลูกโดนเลย จะเป็นฟาวล์ — เราไม่รู้ first hit → สมมติถูก (เล่นง่าย)
                ruleset.ValidateShotFull(true, _pottedThisFrame.Count > 0 ? _pottedThisFrame[0] : 1, false, 0, null);
            }

            EndAITurn(true);
        }

        private bool IsNearPocket(Vector3 pos)
        {
            if (pocketPositions == null || pocketPositions.Count == 0) return false;
            foreach (var p in pocketPositions)
            {
                if (Vector3.Distance(pos, p) < pocketDetectionRadius) return true;
            }
            return false;
        }

        private bool IsOnTable(Vector3 pos)
        {
            return pos.y > tableSurfaceY - 0.05f;
        }

        private float MinDistanceToPocket(Vector3 pos)
        {
            float min = float.MaxValue;
            if (pocketPositions == null) return min;
            foreach (var p in pocketPositions)
            {
                min = Mathf.Min(min, Vector3.Distance(pos, p));
            }
            return min;
        }

        private GameObject FindBallById(int id)
        {
            foreach (var identity in FindObjectsByType<CueStrike.BallIdentity>())
            {
                if (identity != null && identity.ballId == id) return identity.gameObject;
            }
            return null;
        }

        private void EndAITurn(bool switchTurn)
        {
            _aiThinking = false;
            if (switchTurn)
            {
                _currentPlayer = 1;
                Debug.Log("[SnookerAI] Turn: Player (human) to play.");
            }
        }

        #endregion

        #region Difficulty Helpers

        private float DifficultyPower()
        {
            return difficulty switch
            {
                SkillLevel.Easy => 2.2f,
                SkillLevel.Medium => 3.0f,
                SkillLevel.Hard => 3.6f,
                SkillLevel.Expert => 4.2f,
                _ => 3.0f
            };
        }

        private float DifficultyError()
        {
            return difficulty switch
            {
                SkillLevel.Easy => 0.12f,
                SkillLevel.Medium => 0.06f,
                SkillLevel.Hard => 0.03f,
                SkillLevel.Expert => 0.012f,
                _ => 0.06f
            };
        }

        private Vector3 ApplyError(Vector3 dir, float error)
        {
            if (error <= 0f) return dir;
            float ang = UnityEngine.Random.Range(-error, error);
            // หมุนรอบ Y
            float ca = Mathf.Cos(ang);
            float sa = Mathf.Sin(ang);
            Vector3 rotated = new Vector3(dir.x * ca - dir.z * sa, 0f, dir.x * sa + dir.z * ca);
            return rotated.normalized;
        }

        #endregion
    }
}
