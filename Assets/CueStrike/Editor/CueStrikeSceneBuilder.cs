#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CueStrike.Editor
{
    [InitializeOnLoad]
    internal static class CueStrikeSceneBuilder
    {
        private const string ScenePath = "Assets/CueStrike/Scenes/AAA DAY/AAA_RoomDAY.unity";
        private static readonly string[] RequiredFolders =
        {
            "Assets/CueStrike/Scenes/AAA DAY",
            "Assets/CueStrike/Branding"
        };

        static CueStrikeSceneBuilder()
        {
            EditorApplication.delayCall += InitializeProject;
        }

        private static void InitializeProject()
        {
            // Guard: Do not run during Play Mode
            if (EditorApplication.isPlaying)
                return;

            SetProjectSettings();
            EnsureFolders();
            CreateOrUpdateRoomScene();
            CreateOtherRoomScenes();
        }

        private static void SetProjectSettings()
        {
            if (PlayerSettings.productName != "CueStrike")
            {
                PlayerSettings.productName = "CueStrike";
                PlayerSettings.companyName = "CueStrike Studios";
                Debug.Log("[CueStrike] PlayerSettings.productName set to CueStrike.");
            }
        }

        private static void EnsureFolders()
        {
            foreach (var folder in RequiredFolders)
            {
                if (!AssetDatabase.IsValidFolder(folder))
                {
                    var parentFolder = Path.GetDirectoryName(folder).Replace("\\", "/");
                    var folderName = Path.GetFileName(folder);
                    AssetDatabase.CreateFolder(parentFolder, folderName);
                }
            }
        }

        private static void CreateOrUpdateRoomScene()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
            {
                CreateRoomScene();
                return;
            }

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (!SceneContainsRequiredObjects())
            {
                CreateRoomScene();
            }
        }

        private static bool SceneContainsRequiredObjects()
        {
            return GameObject.Find("AAA Table 12ft") != null && GameObject.Find("Tournament Room Camera") != null;
        }

        private static void CreateRoomScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "AAA_RoomDAY";

            CreateRoomMesh("Floor", PrimitiveType.Cube, new Vector3(0f, -0.5f, 0f), new Vector3(30f, 1f, 30f), new Color(0.08f, 0.08f, 0.10f));
            CreateRoomMesh("Wall_Back", PrimitiveType.Cube, new Vector3(0f, 2.5f, -14.5f), new Vector3(30f, 6f, 1f), new Color(0.04f, 0.04f, 0.05f));
            CreateRoomMesh("Wall_Front", PrimitiveType.Cube, new Vector3(0f, 2.5f, 14.5f), new Vector3(30f, 6f, 1f), new Color(0.04f, 0.04f, 0.05f));
            CreateRoomMesh("Wall_Left", PrimitiveType.Cube, new Vector3(-14.5f, 2.5f, 0f), new Vector3(1f, 6f, 30f), new Color(0.04f, 0.04f, 0.05f));
            CreateRoomMesh("Wall_Right", PrimitiveType.Cube, new Vector3(14.5f, 2.5f, 0f), new Vector3(1f, 6f, 30f), new Color(0.04f, 0.04f, 0.05f));
            CreateRoomMesh("Ceiling", PrimitiveType.Cube, new Vector3(0f, 5.5f, 0f), new Vector3(30f, 1f, 30f), new Color(0.05f, 0.05f, 0.06f));

            CreateRoomMesh("AAA Table 12ft", PrimitiveType.Cube, new Vector3(0f, 0.4f, 0f), new Vector3(4f, 0.5f, 8f), new Color(0.03f, 0.18f, 0.07f));
            CreateRoomMesh("Digital Scoreboard", PrimitiveType.Cube, new Vector3(0f, 3.2f, -13.4f), new Vector3(8f, 2f, 0.2f), new Color(0.03f, 0.03f, 0.04f));
            CreateRoomMesh("Judge Chair Left", PrimitiveType.Cube, new Vector3(-5f, 0.5f, 7f), new Vector3(1f, 1f, 1f), new Color(0.12f, 0.12f, 0.12f));
            CreateRoomMesh("Judge Chair Right", PrimitiveType.Cube, new Vector3(5f, 0.5f, 7f), new Vector3(1f, 1f, 1f), new Color(0.12f, 0.12f, 0.12f));
            CreateRoomMesh("Floor_Guide_Lines", PrimitiveType.Cube, new Vector3(0f, 0.01f, 0f), new Vector3(0.2f, 0.02f, 30f), Color.red, 0.3f);
            CreateRoomMesh("Floor_Guide_Lines_2", PrimitiveType.Cube, new Vector3(0f, 0.01f, 0f), new Vector3(30f, 0.02f, 0.2f), Color.blue, 0.3f);

            CreateLight("Spotlight_Red", LightType.Spot, new Vector3(-4f, 6f, -1f), Quaternion.Euler(70f, 20f, 0f), Color.red, 30f, 12f, 1f);
            CreateLight("Spotlight_Blue", LightType.Spot, new Vector3(4f, 6f, -1f), Quaternion.Euler(70f, -20f, 0f), Color.blue, 30f, 12f, 1f);
            CreateLight("Ambient Fill", LightType.Point, new Vector3(0f, 4f, 0f), Quaternion.identity, new Color(0.15f, 0.15f, 0.20f), 60f, 20f, 0.4f);

            var cameraGO = new GameObject("Tournament Room Camera", typeof(Camera), typeof(AudioListener));
            cameraGO.transform.position = new Vector3(0f, 4.5f, -14f);
            cameraGO.transform.rotation = Quaternion.Euler(10f, 0f, 0f);
            var camera = cameraGO.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            camera.fieldOfView = 45f;

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.Refresh();
            Debug.Log("[CueStrike] Created and saved AAA_RoomDAY scene at " + ScenePath);
        }

        private static readonly (string path, string name, string type)[] OtherScenes = new (string, string, string)[]
        {
            ("Assets/CueStrike/Scenes/WarpFantasy/WarpFantasy_Room.unity","WarpFantasy_Room","WarpFantasy"),
            ("Assets/CueStrike/Scenes/Industrial/Industrial_Room.unity","Industrial_Room","Industrial"),
            ("Assets/CueStrike/Scenes/Luxury/Luxury_Room.unity","Luxury_Room","Luxury"),
            ("Assets/CueStrike/Scenes/SpaceNebula/SpaceNebula_Room.unity","SpaceNebula_Room","SpaceNebula"),
            ("Assets/CueStrike/Scenes/ZenDojo/ZenDojo_Room.unity","ZenDojo_Room","ZenDojo"),
            ("Assets/CueStrike/Scenes/Cyberpunk/Cyberpunk_Room.unity","Cyberpunk_Room","Cyberpunk"),
            ("Assets/CueStrike/Scenes/GrandArena/GrandArena_Room.unity","GrandArena_Room","GrandArena")
        };

        [MenuItem("CueStrike/Generate/Rebuild AAA Scenes")]
        public static void RebuildAAAScenes()
        {
            // Guard: Cannot run in Play Mode
            if (EditorApplication.isPlaying)
            {
                Debug.LogError("[CueStrike] Cannot rebuild scenes while in Play Mode. Please exit Play Mode first.");
                return;
            }

            // Delete existing scenes to force rebuild
            foreach (var entry in OtherScenes)
            {
                AssetDatabase.DeleteAsset(entry.path);
            }
            AssetDatabase.DeleteAsset(ScenePath);
            AssetDatabase.Refresh();

            // Run generation
            InitializeProject();
            Debug.Log("CueStrike AAA: All 7 room scenes successfully rebuilt and decorated with high-end props!");
        }

        private static void CreateOtherRoomScenes()
        {
            foreach (var entry in OtherScenes)
            {
                var path = entry.path;
                var name = entry.name;
                var type = entry.type;
                var dir = Path.GetDirectoryName(path).Replace("\\", "/");
                if (!AssetDatabase.IsValidFolder(dir))
                {
                    var parent = Path.GetDirectoryName(dir).Replace("\\", "/");
                    var folderName = Path.GetFileName(dir);
                    AssetDatabase.CreateFolder(parent, folderName);
                }

                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null)
                {
                    var s = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                    s.name = name;
                    CreateRoomByType(type, s);
                    EditorSceneManager.SaveScene(s, path);
                }
            }
            AssetDatabase.Refresh();
        }

        private static void CreateRoomByType(string type, Scene scene)
        {
            // common base: floor, walls, ceiling
            CreateRoomMesh("Floor", PrimitiveType.Cube, new Vector3(0f, -0.5f, 0f), new Vector3(30f, 1f, 30f), new Color(0.08f, 0.08f, 0.10f));
            CreateRoomMesh("Wall_Back", PrimitiveType.Cube, new Vector3(0f, 2.5f, -14.5f), new Vector3(30f, 6f, 1f), new Color(0.04f, 0.04f, 0.05f));
            CreateRoomMesh("Wall_Front", PrimitiveType.Cube, new Vector3(0f, 2.5f, 14.5f), new Vector3(30f, 6f, 1f), new Color(0.04f, 0.04f, 0.05f));
            CreateRoomMesh("Wall_Left", PrimitiveType.Cube, new Vector3(-14.5f, 2.5f, 0f), new Vector3(1f, 6f, 30f), new Color(0.04f, 0.04f, 0.05f));
            CreateRoomMesh("Wall_Right", PrimitiveType.Cube, new Vector3(14.5f, 2.5f, 0f), new Vector3(1f, 6f, 30f), new Color(0.04f, 0.04f, 0.05f));
            CreateRoomMesh("Ceiling", PrimitiveType.Cube, new Vector3(0f, 5.5f, 0f), new Vector3(30f, 1f, 30f), new Color(0.05f, 0.05f, 0.06f));

            // Load wood material
            Material woodMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/CueStrike/Materials/Wood_Table.mat");

            // Instantiate table prefab instead of raw cube!
            string tablePrefabPath = "Assets/CueStrike/Prefabs/Tables/CueStrikeTable_Snooker12ft.prefab";
            if (type.ToLower().Contains("8ball")) tablePrefabPath = "Assets/CueStrike/Prefabs/Tables/CueStrikeTable_Pool_8Ball.prefab";
            else if (type.ToLower().Contains("9ball")) tablePrefabPath = "Assets/CueStrike/Prefabs/Tables/CueStrikeTable_Pool_9Ball.prefab";
            
            GameObject table = null;
            var tablePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(tablePrefabPath);
            if (tablePrefab != null)
            {
                table = (GameObject)PrefabUtility.InstantiatePrefab(tablePrefab);
                table.transform.position = new Vector3(0f, 0f, 0f);
            }
            else
            {
                table = CreateRoomMesh("AAA Table 12ft", PrimitiveType.Cube, new Vector3(0f, 0.4f, 0f), new Vector3(1.778f, 0.5f, 3.569f), new Color(0.03f, 0.18f, 0.07f));
            }

            // common camera
            var cameraGO = new GameObject(name: type + " Camera", typeof(Camera));
            cameraGO.transform.position = new Vector3(0f, 3.5f, -6f);
            cameraGO.transform.rotation = Quaternion.Euler(15f, 0f, 0f);

            // Setup variables for loading props
            string propBase = "Assets/CueStrike/Props/";
            var barCounterPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(propBase + "Bar/Counter/BarCounter.prefab");
            var barStoolPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(propBase + "Bar/Stools/BarStool.prefab");
            var lockerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(propBase + "Locker/PersonalLocker.prefab");
            var chairPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(propBase + "SpaceWindow/LoungeChair/LoungeChair.prefab");
            var telescopePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(propBase + "SpaceWindow/Telescope/Telescope.prefab");
            var portalFramePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(propBase + "Portal/PortalFrame.prefab");
            var portalVfxPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(propBase + "Portal/PortalVFX.prefab");

            switch (type)
            {
                case "WarpFantasy":
                    if (table != null) table.transform.position = new Vector3(0f, 0.8f, 0f);
                    CreateLight("Warp Light Blue", LightType.Point, new Vector3(-2f, 3f, -2f), Quaternion.identity, new Color(0.2f, 0.5f, 1f), 0f, 10f, 1.2f);
                    CreateLight("Warp Light Purple", LightType.Point, new Vector3(2f, 3f, -2f), Quaternion.identity, new Color(0.6f, 0.2f, 1f), 0f, 10f, 1.0f);
                    
                    // Spawn portals in background
                    if (portalFramePrefab != null)
                    {
                        var pf = (GameObject)PrefabUtility.InstantiatePrefab(portalFramePrefab);
                        pf.transform.position = new Vector3(0f, 1.5f, 6f);
                        pf.transform.localScale = new Vector3(1.2f, 1.2f, 1.2f);
                        if (portalVfxPrefab != null)
                        {
                            var pv = (GameObject)PrefabUtility.InstantiatePrefab(portalVfxPrefab);
                            pv.transform.position = new Vector3(0f, 1.5f, 5.95f);
                            pv.transform.localScale = new Vector3(1.1f, 1.1f, 1.1f);
                        }
                    }
                    break;

                case "Industrial":
                    CreateLight("Industrial Warm", LightType.Point, new Vector3(0f, 4f, 0f), Quaternion.identity, new Color(1f, 0.6f, 0.2f), 0f, 25f, 1.8f);
                    CreateRoomMesh("MetalFloor", PrimitiveType.Cube, new Vector3(0f, -0.45f, 0f), new Vector3(30f, 0.1f, 30f), new Color(0.08f, 0.06f, 0.05f));
                    
                    // Spawn industrial lockers and tables
                    if (lockerPrefab != null)
                    {
                        var l1 = (GameObject)PrefabUtility.InstantiatePrefab(lockerPrefab);
                        l1.transform.position = new Vector3(-5f, 0f, 5f);
                        l1.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
                    }
                    break;

                case "Luxury":
                    CreateRoomMesh("RedCarpet", PrimitiveType.Cube, new Vector3(0f, -0.49f, 0f), new Vector3(12f, 0.02f, 18f), new Color(0.45f, 0.03f, 0.03f));
                    CreateLight("SoftWarm", LightType.Point, new Vector3(0f, 4f, -4f), Quaternion.identity, new Color(1f, 0.9f, 0.8f), 0f, 20f, 0.7f);
                    
                    // Spawn high-end bar setup
                    if (barCounterPrefab != null)
                    {
                        var counter = (GameObject)PrefabUtility.InstantiatePrefab(barCounterPrefab);
                        counter.transform.position = new Vector3(0f, 0f, 6f);
                        counter.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
                        
                        if (barStoolPrefab != null)
                        {
                            var s1 = (GameObject)PrefabUtility.InstantiatePrefab(barStoolPrefab);
                            s1.transform.position = new Vector3(-1f, 0f, 4.8f);
                            var s2 = (GameObject)PrefabUtility.InstantiatePrefab(barStoolPrefab);
                            s2.transform.position = new Vector3(1f, 0f, 4.8f);
                        }
                    }

                    // Spawn luxury lounge chairs
                    if (chairPrefab != null)
                    {
                        var c1 = (GameObject)PrefabUtility.InstantiatePrefab(chairPrefab);
                        c1.transform.position = new Vector3(-3.5f, 0f, -3f);
                        c1.transform.rotation = Quaternion.Euler(0f, 45f, 0f);
                        var c2 = (GameObject)PrefabUtility.InstantiatePrefab(chairPrefab);
                        c2.transform.position = new Vector3(3.5f, 0f, -3f);
                        c2.transform.rotation = Quaternion.Euler(0f, -45f, 0f);
                    }
                    break;

                case "SpaceNebula":
                    if (table != null) table.transform.position = new Vector3(0f, 0.8f, 0f);
                    CreateLight("Nebula Blue", LightType.Point, new Vector3(0f, 3f, -2f), Quaternion.identity, new Color(0.3f, 0.5f, 1f), 0f, 30f, 1.5f);
                    
                    // Spawn observatory telescope and chairs
                    if (telescopePrefab != null)
                    {
                        var tele = (GameObject)PrefabUtility.InstantiatePrefab(telescopePrefab);
                        tele.transform.position = new Vector3(4f, 0f, 4f);
                        tele.transform.rotation = Quaternion.Euler(0f, -45f, 0f);
                    }
                    if (chairPrefab != null)
                    {
                        var c1 = (GameObject)PrefabUtility.InstantiatePrefab(chairPrefab);
                        c1.transform.position = new Vector3(-4f, 0f, 4f);
                        c1.transform.rotation = Quaternion.Euler(0f, 45f, 0f);
                    }
                    break;

                case "ZenDojo":
                    CreateRoomMesh("TatamiFloor", PrimitiveType.Cube, new Vector3(0f, -0.49f, 0f), new Vector3(12f, 0.02f, 12f), new Color(0.42f, 0.34f, 0.25f));
                    CreateLight("ShojiLamp", LightType.Point, new Vector3(0f, 3f, -4f), Quaternion.identity, new Color(1f, 0.9f, 0.8f), 0f, 12f, 0.6f);
                    break;

                case "Cyberpunk":
                    CreateLight("Neon Pink", LightType.Point, new Vector3(-3f, 3f, -2f), Quaternion.identity, new Color(1f, 0.2f, 0.8f), 0f, 20f, 1.8f);
                    CreateLight("Neon Blue", LightType.Point, new Vector3(3f, 3f, -2f), Quaternion.identity, new Color(0.2f, 0.8f, 1f), 0f, 20f, 1.6f);
                    CreateRoomMesh("WetFloor", PrimitiveType.Cube, new Vector3(0f, -0.48f, 0f), new Vector3(30f, 0.02f, 30f), new Color(0.02f, 0.02f, 0.03f));
                    
                    // Spawn bar setup with sci-fi look
                    if (barCounterPrefab != null)
                    {
                        var counter = (GameObject)PrefabUtility.InstantiatePrefab(barCounterPrefab);
                        counter.transform.position = new Vector3(0f, 0f, 5.5f);
                        counter.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
                    }
                    break;

                case "GrandArena":
                    // 1. Build massive stadium bleachers (oval arrangement using cubes)
                    GameObject bleachersRoot = new GameObject("StadiumBleachers");
                    for (int step = 0; step < 5; step++)
                    {
                        float ringRadius = 8f + step * 2f;
                        float height = step * 0.8f;
                        int seatsCount = 16 + step * 4;
                        for (int s = 0; s < seatsCount; s++)
                        {
                            float angle = s * Mathf.PI * 2f / seatsCount;
                            Vector3 pos = new Vector3(Mathf.Cos(angle) * ringRadius, height - 0.4f, Mathf.Sin(angle) * ringRadius * 1.5f);
                            var seat = CreateRoomMesh($"Seat_{step}_{s}", PrimitiveType.Cube, pos, new Vector3(1.2f, 0.6f, 1.2f), new Color(0.12f, 0.15f, 0.2f), 0.1f, bleachersRoot.transform);
                            // Face center
                            seat.transform.LookAt(new Vector3(0f, pos.y, 0f));
                        }
                    }

                    // 2. Setup Render Texture for Live Broadcast
                    RenderTexture rt = AssetDatabase.LoadAssetAtPath<RenderTexture>("Assets/CueStrike/Textures/BroadcastFeed.renderTexture");
                    if (rt == null)
                    {
                        rt = new RenderTexture(1024, 1024, 16, RenderTextureFormat.ARGB32);
                        // Ensure textures folder exists
                        if (!AssetDatabase.IsValidFolder("Assets/CueStrike/Textures"))
                        {
                            AssetDatabase.CreateFolder("Assets/CueStrike", "Textures");
                        }
                        AssetDatabase.CreateAsset(rt, "Assets/CueStrike/Textures/BroadcastFeed.renderTexture");
                    }

                    // 3. Create Live Broadcast Camera looking down at table
                    GameObject liveCamGO = new GameObject("BroadcastCamera", typeof(Camera));
                    liveCamGO.transform.position = new Vector3(0f, 6f, 0f);
                    liveCamGO.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                    var liveCam = liveCamGO.GetComponent<Camera>();
                    liveCam.targetTexture = rt;
                    liveCam.fieldOfView = 35f;

                    // 4. Create 4 Giant Suspended Projector Screens
                    GameObject screensRoot = new GameObject("ProjectorScreens");
                    Material screenMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                    screenMat.mainTexture = rt;
                    screenMat.SetFloat("_Smoothness", 0.0f);
                    screenMat.EnableKeyword("_EMISSION");
                    screenMat.SetColor("_EmissionColor", Color.white * 0.5f);

                    Vector3[] screenPositions = new Vector3[]
                    {
                        new Vector3(0f, 8f, 5f),
                        new Vector3(0f, 8f, -5f),
                        new Vector3(5f, 8f, 0f),
                        new Vector3(-5f, 8f, 0f)
                    };
                    Quaternion[] screenRotations = new Quaternion[]
                    {
                        Quaternion.Euler(-15f, 180f, 0f),
                        Quaternion.Euler(-15f, 0f, 0f),
                        Quaternion.Euler(-15f, -90f, 0f),
                        Quaternion.Euler(-15f, 90f, 0f)
                    };

                    for (int sc = 0; sc < 4; sc++)
                    {
                        var screenGO = GameObject.CreatePrimitive(PrimitiveType.Quad);
                        screenGO.name = $"ProjectorScreen_{sc}";
                        screenGO.transform.position = screenPositions[sc];
                        screenGO.transform.rotation = screenRotations[sc];
                        screenGO.transform.localScale = new Vector3(6f, 3.5f, 1f);
                        screenGO.transform.SetParent(screensRoot.transform, false);
                        var rend = screenGO.GetComponent<Renderer>();
                        if (rend != null) rend.sharedMaterial = screenMat;
                    }

                    // 5. Ambient murmur and lights
                    CreateLight("ArenaCenterSpot", LightType.Spot, new Vector3(0f, 10f, 0f), Quaternion.Euler(90f, 0f, 0f), new Color(1f, 0.95f, 0.9f), 45f, 25f, 3.0f);
                    CreateLight("ArenaAmbientWarm", LightType.Point, new Vector3(0f, 8f, 0f), Quaternion.identity, new Color(0.4f, 0.3f, 0.2f), 0f, 40f, 0.8f);

                    // 6. Attach Crowd react
                    GameObject crowdGO = new GameObject("CrowdSystem");
                    crowdGO.AddComponent<CueStrike.Audio.CueStrikeChampionshipCrowd>();
                    break;
            }

            // ensure scene has a camera and save
            EditorSceneManager.MarkSceneDirty(scene);
        }

        private static GameObject CreateRoomMesh(string name, PrimitiveType type, Vector3 position, Vector3 scale, Color color, float glow = 0.2f, Transform parent = null)
        {
            var mesh = GameObject.CreatePrimitive(type);
            mesh.name = name;
            mesh.transform.position = position;
            mesh.transform.localScale = scale;
            if (parent != null)
            {
                mesh.transform.SetParent(parent, true);
            }

            var renderer = mesh.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                mat.color = color;
                mat.SetFloat("_Smoothness", 0.35f);
                mat.SetFloat("_Metallic", glow);
                renderer.sharedMaterial = mat;
            }

            return mesh;
        }

        private static Light CreateLight(string name, LightType type, Vector3 position, Quaternion rotation, Color color, float angle, float range, float intensity)
        {
            var lightGO = new GameObject(name, typeof(Light));
            lightGO.transform.position = position;
            lightGO.transform.rotation = rotation;
            var light = lightGO.GetComponent<Light>();
            light.type = type;
            light.color = color;
            light.range = range;
            light.intensity = intensity;
            if (type == LightType.Spot)
            {
                light.spotAngle = angle;
            }
            light.shadows = LightShadows.Soft;
            return light;
        }
    }
}
#endif