using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using CueStrike.UI;
using CueStrike.UI.ChinesePool;

namespace CueStrike.Editor
{
    /// <summary>
    /// R35 (AAA_RoomDAY) + R39 (Title_NoksGrandHall) — Bo Comedy Scoreboard Setup
    /// วาง ChinesePoolScoreboard จริงลงห้องแข่ง + lobby แล้วผูก ChinesePoolUIManager._scoreboard
    /// เพื่อให้ BoComedyDirector (R32) subscribe OnScoreChanged ได้ → โมเมนต์ "มึนสกอร์เสมอ" ทำงานจริง.
    ///
    /// Idempotent: รันซ้ำไม่สร้างซ้ำ / skip ถ้ามีครบ. Self-test + batchmode พร้อม.
    /// </summary>
    public static class BoScoreboardSetup
    {
        private static readonly string[] ScenePaths =
        {
            "Assets/CueStrike/Scenes/AAA DAY/AAA_RoomDAY.unity",
            "Assets/CueStrike/Scenes/Title_NoksGrandHall.unity",
        };
        private const string ScoreboardGOName = "CueStrike_ChinesePoolScoreboard";

        [MenuItem("Tools/CueStrike/Mascots/95. Setup Bo Comedy Scoreboard (AAA + Title)")]
        public static void SetupFromMenu()
        {
            if (Application.isPlaying)
            {
                Debug.LogError("[BoScoreboardSetup] Cannot run in Play Mode.");
                return;
            }

            bool ok = Run();
            Debug.Log(ok ? "[BoScoreboardSetup] ✅ Setup complete — Bo will react to tied scores in all scenes."
                          : "[BoScoreboardSetup] ❌ Setup failed — see errors above.");
        }

        /// <summary>Batchmode entry point (compile gate + CI).</summary>
        public static void RunFromBatch()
        {
            bool ok = Run();
            if (!ok)
            {
                EditorApplication.Exit(1);
            }
            EditorApplication.Exit(0);
        }

        public static bool Run()
        {
            bool allPass = true;

            foreach (var scenePath in ScenePaths)
            {
                Debug.Log($"[BoScoreboardSetup] === Processing scene: {scenePath} ===");
                allPass &= RunForScene(scenePath);
            }

            Debug.Log(allPass
                ? $"[BoScoreboardSetup] ✅ All {ScenePaths.Length} scenes processed successfully."
                : "[BoScoreboardSetup] ❌ One or more scenes failed — see errors above.");
            return allPass;
        }

