using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using CueStrike.MascotSystem;

namespace CueStrike.EditorTools
{
    /// <summary>
    /// R29+R33 — Editor tool: วาง UncleNok + BoPanda ลงฉากจริง (Title + ห้องแข่งที่เล่นได้)
    ///
    /// - MenuItem: Tools/CueStrike/Mascots/50. Place Mascots in Scenes
    /// - Title_NoksGrandHall: ลบ `UncleNok_Placeholder` (cube) → วาง UncleNok_Prefab ที่ตำแหน่งเดิม
    /// - AAA_RoomDAY + Snooker_Demo: วาง UncleNok_Prefab เป็น referee ริมโต๊ะ (0, 0, -4.6) + BoPanda กองเชียร์ฝั่งตรงข้าม (0, 0, 4.6)
    /// - Idempotent: ถ้ามี prefab instance อยู่แล้ว → ข้าม (กัน duplicate)
    /// - ใช้ batchmode ได้: -executeMethod CueStrike.EditorTools.MascotScenePlacementSetup.PlaceMascots
    /// </summary>
    public static class MascotScenePlacementSetup
    {
        private const string UncleNokPrefabPath = "Assets/CueStrike/Characters/UncleNok/UncleNok_Prefab.prefab";
        private const string BoPandaPrefabPath = "Assets/CueStrike/Characters/BoPanda/BoPanda_Prefab.prefab";
        private const string PlaceholderName = "UncleNok_Placeholder";

        // ฉากเป้าหมาย + ตำแหน่งวางลุงโน๊ก (จาก transform ที่ตรวจจริง)
        private static readonly (string ScenePath, Vector3 Position, Vector3 Rotation)[] Targets =
        {
            // Title: แทนที่ placeholder (0, 0.9, 2) — BoPanda อยู่ฝั่ง (1.8, 0.4, -1.6)
            ("Assets/CueStrike/Scenes/Title_NoksGrandHall.unity", new Vector3(0f, 0.9f, 2f), new Vector3(0f, 180f, 0f)),
            // AAA_RoomDAY: โต๊ะ AAA Table 12ft อยู่ (0, 0.4, 0) scale (4, 0.5, 8) → referee ริมโต๊ะฝั่ง +Z
            ("Assets/CueStrike/Scenes/AAA DAY/AAA_RoomDAY.unity", new Vector3(0f, 0f, -4.6f), new Vector3(0f, 0f, 0f)),
            // Snooker_Demo: โต๊ะ CueStrikeTable_Snooker12ft อยู่ origin → referee ริมโต๊ะฝั่ง +Z
            ("Assets/CueStrike/Scenes/Snooker_Demo.unity", new Vector3(0f, 0f, -4.6f), new Vector3(0f, 0f, 0f)),
        };

        // R33 — ตำแหน่งวาง BoPanda (กองเชียร์ ฝั่งตรงข้ามลุงโน๊ก) — เฉพาะห้องแข่ง
        private static readonly (string ScenePath, Vector3 Position, Vector3 Rotation)[] BoPandaTargets =
        {
            ("Assets/CueStrike/Scenes/AAA DAY/AAA_RoomDAY.unity", new Vector3(0f, 0f, 4.6f), new Vector3(0f, 0f, 0f)),
            ("Assets/CueStrike/Scenes/Snooker_Demo.unity", new Vector3(0f, 0f, 4.6f), new Vector3(0f, 0f, 0f)),
        };

        [MenuItem("Tools/CueStrike/Mascots/50. Place Mascots in Scenes")]
        public static void PlaceMascotsMenu()
        {
            if (!RunGuards()) return;
            PlaceMascots();
        }

        /// <summary>entry สำหรับ batchmode (-executeMethod)</summary>
        public static void PlaceMascots()
        {
            GameObject unclenokPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(UncleNokPrefabPath);
            if (unclenokPrefab == null)
            {
                Debug.LogError($"[MascotPlacement] UncleNok prefab not found: {UncleNokPrefabPath}");
                return;
            }

            GameObject boPandaPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BoPandaPrefabPath);
            if (boPandaPrefab == null)
            {
                Debug.LogError($"[MascotPlacement] BoPanda prefab not found: {BoPandaPrefabPath}");
                return;
            }

            int placed = 0, skipped = 0;
            foreach (var (scenePath, pos, rot) in Targets)
            {
                if (!System.IO.File.Exists(scenePath))
                {
                    Debug.LogWarning($"[MascotPlacement] Scene not found, skipping: {scenePath}");
                    continue;
                }

                Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                if (!scene.IsValid()) continue;

                bool changed = WireMascot(scene, unclenokPrefab, pos, rot, "UncleNok", ref skipped);
                changed |= WireMascot(scene, boPandaPrefab, BoPandaPosFor(scenePath), rot, "BoPanda", ref skipped);

                if (changed && scene.isDirty)
                {
                    EditorSceneManager.SaveScene(scene);
                }

                placed++;
                Debug.Log($"[MascotPlacement] {scenePath} — mascots wired{(changed ? "" : " (already present)")}.");
            }

            Debug.Log($"[MascotPlacement] Done. Processed {placed} scenes ({skipped} already placed). Re-opening first scene...");
            EditorSceneManager.OpenScene(Targets[0].ScenePath, OpenSceneMode.Single);
        }

