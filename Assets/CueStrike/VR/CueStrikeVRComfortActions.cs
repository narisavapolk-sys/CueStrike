using UnityEngine;
using UnityEngine.XR;
using System.Collections.Generic;

namespace CueStrike.VR
{
    /// <summary>
    /// Implements advanced VR Comfort Actions:
    /// 1. Smooth rotation (Smooth Turn) using the right analog joystick.
    /// 2. Auto-Stance Transition (One-Click Auto-Lean to table height, and Auto-Stand on next click) via Button A.
    /// 3. Dynamic Rest Stick (Long Extension Stick) toggled via Left Hand Button X.
    /// 4. Floating HUD Canvas toggled via Left Hand Oculus Menu Button.
    /// </summary>
    public class CueStrikeVRComfortActions : MonoBehaviour
    {
        [Header("Smooth Turn Settings")]
        public float turnSpeed = 45f; // Degrees per second
        public XRNode turnControllerNode = XRNode.RightHand;

        [Header("Auto Stance Settings")]
        public Transform cameraOffsetTransform; // Typically the Camera Offset in XR Origin
        public float standingHeight = 1.6f;
        public float leaningHeight = 0.88f; // Level with standard table play surface
        public float transitionSpeed = 4f;
        public XRNode stanceControllerNode = XRNode.RightHand;

        [Header("Oculus Menu HUD Trigger")]
        public XRNode menuControllerNode = XRNode.LeftHand;

        [Header("Rest Stick Settings")]
        public XRNode restControllerNode = XRNode.LeftHand; // Button X
        [Tooltip("Optional prefab for Rest Stick. If null, a procedural rest stick is generated.")]
        public GameObject restStickPrefab;

        private bool _isLeaning = false;
        private float _targetHeight;
        private bool _aButtonWasPressed = false;
        private bool _menuButtonWasPressed = false;
        private bool _xButtonWasPressed = false;

        private GameObject _activeRestStick;
        private Transform _shaftTransform;

        private void Start()
        {
            if (cameraOffsetTransform == null)
            {
                var offset = GameObject.Find("Camera Offset") ?? GameObject.Find("CameraOffset");
                if (offset != null) cameraOffsetTransform = offset.transform;
                else cameraOffsetTransform = Camera.main != null ? Camera.main.transform.parent : transform;
            }

            _targetHeight = standingHeight;
            if (cameraOffsetTransform != null)
            {
                cameraOffsetTransform.localPosition = new Vector3(
                    cameraOffsetTransform.localPosition.x,
                    standingHeight,
                    cameraOffsetTransform.localPosition.z
                );
            }
        }

        private void Update()
        {
            HandleSmoothTurn();
            HandleAutoStance();
            HandleMenuToggle();
            HandleRestStick();
        }

        /// <summary>
        /// Reads right joystick horizontal axis and rotates the VR rig smoothly.
        /// </summary>
        private void HandleSmoothTurn()
        {
            var device = InputDevices.GetDeviceAtXRNode(turnControllerNode);
            if (device.isValid)
            {
                if (device.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 axisInput))
                {
                    if (Mathf.Abs(axisInput.x) > 0.1f)
                    {
                        transform.Rotate(Vector3.up * axisInput.x * turnSpeed * Time.deltaTime);
                    }
                }
            }
        }

        /// <summary>
        /// Reads Button A press to smoothly transition between standing and leaning eye levels.
        /// </summary>
        private void HandleAutoStance()
        {
            var device = InputDevices.GetDeviceAtXRNode(stanceControllerNode);
            if (device.isValid)
            {
                if (device.TryGetFeatureValue(CommonUsages.primaryButton, out bool aButtonPressed))
                {
                    if (aButtonPressed && !_aButtonWasPressed)
                    {
                        _isLeaning = !_isLeaning;
                        _targetHeight = _isLeaning ? leaningHeight : standingHeight;
                        Debug.Log($"[CueStrike VR] Toggled Stance. Leaning: {_isLeaning}, Target Height: {_targetHeight}m");
                        
                        CueStrikeHapticManager.SendHapticImpulse(stanceControllerNode, 0.3f, 0.05f);
                    }
                    _aButtonWasPressed = aButtonPressed;
                }
            }

            if (cameraOffsetTransform != null)
            {
                float currentY = cameraOffsetTransform.localPosition.y;
                float newY = Mathf.MoveTowards(currentY, _targetHeight, transitionSpeed * Time.deltaTime);
                cameraOffsetTransform.localPosition = new Vector3(
                    cameraOffsetTransform.localPosition.x,
                    newY,
                    cameraOffsetTransform.localPosition.z
                );
            }
        }

