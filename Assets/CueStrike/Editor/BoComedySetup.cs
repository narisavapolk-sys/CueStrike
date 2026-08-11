using System;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using CueStrike.MascotSystem;

namespace CueStrike.EditorTools
{
    /// <summary>
    /// R32 — Editor tool: ผูก BoComedyDirector เข้า BoPanda_Prefab
    ///
    /// - MenuItem: Tools/CueStrike/Mascots/70. Setup Bo Comedy Director
    /// - ใช้ PrefabUtility.LoadPrefabContents → เพิ่ม BoComedyDirector (ถ้ายังไม่มี)
    /// - Idempotent + self-test + batchmode
    /// - ฉากไหนมี BoPanda instance (เช่น Title) ได้ผลอัตโนมัติ
    /// </summary>
    public static class BoComedySetup
    {
        private const string BoPandaPrefabPath = "Assets/CueStrike/Characters/BoPanda/BoPanda_Prefab.prefab";

        [MenuItem("Tools/CueStrike/Mascots/70. Setup Bo Comedy Director")]
        public static void SetupBoComedyMenu()
        {
            if (!RunGuards()) return;
            SetupBoComedy();
        }

        /// <summary>entry สำหรับ batchmode (-executeMethod)</summary>
        public static void SetupBoComedy()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(BoPandaPrefabPath);
            if (prefab == null)
            {
                Debug.LogError($"[BoComedySetup] Prefab not found: {BoPandaPrefabPath}");
                return;
            }

            GameObject contents = PrefabUtility.LoadPrefabContents(BoPandaPrefabPath);
            if (contents == null)
            {
                Debug.LogError("[BoComedySetup] Failed to load prefab contents.");
                return;
            }

            try
            {
                var existing = contents.GetComponentInChildren<BoComedyDirector>(true);
                if (existing != null)
                {
                    Debug.Log("[BoComedySetup] BoComedyDirector already on prefab — skipping (idempotent).");
                }
                else
                {
                    contents.AddComponent<BoComedyDirector>();
                    PrefabUtility.SaveAsPrefabAsset(contents, BoPandaPrefabPath);
                    Debug.Log("[BoComedySetup] Added BoComedyDirector to BoPanda_Prefab and saved.");
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        // ---- Guards ----

        private static bool RunGuards()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorUtility.DisplayDialog("Cannot Run", "Cannot run Setup Bo Comedy during Play Mode.", "OK");
                return false;
            }
            return true;
        }

        // ---- Self-Test ----

        [MenuItem("Tools/CueStrike/Mascots/Test Bo Comedy")]
        public static void SelfTestMenu()
        {
            SelfTest();
        }

        public static void SelfTest()
        {
            Debug.Log("[SelfTest] Bo Comedy check:");
            int pass = 0, fail = 0;

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(BoPandaPrefabPath);
            LogResult("BoPanda_Prefab exists", prefab != null, ref pass, ref fail);

            if (prefab != null)
            {
                var director = prefab.GetComponentInChildren<BoComedyDirector>(true);
                LogResult("BoComedyDirector on prefab", director != null, ref pass, ref fail);

                var animator = prefab.GetComponentInChildren<Animator>(true);
                LogResult("Animator present", animator != null, ref pass, ref fail);
                LogResult("Animator has controller", animator != null && animator.runtimeAnimatorController != null, ref pass, ref fail);

                if (animator != null && animator.runtimeAnimatorController != null)
                {
                    // ตรวจ trigger ที่ใช้: Disappointed + Speak + IsIdle มีใน controller
                    bool hasDisappointed = HasParameter(animator, "Disappointed");
                    bool hasSpeak = HasParameter(animator, "Speak");
                    bool hasIsIdle = HasParameter(animator, "IsIdle");
                    LogResult("Trigger 'Disappointed' in controller", hasDisappointed, ref pass, ref fail);
                    LogResult("Trigger 'Speak' in controller", hasSpeak, ref pass, ref fail);
                    LogResult("Bool 'IsIdle' in controller", hasIsIdle, ref pass, ref fail);
                }
            }

            Debug.Log($"[SelfTest] Bo Comedy: {pass} passed, {fail} failed.");
            if (fail > 0)
            {
                Debug.LogError($"[SelfTest] {fail} check(s) FAILED.");
            }
        }

        private static bool HasParameter(Animator animator, string name)
        {
            var controller = animator.runtimeAnimatorController;
            if (controller == null) return false;

            var assetController = controller as AnimatorController;
            if (assetController == null) return false;

            foreach (var param in assetController.parameters)
            {
                if (param.name == name) return true;
            }
            return false;
        }

        private static void LogResult(string name, bool ok, ref int pass, ref int fail)
        {
            if (ok) { pass++; Debug.Log($"  ✅ {name}"); }
            else { fail++; Debug.LogError($"  ❌ {name}"); }
        }
    }
}
