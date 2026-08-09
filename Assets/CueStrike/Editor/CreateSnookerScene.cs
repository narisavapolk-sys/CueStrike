#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using CueStrike;

public static class CreateSnookerScene
{
    /// <summary>
    /// Creates a Snooker sample scene with:
    ///  - Main Camera + Directional Light + Ground
    ///  - CueStrikeTable_Snooker12ft table
    ///  - CueStrikeWBPSRuleset game rules
    ///  - Full Snooker ball set (15 reds, 6 colors, cue ball)
    /// </summary>
    [MenuItem("CueStrike/Generate/Snooker Scene (WBPS Rules)")]
    public static void CreateSnookerDemoScene()
    {
        // Guard: Cannot run in Play Mode
        if (EditorApplication.isPlaying)
        {
            Debug.LogError("[Snooker Scene] Cannot generate scene while in Play Mode. Please exit Play Mode first.");
            EditorUtility.DisplayDialog("Cannot Generate", "Stop Play Mode first!", "OK");
            return;
        }

        // Create new scene
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "Snooker_Demo";

        // ── Camera ───────────────────────────────────────────────
        var camGO = new GameObject("Main Camera");
        var cam = camGO.AddComponent<Camera>();
        cam.tag = "MainCamera";
        cam.transform.position = new Vector3(0f, 4.5f, -8.5f);
        cam.transform.rotation = Quaternion.Euler(25f, 0f, 0f);
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.12f, 0.18f, 0.25f);

        // ── Directional light ────────────────────────────────────
        var lightGO = new GameObject("Directional Light");
        var light = lightGO.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.2f;
        light.color = Color.white;
        lightGO.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

        // ── Ground ───────────────────────────────────────────────
        var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.position = Vector3.zero;
        ground.transform.localScale = Vector3.one * 2f;

        // ── Snooker table prefab ─────────────────────────────────
        GameObject tableInstance = null;
        var tablePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/CueStrike/Prefabs/Tables/CueStrikeTable_Snooker12ft.prefab");
        if (tablePrefab != null)
        {
            tableInstance = (GameObject)PrefabUtility.InstantiatePrefab(tablePrefab);
            tableInstance.name = "CueStrikeTable_Snooker12ft";
            tableInstance.transform.position = new Vector3(0f, 0f, 0f);
        }
        else
        {
            // Fallback placeholder if prefab is missing
            tableInstance = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tableInstance.name = "SnookerTable_Placeholder";
            tableInstance.transform.position = new Vector3(0f, 0.5f, 0f);
            tableInstance.transform.localScale = new Vector3(3.6f, 1f, 1.8f);
        }

        // ── WBPS Snooker rules ───────────────────────────────────
        var rulesGO = new GameObject("CueStrikeWBPSRuleset");
        var rules = rulesGO.AddComponent<CueStrikeWBPSRuleset>();
        rules.totalRedBalls = 15;
        rules.minFoulPoints = 4;
        rules.maxFoulPoints = 7;
        rules.ResetFrame();

        // ── Snooker ball set ─────────────────────────────────────
        // WBPS table coordinate: length along Z, width along X.
        // Break-off: cue ball behind the baulk line (Z negative), rack near Z positive.
        float ballRadius = 0.026f;      // 52mm snooker ball
        float ballY = 0.42f + ballRadius; // rest just on the table bed

        // Rack apex (black side) placed ~1.3m from the black end
        Vector3 rackApex = new Vector3(0f, ballY, 1.15f);

        // 15 reds in a triangle (side = 3 balls high)
        float redSpacing = ballRadius * 2f * 1.03f;
        int redId = 1;
        for (int row = 0; row < 5; row++)
        {
            for (int i = 0; i <= row; i++)
            {
                Vector3 pos = rackApex
                    + new Vector3((i - row * 0.5f) * redSpacing, 0f, row * redSpacing * 0.866f);
                SpawnBall("Red_" + redId, pos, redId, Color.red, rulesGO.transform);
                redId++;
            }
        }

        // Colors along the string line / baulk spots
        var colorBalls = new (string name, int id, Vector3 pos, Color color)[]
        {
            ("Yellow", 16, new Vector3(0f, ballY, -0.9f),  Color.yellow),
            ("Green",  17, new Vector3(-1.2f, ballY, -0.9f), Color.green),
            ("Brown",  18, new Vector3(1.2f, ballY, -0.9f),  new Color(0.5f, 0.3f, 0.1f)),
            ("Blue",   19, new Vector3(0f, ballY, 0f),       Color.blue),
            ("Pink",   20, new Vector3(0f, ballY, 0.8f),     new Color(1f, 0.5f, 0.7f)),
            ("Black",  21, new Vector3(0f, ballY, 1.5f),     Color.black),
        };
        foreach (var (name, id, pos, color) in colorBalls)
        {
            SpawnBall(name, pos, id, color, rulesGO.transform);
        }

        // Cue ball behind the baulk line
        SpawnBall("CueBall", new Vector3(0.4f, ballY, -1.35f), 0, Color.white, rulesGO.transform);

        // ── Save scene ───────────────────────────────────────────
        string sceneDir = "Assets/CueStrike/Scenes";
        if (!AssetDatabase.IsValidFolder(sceneDir))
        {
            AssetDatabase.CreateFolder("Assets/CueStrike", "Scenes");
        }
        string scenePath = sceneDir + "/Snooker_Demo.unity";
        EditorSceneManager.SaveScene(scene, scenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Snooker Scene",
            "Snooker sample scene created at\n" + scenePath +
            "\n\n• CueStrikeWBPSRuleset with 15 reds\n• 6 colors (Yellow→Black)\n• Cue ball behind baulk line\n" +
            "\nOpen the WBPS ruleset component to adjust ball spotting.\n" +
            "Run 'Tools/CueStrike/Debug' validation using the WBPS APIs.",
            "OK");
    }

    /// <summary>
    /// Spawns a snooker ball as a primitive sphere with BallIdentity,
    /// set to ignore physics so it is static (self-test scene only).
    /// </summary>
    private static void SpawnBall(string ballName, Vector3 position, int ballId, Color color, Transform parent)
    {
        var ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        ball.name = ballName;
        ball.transform.position = position;
        ball.transform.localScale = Vector3.one * 0.052f; // 52mm diameter
        ball.transform.SetParent(parent, true);

        var renderer = ball.GetComponent<Renderer>();
        if (renderer != null) renderer.sharedMaterial.color = color;

        // Identity is enough for a self-test scene (no physics simulation needed)
        var identity = ball.AddComponent<BallIdentity>();
        identity.ballId = ballId;
        identity.ballName = ballName;

        // Remove physics so balls stay in rack (sample only)
        var collider = ball.GetComponent<Collider>();
        if (collider != null) Object.DestroyImmediate(collider);
        var rb = ball.GetComponent<Rigidbody>();
        if (rb != null) Object.DestroyImmediate(rb);
    }
}
#endif
