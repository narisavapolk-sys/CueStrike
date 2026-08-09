$content = Get-Content 'c:\Users\mongo\UnityProjects\CueStrike\CueStrike_Project\Assets\CueStrike\Editor\MCPSelfTest.cs'
$content[95] = ''
$content[218] = ''
$content | Set-Content 'c:\Users\mongo\UnityProjects\CueStrike\CueStrike_Project\Assets\CueStrike\Editor\MCPSelfTest.cs'