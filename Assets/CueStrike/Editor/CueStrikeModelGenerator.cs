#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;
using CueStrike;

/// <summary>
/// Generates proper placeholder prefabs for CueStrike game objects.
/// Creates snooker/pool balls with correct colors, a better-looking table,
/// and cue stick — all as prefabs with correct physics colliders.
/// Menu: CueStrike → Generate → Create Placeholder Models
/// </summary>
public static class CueStrikeModelGenerator
{
    private const string PrefabFolder = "Assets/CueStrike/Prefabs";
    private const string MatFolder    = "Assets/CueStrike/Materials/Generated";

    [MenuItem("CueStrike/Generate/Create Placeholder Models (Balls + Table + Cue)")]
    public static void GenerateAll()
    {
        Directory.CreateDirectory(Path.GetFullPath(PrefabFolder));
        Directory.CreateDirectory(Path.GetFullPath(MatFolder));

        GenerateSnookerBalls();
        GeneratePoolBalls();
        GenerateCuePrefab();
        GenerateSnookerTablePrefab();

        AssetDatabase.Refresh();
        AssetDatabase.SaveAssets();

        EditorUtility.DisplayDialog("Models Generated",
            "Placeholder prefabs created:\n\n" +
            "  • 22 Snooker balls (correct colors)\n" +
            "  • 16 Pool balls (solid + stripe colors)\n" +
            "  • 1 Cue stick\n" +
            "  • 1 Snooker table 12ft\n\n" +
            $"Location: {PrefabFolder}/",
            "OK");
    }

    // ═══════════════════════════════════════════════════════════════
    //  SNOOKER BALLS
    // ═══════════════════════════════════════════════════════════════

    private static void GenerateSnookerBalls()
    {
        string folder = PrefabFolder + "/Balls/Snooker";
        Directory.CreateDirectory(Path.GetFullPath(folder));

        // Standard snooker ball colors
        var balls = new (string name, Color color, int count)[]
        {
            ("White_CueBall",  Color.white,                              1),
            ("Red",            new Color(0.8f, 0.05f, 0.05f),           15),
            ("Yellow",         new Color(0.95f, 0.85f, 0.1f),            1),
            ("Green",          new Color(0.05f, 0.55f, 0.15f),           1),
            ("Brown",          new Color(0.45f, 0.25f, 0.1f),            1),
            ("Blue",           new Color(0.1f, 0.2f, 0.75f),             1),
            ("Pink",           new Color(0.9f, 0.45f, 0.55f),            1),
            ("Black",          new Color(0.05f, 0.05f, 0.05f),           1),
        };

        int totalCreated = 0;
        foreach (var (name, color, count) in balls)
        {
            for (int i = 0; i < count; i++)
            {
                string ballName = count > 1 ? $"Snooker_{name}_{i + 1:D2}" : $"Snooker_{name}";
                CreateBallPrefab(folder, ballName, color, 0.02625f); // 52.5mm diameter
                totalCreated++;
            }
        }
        Debug.Log($"[CueStrike Models] Created {totalCreated} snooker ball prefabs");
    }

    // ═══════════════════════════════════════════════════════════════
    //  POOL BALLS
    // ═══════════════════════════════════════════════════════════════

    private static void GeneratePoolBalls()
    {
        string folder = PrefabFolder + "/Balls/Pool";
        Directory.CreateDirectory(Path.GetFullPath(folder));

        // Pool ball colors (solid 1-7, 8 = black, stripe 9-15)
        var poolColors = new Color[]
        {
            new Color(0.95f, 0.85f, 0.1f),   // 1 Yellow
            new Color(0.1f, 0.2f, 0.75f),     // 2 Blue
            new Color(0.8f, 0.05f, 0.05f),    // 3 Red
            new Color(0.35f, 0.05f, 0.4f),    // 4 Purple
            new Color(0.9f, 0.45f, 0.1f),     // 5 Orange
            new Color(0.05f, 0.55f, 0.15f),   // 6 Green
            new Color(0.5f, 0.1f, 0.1f),      // 7 Maroon
            new Color(0.05f, 0.05f, 0.05f),   // 8 Black
            new Color(0.95f, 0.85f, 0.1f),    // 9 Yellow stripe
            new Color(0.1f, 0.2f, 0.75f),     // 10 Blue stripe
            new Color(0.8f, 0.05f, 0.05f),    // 11 Red stripe
            new Color(0.35f, 0.05f, 0.4f),    // 12 Purple stripe
            new Color(0.9f, 0.45f, 0.1f),     // 13 Orange stripe
            new Color(0.05f, 0.55f, 0.15f),   // 14 Green stripe
            new Color(0.5f, 0.1f, 0.1f),      // 15 Maroon stripe
        };

        // Cue ball
        CreateBallPrefab(folder, "Pool_CueBall", Color.white, 0.02865f); // 57.15mm

        for (int i = 0; i < 15; i++)
        {
            string ballName = $"Pool_Ball_{i + 1:D2}";
            // Stripes (9-15) get slightly different smoothness to visually differentiate
            CreateBallPrefab(folder, ballName, poolColors[i], 0.02865f, i >= 8);
        }
        Debug.Log("[CueStrike Models] Created 16 pool ball prefabs");
    }

