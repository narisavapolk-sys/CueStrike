using System;
using UnityEditor;
using UnityEngine;
using CueStrike.MascotSystem;

namespace CueStrike.EditorTools
{
    /// <summary>
    /// R30 — Editor tool: ผูก UncleNokReferee กับ prefab จริง
    ///
    /// - MenuItem: Tools/CueStrike/Mascots/60. Pin UncleNok Voice & Refs
    /// - เพิ่ม AudioSource (3D spatial) + assign _animator / _audioSource / _homePosition
    ///   ใน UncleNok_Prefab ผ่าน PrefabUtility.LoadPrefabContents (Unity จัดการ fileID เอง)
    /// - clips 14 ตัว assign ครบแล้วใน prefab — tool ตรวจ + รายงาน (ไม่แก้)
    /// - Idempotent: ถ้า AudioSource + refs ครบแล้ว → ข้าม
    /// - ใช้ batchmode ได้: -executeMethod CueStrike.EditorTools.UncleNokVoicePinSetup.PinVoice
    /// </summary>
    public static class UncleNokVoicePinSetup
    {
        private const string PrefabPath = "Assets/CueStrike/Characters/UncleNok/UncleNok_Prefab.prefab";

        // field ใน UncleNokReferee ที่ต้องการ assign
        private static readonly string[] ClipFields =
        {
            "_frameStartClips", "_frameEndClips", "_matchStartClips", "_matchEndClips",
            "_playerTurnStartClips", "_playerTurnEndClips",
            "_potSuccessClips", "_centuryBreakClips", "_highBreakClips", "_maximumBreakClips",
            "_clearanceClips", "_breakClips",
            "_foulCalledClips", "_foulCueBallPottedClips", "_foulNoBallContactedClips",
            "_foulWrongBallFirstClips", "_foulNoCushionClips", "_foulBallOffTableClips",
            "_snookerEscapeClips", "_flukeClips", "_safetyPlayedClips"
        };

        [MenuItem("Tools/CueStrike/Mascots/60. Pin UncleNok Voice & Refs")]
        public static void PinVoiceMenu()
        {
            if (!RunGuards()) return;
            PinVoice();
        }

        /// <summary>entry สำหรับ batchmode (-executeMethod)</summary>
        public static void PinVoice()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null)
            {
                Debug.LogError($"[VoicePin] Prefab not found: {PrefabPath}");
                return;
            }

            // โหลด prefab contents เพื่อแก้ (Unity จัดการ fileID ให้)
            GameObject contents = PrefabUtility.LoadPrefabContents(PrefabPath);
            if (contents == null)
            {
                Debug.LogError("[VoicePin] Failed to load prefab contents.");
                return;
            }

            try
            {
                var referee = contents.GetComponentInChildren<UncleNokReferee>(true);
                if (referee == null)
                {
                    Debug.LogError("[VoicePin] UncleNokReferee not found in prefab.");
                    return;
                }

                bool changed = false;
                var so = new SerializedObject(referee);

                // ---------- 1. AudioSource (เพิ่มถ้ายังไม่มี) ----------
                AudioSource audioSrc = contents.GetComponentInChildren<AudioSource>(true);
                if (audioSrc == null)
                {
                    audioSrc = contents.AddComponent<AudioSource>();
                    audioSrc.spatialBlend = 1f;          // 3D spatial (VR)
                    audioSrc.playOnAwake = false;
                    audioSrc.rolloffMode = AudioRolloffMode.Logarithmic;
                    audioSrc.maxDistance = 20f;
                    changed = true;
                    Debug.Log("[VoicePin] Added AudioSource (3D spatial) to UncleNok_Prefab.");
                }
                else
                {
                    // ปรับค่าถ้าจำเป็น
                    if (Mathf.Abs(audioSrc.spatialBlend - 1f) > 0.01f)
                    {
                        audioSrc.spatialBlend = 1f;
                        changed = true;
                    }
                }

                // ---------- 2. Animator ----------
                Animator animator = contents.GetComponentInChildren<Animator>(true);

                // ---------- 3. assign refs (ผ่าน SerializedObject — รองรับ private fields) ----------
                changed |= SetRef(so, "_animator", animator != null ? animator : null);
                changed |= SetRef(so, "_audioSource", audioSrc);
                changed |= SetRef(so, "_homePosition", contents.transform);
                so.ApplyModifiedPropertiesWithoutUndo();

                // ---------- 4. ตรวจ clips ครบไหม (รายงาน ไม่แก้) ----------
                int filled = 0, total = 0;
                foreach (string field in ClipFields)
                {
                    SerializedProperty prop = so.FindProperty(field);
                    if (prop == null) continue;
                    total++;
                    if (prop.arraySize > 0 && prop.GetArrayElementAtIndex(0).objectReferenceValue != null)
                    {
                        filled++;
                    }
                }
                Debug.Log($"[VoicePin] Clip fields: {filled}/{total} filled (14 wav files on disk).");

                if (changed)
                {
                    PrefabUtility.SaveAsPrefabAsset(contents, PrefabPath);
                    Debug.Log("[VoicePin] Saved UncleNok_Prefab with AudioSource + refs wired.");
                }
                else
                {
                    Debug.Log("[VoicePin] Already wired — skipping (idempotent).");
                }
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
                Debug.LogWarning($"[VoicePin] Field '{fieldName}' not found.");
                return false;
            }

