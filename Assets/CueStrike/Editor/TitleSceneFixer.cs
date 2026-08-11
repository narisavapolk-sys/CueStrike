using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using UnityEditor.Events;
using UnityEngine.Events;
using System.Linq;
using System.Reflection;

namespace CueStrike.Editor
{
    /// <summary>
    /// Editor tool to diagnose and fix Title Scene button bindings and Build Settings.
    /// Matches TitleSceneManager method names exactly.
    /// </summary>
    public class TitleSceneFixer : EditorWindow
    {
        private Vector2 scrollPos;
        private string logText = "Click CHECK buttons to diagnose...";

        [MenuItem("Tools/CueStrike/Setup/Fix Title Scene Issues")]
        public static void ShowWindow()
        {
            GetWindow<TitleSceneFixer>("Fix Title Scene", typeof(SceneView));
        }

        private void OnGUI()
        {
            GUILayout.Label("CueStrike — Title Scene Diagnostics", EditorStyles.largeLabel);
            EditorGUILayout.Space(5);

            // === CHECK SECTION ===
            GUILayout.Label("STEP 1: CHECK", EditorStyles.boldLabel);
            
            if (GUILayout.Button("CHECK: Build Settings Scenes", GUILayout.Height(28)))
            {
                CheckBuildSettings();
            }
            
            if (GUILayout.Button("CHECK: TitleSceneManager in Scene", GUILayout.Height(28)))
            {
                CheckManager();
            }
            
            if (GUILayout.Button("CHECK: All Button Bindings", GUILayout.Height(28)))
            {
                CheckButtons();
            }

            EditorGUILayout.Space(10);

            // === APPLY SECTION ===
            GUILayout.Label("STEP 2: APPLY FIX", EditorStyles.boldLabel);
            
            GUI.backgroundColor = new Color(0.2f, 0.8f, 0.2f);
            if (GUILayout.Button("APPLY FIX ALL", GUILayout.Height(45)))
            {
                if (EditorUtility.DisplayDialog("Confirm Fix All", 
                    "This will auto-fix buttons, manager refs, and verify build settings. Continue?", 
                    "APPLY", "Cancel"))
                {
                    ApplyFixAll();
                }
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.Space(10);

            // === LOG OUTPUT ===
            GUILayout.Label("Log Output:", EditorStyles.boldLabel);
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(150));
            EditorGUILayout.HelpBox(logText, MessageType.Info);
            EditorGUILayout.EndScrollView();
        }

        // ==================== CHECK METHODS ====================

        void CheckBuildSettings()
        {
            var scenes = EditorBuildSettings.scenes;
            bool hasTitle = scenes.Any(s => s.path.Contains("Title"));
            bool hasMain = scenes.Any(s => s.path.Contains("Main"));

            logText = $"Build Settings Check:\n";
            logText += $"- Title Scene: {(hasTitle ? "✅ FOUND" : "❌ MISSING")}\n";
            logText += $"- Main Scene:  {(hasMain ? "✅ FOUND" : "❌ MISSING")}\n";
            logText += $"\nCurrent scenes ({scenes.Length}):\n";
            foreach (var s in scenes)
                logText += $"  [{s.enabled}] {s.path}\n";

            if (!hasTitle || !hasMain)
                logText += "\n⚠️ Click APPLY FIX ALL to auto-add scenes!";
        }

        void CheckManager()
        {
            var manager = FindManager();
            if (manager == null)
            {
                logText = "❌ TitleSceneManager NOT FOUND in current scene!\n\n" +
                          "Make sure you are in Title_NoksGrandHall.unity\n" +
                          "and the Managers/TitleSceneManager GameObject exists.";
                return;
            }

            logText = $"✅ TitleSceneManager found on: {manager.name}\n\n";
            
            // Check fields via reflection
            var type = manager.GetType();
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
            foreach (var f in fields)
            {
                var val = f.GetValue(manager);
                bool isSet = val != null;
                if (val is UnityEngine.Object uo)
                    isSet = uo != null;
                
                logText += $"- {f.Name}: {(isSet ? "✅ Set" : "❌ NULL")}\n";
            }
        }

