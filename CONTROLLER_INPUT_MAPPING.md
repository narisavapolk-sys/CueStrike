# CueStrike VR Physical Input System — Controller Input Mapping Guide

## Overview

CueStrike uses a **Hand-as-Cue** paradigm for physical VR pool. Instead of abstract button presses, the player physically pulls back their dominant hand (holding the virtual cue) and thrusts forward to strike the cue ball.

### Key Principles
- **Grip to hold the cue** (select action on XR Controller)
- **Pull back to charge** (like a real pool cue)
- **Thrust forward to shoot**
- **Off-hand grip + thumbstick for aim orbit** around cue ball
- **Thumbstick only for table orbit**
- **Thumbstick click to toggle stance** (Standing ↔ Crouching)
- **Y/B button for Undo** last shot (single-player only)
- **X/A button for Options/Pause menu**

---

## Hand Assignment

| Setting | Value |
|---------|-------|
| **Dominant Hand** (cue hand) | Right by default. Configurable via `CueStrikeSettingsManager.dominantHand` |
| **Off Hand** (non-dominant) | Opposite of dominant hand |

---

## Controller Mapping (Oculus / Meta Quest Touch Controllers)

| Button/Action | Dominant Hand (Cue Hand) | Off Hand | Function |
|---------------|--------------------------|----------|----------|
| **Grip** (GripBtn) | Hold → Aim/Charge/Shoot | Hold + Thumbstick = Aim Orbit | Cue hold (dom) / Orbit modifier (off) |
| **Trigger** | (Unused for shot — reserved) | (Reserved) | — |
| **Thumbstick X-axis** | (Reserved) | Orbit left/right | Table orbit (no grip) / Aim orbit (with grip) |
| **Thumbstick Y-axis** | (Reserved) | Crouch distance adjust | Only in Crouching stance |
| **Thumbstick Click** (L3/R3) | Stance Toggle | (Reserved) | Standing ↔ Crouching |
| **Primary Button** (A on right, X on left) | Options/Pause | (Reserved) | Toggle pause menu |
| **Secondary Button** (B on right, Y on left) | Undo Last Shot | (Reserved) | Restore ball positions (single-player only) |
| **Menu Button** (Oculus) | System menu | System menu | (Handled by platform) |

---

## Shot Mechanics

### State Machine

```
Idle → Aiming → Charged → Shooting → Resolving → Idle
```

| State | Trigger | Description |
|-------|---------|-------------|
| **Idle** | Grip released | Waiting for grip to start aiming |
| **Aiming** | Grip pressed | Hand position recorded as "cue rest position" |
| **Charged** | Pull-back ≥ `minPullBackDistance` (default 5cm) | Ready to thrust forward |
| **Shooting** | Forward thrust ≥ `minShotVelocity` | Shot data dispatched to CueStrikeShotManager |
| **Resolving** | Shot callback | Waiting for all balls to stop |

### Power Calculation

```
rawPower = pullBackDistance * thrustVelocity * powerMultiplier
normalizedPower = clamp(rawPower / maxShotPower, 0, 1)
```

### Cancellation
- Release grip during **Aiming** → Cancel (return to Idle)
- Release grip during **Charged** → Cancel (return to Idle)
- Hold grip in **Charged** for >10s → Auto-cancel

---

## Stance System

| Stance | Camera Height | Use Case |
|--------|---------------|----------|
| **Standing** | 1.7m (eye level) | Full table view, general play |
| **Crouching** | 0.8m (low) | Close-up aiming, trick shots |

- Toggle with **thumbstick click** (L3/R3)
- While crouching, **thumbstick Y-axis** adjusts distance from cue ball (0.3m–1.5m)
- Distance is persisted in `PlayerPrefs`

---

## Orbit System

| Mode | Activation | Pivot Point | Speed |
|------|-----------|-------------|-------|
| **Table Orbit** | Thumbstick X (no grip held) | Table center | 60°/s |
| **Aim Orbit** | Off-hand grip + Thumbstick X | Cue ball position | 90°/s |

---

## Haptic Feedback

| Event | Amplitude | Notes |
|-------|-----------|-------|
| Charged (pull-back threshold reached) | 0.3 | Low buzz |
| Shot executed (thrust detected) | 0.7 | Strong impulse |
| Shot resolved (balls stop) | 0.5 | Medium impulse |

---

## Undo System (`CueStrikeShotHistory`)

- Maximum last **10 shots** stored
- **Disabled in multiplayer** (auto-detected via Normcore presence)
- Stores full ball state snapshots (position, rotation, velocity, pocketed state)
- Undo restores all balls to pre-shot condition (including pocketed balls)
- Cleared on new frame / scene load

---

## VR Input Manager (`CueStrikeVRInputManager`)

Singleton coordinator that:
1. Auto-detects XR Origin and Controller Interactors
2. Reads dominant hand from `CueStrikeSettingsManager`
3. Wires `CueStrikePhysicalShotController`, `CueStrikeStanceController`, `CueStrikeAimOrbitController`, `CueStrikeShotHistory`
4. Dispatches haptic impulses to the correct hand controller

