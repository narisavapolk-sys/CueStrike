using System;
using UnityEditor;
using UnityEngine;
using CueStrike.MascotSystem;

namespace CueStrike.EditorTools
{
    /// <summary>
    /// R42 — Editor tool: ตั้ง Referee Mode บน BoPanda_Prefab
    ///
    /// - MenuItem: Tools/CueStrike/Mascots/145. Set Referee Mode (Bo only / Bo+Uncle)
    /// - ReplaceUncle (default): Bo กรรมการคนเดียว — ลุงเงียบ (R40)
    /// - DuoWithUncle: Bo + ลุง กรรมการคู่ — ลุง bridge เปิดตอน runtime
    /// - Idempotent + self-test + batchmode
    /// </summary>
    public static class RefereeModeSetup
    {
        private const string BoPrefabPath = "Assets/CueStrike/Characters/BoPanda/BoPanda_Prefab.prefab";

        [MenuItem("Tools/CueStrike/Mascots/145. Set Referee Mode: Bo only (default)")]
        public static void SetBoOnly()
        {
            ApplyMode(BoRefereeEventBridge.RefereeMode.ReplaceUncle);
        }

        [MenuItem("Tools/CueStrike/Mascots/146. Set Referee Mode: Bo + Uncle (duo)")]
        public static void SetDuo()
        {
            ApplyMode(BoRefereeEventBridge.RefereeMode.DuoWithUncle);
        }

        /// <summary>batchmode: ตั้งโหมดตาม arg (0 = ReplaceUncle, 1 = DuoWithUncle)</summary>
        public static void RunFromBatch()
        {
            string[] args = System.Environment.GetCommandLineArgs();
            BoRefereeEventBridge.RefereeMode mode = BoRefereeEventBridge.RefereeMode.ReplaceUncle;
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == "-refereeMode" && int.TryParse(args[i + 1], out int v))
                {
                    mode = (BoRefereeEventBridge.RefereeMode)v;
                }
            }

            bool ok = ApplyMode(mode);
            Debug.Log(ok
                ? $"[RefereeMode] ✅ Referee mode set to {mode}."
                : "[RefereeMode] ❌ Setup failed — see errors above.");
            EditorApplication.Exit(ok ? 0 : 1);
        }

        private static bool ApplyMode(BoRefereeEventBridge.RefereeMode mode)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(BoPrefabPath);
            if (prefab == null)
            {
                Debug.LogError($"[RefereeMode] Prefab not found: {BoPrefabPath}");
                return false;
            }

            GameObject contents = PrefabUtility.LoadPrefabContents(BoPrefabPath);
            if (contents == null)
            {
                Debug.LogError("[RefereeMode] Failed to load prefab contents.");
                return false;
            }

            try
            {
                var bridge = contents.GetComponentInChildren<BoRefereeEventBridge>(true);
                if (bridge == null)
                {
                    Debug.LogError("[RefereeMode] BoRefereeEventBridge not found in prefab — run BoVoicePinSetup (R40) first.");
                    return false;
                }

                var so = new SerializedObject(bridge);
                var modeProp = so.FindProperty("refereeMode");
                if (modeProp == null)
                {
                    Debug.LogError("[RefereeMode] refereeMode field not found.");
                    return false;
                }

                if (modeProp.enumValueIndex != (int)mode)
                {
                    modeProp.enumValueIndex = (int)mode;
                    so.ApplyModifiedPropertiesWithoutUndo();
                    PrefabUtility.SaveAsPrefabAsset(contents, BoPrefabPath);
                    Debug.Log($"[RefereeMode] Saved BoPanda_Prefab with refereeMode = {mode}.");
                }
                else
                {
                    Debug.Log($"[RefereeMode] refereeMode already = {mode} — idempotent skip.");
                }

                return true;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        // ---- Self-Test (กฎข้อ 2) ----

        [MenuItem("Tools/CueStrike/Mascots/Test Referee Mode")]
        public static void SelfTestMenu()
        {
            SelfTest();
        }

        public static void SelfTest()
        {
            Debug.Log("[SelfTest] Referee Mode check:");
            int pass = 0, fail = 0;

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(BoPrefabPath);
            LogResult("BoPanda_Prefab exists", prefab != null, ref pass, ref fail);

            if (prefab != null)
            {
                var bridge = prefab.GetComponentInChildren<BoRefereeEventBridge>(true);
                LogResult("BoRefereeEventBridge present", bridge != null, ref pass, ref fail);

                if (bridge != null)
                {
                    var so = new SerializedObject(bridge);
                    var modeProp = so.FindProperty("refereeMode");
                    LogResult("refereeMode field exists", modeProp != null, ref pass, ref fail);
                    LogResult("refereeMode is valid enum", modeProp != null && Enum.IsDefined(typeof(BoRefereeEventBridge.RefereeMode), modeProp.enumValueIndex), ref pass, ref fail);
                }

                var referee = prefab.GetComponentInChildren<BoReferee>(true);
                LogResult("BoReferee present (Bo can referee)", referee != null, ref pass, ref fail);
            }

            var uncle = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/CueStrike/Characters/UncleNok/UncleNok_Prefab.prefab");
            LogResult("UncleNok_Prefab exists (duo partner available)", uncle != null, ref pass, ref fail);

            Debug.Log($"[SelfTest] Referee Mode: {pass} passed, {fail} failed.");
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
