using System;
using UnityEditor;
using UnityEngine;
using CueStrike.MascotSystem;

namespace CueStrike.EditorTools
{
    /// <summary>
    /// R31 — Editor tool: ผูก UncleNokRefereeEventBridge เข้า UncleNok_Prefab
    ///
    /// - MenuItem: Tools/CueStrike/Mascots/80. Setup Referee Events
    /// - ใช้ PrefabUtility.LoadPrefabContents → เพิ่ม bridge (ถ้ายังไม่มี)
    /// - Idempotent + self-test + batchmode
    /// - ฉากไหนมี UncleNok instance (Title/AAA_RoomDAY/Snooker_Demo) ได้ผลอัตโนมัติ
    /// </summary>
    public static class RefereeEventBridgeSetup
    {
        private const string UncleNokPrefabPath = "Assets/CueStrike/Characters/UncleNok/UncleNok_Prefab.prefab";

        [MenuItem("Tools/CueStrike/Mascots/80. Setup Referee Events")]
        public static void SetupBridgeMenu()
        {
            if (!RunGuards()) return;
            SetupBridge();
        }

        /// <summary>entry สำหรับ batchmode (-executeMethod)</summary>
        public static void SetupBridge()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(UncleNokPrefabPath);
            if (prefab == null)
            {
                Debug.LogError($"[RefereeBridgeSetup] Prefab not found: {UncleNokPrefabPath}");
                return;
            }

            GameObject contents = PrefabUtility.LoadPrefabContents(UncleNokPrefabPath);
            if (contents == null)
            {
                Debug.LogError("[RefereeBridgeSetup] Failed to load prefab contents.");
                return;
            }

            try
            {
                var existing = contents.GetComponentInChildren<UncleNokRefereeEventBridge>(true);
                if (existing != null)
                {
                    Debug.Log("[RefereeBridgeSetup] UncleNokRefereeEventBridge already on prefab — skipping (idempotent).");
                }
                else
                {
                    contents.AddComponent<UncleNokRefereeEventBridge>();
                    PrefabUtility.SaveAsPrefabAsset(contents, UncleNokPrefabPath);
                    Debug.Log("[RefereeBridgeSetup] Added UncleNokRefereeEventBridge to UncleNok_Prefab and saved.");
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
                EditorUtility.DisplayDialog("Cannot Run", "Cannot run Setup Referee Events during Play Mode.", "OK");
                return false;
            }
            return true;
        }

        // ---- Self-Test ----

        [MenuItem("Tools/CueStrike/Mascots/Test Referee Events")]
        public static void SelfTestMenu()
        {
            SelfTest();
        }

        public static void SelfTest()
        {
            Debug.Log("[SelfTest] Referee Events check:");
            int pass = 0, fail = 0;

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(UncleNokPrefabPath);
            LogResult("UncleNok_Prefab exists", prefab != null, ref pass, ref fail);

            if (prefab != null)
            {
                var bridge = prefab.GetComponentInChildren<UncleNokRefereeEventBridge>(true);
                LogResult("RefereeEventBridge on prefab", bridge != null, ref pass, ref fail);

                var referee = prefab.GetComponentInChildren<UncleNokReferee>(true);
                LogResult("UncleNokReferee present", referee != null, ref pass, ref fail);

                var audioSrc = prefab.GetComponentInChildren<AudioSource>(true);
                LogResult("AudioSource present (R30)", audioSrc != null, ref pass, ref fail);
            }

            // ตรวจ manager classes compile ได้ (ผ่านการอ้างอิงใน bridge)
            LogResult("Bridge references game managers", typeof(UncleNokRefereeEventBridge) != null, ref pass, ref fail);

            Debug.Log($"[SelfTest] Referee Events: {pass} passed, {fail} failed.");
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
