using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace CueStrike.VR
{
    /// <summary>
    /// Gold Standard VR Loading Screen.
    /// Loads scenes asynchronously, displays a progress bar in 3D world space in front of the player's face,
    /// and prevents VR headset freeze / motion sickness during loading transitions.
    /// </summary>
    public class CueStrikeLoadingScreen : MonoBehaviour
    {
        private static CueStrikeLoadingScreen _instance;

        public static void LoadScene(string sceneName)
        {
            if (_instance == null)
            {
                var go = new GameObject("VR_LoadingScreen_Manager");
                _instance = go.AddComponent<CueStrikeLoadingScreen>();
                DontDestroyOnLoad(go);
            }
            _instance.StartCoroutine(_instance.LoadAsyncCoroutine(sceneName));
        }

        [Header("UI Styling")]
        private Canvas _canvas;
        private Image _progressBarFill;
        private Text _progressText;
        private Text _tipText;
        private Image _background;

        private readonly string[] LoadingTips = new string[]
        {
            "Tip: Squeeze shots (jump shots) are allowed in Pool, but illegal in Snooker!",
            "Tip: Use the dominant hand setting in the options panel to adjust your cue grip.",
            "Tip: Mute your microphone or opponent via the HUD menu if voice chat is too noisy.",
            "Tip: Center your Real Cue Adapter (RCA) to align your real-world cue stick precisely.",
            "Tip: Applying spin (English) to the cue ball can help you position for the next shot.",
            "Tip: Different rooms have unique acoustics and visual ambient styles."
        };

        private void Awake()
        {
            CreateLoadingCanvas();
        }

        private void CreateLoadingCanvas()
        {
            // 1. Create Canvas GameObject
            var canvasGO = new GameObject("LoadingCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGO.transform.SetParent(transform);
            
            _canvas = canvasGO.GetComponent<Canvas>();
            _canvas.renderMode = RenderMode.WorldSpace; // VR friendly

            var scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 5f;

            var rect = canvasGO.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(1920f, 1080f);
            rect.localScale = Vector3.one * 0.002f; // Scale down for world space (approx 3.8m wide at scale 1)

            // 2. Background
            var bgGO = new GameObject("Background", typeof(Image));
            bgGO.transform.SetParent(canvasGO.transform, false);
            _background = bgGO.GetComponent<Image>();
            _background.color = new Color(0.02f, 0.05f, 0.08f, 1f); // Deep midnight blue
            var bgRect = bgGO.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;

            // 3. Panel Container
            var containerGO = new GameObject("Container");
            containerGO.transform.SetParent(canvasGO.transform, false);
            var containerRect = containerGO.AddComponent<RectTransform>();
            containerRect.sizeDelta = new Vector2(1000f, 600f);

            // 4. Loading Title Text
            var titleGO = new GameObject("LoadingText", typeof(Text));
            titleGO.transform.SetParent(containerGO.transform, false);
            var titleText = titleGO.GetComponent<Text>();
            titleText.text = "LOADING ARENA...";
            titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            titleText.fontSize = 48;
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.color = new Color(1f, 0.82f, 0.2f); // Gold
            var titleRect = titleGO.GetComponent<RectTransform>();
            titleRect.anchoredPosition = new Vector3(0, 150, 0);
            titleRect.sizeDelta = new Vector2(800, 100);

            // 5. Progress Bar Background
            var barBgGO = new GameObject("ProgressBar_BG", typeof(Image));
            barBgGO.transform.SetParent(containerGO.transform, false);
            var barBgImg = barBgGO.GetComponent<Image>();
            barBgImg.color = new Color(1f, 1f, 1f, 0.1f);
            var barBgRect = barBgGO.GetComponent<RectTransform>();
            barBgRect.sizeDelta = new Vector2(600f, 24f);
            barBgRect.anchoredPosition = new Vector3(0, 30, 0);

            // 6. Progress Bar Fill
            var barFillGO = new GameObject("ProgressBar_Fill", typeof(Image));
            barFillGO.transform.SetParent(barBgGO.transform, false);
            _progressBarFill = barFillGO.GetComponent<Image>();
            _progressBarFill.color = new Color(0f, 0.8f, 0.4f); // Neon green
            _progressBarFill.type = Image.Type.Filled;
            _progressBarFill.fillMethod = Image.FillMethod.Horizontal;
            _progressBarFill.fillAmount = 0f;
            var barFillRect = barFillGO.GetComponent<RectTransform>();
            barFillRect.anchorMin = Vector2.zero;
            barFillRect.anchorMax = Vector2.one;
            barFillRect.offsetMin = Vector2.zero;
            barFillRect.offsetMax = Vector2.zero;

            // 7. Progress Text
            var progressTextGO = new GameObject("ProgressText", typeof(Text));
            progressTextGO.transform.SetParent(containerGO.transform, false);
            _progressText = progressTextGO.GetComponent<Text>();
            _progressText.text = "0%";
            _progressText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _progressText.fontSize = 24;
            _progressText.alignment = TextAnchor.MiddleCenter;
            _progressText.color = Color.white;
            var progressTextRect = progressTextGO.GetComponent<RectTransform>();
            progressTextRect.anchoredPosition = new Vector3(0, -20, 0);
            progressTextRect.sizeDelta = new Vector2(200, 50);

            // 8. Tip Text
            var tipGO = new GameObject("TipText", typeof(Text));
            tipGO.transform.SetParent(containerGO.transform, false);
            _tipText = tipGO.GetComponent<Text>();
            _tipText.text = "Tip: Squeeze shots (jump shots) are allowed in Pool, but illegal in Snooker!";
            _tipText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _tipText.fontSize = 20;
            _tipText.alignment = TextAnchor.MiddleCenter;
            _tipText.color = new Color(1f, 1f, 1f, 0.6f);
            var tipRect = tipGO.GetComponent<RectTransform>();
            tipRect.anchoredPosition = new Vector3(0, -180, 0);
            tipRect.sizeDelta = new Vector2(850, 100);

            // Hide initially
            _canvas.gameObject.SetActive(false);
        }

        private void PositionCanvasInFrontOfCamera()
        {
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                // Place 2.5 meters in front of the camera, facing it
                transform.position = mainCam.transform.position + mainCam.transform.forward * 2.5f;
                transform.rotation = Quaternion.LookRotation(transform.position - mainCam.transform.position);
            }
            else
            {
                // Fallback position
                transform.position = new Vector3(0f, 1.5f, 2f);
                transform.rotation = Quaternion.identity;
            }
        }

        private IEnumerator LoadAsyncCoroutine(string sceneName)
        {
            // Pick a random tip
            if (_tipText != null && LoadingTips.Length > 0)
            {
                _tipText.text = LoadingTips[Random.Range(0, LoadingTips.Length)];
            }

            // Position and show loading screen
            PositionCanvasInFrontOfCamera();
            _canvas.gameObject.SetActive(true);

            // Fade in background and elements
            yield return StartCoroutine(FadeCanvas(0f, 1f, 0.25f));

            // Load scene asynchronously
            AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
            op.allowSceneActivation = false;

            while (!op.isDone)
            {
                float progress = Mathf.Clamp01(op.progress / 0.9f);
                
                if (_progressBarFill != null) _progressBarFill.fillAmount = progress;
                if (_progressText != null) _progressText.text = $"{(progress * 100f):F0}%";

                // Normcore and Unity needs time to load assets without locking main thread
                if (op.progress >= 0.9f)
                {
                    // Fully loaded, trigger activation
                    op.allowSceneActivation = true;
                }

                yield return null;
            }

            // Re-align canvas to new main camera in the loaded scene
            PositionCanvasInFrontOfCamera();

            // Hold loading screen for a split second to smooth the camera transition
            yield return new WaitForSeconds(0.4f);

            // Fade out
            yield return StartCoroutine(FadeCanvas(1f, 0f, 0.3f));

            _canvas.gameObject.SetActive(false);
        }

        private IEnumerator FadeCanvas(float startAlpha, float endAlpha, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                float alpha = Mathf.Lerp(startAlpha, endAlpha, t);

                if (_background != null) _background.color = new Color(0.02f, 0.05f, 0.08f, alpha);
                if (_progressBarFill != null) _progressBarFill.color = new Color(0f, 0.8f, 0.4f, alpha);
                if (_progressText != null) _progressText.color = new Color(1f, 1f, 1f, alpha);
                if (_tipText != null) _tipText.color = new Color(1f, 1f, 1f, alpha * 0.6f);

                yield return null;
            }
        }
    }
}
