using UnityEngine;
using CueStrike.AI;

public class CueStrikeAutoDemo : MonoBehaviour
{
    [Header("Auto Demo Settings")]
    public CueStrikeAIController aiController;

    // Removed SkillLevel since it no longer exists in CueStrikeAIController
    // public CueStrikeAIController.SkillLevel skill = CueStrikeAIController.SkillLevel.Medium;

    public float delayBeforeShot = 1.5f;   // Delay before shooting
    public bool autoRepeat = false;        // Repeat shots continuously
    public float repeatDelay = 3f;         // Delay before repeating shot

    private bool hasShot = false;

    void Start()
    {
        if (aiController == null)
        {
            aiController = FindObjectOfType<CueStrikeAIController>();
        }

        // If you want AI skill level, set it directly in AIController
        // aiController.skill = skill;

        Invoke(nameof(FireShot), delayBeforeShot);
    }

    void FireShot()
    {
        if (aiController == null) return;

        aiController.ExecuteAIShot();
        hasShot = true;

        if (autoRepeat)
        {
            Invoke(nameof(FireShot), repeatDelay);
        }
    }
}