using UnityEngine;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;
using UnityEngine.Rendering;
using System.Reflection;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.XR.OpenXR.Features.MetaQuestSupport;
#endif

/// <summary>
/// VRStartup - Boot-time Quest Optimization & XR Initialization
/// Created by Nari for P'Mong | Phase 2 | 2026-07-20
/// 
/// Attaches to the Boot Scene (Scene 0) via the "NARI CUE STRIKE" editor menu.
/// Runs at DefaultExecutionOrder -1000 to apply settings before any other scripts.
/// </summary>
[DefaultExecutionOrder(-1000)]
public class VRStartup : MonoBehaviour
{
    [Header("Frame Rate & Performance")]
    [Tooltip("0 = Auto-detect from device (Meta Quest 2 = 72Hz, Quest 3/Pro = 90Hz, Quest 3 = 120Hz if enable120Hz, PCVR = 90Hz). Set 72, 90, or 120 to force.")]
    public int targetFrameRate = 0;

    [Header("Quest 3 120Hz (Experimental)")]
    [Tooltip("Opt-in: bump frame rate to 120Hz on detected Quest 3 family. Requires VSync disabled. Off by default.")]
    public bool enable120HzOnQuest3 = false;

    [Header("CPU/GPU Levels (Meta Quest)")]
    [Range(0, 4)] public int cpuLevel = 2;
    [Range(0, 4)] public int gpuLevel = 2;

    [Header("VSync & Multithreaded Rendering")]
    public bool disableVSync = true;
    public bool enableMultithreadedRendering = true;

    [Header("Foveated Rendering (Fixed Foveated Rendering - FFR)")]
    public FoveatedRenderingLevel foveatedRendering = FoveatedRenderingLevel.High;

    [Header("Persistence")]
    public bool persistAcrossScenes = true;

    public enum FoveatedRenderingLevel
    {
        Off = 0,
        Low = 1,
        Medium = 2,
        High = 3
    }

    private static bool s_Initialized = false;
    private static VRStartup s_InitInstance = null; // tracks which GO ran init, for correct OnDestroy reset

    void Awake()
    {
        if (persistAcrossScenes)
        {
            DontDestroyOnLoad(gameObject);
        }

        if (s_Initialized) return;
        s_Initialized = true;
        s_InitInstance = this;

        ApplyQuestOptimizations();
        
#if UNITY_EDITOR
        ConfigureOpenXRFeatures();
#endif
    }

        private void ApplyQuestOptimizations()
        {
            // Frame Rate
            if (targetFrameRate > 0)
            {
                Application.targetFrameRate = targetFrameRate;
            }
            else
            {
                Application.targetFrameRate = AutoDetectFrameRate();
            }

            // VSync
            QualitySettings.vSyncCount = disableVSync ? 0 : 1;

            // Multithreaded Rendering (Editor only - applied at build time)
#if UNITY_EDITOR
            PlayerSettings.SetMobileMTRendering(BuildTargetGroup.Android, enableMultithreadedRendering);
#endif

            // CPU/GPU Levels (Meta Quest) - Runtime via OVRManager
            SetCpuGpuLevels();

            // Fixed Foveated Rendering - Runtime via OpenXR
            SetFoveatedRenderingLevel(foveatedRendering);

            Debug.Log($"[VRStartup] Quest optimizations applied: {Application.targetFrameRate}Hz ({DetectDeviceLabel()}), CPU Lv{cpuLevel}, GPU Lv{gpuLevel}, FFR: {foveatedRendering}");
        }

        // Detect frame rate from SystemInfo.deviceModel (Meta Quest 2 = 72Hz, Quest 3 family = 90 or 120Hz, PCVR/Editor = 90Hz).
        // We use substring match because deviceModel is a free-form string and the exact casing varies by Unity/Oculus plugin version.
        private int AutoDetectFrameRate()
        {
            string model = SystemInfo.deviceModel ?? string.Empty;
            string lowered = model.ToLowerInvariant();

            // Quest 3 family: "Meta Quest 3", "Meta Quest 3S", "Meta Quest Pro"
            // Optional 120Hz opt-in only on Quest 3 (not Pro/3S to be conservative).
            if (lowered.Contains("quest 3") && !lowered.Contains("3s") && !lowered.Contains("pro"))
            {
                return enable120HzOnQuest3 ? 120 : 90;
            }
            if (lowered.Contains("quest 3s") || lowered.Contains("quest pro"))
            {
                return 90;
            }

            // Quest 2 / Quest 1: 72Hz (native refresh).
            // We exclude "2" prefix with care — query both "quest 2" AND just "quest" to catch the 1st-gen.
            if (lowered.Contains("quest 2") || lowered.Contains("oculus quest"))
            {
                return 72;
            }

            // Unknown device — default to 90Hz (safe for PCVR + Editor + Stage).
            return 90;
        }

        // Human-readable device label for logging only — never used as a key.
        private string DetectDeviceLabel()
        {
            string model = SystemInfo.deviceModel;
            return string.IsNullOrEmpty(model) ? "Unknown" : model;
        }

#if UNITY_EDITOR
    private void ConfigureOpenXRFeatures()
    {
        var openXRSettings = OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Android);
        if (openXRSettings == null) return;

