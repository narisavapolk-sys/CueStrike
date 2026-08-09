// 🎵 CueStrikeVoiceAndSfxBinder — Editor tool (reusable, Undo supported).
// Binds the EXISTING .wav clips only (no AI credits):
//   * 14 Zira (en-US) voice clips -> UncleNokReferee.AudioClip[] arrays (on UncleNok_Prefab)
//   * 9 SFX placeholder clips      -> CueStrikeAudioManager public fields (on scene instance)
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using CueStrike.MascotSystem;   // UncleNokReferee
using CueStrike.Audio;          // CueStrikeAudioManager

public static class CueStrikeVoiceAndSfxBinder
{
    private const string VDIR = "Assets/CueStrike/Audio/Clips/Voice/UncleNok";
    private const string SDIR = "Assets/CueStrike/Audio/Clips";

    // UncleNokReferee serialized AudioClip[] field -> the 14 Zira wav filenames
    private static readonly Dictionary<string, string[]> VoiceMap = new()
    {
        { "_matchStartClips",        new[] { "match_start_01.wav", "match_start_02.wav" } },
        { "_playerTurnStartClips",   new[] { "turn_start_01.wav", "turn_start_02.wav" } },
        { "_potSuccessClips",        new[] { "pot_success_01.wav", "pot_success_02.wav", "pot_success_03.wav" } },
        { "_centuryBreakClips",      new[] { "century_break.wav" } },
        { "_highBreakClips",         new[] { "high_break.wav" } },
        { "_foulCalledClips",        new[] { "foul_called_01.wav", "foul_called_02.wav" } },
        { "_foulCueBallPottedClips", new[] { "foul_cueball.wav" } },
        { "_breakClips",             new[] { "break_shot.wav" } },
        { "_clearanceClips",         new[] { "clearance.wav" } },
    };

        // CueStrikeAudioManager public field -> existing SFX placeholder wav (Option C: 0 AI credits).
    // Maps all clip slots to the 9 existing placeholder wavs generated in Phase A.
    private static readonly Dictionary<string, string> SfxMap = new()
    {
        { "hitSoft",     "ball_ball_hit.wav" },
        { "hitMedium",   "cue_ball_hit.wav" },
        { "hitHard",     "ball_ball_hit.wav" },        // hard uses same impact wav (placeholder)
        { "cushionHit",  "ball_cushion_hit.wav" },
        { "pocketHit",   "ball_pocket_drop.wav" },
        { "chalkDust",   "chalk_scrape.wav" },
        { "menuClick",   "ui_click.wav" },
        { "menuHover",   "ui_hover.wav" },
        { "ambientRoom", "ambient_room_tone.wav" },
        { "miscued",     "ball_ball_hit.wav" },        // miscue shares impact placeholder
        { "whooshShot",  "ball_cushion_hit.wav" },     // power-shot whoosh shares placeholder
        { "pocketRollClip", "ball_pocket_drop.wav" },  // rolling track shares drop placeholder
    };

    // ── Voice wiring ─────────────────────────────────────────────────────────────────
    public static void AssignVoiceTo(string nokPrefabPath)
    {
        GameObject src = AssetDatabase.LoadAssetAtPath<GameObject>(nokPrefabPath);
        if (!src) { Debug.LogError("[Binder] UncleNok prefab missing: " + nokPrefabPath); return; }

        // Edit the prefab CONTENTS directly so the UncleNokReferee component + clips
        // are really persisted into the prefab asset (Rule 4A: verifiable in the .prefab YAML).
        GameObject contents = PrefabUtility.LoadPrefabContents(nokPrefabPath);
        if (!contents) { Debug.LogError("[Binder] LoadPrefabContents failed: " + nokPrefabPath); return; }

        try
        {
            UncleNokReferee referee = contents.GetComponentInChildren<UncleNokReferee>(true);
            if (!referee)
            {
                var root = contents.transform.root.gameObject;
                referee = root.AddComponent<UncleNokReferee>();
                Debug.Log("[Binder] Added UncleNokReferee component (prefab root: " + root.name + ")");
            }

            using (var so = new SerializedObject(referee))
            {
                int wired = 0; bool missingField = false; int missingClip = 0;
                foreach (var kv in VoiceMap)
                {
                    SerializedProperty prop = so.FindProperty(kv.Key);
                    if (prop == null) { Debug.LogWarning("[Binder] voice field not found: " + kv.Key);
 missingField = true; continue; }
                var list = new List<AudioClip>();
                foreach (string f in kv.Value)
                {
                    AudioClip c = AssetDatabase.LoadAssetAtPath<AudioClip>(Path.Combine(VDIR, f).Replace('\\', '/'));
                    if (c != null) list.Add(c);
                    else Debug.LogWarning("[Binder] missing wav: " + Path.Combine(VDIR, f));
                }
                prop.arraySize = list.Count;
                for (int i = 0; i < list.Count; i++)
                    prop.GetArrayElementAtIndex(i).objectReferenceValue = list[i];
                wired += list.Count;
            }
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(contents);
            Debug.Log($"[Binder] Voice: {wired}/14 clips wired (missingField={missingField}, missingClip={missingClip})");
            }
            PrefabUtility.SaveAsPrefabAsset(contents, nokPrefabPath);
            Debug.Log("[Binder] Voice persisted to prefab asset: " + nokPrefabPath);
            }
        finally { PrefabUtility.UnloadPrefabContents(contents); }
        }

