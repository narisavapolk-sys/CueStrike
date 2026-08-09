using UnityEngine;

// Placeholder for Quest camera / marker based RCA
// This class is intentionally minimal: it exposes the API surface needed for later CV/SDK integration.
public class QuestCameraRCA : MonoBehaviour
{
    public Transform cueRoot;
    public Transform cueTip;

    void Update()
    {
        // TODO: integrate Quest camera/marker tracking or hand tracking SDK
        // For now, keep cue at origin
        if (cueRoot != null) cueRoot.localPosition = Vector3.zero;
        if (cueTip != null) cueTip.localPosition = Vector3.forward * 1f;
    }

    // Example API: inject pose from external tracker
    public void ApplyExternalPose(Vector3 position, Quaternion rotation)
    {
        if (cueRoot != null)
        {
            cueRoot.position = position;
            cueRoot.rotation = rotation;
        }
    }
}
