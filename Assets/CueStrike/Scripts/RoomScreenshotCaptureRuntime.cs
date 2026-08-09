using System.Collections;
using System.IO;
using UnityEngine;

namespace CueStrike
{
    public class RoomScreenshotCaptureRuntime : MonoBehaviour
    {
        void Start()
        {
            StartCoroutine(Capture());
        }

        IEnumerator Capture()
        {
            yield return new WaitForSeconds(3f);
            yield return null;

            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            string dir = Path.Combine(Directory.GetCurrentDirectory(), "RoomScreenshots");
            Directory.CreateDirectory(dir);
            string name = scene.name.Replace(" ", "_");
            string path = Path.Combine(dir, name + ".png");
            Debug.Log("[RoomShot] Capturing " + name);

            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(path, 2);
            File.WriteAllText(path + ".done", "ok");
            Debug.Log("[RoomShot] Saved " + path);
            yield return new WaitForSeconds(1f);

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }
    }
}
