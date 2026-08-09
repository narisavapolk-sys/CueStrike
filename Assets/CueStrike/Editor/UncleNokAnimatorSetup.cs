using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

/// <summary>
/// Creates an Animator Controller for Uncle Nok with the required parameters
/// and a simple Idle state. Run from the Unity menu:
/// CueStrike → Character System → Create Uncle Nok Animator
/// </summary>
public static class UncleNokAnimatorSetup
{
    private const string ControllerPath = "Assets/CueStrike/Characters/UncleNok/UncleNok.controller";

    [MenuItem("CueStrike/Character System/Create Uncle Nok Animator")]
    public static void CreateAnimator()
    {
        // Ensure the target folder exists
        string folder = System.IO.Path.GetDirectoryName(ControllerPath);
        if (!AssetDatabase.IsValidFolder(folder))
        {
            AssetDatabase.CreateFolder("Assets/CueStrike/Characters", "UncleNok");
        }

        // Create the Animator Controller asset
        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);

        // Add the parameters required by the Mascot system
        controller.AddParameter("Speak", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Celebrate", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Disappointed", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Neutral", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("IsIdle", AnimatorControllerParameterType.Bool);

        // Create a simple layer with an Idle state
        AnimatorControllerLayer layer = controller.layers[0];
        AnimatorStateMachine stateMachine = layer.stateMachine;

        AnimatorState idleState = stateMachine.AddState("Idle");
        idleState.motion = null; // No animation clip yet – can be assigned later
        stateMachine.defaultState = idleState;

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Uncle Nok Animator Controller created at {ControllerPath}");
    }
}