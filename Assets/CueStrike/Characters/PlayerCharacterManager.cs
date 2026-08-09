using UnityEngine;
using System.Collections.Generic;

namespace CueStrike.Characters
{
    /// <summary>
    /// Singleton manager for player character selection and spawning.
    /// Spawns the selected character's prefab and manages IK + ability binding.
    /// </summary>
    public class PlayerCharacterManager : MonoBehaviour
    {
        public static PlayerCharacterManager Instance { get; private set; }

        [Header("Character Database")]
        [Tooltip("All available characters")]
        public List<CharacterData> availableCharacters = new List<CharacterData>();

        [Header("Current Selection")]
        [Tooltip("Currently selected character index")]
        public int selectedCharacterIndex = 0;

        [Tooltip("Currently spawned character instance")]
        public GameObject currentCharacterInstance;

        [Header("Spawn Settings")]
        [Tooltip("Parent transform for spawned character")]
        public Transform characterParent;

        [Tooltip("Offset position relative to player")]
        public Vector3 spawnOffset = new Vector3(0f, -0.5f, 0.5f);

        [Header("IK Binding")]
        [Tooltip("Reference to cue IK controller")]
        public CueIKController ikController;

        [Header("Events")]
        public System.Action<CharacterData> OnCharacterSelected;
        public System.Action<CharacterData> OnCharacterSpawned;

        // Runtime state
        private CharacterData _currentData = null;
        private MonoBehaviour _currentAbility = null;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (characterParent == null)
                characterParent = transform;
        }

        private void Start()
        {
            if (ikController == null)
                ikController = FindFirstObjectByType<CueIKController>();

            // Auto-spawn default character
            if (currentCharacterInstance == null && availableCharacters.Count > 0)
            {
                SpawnCharacter(selectedCharacterIndex);
            }
        }

        /// <summary>
        /// Select character by index
        /// </summary>
        public void SelectCharacter(int index)
        {
            if (index < 0 || index >= availableCharacters.Count)
            {
                Debug.LogWarning($"[PlayerCharacter] Invalid character index: {index}");
                return;
            }

            selectedCharacterIndex = index;
            OnCharacterSelected?.Invoke(availableCharacters[index]);
            Debug.Log($"[PlayerCharacter] Selected: {availableCharacters[index].characterName}");
        }

        /// <summary>
        /// Spawn the selected character prefab
        /// </summary>
        public void SpawnCharacter(int index)
        {
            if (index < 0 || index >= availableCharacters.Count)
            {
                Debug.LogWarning($"[PlayerCharacter] Cannot spawn: invalid index {index}");
                return;
            }

            CharacterData data = availableCharacters[index];
            if (data.characterPrefab == null)
            {
                Debug.LogWarning($"[PlayerCharacter] {data.characterName} has no prefab assigned!");
                return;
            }

            // Destroy existing character instance
            if (currentCharacterInstance != null)
            {
                Destroy(currentCharacterInstance);
                currentCharacterInstance = null;
            }

            // Spawn new character
            currentCharacterInstance = Instantiate(data.characterPrefab, characterParent);
            currentCharacterInstance.transform.localPosition = spawnOffset;
            currentCharacterInstance.transform.localRotation = Quaternion.identity;
            currentCharacterInstance.name = data.characterName;

            _currentData = data;

            // Bind IK controller
            if (ikController == null)
                ikController = FindFirstObjectByType<CueIKController>();

            if (ikController != null)
            {
                // Try to find hand IK targets in the spawned character
                var leftHand = currentCharacterInstance.transform.Find("LeftHand_IK");
                var rightHand = currentCharacterInstance.transform.Find("RightHand_IK");

                if (leftHand != null)
                    ikController.leftHandBridge = leftHand;
                if (rightHand != null)
                    ikController.rightHandGrip = rightHand;

                // Apply IK overrides
                if (data.bridgeDistanceOverride > 0f)
                    ikController.bridgeDistance = data.bridgeDistanceOverride;
                if (data.gripOffsetOverride > 0f)
                    ikController.gripOffsetFromButt = data.gripOffsetOverride;

                Debug.Log($"[PlayerCharacter] IK bound for {data.characterName}");
            }

            // Get ability controller
            if (!string.IsNullOrEmpty(data.abilityControllerType))
            {
                _currentAbility = data.GetAbilityController(currentCharacterInstance);
                if (_currentAbility != null)
                {
                    _currentAbility.enabled = true;
                    Debug.Log($"[PlayerCharacter] Ability '{data.abilityControllerType}' enabled for {data.characterName}");
                }
                else
                {
                    Debug.LogWarning($"[PlayerCharacter] Ability '{data.abilityControllerType}' not found on {data.characterName} prefab! Add the component.");
                }
            }

            // Apply material override
            if (data.characterMaterial != null)
            {
                var renderers = currentCharacterInstance.GetComponentsInChildren<Renderer>();
                foreach (var r in renderers)
                {
                    r.material = data.characterMaterial;
                }
            }

            // Apply card color as tint if material supports it
            ApplyColorTint(data.cardColor);

            OnCharacterSpawned?.Invoke(data);
            Debug.Log($"[PlayerCharacter] Spawned: {data.characterName}");
        }

        /// <summary>
        /// Spawn current selected character
        /// </summary>
        public void SpawnSelectedCharacter()
        {
            SpawnCharacter(selectedCharacterIndex);
        }

        /// <summary>
        /// Get current character data
        /// </summary>
        public CharacterData GetCurrentCharacter() => _currentData;

        /// <summary>
        /// Get current ability controller
        /// </summary>
        public MonoBehaviour GetCurrentAbility() => _currentAbility;

        /// <summary>
        /// Get ability controller of specific type
        /// </summary>
        public T GetAbility<T>() where T : MonoBehaviour
        {
            if (_currentAbility is T typed)
                return typed;
            return null;
        }

        /// <summary>
        /// Apply color tint to character materials
        /// </summary>
        private void ApplyColorTint(Color color)
        {
            if (currentCharacterInstance == null) return;

            var renderers = currentCharacterInstance.GetComponentsInChildren<Renderer>();
            foreach (var r in renderers)
            {
                foreach (var mat in r.materials)
                {
                    if (mat.HasProperty("_BaseColor"))
                        mat.SetColor("_BaseColor", Color.Lerp(mat.GetColor("_BaseColor"), color, 0.3f));
                }
            }
        }

        /// <summary>
        /// Get all available characters
        /// </summary>
        public List<CharacterData> GetAllCharacters() => availableCharacters;

        /// <summary>
        /// Get character count
        /// </summary>
        public int GetCharacterCount() => availableCharacters.Count;
    }
}