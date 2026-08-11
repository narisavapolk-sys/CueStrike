using UnityEngine;
using UnityEditor;
using System.IO;

namespace CueStrike.EditorTools
{
    public class CueStrikeVoiceBinderEditor : EditorWindow
    {
        private const string UncleNokVoiceDir = "Assets/CueStrike/Audio/Clips/Voice/UncleNok";
        private const string NongBoVoiceDir = "Assets/CueStrike/Audio/Clips/Voice/NongBo";

        [MenuItem("CueStrike/Audio/Bind Voice Clips to Prefabs")]
        public static void BindVoiceClips()
        {
            try
            {
                Debug.Log("[CueStrikeVoiceBinderEditor] Starting voice bind...");
                BindUncleNokVoice();
                BindNongBoVoice();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log("[CueStrikeVoiceBinderEditor] Voice clips bound successfully!");
                EditorUtility.DisplayDialog("Success", "Voice clips bound successfully!", "OK");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[CueStrikeVoiceBinderEditor] EXCEPTION: {ex.Message}\n{ex.StackTrace}");
                EditorUtility.DisplayDialog("Error", $"Failed to bind voice clips:\n{ex.Message}", "OK");
            }
        }

        private static void BindUncleNokVoice()
        {
            // Try "UncleNokReferee" first, then fall back to the actual prefab name "UncleNok_Prefab"
            string[] guids = AssetDatabase.FindAssets("t:Prefab UncleNokReferee");
            if (guids.Length == 0)
            {
                guids = AssetDatabase.FindAssets("t:Prefab UncleNok_Prefab");
            }
            if (guids.Length == 0)
            {
                Debug.LogWarning("[CueStrikeVoiceBinderEditor] UncleNok prefab not found (tried 'UncleNokReferee' and 'UncleNok_Prefab')!");
                return;
            }

            string prefabPath = AssetDatabase.GUIDToAssetPath(guids[0]);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                Debug.LogWarning($"[CueStrikeVoiceBinderEditor] Failed to load prefab at: {prefabPath}");
                return;
            }

            var referee = prefab.GetComponent<CueStrike.MascotSystem.UncleNokReferee>();
            if (referee == null)
            {
                Debug.LogWarning("[CueStrikeVoiceBinderEditor] UncleNokReferee component not found!");
                return;
            }

            AudioClip[] matchStart = LoadClips(UncleNokVoiceDir, "match_start");
            AudioClip[] turnStart = LoadClips(UncleNokVoiceDir, "turn_start");
            AudioClip[] potSuccess = LoadClips(UncleNokVoiceDir, "pot_success");
            AudioClip[] centuryBreak = LoadClips(UncleNokVoiceDir, "century_break");
            AudioClip[] highBreak = LoadClips(UncleNokVoiceDir, "high_break");
            AudioClip[] foulCalled = LoadClips(UncleNokVoiceDir, "foul_called");
            AudioClip[] foulCueBall = LoadClips(UncleNokVoiceDir, "foul_cueball");
            AudioClip[] breakShot = LoadClips(UncleNokVoiceDir, "break_shot");
            AudioClip[] clearance = LoadClips(UncleNokVoiceDir, "clearance");

            SerializedObject so = new SerializedObject(referee);
            SetClipArray(so, "_matchStartClips", matchStart);
            SetClipArray(so, "_playerTurnStartClips", turnStart);
            SetClipArray(so, "_potSuccessClips", potSuccess);
            SetClipArray(so, "_centuryBreakClips", centuryBreak);
            SetClipArray(so, "_highBreakClips", highBreak);
            SetClipArray(so, "_foulCalledClips", foulCalled);
            SetClipArray(so, "_foulCueBallPottedClips", foulCueBall);
            SetClipArray(so, "_breakClips", breakShot);
            SetClipArray(so, "_clearanceClips", clearance);
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(prefab);
            Debug.Log($"[CueStrikeVoiceBinderEditor] UncleNok voice bound: {matchStart.Length + turnStart.Length + potSuccess.Length + centuryBreak.Length + highBreak.Length + foulCalled.Length + foulCueBall.Length + breakShot.Length + clearance.Length} clips");
        }