        // Use reflection to access Meta Quest features (Editor-only assembly)
        var metaQuestSupportAssembly = System.Reflection.Assembly.Load("UnityEditor.XR.OpenXR.Features.MetaQuestSupport");
        if (metaQuestSupportAssembly != null)
        {
            // MetaQuestFeature
            var metaFeatureType = metaQuestSupportAssembly.GetType("UnityEditor.XR.OpenXR.Features.MetaQuestSupport.MetaQuestFeature");
            if (metaFeatureType != null)
            {
                var getFeatureMethod = typeof(OpenXRSettings).GetMethod("GetFeature", System.Type.EmptyTypes).MakeGenericMethod(metaFeatureType);
                var metaFeature = getFeatureMethod.Invoke(openXRSettings, null);
                if (metaFeature != null)
                {
                    var enabledProp = metaFeatureType.GetProperty("enabled", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                    if (enabledProp != null) enabledProp.SetValue(metaFeature, true);
                }
            }

            // MetaHandTrackingFeature
            var handTrackingType = metaQuestSupportAssembly.GetType("UnityEditor.XR.OpenXR.Features.MetaQuestSupport.MetaHandTrackingFeature");
            if (handTrackingType != null)
            {
                var getFeatureMethod = typeof(OpenXRSettings).GetMethod("GetFeature", System.Type.EmptyTypes).MakeGenericMethod(handTrackingType);
                var handTrackingFeature = getFeatureMethod.Invoke(openXRSettings, null);
                if (handTrackingFeature != null)
                {
                    var enabledProp = handTrackingType.GetProperty("enabled", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                    if (enabledProp != null) enabledProp.SetValue(handTrackingFeature, true);
                }
            }

            // MetaPassthroughFeature
            var passthroughType = metaQuestSupportAssembly.GetType("UnityEditor.XR.OpenXR.Features.MetaQuestSupport.MetaPassthroughFeature");
            if (passthroughType != null)
            {
                var getFeatureMethod = typeof(OpenXRSettings).GetMethod("GetFeature", System.Type.EmptyTypes).MakeGenericMethod(passthroughType);
                var passthroughFeature = getFeatureMethod.Invoke(openXRSettings, null);
                if (passthroughFeature != null)
                {
                    var enabledProp = passthroughType.GetProperty("enabled", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                    if (enabledProp != null) enabledProp.SetValue(passthroughFeature, true);
                }
            }

            // MetaQuestFoveationFeature
            var foveationType = metaQuestSupportAssembly.GetType("UnityEditor.XR.OpenXR.Features.MetaQuestSupport.MetaQuestFoveationFeature");
            if (foveationType != null)
            {
                var getFeatureMethod = typeof(OpenXRSettings).GetMethod("GetFeature", System.Type.EmptyTypes).MakeGenericMethod(foveationType);
                var foveationFeature = getFeatureMethod.Invoke(openXRSettings, null);
                if (foveationFeature != null)
                {
                    var enabledProp = foveationType.GetProperty("enabled", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                    if (enabledProp != null) enabledProp.SetValue(foveationFeature, true);
                }
            }
        }

        Debug.Log("[VRStartup] OpenXR Meta Quest features configured");
    }
#endif

    private void SetCpuGpuLevels()
    {
        try
        {
            // Use reflection to access OVRManager for CPU/GPU levels
            var ovrManagerType = System.Type.GetType("OVRManager, Assembly-CSharp");
            if (ovrManagerType != null)
            {
                var cpuProp = ovrManagerType.GetProperty("cpuLevel", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
                var gpuProp = ovrManagerType.GetProperty("gpuLevel", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
                if (cpuProp != null) cpuProp.SetValue(null, cpuLevel);
                if (gpuProp != null) gpuProp.SetValue(null, gpuLevel);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[VRStartup] Could not set CPU/GPU levels via OVRManager: {e.Message}");
        }
    }

    private void SetFoveatedRenderingLevel(FoveatedRenderingLevel level)
    {
        try
        {
            // Use OpenXR Meta Quest Feature API for FFR at runtime (Editor only)
#if UNITY_EDITOR
            var openXRSettings = OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Android);
            if (openXRSettings != null)
            {
                // Use reflection to get MetaQuestFoveationFeature type (Editor-only assembly)
                var foveationFeatureType = System.Type.GetType("UnityEditor.XR.OpenXR.Features.MetaQuestSupport.MetaQuestFoveationFeature, UnityEditor.XR.OpenXR.Features.MetaQuestSupport");
                if (foveationFeatureType != null)
                {
                    var getFeatureMethod = typeof(OpenXRSettings).GetMethod("GetFeature", System.Type.EmptyTypes).MakeGenericMethod(foveationFeatureType);
                    var ffrFeature = getFeatureMethod.Invoke(openXRSettings, null);
                    if (ffrFeature != null)
                    {
                        var enabledProp = foveationFeatureType.GetProperty("enabled", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                        if (enabledProp != null)
                        {
                            enabledProp.SetValue(ffrFeature, level != FoveatedRenderingLevel.Off);
                        }
                        if (level != FoveatedRenderingLevel.Off)
                        {
                            var levelProp = foveationFeatureType.GetProperty("level", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                            if (levelProp != null)
                            {
                                levelProp.SetValue(ffrFeature, (int)level);
                            }
                        }
                    }
                }
            }
#endif
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[VRStartup] Could not set FFR level: {e.Message}");
        }
    }

    void OnDestroy()
    {
        // Only reset when the instance that originally ran the optimizations is destroyed.
        // (DontDestroyOnLoad normally prevents this, but on domain reload / explicit Destroy,
        // we don't want a subsequent BootManager instance to skip initialization.)
        if (s_InitInstance == this)
        {
            s_Initialized = false;
            s_InitInstance = null;
        }
    }
}
