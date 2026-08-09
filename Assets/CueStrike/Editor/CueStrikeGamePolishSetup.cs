using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace CueStrike.Editor
{
    /// <summary>
    /// Editor tool: Setup AAA Game Polish with one click.
    /// Guard 3 layers: Play Mode block, Unsaved changes prompt, Wrong scene prompt.
    /// Undo support. Detailed logging. Fail-safe.
    /// </summary>
    public class CueStrikeGamePolishSetup : EditorWindow
    {
        #region Menu Items
        [MenuItem("Tools/CueStrike/Apply/Setup AAA Game Polish")]
        public static void SetupAAAGamePolish()
        {
            if (!RunGuards()) return;

            Debug.Log("[CueStrikeGamePolishSetup] === Starting AAA Game Polish Setup ===");

            try
            {
                // 1. Create UI Canvas (World Space for VR)
                GameObject canvasObj = CreateOrGetCanvas();
                Undo.RegisterCreatedObjectUndo(canvasObj, "Create UI Canvas");

                // 2. Create UIManager
                GameObject uiManagerObj = CreateUIManager(canvasObj);
                Undo.RegisterCreatedObjectUndo(uiManagerObj, "Create UI Manager");

                // 3. Create Scoreboard
                GameObject scoreboardObj = CreateScoreboard(canvasObj);
                Undo.RegisterCreatedObjectUndo(scoreboardObj, "Create Scoreboard");

                // 4. Create Ball Tracker
                GameObject trackerObj = CreateBallTracker();
                Undo.RegisterCreatedObjectUndo(trackerObj, "Create Ball Tracker");

                // 5. Wire references
                WireReferences(uiManagerObj, scoreboardObj, trackerObj);

                // 6. Mark scene dirty
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

                Debug.Log("[CueStrikeGamePolishSetup] === Setup Complete ===");
                Debug.Log($"[CueStrikeGamePolishSetup] Created: {canvasObj.name}, {uiManagerObj.name}, {scoreboardObj.name}, {trackerObj.name}");
                Debug.Log("[CueStrikeGamePolishSetup] Next steps: Assign ball transforms and pocket positions in BallPottedTracker inspector.");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[CueStrikeGamePolishSetup] Setup failed: {ex.Message}");
                EditorUtility.DisplayDialog("Setup Failed", $"Error: {ex.Message}\n\nCheck Console for details.", "OK");
            }
        }

        [MenuItem("Tools/CueStrike/Debug/Test AAA Polish")]
        public static void TestAAAPolish()
        {
            if (Application.isPlaying)
            {
                Debug.LogWarning("[Self-Test] Exit Play Mode before running self-test.");
                return;
            }

            Debug.Log("[CueStrikeGamePolishSetup] === Self-Test Started ===");
            bool allPass = true;

            // Test 1: UIManager exists
            var uiManager = FindFirstObjectByType<CueStrike.UI.CueStrikeUIManager>();
            if (uiManager != null)
            {
                allPass &= uiManager.RunSelfTest();
            }
            else
            {
                Debug.LogError("[Self-Test] UIManager not found. Run Setup first.");
                allPass = false;
            }

            // Test 2: Scoreboard exists
            var scoreboard = FindFirstObjectByType<CueStrike.UI.ChinesePoolScoreboard>();
            if (scoreboard != null)
            {
                allPass &= scoreboard.RunSelfTest();
            }
            else
            {
                Debug.LogError("[Self-Test] Scoreboard not found. Run Setup first.");
                allPass = false;
            }

            // Test 3: Ball Tracker exists
            var tracker = FindFirstObjectByType<CueStrike.Gameplay.BallPottedTracker>();
            if (tracker != null)
            {
                allPass &= tracker.RunSelfTest();
            }
            else
            {
                Debug.LogError("[Self-Test] BallPottedTracker not found. Run Setup first.");
                allPass = false;
            }

            // Test 4: UI Animations exists
            var anim = FindFirstObjectByType<CueStrike.UI.CueStrikeUIAnimations>();
            if (anim == null)
            {
                Debug.LogWarning("[Self-Test] UIAnimations component missing (will be auto-added).");
            }

            Debug.Log($"[CueStrikeGamePolishSetup] === Self-Test Result: {(allPass ? "ALL PASS" : "SOME FAILED")} ===");

            EditorUtility.DisplayDialog(
                "Self-Test Result",
                allPass ? "All AAA Polish systems passed!" : "Some tests failed. Check Console.",
                "OK"
            );
        }
        #endregion

        #region Guard Layers
        private static bool RunGuards()
        {
            // Guard 1: Play Mode
            if (Application.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError("[CueStrikeGamePolishSetup] Cannot run in Play Mode. Exit Play Mode first.");
                EditorUtility.DisplayDialog("Blocked", "Exit Play Mode before running Setup.", "OK");
                return false;
            }

            // Guard 2: Unsaved changes
            if (EditorSceneManager.GetActiveScene().isDirty)
            {
                bool save = EditorUtility.DisplayDialog(
                    "Unsaved Changes",
                    "Current scene has unsaved changes. Save before setup?",
                    "Save and Continue",
                    "Cancel"
                );
                if (!save) return false;
                EditorSceneManager.SaveOpenScenes();
            }

            // Guard 3: Wrong scene (warn only)
            string sceneName = EditorSceneManager.GetActiveScene().name;
            string[] validScenes = { "MainScene", "ChinesePool", "NoirMemory", "Title_NoksGrandHall" };
            if (!validScenes.Contains(sceneName))
            {
                bool proceed = EditorUtility.DisplayDialog(
                    "Scene Warning",
                    $"Current scene \"{sceneName}\" is not a standard game scene.\n\nProceed anyway?",
                    "Proceed",
                    "Cancel"
                );
                if (!proceed) return false;
            }

            return true;
        }
        #endregion

        #region Creation Methods
        private static GameObject CreateOrGetCanvas()
        {
            const string canvasName = "CueStrike_UI_Canvas";
            GameObject canvas = GameObject.Find(canvasName);
            if (canvas != null)
            {
                Debug.Log($"[Setup] Using existing canvas: {canvasName}");
                return canvas;
            }

            canvas = new GameObject(canvasName);
            Canvas c = canvas.AddComponent<Canvas>();
            c.renderMode = RenderMode.WorldSpace;
            c.worldCamera = Camera.main;

            CanvasScaler scaler = canvas.AddComponent<CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 10f;

            canvas.AddComponent<GraphicRaycaster>();

            RectTransform rt = canvas.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(1920f, 1080f);
            rt.position = new Vector3(0f, 1.6f, 2f); // VR comfortable position
            rt.rotation = Quaternion.Euler(0f, 0f, 0f);

            Debug.Log($"[Setup] Created WorldSpace canvas: {canvasName}");
            return canvas;
        }

        private static GameObject CreateUIManager(GameObject canvas)
        {
            const string name = "CueStrike_UIManager";
            GameObject existing = GameObject.Find(name);
            if (existing != null)
            {
                // Re-run setup on existing UIManager (fixes stale state)
                var uiManager = existing.GetComponent<CueStrike.UI.CueStrikeUIManager>();
                if (uiManager != null)
                {
                    uiManager.SetPanel("MainMenu", CreatePanel(canvas, "MainMenuPanel"));
                    uiManager.SetPanel("Pause", CreatePanel(canvas, "PausePanel"));
                    uiManager.SetPanel("GameOver", CreatePanel(canvas, "GameOverPanel"));
                    uiManager.SetPanel("Settings", CreatePanel(canvas, "SettingsPanel"));
                    uiManager.SetPanel("Scoreboard", CreatePanel(canvas, "ScoreboardPanel"));
                    uiManager.SetPanel("Notification", CreatePanel(canvas, "NotificationPanel"));
                    uiManager.RefreshPanels();
                    Debug.Log("[Setup] Reconfigured existing UIManager with 6 panels.");
                }
                return existing;
            }

            GameObject obj = new GameObject(name);
            obj.transform.SetParent(canvas.transform, false);

            var newManager = obj.AddComponent<CueStrike.UI.CueStrikeUIManager>();

            // Create placeholder panels and assign via public API
            newManager.SetPanel("MainMenu", CreatePanel(canvas, "MainMenuPanel"));
            newManager.SetPanel("Pause", CreatePanel(canvas, "PausePanel"));
            newManager.SetPanel("GameOver", CreatePanel(canvas, "GameOverPanel"));
            newManager.SetPanel("Settings", CreatePanel(canvas, "SettingsPanel"));
            newManager.SetPanel("Scoreboard", CreatePanel(canvas, "ScoreboardPanel"));
            newManager.SetPanel("Notification", CreatePanel(canvas, "NotificationPanel"));

            // Refresh to ensure registry is up to date
            newManager.RefreshPanels();

            Debug.Log($"[Setup] Created UIManager with 6 panels.");
            return obj;
        }

        private static GameObject CreateScoreboard(GameObject canvas)
        {
            const string name = "CueStrike_ChinesePoolScoreboard";
            GameObject existing = GameObject.Find(name);
            if (existing != null) return existing;

            GameObject obj = new GameObject(name);
            obj.transform.SetParent(canvas.transform, false);

            var sb = obj.AddComponent<CueStrike.UI.ChinesePoolScoreboard>();

            // Create scoreboard UI structure
            GameObject bg = CreatePanel(obj, "Scoreboard_BG", new Color(0.05f, 0.05f, 0.08f, 0.9f));
            bg.GetComponent<RectTransform>().sizeDelta = new Vector2(800f, 400f);

            // Player 1 section
            GameObject p1Section = CreateTextSection(bg, "Player1_Section", "PLAYER 1", new Vector2(-200f, 100f));
            sb.GetType().GetField("_player1NameText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(sb, p1Section.GetComponent<Text>());

            GameObject p1Score = CreateTextSection(bg, "Player1_Score", "00", new Vector2(-200f, 20f), 48);
            sb.GetType().GetField("_player1ScoreText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(sb, p1Score.GetComponent<Text>());

            // Player 2 section
            GameObject p2Section = CreateTextSection(bg, "Player2_Section", "PLAYER 2", new Vector2(200f, 100f));
            sb.GetType().GetField("_player2NameText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(sb, p2Section.GetComponent<Text>());

            GameObject p2Score = CreateTextSection(bg, "Player2_Score", "00", new Vector2(200f, 20f), 48);
            sb.GetType().GetField("_player2ScoreText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(sb, p2Score.GetComponent<Text>());

            // Match info
            GameObject timer = CreateTextSection(bg, "MatchTimer", "00:00", new Vector2(0f, 150f), 28);
            sb.GetType().GetField("_matchTimerText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(sb, timer.GetComponent<Text>());

            GameObject inning = CreateTextSection(bg, "InningText", "Inning 1", new Vector2(0f, -100f), 24);
            sb.GetType().GetField("_currentInningText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(sb, inning.GetComponent<Text>());

            GameObject foul = CreateTextSection(bg, "FoulText", "Fouls: 0", new Vector2(0f, -150f), 24);
            sb.GetType().GetField("_foulCounterText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(sb, foul.GetComponent<Text>());

            // Turn indicators
            GameObject p1Indicator = CreateImageSection(bg, "P1_TurnIndicator", new Vector2(-200f, 80f), new Vector2(120f, 6f));
            sb.GetType().GetField("_player1TurnIndicator", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(sb, p1Indicator.GetComponent<Image>());

            GameObject p2Indicator = CreateImageSection(bg, "P2_TurnIndicator", new Vector2(200f, 80f), new Vector2(120f, 6f));
            sb.GetType().GetField("_player2TurnIndicator", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(sb, p2Indicator.GetComponent<Image>());

            // Ball containers
            GameObject p1Balls = new GameObject("Player1_BallsContainer");
            p1Balls.transform.SetParent(bg.transform, false);
            RectTransform p1Rt = p1Balls.AddComponent<RectTransform>();
            p1Rt.anchoredPosition = new Vector2(-200f, -50f);
            p1Rt.sizeDelta = new Vector2(200f, 40f);
            sb.GetType().GetField("_player1BallsContainer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(sb, p1Balls.transform);

            GameObject p2Balls = new GameObject("Player2_BallsContainer");
            p2Balls.transform.SetParent(bg.transform, false);
            RectTransform p2Rt = p2Balls.AddComponent<RectTransform>();
            p2Rt.anchoredPosition = new Vector2(200f, -50f);
            p2Rt.sizeDelta = new Vector2(200f, 40f);
            sb.GetType().GetField("_player2BallsContainer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(sb, p2Balls.transform);

            Debug.Log($"[Setup] Created ChinesePoolScoreboard with full UI structure.");
            return obj;
        }

        private static GameObject CreateBallTracker()
        {
            const string name = "CueStrike_BallPottedTracker";
            GameObject existing = GameObject.Find(name);
            if (existing != null) return existing;

            GameObject obj = new GameObject(name);
            var tracker = obj.AddComponent<CueStrike.Gameplay.BallPottedTracker>();
            tracker.GetType().GetField("_pocketDetectionRadius", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(tracker, 0.15f);

            Debug.Log($"[Setup] Created BallPottedTracker. Assign ball transforms and pocket positions in Inspector.");
            return obj;
        }
        #endregion

        #region Helpers
        private static GameObject CreatePanel(GameObject parent, string name, Color? color = null)
        {
            GameObject panel = new GameObject(name);
            panel.transform.SetParent(parent.transform, false);

            RectTransform rt = panel.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            Image img = panel.AddComponent<Image>();
            img.color = color ?? new Color(0f, 0f, 0f, 0.5f);
            img.raycastTarget = true;

            panel.SetActive(false); // Safe default
            return panel;
        }

        private static GameObject CreateTextSection(GameObject parent, string name, string text, Vector2 position, int fontSize = 32)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent.transform, false);

            RectTransform rt = obj.AddComponent<RectTransform>();
            rt.anchoredPosition = position;
            rt.sizeDelta = new Vector2(300f, 60f);

            Text txt = obj.AddComponent<Text>();
            txt.text = text;
            txt.fontSize = fontSize;
            txt.color = Color.white;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            return obj;
        }

        private static GameObject CreateImageSection(GameObject parent, string name, Vector2 position, Vector2 size)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent.transform, false);

            RectTransform rt = obj.AddComponent<RectTransform>();
            rt.anchoredPosition = position;
            rt.sizeDelta = size;

            Image img = obj.AddComponent<Image>();
            img.color = new Color(1f, 0.84f, 0f, 1f); // Gold default

            return obj;
        }

        private static void WireReferences(GameObject uiManagerObj, GameObject scoreboardObj, GameObject trackerObj)
        {
            // Auto-find and wire if possible
            var uiManager = uiManagerObj.GetComponent<CueStrike.UI.CueStrikeUIManager>();
            var scoreboard = scoreboardObj.GetComponent<CueStrike.UI.ChinesePoolScoreboard>();
            var tracker = trackerObj.GetComponent<CueStrike.Gameplay.BallPottedTracker>();

            if (uiManager != null && scoreboard != null)
            {
                // UIManager will auto-find scoreboard via FindFirstObjectByType
                Debug.Log("[Setup] References wired. UIManager will auto-locate Scoreboard and Tracker.");
            }
        }
        #endregion
    }
}