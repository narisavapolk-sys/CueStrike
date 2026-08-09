//
// CueStrikeRendererFeatureSetup — AAA Renderer Feature Injection
// Created by Nari for P'Mong | 2026-07-20
// Phase 1: Lighting/Shadow AAA — configures URP Renderer asset with realism features
// Compatible with Unity 6 (6000.x) / URP 17+
//
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEditor;
using System.Reflection;
using System.Collections;

namespace CueStrike.Editor
{
    /// <summary>
    /// One-click AAA Renderer Feature Injection for URP 17+
    /// Adds SSAO (Screen Space Ambient Occlusion) to the active URP Renderer Data.
    /// ScreenSpaceShadows is internal in URP 17+, skipped. Use Decal Projector for contact shadows instead.
    /// Run via menu: Tools/CueStrike/Rendering/Inject AAA Renderer Features
    /// </summary>
    public static class CueStrikeRendererFeatureSetup
    {
        [MenuItem("Tools/CueStrike/Rendering/Inject AAA Renderer Features", false, 1)]
        public static void InjectAAAFeatures()
        {
            var rendererData = GetActiveURPRendererData();
            if (rendererData == null)
            {
                UnityEngine.Debug.LogError("[CueStrike] No UniversalRendererData found. Ensure URP is set up correctly.");
                EditorUtility.DisplayDialog("CueStrike Renderer AAA", "No UniversalRendererData found.\nEnsure URP is configured in Graphics Settings.", "OK");
                return;
            }

            int added = 0;

            // ----- 1. SSAO (Screen Space Ambient Occlusion) -----
            // Note: ScreenSpaceAmbientOcclusion is internal in URP 17+. Use reflection.
            if (!HasFeatureByName(rendererData, "ScreenSpaceAmbientOcclusion"))
            {
                var ssaoType = typeof(UniversalRendererData).Assembly.GetType("UnityEngine.Rendering.Universal.ScreenSpaceAmbientOcclusion");
                if (ssaoType != null)
                {
                    var ssao = ScriptableObject.CreateInstance(ssaoType) as ScriptableRendererFeature;
                    if (ssao != null)
                    {
                        ssao.name = "CueStrike SSAO";
                        ConfigureSSAO(ssao);
                        AddRendererFeature(rendererData, ssao);
                        added++;
                    }
                    else
                    {
                        UnityEngine.Debug.LogWarning("[CueStrike] Created SSAO instance is not a ScriptableRendererFeature.");
                    }
                }
                else
                {
                    UnityEngine.Debug.LogWarning("[CueStrike] ScreenSpaceAmbientOcclusion type not found in URP assembly.");
                }
            }

            // ----- 2. Screen Space Shadows (Internal in URP 17+) -----
            // ScreenSpaceShadows is now internal in URP 17+. 
            // Use Decal Projector (CueStrikeDecalSetup) for contact shadows instead.
            // Note: ScreenSpaceShadows type is internal in URP 17+, cannot be instantiated via reflection.
            UnityEngine.Debug.Log("[CueStrike] ScreenSpaceShadows is internal in URP 17+. Using Decal Projector for contact shadows instead.");

            // NOTE: Decal Projector for chalk marks on cloth will be added via CueStrikeDecalSetup in Phase 2

            EditorUtility.SetDirty(rendererData);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            UnityEngine.Debug.Log($"[CueStrike] AAA Renderer Features injected -> {added} new feature(s) on {rendererData.name}");
            EditorUtility.DisplayDialog(
                "CueStrike Renderer AAA",
                $"AAA Renderer Features added successfully.\nNew features: {added}\n- SSAO (Contact + Ambient Occlusion)\n\nScreen Space Shadows is internal in URP 17+.\nUse CueStrikeDecalSetup for contact shadows on cloth.\n\nEnter Play Mode to see the visual improvement.",
                "OK");
        }

        // Find the URP Renderer Data asset currently in use via Graphics Settings
        private static UniversalRendererData GetActiveURPRendererData()
        {
            // Search all UniversalRendererData assets
            var guids = AssetDatabase.FindAssets("t:UniversalRendererData");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var rd = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(path);
                if (rd != null && rd.name.Contains("PC")) return rd;
            }
            // Fallback: first one found
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var rd = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(path);
                if (rd != null) return rd;
            }
            return null;
        }

        private static bool HasFeatureByName(UniversalRendererData rendererData, string typeName)
        {
            // Use reflection to access rendererFeatures since it may be internal
            var featuresField = typeof(UniversalRendererData).GetField("m_RendererFeatures", 
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (featuresField != null)
            {
                var features = featuresField.GetValue(rendererData) as IList;
                if (features != null)
                {
                    foreach (var f in features)
                    {
                        if (f != null && f.GetType().Name == typeName) return true;
                    }
                }
            }
            
            // Fallback: try rendererFeatures property
            var rendererFeaturesProp = typeof(UniversalRendererData).GetProperty("rendererFeatures",
                BindingFlags.Public | BindingFlags.Instance);
            if (rendererFeaturesProp != null)
            {
                var features = rendererFeaturesProp.GetValue(rendererData) as IList;
                if (features != null)
                {
                    foreach (var f in features)
                    {
                        if (f != null && f.GetType().Name == typeName) return true;
                    }
                }
            }
            return false;
        }

        // Add renderer feature via reflection (m_RendererFeatures is the internal field in URP 17+)
        private static void AddRendererFeature(UniversalRendererData rendererData, ScriptableRendererFeature feature)
        {
            var featuresField = typeof(UniversalRendererData).GetField("m_RendererFeatures", 
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (featuresField != null)
            {
                var features = featuresField.GetValue(rendererData) as IList;
                if (features != null)
                {
                    features.Add(feature);
                    return;
                }
            }
            
            // Fallback: try property
            var featuresProp = typeof(UniversalRendererData).GetProperty("rendererFeatures",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (featuresProp != null && featuresProp.CanWrite)
            {
                var features = featuresProp.GetValue(rendererData) as IList;
                if (features != null)
                {
                    features.Add(feature);
                    return;
                }
            }
        }

        // Configure SSAO settings via reflection (settings may be internal in URP 17+)
        // URP 17+ SSAO uses different property names: intensity, radius, sampleCount, etc.
        private static void ConfigureSSAO(ScriptableRendererFeature feature)
        {
            var settingsField = feature.GetType().GetField("m_Settings", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (settingsField == null) return;

            var settings = settingsField.GetValue(feature);
            if (settings == null) return;

            var settingsType = settings.GetType();

            // Set properties via reflection - use int values directly since enum may be internal in URP 17+
            // URP 17+ SSAO settings properties: intensity, radius, sampleCount, etc.
            // Note: "source" property may not exist in newer versions - try common property names
            SetProperty(settingsType, settings, "intensity", 0.85f);
            SetProperty(settingsType, settings, "radius", 0.35f);
            SetProperty(settingsType, settings, "sampleCount", 1);
            
            // Try alternative property names for URP 17+
            SetProperty(settingsType, settings, "quality", 1); // Alternative for sampleCount
            SetProperty(settingsType, settings, "directLightingStrength", 1f); // If available
        }

        private static void SetProperty(System.Type type, object instance, string propertyName, object value)
        {
            var prop = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (prop != null && prop.CanWrite)
            {
                prop.SetValue(instance, value);
            }
            else
            {
                var field = type.GetField(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (field != null)
                {
                    field.SetValue(instance, value);
                }
            }
        }
    }
}