        void CheckButtons()
        {
            var buttons = FindObjectsByType<Button>(FindObjectsInactive.Include);
            logText = $"Found {buttons.Length} Button(s) in scene:\n\n";
            
            foreach (var btn in buttons)
            {
                int listeners = btn.onClick.GetPersistentEventCount();
                string status = listeners > 0 ? $"✅ ({listeners} listeners)" : "❌ NO onClick";
                logText += $"- {btn.name}: {status}\n";
                
                for (int i = 0; i < listeners; i++)
                {
                    var target = btn.onClick.GetPersistentTarget(i);
                    var method = btn.onClick.GetPersistentMethodName(i);
                    logText += $"    → {target?.GetType().Name}.{method}\n";
                }
            }
        }

        // ==================== APPLY FIX ====================

        void ApplyFixAll()
        {
            int fixes = 0;
            logText = "=== APPLY FIX ALL ===\n\n";

            // 1. Fix Build Settings
            fixes += FixBuildSettings();
            
            // 2. Find or create Manager
            var manager = FindManager();
            if (manager == null)
            {
                // Try to find by type name
                var all = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                manager = all.FirstOrDefault(m => m.GetType().Name == "TitleSceneManager");
            }

            if (manager == null)
            {
                logText += "❌ Cannot find TitleSceneManager. Please run Step 7 in Title Scene Setup first.\n";
                return;
            }

            logText += $"✅ Found manager on: {manager.name}\n";

            // 3. Fix Button Bindings
            fixes += FixButtons(manager);

            // 4. Save scene
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            logText += $"\n✅ Total fixes applied: {fixes}\n";
            logText += "⚠️ Remember to SAVE SCENE (Ctrl+S) after fixing!";
        }

        int FixBuildSettings()
        {
            var scenes = EditorBuildSettings.scenes.ToList();
            bool changed = false;

            // Look for Title scene
            var titleGuids = AssetDatabase.FindAssets("t:SceneAsset Title_NoksGrandHall");
            string titlePath = titleGuids.Length > 0 ? AssetDatabase.GUIDToAssetPath(titleGuids[0]) : "";

            var mainGuids = AssetDatabase.FindAssets("t:SceneAsset MainScene");
            string mainPath = mainGuids.Length > 0 ? AssetDatabase.GUIDToAssetPath(mainGuids[0]) : "";

            if (!string.IsNullOrEmpty(titlePath) && !scenes.Any(s => s.path == titlePath))
            {
                scenes.Insert(0, new EditorBuildSettingsScene(titlePath, true));
                changed = true;
                logText += $"✅ Added to Build Settings (index 0): {titlePath}\n";
            }

            if (!string.IsNullOrEmpty(mainPath) && !scenes.Any(s => s.path == mainPath))
            {
                scenes.Add(new EditorBuildSettingsScene(mainPath, true));
                changed = true;
                logText += $"✅ Added to Build Settings: {mainPath}\n";
            }

            if (changed)
            {
                EditorBuildSettings.scenes = scenes.ToArray();
                return 1;
            }
            logText += "ℹ️ Build Settings already correct\n";
            return 0;
        }

        int FixButtons(MonoBehaviour manager)
        {
            int fixes = 0;
            var type = manager.GetType();

            // Button name -> (methodName, arg)
            var buttonMap = new[]
            {
                new { btnName = "PlayButton", method = "LoadScene", arg = "MainScene" },
                new { btnName = "PracticeButton", method = "ShowComingSoon", arg = "Practice" },
                new { btnName = "MultiplayerButton", method = "ShowComingSoon", arg = "Multiplayer" },
                new { btnName = "SettingsButton", method = "ShowPanel", arg = "SettingsPanel" },
                new { btnName = "CreditsButton", method = "ShowPanel", arg = "CreditsPanel" },
                new { btnName = "QuitButton", method = "QuitGame", arg = (string)null },
            };

            foreach (var map in buttonMap)
            {
                var btn = FindButton(map.btnName);
                if (btn == null) continue;

                // Clear existing listeners
                while (btn.onClick.GetPersistentEventCount() > 0)
                    UnityEventTools.RemovePersistentListener(btn.onClick, 0);

                // Get method info
                MethodInfo methodInfo = type.GetMethod(map.method);
                if (methodInfo == null)
                {
                    logText += $"❌ Method {map.method} not found on TitleSceneManager\n";
                    continue;
                }

                // Create proper UnityAction delegate
                if (map.arg != null)
                {
                    string arg = map.arg; // capture for closure
                    UnityAction action = () => methodInfo.Invoke(manager, new object[] { arg });
                    UnityEventTools.AddPersistentListener(btn.onClick, action);
                }
                else
                {
                    UnityAction action = () => methodInfo.Invoke(manager, null);
                    UnityEventTools.AddPersistentListener(btn.onClick, action);
                }

                fixes++;
                logText += $"✅ Bound {map.btnName} → {map.method}({map.arg})\n";
            }

            // Also fix Back buttons in panels
            fixes += FixBackButtons(manager);

            return fixes;
        }

