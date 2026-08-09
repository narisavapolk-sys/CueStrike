using UnityEngine;
using UnityEditor;
using CueStrike.NoirMemory;

namespace CueStrike.Editor
{
    public class NoirMemoryPuzzleEditor : EditorWindow
    {
        private float testDuration = 5f;
        private float aiAccuracy = 0.85f;

        [MenuItem("Tools/CueStrike/Debug/Test Noir Memory Puzzle")]
        public static void ShowWindow()
        {
            GetWindow<NoirMemoryPuzzleEditor>("Noir Memory Debug");
        }

        private void OnGUI()
        {
            GUILayout.Label("Noir Memory Puzzle Debug", EditorStyles.largeLabel);
            EditorGUILayout.Space(10);

            var manager = FindObjectOfType<NoirMemoryPuzzleManager>();
            if (manager == null)
            {
                EditorGUILayout.HelpBox("NoirMemoryPuzzleManager not found in scene", MessageType.Warning);
                if (GUILayout.Button("Create Manager"))
                {
                    var go = new GameObject("NoirMemoryPuzzleManager");
                    go.AddComponent<NoirMemoryPuzzleManager>();
                    EditorUtility.SetDirty(go);
                }
                return;
            }

            GUILayout.Label("Test Controls", EditorStyles.boldLabel);
            
            testDuration = EditorGUILayout.FloatField("Reveal Duration", testDuration);
            
            GUI.backgroundColor = new Color(0.2f, 0.8f, 0.2f);
            if (GUILayout.Button("START MEMORY MODE", GUILayout.Height(35)))
            {
                manager.StartMemoryMode(testDuration);
            }
            GUI.backgroundColor = Color.white;

            if (GUILayout.Button("STOP MEMORY MODE", GUILayout.Height(25)))
            {
                manager.StopMemoryMode();
            }

            EditorGUILayout.Space(10);
            GUILayout.Label("AI Memory", EditorStyles.boldLabel);
            aiAccuracy = EditorGUILayout.Slider("AI Accuracy", aiAccuracy, 0f, 1f);
            
            var aiMemory = FindObjectOfType<NoirMemoryAIMemory>();
            if (aiMemory != null && GUILayout.Button("Set AI Accuracy"))
            {
                aiMemory.SetMemoryAccuracy(aiAccuracy);
            }

            EditorGUILayout.Space(10);
            GUILayout.Label("Status", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Active: {manager.IsMemoryModeActive()}");
            EditorGUILayout.LabelField($"Noir Phase: {manager.IsNoirPhase()}");
        }
    }
}