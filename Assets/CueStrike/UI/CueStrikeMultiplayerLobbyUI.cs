using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace CueStrike.UI
{
    /// <summary>
    /// Multiplayer Lobby UI for CueStrike.
    /// Allows joining/creating rooms, viewing players, and configuring match settings.
    /// Works with CueStrikeNormcoreManager for Normcore-based multiplayer.
    /// </summary>
    public class CueStrikeMultiplayerLobbyUI : MonoBehaviour
    {
        #region Singleton
        public static CueStrikeMultiplayerLobbyUI Instance { get; private set; }
        #endregion

        #region Events
        public event Action OnLobbyOpened;
        public event Action OnLobbyClosed;
        public event Action<string> OnRoomJoined;
        public event Action OnRoomLeft;
        #endregion

        #region UI References
        [Header("Panels")]
        [SerializeField] private GameObject lobbyPanel;
        [SerializeField] private GameObject roomListPanel;
        [SerializeField] private GameObject createRoomPanel;
        [SerializeField] private GameObject inRoomPanel;

        [Header("Room List")]
        [SerializeField] private RectTransform roomListContent;
        [SerializeField] private GameObject roomListItemPrefab;
        [SerializeField] private Button refreshButton;

        [Header("Create Room")]
        [SerializeField] private TMP_InputField roomNameInput;
        [SerializeField] private TMP_Dropdown maxPlayersDropdown;
        [SerializeField] private TMP_Dropdown gameModeDropdown;
        [SerializeField] private Toggle isPrivateToggle;
        [SerializeField] private Button createRoomButton;

        [Header("In Room")]
        [SerializeField] private TextMeshProUGUI roomNameText;
        [SerializeField] private TextMeshProUGUI playerCountText;
        [SerializeField] private RectTransform playerListContent;
        [SerializeField] private GameObject playerListItemPrefab;
        [SerializeField] private Button startGameButton;
        [SerializeField] private Button leaveRoomButton;
        [SerializeField] private Button readyButton;
        [SerializeField] private TextMeshProUGUI readyButtonText;
        [SerializeField] private Image readyButtonImage;

        [Header("Connection Status")]
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private GameObject loadingIndicator;

        [Header("Settings")]
        [SerializeField] private string defaultRoomName = "CueStrike_Lobby";
        [SerializeField] private Color readyColor = Color.green;
        [SerializeField] private Color notReadyColor = Color.gray;

        [Header("Audio")]
        [SerializeField] private Toggle voiceChatToggle;
        [SerializeField] private Slider voiceVolumeSlider;

        #endregion

        #region State
        private bool _isOpen = false;
        private bool _isReady = false;
        private string _currentRoom = "";
        private List<GameObject> _roomListItems = new List<GameObject>();
        private List<GameObject> _playerListItems = new List<GameObject>();
        private string _playerName = "Player";

        // Dummy room data for UI display when Normcore is not compiled
        private List<RoomInfo> _dummyRooms = new List<RoomInfo>();
        #endregion

        #region Room Info
        [Serializable]
        public struct RoomInfo
        {
            public string name;
            public int playerCount;
            public int maxPlayers;
            public string gameMode;
            public bool hasPassword;
        }
        #endregion

        #region Lifecycle
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            InitializeUI();
            GenerateDummyRooms();
            CloseLobby();
        }

        private void InitializeUI()
        {
            // Setup dropdowns
            if (maxPlayersDropdown != null)
            {
                maxPlayersDropdown.ClearOptions();
                maxPlayersDropdown.AddOptions(new List<string> { "2 Players", "4 Players", "6 Players", "8 Players" });
                maxPlayersDropdown.value = 0;
            }

            if (gameModeDropdown != null)
            {
                gameModeDropdown.ClearOptions();
                gameModeDropdown.AddOptions(new List<string> { "8-Ball", "9-Ball", "Chinese 8-Ball", "Snooker" });
                gameModeDropdown.value = 0;
            }

            // Setup buttons
            if (refreshButton != null)
                refreshButton.onClick.AddListener(RefreshRoomList);

            if (createRoomButton != null)
                createRoomButton.onClick.AddListener(CreateRoom);

            if (leaveRoomButton != null)
                leaveRoomButton.onClick.AddListener(LeaveRoom);

            if (startGameButton != null)
                startGameButton.onClick.AddListener(StartGame);

            if (readyButton != null)
                readyButton.onClick.AddListener(ToggleReady);

            // Voice chat
            if (voiceChatToggle != null)
                voiceChatToggle.onValueChanged.AddListener(OnVoiceChatToggled);

            if (voiceVolumeSlider != null)
                voiceVolumeSlider.onValueChanged.AddListener(OnVoiceVolumeChanged);

            // Set player name from prefs
            _playerName = PlayerPrefs.GetString("CueStrike_PlayerName", "Player");
        }

        private void GenerateDummyRooms()
        {
            _dummyRooms = new List<RoomInfo>
            {
                new RoomInfo { name = "CueStrike_Lobby", playerCount = 2, maxPlayers = 4, gameMode = "8-Ball", hasPassword = false },
                new RoomInfo { name = "Nok's Arena", playerCount = 1, maxPlayers = 2, gameMode = "Chinese 8-Ball", hasPassword = false },
                new RoomInfo { name = "Pro Match", playerCount = 2, maxPlayers = 2, gameMode = "9-Ball", hasPassword = true },
                new RoomInfo { name = "Snooker Masters", playerCount = 3, maxPlayers = 6, gameMode = "Snooker", hasPassword = false },
            };
        }
        #endregion

        #region Public API

        /// <summary>
        /// Opens the multiplayer lobby.
        /// </summary>
        public void OpenLobby()
        {
            _isOpen = true;
            if (lobbyPanel != null) lobbyPanel.SetActive(true);
            if (roomListPanel != null) roomListPanel.SetActive(true);
            if (createRoomPanel != null) createRoomPanel.SetActive(false);
            if (inRoomPanel != null) inRoomPanel.SetActive(false);

            RefreshRoomList();
            OnLobbyOpened?.Invoke();
            SetStatus("Connected to lobby server");
        }

        /// <summary>
        /// Closes the multiplayer lobby.
        /// </summary>
        public void CloseLobby()
        {
            _isOpen = false;
            if (lobbyPanel != null) lobbyPanel.SetActive(false);
            OnLobbyClosed?.Invoke();
        }

        /// <summary>
        /// Whether the lobby UI is open.
        /// </summary>
        public bool IsOpen() => _isOpen;

        /// <summary>
        /// Sets the connection status text.
        /// </summary>
        public void SetStatus(string message)
        {
            if (statusText != null)
                statusText.text = message;
        }

        /// <summary>
        /// Shows or hides the loading indicator.
        /// </summary>
        public void ShowLoading(bool show)
        {
            if (loadingIndicator != null)
                loadingIndicator.SetActive(show);
        }

        /// <summary>
        /// Called when joined a room successfully.
        /// </summary>
        public void OnJoinedRoom(string roomName)
        {
            _currentRoom = roomName;
            if (roomListPanel != null) roomListPanel.SetActive(false);
            if (createRoomPanel != null) createRoomPanel.SetActive(false);
            if (inRoomPanel != null) inRoomPanel.SetActive(true);

            if (roomNameText != null) roomNameText.text = $"Room: {roomName}";
            UpdatePlayerList(1);
            SetStatus($"Joined {roomName}");
            OnRoomJoined?.Invoke(roomName);
        }

        /// <summary>
        /// Called when left a room.
        /// </summary>
        public void OnLeftRoom()
        {
            _currentRoom = "";
            if (roomListPanel != null) roomListPanel.SetActive(true);
            if (inRoomPanel != null) inRoomPanel.SetActive(false);

            SetStatus("Left room");
            OnRoomLeft?.Invoke();
        }

        /// <summary>
        /// Updates the player list display.
        /// </summary>
        public void UpdatePlayerList(int playerCount)
        {
            if (playerCountText != null)
                playerCountText.text = $"Players: {playerCount}/?";

            // Clear existing items
            foreach (var item in _playerListItems)
                Destroy(item);
            _playerListItems.Clear();

            // Add player entries
            for (int i = 0; i < playerCount; i++)
            {
                if (playerListItemPrefab != null && playerListContent != null)
                {
                    var item = Instantiate(playerListItemPrefab, playerListContent);
                    var text = item.GetComponentInChildren<TextMeshProUGUI>();
                    if (text != null)
                    {
                        text.text = i == 0 ? $"{_playerName} (You)" : $"Player {i + 1}";
                        if (i == 0)
                            text.color = readyColor;
                    }
                    _playerListItems.Add(item);
                }
            }
        }

        /// <summary>
        /// Sets the player name.
        /// </summary>
        public void SetPlayerName(string name)
        {
            _playerName = name;
            PlayerPrefs.SetString("CueStrike_PlayerName", name);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Gets the current player name.
        /// </summary>
        public string GetPlayerName() => _playerName;

        /// <summary>
        /// Gets the selected room name from input.
        /// </summary>
        public string GetRoomNameInput()
        {
            if (roomNameInput != null && !string.IsNullOrEmpty(roomNameInput.text))
                return roomNameInput.text;
            return defaultRoomName;
        }

        #endregion

        #region UI Handlers

        private void RefreshRoomList()
        {
            ShowLoading(true);
            SetStatus("Refreshing room list...");

            // Clear old items
            foreach (var item in _roomListItems)
                Destroy(item);
            _roomListItems.Clear();

            // Populate with room data
            foreach (var room in _dummyRooms)
            {
                if (roomListItemPrefab != null && roomListContent != null)
                {
                    var item = Instantiate(roomListItemPrefab, roomListContent);
                    var texts = item.GetComponentsInChildren<TextMeshProUGUI>();
                    if (texts.Length >= 3)
                    {
                        texts[0].text = room.name;
                        texts[1].text = $"{room.playerCount}/{room.maxPlayers}";
                        texts[2].text = room.gameMode;
                    }

                    var button = item.GetComponentInChildren<Button>();
                    if (button != null)
                    {
                        string capturedName = room.name;
                        button.onClick.AddListener(() => JoinRoom(capturedName));
                    }

                    _roomListItems.Add(item);
                }
            }

            ShowLoading(false);
            SetStatus($"{_dummyRooms.Count} rooms found");
        }

        private void JoinRoom(string roomName)
        {
            SetStatus($"Joining {roomName}...");
            ShowLoading(true);

            // In production, this calls CueStrikeNormcoreManager.Instance.ConnectToRoom(roomName)
            // For now, simulate with a delay
            Invoke(nameof(SimulateJoin), 0.5f);
        }

        private void SimulateJoin()
        {
            ShowLoading(false);
            OnJoinedRoom(_currentRoom == "" ? defaultRoomName : _currentRoom);
        }

        private void CreateRoom()
        {
            string roomName = GetRoomNameInput();
            SetStatus($"Creating room: {roomName}...");
            ShowLoading(true);

            // In production: CueStrikeNormcoreManager.Instance.CreateRoom(roomName)
            Invoke(nameof(SimulateCreate), 0.5f);
        }

        private void SimulateCreate()
        {
            ShowLoading(false);
            OnJoinedRoom(GetRoomNameInput());
        }

        private void LeaveRoom()
        {
            // In production: CueStrikeNormcoreManager.Instance.Disconnect()
            OnLeftRoom();
        }

        private void StartGame()
        {
            SetStatus("Starting game...");
            // In production: CueStrikeNormcoreManager.Instance.StartGame()
            OnRoomLeft?.Invoke();
        }

        private void ToggleReady()
        {
            _isReady = !_isReady;
            if (readyButtonText != null)
                readyButtonText.text = _isReady ? "Ready!" : "Not Ready";
            if (readyButtonImage != null)
                readyButtonImage.color = _isReady ? readyColor : notReadyColor;
        }

        private void OnVoiceChatToggled(bool enabled)
        {
            Debug.Log($"[MultiplayerLobby] Voice chat: {(enabled ? "ON" : "OFF")}");
        }

        private void OnVoiceVolumeChanged(float volume)
        {
            Debug.Log($"[MultiplayerLobby] Voice volume: {volume:F2}");
        }

        #endregion
    }
}