using UnityEngine;

namespace CueStrike.Characters
{
    /// <summary>
    /// Character data ScriptableObject.
    /// Stores name, prefab, ability, description, and icon for each playable character.
    /// </summary>
    [CreateAssetMenu(fileName = "NewCharacter", menuName = "CueStrike/Character Data", order = 1)]
    public class CharacterData : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Display name shown in UI")]
        public string characterName = "New Character";

        [Tooltip("Short subtitle / title")]
        public string subtitle = "";

        [Tooltip("Full description shown in character select screen")]
        [TextArea(2, 5)]
        public string description = "";

        [Header("Prefab")]
        [Tooltip("The character's 3D model prefab with all components")]
        public GameObject characterPrefab;

        [Tooltip("Material/Shader variant for the character")]
        public Material characterMaterial;

        [Header("UI")]
        [Tooltip("Portrait / icon for character selection screen")]
        public Sprite portrait;

        [Tooltip("Background color tint for the character card")]
        public Color cardColor = Color.white;

        [Header("Ability")]
        [Tooltip("Type name of the ability controller script (e.g. 'SomchayAbilityController')")]
        public string abilityControllerType = "";

        [Tooltip("Short description of the ability")]
        [TextArea(1, 3)]
        public string abilityDescription = "";

        [Header("Audio")]
        [Tooltip("Default voice/commentary audio clip")]
        public AudioClip voiceClip;

        [Tooltip("Ability activation sound")]
        public AudioClip abilitySound;

        [Header("IK Settings")]
        [Tooltip("Override IK bridge distance for this character (0 = use default)")]
        public float bridgeDistanceOverride = 0f;

        [Tooltip("Override IK grip offset for this character (0 = use default)")]
        public float gripOffsetOverride = 0f;

        /// <summary>
        /// Get the ability controller component from the prefab
        /// </summary>
        public MonoBehaviour GetAbilityController(GameObject instance)
        {
            if (instance == null || string.IsNullOrEmpty(abilityControllerType))
                return null;

            return instance.GetComponent(abilityControllerType) as MonoBehaviour;
        }
    }
}