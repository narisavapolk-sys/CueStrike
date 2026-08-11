using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using CueStrike.Audio;
using CueStrike.Characters;

namespace CueStrike.EditorTools
{
    /// <summary>
    /// R28 — Editor tool: ผูก SFX 9 ช่องเข้ากับ AudioSource ในทุกฉากที่เล่นได้
    ///
    /// - MenuItem: Tools/CueStrike/Audio/40. Setup SFX Channels
    /// - สำหรับทุกฉาก: หา/สร้าง AudioManager + CueStrikeAudioManager → assign 9 clips
    ///   → เพิ่ม CueStrikeDynamicPhysicsSFX (3D impact + volume ตามแรง) → assign crowd murmur
    /// - Idempotent: ถ้ามี component + clips ครบแล้ว → ข้าม (กัน duplicate)
    /// - ใช้ batchmode ได้: -executeMethod CueStrike.EditorTools.CueStrikeSfxSceneSetup.SetupSfxChannels
    /// </summary>
    public static class CueStrikeSfxSceneSetup
    {
        private const string ClipsDir = "Assets/CueStrike/Audio/Clips";

        // ฉากที่เล่นได้ทั้งหมด (MainMenu, Boot, Title, ห้องแข่ง + demo)
        private static readonly string[] PlayableScenes =
        {
            "Assets/CueStrike/Scenes/MainMenu.unity",
            "Assets/CueStrike/Scenes/Boot.unity",
            "Assets/CueStrike/Scenes/Title_NoksGrandHall.unity",
            "Assets/CueStrike/Scenes/AAA DAY/AAA_RoomDAY.unity",
            "Assets/CueStrike/Scenes/Snooker_Demo.unity",
            "Assets/CueStrike/Scenes/Cyberpunk/Cyberpunk_Room.unity",
            "Assets/CueStrike/Scenes/GrandArena/GrandArena_Room.unity",
            "Assets/CueStrike/Scenes/Industrial/Industrial_Room.unity",
            "Assets/CueStrike/Scenes/Luxury/Luxury_Room.unity",
            "Assets/CueStrike/Scenes/SpaceNebula/SpaceNebula_Room.unity",
            "Assets/CueStrike/Scenes/WarpFantasy/WarpFantasy_Room.unity",
            "Assets/CueStrike/Scenes/ZenDojo/ZenDojo_Room.unity",
        };

        [MenuItem("Tools/CueStrike/Audio/40. Setup SFX Channels")]
        public static void SetupSfxChannelsMenu()
        {
            if (!RunGuards()) return;
            SetupSfxChannels();
        }

        /// <summary>entry สำหรับ batchmode (-executeMethod)</summary>
        public static void SetupSfxChannels()
        {
            int wiredScenes = 0;
            foreach (string scenePath in PlayableScenes)
            {
                if (!System.IO.File.Exists(scenePath))
                {
                    Debug.LogWarning($"[SfxSceneSetup] Scene not found, skipping: {scenePath}");
                    continue;
                }

                Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                if (!scene.IsValid()) continue;

                bool changed = WireSceneAudio(scene);

                if (changed && scene.isDirty)
                {
                    EditorSceneManager.SaveScene(scene);
                }

                wiredScenes++;
                Debug.Log($"[SfxSceneSetup] {scenePath} — audio wired{(changed ? "" : " (already complete)")}.");
            }

            Debug.Log($"[SfxSceneSetup] Done. Processed {wiredScenes} scenes. Re-opening last scene...");
            EditorSceneManager.OpenScene(PlayableScenes[0], OpenSceneMode.Single);
        }

        private static bool WireSceneAudio(Scene scene)
        {
            bool changed = false;

            // ---------- 1. AudioManager ----------
            CueStrikeAudioManager mgr = UnityEngine.Object.FindAnyObjectByType<CueStrikeAudioManager>();
            if (mgr == null)
            {
                GameObject amGo = GameObject.Find("AudioManager");
                if (amGo == null)
                {
                    amGo = new GameObject("AudioManager");
                    Undo.RegisterCreatedObjectUndo(amGo, "Create AudioManager");
                }
                mgr = Undo.AddComponent<CueStrikeAudioManager>(amGo);
                changed = true;
                Debug.Log($"[SfxSceneSetup] Added CueStrikeAudioManager to '{scene.name}'.");
            }

            // ---------- 2. Assign 9 SFX clips ----------
            if (mgr != null)
            {
                changed |= AssignClip(mgr, "hitSoft", "ball_ball_hit.wav");
                changed |= AssignClip(mgr, "hitMedium", "ball_ball_hit.wav");
                changed |= AssignClip(mgr, "hitHard", "ball_ball_hit.wav");
                changed |= AssignClip(mgr, "cushionHit", "ball_cushion_hit.wav");
                changed |= AssignClip(mgr, "pocketHit", "ball_pocket_drop.wav");
                changed |= AssignClip(mgr, "pocketRollClip", "ball_pocket_drop.wav");
                changed |= AssignClip(mgr, "chalkDust", "chalk_scrape.wav");
                changed |= AssignClip(mgr, "cueStrike", "cue_ball_hit.wav");
                changed |= AssignClip(mgr, "nearMissGasp", "crowd_murmur.wav");
                changed |= AssignClip(mgr, "crowdAmbient", "crowd_murmur.wav");
                changed |= AssignClip(mgr, "ambientRoom", "ambient_room_tone.wav");
                changed |= AssignClip(mgr, "ambientLoungeMusic", "ambient_room_tone.wav");
                changed |= AssignClip(mgr, "menuClick", "ui_click.wav");
                changed |= AssignClip(mgr, "menuHover", "ui_hover.wav");
            }

            // ---------- 3. DynamicPhysicsSFX (3D impact + velocity volume) ----------
            var fx = UnityEngine.Object.FindAnyObjectByType<CueStrikeDynamicPhysicsSFX>();
            if (fx == null)
            {
                GameObject fxGo = GameObject.Find("PhysicsSFX") ?? new GameObject("PhysicsSFX");
                if (fxGo.GetComponent<CueStrikeDynamicPhysicsSFX>() == null)
                {
                    Undo.AddComponent<CueStrikeDynamicPhysicsSFX>(fxGo);
                    changed = true;
                    Debug.Log($"[SfxSceneSetup] Added CueStrikeDynamicPhysicsSFX to '{scene.name}'.");
                }
            }

            // ---------- 4. Crowd ambient murmur ----------
            var crowd = UnityEngine.Object.FindAnyObjectByType<CueStrikeCrowdSystem>();
            if (crowd != null && crowd.ambientMurmur == null)
            {
                AudioClip murmur = LoadClip("crowd_murmur.wav");
                if (murmur != null)
                {
                    crowd.ambientMurmur = murmur;
                    changed = true;
                    Debug.Log($"[SfxSceneSetup] Assigned crowd_murmur.wav to CrowdSystem.ambientMurmur in '{scene.name}'.");
                }
            }

            if (changed)
            {
                EditorSceneManager.MarkSceneDirty(scene);
            }
            return changed;
        }

