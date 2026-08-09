using UnityEngine;
using UnityEditor;
using System.IO;

namespace CueStrike.EditorTools
{
    public class CueStrikeVoiceBinder : EditorWindow
    {
        private const string UncleNokVoiceDir = "Assets/CueStrike/Audio/Clips/Voice/UncleNok";
        private const string NongBoVoiceDir = "Assets/CueStrike/Audio/Clips/Voice/NongBo";

        [MenuItem("CueStrike/Audio/Bind Voice Clips to Prefabs")]
        public static void BindVoiceClips()
        {
            BindUncleNokVoice();
            BindNongBoVoice();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[CueStrikeVoiceBinder] Voice clips bound successfully!");
        }

        private static void BindUncleNokVoice()
        {
            string[] guids = AssetDatabase.FindAssets("t:Prefab UncleNokReferee");
            if (guids.Length == 0)
            {
                Debug.LogWarning("[CueStrikeVoiceBinder] UncleNokReferee prefab not found!");
                return;
            }

            string prefabPath = AssetDatabase.GUIDToAssetPath(guids[0]);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                Debug.LogWarning("[CueStrikeVoiceBinder] Failed to load UncleNokReferee prefab!");
                return;
            }

            var referee = prefab.GetComponent<CueStrike.MascotSystem.UncleNokReferee>();
            if (referee == null)
            {
                Debug.LogWarning("[CueStrikeVoiceBinder] UncleNokReferee component not found!");
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
            Debug.Log($"[CueStrikeVoiceBinder] UncleNok voice bound: {matchStart.Length + turnStart.Length + potSuccess.Length + centuryBreak.Length + highBreak.Length + foulCalled.Length + foulCueBall.Length + breakShot.Length + clearance.Length} clips");
        }

        private static void BindNongBoVoice()
        {
            GameObject nongBo = GameObject.Find("NongBo");
            if (nongBo == null)
            {
                Debug.LogWarning("[CueStrikeVoiceBinder] NongBo GameObject not found in scene!");
                return;
            }

            AudioClip[] clips = LoadClips(NongBoVoiceDir, "bo_");
            Debug.Log($"[CueStrikeVoiceBinder] NongBo voice loaded: {clips.Length} clips (needs NongBoReferee script to bind)");
        }

        private static AudioClip[] LoadClips(string dir, string prefix)
        {
            if (!Directory.Exists(dir))
            {
                Debug.LogWarning($"[CueStrikeVoiceBinder] Directory not found: {dir}");
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

        private static void SetClipArray(SerializedObject so, string propertyName, AudioClip[] clips)
        {
            SerializedProperty prop = so.FindProperty(propertyName);
            if (prop == null)
            {
                Debug.LogWarning($"[CueStrikeVoiceBinder] Property not found: {propertyName}");
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
