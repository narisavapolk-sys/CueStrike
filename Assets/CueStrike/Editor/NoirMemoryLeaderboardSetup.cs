using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using TMPro;
using CueStrike.NoirMemory;
using System.IO;

namespace CueStrike.Editor.NoirMemory
{
    /// <summary>
    /// Editor tool for wiring the Noir Memory leaderboard UI.
    /// Creates Canvas, ScrollView, and leaderboard entry prefab,
    /// then assigns references to NoirMemoryResultsScreen via SerializedObject.
    /// </summary>
    public static class NoirMemoryLeaderboardSetup
    {
        private const string PrefabPath = "Assets/CueStrike/Prefabs/UI/NoirMemoryLeaderboardEntry.prefab";
        private const string PrefabDir = "Assets/CueStrike/Prefabs/UI";

        #region Setup

        [MenuItem("Tools/CueStrike/Setup/Wire Noir Memory Leaderboard UI")]
        public static void WireLeaderboardUI()
        {
            // Guard 1
            if (Application.isPlaying)
            {
                Debug.LogError("[CueStrike Setup] Cannot wire leaderboard while in Play Mode.");
                EditorUtility.DisplayDialog("Cannot Setup", "Stop Play Mode first!", "OK");
                return;
            }

            // Guard 2
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                Debug.Log("[CueStrike Setup] Setup cancelled by user.");
                return;
            }

            // Guard 3
            var resultsScreen = Object.FindFirstObjectByType<NoirMemoryResultsScreen>();
            if (resultsScreen == null)
            {
                Debug.LogError("[CueStrike Setup] FAIL: NoirMemoryResultsScreen not found.");
                EditorUtility.DisplayDialog("Setup FAILED", "NoirMemoryResultsScreen not found.\nRun 'Tools/CueStrike/Apply/Setup Noir Memory Results' first.", "OK");
                return;
            }

            Undo.IncrementCurrentGroup();
            int groupIndex = Undo.GetCurrentGroup();
            var serialized = new SerializedObject(resultsScreen);

            try
            {
                // Canvas
                var canvas = Object.FindFirstObjectByType<Canvas>();
                if (canvas == null)
                {
                    var canvasGO = new GameObject("NoirMemoryCanvas");
                    Undo.RegisterCreatedObjectUndo(canvasGO, "Create Canvas");
                    canvas = canvasGO.AddComponent<Canvas>();
                    canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                    canvasGO.AddComponent<CanvasScaler>();
                    canvasGO.AddComponent<GraphicRaycaster>();
                    Debug.Log("[CueStrike Setup] Created Canvas.");
                }

                // Leaderboard Content (ScrollView -> Content RectTransform)
                var contentProp = serialized.FindProperty("leaderboardContent");
                if (contentProp.objectReferenceValue == null)
                {
                    var viewportRT = CreateLeaderboardScrollView(canvas);
                    Undo.RegisterCreatedObjectUndo(viewportRT.gameObject, "Create Leaderboard ScrollView");
                    contentProp.objectReferenceValue = viewportRT;
                    serialized.ApplyModifiedProperties();
                    Debug.Log("[CueStrike Setup] Created and assigned leaderboard ScrollView.");
                }
                else
                {
                    Debug.Log("[CueStrike Setup] leaderboardContent already assigned.");
                }

                // Prefab
                var prefabProp = serialized.FindProperty("leaderboardEntryPrefab");
                if (prefabProp.objectReferenceValue == null)
                {
                    var prefab = CreateOrLoadLeaderboardEntryPrefab();
                    prefabProp.objectReferenceValue = prefab;
                    serialized.ApplyModifiedProperties();
                    Debug.Log($"[CueStrike Setup] Assigned leaderboardEntryPrefab.");
                }
                else
                {
                    Debug.Log("[CueStrike Setup] leaderboardEntryPrefab already assigned.");
                }

                // Score texts auto-wire
                AutoWireText(serialized, "totalScoreText", "TotalScore", "ScoreText");
                AutoWireText(serialized, "gradeText", "Grade", "GradeText");
                AutoWireText(serialized, "rankText", "Rank", "RankText");

                // Confetti
                var confettiProp = serialized.FindProperty("confettiEffect");
                if (confettiProp.objectReferenceValue == null)
                {
                    var allParticles = Object.FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None);
                    foreach (var p in allParticles)
                    {
                        if (p.gameObject.name.ToLower().Contains("confetti"))
                        {
                            confettiProp.objectReferenceValue = p;
                            serialized.ApplyModifiedProperties();
                            Debug.Log("[CueStrike Setup] Auto-wired confetti effect.");
                            break;
                        }
                    }
                }

                EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Debug.Log("[CueStrike Setup] Leaderboard UI wired successfully.");
                EditorUtility.DisplayDialog("CueStrike", "Noir Memory Leaderboard UI wired!", "OK");
            }
            finally
            {
                Undo.CollapseUndoOperations(groupIndex);
            }
        }

        #endregion

        #region Helpers

        private static RectTransform CreateLeaderboardScrollView(Canvas canvas)
        {
            var svGO = new GameObject("LeaderboardScrollView", typeof(RectTransform));
            var scrollRect = svGO.AddComponent<ScrollRect>();
            var image = svGO.AddComponent<Image>();
            image.color = new Color(0, 0, 0, 0.3f);
            svGO.AddComponent<Mask>().showMaskGraphic = false;

            var svRT = svGO.GetComponent<RectTransform>();
            svRT.SetParent(canvas.transform, false);
            svRT.anchorMin = new Vector2(0.05f, 0.1f);
            svRT.anchorMax = new Vector2(0.95f, 0.45f);
            svRT.offsetMin = Vector2.zero;
            svRT.offsetMax = Vector2.zero;

            // Viewport
            var vpGO = new GameObject("Viewport", typeof(RectTransform));
            var vpRT = vpGO.GetComponent<RectTransform>();
            vpRT.SetParent(svRT, false);
            vpRT.anchorMin = Vector2.zero;
            vpRT.anchorMax = Vector2.one;
            vpRT.offsetMin = Vector2.zero;
            vpRT.offsetMax = Vector2.zero;
            vpRT.pivot = new Vector2(0f, 1f);
            var vpImage = vpGO.AddComponent<Image>();
            vpImage.color = new Color(0, 0, 0, 0.1f);
            vpGO.AddComponent<Mask>().showMaskGraphic = false;
            scrollRect.viewport = vpRT;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;

            // Content
            var ctGO = new GameObject("Content", typeof(RectTransform));
            var ctRT = ctGO.GetComponent<RectTransform>();
            ctRT.SetParent(vpRT, false);
            ctRT.anchorMin = new Vector2(0, 1);
            ctRT.anchorMax = new Vector2(1, 1);
            ctRT.pivot = new Vector2(0f, 1f);
            ctRT.sizeDelta = new Vector2(0, 0);

            var layout = ctGO.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 4;
            layout.padding = new RectOffset(8, 8, 8, 8);
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;

            var fitter = ctGO.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            scrollRect.content = ctRT;
            return ctRT;
        }

        private static GameObject CreateOrLoadLeaderboardEntryPrefab()
        {
            if (!Directory.Exists(PrefabDir))
                Directory.CreateDirectory(PrefabDir);

            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (existing != null) return existing;

            var go = new GameObject("NoirMemoryLeaderboardEntry", typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0, 40);

            var hlg = go.AddComponent<HorizontalLayoutGroup>();
            hlg.padding = new RectOffset(8, 8, 4, 4);
            hlg.spacing = 4;
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = true;
            hlg.childForceExpandHeight = true;

            var bg = go.AddComponent<Image>();
            bg.color = new Color(1, 1, 1, 0.1f);

            MakeTextChild(go, "Rank", "#1", 36, TextAlignmentOptions.Center);
            MakeTextChild(go, "PlayerName", "Player", 36, TextAlignmentOptions.Left);
            MakeTextChild(go, "Score", "0", 36, TextAlignmentOptions.Right);
            MakeTextChild(go, "Grade", "A", 36, TextAlignmentOptions.Center);

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, PrefabPath);
            Object.DestroyImmediate(go);
            Debug.Log($"[CueStrike Setup] Created prefab: {PrefabPath}");
            return prefab;
        }

        private static void MakeTextChild(GameObject parent, string name, string text, int fontSize, TextAlignmentOptions align)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent.transform, false);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = align;
            tmp.color = Color.white;
            tmp.enableAutoSizing = true;
            tmp.fontSizeMin = 10;
            tmp.fontSizeMax = fontSize;
        }

        private static void AutoWireText(SerializedObject serialized, string propName, params string[] searchNames)
        {
            var prop = serialized.FindProperty(propName);
            if (prop.objectReferenceValue != null) return;

            var allTexts = Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsSortMode.None);
            foreach (var t in allTexts)
            {
                foreach (var n in searchNames)
                {
                    if (t.gameObject.name.Equals(n, System.StringComparison.OrdinalIgnoreCase))
                    {
                        prop.objectReferenceValue = t;
                        serialized.ApplyModifiedProperties();
                        Debug.Log($"[CueStrike Setup] Auto-wired {propName} -> {t.gameObject.name}");
                        return;
                    }
                }
            }
        }

        #endregion
    }
}