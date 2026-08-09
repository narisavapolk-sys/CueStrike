using System;
using System.Collections.Generic;
using UnityEngine;

namespace CueStrike.Characters
{
    /// <summary>
    /// Singleton manager for all playable characters.
    /// Handles selection, activation, and persistence.
    /// </summary>
    public class CueStrikeCharacterManager : MonoBehaviour
    {
        #region Singleton
        private static CueStrikeCharacterManager _instance;
        public static CueStrikeCharacterManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<CueStrikeCharacterManager>();
                }
                return _instance;
            }
        }
        #endregion

        #region Events
        public event Action<CueStrikeCharacterData> OnCharacterSelected;
        public event Action<CueStrikeCharacterAbility> OnAbilityActivated;
        #endregion

        [Header("Character Database")]
        [SerializeField] private List<CueStrikeCharacterData> _allCharacters = new List<CueStrikeCharacterData>();

        [Header("Runtime")]
        [SerializeField] private CueStrikeCharacterData _selectedCharacter;
        [SerializeField] private CueStrikeCharacterAbility _activeAbility;
        [SerializeField] private GameObject _activeCharacterInstance;

        private readonly Dictionary<string, CueStrikeCharacterData> _characterMap = new Dictionary<string, CueStrikeCharacterData>();

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
            BuildCharacterMap();
        }

        private void BuildCharacterMap()
        {
            _characterMap.Clear();
            foreach (var character in _allCharacters)
            {
                if (character != null && !string.IsNullOrEmpty(character.characterId))
                {
                    _characterMap[character.characterId] = character;
                }
            }
        }

        public void SelectCharacter(string characterId)
        {
            if (!_characterMap.TryGetValue(characterId, out CueStrikeCharacterData data))
            {
                Debug.LogError($"[CharacterManager] Character \"{characterId}\" not found.");
                return;
            }

            if (!data.isUnlocked)
            {
                Debug.LogWarning($"[CharacterManager] Character \"{characterId}\" is locked.");
                return;
            }

            _selectedCharacter = data;
            SpawnCharacterAbility(data);
            OnCharacterSelected?.Invoke(data);
            Debug.Log($"[CharacterManager] Selected: {data.displayName}");
        }

        private void SpawnCharacterAbility(CueStrikeCharacterData data)
        {
            // Remove old ability
            if (_activeAbility != null)
            {
                Destroy(_activeAbility);
                _activeAbility = null;
            }

            if (string.IsNullOrEmpty(data.abilityScriptType)) return;

            // Add ability component by type name
            Type abilityType = Type.GetType(data.abilityScriptType);
            if (abilityType == null)
            {
                Debug.LogError($"[CharacterManager] Ability type \"{data.abilityScriptType}\" not found.");
                return;
            }

            _activeAbility = gameObject.AddComponent(abilityType) as CueStrikeCharacterAbility;
            if (_activeAbility != null)
            {
                Debug.Log($"[CharacterManager] Ability attached: {_activeAbility.AbilityName}");
            }
        }

        public CueStrikeCharacterData GetSelectedCharacter() => _selectedCharacter;
        public CueStrikeCharacterAbility GetActiveAbility() => _activeAbility;

        public List<CueStrikeCharacterData> GetAllCharacters() => _allCharacters;
        public List<CueStrikeCharacterData> GetUnlockedCharacters()
        {
            return _allCharacters.FindAll(c => c != null && c.isUnlocked);
        }

        public CueStrikeCharacterData GetCharacterById(string id)
        {
            _characterMap.TryGetValue(id, out CueStrikeCharacterData data);
            return data;
        }

        public void UnlockCharacter(string characterId)
        {
            if (_characterMap.TryGetValue(characterId, out CueStrikeCharacterData data))
            {
                data.isUnlocked = true;
                Debug.Log($"[CharacterManager] Unlocked: {data.displayName}");
            }
        }

        #region Self-Test
        public bool RunSelfTest()
        {
            bool pass = true;
            if (_allCharacters.Count == 0)
            {
                Debug.LogError("[Self-Test] CharacterManager: No characters in database.");
                pass = false;
            }
            else
            {
                Debug.Log($"[Self-Test] CharacterManager: {_allCharacters.Count} characters registered.");
            }

            if (_selectedCharacter == null)
            {
                Debug.LogWarning("[Self-Test] CharacterManager: No character selected (expected before gameplay).");
            }

            Debug.Log($"[Self-Test] CharacterManager: {(pass ? "PASS" : "FAIL")}");
            return pass;
        }
        #endregion
    }
}