#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CueStrike.Editor
{
    /// <summary>
    /// One-click scene setup for the 'Grand Hall (AAA)' showcase.
    ///
    /// Creates/positions:
    ///   • Uncle Nok  – the charismatic announcer (robe, moustache, whistle)
    ///   • Bo         – the trusty sidekick dog (low-poly from primitives)
    ///   • Crowd      – a cheering audience ring (procedurally instanced spectators)
    ///
    /// Menu: CueStrike → Setup → Grand Hall (AAA)
    /// </summary>
    public static class CueStrikeGrandHallSetup
    {
        private const string URP_LIT = "Universal Render Pipeline/Lit";

        [MenuItem("CueStrike/Setup/Grand Hall (AAA)")]
        public static void SetupGrandHall()
        {
            // Don't run while in play mode
            if (EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog("Grand Hall Setup", "Cannot run while in Play Mode.", "OK");
                return;
            }

            // Ensure a valid scene is open (create one if none)
            var activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (!activeScene.IsValid() || string.IsNullOrEmpty(activeScene.path))
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }

            // Root container for organizational clarity
            GameObject root = new GameObject("GrandHall_AAA");
            root.transform.position = Vector3.zero;

            // --- 1. Floor / Stage ---
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Stage_Floor";
            floor.transform.SetParent(root.transform);
            floor.transform.localPosition = new Vector3(0f, -0.5f, 0f);
            floor.transform.localScale = new Vector3(40f, 1f, 30f);
            SetMaterial(floor, new Color(0.35f, 0.18f, 0.08f)); // wood brown

            // --- 2. Uncle Nok (Referee) ---
            GameObject uncleNok = BuildUncleNok();
            uncleNok.transform.SetParent(root.transform);
            uncleNok.transform.localPosition = new Vector3(0f, 0f, 4f);

            // --- 3. Bo (Sidekick) ---
            GameObject bo = BuildBo();
            bo.transform.SetParent(root.transform);
            bo.transform.localPosition = new Vector3(1.6f, 0f, 5.2f);

            // --- 4. Crowd (ring of spectators) ---
            BuildCrowd(40, root.transform);

            // Mark the scene as dirty so a save prompt appears when closing
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

            Debug.Log("[CueStrike] Grand Hall (AAA) setup complete! Uncle Nok, Bo and the crowd are ready.");
            EditorUtility.DisplayDialog("Grand Hall (AAA)",
                "Setup complete!\n\n• Uncle Nok (referee)\n• Bo (sidekick)\n• Crowd of 40 spectators\n\n" +
                "Open your XR rig to test.",
                "OK");
        }

        // ---------- Uncle Nok ----------
        private static GameObject BuildUncleNok()
        {
            var nok = new GameObject("Uncle_Nok_Root");

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

            return nok;
        }

        // ---------- Bo (small sidekick) ----------
        private static GameObject BuildBo()
        {
            var bo = new GameObject("Bo_Root");

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

            return bo;
        }

        // ---------- Crowd ----------
        private static void BuildCrowd(int count, Transform parent)
        {
            float radius = 9f;
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
                spectator.transform.SetParent(parent);
                spectator.transform.position = pos;
                // Face toward the centre
                spectator.transform.rotation = Quaternion.LookRotation(-pos.normalized);

                // Body (capsule) + head (sphere)
                CreatePrimitiveCapsule("Body", spectator.transform, new Vector3(0f, 1f, 0f), shirt);
                CreatePrimitiveSphere("Head", spectator.transform, 0.25f,
                    new Vector3(0f, 1.5f, 0f), new Color(0.8f, 0.6f, 0.5f));
            }
        }

        // ---------- Helpers ----------
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

        /// <summary>Creates/assigns a URP/Lit material with the given color.</summary>
        private static void SetMaterial(GameObject go, Color color)
        {
            var renderer = go.GetComponent<Renderer>();
            if (renderer == null) return;

            Shader litShader = Shader.Find(URP_LIT);
            if (litShader == null)
            {
                // Fallback: try Unlit (also renders correctly in URP) before resorting to legacy shaders.
                litShader = Shader.Find("Universal Render Pipeline/Unlit");
            }
            if (litShader == null)
            {
                // Final fallback: Unlit/Color (renders correctly in URP; Standard would be pink).
                // Keep using URP Unlit as last resort — Standard renders pink/magenta in URP.
                litShader = Shader.Find("Universal Render Pipeline/Unlit");
            }
            Material mat = new Material(litShader);

            if (mat != null)
            {
                mat.color = color;
                renderer.sharedMaterial = mat;
            }
        }
    }
}
#endif