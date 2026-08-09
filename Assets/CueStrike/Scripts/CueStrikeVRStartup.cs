using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;
using Unity.XR.CoreUtils;
 
// Duplicate using UnityEngine.Rendering removed
using UnityEngine.Rendering.Universal;

/// <summary>
/// VR Startup Manager - Handles XR initialization and scene loading for Quest
/// Attached to Boot scene GameObject
/// </summary>
public class CueStrikeVRStartup : MonoBehaviour
{
    [Header("Scene Names")]
    [SerializeField] private string mainSceneName = "Main";
    [SerializeField] private string bootSceneName = "Boot";

    [Header("XR Settings")]
    [SerializeField] private bool autoInitializeXR = true;
    [SerializeField] private float xrInitTimeout = 10f;

    [Header("Quality Settings")]
    [SerializeField] private int targetFrameRate = 72;
    [SerializeField] private int renderScale = 100; // Percentage

    private bool xrInitialized = false;
    private AsyncOperation sceneLoadOperation;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        Application.targetFrameRate = targetFrameRate;

        if (autoInitializeXR)
        {
            InitializeXR();
        }
    }

    private async void InitializeXR()
    {
        Debug.Log("[CueStrikeVRStartup] Initializing XR for Quest...");

        // Wait for XR to initialize
        float timer = 0f;
        while (!XRGeneralSettings.Instance.Manager.isInitializationComplete && timer < xrInitTimeout)
        {
            await System.Threading.Tasks.Task.Yield();
            timer += Time.unscaledDeltaTime;
        }

        if (XRGeneralSettings.Instance.Manager.isInitializationComplete)
        {
            xrInitialized = true;
            Debug.Log("[CueStrikeVRStartup] XR Initialized successfully");

            // Apply render scale for Quest performance
            ApplyRenderScale();

            // Load main scene
            LoadMainScene();
        }
        else
        {
            Debug.LogError("[CueStrikeVRStartup] XR Initialization timed out!");
            // Fallback: load main scene anyway
            LoadMainScene();
        }
    }

    private void ApplyRenderScale()
    {
        var urpAsset = GraphicsSettings.defaultRenderPipeline as UniversalRenderPipelineAsset;
        if (urpAsset != null)
        {
            urpAsset.renderScale = renderScale / 100f;
            Debug.Log($"[CueStrikeVRStartup] URP Render Scale set to {renderScale}%");
        }
    }

    private void LoadMainScene()
    {
        Debug.Log($"[CueStrikeVRStartup] Loading Main Scene: {mainSceneName}");
        sceneLoadOperation = SceneManager.LoadSceneAsync(mainSceneName, LoadSceneMode.Additive);
        sceneLoadOperation.completed += OnMainSceneLoaded;
    }

    private void OnMainSceneLoaded(AsyncOperation obj)
    {
        if (sceneLoadOperation.isDone)
        {
            var mainScene = SceneManager.GetSceneByName(mainSceneName);
            if (mainScene.IsValid())
            {
                SceneManager.SetActiveScene(mainScene);
                Debug.Log($"[CueStrikeVRStartup] Main Scene '{mainSceneName}' loaded and activated");
            }

            // Unload boot scene after main is loaded
            var bootScene = SceneManager.GetSceneByName(bootSceneName);
            if (bootScene.IsValid() && bootScene != mainScene)
            {
                SceneManager.UnloadSceneAsync(bootSceneName);
                Debug.Log($"[CueStrikeVRStartup] Boot Scene '{bootSceneName}' unloaded");
            }
        }
    }

    /// <summary>
    /// Manual XR Initialization trigger
    /// </summary>
    public void InitializeXRManually()
    {
        if (!xrInitialized)
        {
            InitializeXR();
        }
    }

    /// <summary>
    /// Recenter XR tracking (for Quest guardian reset)
    /// </summary>
    public void RecenterTracking()
    {
        var xrInputSubsystem = XRGeneralSettings.Instance?.Manager?.activeLoader?.GetLoadedSubsystem<UnityEngine.XR.XRInputSubsystem>();
        if (xrInputSubsystem != null)
        {
            xrInputSubsystem.TryRecenter();
            Debug.Log("[CueStrikeVRStartup] Tracking recentered");
        }
    }

    /// <summary>
    /// Set target frame rate
    /// </summary>
    public void SetTargetFrameRate(int fps)
    {
        targetFrameRate = fps;
        Application.targetFrameRate = fps;
    }

    /// <summary>
    /// Set render scale percentage (50-150%)
    /// </summary>
    public void SetRenderScale(int scalePercent)
    {
        renderScale = Mathf.Clamp(scalePercent, 50, 150);
        ApplyRenderScale();
    }

    private void OnDestroy()
    {
        if (sceneLoadOperation != null && !sceneLoadOperation.isDone)
        {
            sceneLoadOperation.allowSceneActivation = false;
        }
    }
}