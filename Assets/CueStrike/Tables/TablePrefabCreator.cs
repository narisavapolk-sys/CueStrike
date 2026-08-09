#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using CueStrike;

// Editor utility to create placeholder table prefabs (snooker 12ft, pool 8/9)
public static class TablePrefabCreator
{
    [MenuItem("CueStrike/Generate/Table Prefabs")]
    public static void GenerateTablePrefabs()
    {
        CreateSnooker12ft();
        CreatePoolTable("Pool_8Ball", 2.34f, "CueStrikeTable_Pool_8Ball"); // 8ft Pool standard
        CreatePoolTable("Pool_9Ball", 2.54f, "CueStrikeTable_Pool_9Ball"); // 9ft Pool standard
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("CueStrike: Table prefabs created");
    }

    [MenuItem("CueStrike/Tools/Auto Rack")]
    public static void AutoRackSelected()
    {
        // Apply rack to selected table prefab or scene object
        var go = Selection.activeGameObject;
        string assetPath = AssetDatabase.GetAssetPath(Selection.activeObject);

        GameObject instance = null;
        bool createdTemp = false;

        if (!string.IsNullOrEmpty(assetPath) && assetPath.EndsWith(".prefab"))
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (prefab == null)
            {
                Debug.LogError("Selected asset is not a prefab.");
                return;
            }
            instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            createdTemp = true;
        }
        else if (go != null)
        {
            instance = go;
        }
        else
        {
            EditorUtility.DisplayDialog("Auto Rack", "Select a table prefab or GameObject in the scene.", "OK");
            return;
        }

        // Determine table surface to compute rack positions
        var tableSurface = instance.transform.Find("TableSurface");
        Vector3 center = instance.transform.position;
        Vector3 tableScale = Vector3.one;
        if (tableSurface != null)
        {
            center = tableSurface.position;
            tableScale = tableSurface.localScale;
        }

        string lname = instance.name.ToLower();
        bool isSnooker = lname.Contains("snooker");
        float ballRadius = isSnooker ? 0.02625f : 0.028575f; // WPBSA snooker (52.5mm dia) vs WPA pool (57.15mm dia)
        var positions = new System.Collections.Generic.List<Vector3>();
        if (lname.Contains("snooker"))
        {
            // snooker triangle rows 1..5, centered on table
            int rows = 5;
            float spacing = ballRadius * 2f * 1.01f;
            for (int r = 0; r < rows; r++)
            {
                int count = r + 1;
                float offsetZ = -(rows - 1) * spacing * 0.5f + r * spacing;
                float rowStartX = - (count - 1) * spacing * 0.5f;
                for (int c = 0; c < count; c++)
                {
                    Vector3 pos = center + new Vector3(rowStartX + c * spacing, ballRadius, offsetZ);
                    positions.Add(pos);
                }
            }
        }
        else if (lname.Contains("9ball") || lname.Contains("9_ball") || lname.Contains("9"))
        {
            // 9-ball diamond approximated as 5-row triangle for placement
            int rows = 5;
            float spacing = ballRadius * 2f * 1.01f;
            for (int r = 0; r < rows; r++)
            {
                int count = r + 1;
                float offsetZ = -(rows - 1) * spacing * 0.5f + r * spacing;
                float rowStartX = - (count - 1) * spacing * 0.5f;
                for (int c = 0; c < count; c++)
                {
                    Vector3 pos = center + new Vector3(rowStartX + c * spacing, ballRadius, offsetZ);
                    positions.Add(pos);
                }
            }
        }
        else
        {
            // default 8-ball rack (triangle)
            int rows = 5;
            float spacing = ballRadius * 2f * 1.01f;
            for (int r = 0; r < rows; r++)
            {
                int count = r + 1;
                float offsetZ = -(rows - 1) * spacing * 0.5f + r * spacing;
                float rowStartX = - (count - 1) * spacing * 0.5f;
                for (int c = 0; c < count; c++)
                {
                    Vector3 pos = center + new Vector3(rowStartX + c * spacing, ballRadius, offsetZ);
                    positions.Add(pos);
                }
            }
        }