        private static void BindNongBoVoice()
        {
            // NongBo = BoPanda character. Bind clips into BoPandaBanter on the BoPanda prefab.
            string[] guids = AssetDatabase.FindAssets("t:Prefab BoPanda_Prefab");
            if (guids.Length == 0)
            {
                guids = AssetDatabase.FindAssets("t:Prefab BoPanda");
            }
            if (guids.Length == 0)
            {
                Debug.LogWarning("[CueStrikeVoiceBinderEditor] BoPanda prefab not found!");
                return;
            }

            string prefabPath = AssetDatabase.GUIDToAssetPath(guids[0]);

            // Load prefab contents for editing (allows adding missing components)
            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
            if (prefabRoot == null)
            {
                Debug.LogWarning($"[CueStrikeVoiceBinderEditor] Failed to load prefab at: {prefabPath}");
                return;
            }

            try
            {
                var banter = prefabRoot.GetComponent<CueStrike.MascotSystem.BoPandaBanter>();
                if (banter == null)
                {
                    banter = prefabRoot.AddComponent<CueStrike.MascotSystem.BoPandaBanter>();
                    Debug.Log("[CueStrikeVoiceBinderEditor] Added BoPandaBanter component to prefab.");
                }

                // Ensure AudioSource exists for voice playback
                var audioSource = prefabRoot.GetComponent<AudioSource>();
                if (audioSource == null)
                {
                    audioSource = prefabRoot.AddComponent<AudioSource>();
                    audioSource.playOnAwake = false;
                    audioSource.spatialBlend = 0f;
                    Debug.Log("[CueStrikeVoiceBinderEditor] Added AudioSource component to prefab.");
                }

                AudioClip[] matchStart = LoadClips(NongBoVoiceDir, "bo_match_start");
                AudioClip[] turnStart = LoadClips(NongBoVoiceDir, "bo_turn_start");
                AudioClip[] potSuccess = LoadClips(NongBoVoiceDir, "bo_pot_success");
                AudioClip[] centuryBreak = LoadClips(NongBoVoiceDir, "bo_century_break");
                AudioClip[] highBreak = LoadClips(NongBoVoiceDir, "bo_high_break");
                AudioClip[] foulCalled = LoadClips(NongBoVoiceDir, "bo_foul_called");
                AudioClip[] foulCueBall = LoadClips(NongBoVoiceDir, "bo_foul_cueball");
                AudioClip[] breakShot = LoadClips(NongBoVoiceDir, "bo_break_shot");
                AudioClip[] clearance = LoadClips(NongBoVoiceDir, "bo_clearance");

                SerializedObject so = new SerializedObject(banter);
                SetObjectRef(so, "_audioSource", audioSource);
                SetClipArray(so, "_matchStartClips", matchStart);
                SetClipArray(so, "_turnStartClips", turnStart);
                SetClipArray(so, "_potSuccessClips", potSuccess);
                SetClipArray(so, "_centuryBreakClips", centuryBreak);
                SetClipArray(so, "_highBreakClips", highBreak);
                SetClipArray(so, "_foulCalledClips", foulCalled);
                SetClipArray(so, "_foulCueBallClips", foulCueBall);
                SetClipArray(so, "_breakClips", breakShot);
                SetClipArray(so, "_clearanceClips", clearance);
                so.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
                Debug.Log($"[CueStrikeVoiceBinderEditor] NongBo (BoPanda) voice bound: {matchStart.Length + turnStart.Length + potSuccess.Length + centuryBreak.Length + highBreak.Length + foulCalled.Length + foulCueBall.Length + breakShot.Length + clearance.Length} clips");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }

        private static AudioClip[] LoadClips(string dir, string prefix)
        {
            if (!Directory.Exists(dir))
            {
                Debug.LogWarning($"[CueStrikeVoiceBinderEditor] Directory not found: {dir}");
                return new AudioClip[0];
            }

            string[] files = Directory.GetFiles(dir, $"{prefix}*.wav");
            AudioClip[] clips = new AudioClip[files.Length];
            for (int i = 0; i < files.Length; i++)
            {
                string assetPath = files[i].Replace('\\', '/');
                clips[i] = AssetDatabase.LoadAssetAtPath<AudioClip>(assetPath);
            }
            return clips;
        }

        private static void SetObjectRef(SerializedObject so, string propertyName, Object value)
        {
            SerializedProperty prop = so.FindProperty(propertyName);
            if (prop == null)
            {
                Debug.LogWarning($"[CueStrikeVoiceBinderEditor] Property not found: {propertyName}");
                return;
            }
            prop.objectReferenceValue = value;
        }

        private static void SetClipArray(SerializedObject so, string propertyName, AudioClip[] clips)
        {
            SerializedProperty prop = so.FindProperty(propertyName);
            if (prop == null)
            {
                Debug.LogWarning($"[CueStrikeVoiceBinderEditor] Property not found: {propertyName}");
                return;
            }

            prop.arraySize = clips.Length;
            for (int i = 0; i < clips.Length; i++)
            {
                prop.GetArrayElementAtIndex(i).objectReferenceValue = clips[i];
            }
        }
    }
}
