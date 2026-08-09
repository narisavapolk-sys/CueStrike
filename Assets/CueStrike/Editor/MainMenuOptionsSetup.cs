#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using CueStrike;

/// <summary>
/// Editor script to setup options UI panel in MainMenu.unity.
/// Menu: CueStrike → Generate → Set Up Options Panel in MainMenu
/// </summary>
public static class MainMenuOptionsSetup
{
    private const string ScenePath = "Assets/CueStrike/Scenes/MainMenu.unity";

    [MenuItem("CueStrike/Generate/Set Up Options Panel in MainMenu")]
    public static void SetupOptions()
    {
        // Guard: Cannot run in Play Mode
        if (EditorApplication.isPlaying)
        {
            Debug.LogError("[Options Setup] Cannot setup while in Play Mode. Please exit Play Mode first.");
            EditorUtility.DisplayDialog("Cannot Setup", "Stop Play Mode first!", "OK");
            return;
        }

        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        if (!scene.IsValid())
        {
            Debug.LogError($"[Options Setup] Could not open scene: {ScenePath}");
            return;
        }

        var canvasGO = GameObject.Find("MenuCanvas");
        if (canvasGO == null)
        {
            Debug.LogError("[Options Setup] 'MenuCanvas' not found in MainMenu scene.");
            return;
        }

        var optionsPanel = canvasGO.transform.Find("OptionsPanel");
        if (optionsPanel == null)
        {
            Debug.LogWarning("[Options Setup] 'OptionsPanel' not found under MenuCanvas. Creating it now...");
            var optionsGO = new GameObject("OptionsPanel", typeof(RectTransform), typeof(Image));
            optionsGO.transform.SetParent(canvasGO.transform, false);
            optionsPanel = optionsGO.transform;

            var rt = optionsGO.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var bg = optionsGO.GetComponent<Image>();
            bg.color = new Color(0.04f, 0.1f, 0.12f, 0.98f);
            bg.raycastTarget = true;

            optionsGO.SetActive(false);
            Debug.Log("[Options Setup] Created OptionsPanel under MenuCanvas.");
        }

        // Clean existing children first to rebuild fresh Options UI
        for (int i = optionsPanel.childCount - 1; i >= 0; i--)
        {
            var child = optionsPanel.GetChild(i);
            if (child.name != "CloseOptionsButton" && child.name != "Title")
            {
                Object.DestroyImmediate(child.gameObject);
            }
        }

        // Setup background color
        var bgImg = optionsPanel.GetComponent<Image>();
        if (bgImg != null) bgImg.color = new Color(0.04f, 0.1f, 0.12f, 0.98f);

        // ── Rebuild Settings Options ──

        float startY = 160f;
        float rowSpacing = -70f;

        // 1. Master Volume Row (Slider)
        var volRow = CreateLabel(optionsPanel, "Master Volume", -220f, startY);
        var volSlider = CreateSlider(optionsPanel, 100f, startY, 0f, 1f, 1.0f);

        // 2. Comfort Vignette Row (Toggle)
        var vignetteRow = CreateLabel(optionsPanel, "Comfort Vignette", -220f, startY + rowSpacing);
        var vignetteToggle = CreateToggle(optionsPanel, 100f, startY + rowSpacing, true);

        // 3. Turn Mode Row (Dropdown: Snap/Smooth)
        var turnRow = CreateLabel(optionsPanel, "Locomotion Turn", -220f, startY + rowSpacing * 2);
        var turnDropdown = CreateDropdown(optionsPanel, 100f, startY + rowSpacing * 2, new string[] { "Snap Turn", "Smooth Turn" });

        // 4. Dominant Hand Row (Dropdown: Right/Left)
        var handRow = CreateLabel(optionsPanel, "Dominant Hand", -220f, startY + rowSpacing * 3);
        var handDropdown = CreateDropdown(optionsPanel, 100f, startY + rowSpacing * 3, new string[] { "Right Hand", "Left Hand" });

        // ── Write UI Controller Binding Script ──
        var controllerGO = GameObject.Find("MainMenuUIController");
        if (controllerGO != null)
        {
            var menuController = controllerGO.GetComponent<MainMenuUIController>();
            if (menuController != null)
            {
                // We'll create a settings hook script on MainMenuUIController or create a separate one
                var settingsHook = controllerGO.GetComponent<CueStrikeSettingsUIHook>();
                if (settingsHook == null) settingsHook = controllerGO.AddComponent<CueStrikeSettingsUIHook>();

                settingsHook.volumeSlider = volSlider;
                settingsHook.comfortToggle = vignetteToggle;
                settingsHook.turnDropdown = turnDropdown;
                settingsHook.handDropdown = handDropdown;

                EditorUtility.SetDirty(settingsHook);
                Debug.Log("[Options Setup] Automatically bound UI controls to CueStrikeSettingsUIHook.");
            }
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Options Panel Rebuilt",
            "Options settings panel successfully generated and configured!\n\n" +
            "Created Controls:\n" +
            "  • Master Volume Slider\n" +
            "  • Comfort Vignette Toggle\n" +
            "  • Turn Mode Dropdown (Snap vs Smooth)\n" +
            "  • Dominant Hand Dropdown (Right vs Left)",
            "OK");
    }