        private static bool RunForScene(string scenePath)
        {
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            if (scene == null || !scene.IsValid())
            {
                Debug.LogError($"[BoScoreboardSetup] Cannot open scene: {scenePath}");
                return false;
            }

            bool pass = true;

            // 1. Scoreboard component
            var scoreboard = Object.FindAnyObjectByType<ChinesePoolScoreboard>();
            if (scoreboard == null)
            {
                scoreboard = CreateScoreboard();
                pass &= scoreboard != null;
                if (scoreboard == null) return false;
            }
            else
            {
                Debug.Log("[BoScoreboardSetup] Scoreboard already present — idempotent skip.");
            }

            // 2. Wire UIManager._scoreboard
            var uiManager = Object.FindAnyObjectByType<ChinesePoolUIManager>();
            if (uiManager != null)
            {
                var so = new SerializedObject(uiManager);
                var prop = so.FindProperty("_scoreboard");
                if (prop != null && prop.objectReferenceValue == null)
                {
                    prop.objectReferenceValue = scoreboard;
                    so.ApplyModifiedPropertiesWithoutUndo();
                    Debug.Log("[BoScoreboardSetup] Wired ChinesePoolUIManager._scoreboard.");
                }
                else if (prop != null)
                {
                    Debug.Log("[BoScoreboardSetup] UIManager._scoreboard already assigned — idempotent skip.");
                }
                else
                {
                    Debug.LogWarning("[BoScoreboardSetup] _scoreboard field not found on UIManager.");
                    pass = false;
                }
            }
            else
            {
                Debug.LogWarning("[BoScoreboardSetup] No ChinesePoolUIManager in scene — Bo can still find the scoreboard directly.");
            }

            // 3. Verify Bo has BoComedyDirector (R32)
            var bo = Object.FindAnyObjectByType<MascotSystem.BoComedyDirector>();
            if (bo == null)
            {
                Debug.LogWarning("[BoScoreboardSetup] BoComedyDirector not found in scene (BoPanda prefab instance may be missing).");
                pass = false;
            }
            else
            {
                Debug.Log("[BoScoreboardSetup] BoComedyDirector present — Bo will react to tied scores.");
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[BoScoreboardSetup] Scene saved: {scenePath}");

            bool selfTestOk = RunSelfTest();
            return pass && selfTestOk;
        }

        private static ChinesePoolScoreboard CreateScoreboard()
        {
            var go = new GameObject(ScoreboardGOName);
            var sb = go.AddComponent<ChinesePoolScoreboard>();

            // UI structure พื้นฐาน — ลอก pattern จาก CueStrikeGamePolishSetup.CreateScoreboard
            var bg = CreatePanel(go, "Scoreboard_BG", new Color(0.05f, 0.05f, 0.08f, 0.9f));
            bg.GetComponent<RectTransform>().sizeDelta = new Vector2(800f, 400f);

            var so = new SerializedObject(sb);
            AssignText(so, "_player1NameText", bg, "Player1_Section", "PLAYER 1", new Vector2(-200f, 100f));
            AssignText(so, "_player1ScoreText", bg, "Player1_Score", "00", new Vector2(-200f, 20f), 48);
            AssignText(so, "_player2NameText", bg, "Player2_Section", "PLAYER 2", new Vector2(200f, 100f));
            AssignText(so, "_player2ScoreText", bg, "Player2_Score", "00", new Vector2(200f, 20f), 48);
            AssignText(so, "_matchTimerText", bg, "MatchTimer", "00:00", new Vector2(0f, 150f), 28);
            AssignText(so, "_currentInningText", bg, "InningText", "Inning 1", new Vector2(0f, -100f), 24);
            AssignText(so, "_foulCounterText", bg, "FoulText", "Fouls: 0", new Vector2(0f, -150f), 24);

            AssignImage(so, "_player1TurnIndicator", bg, "P1_TurnIndicator", new Vector2(-200f, 80f), new Vector2(120f, 6f));
            AssignImage(so, "_player2TurnIndicator", bg, "P2_TurnIndicator", new Vector2(200f, 80f), new Vector2(120f, 6f));

            AssignContainer(so, "_player1BallsContainer", bg, "Player1_BallsContainer", new Vector2(-200f, -50f));
            AssignContainer(so, "_player2BallsContainer", bg, "Player2_BallsContainer", new Vector2(200f, -50f));

            so.ApplyModifiedPropertiesWithoutUndo();

            Debug.Log("[BoScoreboardSetup] Created ChinesePoolScoreboard with UI structure.");
            return sb;
        }

        private static void AssignText(SerializedObject so, string field, GameObject parent, string name, string text, Vector2 pos, int fontSize = 32)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent.transform, false);
            var rt = obj.AddComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(300f, 60f);
            var txt = obj.AddComponent<Text>();
            txt.text = text;
            txt.fontSize = fontSize;
            txt.color = Color.white;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            var prop = so.FindProperty(field);
            if (prop != null) prop.objectReferenceValue = txt;
        }

        private static void AssignImage(SerializedObject so, string field, GameObject parent, string name, Vector2 pos, Vector2 size)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent.transform, false);
            var rt = obj.AddComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            var img = obj.AddComponent<Image>();
            img.color = new Color(1f, 0.84f, 0f, 1f);

            var prop = so.FindProperty(field);
            if (prop != null) prop.objectReferenceValue = img;
        }

        private static void AssignContainer(SerializedObject so, string field, GameObject parent, string name, Vector2 pos)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent.transform, false);
            var rt = obj.AddComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(200f, 40f);

            var prop = so.FindProperty(field);
            if (prop != null) prop.objectReferenceValue = rt;
        }

        private static GameObject CreatePanel(GameObject parent, string name, Color color)
        {
            var panel = new GameObject(name);
            panel.transform.SetParent(parent.transform, false);
            var rt = panel.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var img = panel.AddComponent<Image>();
            img.color = color;
            return panel;
        }

        [MenuItem("Tools/CueStrike/Debug/Test Bo Comedy Scoreboard")]
        public static void TestFromMenu()
        {
            bool ok = RunSelfTest();
            Debug.Log(ok ? "[Self-Test] Bo Comedy Scoreboard: ALL PASS" : "[Self-Test] Bo Comedy Scoreboard: SOME FAILED");
        }

        public static bool RunSelfTest()
        {
            bool pass = true;
            pass &= Check("ChinesePoolScoreboard exists", Object.FindAnyObjectByType<ChinesePoolScoreboard>() != null);
            pass &= Check("ChinesePoolUIManager._scoreboard assigned",
                Object.FindAnyObjectByType<ChinesePoolUIManager>() is var m && m != null &&
                new SerializedObject(m).FindProperty("_scoreboard") is var p && p != null && p.objectReferenceValue != null);
            pass &= Check("BoComedyDirector present (BoPanda instance)",
                Object.FindAnyObjectByType<MascotSystem.BoComedyDirector>() != null);
            Debug.Log($"[Self-Test] Bo Comedy Scoreboard: {(pass ? "PASS 3/3" : "FAIL")}");
            return pass;
        }

        private static bool Check(string name, bool condition)
        {
            Debug.Log($"[Self-Test] {name}: {(condition ? "✅" : "❌")}");
            return condition;
        }
    }
}
