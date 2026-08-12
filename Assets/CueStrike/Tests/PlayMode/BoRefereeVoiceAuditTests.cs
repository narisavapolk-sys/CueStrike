using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace CueStrike.Tests.PlayMode
{
    /// <summary>
    /// Permanent R40/R42 audit: Bo's voice clips and referee event path work in AAA_RoomDAY.
    /// Runtime types are resolved by reflection because the gameplay assembly is not auto-referenced by tests.
    /// </summary>
    public class BoRefereeVoiceAuditTests
    {
        private Scene _scene;
        private Type _boType;
        private Type _bridgeType;

        [UnityTest]
        public IEnumerator AAA_BoReferee_HasAudioSource_Bridge_And14Clips()
        {
            yield return LoadAAA();
            ResolveTypes();
            Component bo = FindInScene(_boType);
            Component bridge = FindInScene(_bridgeType);
            Assert.IsNotNull(bo, "BoReferee must exist in AAA_RoomDAY.");
            Assert.IsNotNull(bridge, "BoRefereeEventBridge must exist in AAA_RoomDAY.");
            Assert.IsNotNull(GetPrivate<AudioSource>(bo, "_audioSource"), "Bo AudioSource must be assigned.");
            int clips = CountClips(bo);
            Debug.Log($"[Bo Voice Audit] AAA wiring: clips={clips} audioSource=true bridge=true");
            Assert.GreaterOrEqual(clips, 14, "Bo must have at least 14 voice clips assigned.");
            Assert.IsNotNull(GetPrivate<object>(bridge, "_referee"), "Bridge must wire its BoReferee.");
            yield return UnloadAAA();
        }

        [UnityTest]
        public IEnumerator AAA_BoReferee_AnnouncesMatchStartFoulAndPot()
        {
            yield return LoadAAA();
            ResolveTypes();
            Component bo = FindInScene(_boType);
            Component bridge = FindInScene(_bridgeType);
            Assert.IsNotNull(bo); Assert.IsNotNull(bridge);
            yield return new WaitForSeconds(1f);
            Assert.IsTrue(GetPrivate<bool>(bridge, "_subscribedCp"), "Bridge must subscribe to Chinese Pool events.");

            Invoke(bo, "OnMatchStart");
            yield return null;
            float start = GetPrivate<float>(bo, "_lastAnnouncementTime");
            bool speakState = HasAnimatorState(bo, "Speak");
            Debug.Log($"[Bo Voice Audit] MATCH_START announced={start > -10f} animationSpeak={speakState} time={start:F3}");
            Assert.Greater(start, -10f);

            yield return new WaitForSeconds(3.2f);
            Type foulType = _boType.GetNestedType("FoulType");
            Invoke(bo, "OnFoulCommitted", Enum.Parse(foulType, "Generic"), 0, 4);
            yield return null;
            float foul = GetPrivate<float>(bo, "_lastAnnouncementTime");
            bool disappointedState = HasAnimatorState(bo, "Disappointed");
            Debug.Log($"[Bo Voice Audit] FOUL announced={foul > start} animationDisappointed={disappointedState} time={foul:F3}");
            Assert.Greater(foul, start);

            yield return new WaitForSeconds(3.2f);
            Invoke(bo, "OnBallPotted", 0, 4, 1);
            yield return null;
            float pot = GetPrivate<float>(bo, "_lastAnnouncementTime");
            bool potState = HasAnimatorState(bo, "Speak") || HasAnimatorState(bo, "Celebrate");
            Debug.Log($"[Bo Voice Audit] BALL_POTTED announced={pot > foul} animationSpeakOrCelebrate={potState} time={pot:F3}");
            Assert.Greater(pot, foul);
            Debug.Log("[Bo Voice Audit] PASS — Bo announces match start, foul, and ball potted with animation paths.");
            yield return UnloadAAA();
        }

        private IEnumerator LoadAAA()
        {
            var load = SceneManager.LoadSceneAsync("AAA_RoomDAY", LoadSceneMode.Additive);
            Assert.IsNotNull(load); yield return load;
            _scene = SceneManager.GetSceneByName("AAA_RoomDAY");
            Assert.IsTrue(_scene.IsValid() && _scene.isLoaded);
            SceneManager.SetActiveScene(_scene);
            yield return new WaitForSeconds(1f);
        }

        private IEnumerator UnloadAAA() { if (_scene.IsValid() && _scene.isLoaded) yield return SceneManager.UnloadSceneAsync(_scene); }
        private void ResolveTypes() { _boType = RuntimeType("CueStrike.MascotSystem.BoReferee"); _bridgeType = RuntimeType("CueStrike.MascotSystem.BoRefereeEventBridge"); Assert.IsNotNull(_boType); Assert.IsNotNull(_bridgeType); }
        private Component FindInScene(Type type) { foreach (var c in UnityEngine.Object.FindObjectsByType<Component>(FindObjectsInactive.Include)) if (c != null && c.GetType() == type && c.gameObject.scene == _scene) return c; return null; }
        private static Type RuntimeType(string name) { foreach (var a in AppDomain.CurrentDomain.GetAssemblies()) { var t = a.GetType(name); if (t != null) return t; } return null; }
        private static object Invoke(object instance, string name, params object[] args) { var m = instance.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic); Assert.IsNotNull(m, $"Missing {name}"); return m.Invoke(instance, args); }
        private static T GetPrivate<T>(object instance, string name) { var f = FindField(instance.GetType(), name); Assert.IsNotNull(f, $"Missing {name}"); return (T)f.GetValue(instance); }
        private static FieldInfo FindField(Type t, string name) { while (t != null) { var f = t.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic); if (f != null) return f; t = t.BaseType; } return null; }
        private static int CountClips(object bo) { int total = 0; foreach (var f in bo.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)) if (f.FieldType == typeof(AudioClip[])) { var clips = (AudioClip[])f.GetValue(bo); if (clips != null) foreach (var clip in clips) if (clip != null) total++; } return total; }
        private static bool HasAnimatorState(object bo, string name) { var animator = GetPrivate<Animator>(bo, "_animator"); return animator != null && (animator.GetCurrentAnimatorStateInfo(0).IsName(name) || animator.IsInTransition(0)); }
    }
}
