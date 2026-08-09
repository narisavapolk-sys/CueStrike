using UnityEditor.Android;
using System.IO;
using UnityEngine;

public class CueStrikeGradlePostProcessor : IPostGenerateGradleAndroidProject
{
    public int callbackOrder => 999;

    public void OnPostGenerateGradleAndroidProject(string path)
    {
        Debug.Log($"[CueStrike Gradle PostProcessor] Path: {path}");
        
        // path is usually: "Library/Bee/Android/Prj/IL2CPP/Gradle/unityLibrary"
        // local.properties is in the parent directory "Library/Bee/Android/Prj/IL2CPP/Gradle"
        string rootDir = Path.GetDirectoryName(path);
        if (string.IsNullOrEmpty(rootDir))
        {
            Debug.LogError("[CueStrike Gradle PostProcessor] Root directory is null or empty!");
            return;
        }

        string localPropertiesPath = Path.Combine(rootDir, "local.properties");
        
        if (File.Exists(localPropertiesPath))
        {
            Debug.Log($"[CueStrike Gradle PostProcessor] Editing local.properties at {localPropertiesPath}");
            // Write the path with proper Gradle formatting
            string content = "sdk.dir=C\\:\\\\Users\\\\mongo\\\\AndroidSDK\n";
            File.WriteAllText(localPropertiesPath, content);
            Debug.Log("[CueStrike Gradle PostProcessor] Successfully updated sdk.dir to C:\\Users\\mongo\\AndroidSDK");
        }
        else
        {
            Debug.LogWarning($"[CueStrike Gradle PostProcessor] local.properties not found at {localPropertiesPath}");
        }

        // Copy all accepted licenses directly into the Gradle root project
        string sourceLicensesDir = @"C:\Users\mongo\AndroidSDK\licenses";
        string targetLicensesDir = Path.Combine(rootDir, "licenses");
        if (Directory.Exists(sourceLicensesDir))
        {
            if (!Directory.Exists(targetLicensesDir))
            {
                Directory.CreateDirectory(targetLicensesDir);
            }
            foreach (string file in Directory.GetFiles(sourceLicensesDir))
            {
                string destFile = Path.Combine(targetLicensesDir, Path.GetFileName(file));
                File.Copy(file, destFile, true);
            }
            Debug.Log("[CueStrike Gradle PostProcessor] Successfully copied all SDK licenses directly to Gradle project!");
        }
        else
        {
            Debug.LogWarning($"[CueStrike Gradle PostProcessor] Source licenses directory not found at {sourceLicensesDir}");
        }
    }
}