            if (prop.objectReferenceValue == value) return false;

            prop.objectReferenceValue = value;
            Debug.Log($"[VoicePin] {fieldName} = {(value != null ? value.name : "null")}");
            return true;
        }

        // ---- Guards (ตาม convention) ----

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

        [MenuItem("Tools/CueStrike/Mascots/Test UncleNok Voice Pin")]
        public static void SelfTestMenu()
        {
            SelfTest();
        }

        public static void SelfTest()
        {
            Debug.Log("[SelfTest] UncleNok Voice Pin check:");
            int pass = 0, fail = 0;

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            LogResult("UncleNok_Prefab exists", prefab != null, ref pass, ref fail);

            if (prefab != null)
            {
                var audioSrc = prefab.GetComponentInChildren<AudioSource>(true);
                LogResult("AudioSource present", audioSrc != null, ref pass, ref fail);

                var animator = prefab.GetComponentInChildren<Animator>(true);
                LogResult("Animator present", animator != null, ref pass, ref fail);
                LogResult("Animator has controller", animator != null && animator.runtimeAnimatorController != null, ref pass, ref fail);

                var referee = prefab.GetComponentInChildren<UncleNokReferee>(true);
                LogResult("UncleNokReferee present", referee != null, ref pass, ref fail);

                if (referee != null)
                {
                    var so = new SerializedObject(referee);
                    SerializedProperty animProp = so.FindProperty("_animator");
                    SerializedProperty audioProp = so.FindProperty("_audioSource");
                    SerializedProperty homeProp = so.FindProperty("_homePosition");

                    LogResult("_animator wired", animProp != null && animProp.objectReferenceValue != null, ref pass, ref fail);
                    LogResult("_audioSource wired", audioProp != null && audioProp.objectReferenceValue != null, ref pass, ref fail);
                    LogResult("_homePosition wired", homeProp != null && homeProp.objectReferenceValue != null, ref pass, ref fail);

                    // clips: อย่างน้อย potSuccess + foulCalled + matchStart + turnStart ไม่ว่าง
                    LogResult("matchStart clips filled", ClipArraySize(so, "_matchStartClips") > 0, ref pass, ref fail);
                    LogResult("potSuccess clips filled", ClipArraySize(so, "_potSuccessClips") > 0, ref pass, ref fail);
                    LogResult("foulCalled clips filled", ClipArraySize(so, "_foulCalledClips") > 0, ref pass, ref fail);
                    LogResult("turnStart clips filled", ClipArraySize(so, "_playerTurnStartClips") > 0, ref pass, ref fail);
                }
            }

            Debug.Log($"[SelfTest] UncleNok Voice Pin: {pass} passed, {fail} failed.");
            if (fail > 0)
            {
                Debug.LogError($"[SelfTest] {fail} check(s) FAILED.");
            }
        }

        private static int ClipArraySize(SerializedObject so, string field)
        {
            SerializedProperty prop = so.FindProperty(field);
            return prop == null ? 0 : prop.arraySize;
        }

        private static void LogResult(string name, bool ok, ref int pass, ref int fail)
        {
            if (ok) { pass++; Debug.Log($"  ✅ {name}"); }
            else { fail++; Debug.LogError($"  ❌ {name}"); }
        }
    }
}
