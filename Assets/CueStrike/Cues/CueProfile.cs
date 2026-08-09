using UnityEngine;

[CreateAssetMenu(fileName = "CueProfile", menuName = "CueStrike/Cue Profile")]
public class CueProfile : ScriptableObject
{
    public string cueName = "Default Cue";
    public float length = 1.45f; // meters
    public float mass = 0.5f; // kg
    public float balancePoint = 0.6f; // normalized
    public float tipSize = 11f; // mm
    
    public enum MaterialType { Wood, Carbon }
    public MaterialType material = MaterialType.Wood;

    [Range(0f, 1f)] public float spinEfficiency = 0.6f;

    [Header("Visual Attributes (AAA)")]
    public Color cueColor = new Color(0.45f, 0.25f, 0.12f, 1f);
    [Range(0f, 1f)] public float smoothness = 0.8f;
    [Range(0f, 1f)] public float metallic = 0.05f;
    
    [Header("Tip Attributes")]
    public Color tipColor = new Color(0.15f, 0.5f, 0.7f, 1f); // chalk blue tip
}
