using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace CueStrike.Editor
{
    /// <summary>
    /// Editor tool: Setup Complete Chinese Pool UI with one click.
    /// Creates CallShot UI, Group Display, and integrates with existing Scoreboard.
    /// </summary>
    public class ChinesePoolUISetup
    {
        [MenuItem("Tools/CueStrike/Apply/Setup Chinese Pool UI")]
        public static void SetupChinesePoolUI()
        {
            if (!RunGuards()) return;

            Debug.Log("[ChinesePoolUISetup] === Starting Chinese Pool UI Setup ===");

            try
            {
                GameObject canvasObj = FindOrCreateCanvas();
                Undo.RegisterCreatedObjectUndo(canvasObj, "Create/Find Canvas");

                GameObject uiMgrObj = new GameObject("ChinesePool_UI_Manager");
                uiMgrObj.transform.SetParent(canvasObj.transform, false);
                var uiMgr = uiMgrObj.AddComponent<UI.ChinesePool.ChinesePoolUIManager>();
                Undo.RegisterCreatedObjectUndo(uiMgrObj, "Create ChinesePool UI Manager");

                GameObject callShotObj = CreateCallShotPanel(canvasObj);
                var callShot = callShotObj.AddComponent<UI.ChinesePool.ChinesePoolCallShotUI>();
                Undo.RegisterCreatedObjectUndo(callShotObj, "Create CallShot UI");

                GameObject groupObj = CreateGroupDisplayPanel(canvasObj);
                var groupDisplay = groupObj.AddComponent<UI.ChinesePool.ChinesePoolGroupDisplay>();
                Undo.RegisterCreatedObjectUndo(groupObj, "Create Group Display");

                WirePrivateField(uiMgr, "_callShotUI", callShot);
                WirePrivateField(uiMgr, "_groupDisplay", groupDisplay);

                var scoreboard = Object.FindObjectOfType<UI.ChinesePoolScoreboard>();
                if (scoreboard != null)
                {
                    WirePrivateField(uiMgr, "_scoreboard", scoreboard);
                    Debug.Log("[Setup] Wired existing ChinesePoolScoreboard.");
                }
                else
                {
                    Debug.LogWarning("[Setup] No ChinesePoolScoreboard found. Run Setup AAA Game Polish first.");
                }

                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

                Debug.Log("[ChinesePoolUISetup] === Setup Complete ===");
                Debug.Log("[ChinesePoolUISetup] Created: CallShot UI, Group Display, UI Manager");
                Debug.Log("[ChinesePoolUISetup] Assign portraits/materials in Inspector to complete.");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[ChinesePoolUISetup] Setup failed: {ex.Message}");
                EditorUtility.DisplayDialog("Setup Failed", ex.Message, "OK");
            }
        }

        [MenuItem("Tools/CueStrike/Debug/Test Chinese Pool UI")]
        public static void TestChinesePoolUI()
        {
            if (Application.isPlaying)
            {
                EditorUtility.DisplayDialog("Blocked", "Exit Play Mode first.", "OK");
                return;
            }

            Debug.Log("[ChinesePoolUISetup] === Self-Test Started ===");
            bool allPass = true;
            int pass = 0, fail = 0;

            var uiMgr = Object.FindObjectOfType<UI.ChinesePool.ChinesePoolUIManager>();
            if (uiMgr != null && uiMgr.RunSelfTest()) { pass++; } else { fail++; allPass = false; }

            var callShot = Object.FindObjectOfType<UI.ChinesePool.ChinesePoolCallShotUI>();
            if (callShot != null && callShot.RunSelfTest()) { pass++; } else { fail++; allPass = false; }

            var groupDisp = Object.FindObjectOfType<UI.ChinesePool.ChinesePoolGroupDisplay>();
            if (groupDisp != null && groupDisp.RunSelfTest()) { pass++; } else { fail++; allPass = false; }

            Debug.Log($"[ChinesePoolUISetup] === Result: {pass} PASS, {fail} FAIL ===");
            EditorUtility.DisplayDialog("Self-Test", allPass ? "All passed!" : $"{fail} failed. Check Console.", "OK");
        }

        private static bool RunGuards()
        {
            if (Application.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorUtility.DisplayDialog("Blocked", "Exit Play Mode first.", "OK");
                return false;
            }
            if (EditorSceneManager.GetActiveScene().isDirty)
            {
                bool save = EditorUtility.DisplayDialog("Unsaved Changes", "Save before setup?", "Save", "Cancel");
                if (!save) return false;
                EditorSceneManager.SaveOpenScenes();
            }
            return true;
        }

        private static GameObject FindOrCreateCanvas()
        {
            var canvas = Object.FindObjectOfType<Canvas>();
            if (canvas != null) return canvas.gameObject;

            GameObject obj = new GameObject("CueStrike_UI_Canvas");
            Canvas c = obj.AddComponent<Canvas>();
            c.renderMode = RenderMode.WorldSpace;
            c.worldCamera = Camera.main;
            obj.AddComponent<CanvasScaler>();
            obj.AddComponent<GraphicRaycaster>();
            return obj;
        }

        private static GameObject CreateCallShotPanel(GameObject canvas)
        {
            GameObject panel = new GameObject("CallShot_Panel");
            panel.transform.SetParent(canvas.transform, false);
            RectTransform rt = panel.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(800f, 600f);
            rt.anchoredPosition = Vector2.zero;

            Image img = panel.AddComponent<Image>();
            img.color = new Color(0.05f, 0.05f, 0.08f, 0.95f);

            var callShotComponent = panel.AddComponent<UI.ChinesePool.ChinesePoolCallShotUI>();

            GameObject title = CreateText(panel, "Title", "CALL YOUR SHOT", new Vector2(0f, 250f), 36);
            WirePrivateField(callShotComponent, "_titleText", title.GetComponent<Text>());

            GameObject instr = CreateText(panel, "Instruction", "Select ball and pocket", new Vector2(0f, 200f), 24);
            WirePrivateField(callShotComponent, "_instructionText", instr.GetComponent<Text>());

            GameObject ballGrid = new GameObject("BallGrid");
            ballGrid.transform.SetParent(panel.transform, false);
            ballGrid.AddComponent<RectTransform>().anchoredPosition = new Vector2(0f, 50f);
            WirePrivateField(callShotComponent, "_ballSelectionGrid", ballGrid.transform);

            GameObject pocketGrid = new GameObject("PocketGrid");
            pocketGrid.transform.SetParent(panel.transform, false);
            pocketGrid.AddComponent<RectTransform>().anchoredPosition = new Vector2(0f, -100f);
            WirePrivateField(callShotComponent, "_pocketSelectionGrid", pocketGrid.transform);

            GameObject selBall = CreateText(panel, "SelectedBall", "Ball: None", new Vector2(-150f, -200f), 20);
            WirePrivateField(callShotComponent, "_selectedBallText", selBall.GetComponent<Text>());

            GameObject selPocket = CreateText(panel, "SelectedPocket", "Pocket: None", new Vector2(150f, -200f), 20);
            WirePrivateField(callShotComponent, "_selectedPocketText", selPocket.GetComponent<Text>());

            GameObject confirmBtn = CreateButton(panel, "Confirm", "CONFIRM", new Vector2(-100f, -250f));
            WirePrivateField(callShotComponent, "_confirmButton", confirmBtn.GetComponent<Button>());

            GameObject cancelBtn = CreateButton(panel, "Cancel", "CANCEL", new Vector2(100f, -250f));
            WirePrivateField(callShotComponent, "_cancelButton", cancelBtn.GetComponent<Button>());

            WirePrivateField(callShotComponent, "_callShotPanel", panel);
            panel.SetActive(false);
            return panel;
        }

        private static GameObject CreateGroupDisplayPanel(GameObject canvas)
        {
            GameObject panel = new GameObject("GroupDisplay_Panel");
            panel.transform.SetParent(canvas.transform, false);
            RectTransform rt = panel.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(400f, 300f);
            rt.anchoredPosition = new Vector2(0f, -100f);

            Image img = panel.AddComponent<Image>();
            img.color = new Color(0.05f, 0.05f, 0.08f, 0.9f);

            var groupComp = panel.AddComponent<UI.ChinesePool.ChinesePoolGroupDisplay>();

            GameObject redPanel = new GameObject("RedGroup");
            redPanel.transform.SetParent(panel.transform, false);
            redPanel.AddComponent<RectTransform>().anchoredPosition = new Vector2(-100f, 50f);
            Image redImg = redPanel.AddComponent<Image>();
            redImg.color = new Color(0.9f, 0.1f, 0.1f, 0.3f);
            WirePrivateField(groupComp, "_redGroupPanel", redPanel);
            WirePrivateField(groupComp, "_redGroupBackground", redImg);

            GameObject yellowPanel = new GameObject("YellowGroup");
            yellowPanel.transform.SetParent(panel.transform, false);
            yellowPanel.AddComponent<RectTransform>().anchoredPosition = new Vector2(100f, 50f);
            Image yellowImg = yellowPanel.AddComponent<Image>();
            yellowImg.color = new Color(0.9f, 0.8f, 0.1f, 0.3f);
            WirePrivateField(groupComp, "_yellowGroupPanel", yellowPanel);
            WirePrivateField(groupComp, "_yellowGroupBackground", yellowImg);

            GameObject status = CreateText(panel, "Status", "OPEN TABLE", new Vector2(0f, 120f), 28);
            WirePrivateField(groupComp, "_playerGroupText", status.GetComponent<Text>());

            GameObject remaining = CreateText(panel, "Remaining", "Remaining: 7", new Vector2(0f, -80f), 22);
            WirePrivateField(groupComp, "_remainingCountText", remaining.GetComponent<Text>());

            GameObject redBalls = new GameObject("RedBalls");
            redBalls.transform.SetParent(panel.transform, false);
            redBalls.AddComponent<RectTransform>().anchoredPosition = new Vector2(-100f, 0f);
            WirePrivateField(groupComp, "_redBallsContainer", redBalls.transform);

            GameObject yellowBalls = new GameObject("YellowBalls");
            yellowBalls.transform.SetParent(panel.transform, false);
            yellowBalls.AddComponent<RectTransform>().anchoredPosition = new Vector2(100f, 0f);
            WirePrivateField(groupComp, "_yellowBallsContainer", yellowBalls.transform);

            GameObject warning = CreateText(panel, "8BallWarning", "8-BALL TIME!", new Vector2(0f, -120f), 24);
            warning.GetComponent<Text>().color = Color.red;
            warning.SetActive(false);
            WirePrivateField(groupComp, "_eightBallWarning", warning);

            return panel;
        }

        private static GameObject CreateText(GameObject parent, string name, string text, Vector2 pos, int fontSize)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent.transform, false);
            RectTransform rt = obj.AddComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(300f, 40f);
            Text txt = obj.AddComponent<Text>();
            txt.text = text;
            txt.fontSize = fontSize;
            txt.color = Color.white;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return obj;
        }

        private static GameObject CreateButton(GameObject parent, string name, string text, Vector2 pos)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent.transform, false);
            RectTransform rt = obj.AddComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(150f, 50f);
            Image img = obj.AddComponent<Image>();
            img.color = new Color(0.2f, 0.5f, 0.9f, 1f);
            obj.AddComponent<Button>();
            GameObject txtObj = CreateText(obj, "Text", text, Vector2.zero, 20);
            txtObj.GetComponent<RectTransform>().sizeDelta = new Vector2(150f, 50f);
            return obj;
        }

        private static void WirePrivateField(object target, string fieldName, object value)
        {
            if (target == null) return;
            var field = target.GetType().GetField(fieldName,
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);
            field?.SetValue(target, value);
        }
    }
}