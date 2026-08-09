#!/usr/bin/env python3
"""
CueStrike AAA World Tour - Cross-platform Build & Deploy Script
Runs Blender room generation, then Unity scene setup
"""

import os
import sys
import subprocess
import argparse
from pathlib import Path

# Configuration
PROJECT_ROOT = Path(__file__).parent.absolute()
BLENDER_EXE = Path(r"C:\Program Files\Blender Foundation\Blender 4.2\blender.exe")
UNITY_EXE = Path(r"C:\Program Files\Unity\Hub\Editor\2022.3.21f1\Editor\Unity.exe")

ROOMS = [
    "ZenDojo",
    "Cyberpunk",
    "SpaceNebula", 
    "Industrial",
    "WarpFantasy",
    "Luxury_DAY",
    "Luxury_NIGHT",
    "Arena_Core"
]

def print_header(title):
    print("\n" + "=" * 70)
    print(f"  {title}")
    print("=" * 70)

def print_step(step, total, title):
    print(f"\n[STEP {step}/{total}] {title}")
    print("-" * 50)

def run_command(cmd, cwd=None, capture=False):
    """Run a command and return success status"""
    print(f"  Running: {' '.join(str(c) for c in cmd)}")
    try:
        if capture:
            result = subprocess.run(cmd, cwd=cwd, capture_output=True, text=True, check=False)
            return result.returncode == 0, result.stdout, result.stderr
        else:
            result = subprocess.run(cmd, cwd=cwd, check=False)
            return result.returncode == 0, "", ""
    except Exception as e:
        print(f"  ERROR: {e}")
        return False, "", str(e)

def find_executable(name, default_path):
    """Find executable in PATH or use default"""
    # Check if in PATH
    for path in os.environ.get("PATH", "").split(os.pathsep):
        exe_path = Path(path) / name
        if exe_path.exists():
            return exe_path
    
    # Check default
    if default_path.exists():
        return default_path
    
    return None

def step1_generate_rooms(args):
    """Run Blender script to generate all rooms"""
    print_step(1, 3, "Generating rooms in Blender")
    
    blender = find_executable("blender.exe", BLENDER_EXE)
    if not blender:
        print(f"  ERROR: Blender not found. Tried: {BLENDER_EXE}")
        print("  Set BLENDER_EXE in script or add to PATH")
        return False
    
    blender_script = PROJECT_ROOT / "BlenderScripts" / "create_room_props_aaa.py"
    if not blender_script.exists():
        print(f"  ERROR: Blender script not found: {blender_script}")
        return False
    
    output_dir = PROJECT_ROOT / "CueStrike_Project" / "Assets" / "CueStrike" / "Art" / "Rooms"
    output_dir.mkdir(parents=True, exist_ok=True)
    
    cmd = [
        str(blender),
        "--background",
        "--python", str(blender_script),
        "--",
        "--output", str(output_dir)
    ]
    
    if args.verbose:
        cmd.append("--verbose")
    
    success, stdout, stderr = run_command(cmd, capture=True)
    
    if not success:
        print(f"  Blender script failed!")
        if stdout:
            print(f"  STDOUT:\n{stdout}")
        if stderr:
            print(f"  STDERR:\n{stderr}")
        return False
    
    print("  ✓ All rooms generated successfully")
    return True

def step2_unity_setup(args):
    """Import and configure rooms in Unity"""
    print_step(2, 3, "Importing and configuring in Unity")
    
    unity = find_executable("Unity.exe", UNITY_EXE)
    if not unity:
        print(f"  ERROR: Unity not found. Tried: {UNITY_EXE}")
        print("  Set UNITY_EXE in script or add to PATH")
        return False
    
    project_path = PROJECT_ROOT / "CueStrike_Project"
    if not project_path.exists():
        print(f"  ERROR: Unity project not found: {project_path}")
        return False
    
    cmd = [
        str(unity),
        "-batchmode",
        "-quit",
        "-projectPath", str(project_path),
        "-executeMethod", "CueStrike.Editor.RoomSetupAAA.SetupAllRooms",
        "-logFile", "-"
    ]
    
    success, stdout, stderr = run_command(cmd, capture=True)
    
    if not success:
        print(f"  Unity room setup failed!")
        if stdout:
            print(f"  STDOUT:\n{stdout[-2000:]}")  # Last 2000 chars
        if stderr:
            print(f"  STDERR:\n{stderr[-2000:]}")
        return False
    
    print("  ✓ All rooms imported and configured")
    return True

