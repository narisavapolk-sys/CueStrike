// 🧪 AudioAssetConsistencyTests — Anti-Safe-Mode guard: verifies the 14 voice wavs and
// every SFX slot/field the binder wires actually exist (reflects real types, never guesses).
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using CueStrike.Audio;
using CueStrike.MascotSystem;

namespace CueStrike.Tests.Editor
{
    public class AudioAssetConsistencyTests
    {
        private static string VoiceDir => Path.Combine(Application.dataPath, "CueStrike/Audio/Clips/Voice/UncleNok");
        private static string SfxDir   => Path.Combine(Application.dataPath, "CueStrike/Audio/Clips");

        // The 14 Zira wavs the binder writes into UncleNokReferee (from CueStrikeVoiceAndSfxBinder.VoiceMap).
        private static readonly string[] VoiceFiles =
        {
            "match_start_01.wav", "match_start_02.wav",
            "turn_start_01.wav", "turn_start_02.wav",
            "pot_success_01.wav", "pot_success_02.wav", "pot_success_03.wav",
            "century_break.wav", "high_break.wav",
            "foul_called_01.wav", "foul_called_02.wav",
            "foul_cueball.wav", "break_shot.wav", "clearance.wav",
        };

        // SFX slot -> wav the binder maps (CueStrikeVoiceAndSfxBinder.SfxMap).
        private static readonly Dictionary<string, string> SfxMap = new()
        {
            { "hitSoft",         "ball_ball_hit.wav" },
            { "hitMedium",       "cue_ball_hit.wav" },
            { "hitHard",         "ball_ball_hit.wav" },
            { "cushionHit",      "ball_cushion_hit.wav" },
            { "pocketHit",       "ball_pocket_drop.wav" },
            { "chalkDust",       "chalk_scrape.wav" },
            { "menuClick",       "ui_click.wav" },
            { "menuHover",       "ui_hover.wav" },
            { "ambientRoom",     "ambient_room_tone.wav" },
            { "miscued",         "ball_ball_hit.wav" },
            { "whooshShot",      "ball_cushion_hit.wav" },
            { "pocketRollClip",  "ball_pocket_drop.wav" },
        };

        [Test]
        public void Voice_All14WavFilesExist()
        {
            Assert.IsTrue(Directory.Exists(VoiceDir), "Voice dir missing: " + VoiceDir);
            foreach (string f in VoiceFiles)
                Assert.IsTrue(File.Exists(Path.Combine(VoiceDir, f)), "missing voice wav: " + f);
            Assert.AreEqual(14, VoiceFiles.Length, "expected exactly 14 Zira voice clips");
        }

        [Test]
        public void Sfx_MappedWavFilesExist()
        {
            Assert.IsTrue(Directory.Exists(SfxDir), "SFX dir missing: " + SfxDir);
            foreach (string f in SfxMap.Values.Distinct())
                Assert.IsTrue(File.Exists(Path.Combine(SfxDir, f)), "missing sfx wav: " + f);
        }

        [Test]
        public void AudioManager_HasEverySfxSlot_AsAudioClipField()
        {
            var fields = typeof(CueStrikeAudioManager)
                .GetFields(BindingFlags.Public | BindingFlags.Instance)
                .Where(fi => fi.FieldType == typeof(AudioClip))
                .Select(fi => fi.Name)
                .ToHashSet();

            foreach (string slot in SfxMap.Keys)
                Assert.IsTrue(fields.Contains(slot), "AudioManager missing AudioClip field: " + slot);
        }

        [Test]
        public void UncleNokReferee_HasAllVoiceClipArrays()
        {
            var arrays = typeof(UncleNokReferee)
                .GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(fi => fi.FieldType == typeof(AudioClip[]))
                .Select(fi => fi.Name)
                .ToHashSet();

            string[] expected =
            {
                "_matchStartClips", "_playerTurnStartClips", "_potSuccessClips",
                "_centuryBreakClips", "_highBreakClips", "_foulCalledClips",
                "_foulCueBallPottedClips", "_breakClips", "_clearanceClips",
            };
            foreach (string f in expected)
                Assert.IsTrue(arrays.Contains(f), "UncleNokReferee missing AudioClip[] field: " + f);
        }
    }
}
