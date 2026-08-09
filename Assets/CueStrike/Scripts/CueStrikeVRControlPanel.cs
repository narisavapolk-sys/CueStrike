using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.UI;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Haptics;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.Events;
using Unity.XR.CoreUtils;
using UnityEngine.XR;
using UnityEngine.XR.Management;
using CueStrike.Accessibility;
using CueStrike.Visuals;
using CueStrike.VR.Input;

/// <summary>
/// VR Control Panel - Wrist-mounted UI for game controls
/// Attaches to left hand controller with auto-build UI
/// </summary>
[RequireComponent(typeof(Canvas))]
public class CueStrikeVRControlPanel : MonoBehaviour
{
    [Header("Manager References")]
    [SerializeField] public CueStrikeAccessibilityManager accessibility;
    [SerializeField] public CueStrikeNoirMode noirMode;
    [SerializeField] public CueStrikeBallLabels ballLabels;

    [Header("UI Settings")]
    [SerializeField] private bool autoBuildUi = true;
    [SerializeField] private float panelWidth = 0.2f;
    [SerializeField] private float panelHeight = 0.3f;
    [SerializeField] private float buttonHeight = 0.06f;
    [SerializeField] private float buttonSpacing = 0.01f;

    [Header("Visual")]
    [SerializeField] private Color panelBackgroundColor = new Color(0.1f, 0.1f, 0.15f, 0.9f);
    [SerializeField] private Color buttonNormalColor = new Color(0.2f, 0.2f, 0.3f, 1f);
    [SerializeField] private Color buttonHoverColor = new Color(0.3f, 0.5f, 0.8f, 1f);
    [SerializeField] private Color buttonPressedColor = new Color(0.1f, 0.3f, 0.6f, 1f);
    [SerializeField] private Color textColor = Color.white;
    [SerializeField] private int fontSize = 18;

    private Canvas canvas;
    private RectTransform panelRect;
    private VerticalLayoutGroup layoutGroup;
    private GameObject contentContainer;

    // Events for external listening
    public UnityEvent OnNoirToggled;
    public UnityEvent OnLabelsToggled;
    public UnityEvent OnHighContrastToggled;
    public UnityEvent OnReduceMotionToggled;
    public UnityEvent OnOneHandedToggled;

    private void Awake()
    {
        canvas = GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
    }

    private void Start()
    {
        if (autoBuildUi)
        {
            BuildUI();
        }

        // Auto-find managers if not assigned
        if (accessibility == null) accessibility = FindAnyObjectByType<CueStrikeAccessibilityManager>();
        if (noirMode == null) noirMode = FindAnyObjectByType<CueStrikeNoirMode>();
        if (ballLabels == null) ballLabels = FindAnyObjectByType<CueStrikeBallLabels>();
    }

