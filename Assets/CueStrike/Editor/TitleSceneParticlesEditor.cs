using UnityEngine;
using UnityEditor;
using CueStrike.TitleScene;

namespace CueStrike.TitleScene
{
    public static class TitleSceneParticlesEditor
    {
        [UnityEditor.MenuItem("Tools/CueStrike/Debug/Test TitleSceneParticles")]
        public static void SelfTest()
        {
            bool pass = true;
            var tp = UnityEngine.Object.FindFirstObjectByType<TitleSceneParticles>();
            if (tp == null) { Debug.LogError("❌ FAIL: TitleSceneParticles missing in scene"); pass = false; }
            
            if (pass) Debug.Log("✅ ALL TESTS PASSED — Ready for human verify");
            else Debug.LogWarning("⚠️ TESTS FAILED — Fix before proceeding");
        }
    }
}