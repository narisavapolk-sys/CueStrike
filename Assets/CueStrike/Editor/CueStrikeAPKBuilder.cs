using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// CueStrikeAPKBuilder - Automated Quest 2/3 APK Build Pipeline
/// Created by Nari for P'Mong | 2026-07-19
/// </summary>
public class CueStrikeAPKBuilder : EditorWindow
{
    [MenuItem("Tools/CueStrike/Build/Build Quest APK (Development)")]
    public static void BuildQuestDevelopment()
    {
        BuildQuestAPK(true);
    }

    [MenuItem("Tools/CueStrike/Build/Build Quest APK (Release)")]
    public static void BuildQuestRelease()
    {
        BuildQuestAPK(false);
    }

    private static void BuildQuestAPK(bool development)
    {
        string[] scenes = GetEnabledScenes();
        if (scenes.Length == 0)
        {
            EditorUtility.DisplayDialog("Build Error", "No scenes in build settings!", "OK");
            return;
        }

        // Validate Android SDK/NDK
        string sdkPath = EditorPrefs.GetString("AndroidSdkRoot");
        string ndkPath = EditorPrefs.GetString("AndroidNdkRoot");
        string jdkPath = EditorPrefs.GetString("JdkPath");

        bool sdkOk = !string.IsNullOrEmpty(sdkPath) && System.IO.Directory.Exists(sdkPath);
        bool ndkOk = !string.IsNullOrEmpty(ndkPath) && System.IO.Directory.Exists(ndkPath);

        string sdkStatus = sdkOk ? "Android SDK detected" : "SDK path issue (see below)";
        string report =
            $"Android SDK:  {(sdkOk ? "OK" : "MISSING")} {sdkPath}\n" +
            $"Android NDK:  {(ndkOk ? "OK" : "MISSING")} {ndkPath}\n" +
            $"JDK:          {jdkPath}\n\n";

        if (!sdkOk || !ndkOk)
        {
            report += "Missing SDK or NDK!\n\n" +
                      "HOW TO FIX:\n" +
                      "1. Open Unity Hub\n" +
                      "2. Go to Installs > your Unity version > Gear icon > Add Modules\n" +
                      "3. Check: Android Build Support\n" +
                      "   - Android SDK & NDK Tools\n" +
                      "   - OpenJDK\n" +
                      "4. Apply and wait for install\n";
            EditorUtility.DisplayDialog("Missing SDK/NDK", report, "Build Anyway", "Cancel");
        }
        else
        {
            report += "All tools found. Ready to build!";
            if (EditorUtility.DisplayDialog("Ready to Build", report, "Build", "Cancel"))
            {
                PerformBuild(scenes, development);
            }
        }
    }

    private static string[] GetEnabledScenes()
    {
        var scenes = new System.Collections.Generic.List<string>();
        foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
        {
            if (scene.enabled) scenes.Add(scene.path);
        }
        return scenes.ToArray();
    }

    private static void PerformBuild(string[] scenes, bool development)
    {
        string targetLabel = development ? "Development" : "Release";
        string outputPath = $"Builds/Android/CueStrike_Quest_{targetLabel}_{System.DateTime.Now:yyyyMMdd_HHmm}.apk";

        var buildOptions = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = outputPath,
            target = BuildTarget.Android,
            options = development ? BuildOptions.Development : BuildOptions.None
        };

        // Build settings for Quest (OpenGL ES 3) - Unity 6 compatible
        PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { UnityEngine.Rendering.GraphicsDeviceType.OpenGLES3 });
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel29; // Quest 2 requires API 29+
        PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevel34;
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        // PlayerSettings.virtualRealitySupported is obsolete - VR enabled via XR Management
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.stripEngineCode = true;
        PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.Android, ManagedStrippingLevel.High);

        // URP settings for Quest
        if (UnityEngine.Rendering.Universal.UniversalRenderPipeline.asset != null)
        {
            var urpAsset = UnityEngine.Rendering.Universal.UniversalRenderPipeline.asset;
            urpAsset.renderScale = 1.0f;
            urpAsset.msaaSampleCount = 2;
            // depthPrimingMode doesn't exist in current URP
            // urpAsset.depthPrimingMode = UnityEngine.Rendering.Universal.DepthPrimingMode.Disabled;
            // supportsDynamicBatching doesn't exist in current URP
            // urpAsset.supportsDynamicBatching = true;
            // supportsGPUInstancing doesn't exist in current URP
            // urpAsset.supportsGPUInstancing = true;
            // supportsTerrainHoles is read-only
            // urpAsset.supportsTerrainHoles = false;
        }

        Debug.Log($"[CueStrike Build] Starting {targetLabel} build...");

        var report = BuildPipeline.BuildPlayer(buildOptions);
        var summary = report.summary;

        if (summary.result == BuildResult.Succeeded)
        {
            float mb = summary.totalSize / (1024f * 1024f);
            EditorUtility.DisplayDialog($"Build Succeeded - {targetLabel}",
                $"APK ready!\n\n{outputPath}\nSize: {mb:F1} MB", "OK");
            Debug.Log($"[CueStrike Build] {targetLabel} build succeeded -> {outputPath} ({mb:F1} MB)");
        }
        else
        {
            EditorUtility.DisplayDialog($"Build Failed - {targetLabel}",
                $"{summary.totalErrors} error(s). Check Console for details.", "OK");
            Debug.LogError($"[CueStrike Build] {targetLabel} failed: {summary.result}");
        }
    }
}