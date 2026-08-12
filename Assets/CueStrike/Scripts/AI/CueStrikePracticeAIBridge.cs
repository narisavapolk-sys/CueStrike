using System;
using System.Collections;
using UnityEngine;
using CueStrike.Gameplay.ChinesePool;
using CueStrike.AI;

namespace CueStrike.AI
{
    /// <summary>
    /// R34 — Practice AI Bridge: ผูก AI opponent เข้ากับ turn flow ของ ChinesePoolGameManager
    /// ให้ผู้เล่นซ้อมกับ AI ในโหมด Practice (ลุงโน๊กเป็นคู่ซ้อม) — เลือกระดับ Easy/Medium/Hard/Expert
    ///
    /// Flow:
    ///   GameManager.OnTurnChanged → เมื่อ isAiTurn (Player 2) →
    ///   ChinesePoolAIModifier.DecideCallShot() → gm.SetCallShot() →
    ///   DecideShotParameters() → ยิงจริง (Rigidbody.AddForce, pattern CueStrikeCue.cs) →
    ///   รอลูกหยุด → ประเมินผล → gm.ProcessShotResult()
    ///
    /// Fail-safe: หา refs ไม่เจอ → log + retry ทุก 2s — ไม่พังฉาก
    /// </summary>
    public class CueStrikePracticeAIBridge : MonoBehaviour
    {
        #region Inspector Settings
        [Header("AI References (auto-assigned)")]
        [Tooltip("ChinesePoolAIModifier — ตัดสินใจ call shot + shot parameters (stub ที่มีอยู่)")]
        public ChinesePoolAIModifier aiModifier;

        [Tooltip("CueStrikeAIController — master AI skill level (มีอยู่, 4 ระดับ)")]
        public CueStrikeAIController aiController;

        [Tooltip("CueStrikeShotManager — ถ้ามีในฉาก (สำหรับช็อตขั้นสูง)")]
        public CueStrikeShotManager shotManager;

        [Header("Difficulty (เริ่มต้นจาก PlayerPrefs)")]
        public SkillLevel defaultDifficulty = SkillLevel.Medium;
        #endregion

        #region State
        private ChinesePoolGameManager _gm;
        private ChinesePoolBallSetup _ballSetup;
        private bool _subscribed;
        private bool _aiThinking;
        private Coroutine _shotRoutine;
        private string _prefsKey = "CueStrike_AIDifficulty";
        #endregion

        #region Lifecycle
        private void Start()
        {
            _gm = ChinesePoolGameManager.Instance;
            if (_gm == null) _gm = FindFirstObjectByType<ChinesePoolGameManager>();

            _ballSetup = FindFirstObjectByType<ChinesePoolBallSetup>();

            // โหลด difficulty ที่เลือกไว้จาก UI
            int saved = PlayerPrefs.GetInt(_prefsKey, (int)defaultDifficulty);
            if (Enum.IsDefined(typeof(SkillLevel), saved))
            {
                SetAIDifficulty((SkillLevel)saved);
            }

            StartCoroutine(TrySubscribeLoop());
        }

        private IEnumerator TrySubscribeLoop()
        {
            while (true)
            {
                if (_gm == null)
                {
                    _gm = ChinesePoolGameManager.Instance;
                    if (_gm == null) _gm = FindFirstObjectByType<ChinesePoolGameManager>();
                }
                if (_ballSetup == null)
                {
                    _ballSetup = FindFirstObjectByType<ChinesePoolBallSetup>();
                }
                if (aiModifier == null)
                {
                    aiModifier = FindFirstObjectByType<ChinesePoolAIModifier>();
                }
                if (aiController == null)
                {
                    aiController = FindFirstObjectByType<CueStrikeAIController>();
                }

                if (_gm != null && !_subscribed)
                {
                    _gm.OnTurnChanged += OnTurnChanged;
                    _subscribed = true;
                    Debug.Log("[CueStrikeAI] Bridge subscribed to GameManager.OnTurnChanged.");
                }

                yield return new WaitForSeconds(2f);
            }
        }

        private void OnDestroy()
        {
            if (_gm != null && _subscribed)
            {
                _gm.OnTurnChanged -= OnTurnChanged;
                _subscribed = false;
            }
        }
        #endregion

        #region Public API

        /// <summary>
        /// ตั้งระดับความยาก (เรียกจาก UI เลือกระดับ) — sync ไปทั้ง modifier + controller.
        /// </summary>
        public void SetAIDifficulty(SkillLevel level)
        {
            if (aiModifier != null)
            {
                aiModifier.SetDifficulty((ChinesePoolAIModifier.AIDifficulty)(int)level);
            }
            if (aiController != null)
            {
                aiController.SetSkillLevel(level);
            }
            defaultDifficulty = level;
            PlayerPrefs.SetInt(_prefsKey, (int)level);
            PlayerPrefs.Save();
            Debug.Log($"[CueStrikeAI] Practice AI difficulty set to {level}.");
        }

        public SkillLevel GetAIDifficulty() => defaultDifficulty;

        #endregion

        #region Turn Flow

        private void OnTurnChanged(int playerIndex)
        {
            if (_gm == null || _gm.aiModifier == null) return;

            // AI turn = Player 2 (index 1) และอยู่ในระหว่างเกม
            if (playerIndex == 1 && _gm.currentPhase != ChinesePoolMatchState.Waiting)
            {
                if (_aiThinking) return;
                _aiThinking = true;

                if (_shotRoutine != null) StopCoroutine(_shotRoutine);
                _shotRoutine = StartCoroutine(AITurnRoutine());
            }
        }

