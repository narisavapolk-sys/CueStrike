using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using CueStrike.UI;

namespace CueStrike.Editor
{
    /// <summary>
    /// Editor utility to create and configure the Title Scene (Nok's Grand Hall) step by step.
    /// Run via: Tools/CueStrike/Title Scene/
    /// </summary>
    public class TitleSceneSetup
    {
        private const string SCENE_PATH = "Assets/CueStrike/Scenes/Title_NoksGrandHall.unity";
        private const string SCENES_FOLDER = "Assets/CueStrike/Scenes";

        #region Guard Helpers

        private static bool RunGuards(string stepName)
        {
            // Guard 1: ป้องกันกดขณะ Play Mode
            if (EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog("Error", "Cannot run in Play Mode. Please exit Play Mode first.", "OK");
                return false;
            }

            // Guard 2: ถ้า Scene ปัจจุบันมีการเปลี่ยนแปลงยังไม่ Save → ถามก่อน
            Scene activeScene = EditorSceneManager.GetActiveScene();
            if (activeScene.isDirty)
            {
                if (!EditorUtility.DisplayDialog("Unsaved Changes", 
                    $"Current scene '{activeScene.name}' has unsaved changes. Continue? This will discard them.", 
                    "Continue", "Cancel"))
                {
                    return false;
                }
            }

            // Guard 3: ถ้า Scene ปัจจุบันไม่ใช่ Title_NoksGrandHall → ถามก่อน
            if (activeScene.path != SCENE_PATH && !string.IsNullOrEmpty(activeScene.path))
            {
                if (!EditorUtility.DisplayDialog("Switch Scene?", 
                    $"This will replace '{activeScene.name}' with Title Scene. Continue?", 
                    "Continue", "Cancel"))
                {
                    return false;
                }
            }

            // Ensure Scenes folder exists
            if (!AssetDatabase.IsValidFolder(SCENES_FOLDER))
            {
                AssetDatabase.CreateFolder("Assets/CueStrike", "Scenes");
            }

            Debug.Log($"[TitleSceneSetup] Step: {stepName} — Guards passed");
            return true;
        }

        private static void CreateNewScene()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

