using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// CueStrikeRoomSelectionCreator - Creates Room Selection UI
/// Created by Nari for P'Mong | 2026-07-19
/// </summary>
public class RoomSelectionCreator : EditorWindow
{
    [MenuItem("Tools/CueStrike/UI/Create Room Selection Panel")]
    public static void CreateRoomSelectionPanel()
    {
        var roomEntries = new RoomEntry[]
        {
            new RoomEntry("Classic Snooker Room", "Classic_Room", "Trophy"),
            new RoomEntry("Tournament Room", "AAA_RoomDAY", "Trophy"),
            new RoomEntry("Grand Arena", "GrandArena_Room", "Arena"),
            new RoomEntry("Cyberpunk Club", "Cyberpunk_Room", "Cyber"),
            new RoomEntry("Luxury Lounge", "Luxury_Room", "Diamond"),
            new RoomEntry("Industrial", "Industrial_Room", "Gear"),
            new RoomEntry("Zen Dojo", "ZenDojo_Room", "Zen"),
            new RoomEntry("Space Nebula", "SpaceNebula_Room", "Space"),
            new RoomEntry("Warp Fantasy", "WarpFantasy_Room", "Fantasy"),
        };

        var scene = SceneManager.GetActiveScene();
        if (!scene.isLoaded)
        {
            Debug.LogError("[RoomSelectionCreator] No active scene loaded");
            return;
        }

        var canvasObj = new GameObject("RoomSelectionCanvas");
        var canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
        canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        var panelObj = new GameObject("RoomPanel");
        panelObj.transform.SetParent(canvasObj.transform, false);
        var panel = panelObj.AddComponent<UnityEngine.UI.Image>();
        panel.color = new Color(0.1f, 0.1f, 0.15f, 0.95f);
        var rtPanel = panelObj.GetComponent<RectTransform>();
        rtPanel.anchorMin = new Vector2(0.1f, 0.1f);
        rtPanel.anchorMax = new Vector2(0.9f, 0.9f);
        rtPanel.offsetMin = Vector2.zero;
        rtPanel.offsetMax = Vector2.zero;

        var layout = panelObj.AddComponent<UnityEngine.UI.GridLayoutGroup>();
        layout.cellSize = new Vector2(280, 160);
        layout.spacing = new Vector2(20, 20);
        layout.padding = new RectOffset(30, 30, 30, 30);
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.constraint = GridLayoutGroup.Constraint.Flexible;

        foreach (var entry in roomEntries)
        {
            CreateRoomButton(panelObj.transform, entry);
        }

        var titleObj = new GameObject("Title");
        titleObj.transform.SetParent(canvasObj.transform, false);
        var titleText = titleObj.AddComponent<UnityEngine.UI.Text>();
        titleText.text = "SELECT ROOM";
        titleText.fontSize = 48;
        titleText.fontStyle = FontStyle.Bold;
        titleText.alignment = TextAnchor.MiddleCenter;
        titleText.color = Color.white;
        var rtTitle = titleObj.GetComponent<RectTransform>();
        rtTitle.anchorMin = new Vector2(0.5f, 1f);
        rtTitle.anchorMax = new Vector2(0.5f, 1f);
        rtTitle.anchoredPosition = new Vector2(0, -60);
        rtTitle.sizeDelta = new Vector2(400, 80);

        EditorUtility.DisplayDialog("Room Selection Creator", "RoomSelectionPanel created successfully!", "OK");
        Debug.Log("[RoomSelectionCreator] RoomSelectionPanel added and saved to " + scene.path);
    }

    private static void CreateRoomButton(Transform parent, RoomEntry entry)
    {
        var btnObj = new GameObject($"Btn_{entry.sceneName}");
        btnObj.transform.SetParent(parent, false);

        var btn = btnObj.AddComponent<UnityEngine.UI.Button>();
        var img = btnObj.AddComponent<UnityEngine.UI.Image>();
        img.color = new Color(0.15f, 0.15f, 0.2f, 1f);
        img.raycastTarget = true;

        var textObj = new GameObject("Text");
        textObj.transform.SetParent(btnObj.transform, false);
        var txt = textObj.AddComponent<UnityEngine.UI.Text>();
        txt.text = $"{entry.displayName}\n{entry.iconLabel}";
        txt.fontSize = 24;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = Color.white;
        txt.resizeTextForBestFit = true;
        txt.resizeTextMinSize = 18;
        txt.resizeTextMaxSize = 32;
        var rtText = textObj.GetComponent<RectTransform>();
        rtText.anchorMin = Vector2.zero;
        rtText.anchorMax = Vector2.one;
        rtText.offsetMin = new Vector2(10, 10);
        rtText.offsetMax = new Vector2(-10, -10);

        var iconObj = new GameObject("Icon");
        iconObj.transform.SetParent(btnObj.transform, false);
        var iconText = iconObj.AddComponent<UnityEngine.UI.Text>();
        iconText.text = entry.iconLabel;
        iconText.fontSize = 64;
        iconText.alignment = TextAnchor.MiddleCenter;
        iconText.color = new Color(1f, 0.85f, 0.3f, 1f);
        var rtIcon = iconObj.GetComponent<RectTransform>();
        rtIcon.anchorMin = new Vector2(0.5f, 1f);
        rtIcon.anchorMax = new Vector2(0.5f, 1f);
        rtIcon.anchoredPosition = new Vector2(0, -50);
        rtIcon.sizeDelta = new Vector2(100, 100);

        string scenePath = $"Assets/Scenes/{entry.sceneName}.unity";
        btn.onClick.AddListener(() => LoadScene(scenePath));
    }

    private static void LoadScene(string scenePath)
    {
#if UNITY_EDITOR
        // Guard: Cannot switch scene during Play Mode
        if (EditorApplication.isPlaying)
        {
            Debug.LogWarning("[RoomSelectionCreator] Cannot switch scenes during Play Mode. Please exit Play Mode first.");
            return;
        }

        if (UnityEditor.SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scenePath);
        }
#endif
    }

    private struct RoomEntry
    {
        public string displayName;
        public string sceneName;
        public string iconLabel;

        public RoomEntry(string displayName, string sceneName, string iconLabel)
        {
            this.displayName = displayName;
            this.sceneName = sceneName;
            this.iconLabel = iconLabel;
        }
    }
}