    /// <summary>
    /// Build the VR Control Panel UI programmatically
    /// </summary>
    public void BuildUI()
    {
        // Setup canvas
        var rt = canvas.GetComponent<RectTransform>();
        if (rt == null) rt = canvas.gameObject.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(panelWidth * 1000f, panelHeight * 1000f); // World space to pixels

        // Panel background
        var panelImage = canvas.gameObject.GetComponent<Image>();
        if (panelImage == null) panelImage = canvas.gameObject.AddComponent<Image>();
        panelImage.color = panelBackgroundColor;
        panelImage.raycastTarget = true;

        // Add rounded corners via mask
        var mask = canvas.gameObject.GetComponent<Mask>();
        if (mask == null) mask = canvas.gameObject.AddComponent<Mask>();
        mask.showMaskGraphic = false;

        // Content container with VerticalLayoutGroup
        contentContainer = new GameObject("Content");
        contentContainer.transform.SetParent(canvas.transform, false);

        var contentRect = contentContainer.AddComponent<RectTransform>();
        contentRect.anchorMin = Vector2.zero;
        contentRect.anchorMax = Vector2.one;
        contentRect.offsetMin = new Vector2(0.01f * 1000f, 0.01f * 1000f);
        contentRect.offsetMax = new Vector2(-0.01f * 1000f, -0.01f * 1000f);

        layoutGroup = contentContainer.AddComponent<VerticalLayoutGroup>();
        layoutGroup.spacing = buttonSpacing * 1000f;
        layoutGroup.padding = new RectOffset(
            Mathf.RoundToInt(0.005f * 1000f),
            Mathf.RoundToInt(0.005f * 1000f),
            Mathf.RoundToInt(0.005f * 1000f),
            Mathf.RoundToInt(0.005f * 1000f)
        );
        layoutGroup.childAlignment = TextAnchor.UpperCenter;
        layoutGroup.childForceExpandWidth = true;
        layoutGroup.childForceExpandHeight = false;
        layoutGroup.childControlWidth = true;
        layoutGroup.childControlHeight = true;

        var contentSizeFitter = contentContainer.AddComponent<ContentSizeFitter>();
        contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Build buttons
        CreateButton("Noir Mode", ToggleNoirMode, noirMode?.NoirEnabled == true);
        CreateButton("Ball Labels", ToggleBallLabels, ballLabels != null);
        CreateButton("High Contrast", ToggleHighContrast, accessibility?.HighContrastMode == true);
        CreateButton("Reduce Motion", ToggleReduceMotion, accessibility?.ReduceMotion == true);
        CreateButton("One-Handed Mode", ToggleOneHanded, accessibility?.OneHandedMode == true);
        CreateSeparator();
        CreateButton("Stance: Toggle", ToggleVrStance);
        CreateButton("Reset Accessibility", ResetAccessibility);
        CreateButton("Recenter View", RecenterView);

        Debug.Log("[CueStrikeVRControlPanel] UI Built successfully");
    }

    private GameObject CreateButton(string labelText, UnityAction onClick, bool isOn = false)
    {
        var buttonObj = new GameObject($"Btn_{labelText.Replace(" ", "")}");
        buttonObj.transform.SetParent(contentContainer.transform, false);

        var buttonRect = buttonObj.AddComponent<RectTransform>();
        buttonRect.sizeDelta = new Vector2(0, buttonHeight * 1000f);

        var buttonImage = buttonObj.AddComponent<Image>();
        buttonImage.color = isOn ? buttonHoverColor : buttonNormalColor;
        buttonImage.raycastTarget = true;

        var button = buttonObj.AddComponent<Button>();
        button.targetGraphic = buttonImage;
        button.transition = Selectable.Transition.ColorTint;

        var colors = button.colors;
        colors.normalColor = isOn ? buttonHoverColor : buttonNormalColor;
        colors.highlightedColor = buttonHoverColor;
        colors.pressedColor = buttonPressedColor;
        colors.selectedColor = buttonHoverColor;
        colors.disabledColor = Color.gray;
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.1f;
        button.colors = colors;

        button.onClick.AddListener(onClick);

        // Button text
        var textObj = new GameObject("Text");
        textObj.transform.SetParent(buttonObj.transform, false);

        var textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        var text = textObj.AddComponent<Text>();
        text.text = labelText;
        text.fontSize = fontSize;
        text.color = textColor;
        text.alignment = TextAnchor.MiddleCenter;
        text.fontStyle = FontStyle.Bold;
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = 12;
        text.resizeTextMaxSize = 24;

        // Add XR Simple Interactable for ray interaction
        var interactable = buttonObj.AddComponent<XRSimpleInteractable>();
        // Note: In XRIT 3.x, UI buttons work with XRRayInteractor automatically via EventSystem

        return buttonObj;
    }

