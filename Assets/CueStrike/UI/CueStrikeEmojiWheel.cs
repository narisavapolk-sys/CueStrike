using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// CueStrikeEmojiWheel - Quick emoji reactions for VR multiplayer
/// Created by Nari for P'Mong | 2026-07-19
/// </summary>
public class CueStrikeEmojiWheel : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private InputActionReference thumbstickAction;

    [Header("Emoji Prefabs")]
    [SerializeField] private GameObject thumbsUpPrefab;
    [SerializeField] private GameObject clappingPrefab;
    [SerializeField] private GameObject laughingPrefab;
    [SerializeField] private GameObject shockedPrefab;

    [Header("Spawn Settings")]
    [SerializeField] private float spawnDistance = 0.5f;
    [SerializeField] private float lifetime = 2f;

    private void OnEnable()
    {
        if (thumbstickAction != null)
        {
            thumbstickAction.action.Enable();
        }
    }

    private void OnDisable()
    {
        if (thumbstickAction != null)
        {
            thumbstickAction.action.Disable();
        }
    }

    private void Update()
    {
        if (thumbstickAction == null) return;

        Vector2 axisInput = thumbstickAction.action.ReadValue<Vector2>();

        if (axisInput.magnitude < 0.5f) return;

        if (axisInput.y > 0.5f) TriggerEmoji("ThumbsUp");
        else if (axisInput.y < -0.5f) TriggerEmoji("Clapping");
        else if (axisInput.x < -0.5f) TriggerEmoji("Laughing");
        else if (axisInput.x > 0.5f) TriggerEmoji("Shocked");
    }

    private void TriggerEmoji(string emojiType)
    {
        GameObject prefab = GetEmojiPrefab(emojiType);
        if (prefab == null) return;

        Vector3 spawnPos = transform.position + transform.forward * spawnDistance;
        var instance = Instantiate(prefab, spawnPos, Quaternion.LookRotation(-transform.forward));
        Destroy(instance, lifetime);
    }

    private GameObject GetEmojiPrefab(string emojiType)
    {
        switch (emojiType)
        {
            case "ThumbsUp": return thumbsUpPrefab;
            case "Clapping": return clappingPrefab;
            case "Laughing": return laughingPrefab;
            case "Shocked": return shockedPrefab;
            default: return null;
        }
    }
}