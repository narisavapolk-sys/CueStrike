using UnityEngine;

namespace CueStrike.Gameplay
{
    /// <summary>
    /// On Awake, this script loads the BallMaterial from Resources and assigns it to all MeshRenderers
    /// on the PoolBalls model (named "PoolBalls_AAA").
    /// </summary>
    public class BallMaterialAssigner : MonoBehaviour
    {
        void Awake()
        {
            // Load the material from Resources
            Material ballMat = Resources.Load<Material>("BallMaterial");
            if (ballMat == null)
            {
                Debug.LogWarning("[BallMaterialAssigner] BallMaterial not found in Resources. Please run Tools → CueStrike → Create Ball Material.");
                return;
            }

            // Find the PoolBalls model in the scene (by name)
            GameObject poolBalls = GameObject.Find("PoolBalls_AAA");
            if (poolBalls == null)
            {
                Debug.LogWarning("[BallMaterialAssigner] PoolBalls_AAA GameObject not found in the scene.");
                return;
            }

            // Assign the material to all MeshRenderers under the PoolBalls hierarchy
            MeshRenderer[] renderers = poolBalls.GetComponentsInChildren<MeshRenderer>();
            foreach (var rend in renderers)
            {
                rend.sharedMaterial = ballMat;
            }

            Debug.Log("[BallMaterialAssigner] Ball material assigned to PoolBalls.");
        }
    }
}