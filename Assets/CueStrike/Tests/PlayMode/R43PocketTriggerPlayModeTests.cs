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
    /// R43 regression test: a real Rigidbody enters a Pocket trigger and the
    /// tracker event is raised before Pocket deactivates the ball.
    /// </summary>
    public class R43PocketTriggerPlayModeTests
    {
        private Scene _scene;
        private GameObject _pocketObject;
        private GameObject _ballObject;

        [UnityTest]
        public IEnumerator BallEnteringPocket_InvokesBallPottedTrackerEvent()
        {
            var load = SceneManager.LoadSceneAsync("AAA_RoomDAY", LoadSceneMode.Additive);
            Assert.IsNotNull(load);
            yield return load;
            _scene = SceneManager.GetSceneByName("AAA_RoomDAY");
            SceneManager.SetActiveScene(_scene);
            yield return new WaitForSeconds(1f);

            Type trackerType = RuntimeType("CueStrike.Gameplay.BallPottedTracker");
            Type pocketType = RuntimeType("Pocket");
            Type identityType = RuntimeType("CueStrike.BallIdentity");
            Assert.IsNotNull(trackerType, "BallPottedTracker type must be available.");
            Assert.IsNotNull(pocketType, "Pocket type must be available.");
            Assert.IsNotNull(identityType, "BallIdentity type must be available.");

            Component tracker = FindInScene(trackerType);
            Assert.IsNotNull(tracker, "AAA_RoomDAY must contain BallPottedTracker.");
            Invoke(tracker, "StartTracking");

            bool eventRaised = false;
            int eventBall = -1;
            var eventInfo = trackerType.GetEvent("OnBallPotted", BindingFlags.Instance | BindingFlags.Public);
            var handler = new Action<int, int>((ball, player) =>
            {
                eventRaised = true;
                eventBall = ball;
                Debug.Log($"[R43 Pocket Test] OnBallPotted ball={ball} player={player}");
            });
            eventInfo.AddEventHandler(tracker, handler);

            _pocketObject = new GameObject("R43_TestPocket");
            _pocketObject.transform.position = Vector3.zero;
            _pocketObject.AddComponent<SphereCollider>().radius = 0.45f;
            _pocketObject.AddComponent(pocketType);

            _ballObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            _ballObject.name = "R43_TestBall_1";
            _ballObject.tag = "Ball";
            _ballObject.transform.position = new Vector3(0f, 0f, -1.5f);
            _ballObject.transform.localScale = Vector3.one * 0.2f;
            var rb = _ballObject.AddComponent<Rigidbody>();
            rb.useGravity = false;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            var identity = _ballObject.AddComponent(identityType);
            SetField(identity, "ballId", 1);

            Debug.Log("[R43 Pocket Test] Firing Rigidbody ball toward Pocket trigger.");
            yield return new WaitForFixedUpdate();
            rb.AddForce(Vector3.forward * 100f, ForceMode.Impulse);
            yield return new WaitForSeconds(1.0f);

            Debug.Log($"[R43 Pocket Test] RESULT eventRaised={eventRaised} eventBall={eventBall} ballActive={_ballObject.activeSelf}");
            Assert.IsTrue(eventRaised, "BallPottedTracker.OnBallPotted must fire when a ball enters Pocket.");
            Assert.AreEqual(1, eventBall);
            Assert.IsFalse(_ballObject.activeSelf, "Pocket should deactivate the potted ball after notifying tracker.");

            eventInfo.RemoveEventHandler(tracker, handler);
            yield return SceneManager.UnloadSceneAsync(_scene);
        }

        private Component FindInScene(Type type)
        {
            foreach (var c in UnityEngine.Object.FindObjectsByType<Component>(FindObjectsInactive.Include))
                if (c != null && c.GetType() == type && c.gameObject.scene == _scene) return c;
            return null;
        }

        private static Type RuntimeType(string fullName)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var type = assembly.GetType(fullName);
                if (type != null) return type;
            }
            return null;
        }

        private static object Invoke(object instance, string methodName, params object[] args)
        {
            var method = instance.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.IsNotNull(method, $"Missing method {methodName}.");
            return method.Invoke(instance, args);
        }

        private static void SetField(object instance, string fieldName, object value)
        {
            var field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"Missing field {fieldName}.");
            field.SetValue(instance, value);
        }
    }
}