    // ═══════════════════════════════════════════════════════════════
    //  CUE STICK
    // ═══════════════════════════════════════════════════════════════

    private static void GenerateCuePrefab()
    {
        string folder = PrefabFolder + "/Cues";
        Directory.CreateDirectory(Path.GetFullPath(folder));

        var cueRoot = new GameObject("CueStick_Placeholder");

        // Shaft (long cylinder)
        var shaft = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        shaft.name = "Shaft";
        shaft.transform.SetParent(cueRoot.transform);
        shaft.transform.localPosition = new Vector3(0, 0, 0.35f);
        shaft.transform.localRotation = Quaternion.Euler(90, 0, 0);
        shaft.transform.localScale = new Vector3(0.012f, 0.35f, 0.012f);
        var shaftMat = CreateMaterial("Cue_Shaft", new Color(0.6f, 0.35f, 0.15f), 0.7f, 0.1f);
        shaft.GetComponent<Renderer>().sharedMaterial = shaftMat;

        // Butt (thicker back section)
        var butt = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        butt.name = "Butt";
        butt.transform.SetParent(cueRoot.transform);
        butt.transform.localPosition = new Vector3(0, 0, -0.35f);
        butt.transform.localRotation = Quaternion.Euler(90, 0, 0);
        butt.transform.localScale = new Vector3(0.016f, 0.35f, 0.016f);
        var buttMat = CreateMaterial("Cue_Butt", new Color(0.2f, 0.1f, 0.05f), 0.6f, 0.05f);
        butt.GetComponent<Renderer>().sharedMaterial = buttMat;

        // Tip (small blue cylinder)
        var tip = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        tip.name = "Tip";
        tip.transform.SetParent(cueRoot.transform);
        tip.transform.localPosition = new Vector3(0, 0, 0.71f);
        tip.transform.localRotation = Quaternion.Euler(90, 0, 0);
        tip.transform.localScale = new Vector3(0.011f, 0.005f, 0.011f);
        var tipMat = CreateMaterial("Cue_Tip", new Color(0.15f, 0.5f, 0.7f), 0.3f, 0.0f);
        tip.GetComponent<Renderer>().sharedMaterial = tipMat;

        // Ferrule (white ring)
        var ferrule = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        ferrule.name = "Ferrule";
        ferrule.transform.SetParent(cueRoot.transform);
        ferrule.transform.localPosition = new Vector3(0, 0, 0.7f);
        ferrule.transform.localRotation = Quaternion.Euler(90, 0, 0);
        ferrule.transform.localScale = new Vector3(0.0115f, 0.008f, 0.0115f);
        var ferruleMat = CreateMaterial("Cue_Ferrule", new Color(0.95f, 0.95f, 0.9f), 0.8f, 0.1f);
        ferrule.GetComponent<Renderer>().sharedMaterial = ferruleMat;

        // Remove colliders from visual parts (cue uses its own trigger)
        foreach (var col in cueRoot.GetComponentsInChildren<Collider>())
            Object.DestroyImmediate(col);

        // Add capsule collider to root
        var capsule = cueRoot.AddComponent<CapsuleCollider>();
        capsule.direction = 2; // Z axis
        capsule.center = new Vector3(0, 0, 0.15f);
        capsule.radius = 0.008f;
        capsule.height = 1.45f;
        capsule.isTrigger = true;

        string prefabPath = folder + "/CueStick_Placeholder.prefab";
        PrefabUtility.SaveAsPrefabAsset(cueRoot, prefabPath);
        Object.DestroyImmediate(cueRoot);
        Debug.Log("[CueStrike Models] Created cue stick prefab");
    }

