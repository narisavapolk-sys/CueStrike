using UnityEngine;
using CueStrike.UI;

namespace CueStrike.MascotSystem
{
    /// <summary>
    /// R32 — Bo Comedy Director
    /// โมเมนต์ตลกของน้องโบโดยใช้ animation ที่มีอยู่แล้ว (R27):
    /// 1. Bo หลับ — เมื่อผู้เล่นคิดนานเกิน 30 วินาที (Disappointed = ก้มหน้า)
    /// 2. Bo มึนสกอร์เสมอ — เมื่อสกอร์ P1 == P2 (Speak + ข้อความ "ใครชนะนะ??")
    ///
    /// วาง component ที่ BoPanda_Prefab → ฉากไหนมี Bo instance ก็ทำงานอัตโนมัติ
    /// Fail-safe: หา Animator/Scoreboard ไม่เจอ → log + ข้าม ไม่พัง
    /// </summary>
    public class BoComedyDirector : MonoBehaviour
    {
        [Header("Comedy — Sleep (คิดนานเกิน)")]
        [Tooltip("เวลาที่ผู้เล่นไม่ทำอะไร (วินาที) ก่อนที่ Bo จะหลับ")]
        [Range(10f, 120f)] public float sleepAfterIdleSeconds = 30f;
        [Tooltip("Cooldown หลังตื่น ก่อนหลับได้อีก (วินาที)")]
        [Range(5f, 60f)] public float sleepCooldownSeconds = 15f;

        [Header("Comedy — Score Tie (สกอร์เสมอ)")]
        [Tooltip("Cooldown ระหว่างมึนสกอร์ (วินาที) — กัน spam")]
        [Range(5f, 60f)] public float tieCooldownSeconds = 20f;

        [Header("Debug")]
        public bool verbose = false;

        private Animator _animator;
        private bool _wasSleeping;
        private float _idleTimer;
        private float _lastSleepEndTime = -999f;
        private float _lastTieCommentTime = -999f;
        private bool _hasScoreboard;
        private bool _subscribed;

        private void Start()
        {
            _animator = GetComponent<Animator>();
            if (_animator == null)
            {
                _animator = GetComponentInChildren<Animator>();
            }

            if (_animator == null)
            {
                Debug.LogWarning("[BoComedy] No Animator found — comedy disabled (fail-safe).");
            }

            // subscribe scoreboard (หาแบบ fail-safe — อาจโหลดทีหลัง)
            TrySubscribeScoreboard();
        }

        private void Update()
        {
            if (_animator == null) return;

            // ---------- Bo หลับ (คิดนานเกิน) ----------
            bool ballMoving = BallActivityDetector.IsAnyBallMoving();

            if (ballMoving)
            {
                // มีการเคลื่อนไหว → ผู้เล่นกำลังทำอะไร → reset timer + ตื่น (ถ้าหลับอยู่)
                _idleTimer = 0f;
                if (_wasSleeping)
                {
                    WakeUp();
                }
            }
            else
            {
                _idleTimer += Time.deltaTime;
                if (!_wasSleeping && _idleTimer >= sleepAfterIdleSeconds &&
                    Time.time - _lastSleepEndTime >= sleepCooldownSeconds)
                {
                    FallAsleep();
                }
            }
        }

        private void FallAsleep()
        {
            _wasSleeping = true;
            _animator.SetTrigger("Disappointed"); // ก้มหน้า = หลับ (ใช้ animation ที่มี)
            if (verbose) Debug.Log("[BoComedy] zzz... Bo fell asleep (idle too long).");
        }

        private void WakeUp()
        {
            _wasSleeping = false;
            _animator.SetBool("IsIdle", true); // กลับท่ายืน
            _lastSleepEndTime = Time.time;
            if (verbose) Debug.Log("[BoComedy] Bo woke up! (ball moving)");
        }

        private void TrySubscribeScoreboard()
        {
            if (_subscribed) return;

            var scoreboard = FindAnyObjectByType<ChinesePoolScoreboard>();
            if (scoreboard == null)
            {
                if (!_hasScoreboard)
                {
                    Debug.Log("[BoComedy] No ChinesePoolScoreboard yet — will retry.");
                    _hasScoreboard = true;
                }
                // retry ทุก 2 วิ (scoreboard อาจโหลดทีหลัง)
                Invoke(nameof(TrySubscribeScoreboard), 2f);
                return;
            }

            scoreboard.OnScoreChanged -= OnScoreChanged;
            scoreboard.OnScoreChanged += OnScoreChanged;
            _subscribed = true;
            if (verbose) Debug.Log("[BoComedy] Subscribed to scoreboard.");
        }

        private void OnScoreChanged(int p1Score, int p2Score)
        {
            if (_animator == null) return;

            // สกอร์เสมอ (มากกว่า 0) + cooldown ผ่าน
            if (p1Score > 0 && p1Score == p2Score &&
                Time.time - _lastTieCommentTime >= tieCooldownSeconds)
            {
                _lastTieCommentTime = Time.time;
                _animator.SetTrigger("Speak"); // ปากขยับ = มึน
                if (verbose) Debug.Log($"[BoComedy] Tie score {p1Score}-{p2Score} — Bo confused: 'ใครชนะนะ??'");
            }
        }

        private void OnDestroy()
        {
            var scoreboard = FindAnyObjectByType<ChinesePoolScoreboard>();
            if (scoreboard != null)
            {
                scoreboard.OnScoreChanged -= OnScoreChanged;
            }
            CancelInvoke();
        }
    }

    /// <summary>
    /// Helper: ตรวจว่ามีลูกกำลังขยับไหม (ใช้ Rigidbody velocity)
    /// Fail-safe: ถ้าหา ball ไม่เจอ → return false (ถือว่าไม่ขยับ)
    /// </summary>
    public static class BallActivityDetector
    {
        public static bool IsAnyBallMoving()
        {
            var balls = UnityEngine.Object.FindObjectsByType<Rigidbody>(FindObjectsSortMode.None);
            foreach (var rb in balls)
            {
                if (rb == null) continue;
                if (rb.linearVelocity.sqrMagnitude > 0.01f) return true;
            }
            return false;
        }
    }
}
