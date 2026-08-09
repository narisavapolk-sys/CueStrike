using UnityEngine;
using UnityEditor;
using CueStrike.TitleScene;

namespace CueStrike.TitleScene
{
    public static class TitleSceneAtmosphereEditor
    {
        [UnityEditor.MenuItem("Tools/CueStrike/Debug/Test TitleSceneAtmosphere")]
        public static void SelfTest()
        {
            bool pass = true;
            var atm = UnityEngine.Object.FindFirstObjectByType<TitleSceneAtmosphere>();
            if (atm == null) { Debug.LogError("❌ FAIL: TitleSceneAtmosphere missing in scene"); pass = false; }
            
            if (pass) Debug.Log("✅ ALL TESTS PASSED — Ready for human verify");
            else Debug.LogWarning("⚠️ TESTS FAILED — Fix before proceeding");
        }
    }
}