        /// <summary>ตำแหน่ง BoPanda ตามฉาก (เฉพาะห้องแข่ง — ฝั่งตรงข้ามลุงโน๊ก; Title ไม่วางเพราะมีอยู่แล้ว)</summary>
        private static Vector3 BoPandaPosFor(string scenePath)
        {
            foreach (var (path, pos, _) in BoPandaTargets)
            {
                if (path == scenePath) return pos;
            }
            return Vector3.zero;
        }

        private static bool WireMascot(Scene scene, GameObject prefab, Vector3 position, Vector3 euler, string mascotName, ref int skipped)
        {
            bool changed = false;

            // 1. ถ้ามี instance อยู่แล้ว → ข้าม (idempotent) — ตรวจชื่อ GameObject มี mascotName
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.name.Contains(mascotName))
                {
                    skipped++;
                    Debug.Log($"[MascotPlacement] {mascotName} already in '{scene.name}' — skipping (idempotent).");
                    return false;
                }
            }

            // 2. ลบ placeholder (cube) ถ้ามี — เฉพาะ Title + UncleNok
            if (scene.name.Contains("Title") && mascotName == "UncleNok")
            {
                GameObject placeholder = GameObject.Find(PlaceholderName);
                if (placeholder != null)
                {
                    UnityEngine.Object.DestroyImmediate(placeholder);
                    changed = true;
                    Debug.Log($"[MascotPlacement] Removed '{PlaceholderName}' from '{scene.name}'.");
                }
            }

            // 3. Instantiate prefab ที่ root + วางตำแหน่ง
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            if (instance == null)
            {
                Debug.LogError($"[MascotPlacement] Failed to instantiate prefab in '{scene.name}'.");
                return changed;
            }

            instance.name = mascotName == "UncleNok" ? "UncleNok_Prefab" : "BoPanda_Prefab";
            instance.transform.position = position;
            instance.transform.rotation = Quaternion.Euler(euler);
            Undo.RegisterCreatedObjectUndo(instance, $"Place {mascotName} Mascot");

            changed = true;
            Debug.Log($"[MascotPlacement] Placed {mascotName} at {position} in '{scene.name}'.");
            return changed;
        }

        // ---- Guards (ตาม convention) ----

        private static bool RunGuards()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorUtility.DisplayDialog("Cannot Run", "Cannot run Place Mascots during Play Mode.", "OK");
                return false;
            }

            if (EditorSceneManager.GetActiveScene().isDirty && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                Debug.LogWarning("[MascotPlacement] Setup cancelled — unsaved changes not confirmed.");
                return false;
            }
            return true;
        }

        // ---- Self-Test (กฎข้อ 2) ----

        [MenuItem("Tools/CueStrike/Mascots/Test Mascot Placement")]
        public static void SelfTestMenu()
        {
            SelfTest();
        }

        public static void SelfTest()
        {
            Debug.Log("[SelfTest] Mascot Placement check:");
            int pass = 0, fail = 0;

            // 1. Prefab โหลดได้ + มี Animator + controller
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(UncleNokPrefabPath);
            LogResult("UncleNok_Prefab exists", prefab != null, ref pass, ref fail);

            if (prefab != null)
            {
                var animator = prefab.GetComponentInChildren<Animator>();
                LogResult("Prefab has Animator", animator != null, ref pass, ref fail);
                LogResult("Animator has controller", animator != null && animator.runtimeAnimatorController != null, ref pass, ref fail);
                var referee = prefab.GetComponentInChildren<UncleNokReferee>();
                LogResult("Prefab has UncleNokReferee", referee != null, ref pass, ref fail);
            }

            // 2. ฉากที่เปิดอยู่มี mascot ครบ (ถ้าเป็นฉากเป้าหมาย)
            Scene scene = EditorSceneManager.GetActiveScene();
            if (scene.IsValid() && !string.IsNullOrEmpty(scene.path))
            {
                foreach (var (scenePath, _, _) in Targets)
                {
                    if (scene.path == scenePath)
                    {
                        var referee = UnityEngine.Object.FindAnyObjectByType<UncleNokReferee>();
                        LogResult($"UncleNok in {scene.name}", referee != null, ref pass, ref fail);

                        // R33 — BoPanda (เฉพาะห้องแข่ง)
                        bool hasBo = false;
                        foreach (var root in scene.GetRootGameObjects())
                        {
                            if (root.name.Contains("BoPanda")) { hasBo = true; break; }
                        }
                        LogResult($"BoPanda in {scene.name}", hasBo, ref pass, ref fail);
                        break;
                    }
                }
            }
            else
            {
                Debug.Log("[SelfTest] No scene open — skipping scene check (open one of the 3 target scenes and re-run).");
            }

            Debug.Log($"[SelfTest] Mascot Placement: {pass} passed, {fail} failed.");
            if (fail > 0)
            {
                Debug.LogError($"[SelfTest] {fail} check(s) FAILED.");
            }
        }

        private static void LogResult(string name, bool ok, ref int pass, ref int fail)
        {
            if (ok) { pass++; Debug.Log($"  ✅ {name}"); }
            else { fail++; Debug.LogError($"  ❌ {name}"); }
        }
    }
}
