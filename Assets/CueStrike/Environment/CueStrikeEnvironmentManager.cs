using UnityEngine;
using CueStrike.Environment;
using CueStrike.Gameplay;
using CueStrike.Environment.Lighting;

public enum CueStrikeEnvMode { VR, MR }

public class CueStrikeEnvironmentManager : MonoBehaviour
{
    public static CueStrikeEnvironmentManager Instance { get; private set; }
    // Reference to the lighting manager that handles per‑room lighting
    private RoomLightingManager roomLightingManager;

    public CueStrikeEnvMode mode = CueStrikeEnvMode.VR;
    public GameObject vrRoom; // virtual room root
    public GameObject mrTable; // MR table root (shown in MR)

    void Awake()
    {
        if (Instance != null && Instance != this) Destroy(this);
        Instance = this;
        // Ensure BallMaterialAssigner is attached to assign ball textures at runtime
        if (gameObject.GetComponent<BallMaterialAssigner>() == null)
        {
            gameObject.AddComponent<BallMaterialAssigner>();
        }
        // Locate the RoomLightingManager in the scene (if any)
        roomLightingManager = FindObjectOfType<RoomLightingManager>();
    }

        public void SetMode(CueStrikeEnvMode newMode)
        {
            mode = newMode;
            if (mode == CueStrikeEnvMode.VR)
            {
                if (vrRoom != null) vrRoom.SetActive(true);
                if (mrTable != null) mrTable.SetActive(false);
                DisablePassthrough();
            }
            else
            {
                if (vrRoom != null) vrRoom.SetActive(false);
                if (mrTable != null) mrTable.SetActive(true);
                EnablePassthrough();
            }

            // When changing environment mode, also set a default room lighting profile
            // (e.g., first room). Adjust as needed for your level design.
            if (roomLightingManager != null)
            {
                roomLightingManager.SetRoom(0);
            }

            Debug.Log($"CueStrikeEnvironmentManager: Mode set to {mode}");
        }

    private void EnablePassthrough()
    {
        var mrMgr = CueStrikeMRPassthroughManager.Instance;
        if (mrMgr != null)
        {
            mrMgr.EnablePassthrough();
        }
        else
        {
            Debug.LogWarning("[Environment] CueStrikeMRPassthroughManager not found. Passthrough not available.");
        }
    }

    private void DisablePassthrough()
    {
        var mrMgr = CueStrikeMRPassthroughManager.Instance;
        if (mrMgr != null)
        {
            mrMgr.DisablePassthrough();
        }
    }
}
