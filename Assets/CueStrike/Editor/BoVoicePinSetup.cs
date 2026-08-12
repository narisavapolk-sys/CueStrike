using System;
using UnityEditor;
using UnityEngine;
using CueStrike.MascotSystem;

namespace CueStrike.EditorTools
{
    /// <summary>
    /// R40 — Editor tool: ผูก BoReferee กับ BoPanda_Prefab (Bo เป็นกรรมการ)
    ///
    /// - MenuItem: Tools/CueStrike/Mascots/130. Pin Bo Referee Voice & Refs
    /// - เพิ่ม BoReferee + BoRefereeEventBridge + AudioSource (3D spatial)
    ///   + assign _animator / _audioSource / _homePosition
    ///   + assign 14 clips (NongBo) ผ่าน PrefabUtility.LoadPrefabContents
    /// - สลับบทบาท: disable UncleNokRefereeEventBridge ใน UncleNok_Prefab → ลุงเป็นกองเชียร์
    /// - Idempotent: ถ้าครบแล้ว → ข้าม
    /// - ใช้ batchmode ได้: -executeMethod CueStrike.EditorTools.BoVoicePinSetup.PinVoice
    /// </summary>
    public static class BoVoicePinSetup
    {
        private const string BoPrefabPath = "Assets/CueStrike/Characters/BoPanda/BoPanda_Prefab.prefab";
        private const string UnclePrefabPath = "Assets/CueStrike/Characters/UncleNok/UncleNok_Prefab.prefab";
        private const string NongBoDir = "Assets/CueStrike/Audio/Clips/Voice/NongBo";

        // mapping: field → file names (14 clips)
        private static readonly (string Field, string[] Files)[] ClipMapping =
        {
            ("_matchStartClips", new[] { "bo_match_start_01", "bo_match_start_02" }),
            ("_playerTurnStartClips", new[] { "bo_turn_start_01", "bo_turn_start_02" }),
            ("_potSuccessClips", new[] { "bo_pot_success_01", "bo_pot_success_02", "bo_pot_success_03" }),
            ("_centuryBreakClips", new[] { "bo_century_break" }),
            ("_highBreakClips", new[] { "bo_high_break" }),
            ("_maximumBreakClips", Array.Empty<string>()),
            ("_clearanceClips", new[] { "bo_clearance" }),
            ("_breakClips", new[] { "bo_break_shot" }),
            ("_foulCalledClips", new[] { "bo_foul_called_01", "bo_foul_called_02" }),
            ("_foulCueBallPottedClips", new[] { "bo_foul_cueball" }),
            // Reuse the two turn-start voice lines for frames 2+; Chinese Pool has no dedicated frame-start recordings.
            ("_frameStartClips", new[] { "bo_turn_start_01", "bo_turn_start_02" }),
            ("_frameEndClips", Array.Empty<string>()),
            ("_matchEndClips", Array.Empty<string>()),
            ("_playerTurnEndClips", Array.Empty<string>()),
        };

        [MenuItem("Tools/CueStrike/Mascots/130. Pin Bo Referee Voice & Refs")]
        public static void PinVoiceMenu()
        {
            if (!RunGuards()) return;
            PinVoice();
        }

        /// <summary>entry สำหรับ batchmode (-executeMethod)</summary>
        public static void PinVoice()
        {
            bool ok = PinBoPrefab();
            ok &= DisableUncleBridge();
            Debug.Log(ok
                ? "[BoVoicePin] ✅ Bo referee pinned + Uncle bridge disabled — Bo is now referee, Uncle is cheerleader."
                : "[BoVoicePin] ❌ Setup failed — see errors above.");
            if (!ok) EditorApplication.Exit(1);
            EditorApplication.Exit(0);
        }

        private static bool PinBoPrefab()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(BoPrefabPath);
            if (prefab == null)
            {
                Debug.LogError($"[BoVoicePin] Prefab not found: {BoPrefabPath}");
                return false;
            }

            GameObject contents = PrefabUtility.LoadPrefabContents(BoPrefabPath);
            if (contents == null)
            {
                Debug.LogError("[BoVoicePin] Failed to load prefab contents.");
                return false;
            }