    private void CreateSeparator()
    {
        var sepObj = new GameObject("Separator");
        sepObj.transform.SetParent(contentContainer.transform, false);

        var sepRect = sepObj.AddComponent<RectTransform>();
        sepRect.sizeDelta = new Vector2(0, 2f);

        var sepImage = sepObj.AddComponent<Image>();
        sepImage.color = new Color(1, 1, 1, 0.2f);
        sepImage.raycastTarget = false;

        var layoutElement = sepObj.AddComponent<LayoutElement>();
        layoutElement.preferredHeight = 2f;
    }

    // Button Actions
    private void ToggleNoirMode()
    {
        if (noirMode != null)
        {
            noirMode.ToggleNoirMode();
            OnNoirToggled?.Invoke();
        }
    }

    private void ToggleBallLabels()
    {
        if (ballLabels != null)
        {
            ballLabels.SetLabelsVisible(!ballLabels.ShowLabels);
            OnLabelsToggled?.Invoke();
        }
    }

    private void ToggleHighContrast()
    {
        if (accessibility != null)
        {
            accessibility.ToggleHighContrast();
            OnHighContrastToggled?.Invoke();
        }
    }

    private void ToggleReduceMotion()
    {
        if (accessibility != null)
        {
            accessibility.ToggleReduceMotion();
            OnReduceMotionToggled?.Invoke();
        }
    }

    private void ToggleOneHanded()
    {
        if (accessibility != null)
        {
            accessibility.ToggleOneHandedMode();
            OnOneHandedToggled?.Invoke();
        }
    }

    private void ResetAccessibility()
    {
        if (accessibility != null)
        {
            accessibility.ResetToDefaults();
        }
    }

    private void RecenterView()
    {
        var xrOrigin = FindAnyObjectByType<Unity.XR.CoreUtils.XROrigin>();
        if (xrOrigin != null)
        {
            // For OpenXR, we can use XRInputSubsystem to recenter
            var xrInputSubsystem = UnityEngine.XR.Management.XRGeneralSettings.Instance?.Manager?.activeLoader?.GetLoadedSubsystem<UnityEngine.XR.XRInputSubsystem>();
            if (xrInputSubsystem != null)
            {
                xrInputSubsystem.TryRecenter();
            }
            Debug.Log("[CueStrikeVRControlPanel] View recentered");
        }
    }

    private void ToggleVrStance()
    {
        var inputManager = CueStrikeVRInputManager.Instance;
        if (inputManager != null && inputManager.StanceController != null)
        {
            var newStance = inputManager.StanceController.IsCrouching
                ? CueStrikeStanceController.StanceType.Standing
                : CueStrikeStanceController.StanceType.Crouching;
            inputManager.StanceController.SetStance(newStance);
            Debug.Log($"[CueStrikeVRControlPanel] Stance toggled to {newStance}");
        }
        else
        {
            Debug.LogWarning("[CueStrikeVRControlPanel] VRInputManager or StanceController not available.");
        }
    }

    /// <summary>
    /// Update button visual states based on current settings
    /// </summary>
    public void RefreshButtonStates()
    {
        if (contentContainer == null) return;

        var buttons = contentContainer.GetComponentsInChildren<Button>();
        foreach (var btn in buttons)
        {
            var text = btn.GetComponentInChildren<Text>();
            if (text == null) continue;

            Color targetColor = buttonNormalColor;
            bool isOn = false;

            switch (text.text)
            {
                case "Noir Mode":
                    isOn = noirMode?.NoirEnabled == true;
                    break;
                case "High Contrast":
                    isOn = accessibility?.HighContrastMode == true;
                    break;
                case "Reduce Motion":
                    isOn = accessibility?.ReduceMotion == true;
                    break;
                case "One-Handed Mode":
                    isOn = accessibility?.OneHandedMode == true;
                    break;
            }

            if (isOn) targetColor = buttonHoverColor;

            var colors = btn.colors;
            colors.normalColor = targetColor;
            colors.selectedColor = targetColor;
            btn.colors = colors;
        }
    }

    private void Update()
    {
        // Refresh button states periodically
        if (Time.frameCount % 60 == 0)
        {
            RefreshButtonStates();
        }
    }
}