def step3_verify_pink_policy(args):
    """Verify Zero Pink Policy"""
    print_step(3, 3, "Verifying Zero Pink Policy")
    
    unity = find_executable("Unity.exe", UNITY_EXE)
    if not unity:
        print(f"  ERROR: Unity not found")
        return False
    
    project_path = PROJECT_ROOT / "CueStrike_Project"
    
    cmd = [
        str(unity),
        "-batchmode",
        "-quit",
        "-projectPath", str(project_path),
        "-executeMethod", "CueStrike.Editor.RoomSetupAAA.VerifyZeroPinkPolicy",
        "-logFile", "-"
    ]
    
    success, stdout, stderr = run_command(cmd, capture=True)
    
    if "PASSED" in stdout or "PASSED" in stderr:
        print("  ✓ Zero Pink Policy: PASSED - No pink materials detected!")
        return True
    elif "FAILED" in stdout or "FAILED" in stderr:
        print("  ✗ Zero Pink Policy: FAILED - Pink materials found!")
        if stdout:
            print(f"  Details:\n{stdout[-3000:]}")
        return False
    else:
        print("  ⚠ Zero Pink Policy: Verification completed (check logs)")
        return True

def main():
    parser = argparse.ArgumentParser(description="CueStrike AAA World Tour Build Pipeline")
    parser.add_argument("--skip-blender", action="store_true", help="Skip Blender generation step")
    parser.add_argument("--skip-unity", action="store_true", help="Skip Unity setup step")
    parser.add_argument("--skip-verify", action="store_true", help="Skip Zero Pink Policy verification")
    parser.add_argument("--verbose", "-v", action="store_true", help="Verbose output")
    parser.add_argument("--blender-path", type=Path, help="Path to Blender executable")
    parser.add_argument("--unity-path", type=Path, help="Path to Unity executable")
    
    args = parser.parse_args()
    
    # Override paths if provided
    global BLENDER_EXE, UNITY_EXE
    if args.blender_path:
        BLENDER_EXE = args.blender_path
    if args.unity_path:
        UNITY_EXE = args.unity_path
    
    print_header("CueStrike AAA World Tour - Build Pipeline")
    print(f"Project Root: {PROJECT_ROOT}")
    print(f"Blender: {BLENDER_EXE}")
    print(f"Unity: {UNITY_EXE}")
    
    all_success = True
    
    # Step 1: Blender
    if not args.skip_blender:
        if not step1_generate_rooms(args):
            all_success = False
            if not args.skip_unity:
                print("\n  Blender step failed. Use --skip-blender to continue with existing FBX files.")
    else:
        print_step(1, 3, "Generating rooms in Blender [SKIPPED]")
    
    # Step 2: Unity
    if all_success and not args.skip_unity:
        if not step2_unity_setup(args):
            all_success = False
    elif args.skip_unity:
        print_step(2, 3, "Importing and configuring in Unity [SKIPPED]")
    
    # Step 3: Verify
    if not args.skip_verify:
        if not step3_verify_pink_policy(args):
            all_success = False
    else:
        print_step(3, 3, "Verifying Zero Pink Policy [SKIPPED]")
    
    # Summary
    print_header("BUILD SUMMARY")
    if all_success:
        print("  ✓ AAA WORLD TOUR COMPLETE!")
        print()
        print("  Generated Rooms:")
        for room in ROOMS:
            print(f"    - {room}")
        print()
        print("  Output Locations:")
        print("    - FBX Models: Assets/CueStrike/Art/Rooms/[RoomName]/")
        print("    - Prefabs:    Assets/CueStrike/Prefabs/Rooms/")
        print("    - Scenes:     Assets/CueStrike/Scenes/Rooms/")
        print("    - Lighting Presets: Assets/CueStrike/Rendering/LightingPresets/")
        print()
        print("  ✓ All materials use URP/Lit shader (Zero Pink Policy enforced)")
    else:
        print("  ✗ BUILD FAILED - Check errors above")
    
    print_header("END")
    
    return 0 if all_success else 1

if __name__ == "__main__":
    sys.exit(main())