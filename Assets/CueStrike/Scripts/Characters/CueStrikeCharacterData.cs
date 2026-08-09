using UnityEngine;

namespace CueStrike.Characters
{
    /// <summary>
    /// ScriptableObject holding character data for CueStrike VR.
    /// Create via Assets > Create > CueStrike > Character Data
    /// </summary>
    [CreateAssetMenu(fileName = "NewCharacter", menuName = "CueStrike/Character Data")]
    public class CueStrikeCharacterData : ScriptableObject
    {
        [Header("Identity")]
        public string characterId;
        public string displayName;
        [TextArea(2, 4)] public string description;
        public Sprite portrait;
        public Sprite fullBodyImage;

        [Header("Visuals")]
        public GameObject characterPrefab;
        public Material hologramMaterial;
        public Color themeColor = Color.white;

        [Header("Gameplay")]
        public float accuracyBonus = 0f;
        public float powerBonus = 0f;
        public float spinBonus = 0f;
        public float focusBonus = 0f;

        [Header("Ability")]
        public string abilityName;
        [TextArea(2, 3)] public string abilityDescription;
        public string abilityScriptType; // Full type name e.g. "CueStrike.Characters.Ability_HypeEngine"

        [Header("Audio")]
        public AudioClip voiceClip;      // Character voice line / taunt
        public AudioClip abilitySound;   // Sound when ability activates

        [Header("Unlock")]
        public bool isUnlocked = true;
        public int unlockCost = 0;
        public string unlockCondition = "";
}
}