#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using CueStrike.AI;
using CueStrike;

public static class GenerateDemoScene
{
    [MenuItem("CueStrike/Generate/Demo Scene (AI + HUD + Table)")]
    public static void CreateDemoScene()
    {
        // Guard: Cannot run in Play Mode
        if (EditorApplication.isPlaying)
        {
            Debug.LogError("[Demo Scene] Cannot generate demo scene while in Play Mode. Please exit Play Mode first.");
            EditorUtility.DisplayDialog("Cannot Generate", "Stop Play Mode first!", "OK");
            return;
        }

        // Create new scene
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "CueStrike_Demo";

        // Camera
        var camGO = new GameObject("Main Camera");
        var cam = camGO.AddComponent<Camera>();
        cam.tag = "MainCamera";
        cam.transform.position = new Vector3(0f, 3f, -6f);
        cam.transform.rotation = Quaternion.Euler(20f, 0f, 0f);

        // Directional light
        var lightGO = new GameObject("Directional Light");
        var light = lightGO.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1f;
        light.color = Color.white;
        lightGO.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

        // Ground plane
        var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.position = Vector3.zero;
        ground.transform.localScale = Vector3.one * 2f;
        var groundMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/CueStrike/Materials/Wood_Table.mat");
        if (groundMat != null)
        {
            var rend = ground.GetComponent<Renderer>();
            rend.sharedMaterial = groundMat;
        }

        // Table prefab
        string tableDir = "Assets/CueStrike/Prefabs/Tables";
        var guids = AssetDatabase.FindAssets("t:Prefab", new[] { tableDir });
        GameObject tableInstance = null;
        if (guids != null && guids.Length > 0)
        {
            var path = AssetDatabase.GUIDToAssetPath(guids[0]);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null)
            {
                tableInstance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                tableInstance.name = "CueStrikeTable_Instance";
                tableInstance.transform.position = new Vector3(0f, 0.5f, 0f);
            }
        }

        // HUD prefab
        var hudPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/CueStrike/Prefabs/CueStrikeHUD.prefab");
        if (hudPrefab != null)
        {
            var hud = (GameObject)PrefabUtility.InstantiatePrefab(hudPrefab);
            hud.name = "CueStrikeHUD";
        }

        // Managers
        var physicsMgrGO = new GameObject("CueStrikePhysicsManager");
        var physicsMgr = physicsMgrGO.AddComponent<CueStrikePhysicsManager>();
        if (tableInstance != null) physicsMgr.tablePrefab = tableInstance;

        var shotMgrGO = new GameObject("CueStrikeShotManager");
        var shotMgr = shotMgrGO.AddComponent<CueStrikeShotManager>();
        shotMgr.physicsManager = physicsMgr;

        var rulesGO = new GameObject("CueStrikeRulesManager");
        rulesGO.AddComponent<CueStrikeRulesManager>();

        var turnGO = new GameObject("CueStrikeTurnManager");
        turnGO.AddComponent<CueStrikeTurnManager>();

        // AI Controller
        var aiGO = new GameObject("CueStrikeAIController");
        var ai = aiGO.AddComponent<CueStrike.AI.CueStrikeAIController>();
        ai.SetSkillLevel(CueStrike.AI.SkillLevel.Medium);

        // Auto demo runner
        var demoGO = new GameObject("CueStrikeAutoDemo");
        var demo = demoGO.AddComponent<CueStrikeAutoDemo>();
        demo.aiController = ai;
        demo.delayBeforeShot = 1.5f;
        demo.autoRepeat = true;
        demo.repeatDelay = 4f;

        // Create a cue ball if none exists
        var existingBalls = GameObject.FindGameObjectsWithTag("Ball");
        if (existingBalls == null || existingBalls.Length == 0)
        {
            var cueBall = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            cueBall.name = "CueBall";
            cueBall.tag = "Ball";
            cueBall.transform.position = new Vector3(0f, 0.055f, -1.2f);
            var rb = cueBall.AddComponent<Rigidbody>();
            rb.mass = 0.17f;
            cueBall.AddComponent<BallIdentity>().ballId = 0;
            cueBall.AddComponent<CueStrikeBallPhysics>();
            if (physicsMgr != null) physicsMgr.ResetBalls();
        }

        // Save scene
        string sceneDir = "Assets/CueStrike/Scenes";
        if (!AssetDatabase.IsValidFolder("Assets/CueStrike/Scenes")) AssetDatabase.CreateFolder("Assets/CueStrike", "Scenes");
        string scenePath = sceneDir + "/CueStrike_Demo.unity";
        EditorSceneManager.SaveScene(scene, scenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("CueStrike Demo", "Demo scene created at " + scenePath, "OK");
    }
}
#endif