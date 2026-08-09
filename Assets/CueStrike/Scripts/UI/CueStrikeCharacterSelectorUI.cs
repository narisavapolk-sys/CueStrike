using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CueStrike.UI
{
    /// <summary>
    /// VR-optimized Character Selection UI.
    /// World-space canvas. Shows all unlocked characters with portraits and ability info.
    /// </summary>
    public class CueStrikeCharacterSelectorUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Transform _characterButtonContainer;
        [SerializeField] private GameObject _characterButtonPrefab;
        [SerializeField] private Image _portraitImage;
        [SerializeField] private Text _nameText;
        [SerializeField] private Text _descriptionText;
        [SerializeField] private Text _abilityNameText;
        [SerializeField] private Text _abilityDescriptionText;
        [SerializeField] private Button _selectButton;
        [SerializeField] private Button _backButton;

        [Header("Visual")]
        [SerializeField] private Color _selectedColor = new Color(1f, 0.84f, 0f, 1f);
        [SerializeField] private Color _normalColor = Color.white;
        [SerializeField] private Color _lockedColor = new Color(0.3f, 0.3f, 0.3f, 1f);

        private List<GameObject> _buttonInstances = new List<GameObject>();
        private Characters.CueStrikeCharacterData _highlightedCharacter;
        private CueStrikeUIAnimations _animator;

        private void Awake()
        {
            _animator = FindFirstObjectByType<CueStrikeUIAnimations>();
            if (_selectButton != null)
                _selectButton.onClick.AddListener(OnSelectClicked);
            if (_backButton != null)
                _backButton.onClick.AddListener(OnBackClicked);
        }

        private void OnEnable()
        {
            RefreshCharacterList();
        }

        public void RefreshCharacterList()
        {
            // Clear old buttons
            foreach (var btn in _buttonInstances)
            {
                if (btn != null) Destroy(btn);
            }
            _buttonInstances.Clear();

            var manager = Characters.CueStrikeCharacterManager.Instance;
            if (manager == null)
            {
                Debug.LogError("[CharacterSelectorUI] CharacterManager not found.");
                return;
            }

            var characters = manager.GetAllCharacters();
            foreach (var character in characters)
            {
                if (character == null) continue;
                CreateCharacterButton(character);
            }

            // Select first unlocked
            var firstUnlocked = characters.Find(c => c != null && c.isUnlocked);
            if (firstUnlocked != null)
                HighlightCharacter(firstUnlocked);
        }

        private void CreateCharacterButton(Characters.CueStrikeCharacterData character)
        {
            if (_characterButtonPrefab == null || _characterButtonContainer == null) return;

            GameObject btn = Instantiate(_characterButtonPrefab, _characterButtonContainer);
            _buttonInstances.Add(btn);

            // Portrait
            Image portrait = btn.GetComponentInChildren<Image>();
            if (portrait != null && character.portrait != null)
                portrait.sprite = character.portrait;

            // Lock overlay
            if (!character.isUnlocked)
            {
                portrait.color = _lockedColor;
                Text lockText = btn.GetComponentInChildren<Text>();
                if (lockText != null) lockText.text = "[LOCKED]";
            }

            // Button click
            Button button = btn.GetComponent<Button>();
            if (button != null)
            {
                button.onClick.AddListener(() => HighlightCharacter(character));
            }

            if (_animator != null)
            {
                _animator.ScaleIn(btn.transform, 0.2f);
            }
        }

        private void HighlightCharacter(Characters.CueStrikeCharacterData character)
        {
            _highlightedCharacter = character;

            if (_portraitImage != null && character.portrait != null)
                _portraitImage.sprite = character.portrait;

            if (_nameText != null)
                _nameText.text = character.displayName;

            if (_descriptionText != null)
                _descriptionText.text = character.description;

            if (_abilityNameText != null)
                _abilityNameText.text = character.abilityName;

            if (_abilityDescriptionText != null)
                _abilityDescriptionText.text = character.abilityDescription;

            // Enable/disable select button
            if (_selectButton != null)
                _selectButton.interactable = character.isUnlocked;

            Debug.Log($"[CharacterSelectorUI] Highlighted: {character.displayName}");
        }

        private void OnSelectClicked()
        {
            if (_highlightedCharacter == null) return;

            var manager = Characters.CueStrikeCharacterManager.Instance;
            if (manager != null)
            {
                manager.SelectCharacter(_highlightedCharacter.characterId);
                Debug.Log($"[CharacterSelectorUI] Selected: {_highlightedCharacter.displayName}");
            }

            // Close selector
            gameObject.SetActive(false);
        }

        private void OnBackClicked()
        {
            gameObject.SetActive(false);
        }

        #region Self-Test
        public bool RunSelfTest()
        {
            bool pass = true;
            if (_characterButtonPrefab == null)
            {
                Debug.LogError("[Self-Test] CharacterSelectorUI: Button prefab not assigned.");
                pass = false;
            }
            if (_characterButtonContainer == null)
            {
                Debug.LogError("[Self-Test] CharacterSelectorUI: Button container not assigned.");
                pass = false;
            }
            Debug.Log($"[Self-Test] CharacterSelectorUI: {(pass ? "PASS" : "FAIL")}");
            return pass;
        }
        #endregion
    }
}