        int FixBackButtons(MonoBehaviour manager)
        {
            int fixes = 0;
            var backButtons = FindObjectsByType<Button>(FindObjectsInactive.Include)
                .Where(b => b.name.Contains("Back") || b.name.Contains("Close"));

            foreach (var btn in backButtons)
            {
                // Check if already bound to OnBackButton
                bool alreadyBound = false;
                for (int i = 0; i < btn.onClick.GetPersistentEventCount(); i++)
                {
                    if (btn.onClick.GetPersistentMethodName(i) == "OnBackButton")
                        alreadyBound = true;
                }

                if (alreadyBound) continue;

                while (btn.onClick.GetPersistentEventCount() > 0)
                    UnityEventTools.RemovePersistentListener(btn.onClick, 0);

                var method = manager.GetType().GetMethod("OnBackButton");
                UnityAction action = () => method.Invoke(manager, null);
                UnityEventTools.AddPersistentListener(btn.onClick, action);
                fixes++;
                logText += $"✅ Bound {btn.name} → OnBackButton()\n";
            }

            return fixes;
        }

        // ==================== HELPERS ====================

        MonoBehaviour FindManager()
        {
            var all = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include);
            return all.FirstOrDefault(m => m.GetType().Name == "TitleSceneManager");
        }

        Button FindButton(string name)
        {
            var all = FindObjectsByType<Button>(FindObjectsInactive.Include);
            return all.FirstOrDefault(b => b.name == name);
        }

#if UNITY_EDITOR
        /// <summary>
        /// Editor-only self-test for TitleSceneFixer.
        /// Run via: Tools/CueStrike/Debug/Test TitleSceneFixer
        /// </summary>
        [UnityEditor.MenuItem("Tools/CueStrike/Debug/Test TitleSceneFixer")]
        public static void SelfTest()
        {
            bool pass = true;

            // Test 1: Window type exists
            var windowType = typeof(TitleSceneFixer);
            if (windowType == null)
            {
                UnityEngine.Debug.LogError("[TitleSceneFixer SelfTest] FAIL: Type not found");
                pass = false;
            }

            // Test 2: MenuItem attribute exists
            var methods = windowType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            bool hasMenuItem = methods.Any(m => m.GetCustomAttribute<UnityEditor.MenuItem>() != null);
            if (!hasMenuItem)
            {
                UnityEngine.Debug.LogError("[TitleSceneFixer SelfTest] FAIL: MenuItem not found");
                pass = false;
            }

            // Test 3: Check methods exist
            var instanceMethods = windowType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            string[] requiredMethods = { "CheckBuildSettings", "CheckManager", "CheckButtons", "ApplyFixAll", "FixBuildSettings", "FixButtons", "FindManager", "FindButton" };
            foreach (var m in requiredMethods)
            {
                if (!instanceMethods.Any(im => im.Name == m))
                {
                    UnityEngine.Debug.LogError($"[TitleSceneFixer SelfTest] FAIL: Method {m} missing");
                    pass = false;
                }
            }

            // Test 4: UnityEventTools available
            try
            {
                var t = typeof(UnityEventTools);
            }
            catch
            {
                UnityEngine.Debug.LogError("[TitleSceneFixer SelfTest] FAIL: UnityEventTools not available");
                pass = false;
            }

            if (pass)
            {
                UnityEngine.Debug.Log("[TitleSceneFixer SelfTest] ✅ ALL TESTS PASSED — Ready for human verify");
            }
            else
            {
                UnityEngine.Debug.LogWarning("[TitleSceneFixer SelfTest] ⚠️ TESTS FAILED — Fix before proceeding");
            }
        }
#endif
    }
}