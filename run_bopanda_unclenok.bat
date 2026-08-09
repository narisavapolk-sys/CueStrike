@echo off
cd /d "C:\Program Files\Blender Foundation\Blender 3.6"
set CHARACTERS_ONLY=BoPanda,UncleNok
blender.exe --background --python "C:\Users\mongo\UnityProjects\CueStrike\CueStrike_Project\BlenderScripts\create_all_characters_aaa.py" > "C:\Users\mongo\UnityProjects\CueStrike\CueStrike_Project\blender_bopanda_unclenok.log" 2>&1
echo EXIT_CODE=%ERRORLEVEL%