using UnityEditor;
using UnityEngine;

namespace CueStrike.Editor
{
    public static class BallMaterialCreator
    {
        [MenuItem("Tools/CueStrike/Create Ball Material")]
        public static void CreateBallMaterial()
        {
            // Ensure the Resources folder exists
            string resourcesPath = "Assets/CueStrike/Resources";
            if (!AssetDatabase.IsValidFolder(resourcesPath))
            {
                AssetDatabase.CreateFolder("Assets/CueStrike", "Resources");
            }

            // Create a new material with URP/Lit shader
            Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
            if (urpLit == null)
            {
                Debug.LogError("URP Lit shader not found. Ensure the URP package is installed.");
                return;
            }

            Material ballMat = new Material(urpLit);
            ballMat.name = "BallMaterial";

            // Optionally set a default color
            ballMat.SetColor("_BaseColor", Color.white);

            // Save the material in Resources so it can be loaded at runtime
            string materialPath = $"{resourcesPath}/BallMaterial.mat";
            AssetDatabase.CreateAsset(ballMat, materialPath);
            AssetDatabase.SaveAssets();

            Debug.Log($"Ball material created at {materialPath}");
        }
    }
}