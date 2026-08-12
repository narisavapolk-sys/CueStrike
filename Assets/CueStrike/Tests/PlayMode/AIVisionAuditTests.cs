using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace CueStrike.Tests.PlayMode
{
    /// <summary>
    /// Vision Audit — AI (Chinese Pool Practice) ยิงลูกจริงหรือไม่
    ///
    /// โหลด AAA_RoomDAY จริง → ตรวจ prerequisites (R37: AIModifier + refs, R38: BallSetup)
    /// → ตั้ง Practice + difficulty Expert → StartNewFrame → NextPlayer (AI เทิร์น)
    /// → รอ AI คิด+ยิง → ตรวจว่าลูก cue ขยับจากตำแหน่งเดิม (หลักฐานว่ายิงจริง)
    ///
    /// หลักฐาน console:
    ///   [CueStrike] ChinesePoolGameManager initialized
    ///   [CueStrikeAI] Practice AI difficulty set to Expert
    ///   [CueStrikeAI] Bridge subscribed to GameManager.OnTurnChanged
    ///   [CueStrikeAI] AI shot: ball=... → pocket=..., power=...
    /// </summary>
    public class AIVisionAuditTests
    {
        private static readonly Type GameManagerType = RuntimeType("CueStrike.Gameplay.ChinesePool.ChinesePoolGameManager");
        private static readonly Type BallSetupType = RuntimeType("CueStrike.Gameplay.ChinesePool.ChinesePoolBallSetup");
        private static readonly Type AIModifierType = RuntimeType("CueStrike.Gameplay.ChinesePool.ChinesePoolAIModifier");
        private static readonly Type BridgeType = RuntimeType("CueStrike.AI.CueStrikePracticeAIBridge");
        private static readonly Type SkillLevelType = RuntimeType("CueStrike.AI.SkillLevel");

        private Scene _loadedScene;
        private readonly List<GameObject> _runtimeObjects = new List<GameObject>();

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            AssertRuntimeTypesAvailable();
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (_loadedScene.IsValid() && _loadedScene.isLoaded)
            {
                yield return SceneManager.UnloadSceneAsync(_loadedScene);
                _loadedScene = default;
            }

            for (int i = _runtimeObjects.Count - 1; i >= 0; i--)
            {
                if (_runtimeObjects[i] != null)
                    UnityEngine.Object.Destroy(_runtimeObjects[i]);
            }
            _runtimeObjects.Clear();
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
            yield return null;
        }

        [UnityTest]
        public IEnumerator Audit_AaaRoom_HasAiPrerequisites()
        {
            yield return LoadScene("AAA_RoomDAY");

            // R37: AIModifier + refs
            Component gm = FindInScene(GameManagerType, _loadedScene);
            Assert.IsNotNull(gm, "GameManager must exist in AAA_RoomDAY.");

            object ballSetup = GetField(gm, "ballSetup");
            Assert.IsNotNull(ballSetup, "R38: GameManager.ballSetup must be assigned (ChinesePoolBallSetup).");

            object aiModifier = GetField(gm, "aiModifier");
            Assert.IsNotNull(aiModifier, "R37: GameManager.aiModifier must be assigned (ChinesePoolAIModifier).");

            Component bridge = FindInScene(BridgeType, _loadedScene);
            Assert.IsNotNull(bridge, "CueStrikePracticeAIBridge must exist in AAA_RoomDAY.");

            object bridgeModifier = GetField(bridge, "aiModifier");
            Assert.IsNotNull(bridgeModifier, "R37: bridge.aiModifier must be assigned.");

            Debug.Log("[Audit] ✅ Prerequisites OK — GameManager + BallSetup + AIModifier + bridge wired.");
        }

        [UnityTest]
        public IEnumerator Audit_ExpertAi_TakesTurn_AndShootsCueBall()
        {
            yield return LoadScene("AAA_RoomDAY");

            Component gm = FindInScene(GameManagerType, _loadedScene);
            Assert.IsNotNull(gm, "GameManager must exist.");
            Assert.IsNotNull(GetField(gm, "ballSetup"), "R38: BallSetup required for frame to start.");
            Assert.IsNotNull(GetField(gm, "aiModifier"), "R37: AIModifier required for AI turn.");

            // --- ตั้ง Practice + difficulty Expert ---
            SetField(gm, "isPracticeMode", true);
            SetField(gm, "maxFrames", 0);

            Component bridge = FindInScene(BridgeType, _loadedScene);
            Assert.IsNotNull(bridge, "Bridge must exist.");
            object expert = Enum.Parse(SkillLevelType, "Expert");
            InvokeMethod(bridge, "SetAIDifficulty", expert);

            // --- เริ่มเฟรม + ให้ AI (Player 2) เป็นคนยิง ---
            InvokeMethod(gm, "StartNewFrame");
            yield return new WaitForSeconds(3f); // รอ BallSetup spawn ลูก

            // บันทึกตำแหน่งลูก cue ก่อน
            Component ballSetup = (Component)GetField(gm, "ballSetup");
            GameObject cueBallBefore = GetBall(ballSetup, 0);
            Assert.IsNotNull(cueBallBefore, "Cue ball (id 0) must exist after StartNewFrame.");
            Vector3 cuePosBefore = cueBallBefore.transform.position;

            // AI เทิร์น: NextPlayer → Player 1 → OnTurnChanged(1) → bridge เริ่มยิง
            InvokeMethod(gm, "NextPlayer");
            Debug.Log("[Audit] NextPlayer called — waiting for AI to think and shoot...");

            // รอ AI: decisionDelay + aim + impulse + settle (สูงสุด ~20s)
            bool sawAiShot = false;
            bool cueMoved = false;
            Vector3 cuePosAfter = cuePosBefore;
            float elapsed = 0f;
            const float maxWait = 25f;

            while (elapsed < maxWait)
            {
                yield return new WaitForSeconds(0.5f);
                elapsed += 0.5f;

                GameObject cueBallNow = GetBall(ballSetup, 0);
                if (cueBallNow != null)
                {
                    cuePosAfter = cueBallNow.transform.position;
                    if (Vector3.Distance(cuePosAfter, cuePosBefore) > 0.05f)
                    {
                        cueMoved = true;
                    }
                }

                if (elapsed > 8f && cueMoved)
                {
                    sawAiShot = true;
                    break;
                }
            }

            Debug.Log($"[Audit] RESULT: cueMoved={cueMoved} distance={(cuePosAfter - cuePosBefore).magnitude:F3} " +
                      $"before={cuePosBefore} after={cuePosAfter}");

            Assert.IsTrue(cueMoved,
                "AI must shoot the cue ball — cue ball position must change after NextPlayer to Player 2 (AI). " +
                "If false, check console for [CueStrikeAI] warnings (modifier/ballSetup missing, no valid shot, etc).");
        }

        // ================= helpers =================

        private Component FindInScene(Type componentType, Scene scene)
        {
            Component[] all = UnityEngine.Object.FindObjectsByType<Component>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (Component component in all)
            {
                if (component != null && component.GetType() == componentType && component.gameObject.scene == scene)
                    return component;
            }
            return null;
        }

        private GameObject GetBall(Component ballSetup, int id)
        {
            MethodInfo getter = ballSetup.GetType().GetMethod("GetBallById");
            if (getter == null) return null;
            return getter.Invoke(ballSetup, new object[] { id }) as GameObject;
        }

        private IEnumerator LoadScene(string sceneName)
        {
            AsyncOperation load = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            Assert.IsNotNull(load, $"Scene '{sceneName}' is not available to PlayMode tests.");
            yield return load;

            _loadedScene = SceneManager.GetSceneByName(sceneName);
            Assert.IsTrue(_loadedScene.IsValid() && _loadedScene.isLoaded, $"Failed to load '{sceneName}'.");

            Scene active = SceneManager.GetActiveScene();
            SceneManager.SetActiveScene(_loadedScene);
            yield return new WaitForSeconds(1f); // ให้ Start() ทั้งหมดรัน
        }

        // ================= reflection helpers (ลอกจาก R14R16R17PlayModeTests) =================

        private static Type RuntimeType(string fullName)
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type type = assembly.GetType(fullName);
                if (type != null) return type;
            }
            return null;
        }

        private static void AssertRuntimeTypesAvailable()
        {
            Assert.IsNotNull(GameManagerType, "ChinesePoolGameManager type missing.");
            Assert.IsNotNull(BallSetupType, "ChinesePoolBallSetup type missing.");
            Assert.IsNotNull(AIModifierType, "ChinesePoolAIModifier type missing.");
            Assert.IsNotNull(BridgeType, "CueStrikePracticeAIBridge type missing.");
            Assert.IsNotNull(SkillLevelType, "SkillLevel type missing.");
        }

        private static object GetField(object instance, string fieldName)
        {
            FieldInfo field = FindField(instance.GetType(), fieldName);
            Assert.IsNotNull(field, $"Field '{fieldName}' not found on {instance.GetType().Name}.");
            return field.GetValue(instance);
        }

        private static void SetField(object instance, string fieldName, object value)
        {
            FieldInfo field = FindField(instance.GetType(), fieldName);
            Assert.IsNotNull(field, $"Field '{fieldName}' not found on {instance.GetType().Name}.");
            field.SetValue(instance, value);
        }

        private static object InvokeMethod(object instance, string methodName, params object[] args)
        {
            MethodInfo method = instance.GetType().GetMethod(methodName,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(method, $"Method '{methodName}' not found on {instance.GetType().Name}.");
            return method.Invoke(instance, args);
        }

        private static FieldInfo FindField(Type type, string name)
        {
            Type current = type;
            while (current != null)
            {
                FieldInfo field = current.GetField(name,
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (field != null) return field;
                current = current.BaseType;
            }
            return null;
        }
    }
}
