//
// CueStrikeLightingProfileSetup — Bakes AAA Cinematic Lighting Volume Profile
// Created by Nari for P'Mong | 2026-07-19
// Phase 1: Lighting/Shadow AAA — creates URP Volume Profile with cinematic grading
//
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEditor;

namespace CueStrike.Editor
{
    /// <summary>
    /// One-click baker that creates a cinematic Volume Profile asset with AAA overrides.
    /// Run via menu: Tools/CueStrike/Lighting/Bake AAA Lighting Profile
    /// </summary>
    public static class CueStrikeLightingProfileSetup
    {
        [MenuItem("Tools/CueStrike/Lighting/Bake AAA Lighting Profile", false, 1)]
        public static void BakeProfile()
        {
            // Output path
            const string folder = "Assets/CueStrike/Settings/Lighting";
            const string assetName = "CueStrike_CinematicProfile.asset";
            var fullPath = $"{folder}/{assetName}";

            if (!AssetDatabase.IsValidFolder(folder))
            {
                AssetDatabase.CreateFolder("Assets/CueStrike/Settings", "Lighting");
            }

            // Create Volume Profile
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            profile.name = "CueStrike_CinematicProfile";

            // ---- Tonemapping (ACES Filmic) ----
            var tonemapping = profile.Add<Tonemapping>(true);
            tonemapping.mode.value = TonemappingMode.ACES;

            // ---- Bloom (Warm HDR glow) ----
            var bloom = profile.Add<Bloom>(true);
            bloom.intensity.overrideState = true;
            bloom.intensity.value = 0.18f;
            bloom.threshold.overrideState = true;
            bloom.threshold.value = 0.9f;
            bloom.scatter.overrideState = true;
            bloom.scatter.value = 0.7f;
            bloom.tint.overrideState = true;
            bloom.tint.value = new Color(1f, 0.92f, 0.78f); // Warm golden

            // ---- Color Adjustments (Teal-Orange cinematic look) ----
            var colorAdj = profile.Add<ColorAdjustments>(true);
            colorAdj.postExposure.overrideState = true;
            colorAdj.postExposure.value = 0.35f;
            colorAdj.contrast.overrideState = true;
            colorAdj.contrast.value = 12f;
            colorAdj.colorFilter.overrideState = true; // URP 17+ uses colorFilter (was 'filter')
            colorAdj.colorFilter.value = new Color(1f, 0.95f, 0.85f); // Warm highlight tint
            colorAdj.hueShift.overrideState = true;
            colorAdj.hueShift.value = -3f;
            colorAdj.saturation.overrideState = true;
            colorAdj.saturation.value = -8f; // Slightly desaturated for cinematic feel

            // ---- White Balance ----
            var wb = profile.Add<WhiteBalance>(true);
            wb.temperature.overrideState = true;
            wb.temperature.value = 25f; // Slightly warm
            wb.tint.overrideState = true;
            wb.tint.value = -3f;

            // ---- Shadows / Midtones / Highlights (Split Toning) ----
            var smh = profile.Add<LiftGammaGain>(true);
            smh.lift.overrideState = true;
            smh.lift.value = new Color(0.02f, 0.01f, 0.005f, 0f); // Teal shadows
            smh.gamma.overrideState = true;
            smh.gamma.value = new Color(1.02f, 0.98f, 0.92f, 0f); // Warm mids
            smh.gain.overrideState = true;
            smh.gain.value = new Color(1.05f, 1.0f, 0.95f, 0f); // Warm highlights

            // ---- Channel Mixer (Teal-Orange grade) ----
            var mixer = profile.Add<ChannelMixer>(true);
            mixer.redOutRedIn.overrideState = true;
            mixer.redOutRedIn.value = 100f;
            mixer.redOutGreenIn.overrideState = true;
            mixer.redOutGreenIn.value = 0f;
            mixer.redOutBlueIn.overrideState = true;
            mixer.redOutBlueIn.value = 0f;
            mixer.greenOutRedIn.overrideState = true;
            mixer.greenOutRedIn.value = 0f;
            mixer.greenOutGreenIn.overrideState = true;
            mixer.greenOutGreenIn.value = 96f;
            mixer.greenOutBlueIn.overrideState = true;
            mixer.greenOutBlueIn.value = 4f;
            mixer.blueOutRedIn.overrideState = true;
            mixer.blueOutRedIn.value = 6f;
            mixer.blueOutGreenIn.overrideState = true;
            mixer.blueOutGreenIn.value = 0f;
            mixer.blueOutBlueIn.overrideState = true;
            mixer.blueOutBlueIn.value = 94f;

            // ---- Vignette (Subtle cinematic frame) ----
            var vignette = profile.Add<Vignette>(true);
            vignette.intensity.overrideState = true;
            vignette.intensity.value = 0.18f;
            vignette.smoothness.overrideState = true;
            vignette.smoothness.value = 0.45f;
            // URP: 'rounded' is a BoolParameter (true = rounded, false = square)
            vignette.rounded.overrideState = true;
            vignette.rounded.value = true; // Fully rounded corners (bool in URP)
            vignette.color.overrideState = true;
            vignette.color.value = new Color(0.02f, 0.01f, 0.005f);

            // Save asset
            AssetDatabase.CreateAsset(profile, fullPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            UnityEngine.Debug.Log($"[CueStrike] AAA Lighting Profile baked -> {fullPath}");
            EditorUtility.DisplayDialog(
                "CueStrike Lighting AAA",
                "Cinematic Volume Profile created successfully.\nAdd a Volume (Global) component to your scene and assign this profile to see the AAA look instantly.",
                "OK");
        }
    }
}