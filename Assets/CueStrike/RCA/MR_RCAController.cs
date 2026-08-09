using UnityEngine;

public class MR_RCAController : MonoBehaviour
{
    public RCA rca;
    public Transform realTableAnchor; // world anchor for virtual table positioning

    public bool autoPlaceOnFloor = true;

    void Start()
    {
        // Placeholder: enable passthrough via SDK when available
        if (autoPlaceOnFloor)
        {
            PlaceTableOnRealFloor();
        }
    }

    void PlaceTableOnRealFloor()
    {
        // Platform-specific: here we simply position mrTable at y=0 if anchor empty
        if (realTableAnchor == null)
        {
            var t = new GameObject("MR_TableAnchor");
            realTableAnchor = t.transform;
            realTableAnchor.position = Vector3.zero; // the user should calibrate to floor
        }

        Debug.Log("MR_RCAController: Table placed at " + realTableAnchor.position);
    }
}
