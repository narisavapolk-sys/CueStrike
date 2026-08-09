#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;
using CueStrike;

public static class CueStrikeBatchSetup
{
    [MenuItem("CueStrike/Tools/Run Batch Setup")] 
    public static void Run()
    {
        // 1) Ensure folders
        if (!AssetDatabase.IsValidFolder("Assets/CueStrike")) AssetDatabase.CreateFolder("Assets", "CueStrike");
        if (!AssetDatabase.IsValidFolder("Assets/CueStrike/Cues")) AssetDatabase.CreateFolder("Assets/CueStrike", "Cues");
        if (!AssetDatabase.IsValidFolder("Assets/CueStrike/Prefabs")) AssetDatabase.CreateFolder("Assets/CueStrike", "Prefabs");
        if (!AssetDatabase.IsValidFolder("Assets/CueStrike/Prefabs/Tables")) AssetDatabase.CreateFolder("Assets/CueStrike/Prefabs", "Tables");

        // 2) Create sample CueProfile assets
        CreateCueProfile("AshCue", 1.45f, 0.48f, CueProfile.MaterialType.Wood, 0.55f);
        CreateCueProfile("CarbonCue", 1.45f, 0.42f, CueProfile.MaterialType.Carbon, 0.72f);
        CreateCueProfile("ProCue", 1.48f, 0.5f, CueProfile.MaterialType.Carbon, 0.85f);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // 3) Create HUD prefab
        HUDPrefabCreator.CreateHUDPrefab();

        // 4) Create table prefabs
        TablePrefabCreator.GenerateTablePrefabs();

        // 5) Assign table prefabs to physics manager (if present in scene)
        TableAutoAssign.Assign();

        // 6) Auto-rack each table prefab and save back to prefab
        var guids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/CueStrike/Prefabs/Tables" });
        foreach (var g in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(g);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) continue;

            // instantiate temporarily
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            // compute center from TableSurface if exists
            var tableSurface = instance.transform.Find("TableSurface");
            Vector3 center = instance.transform.position;
            string lname = instance.name.ToLower();
            bool isSnooker = lname.Contains("snooker");
            float ballRadius = isSnooker ? 0.02625f : 0.028575f; // WPBSA snooker vs WPA pool
            if (tableSurface != null) center = tableSurface.position;

            // decide rack type
            // decide rack type
            System.Collections.Generic.List<Vector3> positions = new System.Collections.Generic.List<Vector3>();
            System.Collections.Generic.List<int> ids = new System.Collections.Generic.List<int>();
            int rows = 5;
            float spacing = ballRadius * 2f * 1.01f;

            if (lname.Contains("snooker"))
            {
                // Cue Ball (White)
                positions.Add(center + new Vector3(0f, ballRadius, 1.25f));
                ids.Add(0);

                // Yellow (ID 1)
                positions.Add(center + new Vector3(0.2921f, ballRadius, 1.0475f));
                ids.Add(1);

                // Green (ID 2)
                positions.Add(center + new Vector3(-0.2921f, ballRadius, 1.0475f));
                ids.Add(2);

                // Brown (ID 3)
                positions.Add(center + new Vector3(0f, ballRadius, 1.0475f));
                ids.Add(3);

                // Blue (ID 4)
                positions.Add(center + new Vector3(0f, ballRadius, 0f));
                ids.Add(4);

                // Pink (ID 5)
                positions.Add(center + new Vector3(0f, ballRadius, -0.89225f));
                ids.Add(5);

                // Black (ID 6)
                positions.Add(center + new Vector3(0f, ballRadius, -1.4605f));
                ids.Add(6);

                // 15 Reds (IDs 7 to 21) pointing to Pink, spreading towards Black
                float apexZ = -0.89225f - ballRadius * 2.05f;
                int redId = 7;
                for (int r = 0; r < rows; r++)
                {
                    int count = r + 1;
                    float offsetZ = apexZ - r * spacing * 0.866f;
                    float rowStartX = - r * spacing * 0.5f;
                    for (int c = 0; c < count; c++)
                    {
                        positions.Add(center + new Vector3(rowStartX + c * spacing, ballRadius, offsetZ));
                        ids.Add(redId++);
                    }
                }
            }
            else
            {
                // Pool rack: 15 balls in a triangle
                int poolId = 1;
                float apexZ = -0.8f;
                for (int r = 0; r < rows; r++)
                {
                    int count = r + 1;
                    float offsetZ = apexZ - r * spacing * 0.866f;
                    float rowStartX = - r * spacing * 0.5f;
                    for (int c = 0; c < count; c++)
                    {
                        positions.Add(center + new Vector3(rowStartX + c * spacing, ballRadius, offsetZ));
                        ids.Add(poolId++);
                    }
                }
                // Add Cue Ball (White)
                positions.Add(center + new Vector3(0f, ballRadius, 0.8f));
                ids.Add(0);
            }

            // remove existing balls under instance to avoid duplicates
            var existingBalls = instance.GetComponentsInChildren<BallIdentity>(true);
            foreach (var eb in existingBalls)
            {
                GameObject.DestroyImmediate(eb.gameObject);
            }

            var ballPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/CueStrike/Prefabs/CueStrikeBall.prefab");
            for (int i = 0; i < positions.Count; i++)
            {
                Vector3 pos = positions[i];
                int currentId = ids[i];
                GameObject ball;
                Rigidbody rb = null;
                if (ballPrefab != null)
                {
                    ball = (GameObject)PrefabUtility.InstantiatePrefab(ballPrefab);
                }
                else
                {
                    ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    rb = ball.GetComponent<Rigidbody>();
                    if (rb == null) rb = ball.AddComponent<Rigidbody>();
                    rb.mass = 0.17f;
                    ball.tag = "Ball";
                }
                ball.name = "CueStrikeBall_" + currentId;
                ball.transform.position = pos;
                ball.transform.SetParent(instance.transform, true);
                var col = ball.GetComponent<SphereCollider>();
                if (col == null) col = ball.AddComponent<SphereCollider>();
                col.radius = ballRadius;
                rb = ball.GetComponent<Rigidbody>();
                if (rb == null) rb = ball.AddComponent<Rigidbody>();
                rb.mass = 0.17f;
                ball.tag = "Ball";
                var idComp = ball.GetComponent<BallIdentity>();
                if (idComp == null) idComp = ball.AddComponent<BallIdentity>();
                idComp.ballId = currentId;

                // Color the ball based on ID immediately for editor view
                var rend = ball.GetComponent<Renderer>();
                if (rend != null)
                {
                    Material ballMat = null;
                    if (lname.Contains("snooker"))
                    {
                        if (currentId == 0) ballMat = Resources.Load<Material>("CueStrike/BallMaterials/Ball_0");
                        else if (currentId == 1) ballMat = Resources.Load<Material>("CueStrike/BallMaterials/Ball_1");
                        else if (currentId == 2) ballMat = Resources.Load<Material>("CueStrike/BallMaterials/Ball_6");
                        else if (currentId == 3) ballMat = Resources.Load<Material>("CueStrike/BallMaterials/Ball_7");
                        else if (currentId == 4) ballMat = Resources.Load<Material>("CueStrike/BallMaterials/Ball_Blue");
                        else if (currentId == 5) ballMat = Resources.Load<Material>("CueStrike/BallMaterials/Ball_Pink");
                        else if (currentId == 6) ballMat = Resources.Load<Material>("CueStrike/BallMaterials/Ball_Black");
                        else ballMat = Resources.Load<Material>("CueStrike/BallMaterials/Ball_Red");
                    }
                    else
                    {
                        int clampedId = Mathf.Clamp(currentId, 0, 15);
                        ballMat = Resources.Load<Material>("CueStrike/BallMaterials/Ball_" + clampedId);
                    }
                    if (ballMat != null) rend.sharedMaterial = ballMat;
                }
            }

            // save back to prefab
            PrefabUtility.SaveAsPrefabAsset(instance, path);
            GameObject.DestroyImmediate(instance);
            Debug.Log("Auto-racked and saved: " + path);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("CueStrike Batch", "Batch setup complete. Check Assets/CueStrike/Prefabs and Cues.", "OK");
    }

