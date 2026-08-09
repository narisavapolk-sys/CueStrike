#if CUESTRIKE_NORMCORE
using UnityEngine;
using Normal.Realtime;

namespace CueStrike.Multiplayer
{
    /// <summary>
    /// Manages connections to Normcore servers and houses matchmaking options.
    /// Easily extendable to support custom server regions, matchmaking lobbies, or voice chat.
    /// </summary>
    [RequireComponent(typeof(Realtime))]
    public class CueStrikeNormcoreManager : MonoBehaviour
    {
        public static CueStrikeNormcoreManager Instance { get; private set; }

        [Header("Matchmaking Settings")]
        [Tooltip("The name of the room to join for quick matchmaking.")]
        public string defaultRoomName = "CueStrike_Lobby";
        
        private Realtime _realtime;

        public bool IsConnected => _realtime != null && _realtime.connected;
        public string ActiveRoomName => _realtime != null ? _realtime.roomName : string.Empty;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            _realtime = GetComponent<Realtime>();
        }

        private void Start()
        {
            // Auto-connect to default lobby on startup (optional)
            ConnectToLobby();
        }

        /// <summary>
        /// Connects to a default room for quick matchmaking.
        /// </summary>
        public void ConnectToLobby()
        {
            ConnectToRoom(defaultRoomName);
        }

        /// <summary>
        /// Connects to a specific room by name (supports private rooms with codes).
        /// </summary>
        public void ConnectToRoom(string roomName)
        {
            if (_realtime == null) return;
            
            if (_realtime.connected)
            {
                _realtime.Disconnect();
            }

            Debug.Log($"[CueStrike Multiplayer] Connecting to room: {roomName}");
            _realtime.Connect(roomName);
        }

        /// <summary>
        /// Disconnects from the current server room.
        /// </summary>
        public void Disconnect()
        {
            if (_realtime != null && _realtime.connected)
            {
                _realtime.Disconnect();
            }
        }
    }
}
#else
using UnityEngine;

namespace CueStrike.Multiplayer
{
    /// <summary>
    /// Fallback script to explain Normcore setup inside the inspector when SDK is not present.
    /// </summary>
    public class CueStrikeNormcoreManager : MonoBehaviour
    {
        [Header("Normcore SDK Missing")]
        [TextArea(4, 10)]
        public string setupInstructions = "Normcore (Normal VR) SDK is currently not imported in this project.\n\n" +
                                          "To enable Multiplayer:\n" +
                                          "1. Download & Import the 'Normcore' package into your Assets.\n" +
                                          "2. Go to Edit > Project Settings > Player > Scripting Define Symbols.\n" +
                                          "3. Add 'CUESTRIKE_NORMCORE' to the list and click Apply.\n" +
                                          "4. This script will automatically activate and compile the Normcore Multiplayer features.";
    }
}
#endif
