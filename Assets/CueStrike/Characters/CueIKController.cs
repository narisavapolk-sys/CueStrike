using UnityEngine;

public class CueIKController : MonoBehaviour
{
    [Header("Target Cue")]
    public CueStrikeCue activeCue;

    [Header("Character Hand Transforms")]
    public Transform leftHandBridge;
    public Transform rightHandGrip;
    public Transform headLookTarget;

    [Header("Glove Visual Settings")]
    public GameObject leftGloveVisual;
    public GameObject rightGloveVisual;

    [Header("IK Position Offsets")]
    [Tooltip("Distance from cue tip to left hand bridge point (meters)")]
    public float bridgeDistance = 0.35f;
    [Tooltip("Distance from cue butt to right hand grip point (meters)")]
    public float gripOffsetFromButt = 0.15f;

    [Header("Stance Settings")]
    [Range(0f, 1f)]
    public float stanceBendingWeight = 0.85f;

    private void Start()
    {
        // 1. Auto-discover active cue stick in scene if not assigned
        if (activeCue == null)
        {
            activeCue = FindFirstObjectByType<CueStrikeCue>();
        }

        // 2. Procedurally find or create Left/Right Hand targets for Somchay if not assigned
        SetupProceduralHands();

        // 3. Load Glove status and show/hide glove visuals
        ApplyGloveSettings();
    }

    private void LateUpdate()
    {
        if (activeCue == null)
        {
            activeCue = FindFirstObjectByType<CueStrikeCue>();
            if (activeCue == null) return;
        }

        if (activeCue.profile == null) return;

        UpdateIKPostures();
    }

    private void SetupProceduralHands()
    {
        // If left/right hand transforms are not assigned, look for children or create them
        if (leftHandBridge == null)
        {
            var lh = transform.Find("LeftHand_IK");
            if (lh != null)
            {
                leftHandBridge = lh;
            }
            else
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                go.name = "LeftHand_IK";
                go.transform.SetParent(transform, false);
                go.transform.localScale = new Vector3(0.08f, 0.08f, 0.08f);
                var col = go.GetComponent<Collider>();
                if (col != null) Destroy(col);
                leftHandBridge = go.transform;

                // Simple glove visual cylinder child
                var visual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                visual.name = "GloveVisual";
                visual.transform.SetParent(go.transform, false);
                visual.transform.localScale = new Vector3(0.9f, 0.5f, 0.9f);
                visual.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                var vCol = visual.GetComponent<Collider>();
                if (vCol != null) Destroy(vCol);
                leftGloveVisual = visual;
            }
        }

        if (rightHandGrip == null)
        {
            var rh = transform.Find("RightHand_IK");
            if (rh != null)
            {
                rightHandGrip = rh;
            }
            else
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                go.name = "RightHand_IK";
                go.transform.SetParent(transform, false);
                go.transform.localScale = new Vector3(0.08f, 0.08f, 0.08f);
                var col = go.GetComponent<Collider>();
                if (col != null) Destroy(col);
                rightHandGrip = go.transform;

                var visual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                visual.name = "GloveVisual";
                visual.transform.SetParent(go.transform, false);
                visual.transform.localScale = new Vector3(0.9f, 0.5f, 0.9f);
                visual.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                var vCol = visual.GetComponent<Collider>();
                if (vCol != null) Destroy(vCol);
                rightGloveVisual = visual;
            }
        }

        // Apply materials to procedural hands
        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLit != null)
        {
            Material handSkinMat = new Material(urpLit);
            handSkinMat.name = "SomchayHandSkin";
            handSkinMat.SetColor("_BaseColor", new Color(0.85f, 0.65f, 0.52f, 1f)); // skin tone
            handSkinMat.SetFloat("_Smoothness", 0.3f);
            
            leftHandBridge.GetComponent<Renderer>().sharedMaterial = handSkinMat;
            rightHandGrip.GetComponent<Renderer>().sharedMaterial = handSkinMat;

            // Apply leather glove styling if visual exists
            Material leatherGloveMat = new Material(urpLit);
            leatherGloveMat.name = "SomchayLeatherGlove";
            leatherGloveMat.SetColor("_BaseColor", new Color(0.12f, 0.12f, 0.12f, 1f)); // sleek black leather
            leatherGloveMat.SetFloat("_Smoothness", 0.75f);
            leatherGloveMat.SetFloat("_Metallic", 0.1f);

            if (leftGloveVisual != null) leftGloveVisual.GetComponent<Renderer>().sharedMaterial = leatherGloveMat;
            if (rightGloveVisual != null) rightGloveVisual.GetComponent<Renderer>().sharedMaterial = leatherGloveMat;
        }
    }

    public void ApplyGloveSettings()
    {
        // 0 = OFF, 1 = ON
        bool useGlove = PlayerPrefs.GetInt("CueStrike_UseGlove", 0) == 1;
        
        if (leftGloveVisual != null)
        {
            leftGloveVisual.SetActive(useGlove);
        }
        if (rightGloveVisual != null)
        {
            rightGloveVisual.SetActive(useGlove);
        }
    }

    private void UpdateIKPostures()
    {
        float totalLength = activeCue.profile.length;
        
        // 1. Position Left Hand (Bridge) near the front of the cue stick
        // The cue stick forward vector is pointing to the cue tip (Z+ axis).
        // Since tip is at local 0,0,0, we move back along local Z axis by bridgeDistance.
        Vector3 bridgeLocalPos = new Vector3(0f, -0.015f, -bridgeDistance); // slightly below cue center
        Vector3 targetBridgeWorld = activeCue.transform.TransformPoint(bridgeLocalPos);
        
        leftHandBridge.position = Vector3.Lerp(leftHandBridge.position, targetBridgeWorld, Time.deltaTime * 15f);
        leftHandBridge.rotation = Quaternion.Slerp(leftHandBridge.rotation, activeCue.transform.rotation, Time.deltaTime * 15f);

        // 2. Position Right Hand (Grip) near the butt of the cue stick
        float gripDistFromTip = totalLength - gripOffsetFromButt;
        Vector3 gripLocalPos = new Vector3(0f, 0f, -gripDistFromTip);
        Vector3 targetGripWorld = activeCue.transform.TransformPoint(gripLocalPos);

        rightHandGrip.position = Vector3.Lerp(rightHandGrip.position, targetGripWorld, Time.deltaTime * 15f);
        rightHandGrip.rotation = Quaternion.Slerp(rightHandGrip.rotation, activeCue.transform.rotation, Time.deltaTime * 15f);

        // 3. Align body (Somchay) posture to look bent over the table looking down the cue stick
        // Align root body transform so that it faces the cue forward direction.
        Vector3 cueDir = activeCue.transform.forward;
        Vector3 flatCueDir = new Vector3(cueDir.x, 0f, cueDir.z).normalized;
        
        if (flatCueDir.sqrMagnitude > 0.001f)
        {
            Quaternion targetBodyRotation = Quaternion.LookRotation(flatCueDir, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetBodyRotation, Time.deltaTime * 8f);
        }

        // Adjust body position to follow the grip hand but slightly offset backwards to keep the stance balanced.
        Vector3 bodyAnchor = targetGripWorld - flatCueDir * 0.4f;
        // Keep body on the ground plane (y-level of player space)
        bodyAnchor.y = transform.parent != null ? transform.parent.position.y : 0f;
        
        transform.position = Vector3.Lerp(transform.position, bodyAnchor, Time.deltaTime * 8f);
    }
}
