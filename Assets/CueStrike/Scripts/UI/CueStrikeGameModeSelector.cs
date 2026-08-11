using UnityEngine;

namespace CueStrike.UI
{
    /// <summary>
    /// R26 — Game mode selection (coach-approved: Snooker 15/10/6 เป็นโหมดหลัก).
    /// Static selector: MainMenu เลือกโหมด → ตั้งค่า → โหลดฉากห้องที่ถูกต้อง.
    ///
    /// Mode → Red balls / Scene:
    ///   Snooker15   → 15 reds → Snooker_Demo
    ///   Snooker10   → 10 reds → Snooker_Demo
    ///   Snooker6    → 6 reds  → Snooker_Demo
    ///   ChinesePool → 15 balls → AAA_RoomDAY
    ///   EightBall   → AAA_RoomDAY (fallback) — dedicated 8-ball scene มาใน R26+
    ///   NineBall    → AAA_RoomDAY (fallback)
    ///
    /// Fail-safe: ถ้า scene ไม่มีใน Build Settings → log warning (SceneManager จะเตือนเอง),
    /// selector ยังตั้งค่า mode ไว้ให้ scene ที่โหลดได้อ่าน
    /// </summary>
    public static class CueStrikeGameModeSelector
    {
        public enum GameMode
        {
            Snooker15 = 0,
            Snooker10 = 1,
            Snooker6 = 2,
            EightBall = 3,
            NineBall = 4,
            ChinesePool = 5
        }

        private const string PrefsKey = "CueStrike_SelectedGameMode";

        /// <summary>โหมดที่เลือก (เริ่มต้น Snooker15 — โหมดหลักของเกม)</summary>
        public static GameMode SelectedMode
        {
            get => (GameMode)PlayerPrefs.GetInt(PrefsKey, (int)GameMode.Snooker15);
            set
            {
                PlayerPrefs.SetInt(PrefsKey, (int)value);
                PlayerPrefs.Save();
            }
        }

        /// <summary>จำนวนลูกแดงสำหรับ Snooker (15/10/6); โหมดอื่นคืน 0 (ไม่ใช้)</summary>
        public static int GetRedBallsForMode(GameMode mode)
        {
            switch (mode)
            {
                case GameMode.Snooker15: return 15;
                case GameMode.Snooker10: return 10;
                case GameMode.Snooker6: return 6;
                default: return 0;
            }
        }

        /// <summary>ฉากที่ควรโหลดสำหรับโหมดนี้</summary>
        public static string ModeToSceneName(GameMode mode)
        {
            switch (mode)
            {
                case GameMode.Snooker15:
                case GameMode.Snooker10:
                case GameMode.Snooker6:
                    return "Snooker_Demo";
                case GameMode.ChinesePool:
                    return "AAA_RoomDAY";
                case GameMode.EightBall:
                case GameMode.NineBall:
                    // Dedicated scenes ยังไม่พร้อม — fallback ไปห้อง ChinesePool ที่มีโต๊ะ/UI ครบ
                    return "AAA_RoomDAY";
                default:
                    return "Snooker_Demo";
            }
        }

        /// <summary>ป้ายชื่อโหมดสำหรับ UI</summary>
        public static string GetModeLabel(GameMode mode)
        {
            switch (mode)
            {
                case GameMode.Snooker15: return "Snooker 15";
                case GameMode.Snooker10: return "Snooker 10";
                case GameMode.Snooker6: return "Snooker 6";
                case GameMode.EightBall: return "8-Ball";
                case GameMode.NineBall: return "9-Ball";
                case GameMode.ChinesePool: return "Chinese Pool";
                default: return mode.ToString();
            }
        }

        /// <summary>
        /// ตั้งค่าให้ scene ที่กำลังจะโหลดตามโหมดที่เลือก (เรียกก่อน LoadScene หรือใน Start ของ ruleset).
        /// - Snooker: ตั้ง CueStrikeWBPSRuleset.totalRedBalls
        /// - ChinesePool: ไม่ต้องตั้ง (กติกาเหมือนเดิม)
        /// </summary>
        public static void ApplyModeToScene()
        {
            GameMode mode = SelectedMode;
            int reds = GetRedBallsForMode(mode);
            if (reds > 0)
            {
                var wbps = Object.FindAnyObjectByType<CueStrikeWBPSRuleset>();
                if (wbps != null)
                {
                    wbps.totalRedBalls = reds;
                    wbps.ResetFrame();
                    Debug.Log($"[GameModeSelector] Applied Snooker mode: {GetModeLabel(mode)} ({reds} reds).");
                    return;
                }
                Debug.LogWarning($"[GameModeSelector] CueStrikeWBPSRuleset not found — cannot apply {reds} reds.");
            }
            else
            {
                Debug.Log($"[GameModeSelector] Mode {GetModeLabel(mode)} — no rack override needed.");
            }
        }
    }
}
