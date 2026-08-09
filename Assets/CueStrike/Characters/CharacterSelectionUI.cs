using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

namespace CueStrike.Characters
{
    /// <summary>
    /// VR character selection UI.
    /// Displays available characters in a horizontal list with ray interactor support.
    /// </summary>
    public class CharacterSelectionUI : MonoBehaviour
    {
        [Header("UI References")]
        [Tooltip("Character card prefab (must have CharacterCard component)")]
        public GameObject cardPrefab;

        [Tooltip("Parent transform for card grid/list")]
        public Transform cardContainer;

        [Tooltip("Character name display text")]
        public TextMeshProUGUI nameText;

        [Tooltip("Character subtitle display text")]
        public TextMeshProUGUI subtitleText;

        [Tooltip("Character description display text")]
        public TextMeshProUGUI descriptionText;

        [Tooltip("Ability description display text")]
        public TextMeshProUGUI abilityText;

        [Tooltip("Confirm button")]
        public Button confirmButton;

        [Header("Navigation")]
        public Button nextButton;
        public Button prevButton;

        [Header("Preview")]
        [Tooltip("Preview spawn point for character model")]
        public Transform previewSpawnPoint;

        [Header("Settings")]
        [Tooltip("Auto-close after selection")]
        public bool autoCloseOnSelect = true;

        // Internal state
        private int _currentIndex = 0;
        private List<CharacterData> _characters = new List<CharacterData>();
        private GameObject _previewInstance;
        private PlayerCharacterManager _manager;

        private void Start()
        {
            _manager = PlayerCharacterManager.Instance;
            if (_manager == null)
            {
                Debug.LogError("[CharacterSelectionUI] PlayerCharacterManager not found!");
                return;
            }

            _characters = _manager.GetAllCharacters();
            if (_characters.Count == 0)
            {
                Debug.LogWarning("[CharacterSelectionUI] No characters available!");
                return;
            }

            // Setup navigation
            if (nextButton != null)
                nextButton.onClick.AddListener(NextCharacter);

            if (prevButton != null)
                prevButton.onClick.AddListener(PreviousCharacter);

            if (confirmButton != null)
                confirmButton.onClick.AddListener(ConfirmSelection);

            // Show first character
            ShowCharacter(0);
        }

        /// <summary>
        /// Show character at index
        /// </summary>
        public void ShowCharacter(int index)
        {
            if (_characters.Count == 0) return;

            _currentIndex = Mathf.Clamp(index, 0, _characters.Count - 1);
            CharacterData data = _characters[_currentIndex];

            // Update UI text
            if (nameText != null) nameText.text = data.characterName;
            if (subtitleText != null) subtitleText.text = data.subtitle;
            if (descriptionText != null) descriptionText.text = data.description;
            if (abilityText != null) abilityText.text = $"Ability: {data.abilityDescription}";

            // Update preview
            UpdatePreview(data);

            Debug.Log($"[CharacterSelectionUI] Showing: {data.characterName}");
        }

        /// <summary>
        /// Update 3D preview of character
        /// </summary>
        private void UpdatePreview(CharacterData data)
        {
            // Destroy old preview
            if (_previewInstance != null)
            {
                Destroy(_previewInstance);
                _previewInstance = null;
            }

            // Create new preview
            if (data.characterPrefab != null && previewSpawnPoint != null)
            {
                _previewInstance = Instantiate(data.characterPrefab, previewSpawnPoint);
                _previewInstance.transform.localPosition = Vector3.zero;
                _previewInstance.transform.localRotation = Quaternion.Euler(0f, 180f, 0f); // Face camera

                // Remove ability controllers from preview (they'd run in background)
                var abilities = _previewInstance.GetComponents<MonoBehaviour>();
                foreach (var ab in abilities)
                {
                    if (ab is Somchay.SomchayAbilityController ||
                        ab is MeiLing.MeiLingAbilityController ||
                        ab is Gentleman.GentlemanAbilityController)
                    {
                        Destroy(ab);
                    }
                }

                // Scale preview appropriately
                _previewInstance.transform.localScale = Vector3.one * 0.8f;
            }
        }

        /// <summary>
        /// Go to next character
        /// </summary>
        public void NextCharacter()
        {
            ShowCharacter(_currentIndex + 1);
        }

        /// <summary>
        /// Go to previous character
        /// </summary>
        public void PreviousCharacter()
        {
            ShowCharacter(_currentIndex - 1);
        }

        /// <summary>
        /// Confirm current selection and spawn character
        /// </summary>
        public void ConfirmSelection()
        {
            if (_manager != null)
            {
                _manager.SelectCharacter(_currentIndex);
                _manager.SpawnSelectedCharacter();
                Debug.Log($"[CharacterSelectionUI] Confirmed: {_characters[_currentIndex].characterName}");
            }

            if (autoCloseOnSelect)
                gameObject.SetActive(false);
        }

        /// <summary>
        /// Open the selection UI
        /// </summary>
        public void Open()
        {
            gameObject.SetActive(true);
            ShowCharacter(_currentIndex);
        }

        /// <summary>
        /// Close the selection UI
        /// </summary>
        public void Close()
        {
            gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (_previewInstance != null)
                Destroy(_previewInstance);
        }
    }
}