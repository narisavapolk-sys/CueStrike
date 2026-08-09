$content = @"
        static bool CheckZeroPinkPolicy()
        {
            var renderers = Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var renderer in renderers)
            {
                foreach (var material in renderer.sharedMaterials)
                {
                    if (material == null) continue;

                    if (material.shader == null || material.shader.name.Contains(\"Hidden/Error\") || material.shader.name == \"Standard\")
                    {
                        Debug.LogWarning(\$"[MCP Self-Test] Pink shader detected: {renderer.gameObject.name} {material.name} (shader: {material.shader?.name ?? \"null\"})");
                        return false;
                    }
                }
            }
            return true;
        }

        static bool CheckAudioLinks()
        {
            string[] requiredAudioClips = new[] { \"ball_impact\", \"ball_cushion\", \"ball_pocket\", \"cue_hit\", \"ambient_room\" };
            bool allFound = true;
            foreach (var clipName in requiredAudioClips)
            {
                var guids = AssetDatabase.FindAssets(\$"t:AudioClip {clipName}");
                if (guids.Length == 0)
                {
                    Debug.LogWarning(\$"[MCP Self-Test] Missing audio clip: {clipName}");
                    allFound = false;
                }
            }
            return allFound;
        }
    }
}
"@
$content | Add-Content 'c:\Users\mongo\UnityProjects\CueStrike\CueStrike_Project\Assets\CueStrike\Editor\MCPSelfTest.cs' -Encoding UTF8