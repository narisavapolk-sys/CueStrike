using UnityEngine;
using CueStrike.Gameplay.ChinesePool;

namespace CueStrike.MascotSystem
{
    /// <summary>
    /// R31 — กรรมการจริง: ผูก UncleNokReferee กับ game events
    /// เชื่อม events ของ ChinesePoolGameManager / CueStrikeWBPSRuleset
    /// → เรียก referee methods (ประกาศคะแนน / ฟาวล์ / เริ่ม-จบเฟรม / จบแมตช์)
    ///
    /// วาง component ที่ UncleNok_Prefab → ฉากไหนมีลุงโน๊ก + game manager ได้ผลอัตโนมัติ
    /// Fail-safe: หา manager/referee ไม่เจอ → log + retry (คล้าย BoComedy)
    /// </summary>
    public class UncleNokRefereeEventBridge : MonoBehaviour
    {
        [Header("Debug")]
        public bool verbose = false;

        private UncleNokReferee _referee;
        private bool _subscribedCp;
        private bool _subscribedWbps;
        private int _frameNumber;

        private void Start()
        {
            _referee = GetComponent<UncleNokReferee>();
            if (_referee == null)
            {
                _referee = GetComponentInChildren<UncleNokReferee>(true);
            }

            if (_referee == null)
            {
                Debug.LogWarning("[RefereeBridge] No UncleNokReferee found — bridge disabled (fail-safe).");
            }

            // retry subscribe (managers อาจโหลดทีหลัง)
            TrySubscribe();
        }

        private void Update()
        {
            // retry เป็นระยะจนกว่า manager จะโหลด
            if (!_subscribedCp || !_subscribedWbps)
            {
                TrySubscribe();
            }
        }

        private void TrySubscribe()
        {
            if (_referee == null) return;

            if (!_subscribedCp)
            {
                var cp = ChinesePoolGameManager.Instance;
                if (cp != null)
                {
                    cp.OnPhaseChanged += OnCpPhaseChanged;
                    cp.OnFrameWon += OnCpFrameWon;
                    cp.OnFrameLost += OnCpFrameLost;
                    cp.OnFoulCommitted += OnCpFoul;
                    cp.OnMatchOver += OnCpMatchOver;
                    _subscribedCp = true;
                    if (verbose) Debug.Log("[RefereeBridge] Subscribed to ChinesePoolGameManager.");
                }
            }

            if (!_subscribedWbps)
            {
                var wbps = CueStrikeWBPSRuleset.Instance;
                if (wbps != null)
                {
                    wbps.OnBallPotted += OnWbpsBallPotted;
                    wbps.OnFoulCommitted += OnWbpsFoul;
                    wbps.OnFrameWon += OnWbpsFrameWon;
                    _subscribedWbps = true;
                    if (verbose) Debug.Log("[RefereeBridge] Subscribed to CueStrikeWBPSRuleset.");
                }
            }
        }

        // ============ ChinesePoolGameManager ============

        private void OnCpPhaseChanged(ChinesePoolMatchState phase)
        {
            if (_referee == null) return;

            if (phase == ChinesePoolMatchState.Break)
            {
                // เริ่มเฟรมใหม่ (เฟรมแรก = เริ่มแมตช์ด้วย)
                _frameNumber++;
                _referee.OnFrameStart(_frameNumber);
                if (_frameNumber == 1)
                {
                    _referee.OnMatchStart();
                }
            }
        }

        private void OnCpFrameWon(int winnerIndex)
        {
            _referee?.OnFrameEnd(winnerIndex);
        }

        private void OnCpFrameLost(int loserIndex)
        {
            // จบเฟรม — ประกาศผู้ชนะ (ฝั่งตรงข้าม)
            _referee?.OnFrameEnd(1 - loserIndex);
        }

        private void OnCpFoul(int playerIndex, string foulType)
        {
            _referee?.OnFoulCommitted(MapFoulType(foulType), playerIndex, 1);
        }

        private void OnCpMatchOver()
        {
            int winner = 0;
            var cp = ChinesePoolGameManager.Instance;
            if (cp != null)
            {
                // ใช้ GetFrameWinner ถ้ามี — fallback 0
                winner = cp.GetFrameWinner();
            }
            _referee?.OnMatchEnd(winner);
        }

        // ============ CueStrikeWBPSRuleset (Snooker) ============

        private void OnWbpsBallPotted(int points)
        {
            _referee?.OnBallPotted(0, points, 1);
        }

        private void OnWbpsFoul(int penalty, string reason)
        {
            _referee?.OnFoulCommitted(MapFoulType(reason), 0, penalty);
        }

        private void OnWbpsFrameWon()
        {
            _referee?.OnFrameEnd(0);
        }

        // ============ helpers ============

        private static UncleNokReferee.FoulType MapFoulType(string foul)
        {
            if (string.IsNullOrEmpty(foul)) return UncleNokReferee.FoulType.Generic;

            string f = foul.ToLowerInvariant();
            if (f.Contains("cueball") || f.Contains("cue ball")) return UncleNokReferee.FoulType.CueBallPotted;
            if (f.Contains("nocontact") || f.Contains("no contact") || f.Contains("not contacted")) return UncleNokReferee.FoulType.NoBallContacted;
            if (f.Contains("wrongball") || f.Contains("wrong ball")) return UncleNokReferee.FoulType.WrongBallFirst;
            if (f.Contains("nocushion") || f.Contains("no cushion")) return UncleNokReferee.FoulType.NoCushionAfterContact;
            if (f.Contains("offtable") || f.Contains("off table") || f.Contains("off the table")) return UncleNokReferee.FoulType.BallOffTable;
            if (f.Contains("doublehit") || f.Contains("double hit")) return UncleNokReferee.FoulType.DoubleHit;
            if (f.Contains("pushshot") || f.Contains("push shot")) return UncleNokReferee.FoulType.PushShot;
            if (f.Contains("miscue")) return UncleNokReferee.FoulType.Miscue;
            return UncleNokReferee.FoulType.Generic;
        }

        private void OnDestroy()
        {
            if (_subscribedCp)
            {
                var cp = ChinesePoolGameManager.Instance;
                if (cp != null)
                {
                    cp.OnPhaseChanged -= OnCpPhaseChanged;
                    cp.OnFrameWon -= OnCpFrameWon;
                    cp.OnFrameLost -= OnCpFrameLost;
                    cp.OnFoulCommitted -= OnCpFoul;
                    cp.OnMatchOver -= OnCpMatchOver;
                }
            }

            if (_subscribedWbps)
            {
                var wbps = CueStrikeWBPSRuleset.Instance;
                if (wbps != null)
                {
                    wbps.OnBallPotted -= OnWbpsBallPotted;
                    wbps.OnFoulCommitted -= OnWbpsFoul;
                    wbps.OnFrameWon -= OnWbpsFrameWon;
                }
            }
        }
    }
}
