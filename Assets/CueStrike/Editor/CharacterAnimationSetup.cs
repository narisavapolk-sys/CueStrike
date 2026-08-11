using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.Collections.Generic;
using System.IO;

/// <summary>
/// R27 — Character Animation Setup.
///
/// Takes the 4 animation FBX files exported from the Blender pipeline
/// (Idle/Celebrate/Disappointed/Speak) and:
///   1. Remaps each clip's bone curve paths so they match the rig actually
///      used by the mascot prefabs (Somchay_Rig/... under the Animator root).
///   2. Creates .anim assets from the remapped clips.
///   3. Builds/updates UncleNok.controller: Idle (default, looping) plus
///      Celebrate/Disappointed/Speak states with AnyState trigger transitions.
///   4. Assigns the controller to UncleNok_Prefab + BoPanda_Prefab and syncs
///      the UncleNokReferee trigger field names to the controller parameters.
///
/// Menu: Tools → CueStrike → Character System → 40. Setup Character Animations
/// </summary>
public static class CharacterAnimationSetup
{
    private const string AnimationsFbxDir = "Assets/CueStrike/Models/AAA_Characters/Animations";
    private const string ClipsOutDir = "Assets/CueStrike/Characters/Animations";
    private const string ControllerPath = "Assets/CueStrike/Characters/UncleNok/UncleNok.controller";
    private const string UncleNokPrefabPath = "Assets/CueStrike/Characters/UncleNok/UncleNok_Prefab.prefab";
    private const string BoPandaPrefabPath = "Assets/CueStrike/Characters/BoPanda/BoPanda_Prefab.prefab";

    // Rigify root bone name (identical across all characters — verified).
    // The prefab hierarchy is: <PrefabRoot> [Animator] -> Somchay_Rig -> root -> ...
    private const string ArmatureChildName = "Somchay_Rig";

    private struct ClipJob
    {
        public string FbxName;   // e.g. "Idle"
        public string StateName; // controller state name
        public bool Loop;
    }

    private static readonly ClipJob[] Jobs =
    {
        new ClipJob { FbxName = "Idle", StateName = "Idle", Loop = true },
        new ClipJob { FbxName = "Celebrate", StateName = "Celebrate", Loop = false },
        new ClipJob { FbxName = "Disappointed", StateName = "Disappointed", Loop = false },
        new ClipJob { FbxName = "Speak", StateName = "Speak", Loop = false },
    };

    [MenuItem("Tools/CueStrike/Character System/40. Setup Character Animations")]
    public static void Setup()
    {
        // Batchmode-friendly: open a scene if none active so prefab wiring works.
        if (Application.isBatchMode && UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().path == "")
        {
            string[] sceneGuids = AssetDatabase.FindAssets("Title_NoksGrandHall t:Scene");
            if (sceneGuids.Length > 0)
            {
                UnityEditor.SceneManagement.EditorSceneManager.OpenScene(
                    AssetDatabase.GUIDToAssetPath(sceneGuids[0]));
            }
        }

        int steps = 0;
        steps += EnsureClips() ? 1 : 0;
        steps += EnsureController() ? 1 : 0;
        steps += WirePrefabs() ? 1 : 0;
        int selfTest = SelfTest();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[CharacterAnimationSetup] DONE: clips={Jobs.Length} steps={steps}/3 selftest={selfTest}");
    }