        /// <summary>
        /// Reads Left Menu button to toggle the floating HUD menu.
        /// </summary>
        private void HandleMenuToggle()
        {
            var device = InputDevices.GetDeviceAtXRNode(menuControllerNode);
            if (device.isValid)
            {
                if (device.TryGetFeatureValue(CommonUsages.menuButton, out bool menuButtonPressed))
                {
                    if (menuButtonPressed && !_menuButtonWasPressed)
                    {
                        var hud = FindFirstObjectByType<CueStrikeHUDController>();
                        if (hud != null)
                        {
                            var canvas = hud.GetComponent<Canvas>();
                            if (canvas != null)
                            {
                                canvas.enabled = !canvas.enabled;
                                Debug.Log($"[CueStrike VR] Menu Button: Toggled HUD Canvas state to: {canvas.enabled}");
                            }
                        }
                        CueStrikeHapticManager.SendHapticImpulse(menuControllerNode, 0.4f, 0.08f);
                    }
                    _menuButtonWasPressed = menuButtonPressed;
                }
            }
        }

        /// <summary>
        /// Reads Button X to toggle the dynamic Rest Stick (Long Extension Stick) pointing at the cue ball.
        /// </summary>
        private void HandleRestStick()
        {
            var device = InputDevices.GetDeviceAtXRNode(restControllerNode);
            if (device.isValid)
            {
                if (device.TryGetFeatureValue(CommonUsages.primaryButton, out bool xButtonPressed))
                {
                    if (xButtonPressed && !_xButtonWasPressed)
                    {
                        ToggleRestStick();
                    }
                    _xButtonWasPressed = xButtonPressed;
                }
            }

            // Update active Rest Stick scale and orientation pointing at cue ball
            if (_activeRestStick != null)
            {
                Vector3 handPos = GetControllerPosition(restControllerNode);
                GameObject cueBall = GameObject.Find("CueBall");
                Vector3 targetPos = cueBall != null ? cueBall.transform.position : (handPos + transform.forward * 1.5f);

                _activeRestStick.transform.position = handPos;
                _activeRestStick.transform.LookAt(targetPos);

                // Dynamically scale shaft length to bridge the distance
                float distance = Vector3.Distance(handPos, targetPos);
                if (_shaftTransform != null)
                {
                    // Scale along Z axis (shaft length)
                    _shaftTransform.localScale = new Vector3(0.015f, 0.015f, distance);
                    // Position shaft centered between hand and target
                    _shaftTransform.localPosition = new Vector3(0f, 0f, distance * 0.5f);
                }
            }
        }

        private void ToggleRestStick()
        {
            if (_activeRestStick != null)
            {
                Destroy(_activeRestStick);
                _activeRestStick = null;
                _shaftTransform = null;
                Debug.Log("[CueStrike VR] Rest Stick: Deactivated.");
                CueStrikeHapticManager.SendHapticImpulse(restControllerNode, 0.4f, 0.05f);
            }
            else
            {
                Debug.Log("[CueStrike VR] Rest Stick: Activated.");
                CueStrikeHapticManager.SendHapticImpulse(restControllerNode, 0.5f, 0.1f);

                if (restStickPrefab != null)
                {
                    _activeRestStick = Instantiate(restStickPrefab);
                }
                else
                {
                    // Procedural Rest Stick (AAA look)
                    _activeRestStick = new GameObject("Procedural_RestStick");

                    // 1. Wood shaft cylinder
                    GameObject shaft = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    shaft.name = "Shaft";
                    shaft.transform.SetParent(_activeRestStick.transform, false);
                    var shaftRend = shaft.GetComponent<Renderer>();
                    if (shaftRend != null)
                    {
                        shaftRend.material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                        shaftRend.material.color = new Color(0.6f, 0.45f, 0.3f); // Wood color
                        shaftRend.material.SetFloat("_Smoothness", 0.5f);
                    }
                    _shaftTransform = shaft.transform;

                    // 2. Brass X-shaped rest head at the tip
                    GameObject head = new GameObject("RestHead");
                    head.transform.SetParent(_activeRestStick.transform, false);

                    // Cross bar 1
                    var bar1 = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    bar1.transform.SetParent(head.transform, false);
                    bar1.transform.localScale = new Vector3(0.01f, 0.08f, 0.01f);
                    bar1.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
                    
                    // Cross bar 2
                    var bar2 = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    bar2.transform.SetParent(head.transform, false);
                    bar2.transform.localScale = new Vector3(0.01f, 0.08f, 0.01f);
                    bar2.transform.localRotation = Quaternion.Euler(0f, 0f, -45f);

                    // Color brass/gold
                    var headMaterials = head.GetComponentsInChildren<Renderer>();
                    foreach (var rend in headMaterials)
                    {
                        rend.material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                        rend.material.color = new Color(0.85f, 0.7f, 0.2f); // Brass Gold
                        rend.material.SetFloat("_Metallic", 0.8f);
                        rend.material.SetFloat("_Smoothness", 0.7f);
                    }

                    // Position head at the end of the shaft (will be dynamically updated based on distance)
                    head.transform.localPosition = new Vector3(0f, 0f, 1f); // default offset
                }
            }
        }

        private Vector3 GetControllerPosition(XRNode node)
        {
            var device = InputDevices.GetDeviceAtXRNode(node);
            if (device.isValid && device.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 position))
            {
                return transform.TransformPoint(position);
            }
            return transform.position + (node == XRNode.LeftHand ? -transform.right : transform.right) * 0.3f;
        }
    }
}