            try
            {
                bool changed = false;

                // ---------- 1. BoReferee (เพิ่มถ้ายังไม่มี) ----------
                var referee = contents.GetComponentInChildren<BoReferee>(true);
                if (referee == null)
                {
                    referee = contents.AddComponent<BoReferee>();
                    changed = true;
                    Debug.Log("[BoVoicePin] Added BoReferee to BoPanda_Prefab.");
                }

                // ---------- 2. AudioSource (เพิ่มถ้ายังไม่มี) ----------
                AudioSource audioSrc = contents.GetComponentInChildren<AudioSource>(true);
                if (audioSrc == null)
                {
                    audioSrc = contents.AddComponent<AudioSource>();
                    audioSrc.spatialBlend = 1f;          // 3D spatial (VR)
                    audioSrc.playOnAwake = false;
                    audioSrc.rolloffMode = AudioRolloffMode.Logarithmic;
                    audioSrc.maxDistance = 20f;
                    changed = true;
                    Debug.Log("[BoVoicePin] Added AudioSource (3D spatial) to BoPanda_Prefab.");
                }
                else if (Mathf.Abs(audioSrc.spatialBlend - 1f) > 0.01f)
                {
                    audioSrc.spatialBlend = 1f;
                    changed = true;
                }

                Animator animator = contents.GetComponentInChildren<Animator>(true);

                // ---------- 3. assign refs ----------
                var so = new SerializedObject(referee);
                changed |= SetRef(so, "_animator", animator != null ? animator : null);
                changed |= SetRef(so, "_audioSource", audioSrc);
                changed |= SetRef(so, "_homePosition", contents.transform);
                so.ApplyModifiedPropertiesWithoutUndo();

                // ---------- 4. assign clips (14) ----------
                foreach (var (field, files) in ClipMapping)
                {
                    var prop = so.FindProperty(field);
                    if (prop == null) continue;

                    prop.arraySize = files.Length;
                    for (int i = 0; i < files.Length; i++)
                    {
                        string path = $"{NongBoDir}/{files[i]}.wav";
                        AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                        if (clip == null)
                        {
                            Debug.LogWarning($"[BoVoicePin] Clip not found: {path}");
                            continue;
                        }
                        var elem = prop.GetArrayElementAtIndex(i);
                        if (elem.objectReferenceValue != clip)
                        {
                            elem.objectReferenceValue = clip;
                            changed = true;
                        }
                    }
                }
                so.ApplyModifiedPropertiesWithoutUndo();

                // ---------- 5. BoRefereeEventBridge (เพิ่มถ้ายังไม่มี) ----------
                var bridge = contents.GetComponentInChildren<BoRefereeEventBridge>(true);
                if (bridge == null)
                {
                    contents.AddComponent<BoRefereeEventBridge>();
                    changed = true;
                    Debug.Log("[BoVoicePin] Added BoRefereeEventBridge to BoPanda_Prefab.");
                }

                if (changed)
                {
                    PrefabUtility.SaveAsPrefabAsset(contents, BoPrefabPath);
                    Debug.Log("[BoVoicePin] Saved BoPanda_Prefab with BoReferee + clips + bridge.");
                }
                else
                {
                    Debug.Log("[BoVoicePin] BoPanda already wired — skipping (idempotent).");
                }

                return true;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        private static bool DisableUncleBridge()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(UnclePrefabPath);
            if (prefab == null)
            {
                Debug.LogError($"[BoVoicePin] UncleNok prefab not found: {UnclePrefabPath}");
                return false;
            }

            GameObject contents = PrefabUtility.LoadPrefabContents(UnclePrefabPath);
            if (contents == null)
            {
                Debug.LogError("[BoVoicePin] Failed to load UncleNok prefab contents.");
                return false;
            }

            try
            {
                var bridge = contents.GetComponentInChildren<UncleNokRefereeEventBridge>(true);
                if (bridge == null)
                {
                    Debug.Log("[BoVoicePin] UncleNokRefereeEventBridge not present — nothing to disable (idempotent).");
                    return true;
                }

                if (bridge.enabled)
                {
                    bridge.enabled = false;
                    PrefabUtility.SaveAsPrefabAsset(contents, UnclePrefabPath);
                    Debug.Log("[BoVoicePin] Disabled UncleNokRefereeEventBridge — Uncle is now cheerleader (no score calls).");
                }
                else
                {
                    Debug.Log("[BoVoicePin] UncleNokRefereeEventBridge already disabled — idempotent skip.");
                }
                return true;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        private static bool SetRef(SerializedObject so, string fieldName, UnityEngine.Object value)
        {
            SerializedProperty prop = so.FindProperty(fieldName);
            if (prop == null)
            {
                Debug.LogWarning($"[BoVoicePin] Field '{fieldName}' not found.");
                return false;
            }

            if (prop.objectReferenceValue == value) return false;

            prop.objectReferenceValue = value;
            Debug.Log($"[BoVoicePin] {fieldName} = {(value != null ? value.name : "null")}");
            return true;
        }

        private static bool RunGuards()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorUtility.DisplayDialog("Cannot Run", "Cannot run Pin Voice during Play Mode.", "OK");
                return false;
            }
            return true;
        }

