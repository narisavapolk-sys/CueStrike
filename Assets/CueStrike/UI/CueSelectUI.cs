using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CueSelectUI : MonoBehaviour
{
    public Dropdown cueDropdown;
    public List<CueProfile> availableCues = new List<CueProfile>();

    public CueProfile selectedCue;

    public static System.Action<CueProfile> OnCueChanged;

    void Start()
    {
        if (cueDropdown == null) return;
        RuntimePopulate();
        cueDropdown.onValueChanged.AddListener(OnCueSelected);
    }

    void Awake()
    {
        // Editor convenience: auto-scan CueProfile assets so the dropdown is populated when opening scenes in Editor
        #if UNITY_EDITOR
        if ((availableCues == null || availableCues.Count == 0))
        {
            var guids = UnityEditor.AssetDatabase.FindAssets("t:CueProfile", new[] { "Assets/CueStrike/Cues" });
            availableCues = new List<CueProfile>();
            foreach (var g in guids)
            {
                var path = UnityEditor.AssetDatabase.GUIDToAssetPath(g);
                var cp = UnityEditor.AssetDatabase.LoadAssetAtPath<CueProfile>(path);
                if (cp != null) availableCues.Add(cp);
            }
        }
        #endif
    }

    void PopulateDropdown()
    {
        cueDropdown.ClearOptions();
        var options = new List<string>();
        foreach (var c in availableCues)
        {
            options.Add(c != null ? (string.IsNullOrEmpty(c.cueName) ? "Cue" : c.cueName) : "EmptyCue");
        }
        cueDropdown.AddOptions(options);
        if (options.Count > 0) { cueDropdown.value = 0; selectedCue = availableCues[0]; }
    }

    void OnCueSelected(int idx)
    {
        if (idx >= 0 && idx < availableCues.Count) selectedCue = availableCues[idx];
        // Broadcast selection event if needed
        Debug.Log("CueSelectUI: selected cue " + (selectedCue != null ? selectedCue.cueName : "null"));
        OnCueChanged?.Invoke(selectedCue);
    }

    void RuntimePopulate()
    {
        // Try runtime Resources first
        if (availableCues == null) availableCues = new List<CueProfile>();
        availableCues.Clear();
        var loaded = Resources.LoadAll<CueProfile>("CueStrike/Cues");
        if (loaded != null && loaded.Length > 0)
        {
            availableCues.AddRange(loaded);
        }
#if UNITY_EDITOR
        // In the Editor, prefer AssetDatabase to pick up assets outside Resources
        else
        {
            var guids = UnityEditor.AssetDatabase.FindAssets("t:CueProfile", new[] { "Assets/CueStrike/Cues" });
            foreach (var g in guids)
            {
                var path = UnityEditor.AssetDatabase.GUIDToAssetPath(g);
                var cp = UnityEditor.AssetDatabase.LoadAssetAtPath<CueProfile>(path);
                if (cp != null) availableCues.Add(cp);
            }
        }
#endif
        PopulateDropdown();
        // invoke initial selection event
        if (selectedCue != null) OnCueChanged?.Invoke(selectedCue);
    }
}