    // ═══════════════════════════════════════════════════════════════
    //  SNOOKER TABLE
    // ═══════════════════════════════════════════════════════════════

    private static void GenerateSnookerTablePrefab()
    {
        string folder = PrefabFolder + "/Tables";
        Directory.CreateDirectory(Path.GetFullPath(folder));

        var table = new GameObject("SnookerTable_12ft_Placeholder");

        // Table dimensions: 12ft x 6ft playing area = 3.569m x 1.778m
        float playL = 3.569f, playW = 1.778f;
        float bedH = 0.87f; // standard height
        float railW = 0.08f;
        float legH = bedH - 0.1f;

        // Bed (playing surface)
        var bed = GameObject.CreatePrimitive(PrimitiveType.Cube);
        bed.name = "Bed";
        bed.transform.SetParent(table.transform);
        bed.transform.localPosition = new Vector3(0, bedH, 0);
        bed.transform.localScale = new Vector3(playW, 0.04f, playL);
        var feltMat = CreateMaterial("Table_Felt", new Color(0.0f, 0.35f, 0.15f), 0.3f, 0.0f);
        bed.GetComponent<Renderer>().sharedMaterial = feltMat;

        // Rails (4 sides)
        var railMat = CreateMaterial("Table_Rail", new Color(0.3f, 0.15f, 0.05f), 0.6f, 0.1f);
        CreateRail(table.transform, "Rail_North", new Vector3(0, bedH + 0.025f, playL / 2 + railW / 2),
            new Vector3(playW + railW * 2, 0.05f, railW), railMat);
        CreateRail(table.transform, "Rail_South", new Vector3(0, bedH + 0.025f, -playL / 2 - railW / 2),
            new Vector3(playW + railW * 2, 0.05f, railW), railMat);
        CreateRail(table.transform, "Rail_East", new Vector3(playW / 2 + railW / 2, bedH + 0.025f, 0),
            new Vector3(railW, 0.05f, playL), railMat);
        CreateRail(table.transform, "Rail_West", new Vector3(-playW / 2 - railW / 2, bedH + 0.025f, 0),
            new Vector3(railW, 0.05f, playL), railMat);

        // Legs (8 legs for 12ft table)
        var legMat = CreateMaterial("Table_Leg", new Color(0.25f, 0.12f, 0.05f), 0.5f, 0.05f);
        float legInset = 0.15f;
        float lx = playW / 2 - legInset;
        float lz = playL / 2 - legInset;
        float lzMid = playL / 4;
        CreateLeg(table.transform, "Leg_1", new Vector3(-lx, legH / 2, -lz), legH, legMat);
        CreateLeg(table.transform, "Leg_2", new Vector3(lx, legH / 2, -lz), legH, legMat);
        CreateLeg(table.transform, "Leg_3", new Vector3(-lx, legH / 2, lz), legH, legMat);
        CreateLeg(table.transform, "Leg_4", new Vector3(lx, legH / 2, lz), legH, legMat);
        CreateLeg(table.transform, "Leg_5", new Vector3(-lx, legH / 2, -lzMid), legH, legMat);
        CreateLeg(table.transform, "Leg_6", new Vector3(lx, legH / 2, -lzMid), legH, legMat);
        CreateLeg(table.transform, "Leg_7", new Vector3(-lx, legH / 2, lzMid), legH, legMat);
        CreateLeg(table.transform, "Leg_8", new Vector3(lx, legH / 2, lzMid), legH, legMat);

        // Pockets (6 triggers)
        float pocketR = 0.045f;
        CreatePocket(table.transform, "Pocket_TL", new Vector3(-playW / 2, bedH, playL / 2), pocketR);
        CreatePocket(table.transform, "Pocket_TR", new Vector3(playW / 2, bedH, playL / 2), pocketR);
        CreatePocket(table.transform, "Pocket_BL", new Vector3(-playW / 2, bedH, -playL / 2), pocketR);
        CreatePocket(table.transform, "Pocket_BR", new Vector3(playW / 2, bedH, -playL / 2), pocketR);
        CreatePocket(table.transform, "Pocket_ML", new Vector3(-playW / 2, bedH, 0), pocketR);
        CreatePocket(table.transform, "Pocket_MR", new Vector3(playW / 2, bedH, 0), pocketR);

        string prefabPath = folder + "/SnookerTable_12ft_Placeholder.prefab";
        PrefabUtility.SaveAsPrefabAsset(table, prefabPath);
        Object.DestroyImmediate(table);
        Debug.Log("[CueStrike Models] Created snooker table 12ft prefab");
    }