        // ---- Self-Test (กฎข้อ 2) ----

        [MenuItem("Tools/CueStrike/Mascots/Test Bo Referee Voice Pin")]
        public static void SelfTestMenu()
        {
            SelfTest();
        }

        public static void SelfTest()
        {
            Debug.Log("[SelfTest] Bo Referee Voice Pin check:");
            int pass = 0, fail = 0;

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(BoPrefabPath);
            LogResult("BoPanda_Prefab exists", prefab != null, ref pass, ref fail);

            if (prefab != null)
            {
                var audioSrc = prefab.GetComponentInChildren<AudioSource>(true);
                LogResult("AudioSource present", audioSrc != null, ref pass, ref fail);

                var animator = prefab.GetComponentInChildren<Animator>(true);
                LogResult("Animator present", animator != null, ref pass, ref fail);

                var referee = prefab.GetComponentInChildren<BoReferee>(true);
                LogResult("BoReferee present", referee != null, ref pass, ref fail);

                var bridge = prefab.GetComponentInChildren<BoRefereeEventBridge>(true);
                LogResult("BoRefereeEventBridge present", bridge != null, ref pass, ref fail);

                if (referee != null)
                {
                    var so = new SerializedObject(referee);
                    LogResult("_animator wired", FindRef(so, "_animator") != null, ref pass, ref fail);
                    LogResult("_audioSource wired", FindRef(so, "_audioSource") != null, ref pass, ref fail);
                    LogResult("_homePosition wired", FindRef(so, "_homePosition") != null, ref pass, ref fail);

                    LogResult("matchStart clips filled (2)", ClipArraySize(so, "_matchStartClips") == 2, ref pass, ref fail);
                    LogResult("turnStart clips filled (2)", ClipArraySize(so, "_playerTurnStartClips") == 2, ref pass, ref fail);
                    LogResult("frameStart clips filled (2)", ClipArraySize(so, "_frameStartClips") == 2, ref pass, ref fail);
                    LogResult("potSuccess clips filled (3)", ClipArraySize(so, "_potSuccessClips") == 3, ref pass, ref fail);
                    LogResult("century clip filled (1)", ClipArraySize(so, "_centuryBreakClips") == 1, ref pass, ref fail);
                    LogResult("highBreak clip filled (1)", ClipArraySize(so, "_highBreakClips") == 1, ref pass, ref fail);
                    LogResult("clearance clip filled (1)", ClipArraySize(so, "_clearanceClips") == 1, ref pass, ref fail);
                    LogResult("break clip filled (1)", ClipArraySize(so, "_breakClips") == 1, ref pass, ref fail);
                    LogResult("foulCalled clips filled (2)", ClipArraySize(so, "_foulCalledClips") == 2, ref pass, ref fail);
                    LogResult("foulCueBall clip filled (1)", ClipArraySize(so, "_foulCueBallPottedClips") == 1, ref pass, ref fail);
                }
            }

            var uncle = AssetDatabase.LoadAssetAtPath<GameObject>(UnclePrefabPath);
            if (uncle != null)
            {
                var uncleContents = PrefabUtility.LoadPrefabContents(UnclePrefabPath);
                try
                {
                    var uncleBridge = uncleContents.GetComponentInChildren<UncleNokRefereeEventBridge>(true);
                    LogResult("UncleNokRefereeEventBridge disabled (cheerleader)", uncleBridge != null && !uncleBridge.enabled, ref pass, ref fail);
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(uncleContents);
                }
            }

            Debug.Log($"[SelfTest] Bo Referee Voice Pin: {pass} passed, {fail} failed.");
            if (fail > 0)
            {
                Debug.LogError($"[SelfTest] {fail} check(s) FAILED.");
            }
        }

        private static UnityEngine.Object FindRef(SerializedObject so, string field)
        {
            SerializedProperty prop = so.FindProperty(field);
            return prop == null ? null : prop.objectReferenceValue;
        }

        private static int ClipArraySize(SerializedObject so, string field)
        {
            SerializedProperty prop = so.FindProperty(field);
            return prop == null ? -1 : prop.arraySize;
        }

        private static void LogResult(string name, bool ok, ref int pass, ref int fail)
        {
            if (ok) { pass++; Debug.Log($"  ✅ {name}"); }
            else { fail++; Debug.LogError($"  ❌ {name}"); }
        }
    }
}
