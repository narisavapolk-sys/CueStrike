using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace CueStrike.Editor
{
    /// <summary>
    /// Emergency compile fix tool. Run via: Tools/CueStrike/Emergency Compile Fix
    /// Fixes common compile errors automatically.
    /// </summary>
    public class CompileEmergencyFixer : EditorWindow
    {
        [MenuItem("Tools/CueStrike/Emergency Compile Fix")]
        public static void ShowWindow()
        {
            GetWindow<CompileEmergencyFixer>("Compile Fix");
        }

        private Vector2 scrollPos;
        private string log = "";

        void OnGUI()
        {
            GUILayout.Label("CueStrike Emergency Compile Fix", EditorStyles.boldLabel);
            GUILayout.Space(10);

            GUILayout.Label("This tool fixes common compile errors:", EditorStyles.label);
            GUILayout.Label("- Typo: UnityEngine.Ul -> UnityEngine.UI", EditorStyles.label);
            GUILayout.Label("- Duplicate files (RCA, PracticeManager)", EditorStyles.label);
            GUILayout.Label("- Missing XR Hands stub", EditorStyles.label);
            GUILayout.Label("- Ambiguous DrillSettingsData", EditorStyles.label);
            GUILayout.Space(10);

            GUI.backgroundColor = Color.red;
            if (GUILayout.Button("RUN FIX", GUILayout.Height(40)))
            {
                RunFix();
            }
            GUI.backgroundColor = Color.white;

            GUILayout.Space(10);
            GUILayout.Label("Log:", EditorStyles.boldLabel);
            scrollPos = GUILayout.BeginScrollView(scrollPos, GUILayout.Height(200));
            GUILayout.TextArea(log, GUILayout.ExpandHeight(true));
            GUILayout.EndScrollView();
        }

        private void RunFix()
        {
            log = "";
            int fixCount = 0;

            // 1. Delete duplicate/typo files
            string[] filesToDelete = new string[]
            {
                "Assets/CueStrike/Customization/CustomizationUl.cs",
                "Assets/CueStrike/Customization/CustomizationUl.cs.meta",
                "Assets/CueStrike/Scripts/ChinesePool/ChinesePoolCallShotUl.cs",
                "Assets/CueStrike/Scripts/ChinesePool/ChinesePoolCallShotUl.cs.meta",
                "Assets/CueStrike/Scripts/RCA.cs",
                "Assets/CueStrike/Scripts/RCA.cs.meta",
                "Assets/CueStrike/UI/CueStrikePracticeManager.cs",
                "Assets/CueStrike/UI/CueStrikePracticeManager.cs.meta"
            };

            foreach (string path in filesToDelete)
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                    Log("DELETED: " + path);
                    fixCount++;
                }
            }

            // 2. Fix typo in files
            string[] filesToFix = new string[]
            {
                "Assets/CueStrike/Gameplay/CueStrikePracticeExit.cs",
                "Assets/CueStrike/Gameplay/Tutorial/CueStrikeTutorialOverlay.cs",
                "Assets/CueStrike/Gameplay/Tutorial/CueStrikeTutorialStepUI.cs",
                "Assets/CueStrike/Scripts/CueStrikeAccessibilityManager.cs"
            };

            foreach (string path in filesToFix)
            {
                if (File.Exists(path))
                {
                    string content = File.ReadAllText(path);
                    if (content.Contains("UnityEngine.Ul"))
                    {
                        content = content.Replace("UnityEngine.Ul", "UnityEngine.UI");
                        File.WriteAllText(path, content);
                        Log("FIXED typo: " + path);
                        fixCount++;
                    }
                }
            }

            // 3. Fix TutorialStepUI missing using
            string stepUI = "Assets/CueStrike/Gameplay/Tutorial/CueStrikeTutorialStepUI.cs";
            if (File.Exists(stepUI))
            {
                string content = File.ReadAllText(stepUI);
                if (!content.Contains("using UnityEngine.UI;"))
                {
                    content = "using UnityEngine.UI;\n" + content;
                    File.WriteAllText(stepUI, content);
                    Log("ADDED using: " + stepUI);
                    fixCount++;
                }
            }

            // 4. Fix AccessibilityManager XR
            string accMgr = "Assets/CueStrike/Scripts/CueStrikeAccessibilityManager.cs";
            if (File.Exists(accMgr))
            {
                string content = File.ReadAllText(accMgr);
                if (content.Contains("using UnityEngine.XR;") && !content.Contains("#if UNITY_XR_AVAILABLE"))
                {
                    content = content.Replace("using UnityEngine.XR;", "#if UNITY_XR_AVAILABLE\nusing UnityEngine.XR;\n#endif");
                    File.WriteAllText(accMgr, content);
                    Log("WRAPPED XR: " + accMgr);
                    fixCount++;
                }
            }

            // 5. Fix CustomDrillBuilderUI ambiguous DrillSettingsData
            string drillUI = "Assets/CueStrike/UI/CustomDrillBuilderUI.cs";
            if (File.Exists(drillUI))
            {
                string content = File.ReadAllText(drillUI);
                // Fix ambiguous DrillSettingsData - use SaveSystem namespace
                string pattern = @"(?<!CueStrike\.Gameplay\.SaveSystem\.)DrillSettingsData";
                if (Regex.IsMatch(content, pattern))
                {
                    content = Regex.Replace(content, pattern, "CueStrike.Gameplay.SaveSystem.DrillSettingsData");
                    File.WriteAllText(drillUI, content);
                    Log("FIXED ambiguous DrillSettingsData: " + drillUI);
                    fixCount++;
                }
            }

            // 6. Create XR Hands stub
            string xrStubPath = "Assets/CueStrike/RCA/UnityEngine.XR.Hands.cs";
            if (!File.Exists(xrStubPath))
            {
                string dir = Path.GetDirectoryName(xrStubPath);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

                string stub = "// STUB: UnityEngine.XR.Hands\n" +
                    "// Remove after installing XR Hands package\n" +
                    "namespace UnityEngine.XR.Hands\n" +
                    "{\n" +
                    "    public class XRHandSubsystem { }\n" +
                    "    public class XRHandSubsystemDescriptor { }\n" +
                    "    public class XRHand { public XRHandJoint GetJoint(XRHandJointID id) => new XRHandJoint(); }\n" +
                    "    public class XRHandJoint { public bool TryGetPose(out Pose p) { p = Pose.identity; return false; } }\n" +
                    "    public enum XRHandJointID { Palm, Wrist, ThumbMetacarpal, ThumbProximal, ThumbDistal, ThumbTip, IndexMetacarpal, IndexProximal, IndexIntermediate, IndexDistal, IndexTip, MiddleMetacarpal, MiddleProximal, MiddleIntermediate, MiddleDistal, MiddleTip, RingMetacarpal, RingProximal, RingIntermediate, RingDistal, RingTip, LittleMetacarpal, LittleProximal, LittleIntermediate, LittleDistal, LittleTip, EndMarker }\n" +
                    "}\n";
                File.WriteAllText(xrStubPath, stub);
                Log("CREATED: " + xrStubPath);
                fixCount++;
            }

            Log("\n=== TOTAL FIXES: " + fixCount + " ===");
            if (fixCount > 0)
            {
                Log("Please wait for Unity to recompile...");
                AssetDatabase.Refresh();
            }
            else
            {
                Log("No fixes needed. If errors persist, check Console manually.");
            }
        }

        private void Log(string msg)
        {
            log += msg + "\n";
            Debug.Log("[CompileFix] " + msg);
        }
    }
}
