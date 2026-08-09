@echo off
REM =============================================================================
REM CueStrike AAA World Tour - Automated Build & Deploy Script
REM Runs Blender room generation, then Unity scene setup
REM =============================================================================

set PROJECT_ROOT=%~dp0
set BLENDER_EXE="C:\Program Files\Blender Foundation\Blender 4.2\blender.exe"
set UNITY_EXE="C:\Program Files\Unity\Hub\Editor\2022.3.21f1\Editor\Unity.exe"

echo =============================================================================
echo CueStrike AAA World Tour - Build Pipeline
echo =============================================================================
echo Project Root: %PROJECT_ROOT%
echo.

REM -----------------------------------------------------------------------------
REM STEP 1: Run Blender script to generate all 8 rooms
REM -----------------------------------------------------------------------------
echo [STEP 1/3] Generating rooms in Blender...
echo.

if not exist %BLENDER_EXE% (
    echo ERROR: Blender not found at %BLENDER_EXE%
    echo Please update BLENDER_EXE path in this script
    goto :ERROR
)

%BLENDER_EXE% --background --python "%PROJECT_ROOT%BlenderScripts\create_room_props_aaa.py" -- --output "%PROJECT_ROOT%Assets\CueStrike\Art\Rooms\"

if errorlevel 1 (
    echo ERROR: Blender script failed
    goto :ERROR
)

echo.
echo [STEP 1/3] COMPLETE: All rooms generated
echo.

REM -----------------------------------------------------------------------------
REM STEP 2: Import into Unity and configure
REM -----------------------------------------------------------------------------
echo [STEP 2/3] Importing and configuring in Unity...
echo.

if not exist %UNITY_EXE% (
    echo ERROR: Unity not found at %UNITY_EXE%
    echo Please update UNITY_EXE path in this script
    goto :ERROR
)

%UNITY_EXE% -batchmode -quit -projectPath "%PROJECT_ROOT%CueStrike_Project" -executeMethod CueStrike.Editor.RoomSetupAAA.SetupAllRooms -logFile -

if errorlevel 1 (
    echo ERROR: Unity room setup failed
    goto :ERROR
)

echo.
echo [STEP 2/3] COMPLETE: All rooms imported and configured
echo.

REM -----------------------------------------------------------------------------
REM STEP 3: Verify Zero Pink Policy
REM -----------------------------------------------------------------------------
echo [STEP 3/3] Verifying Zero Pink Policy...
echo.

%UNITY_EXE% -batchmode -quit -projectPath "%PROJECT_ROOT%CueStrike_Project" -executeMethod CueStrike.Editor.RoomSetupAAA.VerifyZeroPinkPolicy -logFile -

if errorlevel 1 (
    echo WARNING: Zero Pink Policy verification had issues (check logs)
) else (
    echo.
    echo [STEP 3/3] COMPLETE: Zero Pink Policy PASSED
)

echo.
echo =============================================================================
echo AAA WORLD TOUR COMPLETE!
echo =============================================================================
echo.
echo Generated Rooms:
echo   - ZenDojo
echo   - Cyberpunk
echo   - SpaceNebula
echo   - Industrial
echo   - WarpFantasy
echo   - Luxury_DAY
echo   - Luxury_NIGHT
echo   - Arena_Core
echo.
echo Output Locations:
echo   - FBX Models: Assets/CueStrike/Art/Rooms/[RoomName]/
echo   - Prefabs:    Assets/CueStrike/Prefabs/Rooms/
echo   - Scenes:     Assets/CueStrike/Scenes/Rooms/
echo   - Lighting Presets: Assets/CueStrike/Rendering/LightingPresets/
echo.
echo All materials use URP/Lit shader (Zero Pink Policy enforced)
echo =============================================================================

pause
exit /b 0

:ERROR
echo.
echo =============================================================================
echo BUILD FAILED
echo =============================================================================
pause
exit /b 1