### Setup (Editor)

1. Ensure scene has **XROrigin** with **Left and Right Controller Interactors**
2. Run: **Tools → CueStrike → Setup → Wire VR Input System**
3. Assign `InputActionReference` assets in the created `VRInputMapping.asset`
4. Run: **Tools → CueStrike → Debug → Test VR Input System** to verify

### Manual Setup

If the auto-wire wizard is not used:
1. Create a `CueStrikeVRInputManager` GameObject with all required components
2. Assign the `VRInputMapping` ScriptableObject
3. Set the XR Origin reference
4. Ensure `CueStrikeVRInputManager` has `DefaultExecutionOrder(-50)`

---

## VR Input Mapping Asset Fields

| Field | Default | Description |
|-------|---------|-------------|
| `gripAction` | — | InputActionReference for grip button (dom hand) |
| `optionsButtonAction` | — | InputActionReference for primary button (X/A) |
| `undoButtonAction` | — | InputActionReference for secondary button (Y/B) |
| `offHandGripAction` | — | InputActionReference for grip (off hand) |
| `stanceToggleAction` | — | InputActionReference for thumbstick click |
| `orbitStickAction` | — | InputActionReference for thumbstick 2D axis |
| `stanceDistanceStickAction` | — | InputActionReference for secondary thumbstick Y-axis |
| `minPullBackDistance` | 0.05m | Minimum pull-back to register charge |
| `minShotVelocity` | 0.3 m/s | Minimum forward thrust velocity to execute shot |
| `maxShotPower` | 10 | Maximum shot power (clamp) |
| `powerMultiplier` | 1.5 | Power multiplier coefficient |
| `hapticChargeAmplitude` | 0.3 | Haptic amplitude when charge threshold reached |
| `hapticShotAmplitude` | 0.7 | Haptic amplitude on shot execution |
| `hapticImpactAmplitude` | 0.5 | Haptic amplitude on ball impact/resolve |
| `crouchDistanceMin` | 0.3m | Minimum crouch distance from cue ball |
| `crouchDistanceMax` | 1.5m | Maximum crouch distance from cue ball |
| `crouchDistanceDefault` | 0.8m | Default crouch distance |
| `aimOrbitSpeed` | 90°/s | Rotation speed for Aim Orbit |
| `tableOrbitSpeed` | 60°/s | Rotation speed for Table Orbit |

---

## Integration Points

| Existing Class | Integration |
|---------------|-------------|
| `CueStrikeSettingsManager` | Reads `dominantHand` → applies to `VRInputManager.DominantHand` |
| `CueStrikePauseMenu` | Subscribes to `VRInputManager.OnOptionsPressed` → `TogglePause()` |
| `CueStrikeVRControlPanel` | Adds "Stance: Toggle" button → calls `StanceController.SetStance()` |
| `CueStrikePhysicalShotController` | Fires `OnShotExecuted` → expects `CueStrikeShotManager` subscriber |
| `CueStrikeAIController` | Can call `PhysicalShotController.SimulateShot()` for AI play |
| `GhostReplayRecorder` | Can use `PhysicalShotData` for replay capture |

---

## Troubleshooting

| Symptom | Likely Cause | Fix |
|---------|-------------|-----|
| "Instance is null" | VRInputManager not in scene | Run Setup wizard or create manually |
| Grip doesn't register | `gripAction` not assigned | Assign in VRInputMapping asset |
| Can't toggle stance | `stanceToggleAction` not assigned | Assign thumbstick click action reference |
| Undo does nothing | In multiplayer mode (`_isMultiplayer = true`) | Disconnect from Normcore or switch to single-player |
| Shot executes at wrong angle | `dominantHandTransform` not correctly assigned | Run auto-wire or manually assign hand |
| No haptic feedback | Controller haptic not supported or `dominantHandInteractor.xrController` is null | Verify XR Controller is configured |

---

## Files

| File | Path | Purpose |
|------|------|---------|
| `CueStrikeVRInputMapping.cs` | `Scripts/VR/Input/` | ScriptableObject with all input action references |
| `CueStrikePhysicalShotController.cs` | `Scripts/VR/Input/` | State machine for hand-as-cue shot execution |
| `CueStrikeStanceController.cs` | `Scripts/VR/Input/` | Standing/Crouching stance manager |
| `CueStrikeAimOrbitController.cs` | `Scripts/VR/Input/` | Camera orbit around cue ball and table |
| `CueStrikeShotHistory.cs` | `Scripts/Core/` | Undo system with ball state snapshots |
| `CueStrikeVRInputManager.cs` | `Scripts/VR/Input/` | Singleton coordinator |
| `CueStrikeVRInputSetup.cs` | `Editor/` | Editor wizard for automated setup |
| `VRInputSelfTest.cs` | `Editor/` | Self-test suite for verification |

---

*Last updated: July 2026*  
*CueStrike VR Physical Input System v1.0*