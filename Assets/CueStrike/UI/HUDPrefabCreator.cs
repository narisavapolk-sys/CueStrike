#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class HUDPrefabCreator
{
    [MenuItem("CueStrike/Generate/HUD Prefab")]
    public static void CreateHUDPrefab()
    {
        var path = "Assets/CueStrike/Prefabs/CueStrikeHUD.prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
        {
            if (!EditorUtility.DisplayDialog("Overwrite HUD?", "HUD prefab already exists. Overwrite?", "Yes", "No")) return;
        }

        var canvasGO = new GameObject("CueStrikeHUD_Canvas");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();

        // Panel
        var panelGO = new GameObject("HUD_Panel");
        panelGO.transform.SetParent(canvasGO.transform, false);
        var img = panelGO.AddComponent<UnityEngine.UI.Image>();
        img.color = new Color(0f, 0f, 0f, 0.35f);
        var rect = panelGO.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 0.72f); // Taller panel for three rows
        rect.anchorMax = new Vector2(1f, 1f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        // --- ROW 1 (y = 0.70f to 0.95f) ---

        // Mode Text
        var modeTextGO = new GameObject("ModeText");
        modeTextGO.transform.SetParent(panelGO.transform, false);
        var modeText = modeTextGO.AddComponent<Text>();
        modeText.text = "Mode: VR";
        modeText.alignment = TextAnchor.MiddleLeft;
        modeText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        var mtRect = modeTextGO.GetComponent<RectTransform>();
        mtRect.anchorMin = new Vector2(0.01f, 0.70f);
        mtRect.anchorMax = new Vector2(0.14f, 0.95f);
        mtRect.offsetMin = Vector2.zero;
        mtRect.offsetMax = Vector2.zero;

        // Cue Select Dropdown
        var cueSelectGO = new GameObject("CueSelect");
        cueSelectGO.transform.SetParent(panelGO.transform, false);
        var dropdown = cueSelectGO.AddComponent<Dropdown>();
        var ddRect = cueSelectGO.GetComponent<RectTransform>();
        ddRect.anchorMin = new Vector2(0.16f, 0.70f);
        ddRect.anchorMax = new Vector2(0.31f, 0.95f);
        ddRect.offsetMin = Vector2.zero;
        ddRect.offsetMax = Vector2.zero;

        // Felt Select Dropdown
        var feltSelectGO = new GameObject("FeltSelect");
        feltSelectGO.transform.SetParent(panelGO.transform, false);
        var feltDropdown = feltSelectGO.AddComponent<Dropdown>();
        var feltOptions = new System.Collections.Generic.List<Dropdown.OptionData>
        {
            new Dropdown.OptionData("Green Velvet"),
            new Dropdown.OptionData("Royal Blue"),
            new Dropdown.OptionData("Cyber Grid"),
            new Dropdown.OptionData("Burgundy Red")
        };
        feltDropdown.options = feltOptions;
        var fdRect = feltSelectGO.GetComponent<RectTransform>();
        fdRect.anchorMin = new Vector2(0.33f, 0.70f);
        fdRect.anchorMax = new Vector2(0.47f, 0.95f);
        fdRect.offsetMin = Vector2.zero;
        fdRect.offsetMax = Vector2.zero;

        // Ball Select Dropdown
        var ballSelectGO = new GameObject("BallSelect");
        ballSelectGO.transform.SetParent(panelGO.transform, false);
        var ballDropdown = ballSelectGO.AddComponent<Dropdown>();
        var ballOptions = new System.Collections.Generic.List<Dropdown.OptionData>
        {
            new Dropdown.OptionData("Classic Resin"),
            new Dropdown.OptionData("Neon Cyber"),
            new Dropdown.OptionData("Gold Marble"),
            new Dropdown.OptionData("Reflective Holo")
        };
        ballDropdown.options = ballOptions;
        var bdRect = ballSelectGO.GetComponent<RectTransform>();
        bdRect.anchorMin = new Vector2(0.49f, 0.70f);
        bdRect.anchorMax = new Vector2(0.63f, 0.95f);
        bdRect.offsetMin = Vector2.zero;
        bdRect.offsetMax = Vector2.zero;

        // Toggle Env Button (Switch)
        var buttonGO = new GameObject("ToggleEnvButton");
        buttonGO.transform.SetParent(panelGO.transform, false);
        var btnImg = buttonGO.AddComponent<UnityEngine.UI.Image>();
        btnImg.color = new Color(1f, 1f, 1f, 0.06f);
        var btn = buttonGO.AddComponent<Button>();
        var btnRect = buttonGO.GetComponent<RectTransform>();
        btnRect.anchorMin = new Vector2(0.91f, 0.70f);
        btnRect.anchorMax = new Vector2(0.99f, 0.95f);
        btnRect.offsetMin = Vector2.zero;
        btnRect.offsetMax = Vector2.zero;

        var btnTextGO = new GameObject("BtnText");
        btnTextGO.transform.SetParent(buttonGO.transform, false);
        var btText = btnTextGO.AddComponent<Text>();
        btText.text = "Switch";
        btText.alignment = TextAnchor.MiddleCenter;
        btText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        var btRect = btnTextGO.GetComponent<RectTransform>();
        btRect.anchorMin = Vector2.zero;
        btRect.anchorMax = Vector2.one;
        btRect.offsetMin = Vector2.zero;
        btRect.offsetMax = Vector2.zero;

        // --- ROW 2 (y = 0.38f to 0.63f) ---

        // Toggle Glove Button
        var gloveButtonGO = new GameObject("ToggleGloveButton");
        gloveButtonGO.transform.SetParent(panelGO.transform, false);
        var gloveBtnImg = gloveButtonGO.AddComponent<UnityEngine.UI.Image>();
        gloveBtnImg.color = new Color(1f, 1f, 1f, 0.06f);
        var gloveBtn = gloveButtonGO.AddComponent<Button>();
        var gloveRect = gloveButtonGO.GetComponent<RectTransform>();
        gloveRect.anchorMin = new Vector2(0.01f, 0.38f);
        gloveRect.anchorMax = new Vector2(0.12f, 0.63f);
        gloveRect.offsetMin = Vector2.zero;
        gloveRect.offsetMax = Vector2.zero;

        var gloveTextGO = new GameObject("GloveText");
        gloveTextGO.transform.SetParent(gloveButtonGO.transform, false);
        var gloveText = gloveTextGO.AddComponent<Text>();
        gloveText.text = "Glove: OFF";
        gloveText.alignment = TextAnchor.MiddleCenter;
        gloveText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        var glRect = gloveTextGO.GetComponent<RectTransform>();
        glRect.anchorMin = Vector2.zero;
        glRect.anchorMax = Vector2.one;
        glRect.offsetMin = Vector2.zero;
        glRect.offsetMax = Vector2.zero;

        // Toggle Aim Assist Button
        var assistButtonGO = new GameObject("ToggleAimAssistButton");
        assistButtonGO.transform.SetParent(panelGO.transform, false);
        var assistBtnImg = assistButtonGO.AddComponent<UnityEngine.UI.Image>();
        assistBtnImg.color = new Color(1f, 1f, 1f, 0.06f);
        var assistBtn = assistButtonGO.AddComponent<Button>();
        var assistRect = assistButtonGO.GetComponent<RectTransform>();
        assistRect.anchorMin = new Vector2(0.14f, 0.38f);
        assistRect.anchorMax = new Vector2(0.26f, 0.63f);
        assistRect.offsetMin = Vector2.zero;
        assistRect.offsetMax = Vector2.zero;

        var assistTextGO = new GameObject("AimAssistText");
        assistTextGO.transform.SetParent(assistButtonGO.transform, false);
        var assistText = assistTextGO.AddComponent<Text>();
        assistText.text = "Aim Assist: OFF";
        assistText.alignment = TextAnchor.MiddleCenter;
        assistText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        var alRect = assistTextGO.GetComponent<RectTransform>();
        alRect.anchorMin = Vector2.zero;
        alRect.anchorMax = Vector2.one;
        alRect.offsetMin = Vector2.zero;
        alRect.offsetMax = Vector2.zero;

        // Toggle Sim Mode Button
        var simModeButtonGO = new GameObject("ToggleSimModeButton");
        simModeButtonGO.transform.SetParent(panelGO.transform, false);
        var simModeBtnImg = simModeButtonGO.AddComponent<UnityEngine.UI.Image>();
        simModeBtnImg.color = new Color(1f, 1f, 1f, 0.06f);
        var simModeBtn = simModeButtonGO.AddComponent<Button>();
        var simModeRect = simModeButtonGO.GetComponent<RectTransform>();
        simModeRect.anchorMin = new Vector2(0.28f, 0.38f);
        simModeRect.anchorMax = new Vector2(0.40f, 0.63f);
        simModeRect.offsetMin = Vector2.zero;
        simModeRect.offsetMax = Vector2.zero;

        var simModeTextGO = new GameObject("SimModeText");
        simModeTextGO.transform.SetParent(simModeButtonGO.transform, false);
        var simModeText = simModeTextGO.AddComponent<Text>();
        simModeText.text = "Sim Mode: ON";
        simModeText.alignment = TextAnchor.MiddleCenter;
        simModeText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        var smRect = simModeTextGO.GetComponent<RectTransform>();
        smRect.anchorMin = Vector2.zero;
        smRect.anchorMax = Vector2.one;
        smRect.offsetMin = Vector2.zero;
        smRect.offsetMax = Vector2.zero;

        // Routine Select Dropdown
        var routineSelectGO = new GameObject("RoutineSelect");
        routineSelectGO.transform.SetParent(panelGO.transform, false);
        var routineDropdown = routineSelectGO.AddComponent<Dropdown>();
        var routineOptions = new System.Collections.Generic.List<Dropdown.OptionData>
        {
            new Dropdown.OptionData("Free Placement"),
            new Dropdown.OptionData("Line Up (Reds)"),
            new Dropdown.OptionData("D-Zone Clearance"),
            new Dropdown.OptionData("Cushion Kiss"),
            new Dropdown.OptionData("Around the Black"),
            new Dropdown.OptionData("Spiral Curve")
        };
        routineDropdown.options = routineOptions;
        var rdRect = routineSelectGO.GetComponent<RectTransform>();
        rdRect.anchorMin = new Vector2(0.42f, 0.38f);
        rdRect.anchorMax = new Vector2(0.67f, 0.63f);
        rdRect.offsetMin = Vector2.zero;
        rdRect.offsetMax = Vector2.zero;

        // Table Style Dropdown
        var tableStyleSelectGO = new GameObject("TableStyleSelect");
        tableStyleSelectGO.transform.SetParent(panelGO.transform, false);
        var tableDropdown = tableStyleSelectGO.AddComponent<Dropdown>();
        var tableOptions = new System.Collections.Generic.List<Dropdown.OptionData>
        {
            new Dropdown.OptionData("Snooker 12ft"),
            new Dropdown.OptionData("8-Ball Pool")
        };
        tableDropdown.options = tableOptions;
        var tsdRect = tableStyleSelectGO.GetComponent<RectTransform>();
        tsdRect.anchorMin = new Vector2(0.69f, 0.38f);
        tsdRect.anchorMax = new Vector2(0.89f, 0.63f);
        tsdRect.offsetMin = Vector2.zero;
        tsdRect.offsetMax = Vector2.zero;

        // --- ROW 3 (y = 0.05f to 0.30f) ---

        // Toggle Mic Mute Button
        var micMuteButtonGO = new GameObject("ToggleMicMuteButton");
        micMuteButtonGO.transform.SetParent(panelGO.transform, false);
        var micMuteBtnImg = micMuteButtonGO.AddComponent<UnityEngine.UI.Image>();
        micMuteBtnImg.color = new Color(1f, 1f, 1f, 0.06f);
        var micMuteBtn = micMuteButtonGO.AddComponent<Button>();
        var micMuteRect = micMuteButtonGO.GetComponent<RectTransform>();
        micMuteRect.anchorMin = new Vector2(0.01f, 0.05f);
        micMuteRect.anchorMax = new Vector2(0.15f, 0.30f);
        micMuteRect.offsetMin = Vector2.zero;
        micMuteRect.offsetMax = Vector2.zero;

        var micMuteTextGO = new GameObject("MicMuteText");
        micMuteTextGO.transform.SetParent(micMuteButtonGO.transform, false);
        var micMuteText = micMuteTextGO.AddComponent<Text>();
        micMuteText.text = "Mic: ON";
        micMuteText.alignment = TextAnchor.MiddleCenter;
        micMuteText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        var mmRect = micMuteTextGO.GetComponent<RectTransform>();
        mmRect.anchorMin = Vector2.zero;
        mmRect.anchorMax = Vector2.one;
        mmRect.offsetMin = Vector2.zero;
        mmRect.offsetMax = Vector2.zero;

        // Toggle Opponent Mute Button
        var oppMuteButtonGO = new GameObject("ToggleOpponentMuteButton");
        oppMuteButtonGO.transform.SetParent(panelGO.transform, false);
        var oppMuteBtnImg = oppMuteButtonGO.AddComponent<UnityEngine.UI.Image>();
        oppMuteBtnImg.color = new Color(1f, 1f, 1f, 0.06f);
        var oppMuteBtn = oppMuteButtonGO.AddComponent<Button>();
        var oppMuteRect = oppMuteButtonGO.GetComponent<RectTransform>();
        oppMuteRect.anchorMin = new Vector2(0.17f, 0.05f);
        oppMuteRect.anchorMax = new Vector2(0.35f, 0.30f);
        oppMuteRect.offsetMin = Vector2.zero;
        oppMuteRect.offsetMax = Vector2.zero;

        var oppMuteTextGO = new GameObject("OpponentMuteText");
        oppMuteTextGO.transform.SetParent(oppMuteButtonGO.transform, false);
        var oppMuteText = oppMuteTextGO.AddComponent<Text>();
        oppMuteText.text = "Opponent: ON";
        oppMuteText.alignment = TextAnchor.MiddleCenter;
        oppMuteText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        var omRect = oppMuteTextGO.GetComponent<RectTransform>();
        omRect.anchorMin = Vector2.zero;
        omRect.anchorMax = Vector2.one;
        omRect.offsetMin = Vector2.zero;
        omRect.offsetMax = Vector2.zero;

        // Toggle Ghost Mode Button
        var ghostButtonGO = new GameObject("ToggleGhostModeButton");
        ghostButtonGO.transform.SetParent(panelGO.transform, false);
        var ghostBtnImg = ghostButtonGO.AddComponent<UnityEngine.UI.Image>();
        ghostBtnImg.color = new Color(1f, 1f, 1f, 0.06f);
        var ghostBtn = ghostButtonGO.AddComponent<Button>();
        var ghostRect = ghostButtonGO.GetComponent<RectTransform>();
        ghostRect.anchorMin = new Vector2(0.37f, 0.05f);
        ghostRect.anchorMax = new Vector2(0.55f, 0.30f);
        ghostRect.offsetMin = Vector2.zero;
        ghostRect.offsetMax = Vector2.zero;

        var ghostTextGO = new GameObject("GhostModeText");
        ghostTextGO.transform.SetParent(ghostButtonGO.transform, false);
        var ghostText = ghostTextGO.AddComponent<Text>();
        ghostText.text = "Ghost Mode: OFF";
        ghostText.alignment = TextAnchor.MiddleCenter;
        ghostText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        var gmRect = ghostTextGO.GetComponent<RectTransform>();
        gmRect.anchorMin = Vector2.zero;
        gmRect.anchorMax = Vector2.one;
        gmRect.offsetMin = Vector2.zero;
        gmRect.offsetMax = Vector2.zero;

        // Toggle My Stats Button
        var myStatsButtonGO = new GameObject("ToggleMyStatsButton");
        myStatsButtonGO.transform.SetParent(panelGO.transform, false);
        var myStatsBtnImg = myStatsButtonGO.AddComponent<UnityEngine.UI.Image>();
        myStatsBtnImg.color = new Color(1f, 1f, 1f, 0.06f);
        var myStatsBtn = myStatsButtonGO.AddComponent<Button>();
        var myStatsRect = myStatsButtonGO.GetComponent<RectTransform>();
        myStatsRect.anchorMin = new Vector2(0.57f, 0.05f);
        myStatsRect.anchorMax = new Vector2(0.70f, 0.30f);
        myStatsRect.offsetMin = Vector2.zero;
        myStatsRect.offsetMax = Vector2.zero;

        var myStatsTextGO = new GameObject("MyStatsButtonText");
        myStatsTextGO.transform.SetParent(myStatsButtonGO.transform, false);
        var msBtnTxt = myStatsTextGO.AddComponent<Text>();
        msBtnTxt.text = "My Profile";
        msBtnTxt.alignment = TextAnchor.MiddleCenter;
        msBtnTxt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        var msRect = myStatsTextGO.GetComponent<RectTransform>();
        msRect.anchorMin = Vector2.zero;
        msRect.anchorMax = Vector2.one;
        msRect.offsetMin = Vector2.zero;
        msRect.offsetMax = Vector2.zero;

        // My Stats Panel Overlay
        var statsPanelGO = new GameObject("MyStatsPanel");
        statsPanelGO.transform.SetParent(canvasGO.transform, false);
        var statsPanelImg = statsPanelGO.AddComponent<UnityEngine.UI.Image>();
        statsPanelImg.color = new Color(0.08f, 0.12f, 0.16f, 0.95f);
        var spRect = statsPanelGO.GetComponent<RectTransform>();
        spRect.anchorMin = new Vector2(0.3f, 0.25f);
        spRect.anchorMax = new Vector2(0.7f, 0.65f);
        spRect.offsetMin = Vector2.zero;
        spRect.offsetMax = Vector2.zero;

        var statsTitleGO = new GameObject("StatsTitle");
        statsTitleGO.transform.SetParent(statsPanelGO.transform, false);
        var statsTitle = statsTitleGO.AddComponent<Text>();
        statsTitle.text = "=== MY STATISTICS ===";
        statsTitle.alignment = TextAnchor.UpperCenter;
        statsTitle.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        var stRect = statsTitleGO.GetComponent<RectTransform>();
        stRect.anchorMin = new Vector2(0f, 0.8f);
        stRect.anchorMax = new Vector2(1f, 1f);
        stRect.offsetMin = Vector2.zero;
        stRect.offsetMax = Vector2.zero;

        var statsInfoGO = new GameObject("StatsInfoText");
        statsInfoGO.transform.SetParent(statsPanelGO.transform, false);
        var statsInfoText = statsInfoGO.AddComponent<Text>();
        statsInfoText.text = "Loading...";
        statsInfoText.alignment = TextAnchor.MiddleCenter;
        statsInfoText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        var siRect = statsInfoGO.GetComponent<RectTransform>();
        siRect.anchorMin = new Vector2(0f, 0f);
        siRect.anchorMax = new Vector2(1f, 0.75f);
        siRect.offsetMin = Vector2.zero;
        siRect.offsetMax = Vector2.zero;

        // ── Ball Status Toggle Button (anchored beside My Profile) ──
        var ballStatusBtnGO = new GameObject("ToggleBallStatusButton");
        ballStatusBtnGO.transform.SetParent(panelGO.transform, false);
        var ballStatusBtnImg = ballStatusBtnGO.AddComponent<UnityEngine.UI.Image>();
        ballStatusBtnImg.color = new Color(0.0f, 0.8f, 0.4f, 0.15f); // Teal tint
        var ballStatusBtn = ballStatusBtnGO.AddComponent<Button>();
        var bsRect = ballStatusBtnGO.GetComponent<RectTransform>();
        bsRect.anchorMin = new Vector2(0.72f, 0.05f);
        bsRect.anchorMax = new Vector2(0.86f, 0.30f);
        bsRect.offsetMin = Vector2.zero;
        bsRect.offsetMax = Vector2.zero;

        var ballStatusBtnTextGO = new GameObject("BallStatusButtonText");
        ballStatusBtnTextGO.transform.SetParent(ballStatusBtnGO.transform, false);
        var bsBtnTxt = ballStatusBtnTextGO.AddComponent<Text>();
        bsBtnTxt.text = "Balls";
        bsBtnTxt.alignment = TextAnchor.MiddleCenter;
        bsBtnTxt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        var bsBtnRect = ballStatusBtnTextGO.GetComponent<RectTransform>();
        bsBtnRect.anchorMin = Vector2.zero;
        bsBtnRect.anchorMax = Vector2.one;
        bsBtnRect.offsetMin = Vector2.zero;
        bsBtnRect.offsetMax = Vector2.zero;

        // ── Ball Status Panel Overlay ──
        var ballStatusPanelGO = new GameObject("BallStatusPanel");
        ballStatusPanelGO.transform.SetParent(canvasGO.transform, false);
        var ballStatusPanelImg = ballStatusPanelGO.AddComponent<UnityEngine.UI.Image>();
        ballStatusPanelImg.color = new Color(0.04f, 0.10f, 0.08f, 0.95f); // Dark teal
        var bsPanelRect = ballStatusPanelGO.GetComponent<RectTransform>();
        bsPanelRect.anchorMin = new Vector2(0.30f, 0.15f);
        bsPanelRect.anchorMax = new Vector2(0.70f, 0.60f);
        bsPanelRect.offsetMin = Vector2.zero;
        bsPanelRect.offsetMax = Vector2.zero;

        // Title
        var bsTitleGO = new GameObject("BallStatusTitle");
        bsTitleGO.transform.SetParent(ballStatusPanelGO.transform, false);
        var bsTitle = bsTitleGO.AddComponent<Text>();
        bsTitle.text = "=== BALL STATUS ===";
        bsTitle.alignment = TextAnchor.UpperCenter;
        bsTitle.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        bsTitle.color = new Color(0.2f, 1f, 0.6f);
        var bsTitleRect = bsTitleGO.GetComponent<RectTransform>();
        bsTitleRect.anchorMin = new Vector2(0f, 0.80f);
        bsTitleRect.anchorMax = new Vector2(1f, 1f);
        bsTitleRect.offsetMin = Vector2.zero;
        bsTitleRect.offsetMax = Vector2.zero;

        // Content Text
        var bsInfoGO = new GameObject("BallStatusInfoText");
        bsInfoGO.transform.SetParent(ballStatusPanelGO.transform, false);
        var ballStatusInfoText = bsInfoGO.AddComponent<Text>();
        ballStatusInfoText.text = "Waiting for game...";
        ballStatusInfoText.alignment = TextAnchor.UpperLeft;
        ballStatusInfoText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        ballStatusInfoText.color = Color.white;
        ballStatusInfoText.fontSize = 14;
        var bsInfoRect = bsInfoGO.GetComponent<RectTransform>();
        bsInfoRect.anchorMin = new Vector2(0.02f, 0f);
        bsInfoRect.anchorMax = new Vector2(0.98f, 0.78f);
        bsInfoRect.offsetMin = Vector2.zero;
        bsInfoRect.offsetMax = Vector2.zero;

        ballStatusPanelGO.SetActive(false); // hidden by default

        // Add CueStrikeHUDController
        var hudController = canvasGO.AddComponent<CueStrikeHUDController>();
        hudController.modeText = modeText;
        hudController.toggleEnvButton = btn;
        hudController.toggleGloveButton = gloveBtn;
        hudController.gloveStatusText = gloveText;
        hudController.toggleAimAssistButton = assistBtn;
        hudController.aimAssistStatusText = assistText;
        hudController.toggleSimModeButton = simModeBtn;
        hudController.simModeStatusText = simModeText;
        hudController.toggleMicMuteButton = micMuteBtn;
        hudController.micMuteStatusText = micMuteText;
        hudController.toggleOpponentMuteButton = oppMuteBtn;
        hudController.opponentMuteStatusText = oppMuteText;
        hudController.toggleGhostModeButton = ghostBtn;
        hudController.ghostModeStatusText = ghostText;
        hudController.toggleMyStatsButton = myStatsBtn;
        hudController.myStatsPanel = statsPanelGO;
        hudController.myStatsText = statsInfoText;
        // ── Ball Status wiring ──
        hudController.toggleBallStatusButton = ballStatusBtn;
        hudController.ballStatusPanel = ballStatusPanelGO;
        hudController.ballStatusText = ballStatusInfoText;
        // ───────────────────────
        hudController.feltDropdown = feltDropdown;
        hudController.ballDropdown = ballDropdown;
        hudController.routineDropdown = routineDropdown;
        hudController.tableTypeDropdown = tableDropdown;

        // Load and assign materials editor-only (to save in prefab serialized data)
#if UNITY_EDITOR
        string matDir = "Assets/CueStrike/Materials";
        hudController.feltMaterials = new System.Collections.Generic.List<Material>
        {
            AssetDatabase.LoadAssetAtPath<Material>(matDir + "/Felt_Snooker.mat"),
            AssetDatabase.LoadAssetAtPath<Material>(matDir + "/Felt_Pool.mat"),
            AssetDatabase.LoadAssetAtPath<Material>(matDir + "/Felt_Cyber_Grid.mat"),
            AssetDatabase.LoadAssetAtPath<Material>(matDir + "/Felt_Burgundy.mat")
        };
        hudController.ballMaterials = new System.Collections.Generic.List<Material>
        {
            AssetDatabase.LoadAssetAtPath<Material>(matDir + "/Ball_Classic.mat"),
            AssetDatabase.LoadAssetAtPath<Material>(matDir + "/Ball_Neon.mat"),
            AssetDatabase.LoadAssetAtPath<Material>(matDir + "/Ball_Gold_Marble.mat"),
            AssetDatabase.LoadAssetAtPath<Material>(matDir + "/Ball_Holo.mat")
        };
#endif

        // Add CueSelectUI
        var cueSelect = canvasGO.AddComponent<CueSelectUI>();
        cueSelect.cueDropdown = dropdown;

        // Populate available cues from Assets/CueStrike/Cues if any (editor-only)
    #if UNITY_EDITOR
        var cueGuids = AssetDatabase.FindAssets("t:CueProfile", new[] { "Assets/CueStrike/Cues" });
        cueSelect.availableCues = new System.Collections.Generic.List<CueProfile>();
        foreach (var g in cueGuids)
        {
            var p = AssetDatabase.GUIDToAssetPath(g);
            var cp = AssetDatabase.LoadAssetAtPath<CueProfile>(p);
            if (cp != null) cueSelect.availableCues.Add(cp);
        }
    #endif

        // Save prefab
        var prefab = PrefabUtility.SaveAsPrefabAsset(canvasGO, path);
        // After saving, also update the prefab instance asset file with available cues (editor only)
#if UNITY_EDITOR
        if (prefab != null)
        {
            var loaded = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            var comp = loaded.GetComponent<CueSelectUI>();
            if (comp != null)
            {
                var cueGuids2 = AssetDatabase.FindAssets("t:CueProfile", new[] { "Assets/CueStrike/Cues" });
                comp.availableCues = new System.Collections.Generic.List<CueProfile>();
                foreach (var g in cueGuids2)
                {
                    var p2 = AssetDatabase.GUIDToAssetPath(g);
                    var cp2 = AssetDatabase.LoadAssetAtPath<CueProfile>(p2);
                    if (cp2 != null) comp.availableCues.Add(cp2);
                }
                EditorUtility.SetDirty(loaded);
                AssetDatabase.SaveAssets();
            }
        }
#endif

        GameObject.DestroyImmediate(canvasGO);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("CueStrike: HUD prefab created at " + path);
    }
}
#endif