using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace CueStrike.Tests.PlayMode
{
    /// <summary>
    /// Runtime coverage for the three manual-audit paths delivered in R14-R17.
    /// Production types are resolved by name so this test assembly does not force
    /// a new assembly-definition boundary onto the existing project runtime.
    /// The tests still instantiate and exercise the real MonoBehaviours and scenes.
    /// </summary>
    public class R14R16R17PlayModeTests
    {
        private static readonly Type GameManagerType = RuntimeType("CueStrike.Gameplay.ChinesePool.ChinesePoolGameManager");
        private static readonly Type CallShotUIType = RuntimeType("CueStrike.UI.ChinesePool.ChinesePoolCallShotUI");
        private static readonly Type UIManagerType = RuntimeType("CueStrike.UI.ChinesePool.ChinesePoolUIManager");
        private static readonly Type AIModifierType = RuntimeType("CueStrike.Gameplay.ChinesePool.ChinesePoolAIModifier");
        private static readonly Type MatchStateType = RuntimeType("CueStrike.Gameplay.ChinesePool.ChinesePoolMatchState");
        private static readonly Type VRStartupType = RuntimeType("VRStartup");

        private readonly List<GameObject> _runtimeObjects = new List<GameObject>();
        private Scene _loadedScene;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            AssertRuntimeTypesAvailable();
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
            yield return null;
        }

        [UnityTest]
        public IEnumerator R14_HumanTurn_ShowsCallShot_ConfirmStoresBallAndPocket()
        {
            CallShotGraph graph = CreateCallShotGraph();
            yield return null; // Allow Start() to subscribe the event handlers.

            SetField(graph.Manager, "currentPhase", Enum.Parse(MatchStateType, "Playing"));
            SetField(graph.Manager, "callShotRequired", true);
            // NextPlayer toggles the index; start at Player 2 so the human Player 1
            // (RED) receives the turn and Ball_3 remains selectable.
            SetField(graph.Manager, "currentPlayerIndex", 1);
            SetField(graph.Manager, "player1Group", Enum.Parse(GetFieldType(graph.Manager, "player1Group"), "Red"));
            SetField(graph.Manager, "player2Group", Enum.Parse(GetFieldType(graph.Manager, "player2Group"), "Yellow"));
            SetField(graph.Manager, "isAiTurn", false);
            graph.Panel.SetActive(false);

            InvokeMethod(graph.Manager, "NextPlayer");

            Assert.IsTrue(graph.Panel.activeSelf, "R14: human turn must show the CallShot panel.");
            Assert.AreEqual(15, graph.BallGrid.childCount, "R14: expected one button for balls 1-15.");
            Assert.AreEqual(6, graph.PocketGrid.childCount, "R14: expected six pocket buttons.");

            graph.BallGrid.Find("Ball_3").GetComponent<Button>().onClick.Invoke();
            graph.PocketGrid.Find("Pocket_2").GetComponent<Button>().onClick.Invoke();
            Assert.IsTrue(graph.Confirm.interactable, "R14: Confirm enables after ball and pocket selection.");

            graph.Confirm.onClick.Invoke();

            Assert.IsFalse(graph.Panel.activeSelf, "R14: Confirm hides the panel.");
            Assert.AreEqual(3, (int)GetField(graph.Manager, "calledBallId"),
                "R14: OnShotCalled must reach SetCallShot(ball).");
            Assert.AreEqual(2, (int)GetField(graph.Manager, "calledPocketId"),
                "R14: OnShotCalled must reach SetCallShot(pocket).");
        }

        [UnityTest]
        public IEnumerator R14_CancelClearsCallShot_AndAiTurnDoesNotShow()
        {
            CallShotGraph graph = CreateCallShotGraph();
            yield return null;

            SetField(graph.Manager, "currentPhase", Enum.Parse(MatchStateType, "Playing"));
            SetField(graph.Manager, "callShotRequired", true);
            SetField(graph.Manager, "player1Group", Enum.Parse(GetFieldType(graph.Manager, "player1Group"), "Red"));
            SetField(graph.Manager, "player2Group", Enum.Parse(GetFieldType(graph.Manager, "player2Group"), "Yellow"));
            SetField(graph.Manager, "currentPlayerIndex", 1);
            InvokeMethod(graph.Manager, "SetCallShot", 4, 1);
            graph.Panel.SetActive(true);

            graph.Cancel.onClick.Invoke();

            Assert.IsFalse(graph.Panel.activeSelf, "R14: Cancel must hide the panel.");
            Assert.AreEqual(-1, (int)GetField(graph.Manager, "calledBallId"),
                "R14: OnCallShotCancelled must clear ball.");
            Assert.AreEqual(-1, (int)GetField(graph.Manager, "calledPocketId"),
                "R14: OnCallShotCancelled must clear pocket.");

            graph.AiModifier = graph.Root.AddComponent(AIModifierType);
            SetField(graph.Manager, "aiModifier", graph.AiModifier);
            SetField(graph.Manager, "currentPlayerIndex", 0);
            graph.Panel.SetActive(false);

            InvokeMethod(graph.Manager, "NextPlayer");

            Assert.IsTrue((bool)GetField(graph.Manager, "isAiTurn"),
                "R14: Player 2 with AI modifier must be AI turn.");
            Assert.IsFalse(graph.Panel.activeSelf, "R14: AI turn must not show CallShot UI.");
        }

        [TestCase("Meta Quest 2", false, 72)]
        [TestCase("Oculus Quest", false, 72)]
        [TestCase("Meta Quest 3", false, 90)]
        [TestCase("Meta Quest 3", true, 120)]
        [TestCase("Meta Quest 3S", true, 90)]
        [TestCase("Meta Quest Pro", true, 90)]
        [TestCase("Generic PCVR", false, 90)]
        [TestCase("", false, 90)]
        public void R16_AutoFrameRatePolicy_MapsDeviceModelToExpectedHz(string deviceModel, bool enable120, int expectedHz)
        {
            Assert.IsNotNull(VRStartupType, "R16: VRStartup type is missing from Assembly-CSharp.");
            MethodInfo resolver = VRStartupType.GetMethod(
                "ResolveAutoFrameRate",
                BindingFlags.NonPublic | BindingFlags.Static);

            Assert.IsNotNull(resolver, "R16: deterministic frame-rate policy seam is missing.");
            int actual = (int)resolver.Invoke(null, new object[] { deviceModel, enable120 });

            Assert.AreEqual(expectedHz, actual, $"R16: unexpected target Hz for device '{deviceModel}'.");
        }

        [UnityTest]
        public IEnumerator R17_TitleScene_HasOneWiredCallShotUI()
        {
            yield return LoadAndAssertCallShotScene("Title_NoksGrandHall");
        }

        [UnityTest]
        public IEnumerator R17_AaaRoomScene_HasOneWiredCallShotUI()
        {
            yield return LoadAndAssertCallShotScene("AAA_RoomDAY");
        }

        private IEnumerator LoadAndAssertCallShotScene(string sceneName)
        {
            AsyncOperation load = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            Assert.IsNotNull(load, $"R17: scene '{sceneName}' is not available to PlayMode tests.");
            yield return load;

            _loadedScene = SceneManager.GetSceneByName(sceneName);
            Assert.IsTrue(_loadedScene.IsValid() && _loadedScene.isLoaded, $"R17: failed to load '{sceneName}'.");

            List<Component> sceneCallShotUIs = FindComponentsInScene(CallShotUIType, _loadedScene);
            Assert.AreEqual(1, sceneCallShotUIs.Count,
                $"R17: '{sceneName}' must contain exactly one ChinesePoolCallShotUI component.");

            Component survivor = sceneCallShotUIs[0];
            Assert.IsTrue((bool)InvokeMethod(survivor, "RunSelfTest"),
                $"R17: '{sceneName}' CallShotUI refs are incomplete.");
            AssertPrivateFieldNotNull(survivor, "_callShotPanel", sceneName);
            AssertPrivateFieldNotNull(survivor, "_titleText", sceneName);
            AssertPrivateFieldNotNull(survivor, "_instructionText", sceneName);
            AssertPrivateFieldNotNull(survivor, "_selectedBallText", sceneName);
            AssertPrivateFieldNotNull(survivor, "_selectedPocketText", sceneName);
            AssertPrivateFieldNotNull(survivor, "_ballSelectionGrid", sceneName);
            AssertPrivateFieldNotNull(survivor, "_pocketSelectionGrid", sceneName);
            AssertPrivateFieldNotNull(survivor, "_confirmButton", sceneName);
            AssertPrivateFieldNotNull(survivor, "_cancelButton", sceneName);

            List<Component> managers = FindComponentsInScene(UIManagerType, _loadedScene);
            Assert.AreEqual(1, managers.Count, $"R17: '{sceneName}' must contain one ChinesePoolUIManager.");
            object managerRef = GetField(managers[0], "_callShotUI");
            Assert.AreSame(survivor, managerRef,
                $"R17: '{sceneName}' UIManager must reference the wired CallShotUI survivor.");
        }

        private CallShotGraph CreateCallShotGraph()
        {
            var graph = new CallShotGraph
            {
                Root = new GameObject("PlayMode_CallShot_Root")
            };
            _runtimeObjects.Add(graph.Root);

            GameObject uiManagerObject = new GameObject("PlayMode_ChinesePoolUIManager");
            uiManagerObject.transform.SetParent(graph.Root.transform);
            graph.UiManager = uiManagerObject.AddComponent(UIManagerType);

            GameObject callShotObject = new GameObject("PlayMode_ChinesePoolCallShotUI");
            callShotObject.transform.SetParent(graph.Root.transform);
            graph.CallShotUI = callShotObject.AddComponent(CallShotUIType);

            graph.Panel = CreateChild(graph.Root, "CallShotPanel");
            graph.Panel.SetActive(false);
            graph.BallGrid = CreateChild(graph.Root, "BallGrid").transform;
            graph.PocketGrid = CreateChild(graph.Root, "PocketGrid").transform;
            graph.Confirm = CreateChild(graph.Root, "Confirm").AddComponent<Button>();
            graph.Cancel = CreateChild(graph.Root, "Cancel").AddComponent<Button>();

            SetField(graph.CallShotUI, "_callShotPanel", graph.Panel);
            SetField(graph.CallShotUI, "_ballSelectionGrid", graph.BallGrid);
            SetField(graph.CallShotUI, "_pocketSelectionGrid", graph.PocketGrid);
            SetField(graph.CallShotUI, "_confirmButton", graph.Confirm);
            SetField(graph.CallShotUI, "_cancelButton", graph.Cancel);
            SetField(graph.UiManager, "_callShotUI", graph.CallShotUI);
            // Awake ran before the private refs were injected; replay it so the real
            // button listeners are installed exactly as they are on a serialized scene.
            InvokePrivateMethod(graph.CallShotUI, "Awake");

            GameObject managerObject = new GameObject("PlayMode_ChinesePoolGameManager");
            managerObject.transform.SetParent(graph.Root.transform);
            graph.Manager = managerObject.AddComponent(GameManagerType);
            SetField(graph.Manager, "callShotUI", graph.CallShotUI);

            return graph;
        }

        private static GameObject CreateChild(GameObject parent, string name)
        {
            var child = new GameObject(name);
            child.transform.SetParent(parent.transform, false);
            return child;
        }

        private static List<Component> FindComponentsInScene(Type componentType, Scene scene)
        {
            var result = new List<Component>();
            Component[] all = UnityEngine.Object.FindObjectsByType<Component>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            foreach (Component component in all)
            {
                if (component != null && component.GetType() == componentType && component.gameObject.scene == scene)
                    result.Add(component);
            }
            return result;
        }

        private static void AssertPrivateFieldNotNull(object instance, string fieldName, string sceneName)
        {
            FieldInfo field = FindField(instance.GetType(), fieldName);
            Assert.IsNotNull(field, $"R17: field '{fieldName}' no longer exists in '{sceneName}'.");
            Assert.IsNotNull(field.GetValue(instance), $"R17: field '{fieldName}' is empty in '{sceneName}'.");
        }

        private static Type RuntimeType(string fullName)
        {
            return Type.GetType(fullName + ", Assembly-CSharp");
        }

        private static void AssertRuntimeTypesAvailable()
        {
            Assert.IsNotNull(GameManagerType, "R14: ChinesePoolGameManager is unavailable.");
            Assert.IsNotNull(CallShotUIType, "R14: ChinesePoolCallShotUI is unavailable.");
            Assert.IsNotNull(UIManagerType, "R17: ChinesePoolUIManager is unavailable.");
            Assert.IsNotNull(AIModifierType, "R14: ChinesePoolAIModifier is unavailable.");
            Assert.IsNotNull(MatchStateType, "R14: ChinesePoolMatchState is unavailable.");
        }

        private static FieldInfo FindField(Type type, string fieldName)
        {
            return type.GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        }

        private static Type GetFieldType(object instance, string fieldName)
        {
            FieldInfo field = FindField(instance.GetType(), fieldName);
            Assert.IsNotNull(field, $"Field '{fieldName}' is missing from {instance.GetType().Name}.");
            return field.FieldType;
        }

        private static object GetField(object instance, string fieldName)
        {
            FieldInfo field = FindField(instance.GetType(), fieldName);
            Assert.IsNotNull(field, $"Field '{fieldName}' is missing from {instance.GetType().Name}.");
            return field.GetValue(instance);
        }

        private static void SetField(object instance, string fieldName, object value)
        {
            FieldInfo field = FindField(instance.GetType(), fieldName);
            Assert.IsNotNull(field, $"Field '{fieldName}' is missing from {instance.GetType().Name}.");
            field.SetValue(instance, value);
        }

        private static object InvokeMethod(object instance, string methodName, params object[] arguments)
        {
            MethodInfo method = instance.GetType().GetMethod(
                methodName,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
                null,
                GetArgumentTypes(arguments),
                null);
            Assert.IsNotNull(method, $"Method '{methodName}' is missing from {instance.GetType().Name}.");
            return method.Invoke(instance, arguments);
        }

        private static void InvokePrivateMethod(object instance, string methodName)
        {
            MethodInfo method = instance.GetType().GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(method, $"Private method '{methodName}' is missing from {instance.GetType().Name}.");
            method.Invoke(instance, null);
        }

        private static Type[] GetArgumentTypes(object[] arguments)
        {
            var types = new Type[arguments.Length];
            for (int i = 0; i < arguments.Length; i++)
                types[i] = arguments[i].GetType();
            return types;
        }

        private sealed class CallShotGraph
        {
            public GameObject Root;
            public GameObject Panel;
            public Transform BallGrid;
            public Transform PocketGrid;
            public Button Confirm;
            public Button Cancel;
            public Component CallShotUI;
            public Component UiManager;
            public Component Manager;
            public Component AiModifier;
        }
    }
}
