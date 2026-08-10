using UnityEngine;

namespace CueStrike.VR
{
    /// <summary>
    /// Boot scene transition. Loads the next scene (default: Title_NoksGrandHall)
    /// through the gold-standard VR loading screen once VRStartup has applied its
    /// boot-time Quest optimizations (VRStartup runs first at DefaultExecutionOrder -1000).
    /// </summary>
    public class BootSceneLoader : MonoBehaviour
    {
        [Header("Boot Flow")]
        [Tooltip("Scene loaded after boot. Defaults to the Title scene (main menu).")]
        public string nextSceneName = "Title_NoksGrandHall";

        private void Start()
        {
            if (string.IsNullOrEmpty(nextSceneName))
            {
                Debug.LogWarning("[BootSceneLoader] nextSceneName is empty - boot scene will not transition.");
                return;
            }

            CueStrikeLoadingScreen.LoadScene(nextSceneName);
            Debug.Log($"[BootSceneLoader] Loading next scene: {nextSceneName}");
        }
    }
}
