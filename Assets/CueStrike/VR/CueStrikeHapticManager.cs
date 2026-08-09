using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

namespace CueStrike.VR
{
    /// <summary>
    /// Coordinates OpenXR haptic feedback (rumble impulses) for VR hand controllers.
    /// Fully cross-platform compatible with Quest 2/3, Pico, and Index.
    /// </summary>
    public static class CueStrikeHapticManager
    {
        /// <summary>
        /// Sends a haptic impulse to the specified XR controller node.
        /// </summary>
        public static void SendHapticImpulse(XRNode controllerNode, float amplitude, float durationSeconds)
        {
            var devices = new List<InputDevice>();
            InputDevices.GetDevicesAtXRNode(controllerNode, devices);

            foreach (var device in devices)
            {
                if (device.TryGetHapticCapabilities(out var capabilities) && capabilities.supportsImpulse)
                {
                    // Clamp inputs to safe standard values
                    float amp = Mathf.Clamp01(amplitude);
                    float dur = Mathf.Clamp(durationSeconds, 0.01f, 1.0f);
                    device.SendHapticImpulse(0, amp, dur);
                }
            }
        }

        /// <summary>
        /// Sends a haptic pulse to both left and right controllers.
        /// </summary>
        public static void SendHapticToAll(float amplitude, float durationSeconds)
        {
            SendHapticImpulse(XRNode.LeftHand, amplitude, durationSeconds);
            SendHapticImpulse(XRNode.RightHand, amplitude, durationSeconds);
        }
    }
}