        private static void SaveScene()
        {
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), SCENE_PATH);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[TitleSceneSetup] Title Scene saved at: {SCENE_PATH}");
        }

        #endregion

        #region Menu Items (8 Steps)

        [MenuItem("Tools/CueStrike/Title Scene/1. Create Empty Scene")]
        public static void Step1_CreateEmptyScene()
        {
            if (!RunGuards("1. Create Empty Scene")) return;
            CreateNewScene();
            SaveScene();
            Debug.Log("[Step 1/8] Empty scene created and saved");
            EditorUtility.DisplayDialog("Step 1 Complete", "Empty scene created and saved as Title_NoksGrandHall.unity", "OK");
        }

        [MenuItem("Tools/CueStrike/Title Scene/2. Setup Lighting & Camera")]
        public static void Step2_SetupLightingAndCamera()
        {
            if (!RunGuards("2. Setup Lighting & Camera")) return;
            
            // Check if already exists
            Light existingLight = Object.FindFirstObjectByType<Light>();
            Camera existingCam = Object.FindFirstObjectByType<Camera>();
            
            if (existingLight == null)
            {
                SetupLighting();
                Debug.Log("[Step 2/8] Directional Light created");
            }
            else
            {
                Debug.Log("[Step 2/8] Directional Light already exists — skipping creation");
            }

            if (existingCam == null)
            {
                SetupCamera();
                Debug.Log("[Step 2/8] Main Camera created");
            }
            else
            {
                Debug.Log("[Step 2/8] Main Camera already exists — skipping creation");
            }

            SaveScene();
            EditorUtility.DisplayDialog("Step 2 Complete", "Lighting & Camera ready", "OK");
        }

        [MenuItem("Tools/CueStrike/Title Scene/3. Create Uncle Nok Placeholder")]
        public static void Step3_CreateUncleNokPlaceholder()
        {
            if (!RunGuards("3. Create Uncle Nok Placeholder")) return;
            
            GameObject existing = GameObject.Find("UncleNok_Placeholder");
            if (existing == null)
            {
                CreateUncleNokPlaceholder();
                Debug.Log("[Step 3/8] Uncle Nok placeholder placed");
            }
            else
            {
                Debug.Log("[Step 3/8] UncleNok_Placeholder already exists — skipping creation");
            }

            SaveScene();
            EditorUtility.DisplayDialog("Step 3 Complete", "Uncle Nok placeholder placed", "OK");
        }

        [MenuItem("Tools/CueStrike/Title Scene/4. Create Bo Panda Placeholder")]
        public static void Step4_CreateBoPandaPlaceholder()
        {
            if (!RunGuards("4. Create Bo Panda Placeholder")) return;
            
            GameObject existing = GameObject.Find("BoPanda_Placeholder");
            if (existing == null)
            {
                CreateBoPandaPlaceholder();
                Debug.Log("[Step 4/8] Bo Panda placeholder placed");
            }
            else
            {
                Debug.Log("[Step 4/8] BoPanda_Placeholder already exists — skipping creation");
            }

            SaveScene();
            EditorUtility.DisplayDialog("Step 4 Complete", "Bo Panda placeholder placed", "OK");
        }

        [MenuItem("Tools/CueStrike/Title Scene/5. Create UI Canvas & Menu Buttons")]
        public static void Step5_CreateUICanvasAndButtons()
        {
            if (!RunGuards("5. Create UI Canvas & Menu Buttons")) return;
            
            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                CreateWorldSpaceUI();
                Debug.Log("[Step 5/8] UI Canvas + 5 buttons created");
            }
            else
            {
                Debug.Log("[Step 5/8] Canvas already exists — skipping creation");
            }

            // ALWAYS ensure EventSystem exists (even if Canvas was reused)
            EnsureEventSystem();

            SaveScene();
            EditorUtility.DisplayDialog("Step 5 Complete", "UI Canvas & 5 buttons created", "OK");
        }

        private static void EnsureEventSystem()
        {
            if (Object.FindFirstObjectByType<EventSystem>() == null)
            {
                GameObject eventSystemObj = new GameObject("EventSystem");
                eventSystemObj.AddComponent<EventSystem>();
                eventSystemObj.AddComponent<StandaloneInputModule>();
                Debug.Log("[EnsureEventSystem] EventSystem + StandaloneInputModule created");
            }
            else
            {
                Debug.Log("[EnsureEventSystem] EventSystem already exists");
            }
        }

        [MenuItem("Tools/CueStrike/Title Scene/6. Create Settings & Credits Panels")]
        public static void Step6_CreateSettingsAndCreditsPanels()
        {
            if (!RunGuards("6. Create Settings & Credits Panels")) return;
            
            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas != null)
            {
                // Check if panels already exist
                Transform settingsPanel = canvas.transform.Find("SettingsPanel");
                Transform creditsPanel = canvas.transform.Find("CreditsPanel");
                Transform comingSoonPanel = canvas.transform.Find("ComingSoonPanel");
                
                if (settingsPanel == null && creditsPanel == null && comingSoonPanel == null)
                {
                    CreatePanelsAndBoPanda(canvas);
                    Debug.Log("[Step 6/8] Settings, Credits, ComingSoon panels created");
                }
                else
                {
                    Debug.Log("[Step 6/8] Panels already exist — skipping creation");
                }
            }
            else
            {
                Debug.LogWarning("[Step 6/8] Canvas not found — run Step 5 first");
            }

            SaveScene();
            EditorUtility.DisplayDialog("Step 6 Complete", "Settings & Credits panels created", "OK");
        }

        [MenuItem("Tools/CueStrike/Title Scene/7. Create Audio & Managers")]
        public static void Step7_CreateAudioAndManagers()
        {
            if (!RunGuards("7. Create Audio & Managers")) return;
            
            // Check if AmbientAudio exists
            GameObject ambientAudio = GameObject.Find("AmbientAudio");
            if (ambientAudio == null)
            {
                CreateAmbientAudio();
                Debug.Log("[Step 7/8] AmbientAudio created");
            }
            else
            {
                Debug.Log("[Step 7/8] AmbientAudio already exists — skipping");
            }

            // Check if TitleSceneManager exists
            TitleSceneManager mgr = Object.FindFirstObjectByType<TitleSceneManager>();
            if (mgr == null)
            {
                CreateTitleSceneManager();
                Debug.Log("[Step 7/8] TitleSceneManager created");
            }
            else
            {
                Debug.Log("[Step 7/8] TitleSceneManager already exists — skipping");
            }

            SaveScene();
            EditorUtility.DisplayDialog("Step 7 Complete", "Audio & Managers ready", "OK");
        }

        [MenuItem("Tools/CueStrike/Title Scene/8. Wire Buttons to TitleSceneManager")]
        public static void Step8_WireButtonsToManager()
        {
            if (!RunGuards("8. Wire Buttons to TitleSceneManager")) return;
            
            TitleSceneManager mgr = Object.FindFirstObjectByType<TitleSceneManager>();
            Canvas canvas = Object.FindFirstObjectByType<Canvas>();

            if (mgr == null)
            {
                Debug.LogError("[Step 8/8] TitleSceneManager not found — run Step 7 first");
                EditorUtility.DisplayDialog("Error", "TitleSceneManager not found. Run Step 7 first.", "OK");
                return;
            }

            if (canvas == null)
            {
                Debug.LogError("[Step 8/8] Canvas not found — run Step 5 first");
                EditorUtility.DisplayDialog("Error", "Canvas not found. Run Step 5 first.", "OK");
                return;
            }

            // Find and wire buttons
            Button[] buttons = canvas.GetComponentsInChildren<Button>();
            int wiredCount = 0;
            foreach (Button btn in buttons)
            {
                switch (btn.name)
                {
                    case "Btn_Play": mgr.btnPlay = btn; wiredCount++; break;
                    case "Btn_Practice": mgr.btnPractice = btn; wiredCount++; break;
                    case "Btn_Multiplayer": mgr.btnMultiplayer = btn; wiredCount++; break;
                    case "Btn_Settings": mgr.btnSettings = btn; wiredCount++; break;
                    case "Btn_Credits": mgr.btnCredits = btn; wiredCount++; break;
                }
            }

            // Wire panel references
            Transform btnContainer = canvas.transform.Find("ButtonContainer");
            if (btnContainer != null) mgr.mainMenuPanel = btnContainer.gameObject;
            else mgr.mainMenuPanel = canvas.transform.Find("Panel_BG")?.gameObject;

            mgr.settingsPanel = canvas.transform.Find("SettingsPanel")?.gameObject;
            mgr.creditsPanel = canvas.transform.Find("CreditsPanel")?.gameObject;
            mgr.comingSoonPanel = canvas.transform.Find("ComingSoonPanel")?.gameObject;
            mgr.comingSoonText = mgr.comingSoonPanel?.transform.Find("SoonContent/SoonText")?.GetComponent<TextMeshProUGUI>();

            // Wire Back buttons in panels
            WireBackButtons(mgr);

            // CRITICAL: Call BindButtons() to wire onClick listeners to TitleSceneManager methods
            // This prevents "use SceneManager.LoadScene() instead" error
            mgr.BindButtons();
            Debug.Log("[Step 8/8] TitleSceneManager.BindButtons() called - onClick listeners wired");

            Debug.Log($"[Step 8/8] {wiredCount} buttons wired successfully!");
            EditorUtility.DisplayDialog("Step 8 Complete", $"All buttons wired ({wiredCount}/5). Title Scene ready!", "OK");
        }

        [MenuItem("Tools/CueStrike/Title Scene/9. Auto-Wire All References")]
        public static void Step9_AutoWireAllReferences()
        {
            // Guard 1: Prevent running in Play Mode
            if (EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog("Error", "Cannot run in Play Mode. Please exit Play Mode first.", "OK");
                Debug.LogError("[Auto-Wire] Cannot run in Play Mode");
                return;
            }

            // Guard 2: Check for unsaved changes
            Scene activeScene = EditorSceneManager.GetActiveScene();
            if (activeScene.isDirty)
            {
                if (!EditorUtility.DisplayDialog("Unsaved Changes", 
                    $"Current scene '{activeScene.name}' has unsaved changes. Continue? This will discard them.", 
                    "Continue", "Cancel"))
                {
                    return;
                }
            }

            // Guard 3: Ensure we're in the right scene (optional - just warn)
            if (activeScene.path != SCENE_PATH && !string.IsNullOrEmpty(activeScene.path))
            {
                if (!EditorUtility.DisplayDialog("Switch Scene?", 
                    $"This will wire references in '{activeScene.name}' instead of Title Scene. Continue?", 
                    "Continue", "Cancel"))
                {
                    return;
                }
            }

            // Find TitleSceneManager
            TitleSceneManager mgr = Object.FindFirstObjectByType<TitleSceneManager>();
            if (mgr == null)
            {
                Debug.LogError("[Auto-Wire] TitleSceneManager not found in scene! Run Step 7 first.");
                EditorUtility.DisplayDialog("Error", "TitleSceneManager not found! Run Step 7 first.", "OK");
                return;
            }

            // Find Canvas
            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("[Auto-Wire] Canvas not found in scene! Run Step 5 first.");
                EditorUtility.DisplayDialog("Error", "Canvas not found! Run Step 5 first.", "OK");
                return;
            }

            // Wire all 5 buttons by name
            Button[] allButtons = canvas.GetComponentsInChildren<Button>(true);
            int wiredCount = 0;
            foreach (Button btn in allButtons)
            {
                switch (btn.name)
                {
                    case "Btn_Play": mgr.btnPlay = btn; wiredCount++; break;
                    case "Btn_Practice": mgr.btnPractice = btn; wiredCount++; break;
                    case "Btn_Multiplayer": mgr.btnMultiplayer = btn; wiredCount++; break;
                    case "Btn_Settings": mgr.btnSettings = btn; wiredCount++; break;
                    case "Btn_Credits": mgr.btnCredits = btn; wiredCount++; break;
                }
            }

            // Wire panels by name
            Transform canvasTrans = canvas.transform;
            mgr.mainMenuPanel = canvasTrans.Find("Panel_BG")?.gameObject;
            mgr.settingsPanel = canvasTrans.Find("SettingsPanel")?.gameObject;
            mgr.creditsPanel = canvasTrans.Find("CreditsPanel")?.gameObject;
            mgr.comingSoonPanel = canvasTrans.Find("ComingSoonPanel")?.gameObject;

            // Wire Coming Soon Text (first TextMeshProUGUI in ComingSoonPanel)
            if (mgr.comingSoonPanel != null)
            {
                mgr.comingSoonText = mgr.comingSoonPanel.GetComponentInChildren<TextMeshProUGUI>(true);
            }

            // Mark dirty and save
            EditorUtility.SetDirty(mgr);
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();

            Debug.Log($"[Auto-Wire] All references assigned successfully! ({wiredCount} buttons, 4 panels)");
            EditorUtility.DisplayDialog("Step 9 Complete", $"Auto-wire complete!\n{wiredCount} buttons + 4 panels wired to TitleSceneManager", "OK");
        }

        private static void WireBackButtons(TitleSceneManager mgr)
        {
            if (mgr.settingsPanel != null)
            {
                Button backBtn = mgr.settingsPanel.GetComponentInChildren<Button>();
                if (backBtn != null && backBtn.name == "BackBtn")
                {
                    backBtn.onClick.RemoveAllListeners();
                    backBtn.onClick.AddListener(() => mgr.OnBackButton());
                }
            }
            if (mgr.creditsPanel != null)
            {
                Button backBtn = mgr.creditsPanel.GetComponentInChildren<Button>();
                if (backBtn != null && backBtn.name == "BackBtn")
                {
                    backBtn.onClick.RemoveAllListeners();
                    backBtn.onClick.AddListener(() => mgr.OnBackButton());
                }
            }
        }

        #endregion

        #region Private Implementation Methods (unchanged from original)

        private static void SetupLighting()
        {
            // Directional Light (Sun)
            GameObject sun = new GameObject("Directional Light");
            Light sunLight = sun.AddComponent<Light>();
            sunLight.type = LightType.Directional;
            sunLight.color = new Color32(0xFF, 0xF8, 0xE7, 0xFF); // #FFF8E7 warm
            sunLight.intensity = 1.2f;
            sunLight.shadows = LightShadows.Soft;
            sunLight.shadowStrength = 1f;
            sun.transform.rotation = Quaternion.Euler(50, -30, 0);

            // Ambient Light Settings - Use correct enum value for Skybox (Unity 6+)
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Skybox;
            RenderSettings.ambientSkyColor = new Color32(0x87, 0xCE, 0xEB, 0xFF); // #87CEEB
            RenderSettings.ambientEquatorColor = new Color32(0xFF, 0xE4, 0xB5, 0xFF); // #FFE4B5
            RenderSettings.ambientGroundColor = new Color32(0x8B, 0x45, 0x13, 0xFF); // #8B4513
            RenderSettings.ambientIntensity = 1f;
        }

        private static void SetupCamera()
        {
            GameObject cameraObj = new GameObject("Main Camera");
            cameraObj.tag = "MainCamera";
            Camera cam = cameraObj.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.Skybox;
            cam.fieldOfView = 60f;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 1000f;
            cameraObj.transform.position = new Vector3(0, 1.6f, -3f);
            cameraObj.transform.LookAt(new Vector3(0, 1.2f, 0));
            
            // Add AudioListener
            cameraObj.AddComponent<AudioListener>();
        }

        private static void CreateUncleNokPlaceholder()
        {
            GameObject uncleNok = new GameObject("UncleNok_Placeholder");
            uncleNok.transform.position = new Vector3(0, 0.9f, 2f);
            
            // Capsule as placeholder (1.8m tall)
            GameObject capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            capsule.name = "Body";
            capsule.transform.SetParent(uncleNok.transform);
            capsule.transform.localPosition = Vector3.zero;
            capsule.transform.localScale = new Vector3(0.5f, 1.8f, 0.5f);
            
            // Gray material
            Renderer rend = capsule.GetComponent<Renderer>();
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = new Color(0.5f, 0.5f, 0.5f, 1f);
            rend.material = mat;
            
            // Remove collider (we'll add proper one later) - Use Object.DestroyImmediate
            Object.DestroyImmediate(capsule.GetComponent<CapsuleCollider>());
            
            // Add tag for identification
            uncleNok.tag = "NPC";
        }

        private static void CreateWorldSpaceUI()
        {
            // Canvas
            GameObject canvasObj = new GameObject("TitleCanvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvasObj.AddComponent<GraphicRaycaster>();
            
            RectTransform canvasRect = canvasObj.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(2000, 1500); // 2m x 1.5m in canvas units
            canvasRect.position = new Vector3(0, 1.2f, 2f);
            canvasRect.rotation = Quaternion.Euler(0, 180, 0); // Face camera
            canvasRect.localScale = new Vector3(0.001f, 0.001f, 0.001f); // 1 unit = 1 meter

            // Assign world camera for World Space canvas to work with mouse/VR input
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                canvas.worldCamera = mainCam;
            }
            else
            {
                Debug.LogWarning("[CreateWorldSpaceUI] No Main Camera found - canvas.worldCamera not set");
            }

            // Ensure EventSystem exists for UI input (mouse/VR)
            if (Object.FindFirstObjectByType<EventSystem>() == null)
            {
                GameObject eventSystemObj = new GameObject("EventSystem");
                eventSystemObj.AddComponent<EventSystem>();
                eventSystemObj.AddComponent<StandaloneInputModule>();
                Debug.Log("[CreateWorldSpaceUI] EventSystem + StandaloneInputModule created");
            }
            else
            {
                Debug.Log("[CreateWorldSpaceUI] EventSystem already exists");
            }

            // Panel Background
            GameObject panelObj = new GameObject("Panel_BG");
            panelObj.transform.SetParent(canvasObj.transform, false);
            RectTransform panelRect = panelObj.AddComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;
            
            Image panelImage = panelObj.AddComponent<Image>();
            panelImage.color = new Color(0, 0, 0, 0.8f); // Black 80% alpha

            // Button Container
            GameObject btnContainer = new GameObject("ButtonContainer");
            btnContainer.transform.SetParent(canvasObj.transform, false);
            RectTransform containerRect = btnContainer.AddComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(0.5f, 0.5f);
            containerRect.anchorMax = new Vector2(0.5f, 0.5f);
            containerRect.pivot = new Vector2(0.5f, 0.5f);
            containerRect.anchoredPosition = Vector2.zero;
            containerRect.sizeDelta = new Vector2(600, 800);
            
            VerticalLayoutGroup layout = btnContainer.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 20;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.padding = new RectOffset(20, 20, 20, 20);

            ContentSizeFitter fitter = btnContainer.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Button data
            string[] buttonNames = { "Play", "Practice", "Multiplayer", "Settings", "Credits" };
            string[] buttonTooltips = 
            { 
                "Start single player game", 
                "Practice mode", 
                "Multiplayer lobby", 
                "Game settings", 
                "Credits" 
            };

            for (int i = 0; i < buttonNames.Length; i++)
            {
                CreateButton(btnContainer.transform, buttonNames[i], buttonTooltips[i], i);
            }
        }

        private static GameObject CreateButton(Transform parent, string name, string tooltip, int index)
        {
            GameObject btnObj = new GameObject($"Btn_{name}");
            btnObj.transform.SetParent(parent, false);
            
            Button btn = btnObj.AddComponent<Button>();
            Image btnImage = btnObj.AddComponent<Image>();
            btnImage.color = new Color(0.2f, 0.2f, 0.3f, 1f);
            
            // Button states
            ColorBlock colors = btn.colors;
            colors.normalColor = new Color(0.2f, 0.2f, 0.3f, 1f);
            colors.highlightedColor = new Color(0.3f, 0.3f, 0.5f, 1f);
            colors.pressedColor = new Color(0.15f, 0.15f, 0.25f, 1f);
            colors.selectedColor = new Color(0.25f, 0.25f, 0.4f, 1f);
            colors.disabledColor = new Color(0.1f, 0.1f, 0.15f, 0.5f);
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.1f;
            btn.colors = colors;
            
            // Navigation
            Navigation nav = btn.navigation;
            nav.mode = Navigation.Mode.Explicit;
            btn.navigation = nav;

            // Button Text
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(btnObj.transform, false);
            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = name;
            tmp.fontSize = 60;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            // Use textWrappingMode instead of obsolete enableWordWrapping
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            
            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(20, 10);
            textRect.offsetMax = new Vector2(-20, -10);

            // Button size
            RectTransform btnRect = btnObj.GetComponent<RectTransform>();
            btnRect.sizeDelta = new Vector2(500, 100);

            return btnObj;
        }

        private static void CreateAmbientAudio()
        {
            GameObject audioObj = new GameObject("AmbientAudio");
            AudioSource source = audioObj.AddComponent<AudioSource>();
            source.playOnAwake = true;
            source.loop = true;
            source.volume = 0.3f;
            source.spatialBlend = 0f; // 2D
            source.priority = 256; // Low priority
            
            // TODO: Assign ambient audio clip here when available
            // source.clip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/CueStrike/Audio/Ambient/Title_Ambient.ogg");
        }

        private static void CreateTitleSceneManager()
        {
            GameObject mgrObj = new GameObject("Managers");
            GameObject titleMgrObj = new GameObject("TitleSceneManager");
            titleMgrObj.transform.SetParent(mgrObj.transform);
            
            TitleSceneManager mgr = titleMgrObj.AddComponent<TitleSceneManager>();
            
            // Find buttons in canvas
            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas != null)
            {
                Button[] buttons = canvas.GetComponentsInChildren<Button>();
                foreach (Button btn in buttons)
                {
                    switch (btn.name)
                    {
                        case "Btn_Play": mgr.btnPlay = btn; break;
                        case "Btn_Practice": mgr.btnPractice = btn; break;
                        case "Btn_Multiplayer": mgr.btnMultiplayer = btn; break;
                        case "Btn_Settings": mgr.btnSettings = btn; break;
                        case "Btn_Credits": mgr.btnCredits = btn; break;
                    }
                }
            }
        }

        private static void CreatePanelsAndBoPanda(Canvas canvas)
        {
            // === SETTINGS PANEL ===
            GameObject settingsPanel = new GameObject("SettingsPanel");
            settingsPanel.transform.SetParent(canvas.transform, false);
            RectTransform settingsRect = settingsPanel.AddComponent<RectTransform>();
            settingsRect.anchorMin = Vector2.zero;
            settingsRect.anchorMax = Vector2.one;
            settingsRect.offsetMin = Vector2.zero;
            settingsRect.offsetMax = Vector2.zero;
            
            Image settingsBg = settingsPanel.AddComponent<Image>();
            settingsBg.color = new Color(0, 0, 0, 0.9f);
            
            // Settings Content
            GameObject settingsContent = new GameObject("SettingsContent");
            settingsContent.transform.SetParent(settingsPanel.transform, false);
            RectTransform contentRect = settingsContent.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0.5f, 0.5f);
            contentRect.anchorMax = new Vector2(0.5f, 0.5f);
            contentRect.pivot = new Vector2(0.5f, 0.5f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(500, 400);
            
            VerticalLayoutGroup vLayout = settingsContent.AddComponent<VerticalLayoutGroup>();
            vLayout.spacing = 20;
            vLayout.childAlignment = TextAnchor.MiddleCenter;
            vLayout.childControlWidth = true;
            vLayout.childControlHeight = false;
            vLayout.padding = new RectOffset(20, 20, 20, 20);
            
            // Title
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(settingsContent.transform, false);
            TextMeshProUGUI titleTmp = titleObj.AddComponent<TextMeshProUGUI>();
            titleTmp.text = "SETTINGS";
            titleTmp.fontSize = 72;
            titleTmp.fontStyle = FontStyles.Bold;
            titleTmp.alignment = TextAlignmentOptions.Center;
            titleTmp.color = Color.white;
            titleTmp.textWrappingMode = TextWrappingModes.NoWrap;
            
            // Volume Slider
            GameObject sliderObj = new GameObject("VolumeSlider");
            sliderObj.transform.SetParent(settingsContent.transform, false);
            Slider slider = sliderObj.AddComponent<Slider>();
            sliderObj.AddComponent<Image>().color = new Color(0.2f, 0.2f, 0.3f, 1f);
            RectTransform sliderRect = sliderObj.GetComponent<RectTransform>();
            sliderRect.sizeDelta = new Vector2(400, 40);
            
            // Slider handle
            GameObject handleObj = new GameObject("Handle Slide Area");
            handleObj.transform.SetParent(sliderObj.transform, false);
            RectTransform handleRect = handleObj.AddComponent<RectTransform>();
            handleRect.anchorMin = Vector2.zero;
            handleRect.anchorMax = Vector2.one;
            handleRect.sizeDelta = Vector2.zero;
            
            GameObject handle = new GameObject("Handle");
            handle.transform.SetParent(handleObj.transform, false);
            RectTransform handleTransform = handle.AddComponent<RectTransform>();
            handleTransform.sizeDelta = new Vector2(20, 40);
            Image handleImg = handle.AddComponent<Image>();
            handleImg.color = Color.white;
            slider.targetGraphic = handleImg;
            slider.handleRect = handleTransform;
            
            GameObject fillObj = new GameObject("Fill Area");
            fillObj.transform.SetParent(sliderObj.transform, false);
            RectTransform fillRect = fillObj.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.sizeDelta = Vector2.zero;
            
            GameObject fill = new GameObject("Fill");
            fill.transform.SetParent(fillObj.transform, false);
            RectTransform fillTransform = fill.AddComponent<RectTransform>();
            fillTransform.anchorMin = Vector2.zero;
            fillTransform.anchorMax = Vector2.one;
            fillTransform.sizeDelta = Vector2.zero;
            Image fillImg = fill.AddComponent<Image>();
            fillImg.color = new Color(0.3f, 0.6f, 1f, 1f);
            slider.fillRect = fillTransform;
            
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = PlayerPrefs.GetFloat("MasterVolume", 1f);
            slider.onValueChanged.AddListener((v) => PlayerPrefs.SetFloat("MasterVolume", v));
            
            // Volume Label
            GameObject volLabel = new GameObject("VolumeLabel");
            volLabel.transform.SetParent(sliderObj.transform, false);
            TextMeshProUGUI volTmp = volLabel.AddComponent<TextMeshProUGUI>();
            volTmp.text = "VOLUME";
            volTmp.fontSize = 36;
            volTmp.alignment = TextAlignmentOptions.Center;
            volTmp.color = Color.white;
            volTmp.textWrappingMode = TextWrappingModes.NoWrap;
            RectTransform volLabelRect = volLabel.GetComponent<RectTransform>();
            volLabelRect.anchoredPosition = new Vector2(0, 30);
            
            // VR Comfort Toggle
            GameObject toggleObj = new GameObject("VRComfortToggle");
            toggleObj.transform.SetParent(settingsContent.transform, false);
            Toggle toggle = toggleObj.AddComponent<Toggle>();
            toggleObj.AddComponent<Image>().color = new Color(0.2f, 0.2f, 0.3f, 1f);
            RectTransform toggleRect = toggleObj.GetComponent<RectTransform>();
            toggleRect.sizeDelta = new Vector2(400, 50);
            
            GameObject toggleBg = new GameObject("Background");
            toggleBg.transform.SetParent(toggleObj.transform, false);
            Image toggleBgImg = toggleBg.AddComponent<Image>();
            toggleBgImg.color = new Color(0.15f, 0.15f, 0.2f, 1f);
            RectTransform bgRect = toggleBg.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;
            
            GameObject checkmark = new GameObject("Checkmark");
            checkmark.transform.SetParent(toggleObj.transform, false);
            Image checkImg = checkmark.AddComponent<Image>();
            checkImg.color = new Color(0.3f, 0.8f, 0.3f, 1f);
            RectTransform checkRect = checkmark.GetComponent<RectTransform>();
            checkRect.anchorMin = new Vector2(0.5f, 0.5f);
            checkRect.anchorMax = new Vector2(0.5f, 0.5f);
            checkRect.sizeDelta = new Vector2(30, 30);
            toggle.targetGraphic = checkImg;
            toggle.graphic = checkImg;
            toggle.isOn = PlayerPrefs.GetInt("VRComfortMode", 0) == 1;
            toggle.onValueChanged.AddListener((v) => PlayerPrefs.SetInt("VRComfortMode", v ? 1 : 0));
            
            GameObject toggleText = new GameObject("Label");
            toggleText.transform.SetParent(toggleObj.transform, false);
            TextMeshProUGUI toggleTmp = toggleText.AddComponent<TextMeshProUGUI>();
            toggleTmp.text = "VR Comfort Mode";
            toggleTmp.fontSize = 36;
            toggleTmp.alignment = TextAlignmentOptions.MidlineLeft;
            toggleTmp.color = Color.white;
            toggleTmp.textWrappingMode = TextWrappingModes.NoWrap;
            
            // Back Button
            GameObject backBtn = CreateBackButton(settingsContent.transform, "BackBtn", () => {
                var mgr = Object.FindFirstObjectByType<TitleSceneManager>();
                if (mgr != null) mgr.OnBackButton();
            });
            
            settingsPanel.SetActive(false);

            // === CREDITS PANEL ===
            GameObject creditsPanel = new GameObject("CreditsPanel");
            creditsPanel.transform.SetParent(canvas.transform, false);
            RectTransform creditsRect = creditsPanel.AddComponent<RectTransform>();
            creditsRect.anchorMin = Vector2.zero;
            creditsRect.anchorMax = Vector2.one;
            creditsRect.offsetMin = Vector2.zero;
            creditsRect.offsetMax = Vector2.zero;
            
            Image creditsBg = creditsPanel.AddComponent<Image>();
            creditsBg.color = new Color(0, 0, 0, 0.95f);
            
            GameObject creditsContent = new GameObject("CreditsContent");
            creditsContent.transform.SetParent(creditsPanel.transform, false);
            RectTransform credRect = creditsContent.AddComponent<RectTransform>();
            credRect.anchorMin = new Vector2(0.5f, 0.5f);
            credRect.anchorMax = new Vector2(0.5f, 0.5f);
            credRect.pivot = new Vector2(0.5f, 0.5f);
            credRect.anchoredPosition = Vector2.zero;
            credRect.sizeDelta = new Vector2(600, 500);
            
            VerticalLayoutGroup credLayout = creditsContent.AddComponent<VerticalLayoutGroup>();
            credLayout.spacing = 15;
            credLayout.childAlignment = TextAnchor.MiddleCenter;
            credLayout.childControlWidth = true;
            credLayout.childControlHeight = false;
            
            string[] creditLines = {
                "CueStrike Team",
                "",
                "Developed by [Your Name]",
                "",
                "Powered by Unity 6000.4.4f1",
                "Normcore Multiplayer",
                "URP (Universal Render Pipeline)",
                "",
                "Special Thanks:",
                "Bo Panda (Mascot)",
                "Uncle Nok (Referee)",
                "",
                "Version 0.6.1"
            };
            
            foreach (string line in creditLines)
            {
                GameObject lineObj = new GameObject("CreditLine");
                lineObj.transform.SetParent(creditsContent.transform, false);
                TextMeshProUGUI lineTmp = lineObj.AddComponent<TextMeshProUGUI>();
                lineTmp.text = line;
                lineTmp.fontSize = line == "" ? 24 : (line.StartsWith("CueStrike") ? 48 : 36);
                lineTmp.fontStyle = line.StartsWith("CueStrike") ? FontStyles.Bold : FontStyles.Normal;
                lineTmp.alignment = TextAlignmentOptions.Center;
                lineTmp.color = line.StartsWith("CueStrike") ? Color.yellow : Color.white;
                lineTmp.textWrappingMode = TextWrappingModes.NoWrap;
            }
            
            // Back Button
            CreateBackButton(creditsContent.transform, "BackBtn", () => {
                var mgr = Object.FindFirstObjectByType<TitleSceneManager>();
                if (mgr != null) mgr.OnBackButton();
            });
            
            creditsPanel.SetActive(false);

            // === COMING SOON PANEL ===
            GameObject comingSoonPanel = new GameObject("ComingSoonPanel");
            comingSoonPanel.transform.SetParent(canvas.transform, false);
            RectTransform soonRect = comingSoonPanel.AddComponent<RectTransform>();
            soonRect.anchorMin = Vector2.zero;
            soonRect.anchorMax = Vector2.one;
            soonRect.offsetMin = Vector2.zero;
            soonRect.offsetMax = Vector2.zero;
            
            Image soonBg = comingSoonPanel.AddComponent<Image>();
            soonBg.color = new Color(0, 0, 0, 0.85f);
            
            GameObject soonContent = new GameObject("SoonContent");
            soonContent.transform.SetParent(comingSoonPanel.transform, false);
            RectTransform soonContentRect = soonContent.AddComponent<RectTransform>();
            soonContentRect.anchorMin = new Vector2(0.5f, 0.5f);
            soonContentRect.anchorMax = new Vector2(0.5f, 0.5f);
            soonContentRect.pivot = new Vector2(0.5f, 0.5f);
            soonContentRect.anchoredPosition = Vector2.zero;
            soonContentRect.sizeDelta = new Vector2(600, 200);
            
            GameObject soonTextObj = new GameObject("SoonText");
            soonTextObj.transform.SetParent(soonContent.transform, false);
            TextMeshProUGUI soonTmp = soonTextObj.AddComponent<TextMeshProUGUI>();
            soonTmp.text = "Coming Soon: Feature";
            soonTmp.fontSize = 60;
            soonTmp.fontStyle = FontStyles.Bold;
            soonTmp.alignment = TextAlignmentOptions.Center;
            soonTmp.color = Color.yellow;
            soonTmp.textWrappingMode = TextWrappingModes.NoWrap;
            
            comingSoonPanel.SetActive(false);

            // === ASSIGN PANEL REFERENCES TO MANAGER ===
            TitleSceneManager mgr = Object.FindFirstObjectByType<TitleSceneManager>();
            if (mgr != null)
            {
                mgr.mainMenuPanel = canvas.transform.Find("ButtonContainer")?.gameObject ?? 
                                   canvas.transform.Find("Panel_BG")?.gameObject;
                
                // Find ButtonContainer as main menu
                Transform btnContainer = canvas.transform.Find("ButtonContainer");
                if (btnContainer != null) mgr.mainMenuPanel = btnContainer.gameObject;
                else mgr.mainMenuPanel = canvas.transform.Find("Panel_BG")?.gameObject;
                
                mgr.settingsPanel = settingsPanel;
                mgr.creditsPanel = creditsPanel;
                mgr.comingSoonPanel = comingSoonPanel;
                mgr.comingSoonText = comingSoonPanel.transform.Find("SoonContent/SoonText")?.GetComponent<TextMeshProUGUI>();
            }
        }

        private static GameObject CreateBackButton(Transform parent, string name, System.Action onClick)
        {
            GameObject btnObj = new GameObject(name);
            btnObj.transform.SetParent(parent, false);
            
            Button btn = btnObj.AddComponent<Button>();
            Image btnImage = btnObj.AddComponent<Image>();
            btnImage.color = new Color(0.3f, 0.2f, 0.2f, 1f);
            
            ColorBlock colors = btn.colors;
            colors.normalColor = new Color(0.3f, 0.2f, 0.2f, 1f);
            colors.highlightedColor = new Color(0.4f, 0.3f, 0.3f, 1f);
            colors.pressedColor = new Color(0.2f, 0.15f, 0.15f, 1f);
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.1f;
            btn.colors = colors;
            
            Navigation nav = btn.navigation;
            nav.mode = Navigation.Mode.Explicit;
            btn.navigation = nav;
            
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(btnObj.transform, false);
            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = "Back";
            tmp.fontSize = 48;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            
            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(20, 10);
            textRect.offsetMax = new Vector2(-20, -10);
            
            RectTransform btnRect = btnObj.GetComponent<RectTransform>();
            btnRect.sizeDelta = new Vector2(200, 60);
            
            if (onClick != null) btn.onClick.AddListener(() => onClick());
            
            return btnObj;
        }

        private static void CreateBoPandaPlaceholder()
        {
            GameObject boPanda = new GameObject("BoPanda_Placeholder");
            boPanda.transform.position = new Vector3(-2.5f, 0.6f, 2f);
            
            // Body (black capsule)
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            body.transform.SetParent(boPanda.transform);
            body.transform.localPosition = new Vector3(0, 0.6f, 0);
            body.transform.localScale = new Vector3(0.4f, 1.2f, 0.4f);
            
            Renderer bodyRend = body.GetComponent<Renderer>();
            Material bodyMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            bodyMat.color = Color.black;
            bodyRend.material = bodyMat;
            Object.DestroyImmediate(body.GetComponent<CapsuleCollider>());
            
            // Head (white sphere)
            GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Head";
            head.transform.SetParent(boPanda.transform);
            head.transform.localPosition = new Vector3(0, 1.3f, 0);
            head.transform.localScale = new Vector3(0.35f, 0.35f, 0.35f);
            
            Renderer headRend = head.GetComponent<Renderer>();
            Material headMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            headMat.color = Color.white;
            headRend.material = headMat;
            Object.DestroyImmediate(head.GetComponent<SphereCollider>());
            
            // Left ear (black sphere)
            GameObject earL = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            earL.name = "EarL";
            earL.transform.SetParent(boPanda.transform);
            earL.transform.localPosition = new Vector3(-0.18f, 1.5f, 0.1f);
            earL.transform.localScale = new Vector3(0.12f, 0.12f, 0.12f);
            Renderer earLRend = earL.GetComponent<Renderer>();
            Material earMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            earMat.color = Color.black;
            earLRend.material = earMat;
            Object.DestroyImmediate(earL.GetComponent<SphereCollider>());
            
            // Right ear (black sphere)
            GameObject earR = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            earR.name = "EarR";
            earR.transform.SetParent(boPanda.transform);
            earR.transform.localPosition = new Vector3(0.18f, 1.5f, 0.1f);
            earR.transform.localScale = new Vector3(0.12f, 0.12f, 0.12f);
            Renderer earRRend = earR.GetComponent<Renderer>();
            earRRend.material = earMat;
            Object.DestroyImmediate(earR.GetComponent<SphereCollider>());
            
            // Eye patch (black)
            GameObject eyePatch = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            eyePatch.name = "EyePatch";
            eyePatch.transform.SetParent(boPanda.transform);
            eyePatch.transform.localPosition = new Vector3(-0.1f, 1.35f, 0.25f);
            eyePatch.transform.localScale = new Vector3(0.08f, 0.08f, 0.05f);
            Renderer patchRend = eyePatch.GetComponent<Renderer>();
            Material patchMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            patchMat.color = Color.black;
            patchRend.material = patchMat;
            Object.DestroyImmediate(eyePatch.GetComponent<SphereCollider>());
            
            // Tag
            boPanda.tag = "NPC";
        }

        #endregion
    }
}