        private IEnumerator AITurnRoutine()
        {
            if (aiModifier == null)
            {
                aiModifier = FindFirstObjectByType<ChinesePoolAIModifier>();
            }
            if (_ballSetup == null)
            {
                _ballSetup = FindFirstObjectByType<ChinesePoolBallSetup>();
            }

            if (aiModifier == null || _ballSetup == null)
            {
                Debug.LogWarning("[CueStrikeAI] AI modifier or ball setup missing — skipping AI turn (fail-safe).");
                _aiThinking = false;
                yield break;
            }

            // 1. คิดสักครู่ (จำลองการคิด)
            yield return new WaitForSeconds(aiModifier.decisionDelay);

            // 2. ตัดสินใจ call shot (ballId, pocketId)
            var callShot = aiModifier.DecideCallShot();
            if (callShot.ballId < 0)
            {
                Debug.Log("[CueStrikeAI] No valid shot — AI plays safe (pass turn).");
                SafePassTurn();
                yield break;
            }

            if (_gm.callShotRequired && _gm.currentPhase != ChinesePoolMatchState.Break)
            {
                _gm.SetCallShot(callShot.ballId, callShot.pocketId);
            }

            // 3. คำนวณ shot parameters
            var shotParams = aiModifier.DecideShotParameters(callShot.ballId, callShot.pocketId);
            if (shotParams.aimPoint == Vector3.zero && shotParams.power <= 0f)
            {
                Debug.LogWarning("[CueStrikeAI] Invalid shot parameters — passing turn (fail-safe).");
                SafePassTurn();
                yield break;
            }

            // 4. ยิงจริง
            var cueBallGO = _ballSetup.GetBallById(0);
            var targetGO = _ballSetup.GetBallById(callShot.ballId);
            if (cueBallGO == null || targetGO == null)
            {
                Debug.LogWarning("[CueStrikeAI] Cue ball / target ball missing — passing turn (fail-safe).");
                SafePassTurn();
                yield break;
            }

            var cueRb = cueBallGO.GetComponent<Rigidbody>();
            if (cueRb == null)
            {
                Debug.LogWarning("[CueStrikeAI] Cue ball has no Rigidbody — passing turn (fail-safe).");
                SafePassTurn();
                yield break;
            }

            // ยิงผ่าน ShotManager ถ้ามี (ช็อตขั้นสูง) — ไม่งั้น AddForce ตรง (pattern CueStrikeCue)
            if (shotManager != null && cueRb != null)
            {
                Vector3 dir = (shotParams.aimPoint - cueBallGO.transform.position).normalized;
                dir.y = 0f;
                shotManager.ExecuteShot(cueRb, dir, Mathf.Max(1f, shotParams.power * 20f), 0f);
            }
            else
            {
                Vector3 dir = (shotParams.aimPoint - cueBallGO.transform.position).normalized;
                dir.y = 0f;
                float force = Mathf.Clamp(shotParams.power, 0.3f, 2.0f) * 3.0f; // impulse scale
                cueRb.AddForce(dir * force, ForceMode.Impulse);
            }

            Debug.Log($"[CueStrikeAI] AI shot: ball={callShot.ballId} → pocket={callShot.pocketId}, power={shotParams.power:F2}");

            // 5. รอลูกหยุดนิ่ง (ประเมินผลโดยรอให้ทุก Rigidbody หยุด)
            yield return StartCoroutine(WaitForBallsToSettle());

            // 6. ประเมินผล + ส่งให้ GameManager (fail-safe: ประเมินไม่ได้ → สลับเทิร์น)
            EvaluateAndReportShot(cueRb, callShot.ballId, callShot.pocketId);

            _aiThinking = false;
        }

        private IEnumerator WaitForBallsToSettle()
        {
            float waitTimer = 0f;
            const float maxWait = 8f;
            const float settleThreshold = 0.05f;

            while (waitTimer < maxWait)
            {
                bool anyMoving = false;
                if (_ballSetup != null)
                {
                    foreach (var id in _ballSetup.GetBallsOnTable())
                    {
                        var go = _ballSetup.GetBallById(id);
                        if (go == null) continue;
                        var rb = go.GetComponent<Rigidbody>();
                        if (rb != null && rb.velocity.magnitude > settleThreshold)
                        {
                            anyMoving = true;
                            break;
                        }
                    }
                }
                if (!anyMoving)
                {
                    yield return new WaitForSeconds(0.5f); // ให้ลูกตั้งหลัก
                    break;
                }
                yield return new WaitForSeconds(0.25f);
                waitTimer += 0.25f;
            }
        }

        private void EvaluateAndReportShot(Rigidbody cueRb, int targetBallId, int pocketId)
        {
            if (_gm == null) return;

            // ประเมินง่ายๆ: เป้าหมายถูกลบออกจากโต๊ะ = หลุมถูกต้อง
            bool targetPotted = _ballSetup != null && _ballSetup.GetBallById(targetBallId) == null;

            var result = new ShotResult
            {
                isFoul = false,
                foulType = "",
                ballPottedId = targetPotted ? targetBallId : -1,
                callShotMatched = targetPotted,
                redBallsPotted = 0,
                yellowBallsPotted = 0,
                cueBallPocketId = -1
            };

            // ถ้าลูกคิวหลุด (หาไม่เจอบนโต๊ะ) → ฟาวล์
            if (_ballSetup != null && _ballSetup.GetBallById(0) == null)
            {
                result.isFoul = true;
                result.foulType = "CueBallPotted";
            }

            _gm.ProcessShotResult(result);
        }

        private void SafePassTurn()
        {
            if (_gm == null) return;
            _gm.NextPlayer();
            _aiThinking = false;
        }

        #endregion
    }
}
