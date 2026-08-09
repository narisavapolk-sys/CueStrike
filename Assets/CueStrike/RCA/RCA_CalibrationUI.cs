using UnityEngine;

public class RCA_CalibrationUI : MonoBehaviour
{
    public RCA rca;
    public Transform tipMarker;
    public Transform midMarker;
    public Transform buttMarker;

    public void CalibrateCue()
    {
        if (rca == null) return;
        rca.Calibrate(tipMarker.position, midMarker.position, buttMarker.position);
        Debug.Log("RCA calibrated");
    }
}
