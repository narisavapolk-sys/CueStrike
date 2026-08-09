$content = @"
            bool executeCodeWorks = TestExecuteCodeTool().Result;
            results.Add(executeCodeWorks ? \"PASS: Execute Code tool works\" : \"FAIL: Execute Code tool failed\");
            if (!executeCodeWorks) allPassed = false;

            bool readFileWorks = TestReadFileTool().Result;
            results.Add(readFileWorks ? \"PASS: Read File tool works\" : \"FAIL: Read File tool failed\");
            if (!readFileWorks) allPassed = false;

            bool listFilesWorks = TestListFilesTool().Result;
            results.Add(listFilesWorks ? \"PASS: List Files tool works\" : \"FAIL: List Files tool failed\");
            if (!listFilesWorks) allPassed = false;

            bool searchFilesWorks = TestSearchFilesTool().Result;
            results.Add(searchFilesWorks ? \"PASS: Search Files tool works\" : \"FAIL: Search Files tool failed\");
            if (!searchFilesWorks) allPassed = false;

            bool pinkCheck = CheckZeroPinkPolicy();
            results.Add(pinkCheck ? \"PASS: Zero Pink Policy - No pink shaders detected\" : \"FAIL: Pink shaders found in scene/materials\");
            if (!pinkCheck) allPassed = false;

            bool audioCheck = CheckAudioLinks();
            results.Add(audioCheck ? \"PASS: Audio links configured\" : \"WARN: Some audio clips missing\");

            results.Add(\"\");
            results.Add(allPassed ? \"=== ALL TESTS PASSED ===\" : \"=== SOME TESTS FAILED ===\");

            foreach (var line in results)
            {
                if (line.StartsWith(\"PASS\")) Debug.Log(line);
                else if (line.StartsWith(\"FAIL\")) Debug.LogError(line);
                else if (line.StartsWith(\"WARN\")) Debug.LogWarning(line);
                else Debug.Log(line);
            }

            string message = string.Join(\"\\n\", results);
            EditorUtility.DisplayDialog(\"MCP Self-Test Results\", message, \"OK\");
        }

        static bool CheckMCPSettings()
        {
            var guids = AssetDatabase.FindAssets(\"t:CueStrike.MCP.McpSettings\");
            return guids.Length > 0;
        }
"@
$content | Add-Content 'c:\Users\mongo\UnityProjects\CueStrike\CueStrike_Project\Assets\CueStrike\Editor\MCPSelfTest.cs' -Encoding UTF8