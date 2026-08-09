#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CueStrike.Editor
{
    /// <summary>
    /// Final AAA Setup - Arranges all props in ZenDojo and Grand Hall rooms.
    /// This is the ONE BUTTON for final AAA polish in the two showcase rooms.
    ///
    /// Menu: Tools → CueStrike → Apply → Final AAA Setup
    /// </summary>
    public static class CueStrikeFinalAAASetup
    {
        private const string PropsPrefabDir = "Assets/CueStrike/Prefabs/AAA_Props";
        private const string URP_LIT = "Universal Render Pipeline/Lit";

        [MenuItem("Tools/CueStrike/Apply/Final AAA Setup")]
        public static void FinalAAASetup()
        {
            if (EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog("Final AAA Setup", "Cannot run while in Play Mode.", "OK");
                return;
            }

            // Remember original scene
            string originalScenePath = EditorSceneManager.GetActiveScene().path;

            Debug.Log("[CueStrike Final AAA] ═══ Final AAA Setup Started ═══");
            int totalSteps = 0;

            // 1. Setup ZenDojo Room
            totalSteps += SetupZenDojo();

            // 2. Setup Grand Hall (Title_NoksGrandHall)
            totalSteps += SetupGrandHall();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // Restore original scene
            if (!string.IsNullOrEmpty(originalScenePath))
            {
                EditorSceneManager.OpenScene(originalScenePath, OpenSceneMode.Single);
            }

            Debug.Log($"[CueStrike Final AAA] ═══ Final AAA Setup Complete: {totalSteps} steps executed ═══");
            EditorUtility.DisplayDialog("Final AAA Setup",
                "✅ Final AAA Setup completed!\n\n" +
                "• ZenDojo: ZenLantern + BarBottleSet + HoloScreen placed\n" +
                "• Grand Hall: Uncle Nok + Bo + Crowd + LuxuryChandelier placed\n\n" +
                "Open the scenes to see the AAA polish!",
                "OK");
        }

        // =====================================================================
        // ZEN DOJO ROOM SETUP
        // =====================================================================
        private static int SetupZenDojo()
        {
            string scenePath = FindScenePath("ZenDojo_Room");
            if (string.IsNullOrEmpty(scenePath))
            {
                Debug.LogWarning("[CueStrike Final AAA] ZenDojo_Room scene not found!");
                return 0;
            }

            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            GameObject root = new GameObject("AAA_FinalDecor");
            root.transform.position = Vector3.zero;

            bool placed = false;

            // 1. Main prop: ZenLantern at center above table
            placed |= PlaceDecorProp(root.transform, "ZenLantern", new Vector3(0f, 2.5f, 0f));

            // 2. Secondary props for atmosphere
            placed |= PlaceDecorProp(root.transform, "BarBottleSet", new Vector3(-2.5f, 0.8f, 2.5f));
            placed |= PlaceDecorProp(root.transform, "HoloScreen", new Vector3(2.5f, 1.5f, -2.5f));

            // 3. Add subtle floor candles around the table (procedural)
            placed |= PlaceZenCandles(root.transform);

            if (placed)
            {
                EditorSceneManager.SaveScene(scene);
                Debug.Log("[CueStrike Final AAA] ZenDojo_Room decorated with AAA props.");
                return 1;
            }
            else
            {
                GameObject.DestroyImmediate(root);
                return 0;
            }
        }

        private static bool PlaceZenCandles(Transform parent)
        {
            // Small candles around the pool table for zen atmosphere
            int candleCount = 8;
            float radius = 3f;
            bool placed = false;

            for (int i = 0; i < candleCount; i++)
            {
                float angle = (float)i / candleCount * Mathf.PI * 2f;
                Vector3 pos = new Vector3(
                    Mathf.Cos(angle) * radius,
                    0.3f,
                    Mathf.Sin(angle) * radius
                );

                // Create simple candle from primitives (no FBX needed)
                GameObject candle = CreateCandlePrimitive($"ZenCandle_{i:00}", parent, pos);
                if (candle != null) placed = true;
            }

            return placed;
        }

        private static GameObject CreateCandlePrimitive(string name, Transform parent, Vector3 worldPos)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent);
            go.transform.position = worldPos;

            // Candle body (thin cylinder)
            var body = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            body.name = "Body";
            body.transform.SetParent(go.transform);
            body.transform.localScale = new Vector3(0.08f, 0.4f, 0.08f);
            body.transform.localPosition = new Vector3(0f, 0.2f, 0f);
            SetMaterial(body, new Color(0.95f, 0.9f, 0.8f)); // warm wax color

            // Flame (small sphere with emissive material)
            var flame = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            flame.name = "Flame";
            flame.transform.SetParent(go.transform);
            flame.transform.localScale = new Vector3(0.12f, 0.2f, 0.12f);
            flame.transform.localPosition = new Vector3(0f, 0.5f, 0f);
            SetEmissiveMaterial(flame, new Color(1f, 0.6f, 0.1f), 3f);

            // Remove colliders from candle parts
            var colliders = go.GetComponentsInChildren<Collider>();
            foreach (var c in colliders) GameObject.DestroyImmediate(c);

            return go;
        }

        // =====================================================================
        // GRAND HALL SETUP (Title_NoksGrandHall)
        // =====================================================================
        private static int SetupGrandHall()
        {
            string scenePath = FindScenePath("Title_NoksGrandHall");
            if (string.IsNullOrEmpty(scenePath))
            {
                Debug.LogWarning("[CueStrike Final AAA] Title_NoksGrandHall scene not found!");
                return 0;
            }

            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            GameObject root = new GameObject("AAA_FinalDecor");
            root.transform.position = Vector3.zero;

            bool placed = false;

            // 1. Uncle Nok (referee announcer)
            placed |= PlaceUncleNok(root.transform);

            // 2. Bo (sidekick dog)
            placed |= PlaceBo(root.transform);

            // 3. Crowd ring
            placed |= PlaceCrowd(root.transform, 40, 9f);

            // 4. Luxury Chandelier
            placed |= PlaceDecorProp(root.transform, "LuxuryChandelier", new Vector3(0f, 4.5f, 0f));

            if (placed)
            {
                EditorSceneManager.SaveScene(scene);
                Debug.Log("[CueStrike Final AAA] Grand Hall (Title_NoksGrandHall) decorated with AAA props.");
                return 1;
            }
            else
            {
                GameObject.DestroyImmediate(root);
                return 0;
            }
        }

        // ---------- Uncle Nok ----------
        private static bool PlaceUncleNok(Transform parent)
        {
            var nok = new GameObject("Uncle_Nok_Root");
            nok.transform.SetParent(parent);
            nok.transform.localPosition = new Vector3(0f, 0f, 4f);

            // Body (black referee robe)
            CreatePrimitiveCube("Body", nok.transform, new Vector3(0.7f, 1.4f, 0.4f),
                new Vector3(0f, 0.7f, 0f), new Color(0.05f, 0.05f, 0.05f));

            // Head (skin tone)
            CreatePrimitiveSphere("Head", nok.transform, 0.35f,
                new Vector3(0f, 1.55f, 0f), new Color(0.85f, 0.60f, 0.45f));

            // Moustache (dark bar under nose)
            CreatePrimitiveCube("Moustache", nok.transform, new Vector3(0.5f, 0.05f, 0.1f),
                new Vector3(0f, 1.40f, 0.32f), new Color(0.25f, 0.15f, 0.05f));

            // Whistle (tiny yellow sphere on chest)
            CreatePrimitiveSphere("Whistle", nok.transform, 0.06f,
                new Vector3(0.3f, 1.35f, 0.2f), new Color(1f, 0.85f, 0f));

            // Arms (simple capsules)
            CreatePrimitiveCapsule("LeftArm", nok.transform, new Vector3(-0.5f, 1.1f, 0f), new Color(0.05f, 0.05f, 0.05f));
            CreatePrimitiveCapsule("RightArm", nok.transform, new Vector3(0.5f, 1.1f, 0f), new Color(0.05f, 0.05f, 0.05f));

            return true;
        }

        // ---------- Bo (small sidekick) ----------
        private static bool PlaceBo(Transform parent)
        {
            var bo = new GameObject("Bo_Root");
            bo.transform.SetParent(parent);
            bo.transform.localPosition = new Vector3(1.6f, 0f, 5.2f);

            // Body (small white fluffy)
            CreatePrimitiveSphere("Body", bo.transform, 0.30f,
                new Vector3(0f, 0.30f, 0f), Color.white);

            // Ears (two small)
            CreatePrimitiveSphere("EarL", bo.transform, 0.08f,
                new Vector3(-0.15f, 0.55f, 0f), Color.white);
            CreatePrimitiveSphere("EarR", bo.transform, 0.08f,
                new Vector3(0.15f, 0.55f, 0f), Color.white);

            // Nose (black dot)
            CreatePrimitiveSphere("Nose", bo.transform, 0.04f,
                new Vector3(0f, 0.35f, 0.25f), new Color(0.1f, 0.1f, 0.1f));

            return true;
        }

        // ---------- Crowd ----------
        private static bool PlaceCrowd(Transform parent, int count, float radius)
        {
            var crowdRoot = new GameObject("Crowd_Ring");
            crowdRoot.transform.SetParent(parent);

            for (int i = 0; i < count; i++)
            {
                float angle = (float)i / count * Mathf.PI * 2f;
                Vector3 pos = new Vector3(
                    Mathf.Cos(angle) * radius,
                    0.5f,
                    Mathf.Sin(angle) * radius
                );

                // Random shirt colour to look alive
                Color shirt = Random.ColorHSV(0f, 1f, 0.6f, 0.9f, 0.3f, 0.6f);

                GameObject spectator = new GameObject($"Spectator_{i:00}");
                spectator.transform.SetParent(crowdRoot.transform);
                spectator.transform.position = pos;
                // Face toward the centre
                spectator.transform.rotation = Quaternion.LookRotation(-pos.normalized);

                // Body (capsule) + head (sphere)
                CreatePrimitiveCapsule("Body", spectator.transform, new Vector3(0f, 1f, 0f), shirt);
                CreatePrimitiveSphere("Head", spectator.transform, 0.25f,
                    new Vector3(0f, 1.5f, 0f), new Color(0.8f, 0.6f, 0.5f));
            }

            return true;
        }

        // =====================================================================
        // HELPERS
        // =====================================================================
        private static bool PlaceDecorProp(Transform parent, string prefabName, Vector3 position)
        {
            string prefabPath = $"{PropsPrefabDir}/{prefabName}.prefab";
            if (!System.IO.File.Exists(prefabPath))
            {
                Debug.LogWarning($"[CueStrike Final AAA] Prefab not found: {prefabPath}");
                return false;
            }

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null) return false;

            // Check if already placed (avoid duplicates on re-run)
            string containerName = "AAA_FinalDecor";
            var existingRoot = GameObject.Find(containerName);
            Transform existing = existingRoot != null ? existingRoot.transform.Find(prefabName) : null;
            if (existing != null) return true; // Already placed

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.name = prefabName;
            instance.transform.SetParent(parent);
            instance.transform.position = position;

            // Ensure materials are URP/Lit
            EnsureURPMaterials(instance);

            return true;
        }

        private static void EnsureURPMaterials(GameObject go)
        {
            var renderers = go.GetComponentsInChildren<Renderer>(true);
            Shader urpLit = Shader.Find(URP_LIT);
            if (urpLit == null) return;

            foreach (var rend in renderers)
            {
                if (rend.sharedMaterial != null && rend.sharedMaterial.shader != null)
                {
                    if (!rend.sharedMaterial.shader.name.Contains("Universal Render Pipeline"))
                    {
                        // Create a new URP material with same color/texture
                        Material newMat = new Material(urpLit);
                        newMat.color = rend.sharedMaterial.color;
                        if (rend.sharedMaterial.mainTexture != null)
                        {
                            newMat.SetTexture("_BaseMap", rend.sharedMaterial.mainTexture);
                        }
                        rend.sharedMaterial = newMat;
                    }
                }
            }
        }

        private static GameObject CreatePrimitiveCube(string name, Transform parent,
            Vector3 scale, Vector3 localPos, Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Configure(go, name, parent, scale, localPos, color);
            return go;
        }

        private static GameObject CreatePrimitiveSphere(string name, Transform parent,
            float radius, Vector3 localPos, Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Configure(go, name, parent, Vector3.one * (radius * 2f), localPos, color);
            return go;
        }

        private static GameObject CreatePrimitiveCapsule(string name, Transform parent,
            Vector3 localPos, Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            Configure(go, name, parent, Vector3.one, localPos, color);
            return go;
        }

        private static void Configure(GameObject go, string name, Transform parent,
            Vector3 scale, Vector3 localPos, Color color)
        {
            go.name = name;
            go.transform.SetParent(parent);
            go.transform.localScale = scale;
            go.transform.localPosition = localPos;
            SetMaterial(go, color);
        }

        private static void SetMaterial(GameObject go, Color color)
        {
            var renderer = go.GetComponent<Renderer>();
            if (renderer == null) return;

            Shader litShader = Shader.Find(URP_LIT);
            if (litShader == null)
            {
                litShader = Shader.Find("Universal Render Pipeline/Unlit");
            }
            if (litShader == null)
            {
                litShader = Shader.Find("Universal Render Pipeline/Unlit");
            }
            Material mat = new Material(litShader);

            if (mat != null)
            {
                mat.color = color;
                renderer.sharedMaterial = mat;
            }
        }

        private static void SetEmissiveMaterial(GameObject go, Color emissionColor, float intensity)
        {
            var renderer = go.GetComponent<Renderer>();
            if (renderer == null) return;

            Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit");
            if (unlitShader == null) unlitShader = Shader.Find(URP_LIT);

            Material mat = new Material(unlitShader);
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", emissionColor * intensity);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", Color.black);
            renderer.sharedMaterial = mat;
        }

        private static string FindScenePath(string sceneName)
        {
            string[] sceneGuids = AssetDatabase.FindAssets($"t:Scene {sceneName}");
            foreach (var g in sceneGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(g);
                if (System.IO.Path.GetFileNameWithoutExtension(path).Equals(sceneName, System.StringComparison.OrdinalIgnoreCase))
                    return path;
            }
            return null;
        }
    }
}
#endif