    private static GameObject CreateLabel(Transform parent, string text, float x, float y)
    {
        var labelGO = new GameObject(text + "_Label", typeof(Text));
        labelGO.transform.SetParent(parent, false);
        var rect = labelGO.GetComponent<RectTransform>();
        rect.anchoredPosition = new Vector2(x, y);
        rect.sizeDelta = new Vector2(250, 40);

        var txt = labelGO.GetComponent<Text>();
        txt.text = text;
        txt.fontSize = 22;
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.color = Color.white;
        txt.alignment = TextAnchor.MiddleLeft;

        return labelGO;
    }

    private static Slider CreateSlider(Transform parent, float x, float y, float min, float max, float val)
    {
        var sliderGO = new GameObject("Volume_Slider", typeof(Slider));
        sliderGO.transform.SetParent(parent, false);
        var rect = sliderGO.GetComponent<RectTransform>();
        rect.anchoredPosition = new Vector2(x, y);
        rect.sizeDelta = new Vector2(250, 24);

        var slider = sliderGO.GetComponent<Slider>();
        slider.minValue = min;
        slider.maxValue = max;
        slider.value = val;

        // Background
        var bg = new GameObject("Background", typeof(Image));
        bg.transform.SetParent(sliderGO.transform, false);
        bg.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.1f);
        var bgRect = bg.GetComponent<RectTransform>();
        bgRect.anchorMin = new Vector2(0f, 0.25f);
        bgRect.anchorMax = new Vector2(1f, 0.75f);
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;

        // Fill Area
        var fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(sliderGO.transform, false);
        var faRect = fillArea.AddComponent<RectTransform>();
        faRect.anchorMin = new Vector2(0f, 0.25f);
        faRect.anchorMax = new Vector2(1f, 0.75f);
        faRect.offsetMin = new Vector2(5f, 0f);
        faRect.offsetMax = new Vector2(-5f, 0f);

        var fill = new GameObject("Fill", typeof(Image));
        fill.transform.SetParent(fillArea.transform, false);
        fill.GetComponent<Image>().color = new Color(0f, 0.8f, 0.4f); // Neon green
        var fillRect = fill.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = new Vector2(0.5f, 1f); // default half
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
        slider.fillRect = fillRect;

        // Handle Slide Area
        var handleArea = new GameObject("Handle Slide Area");
        handleArea.transform.SetParent(sliderGO.transform, false);
        var haRect = handleArea.AddComponent<RectTransform>();
        haRect.anchorMin = Vector2.zero;
        haRect.anchorMax = Vector2.one;
        haRect.offsetMin = new Vector2(10f, 0f);
        haRect.offsetMax = new Vector2(-10f, 0f);

        var handle = new GameObject("Handle", typeof(Image));
        handle.transform.SetParent(handleArea.transform, false);
        handle.GetComponent<Image>().color = Color.white;
        var hRect = handle.GetComponent<RectTransform>();
        hRect.sizeDelta = new Vector2(20, 20);
        slider.handleRect = hRect;

        return slider;
    }

    private static Toggle CreateToggle(Transform parent, float x, float y, bool active)
    {
        var toggleGO = new GameObject("Vignette_Toggle", typeof(Toggle));
        toggleGO.transform.SetParent(parent, false);
        var rect = toggleGO.GetComponent<RectTransform>();
        rect.anchoredPosition = new Vector2(x, y);
        rect.sizeDelta = new Vector2(40, 40);

        var toggle = toggleGO.GetComponent<Toggle>();
        toggle.isOn = active;

        // Background
        var bg = new GameObject("Background", typeof(Image));
        bg.transform.SetParent(toggleGO.transform, false);
        bg.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.1f);
        var bgRect = bg.GetComponent<RectTransform>();
        bgRect.sizeDelta = new Vector2(30, 30);

        // Checkmark
        var checkmark = new GameObject("Checkmark", typeof(Image));
        checkmark.transform.SetParent(bg.transform, false);
        checkmark.GetComponent<Image>().color = new Color(0f, 0.8f, 0.4f);
        var cmRect = checkmark.GetComponent<RectTransform>();
        cmRect.sizeDelta = new Vector2(20, 20);
        toggle.graphic = checkmark.GetComponent<Image>();

        return toggle;
    }

    private static Dropdown CreateDropdown(Transform parent, float x, float y, string[] options)
    {
        var dropdownGO = new GameObject("Setting_Dropdown", typeof(Image), typeof(Dropdown));
        dropdownGO.transform.SetParent(parent, false);
        var rect = dropdownGO.GetComponent<RectTransform>();
        rect.anchoredPosition = new Vector2(x, y);
        rect.sizeDelta = new Vector2(250, 40);

        var bgImg = dropdownGO.GetComponent<Image>();
        bgImg.color = new Color(1f, 1f, 1f, 0.1f);

        var dropdown = dropdownGO.GetComponent<Dropdown>();

        // Label (Displays active choice)
        var labelGO = new GameObject("Label", typeof(Text));
        labelGO.transform.SetParent(dropdownGO.transform, false);
        var labelRect = labelGO.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(10f, 0f);
        labelRect.offsetMax = new Vector2(-10f, 0f);

        var txt = labelGO.GetComponent<Text>();
        txt.alignment = TextAnchor.MiddleLeft;
        txt.fontSize = 20;
        txt.color = Color.white;
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        dropdown.captionText = txt;

        foreach (var opt in options)
        {
            dropdown.options.Add(new Dropdown.OptionData(opt));
        }

        return dropdown;
    }
}
#endif
