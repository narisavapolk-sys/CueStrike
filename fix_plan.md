# CueStrike Compilation Error Fix Plan

## Files with Errors:

### 1. ImportBlenderAssets.cs (Line 266)
**Error**: `CS0246: The type or namespace name 'List'`
**Fix**: Add `using System.Collections.Generic;` at top

### 2. RoomSetupAAA.cs (Multiple errors)
- **Line 324**: `CS0619: 'ModelImporter.importMaterials' is obsolete`
  - Fix: Remove or use new API
- **Line 324**: `CS0200: Property or indexer 'ModelImporter.importMaterials' cannot be assigned`
  - Fix: Use `importer.importMaterials` differently or remove
- **Line 479**: `CS0619: 'ReflectionProbe.type' is obsolete`
  - Fix: Use `probe.mode = ReflectionProbeMode.Box;`
- **Line 479**: `CS0117: 'ReflectionProbeType' does not contain a definition`
  - Fix: Use `ReflectionProbeMode` instead
- **Line 523**: `CS0103: The name 'EditorSceneManager' does not exist`
  - Fix: Add `using UnityEditor.SceneManagement;`
- **Line 523**: `CS0103: The name 'NewSceneSetup' does not exist`
  - Fix: Update to Unity 6 API
- **Line 579**: `CS0103: The name 'CollectObjects' does not exist`
  - Fix: Use `CollectObjects.Children` enum properly

### 3. CueStrikeWBPSRuleset.cs (Encoding corruption)
**Error**: Corrupted Unicode characters () in Debug.Log strings
**Fix**: Replace corrupted characters with proper Thai/English text

## AAA Room Creation Task:
After fixing compilation errors, run the AAA World Tour setup:
1. Tools → CueStrike → AAA World Tour → Setup All Rooms
2. Verify 8 rooms created: ZenDojo, Cyberpunk, SpaceNebula, Industrial, WarpFantasy, Luxury_DAY, Luxury_NIGHT, Arena_Core

## Pending Tasks from ROADMAP.md:
- Phase 1: XR Packages, Normcore, Audio Assets, 3D Models
- Phase 2: Multiplayer via Normcore
- Phase 3: Playability features
- Phase 4: AAA Polish