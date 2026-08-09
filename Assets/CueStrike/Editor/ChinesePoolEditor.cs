using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using CueStrike.Gameplay.ChinesePool;

namespace CueStrike.Editor
{
    public class ChinesePoolEditor : EditorWindow
    {
        [MenuItem("Tools/CueStrike/Chinese Pool/Debug Setup")]
        public static void ShowWindow()
        {
            GetWindow<ChinesePoolEditor>("Chinese Pool Debug");
        }

        private ChinesePoolBallSetup ballSetup;
        private ChinesePoolAIModifier aiModifier;
        // ChinesePoolRules is a static class - no instance reference needed

        private Vector2 scrollPosition;
        #pragma warning disable CS0414
        private bool showRackPositions = true;
        #pragma warning restore CS0414
        private bool showRulesTest = true;
        private bool showAITest = true;
        private bool showBallSetup = true;

        private int testPlayerIndex = 0;
        private ChinesePoolBallType testAssignedGroup = ChinesePoolBallType.Red;
        private bool testIsOpenTable = true;
        private int[] testAvailableBalls = new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15 };
        private int[] testAvailablePockets = new int[] { 0, 1, 2, 3, 4, 5 };
        private int[] testBallsOnTable = new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15 };

        private void OnEnable()
        {
            RefreshReferences();
        }

        private void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            GUILayout.Label("Chinese Pool Debug Tools", EditorStyles.largeLabel);
            EditorGUILayout.Space(10);

            // References section
            DrawReferencesSection();
            EditorGUILayout.Space(10);

            // Ball Setup section
            if (ballSetup != null)
            {
                DrawBallSetupSection();
                EditorGUILayout.Space(10);
            }

            // Rules Test section
            DrawRulesTestSection();
            EditorGUILayout.Space(10);

            // AI Test section
            if (aiModifier != null)
            {
                DrawAITestSection();
                EditorGUILayout.Space(10);
            }

            // Ball Type Reference
            DrawBallTypeReference();
            EditorGUILayout.Space(10);

            EditorGUILayout.EndScrollView();
        }

        private void DrawReferencesSection()
        {
            GUILayout.Label("References", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            ballSetup = (ChinesePoolBallSetup)EditorGUILayout.ObjectField("Ball Setup", ballSetup, typeof(ChinesePoolBallSetup), true);
            aiModifier = (ChinesePoolAIModifier)EditorGUILayout.ObjectField("AI Modifier", aiModifier, typeof(ChinesePoolAIModifier), true);

            if (GUILayout.Button("Auto-Find References"))
            {
                RefreshReferences();
            }

            EditorGUI.indentLevel--;
        }

        private void DrawBallSetupSection()
        {
            showBallSetup = EditorGUILayout.Foldout(showBallSetup, "Ball Setup", true);
            if (!showBallSetup) return;

            EditorGUI.indentLevel++;

            if (GUILayout.Button("Setup Rack (Spawn All Balls)"))
            {
                ballSetup.SetupRack();
                Debug.Log("[ChinesePoolEditor] Setup rack (spawned all balls)");
            }

            if (GUILayout.Button("Clear Rack"))
            {
                ballSetup.ClearRack();
                Debug.Log("[ChinesePoolEditor] Cleared rack");
            }

            EditorGUILayout.Space(5);

            if (GUILayout.Button("Log Ball Count"))
            {
                int[] balls = ballSetup.GetBallsOnTable();
                Debug.Log($"[ChinesePoolEditor] Balls on table: {balls.Length}");
                foreach (int id in balls)
                {
                    var ball = ballSetup.GetBallById(id);
                    if (ball != null && ball.transform != null)
                    {
                        Debug.Log($"  Ball {id}: {ball.name} at {ball.transform.position}");
                    }
                }
            }

            EditorGUILayout.Space(5);

            if (GUILayout.Button("Get Ball By ID (Test)"))
            {
                for (int i = 0; i <= 15; i++)
                {
                    var ball = ballSetup.GetBallById(i);
                    if (ball != null)
                    {
                        var idComp = ball.GetComponent<ChinesePoolBallIdentifier>();
                        Debug.Log($"[ChinesePoolEditor] Ball {i}: {ball.name} at {ball.transform.position} (ID: {idComp?.ballId})");
                    }
                    else
                    {
                        Debug.Log($"[ChinesePoolEditor] Ball {i}: NOT FOUND");
                    }
                }
            }

            EditorGUI.indentLevel--;
        }

        private void DrawRulesTestSection()
        {
            showRulesTest = EditorGUILayout.Foldout(showRulesTest, "Rules Test", true);
            if (!showRulesTest) return;

            EditorGUI.indentLevel++;

            GUILayout.Label("Ball Type Tests", EditorStyles.boldLabel);
            if (GUILayout.Button("Log All Ball Types"))
            {
                for (int i = 0; i <= 15; i++)
                {
                    var type = ChinesePoolRules.GetBallType(i);
                    Debug.Log($"[ChinesePoolRules] Ball {i}: {type}");
                }
            }

            EditorGUILayout.Space(5);

            GUILayout.Label("Validation Tests", EditorStyles.boldLabel);
            testPlayerIndex = EditorGUILayout.IntField("Test Player Index", testPlayerIndex);
            testAssignedGroup = (ChinesePoolBallType)EditorGUILayout.EnumPopup("Assigned Group", testAssignedGroup);
            testIsOpenTable = EditorGUILayout.Toggle("Is Open Table", testIsOpenTable);

            EditorGUILayout.Space(5);

            if (GUILayout.Button("Test IsValidCallShot"))
            {
                bool valid = ChinesePoolRules.IsValidCallShot(1, 0, testAvailableBalls, testAvailablePockets);
                Debug.Log($"[ChinesePoolRules] IsValidCallShot(1, 0): {valid}");
            }

            if (GUILayout.Button("Test IsLegalShot (Open Table)"))
            {
                bool legal = ChinesePoolRules.IsLegalShot(1, 1, 0, 1, 0, testAssignedGroup, true, false);
                Debug.Log($"[ChinesePoolRules] IsLegalShot (Open Table, Ball 1->Pocket 0): {legal}");
            }

            if (GUILayout.Button("Test IsLegalShot (Group Assigned)"))
            {
                bool legal = ChinesePoolRules.IsLegalShot(1, 1, 0, 1, 0, testAssignedGroup, false, false);
                Debug.Log($"[ChinesePoolRules] IsLegalShot (Group {testAssignedGroup}, Ball 1->Pocket 0): {legal}");
            }

            if (GUILayout.Button("Test IsFoul (Cue Ball Potted)"))
            {
                bool foul = ChinesePoolRules.IsFoul(1, 1, 1, 0, 1, 0, testAssignedGroup, testIsOpenTable, false);
                Debug.Log($"[ChinesePoolRules] IsFoul (Cue Ball Potted): {foul}");
            }

            if (GUILayout.Button("Test IsFoul (Wrong Ball First)"))
            {
                bool foul = ChinesePoolRules.IsFoul(0, 9, 1, 0, 1, 0, ChinesePoolBallType.Red, false, false);
                Debug.Log($"[ChinesePoolRules] IsFoul (Wrong Ball First - hit Yellow when Red assigned): {foul}");
            }

            if (GUILayout.Button("Test GetBallsInGroup"))
            {
                var redBalls = ChinesePoolRules.GetBallsInGroup(ChinesePoolBallType.Red);
                var yellowBalls = ChinesePoolRules.GetBallsInGroup(ChinesePoolBallType.Yellow);
                Debug.Log($"[ChinesePoolRules] Red Balls: [{string.Join(", ", redBalls)}]");
                Debug.Log($"[ChinesePoolRules] Yellow Balls: [{string.Join(", ", yellowBalls)}]");
            }

            if (GUILayout.Button("Test AreGroupBallsCleared"))
            {
                int[] remainingBalls = new int[] { 8, 9, 10, 11, 12, 13, 14, 15 };
                bool cleared = ChinesePoolRules.AreGroupBallsCleared(ChinesePoolBallType.Red, remainingBalls);
                Debug.Log($"[ChinesePoolRules] Red group cleared (only black+yellows left): {cleared}");

                remainingBalls = new int[] { 1, 2, 8, 9, 10 };
                cleared = ChinesePoolRules.AreGroupBallsCleared(ChinesePoolBallType.Red, remainingBalls);
                Debug.Log($"[ChinesePoolRules] Red group cleared (reds remain): {cleared}");
            }

            if (GUILayout.Button("Test CalculateFoulPenalty"))
            {
                int penalty = ChinesePoolRules.CalculateFoulPenalty(true, false, false, false);
                Debug.Log($"[ChinesePoolRules] Standard foul penalty: {penalty}");

                penalty = ChinesePoolRules.CalculateFoulPenalty(false, false, false, true);
                Debug.Log($"[ChinesePoolRules] Black ball foul penalty: {penalty}");
            }

            EditorGUI.indentLevel--;
        }

        private void DrawAITestSection()
        {
            showAITest = EditorGUILayout.Foldout(showAITest, "AI Test", true);
            if (!showAITest) return;

            EditorGUI.indentLevel++;

            if (GUILayout.Button("Test AI Call Shot (Open Table)"))
            {
                var result = aiModifier.DecideCallShot();
                Debug.Log($"[ChinesePoolAI] Open Table Call Shot: Ball {result.ballId}, Pocket {result.pocketId}");
            }

            if (GUILayout.Button("Test AI Shot Parameters"))
            {
                var shotParams = aiModifier.DecideShotParameters(1, 0);
                Debug.Log($"[ChinesePoolAI] Shot Params - Aim: {shotParams.aimPoint}, Power: {shotParams.power:F2}, Spin: {shotParams.spin}");
            }

            EditorGUILayout.Space(5);

            GUILayout.Label("AI Difficulty", EditorStyles.boldLabel);
            var newDifficulty = (ChinesePoolAIModifier.AIDifficulty)EditorGUILayout.EnumPopup("Difficulty", aiModifier.GetDifficulty());
            if (newDifficulty != aiModifier.GetDifficulty())
            {
                aiModifier.SetDifficulty(newDifficulty);
                Debug.Log($"[ChinesePoolAI] Difficulty changed to: {newDifficulty}");
            }

            EditorGUI.indentLevel--;
        }

        private void DrawBallTypeReference()
        {
            GUILayout.Label("Ball Type Reference", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            EditorGUILayout.LabelField("Ball 0", "Cue Ball (White)");
            EditorGUILayout.LabelField("Balls 1-7", "Red (Solid)");
            EditorGUILayout.LabelField("Ball 8", "Black Ball (8-ball)");
            EditorGUILayout.LabelField("Balls 9-15", "Yellow (Stripe)");

            EditorGUILayout.Space(5);

            EditorGUILayout.LabelField("Rack Layout (Standard Chinese 8-Ball):", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Row 1 (Apex):", "Ball 1 (Red)");
            EditorGUILayout.LabelField("Row 2:", "Ball 2 (Red), Ball 3 (Red)");
            EditorGUILayout.LabelField("Row 3:", "Ball 4 (Red), Ball 8 (Black), Ball 5 (Red)");
            EditorGUILayout.LabelField("Row 4:", "Ball 6 (Red), Ball 9 (Yellow), Ball 10 (Yellow), Ball 7 (Red)");
            EditorGUILayout.LabelField("Row 5:", "Ball 11 (Yellow), Ball 12 (Yellow), Ball 13 (Yellow), Ball 14 (Yellow), Ball 15 (Yellow)");

            EditorGUI.indentLevel--;
        }

        private void RefreshReferences()
        {
            ballSetup = FindObjectOfType<ChinesePoolBallSetup>();
            aiModifier = FindObjectOfType<ChinesePoolAIModifier>();

            if (ballSetup != null)
                Debug.Log("[ChinesePoolEditor] Found ChinesePoolBallSetup");
            else
                Debug.LogWarning("[ChinesePoolEditor] ChinesePoolBallSetup not found in scene");

            if (aiModifier != null)
                Debug.Log("[ChinesePoolEditor] Found ChinesePoolAIModifier");
            else
                Debug.LogWarning("[ChinesePoolEditor] ChinesePoolAIModifier not found in scene");
        }
    }
}