    // ── SFX placeholder wiring (scene AudioManager) ─────────────────────────────────
    public static void AssignSfxToScene()
    {
                CueStrikeAudioManager am = Object.FindFirstObjectByType<CueStrikeAudioManager>();
        if (!am) { Debug.LogWarning("[Binder] no CueStrikeAudioManager found in active scene; SFX placeholders skipped"); return; }

        Undo.RecordObject(am, "Wire 9 SFX placeholder clips (AudioManager)");
        using (var so = new SerializedObject(am))
        {
            int wired = 0;
            foreach (var kv in SfxMap)
            {
                string p = Path.Combine(SDIR, kv.Value).Replace('\\', '/');
                AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(p);
                if (!clip) { Debug.LogWarning("[Binder] missing sfx wav: " + p); continue; }
                SerializedProperty prop = so.FindProperty(kv.Key);
                if (prop == null) { Debug.LogWarning("[Binder] sfx field not found: " + kv.Key); continue; }
                prop.objectReferenceValue = clip;
                wired++;
            }
                        so.ApplyModifiedProperties();
            EditorUtility.SetDirty(am);
            Debug.Log($"[Binder] SFX: {wired}/{SfxMap.Count} placeholder clips wired to AudioManager on '{am.gameObject.name}'");
        }
    }

    // ── Scene placement helpers ───────────────────────────────────────────────
    /// <summary>Locate or create a CueStrikeAudioManager singleton instance in the active scene.</summary>
    public static CueStrikeAudioManager EnsureAudioManagerInScene(UnityEngine.SceneManagement.Scene scene, string goName = "CueStrikeAudioManager")
    {
        CueStrikeAudioManager am = Object.FindFirstObjectByType<CueStrikeAudioManager>();
        if (am != null) { Debug.Log("[Binder] AudioManager already present in scene: " + am.gameObject.name); return am; }

        GameObject go = new GameObject(goName);
        Undo.RegisterCreatedObjectUndo(go, "Place CueStrikeAudioManager");
                // Move the new GO into the active scene (Unity 6000 API: SceneManager.MoveGameObjectToScene, static)
        if (scene.IsValid()) UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, scene);
        go.hideFlags = HideFlags.None;
        Undo.AddComponent<CueStrikeAudioManager>(go);                // adds + registers as singleton via Awake
        // AudioManager.Awake() auto-adds 2 AudioSources; nothing else needed.
        Debug.Log("[Binder] AudioManager instance created in scene: " + go.name + " (singleton wired via Awake)");
        return go.GetComponent<CueStrikeAudioManager>();
    }

    // ── Coach-facing menu (Editor GUI) ───────────────────────────────────────────
    [MenuItem("Tools/CueStrike/Audio/Assign Voice Clips (UncleNok)")]
    public static void MenuVoice()
    {
        AssignVoiceTo("Assets/CueStrike/Characters/UncleNok/UncleNok_Prefab.prefab");
        AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
    }

    [MenuItem("Tools/CueStrike/Audio/Assign SFX Placeholders")]
    public static void MenuSfx()
    {
        AssignSfxToScene();
        AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
    }
}