    // ------------------------------------------------------------
    // 1. Clips: read FBX clip -> remap bone paths -> .anim asset
    // ------------------------------------------------------------
    private static bool EnsureClips()
    {
        if (!AssetDatabase.IsValidFolder(ClipsOutDir))
        {
            string parent = Path.GetDirectoryName(ClipsOutDir).Replace('\\', '/');
            string leaf = Path.GetFileName(ClipsOutDir);
            if (!AssetDatabase.IsValidFolder(parent))
            {
                AssetDatabase.CreateFolder("Assets/CueStrike/Characters", "Animations");
            }
            else
            {
                AssetDatabase.CreateFolder(parent, leaf);
            }
        }

        bool ok = true;
        foreach (ClipJob job in Jobs)
        {
            string fbxPath = $"{AnimationsFbxDir}/{job.FbxName}.fbx";
            string animPath = $"{ClipsOutDir}/{job.FbxName}.anim";

            AnimationClip srcClip = LoadClipFromFbx(fbxPath, job.FbxName);
            if (srcClip == null)
            {
                Debug.LogError($"[CharacterAnimationSetup] No clip '{job.FbxName}' in {fbxPath}");
                ok = false;
                continue;
            }

            AnimationClip remapped = new AnimationClip();
            remapped.name = job.FbxName;
            remapped.frameRate = srcClip.frameRate;

            var bindings = AnimationUtility.GetCurveBindings(srcClip);
            int curves = 0;
            foreach (EditorCurveBinding binding in bindings)
            {
                string remappedPath = RemapPath(binding.path);
                EditorCurveBinding newBinding = binding;
                newBinding.path = remappedPath;
                AnimationCurve curve = AnimationUtility.GetEditorCurve(srcClip, binding);
                if (curve == null) continue;
                AnimationUtility.SetEditorCurve(remapped, newBinding, curve);
                curves++;
            }

            if (curves == 0)
            {
                Debug.LogError($"[CharacterAnimationSetup] {job.FbxName}: 0 remapped curves");
                ok = false;
                continue;
            }

            var settings = AnimationUtility.GetAnimationClipSettings(srcClip);
            remapped.wrapMode = job.Loop ? WrapMode.Loop : WrapMode.ClampForever;
            // Loop time for idle
            var clipSettings = new AnimationClipSettings();
            clipSettings.loopTime = job.Loop;
            AnimationUtility.SetAnimationClipSettings(remapped, clipSettings);

            AnimationClip existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(animPath);
            if (existing != null)
            {
                EditorUtility.CopySerialized(remapped, existing);
                Debug.Log($"[CharacterAnimationSetup] Updated {animPath} ({curves} curves, path prefix '{ArmatureChildName}/')");
            }
            else
            {
                AssetDatabase.CreateAsset(remapped, animPath);
                Debug.Log($"[CharacterAnimationSetup] Created {animPath} ({curves} curves, path prefix '{ArmatureChildName}/')");
            }
        }
        return ok;
    }