    // ═══════════════════════════════════════════════════════════════
    //  HELPERS
    // ═══════════════════════════════════════════════════════════════

    private static void CreateBallPrefab(string folder, string name, Color color, float radius, bool isStripe = false)
    {
        var ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        ball.name = name;
        ball.transform.localScale = Vector3.one * radius * 2f;

        // Material
        float smoothness = isStripe ? 0.7f : 0.85f;
        var mat = CreateMaterial($"Ball_{name}", color, smoothness, 0.02f);
        ball.GetComponent<Renderer>().sharedMaterial = mat;

        // Physics
        var rb = ball.AddComponent<Rigidbody>();
        rb.mass = 0.17f; // 170g standard
        rb.angularDamping = 0.5f;
        rb.linearDamping = 0.3f;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        // Ball identity
        ball.AddComponent<BallIdentity>();

        var col = ball.GetComponent<SphereCollider>();
        var physicsMat = new PhysicsMaterial($"BallPhysMat_{name}");
        physicsMat.bounciness = 0.7f;
        physicsMat.dynamicFriction = 0.2f;
        physicsMat.staticFriction = 0.3f;
        physicsMat.bounceCombine = PhysicsMaterialCombine.Average;
        physicsMat.frictionCombine = PhysicsMaterialCombine.Average;

        string physMatPath = $"{MatFolder}/BallPhysMat_{name}.physicsMaterial";
        // Save physics material as asset (can't do ScriptableObject.CreateInstance approach for PhysicsMaterial easily, just assign)
        col.material = physicsMat;

        string prefabPath = $"{folder}/{name}.prefab";
        PrefabUtility.SaveAsPrefabAsset(ball, prefabPath);
        Object.DestroyImmediate(ball);
    }

    private static Material CreateMaterial(string name, Color color, float smoothness, float metallic)
    {
        string matPath = $"{MatFolder}/{name}.mat";

        // Check if material already exists
        var existing = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (existing != null) return existing;

        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.color = color;
        mat.SetFloat("_Smoothness", smoothness);
        mat.SetFloat("_Metallic", metallic);

        AssetDatabase.CreateAsset(mat, matPath);
        return mat;
    }

    private static void CreateRail(Transform parent, string name, Vector3 pos, Vector3 scale, Material mat)
    {
        var rail = GameObject.CreatePrimitive(PrimitiveType.Cube);
        rail.name = name;
        rail.transform.SetParent(parent);
        rail.transform.localPosition = pos;
        rail.transform.localScale = scale;
        rail.GetComponent<Renderer>().sharedMaterial = mat;
    }

    private static void CreateLeg(Transform parent, string name, Vector3 pos, float height, Material mat)
    {
        var leg = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        leg.name = name;
        leg.transform.SetParent(parent);
        leg.transform.localPosition = pos;
        leg.transform.localScale = new Vector3(0.06f, height / 2, 0.06f);
        leg.GetComponent<Renderer>().sharedMaterial = mat;
    }

    private static void CreatePocket(Transform parent, string name, Vector3 pos, float radius)
    {
        var pocket = new GameObject(name);
        pocket.transform.SetParent(parent);
        pocket.transform.localPosition = pos;

        var col = pocket.AddComponent<SphereCollider>();
        col.radius = radius;
        col.isTrigger = true;

        // Add Pocket component if available
        var pocketComp = pocket.AddComponent<Pocket>();
        if (pocketComp != null)
        {
            // Pocket script handles ball detection
        }
    }
}
#endif