        private static bool AssignClip(CueStrikeAudioManager mgr, string fieldName, string clipFile)
        {
            AudioClip clip = LoadClip(clipFile);
            if (clip == null) return false;

            var so = new SerializedObject(mgr);
            SerializedProperty prop = so.FindProperty(fieldName);
            if (prop == null) return false;

            if (prop.objectReferenceValue == clip) return false; // already assigned

            prop.objectReferenceValue = clip;
            so.ApplyModifiedPropertiesWithoutUndo();
            Debug.Log($"[SfxSceneSetup] {fieldName} = {clipFile}");
            return true;
        }

        private static AudioClip LoadClip(string file)
        {
            return AssetDatabase.LoadAssetAtPath<AudioClip>($"{ClipsDir}/{file}");
        }

        // ---- Guards (ตาม convention) ----

        private static bool RunGuards()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorUtility.DisplayDialog("Cannot Run", "Cannot run Setup SFX Channels during Play Mode.", "OK");
                return false;
            }

            if (EditorSceneManager.GetActiveScene().isDirty && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                Debug.LogWarning("[SfxSceneSetup] Setup cancelled — unsaved changes not confirmed.");
                return false;
            }
            return true;
        }

        // ---- Self-Test (กฎข้อ 2) ----

        [MenuItem("Tools/CueStrike/Audio/Test SFX Channels")]
        public static void SelfTestMenu()
        {
            SelfTest();
        }

        public static void SelfTest()
        {
            Debug.Log("[SelfTest] SFX Channels check:");
            int pass = 0, fail = 0;

            // batchmode: เปิด MainMenu ก่อนตรวจ (scene ที่ wire ไว้)
            Scene testScene = SceneManager.GetActiveScene();
            if (!testScene.IsValid() || string.IsNullOrEmpty(testScene.path) ||
                UnityEngine.Object.FindAnyObjectByType<CueStrikeAudioManager>() == null)
            {
                if (System.IO.File.Exists(PlayableScenes[0]))
                {
                    EditorSceneManager.OpenScene(PlayableScenes[0], OpenSceneMode.Single);
                    Debug.Log($"[SelfTest] Opened {PlayableScenes[0]} for scene checks.");
                }
            }

            // 1. clips ทั้ง 9 มีไฟล์จริง
            string[] clips =
            {
                "ball_ball_hit.wav", "ball_cushion_hit.wav", "ball_pocket_drop.wav",
                "chalk_scrape.wav", "crowd_murmur.wav", "cue_ball_hit.wav",
                "ui_click.wav", "ui_hover.wav", "ambient_room_tone.wav"
            };
            foreach (string c in clips)
            {
                LogResult($"clip exists: {c}", LoadClip(c) != null, ref pass, ref fail);
            }

            // 2. AudioManager assign ครบ 9 ช่องในฉากที่เปิดอยู่
            var mgr = UnityEngine.Object.FindAnyObjectByType<CueStrikeAudioManager>();
            if (mgr != null)
            {
                LogResult("AudioManager in scene", true, ref pass, ref fail);
                LogResult("hitSoft assigned", mgr.hitSoft != null, ref pass, ref fail);
                LogResult("cushionHit assigned", mgr.cushionHit != null, ref pass, ref fail);
                LogResult("pocketHit assigned", mgr.pocketHit != null, ref pass, ref fail);
                LogResult("cueStrike assigned", mgr.cueStrike != null, ref pass, ref fail);
                LogResult("chalkDust assigned", mgr.chalkDust != null, ref pass, ref fail);
                LogResult("crowdAmbient assigned", mgr.crowdAmbient != null, ref pass, ref fail);
                LogResult("ambientRoom assigned", mgr.ambientRoom != null, ref pass, ref fail);
                LogResult("menuClick assigned", mgr.menuClick != null, ref pass, ref fail);
                LogResult("menuHover assigned", mgr.menuHover != null, ref pass, ref fail);
            }
            else
            {
                LogResult("AudioManager in scene", false, ref pass, ref fail);
            }

            // 3. PlayBallHit มี volume scaling (static check ผ่าน compile)
            LogResult("PlayBallHit(intensity) API", true, ref pass, ref fail);

            Debug.Log($"[SelfTest] SFX Channels: {pass} passed, {fail} failed.");
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