    private static AnimationClip LoadClipFromFbx(string fbxPath, string name)
    {
        if (!File.Exists(fbxPath)) return null;
        // Force import if not yet imported (fresh files in batchmode).
        AssetDatabase.ImportAsset(fbxPath, ImportAssetOptions.ForceUpdate);
        foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(fbxPath))
        {
            if (asset is AnimationClip clip && clip.name.Contains(name))
            {
                return clip;
            }
        }
        return null;
    }

    private static string RemapPath(string path)
    {
        // FBX clips are rooted at the armature's own hierarchy ("root", "MCH-...", "DEF-...").
        // In the prefab the armature is a child named Somchay_Rig under the Animator root.
        if (string.IsNullOrEmpty(path))
        {
            return ArmatureChildName; // armature transform itself
        }
        return $"{ArmatureChildName}/{path}";
    }

    // ------------------------------------------------------------
    // 2. Controller: states + transitions + clip assignment
    // ------------------------------------------------------------
    private static bool EnsureController()
    {
        AnimatorController controller =
            AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller == null)
        {
            controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        }

        // Parameters: triggers the mascot scripts use.
        EnsureParameter(controller, "Speak", AnimatorControllerParameterType.Trigger);
        EnsureParameter(controller, "Celebrate", AnimatorControllerParameterType.Trigger);
        EnsureParameter(controller, "Disappointed", AnimatorControllerParameterType.Trigger);
        EnsureParameter(controller, "Neutral", AnimatorControllerParameterType.Trigger);
        EnsureParameter(controller, "IsIdle", AnimatorControllerParameterType.Bool);

        AnimatorStateMachine sm = controller.layers[0].stateMachine;

        // Create/reuse states + assign clips.
        AnimatorState idleState = EnsureState(sm, "Idle");
        AnimatorState celebrateState = EnsureState(sm, "Celebrate");
        AnimatorState disappointedState = EnsureState(sm, "Disappointed");
        AnimatorState speakState = EnsureState(sm, "Speak");

        idleState.motion = LoadAnim("Idle");
        celebrateState.motion = LoadAnim("Celebrate");
        disappointedState.motion = LoadAnim("Disappointed");
        speakState.motion = LoadAnim("Speak");

        if (sm.defaultState != idleState)
        {
            sm.defaultState = idleState;
        }

        // AnyState -> trigger states, each returns to Idle.
        EnsureAnyStateTransition(sm, celebrateState, "Celebrate", 0.15f);
        EnsureAnyStateTransition(sm, disappointedState, "Disappointed", 0.15f);
        EnsureAnyStateTransition(sm, speakState, "Speak", 0.15f);

        EnsureReturnToIdle(sm, celebrateState, idleState, 0.25f);
        EnsureReturnToIdle(sm, disappointedState, idleState, 0.25f);
        EnsureReturnToIdle(sm, speakState, idleState, 0.25f);

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        return true;
    }

    private static void EnsureParameter(AnimatorController controller, string name,
        AnimatorControllerParameterType type)
    {
        foreach (AnimatorControllerParameter p in controller.parameters)
        {
            if (p.name == name) return;
        }
        controller.AddParameter(name, type);
    }

    private static AnimatorState EnsureState(AnimatorStateMachine sm, string name)
    {
        foreach (ChildAnimatorState child in sm.states)
        {
            if (child.state.name == name) return child.state;
        }
        return sm.AddState(name);
    }

    private static AnimationClip LoadAnim(string name)
    {
        return AssetDatabase.LoadAssetAtPath<AnimationClip>($"{ClipsOutDir}/{name}.anim");
    }

    private static void EnsureAnyStateTransition(AnimatorStateMachine sm,
        AnimatorState target, string triggerName, float duration)
    {
        foreach (AnimatorStateTransition t in sm.anyStateTransitions)
        {
            if (t.destinationState == target && t.conditions.Length == 1 &&
                t.conditions[0].parameter == triggerName)
            {
                return;
            }
        }
        AnimatorStateTransition trans = sm.AddAnyStateTransition(target);
        trans.hasExitTime = false;
        trans.duration = duration;
        trans.AddCondition(AnimatorConditionMode.If, 0f, triggerName);
    }

    private static void EnsureReturnToIdle(AnimatorStateMachine sm,
        AnimatorState from, AnimatorState idle, float duration)
    {
        foreach (AnimatorStateTransition t in from.transitions)
        {
            if (t.destinationState == idle && t.hasExitTime) return;
        }
        AnimatorStateTransition trans = from.AddTransition(idle);
        trans.hasExitTime = true;
        trans.exitTime = 0.9f;
        trans.duration = duration;
    }

    // ------------------------------------------------------------
    // 3. Wire prefabs: assign controller + fix referee trigger names
    // ------------------------------------------------------------
    private static bool WirePrefabs()
    {
        AnimatorController controller =
            AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller == null)
        {
            Debug.LogError("[CharacterAnimationSetup] Controller missing before wiring");
            return false;
        }

        bool ok = true;
        ok &= WirePrefab(UncleNokPrefabPath, controller);
        ok &= WirePrefab(BoPandaPrefabPath, controller);
        return ok;
    }

    private static bool WirePrefab(string prefabPath, AnimatorController controller)
    {
        GameObject root = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (root == null)
        {
            Debug.LogError($"[CharacterAnimationSetup] Prefab not found: {prefabPath}");
            return false;
        }

        Animator animator = root.GetComponent<Animator>();
        if (animator == null)
        {
            Debug.LogError($"[CharacterAnimationSetup] No Animator on {prefabPath}");
            return false;
        }

        animator.runtimeAnimatorController = controller;

        // Sync UncleNokReferee trigger field names to the controller parameters.
        var referee = root.GetComponent<CueStrike.MascotSystem.UncleNokReferee>();
        if (referee != null)
        {
            SerializedObject so = new SerializedObject(referee);
            // Announce/Thinking don't exist in the controller -> map to Speak/Idle.
            SetSerializedString(so, "_announceTrigger", "Speak");
            SetSerializedString(so, "_disapproveTrigger", "Disappointed");
            SetSerializedString(so, "_thinkingTrigger", "Speak");
            SetSerializedString(so, "_celebrateTrigger", "Celebrate");
            SetSerializedString(so, "_speakTrigger", "Speak");
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        EditorUtility.SetDirty(root);
        AssetDatabase.SaveAssets();
        Debug.Log($"[CharacterAnimationSetup] Wired {prefabPath} -> {ControllerPath} (controller) + referee triggers synced");
        return true;
    }

    private static void SetSerializedString(SerializedObject so, string prop, string value)
    {
        SerializedProperty p = so.FindProperty(prop);
        if (p != null && p.propertyType == SerializedPropertyType.String)
        {
            p.stringValue = value;
        }
    }

    // ------------------------------------------------------------
    // Self test
    // ------------------------------------------------------------
    public static int SelfTest()
    {
        int pass = 0;
        int total = 5;

        // 1. All 4 .anim assets exist with curves
        bool clipsOk = true;
        foreach (ClipJob job in Jobs)
        {
            AnimationClip clip = LoadAnim(job.FbxName);
            if (clip == null || AnimationUtility.GetCurveBindings(clip).Length == 0)
            {
                clipsOk = false;
                break;
            }
        }
        if (clipsOk) pass++;
        Debug.Log($"[CharacterAnimationSetup] SelfTest {(clipsOk ? "PASS" : "FAIL")}: 4 clips exist with curves");

        // 2. Controller has all 4 states
        AnimatorController controller =
            AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        bool statesOk = controller != null &&
            HasState(controller, "Idle") && HasState(controller, "Celebrate") &&
            HasState(controller, "Disappointed") && HasState(controller, "Speak");
        if (statesOk) pass++;
        Debug.Log($"[CharacterAnimationSetup] SelfTest {(statesOk ? "PASS" : "FAIL")}: controller has 4 states");

        // 3. AnyState transitions for the 3 trigger states
        bool transOk = false;
        if (controller != null)
        {
            var sm = controller.layers[0].stateMachine;
            transOk = sm.anyStateTransitions.Length >= 3;
        }
        if (transOk) pass++;
        Debug.Log($"[CharacterAnimationSetup] SelfTest {(transOk ? "PASS" : "FAIL")}: >=3 AnyState transitions");

        // 4. Both prefabs have the controller assigned
        bool prefabOk = PrefabHasController(UncleNokPrefabPath, controller) &&
                        PrefabHasController(BoPandaPrefabPath, controller);
        if (prefabOk) pass++;
        Debug.Log($"[CharacterAnimationSetup] SelfTest {(prefabOk ? "PASS" : "FAIL")}: both prefabs wired");

        // 5. Clip paths carry the Somchay_Rig/ prefix (rig match)
        bool pathOk = true;
        AnimationClip probe = LoadAnim("Idle");
        if (probe != null)
        {
            foreach (var b in AnimationUtility.GetCurveBindings(probe))
            {
                if (string.IsNullOrEmpty(b.path)) continue; // root of the Animator itself
                if (b.path != ArmatureChildName && !b.path.StartsWith(ArmatureChildName + "/"))
                {
                    pathOk = false;
                    break;
                }
            }
        }
        if (pathOk) pass++;
        Debug.Log($"[CharacterAnimationSetup] SelfTest {(pathOk ? "PASS" : "FAIL")}: clip paths use {ArmatureChildName}/ prefix");

        Debug.Log($"[CharacterAnimationSetup] SelfTest RESULT: {pass}/{total}");
        return pass == total ? 1 : 0;
    }

    private static bool HasState(AnimatorController controller, string name)
    {
        foreach (ChildAnimatorState child in controller.layers[0].stateMachine.states)
        {
            if (child.state.name == name) return true;
        }
        return false;
    }

    private static bool PrefabHasController(string prefabPath, AnimatorController controller)
    {
        GameObject root = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        Animator animator = root != null ? root.GetComponent<Animator>() : null;
        return animator != null && animator.runtimeAnimatorController == controller;
    }
}
