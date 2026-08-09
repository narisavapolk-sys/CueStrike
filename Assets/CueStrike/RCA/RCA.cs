using UnityEngine;

public class RCA : MonoBehaviour
{
    [Header("Real Cue Tracking")]
    public Transform cueRoot;          // Butt
    public Transform cueTip;           // Tip
    public Transform controller;       // XR controller

    [Header("Calibration")]
    public Vector3 tipOffset;
    public Vector3 buttOffset;
    public bool calibrated = false;

    [Header("Impact Settings")]
    public float impactThreshold = 0.02f;
    public float maxForce = 20f;

    private Vector3 lastControllerPos;
    private Vector3 controllerVelocity;
    [Header("Smoothing")]
    public float positionSmooth = 0.15f;
    public float rotationSmooth = 0.15f;

    public delegate void ImpactHandler(Rigidbody ballRb, Vector3 contactPoint, Vector3 direction, float baseForce);
    public event ImpactHandler OnImpact;

    void Update()
    {
        if (!calibrated || controller == null) return;

        // smooth tracking
        var targetRootPos = controller.TransformPoint(buttOffset);
        var targetTipPos = controller.TransformPoint(tipOffset);
        cueRoot.position = Vector3.Lerp(cueRoot.position, targetRootPos, 1f - Mathf.Exp(-positionSmooth * Time.deltaTime * 60f));
        cueTip.position = Vector3.Lerp(cueTip.position, targetTipPos, 1f - Mathf.Exp(-positionSmooth * Time.deltaTime * 60f));

        cueRoot.rotation = Quaternion.Slerp(cueRoot.rotation, controller.rotation, 1f - Mathf.Exp(-rotationSmooth * Time.deltaTime * 60f));
        cueTip.rotation = Quaternion.Slerp(cueTip.rotation, controller.rotation, 1f - Mathf.Exp(-rotationSmooth * Time.deltaTime * 60f));

        controllerVelocity = (controller.position - lastControllerPos) / Time.deltaTime;
        lastControllerPos = controller.position;

        DetectBallImpact();
    }

    void DetectBallImpact()
    {
        RaycastHit hit;
        if (Physics.Raycast(cueTip.position, cueTip.forward, out hit, impactThreshold))
        {
            if (hit.collider.CompareTag("Ball"))
            {
                Rigidbody rb = hit.collider.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    float force = Mathf.Clamp(controllerVelocity.magnitude * 5f, 0f, maxForce);
                    var contactPoint = hit.point;
                    var dir = cueTip.forward.normalized;
                    OnImpact?.Invoke(rb, contactPoint, dir, force);
                }
            }
        }
    }

    public void Calibrate(Vector3 tip, Vector3 mid, Vector3 butt)
    {
        tipOffset = controller.InverseTransformPoint(tip);
        buttOffset = controller.InverseTransformPoint(butt);
        calibrated = true;
    }

    public void SetController(Transform newController)
    {
        controller = newController;
    }
}
