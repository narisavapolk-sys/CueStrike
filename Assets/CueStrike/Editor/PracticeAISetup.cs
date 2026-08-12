using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using CueStrike.AI;
using CueStrike.Gameplay.ChinesePool;

namespace CueStrike.EditorTools
{
    /// <summary>
    /// R34 — Editor tool: ผูก CueStrikePracticeAIBridge ลงฉากที่เล่นได้ (AAA_RoomDAY + Snooker_Demo)
    /// ให้ AI opponent (ลุงโน๊กคู่ซ้อม) ทำงานในโหมด Practice
    /// idempotent + self-test + batchmode
    /// </summary>
    public static class PracticeAISetup
    {
        private const string MenuRoot = "Tools/CueStrike/AI/90. Setup Practice AI";
        private const string BridgeGOName = "PracticeAI_Bridge";

        /// <summary>
        /// ฉากที่มี ChinesePoolGameManager (ที่ตรวจแล้ว: AAA_RoomDAY เท่านั้น —
        /// Title เป็น lobby, Snooker_Demo ใช้ WBPS ruleset คนละระบบ)
        /// </summary>
        private static readonly string[] TargetScenes =
        {
            "Assets/CueStrike/Scenes/AAA DAY/AAA_RoomDAY.unity"
        };

        [MenuItem(MenuRoot)]
        public static void SetupAll()
        {
            bool allOk = true;
            foreach (var scenePath in TargetScenes)
            {
                if (!System.IO.File.Exists(scenePath))
                {
                    Debug.LogWarning($"[PracticeAI] Scene not found, skipping: {scenePath}");
                    continue;
                }

                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                {
                    Debug.LogWarning("[PracticeAI] Setup cancelled by user.");
                    return;
                }

                var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                bool ok = SetupScene(scene);
                allOk &= ok;

                if (ok)
                {
                    EditorSceneManager.SaveScene(scene);
                    Debug.Log($"[PracticeAI] ✅ Scene wired & saved: {scenePath}");
                }
            }

            if (allOk)
            {
                Debug.Log("[PracticeAI] ✅ ALL SCENES WIRED — Practice AI ready (Easy/Medium/Hard/Expert).");
            }
            else
            {
                Debug.LogWarning("[PracticeAI] ⚠️ Some scenes failed — check warnings above.");
            }
        }

        private static bool SetupScene(Scene scene)
        {
            var gm = Object.FindFirstObjectByType<ChinesePoolGameManager>();
            if (gm == null)
            {
                Debug.LogWarning($"[PracticeAI] No ChinesePoolGameManager in {scene.name} — skipping (fail-safe).");
                return false;
            }

            var existing = Object.FindFirstObjectByType<CueStrikePracticeAIBridge>();
            if (existing != null)
            {
                Debug.Log($"[PracticeAI] Bridge already present in {scene.name} — idempotent skip.");
                return true;
            }

            // หา GameObject เดิมที่มี aiModifier หรือ controller (ใช้ร่วม node — ไม่สร้างโฟมใหม่รกฉาก)
            var host = gm.gameObject;

            var modifier = Object.FindFirstObjectByType<ChinesePoolAIModifier>();
            var controller = Object.FindFirstObjectByType<CueStrikeAIController>();
            var shotManager = Object.FindFirstObjectByType<CueStrikeShotManager>();

            // ถ้า modifier/controller อยู่ node อื่น → host เป็น node นั้น (refs ใกล้กัน)
            if (modifier != null) host = modifier.gameObject;
            else if (controller != null) host = controller.gameObject;

            var bridge = host.GetComponent<CueStrikePracticeAIBridge>();
            if (bridge == null)
            {
                bridge = host.AddComponent<CueStrikePracticeAIBridge>();
            }

            bridge.aiModifier = modifier;
            bridge.aiController = controller;
            bridge.shotManager = shotManager;

            // default difficulty จาก PlayerPrefs ที่เคยเลือก (ถ้ามี)
            int saved = PlayerPrefs.GetInt("CueStrike_AIDifficulty", (int)SkillLevel.Medium);
            if (System.Enum.IsDefined(typeof(SkillLevel), saved))
            {
                bridge.defaultDifficulty = (SkillLevel)saved;
            }

            Debug.Log($"[PracticeAI] Bridge added to '{host.name}' in {scene.name} (modifier={modifier != null}, controller={controller != null}, shotManager={shotManager != null}).");
            return true;
        }

        [MenuItem("Tools/CueStrike/AI/91. Practice AI Self-Test")]
        public static void SelfTest()
        {
            int pass = 0;
            int total = 0;

            total++; pass += Check("CueStrikePracticeAIBridge class exists",
                typeof(CueStrikePracticeAIBridge) != null);

            total++; pass += Check("ChinesePoolAIModifier class exists",
                typeof(ChinesePoolAIModifier) != null);

            total++; pass += Check("CueStrikeAIController + SkillLevel enum exists",
                typeof(CueStrikeAIController) != null && System.Enum.IsDefined(typeof(SkillLevel), SkillLevel.Easy));

            total++; pass += Check("CueStrikeShotManager.ExecuteShot exists",
                typeof(CueStrikeShotManager).GetMethod("ExecuteShot") != null);

            total++; pass += Check("GameManager.ProcessShotResult exists",
                typeof(ChinesePoolGameManager).GetMethod("ProcessShotResult") != null);

            total++; pass += Check("GameManager.aiModifier public field exists",
                typeof(ChinesePoolGameManager).GetField("aiModifier") != null);

            total++; pass += Check("GameManager.SetCallShot exists",
                typeof(ChinesePoolGameManager).GetMethod("SetCallShot") != null);

            total++; pass += Check("GameManager.NextPlayer exists",
                typeof(ChinesePoolGameManager).GetMethod("NextPlayer") != null);

            // ตรวจฉาก (ถ้าเปิดอยู่)
            total++; pass += Check("AAA_RoomDAY scene exists on disk (มี GameManager)",
                System.IO.File.Exists("Assets/CueStrike/Scenes/AAA DAY/AAA_RoomDAY.unity"));

            total++; pass += Check("AAA_RoomDAY มี ChinesePoolGameManager",
                System.IO.File.ReadAllText("Assets/CueStrike/Scenes/AAA DAY/AAA_RoomDAY.unity").Contains("ChinesePoolGameManager"));

            if (pass == total)
            {
                Debug.Log($"✅ Practice AI SELF-TEST PASSED ({pass}/{total}).");
            }
            else
            {
                Debug.LogWarning($"⚠️ Practice AI SELF-TEST FAILED ({pass}/{total}).");
            }
        }

        private static int Check(string label, bool condition)
        {
            if (condition)
            {
                Debug.Log($"  ✅ {label}");
                return 1;
            }
            Debug.LogError($"  ❌ {label}");
            return 0;
        }
    }
}