    static void CreateCueProfile(string name, float length, float mass, CueProfile.MaterialType mat, float spin)
    {
        string path = $"Assets/CueStrike/Cues/{name}.asset";
        if (File.Exists(Path.GetFullPath(path))) return;
        var cp = ScriptableObject.CreateInstance<CueProfile>();
        cp.cueName = name;
        cp.length = length;
        cp.mass = mass;
        cp.material = mat;
        cp.spinEfficiency = spin;
        AssetDatabase.CreateAsset(cp, path);
        Debug.Log("Created CueProfile: " + path);

        // create a runtime copy under Resources for builds
        string resDir = "Assets/Resources/CueStrike/Cues";
        if (!AssetDatabase.IsValidFolder("Assets/Resources")) AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder("Assets/Resources/CueStrike")) AssetDatabase.CreateFolder("Assets/Resources", "CueStrike");
        if (!AssetDatabase.IsValidFolder(resDir)) AssetDatabase.CreateFolder("Assets/Resources/CueStrike", "Cues");
        string resPath = $"{resDir}/{name}.asset";
        if (!File.Exists(Path.GetFullPath(resPath)))
        {
            var cp2 = ScriptableObject.CreateInstance<CueProfile>();
            cp2.cueName = cp.cueName;
            cp2.length = cp.length;
            cp2.mass = cp.mass;
            cp2.balancePoint = cp.balancePoint;
            cp2.tipSize = cp.tipSize;
            cp2.material = cp.material;
            cp2.spinEfficiency = cp.spinEfficiency;
            AssetDatabase.CreateAsset(cp2, resPath);
            Debug.Log("Created runtime CueProfile copy: " + resPath);
        }
    }
}
#endif