        // Spawn balls as children of the table instance (editor-safe)
        int id = 1;
        foreach (var p in positions)
        {
            var ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ball.name = "CueStrikeBall_" + id;
            ball.transform.position = p;
            ball.transform.SetParent(instance.transform, true);
            var col = ball.GetComponent<SphereCollider>();
            if (col == null) col = ball.AddComponent<SphereCollider>();
            col.radius = ballRadius;
            var rb = ball.GetComponent<Rigidbody>();
            if (rb == null) rb = ball.AddComponent<Rigidbody>();
            rb.mass = 0.17f;
            ball.tag = "Ball";
            var idComp = ball.GetComponent<BallIdentity>();
            if (idComp == null) idComp = ball.AddComponent<BallIdentity>();
            idComp.ballId = id;
            id++;
        }

        // If we instantiated a prefab, save changes back to prefab and destroy temp
        if (createdTemp)
        {
            var path = AssetDatabase.GetAssetPath(Selection.activeObject);
            PrefabUtility.SaveAsPrefabAsset(instance, path);
            GameObject.DestroyImmediate(instance);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Auto Rack: applied and saved to prefab.");
        }
        else
        {
            Debug.Log("Auto Rack: spawned balls in scene for selected table.");
        }
    }

    private static void CreateSnooker12ft()
    {
        var dir = "Assets/CueStrike/Prefabs/Tables";
        if (!AssetDatabase.IsValidFolder(dir)) AssetDatabase.CreateFolder("Assets/CueStrike/Prefabs", "Tables");
        var path = dir + "/CueStrikeTable_Snooker12ft.prefab";

        // Remove old prefab if it exists to ensure overwrite
        AssetDatabase.DeleteAsset(path);

        var root = new GameObject("CueStrikeTable_Snooker12ft");
        var table = GameObject.CreatePrimitive(PrimitiveType.Cube);
        table.name = "TableSurface";
        table.transform.SetParent(root.transform, false);
        
        // WPBSA Snooker Table dimensions (12ft): 3.569m long, 1.778m wide
        float width = 1.778f;
        float length = 3.569f;
        table.transform.localScale = new Vector3(width, 0.1f, length);
        table.transform.localPosition = Vector3.zero;
        var tableCollider = table.GetComponent<BoxCollider>();
        tableCollider.material = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>("Assets/CueStrike/Physics/Materials/TableFelt.asset");

        // Load Wood Material for cushions and legs
        Material woodMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/CueStrike/Materials/Wood_Table.mat");

        // Official Snooker pocket positions (4 corners, 2 center long sides)
        float hW = width / 2.0f;
        float hL = length / 2.0f;
        CreatePocket(root.transform, new Vector3(hW, 0f, hL));
        CreatePocket(root.transform, new Vector3(-hW, 0f, hL));
        CreatePocket(root.transform, new Vector3(hW, 0f, -hL));
        CreatePocket(root.transform, new Vector3(-hW, 0f, -hL));
        CreatePocket(root.transform, new Vector3(hW, 0f, 0f));
        CreatePocket(root.transform, new Vector3(-hW, 0f, 0f));

        // Create 8 table legs (4 on each long side for standard 12ft snooker table)
        float legX = hW - 0.15f;
        CreateLeg(root.transform, new Vector3(legX, -0.45f, hL - 0.25f), woodMat);
        CreateLeg(root.transform, new Vector3(-legX, -0.45f, hL - 0.25f), woodMat);
        CreateLeg(root.transform, new Vector3(legX, -0.45f, hL * 0.33f), woodMat);
        CreateLeg(root.transform, new Vector3(-legX, -0.45f, hL * 0.33f), woodMat);
        CreateLeg(root.transform, new Vector3(legX, -0.45f, -hL * 0.33f), woodMat);
        CreateLeg(root.transform, new Vector3(-legX, -0.45f, -hL * 0.33f), woodMat);
        CreateLeg(root.transform, new Vector3(legX, -0.45f, -hL + 0.25f), woodMat);
        CreateLeg(root.transform, new Vector3(-legX, -0.45f, -hL + 0.25f), woodMat);

        // Create Official WPBSA ball spots (thin cylinders sitting on top of felt)
        CreateBallSpot(root.transform, "Spot_Black", new Vector3(0f, 0.051f, -1.4605f), Color.black);
        CreateBallSpot(root.transform, "Spot_Pink", new Vector3(0f, 0.051f, -0.89225f), new Color(1f, 0.4f, 0.7f));
        CreateBallSpot(root.transform, "Spot_Blue", new Vector3(0f, 0.051f, 0f), Color.blue);
        CreateBallSpot(root.transform, "Spot_Brown", new Vector3(0f, 0.051f, 1.0475f), new Color(0.5f, 0.25f, 0f));
        CreateBallSpot(root.transform, "Spot_Green", new Vector3(-0.2921f, 0.051f, 1.0475f), Color.green);
        CreateBallSpot(root.transform, "Spot_Yellow", new Vector3(0.2921f, 0.051f, 1.0475f), Color.yellow);

        var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        GameObject.DestroyImmediate(root);
    }

    private static void CreatePoolTable(string name, float length, string prefabName)
    {
        var dir = "Assets/CueStrike/Prefabs/Tables";
        if (!AssetDatabase.IsValidFolder(dir)) AssetDatabase.CreateFolder("Assets/CueStrike/Prefabs", "Tables");
        var path = $"{dir}/{prefabName}.prefab";

        AssetDatabase.DeleteAsset(path);

        var root = new GameObject(prefabName);
        var table = GameObject.CreatePrimitive(PrimitiveType.Cube);
        table.name = "TableSurface";
        table.transform.SetParent(root.transform, false);
        
        // Pool table width is exactly half of its length (1:2 ratio)
        float width = length / 2.0f;
        table.transform.localScale = new Vector3(width, 0.1f, length);
        table.transform.localPosition = Vector3.zero;
        var tableCollider = table.GetComponent<BoxCollider>();
        tableCollider.material = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>("Assets/CueStrike/Physics/Materials/TableFelt.asset");

        Material woodMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/CueStrike/Materials/Wood_Table.mat");

        // Official WPA Pool pocket positions (4 corners, 2 center long sides)
        float hW = width / 2.0f;
        float hL = length / 2.0f;
        CreatePocket(root.transform, new Vector3(hW, 0f, hL));
        CreatePocket(root.transform, new Vector3(-hW, 0f, hL));
        CreatePocket(root.transform, new Vector3(hW, 0f, -hL));
        CreatePocket(root.transform, new Vector3(-hW, 0f, -hL));
        CreatePocket(root.transform, new Vector3(hW, 0f, 0f));
        CreatePocket(root.transform, new Vector3(-hW, 0f, 0f));

        // Create 4 table legs
        float legX = hW - 0.1f;
        float legZ = hL - 0.2f;
        CreateLeg(root.transform, new Vector3(legX, -0.45f, legZ), woodMat);
        CreateLeg(root.transform, new Vector3(-legX, -0.45f, legZ), woodMat);
        CreateLeg(root.transform, new Vector3(legX, -0.45f, -legZ), woodMat);
        CreateLeg(root.transform, new Vector3(-legX, -0.45f, -legZ), woodMat);

        var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        GameObject.DestroyImmediate(root);
    }

    private static void CreatePocket(Transform parent, Vector3 localPos)
    {
        var pocketGO = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        pocketGO.name = "Pocket";
        pocketGO.transform.SetParent(parent, false);
        pocketGO.transform.localScale = new Vector3(0.2f, 0.01f, 0.2f);
        pocketGO.transform.localPosition = localPos;
        var col = pocketGO.GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
        
        var script = pocketGO.GetComponent<Pocket>();
        if (script == null) script = pocketGO.AddComponent<Pocket>();
        script.scoreValue = 1;
    }

    private static void CreateLeg(Transform parent, Vector3 localPos, Material woodMat)
    {
        var legGO = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        legGO.name = "TableLeg";
        legGO.transform.SetParent(parent, false);
        
        // 0.8m height in Unity cylinder primitive (default height 2 units, so scale 0.4 gives 0.8m)
        legGO.transform.localScale = new Vector3(0.12f, 0.4f, 0.12f);
        legGO.transform.localPosition = localPos;

        // Remove cylinder collider to prevent physics collision bugs with sticks/balls below table
        var col = legGO.GetComponent<Collider>();
        if (col != null) GameObject.DestroyImmediate(col);

        if (woodMat != null)
        {
            var rend = legGO.GetComponent<Renderer>();
            if (rend != null) rend.sharedMaterial = woodMat;
        }
    }

    private static void CreateBallSpot(Transform parent, string name, Vector3 localPos, Color color)
    {
        var spotGO = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        spotGO.name = name;
        spotGO.transform.SetParent(parent, false);
        spotGO.transform.localScale = new Vector3(0.06f, 0.001f, 0.06f); // flat spot
        spotGO.transform.localPosition = localPos;
        
        var col = spotGO.GetComponent<Collider>();
        if (col != null) GameObject.DestroyImmediate(col);

        var rend = spotGO.GetComponent<Renderer>();
        if (rend != null)
        {
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = color;
            mat.SetFloat("_Smoothness", 0.0f);
            rend.sharedMaterial = mat;
        }
    }
}
#endif