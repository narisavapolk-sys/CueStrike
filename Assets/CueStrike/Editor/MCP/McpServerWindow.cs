using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace CueStrike.MCP
{
    /// <summary>
    /// Editor window for managing the MCP Server.
    /// </summary>
    public class McpServerWindow : EditorWindow
    {
        private Vector2 _scrollPosition;
        private McpSettings _settings;
        private bool _showTools = true;
        private bool _showLogs = true;
        private List<string> _logs = new();
        private int _maxLogs = 100;

        [MenuItem("CueStrike/MCP Server", false, 100)]
        public static void ShowWindow()
        {
            var window = GetWindow<McpServerWindow>("MCP Server");
            window.minSize = new Vector2(400, 500);
        }

        private void OnEnable()
        {
            LoadSettings();
            McpServer.OnLog += AddLog;
        }

        private void OnDisable()
        {
            McpServer.OnLog -= AddLog;
        }

        private void LoadSettings()
        {
            var guids = AssetDatabase.FindAssets("t:McpSettings");
            if (guids.Length > 0)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[0]);
                _settings = AssetDatabase.LoadAssetAtPath<McpSettings>(path);
            }

            if (_settings == null)
            {
                _settings = ScriptableObject.CreateInstance<McpSettings>();
            }
        }

        private void OnGUI()
        {
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            DrawHeader();
            DrawServerControls();
            DrawSettings();
            DrawTools();
            DrawLogs();

            EditorGUILayout.EndScrollView();
        }

        private void DrawHeader()
        {
            EditorGUILayout.Space(10);
            var titleStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 18, alignment = TextAnchor.MiddleCenter };
            EditorGUILayout.LabelField("🔌 CueStrike MCP Server", titleStyle);
            EditorGUILayout.Space(5);

            var statusStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 12,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = McpServer.IsRunning ? Color.green : Color.red }
            };
            EditorGUILayout.LabelField(McpServer.IsRunning ? "● RUNNING" : "● STOPPED", statusStyle);
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            EditorGUILayout.Space(5);
        }

        private void DrawServerControls()
        {
            EditorGUILayout.LabelField("Server Controls", EditorStyles.boldLabel);

            EditorGUI.BeginDisabledGroup(McpServer.IsRunning);
            if (GUILayout.Button("▶ Start Server", GUILayout.Height(30)))
            {
                if (ValidateAndStart())
                {
                    AddLog($"Server started on port {_settings.port}");
                }
            }
            EditorGUI.EndDisabledGroup();

            EditorGUI.BeginDisabledGroup(!McpServer.IsRunning);
            if (GUILayout.Button("■ Stop Server", GUILayout.Height(30)))
            {
                McpServer.StopServer();
                AddLog("Server stopped");
            }
            EditorGUI.EndDisabledGroup();

            if (McpServer.IsRunning)
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.SelectableLabel($"Server URL: {McpServer.ServerUrl}", EditorStyles.textField, GUILayout.Height(20));
                EditorGUILayout.SelectableLabel($"MCP Endpoint: {McpServer.ServerUrl}mcp", EditorStyles.textField, GUILayout.Height(20));

                if (GUILayout.Button("Copy Server URL"))
                {
                    EditorGUIUtility.systemCopyBuffer = McpServer.ServerUrl;
                    AddLog("Server URL copied to clipboard");
                }
                if (GUILayout.Button("Copy MCP Endpoint"))
                {
                    EditorGUIUtility.systemCopyBuffer = McpServer.ServerUrl + "mcp";
                    AddLog("MCP endpoint copied to clipboard");
                }
            }
        }

        private void DrawSettings()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Settings", EditorStyles.boldLabel);

            if (_settings == null)
            {
                LoadSettings();
            }

            EditorGUI.BeginChangeCheck();

            _settings.port = EditorGUILayout.IntField("Port", _settings.port);
            _settings.requireAuth = EditorGUILayout.Toggle("Require Authentication", _settings.requireAuth);

            if (_settings.requireAuth)
            {
                _settings.authToken = EditorGUILayout.PasswordField("Auth Token", _settings.authToken);
            }

            _settings.enableCors = EditorGUILayout.Toggle("Enable CORS", _settings.enableCors);
            _settings.maxRequestSize = EditorGUILayout.IntField("Max Request Size (bytes)", _settings.maxRequestSize);
            _settings.requestTimeoutSeconds = EditorGUILayout.IntField("Request Timeout (seconds)", _settings.requestTimeoutSeconds);
            _settings.logRequests = EditorGUILayout.Toggle("Log Requests", _settings.logRequests);
            _settings.logResponses = EditorGUILayout.Toggle("Log Responses", _settings.logResponses);

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(_settings);
                var error = _settings.Validate();
                if (error != null)
                {
                    EditorGUILayout.HelpBox(error, MessageType.Error);
                }
            }

            EditorGUILayout.Space(5);
            if (GUILayout.Button("Save Settings"))
            {
                SaveSettings();
            }

            if (GUILayout.Button("Create Settings Asset"))
            {
                CreateSettingsAsset();
            }
        }

        private void DrawTools()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            EditorGUILayout.Space(5);

            _showTools = EditorGUILayout.Foldout(_showTools, "Registered Tools", true, EditorStyles.foldoutHeader);
            if (_showTools)
            {
                var tools = McpServer.GetTools();
                foreach (var kvp in tools)
                {
                    var tool = kvp.Value;
                    EditorGUILayout.BeginVertical("box");
                    EditorGUILayout.LabelField(tool.Name, EditorStyles.boldLabel);
                    EditorGUILayout.LabelField(tool.Description, EditorStyles.wordWrappedMiniLabel);
                    EditorGUILayout.EndVertical();
                    EditorGUILayout.Space(2);
                }

                EditorGUILayout.HelpBox($"{tools.Count} tools registered", MessageType.Info);
            }
        }

        private void DrawLogs()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            EditorGUILayout.Space(5);

            _showLogs = EditorGUILayout.Foldout(_showLogs, "Server Logs", true, EditorStyles.foldoutHeader);
            if (_showLogs)
            {
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Clear Logs", GUILayout.Width(100)))
                {
                    _logs.Clear();
                }
                _maxLogs = EditorGUILayout.IntField("Max Logs", _maxLogs, GUILayout.Width(150));
                EditorGUILayout.EndHorizontal();

                var logStyle = new GUIStyle(EditorStyles.textArea)
                {
                    wordWrap = true,
                    fontSize = 10,
                    richText = true
                };

                string logText = string.Join("\n", _logs);
                EditorGUILayout.TextArea(logText, logStyle, GUILayout.MinHeight(150), GUILayout.MaxHeight(300));
            }
        }

        private bool ValidateAndStart()
        {
            if (_settings == null)
            {
                LoadSettings();
            }

            var error = _settings?.Validate();
            if (error != null)
            {
                EditorUtility.DisplayDialog("Invalid Settings", error, "OK");
                return false;
            }

            return McpServer.StartServer(_settings.port, _settings.requireAuth, _settings.authToken);
        }

        private void SaveSettings()
        {
            if (_settings == null) return;

            var path = AssetDatabase.GetAssetPath(_settings);
            if (string.IsNullOrEmpty(path))
            {
                CreateSettingsAsset();
            }
            else
            {
                EditorUtility.SetDirty(_settings);
                AssetDatabase.SaveAssets();
                AddLog("Settings saved");
            }
        }

        private void CreateSettingsAsset()
        {
            string folder = "Assets/CueStrike/Editor/MCP";
            if (!AssetDatabase.IsValidFolder(folder))
            {
                var guids = AssetDatabase.FindAssets("CueStrike");
                foreach (var guid in guids)
                {
                    var p = AssetDatabase.GUIDToAssetPath(guid);
                    if (p.EndsWith("CueStrike") && AssetDatabase.IsValidFolder(p))
                    {
                        folder = p + "/Editor/MCP";
                        break;
                    }
                }
            }

            if (!AssetDatabase.IsValidFolder(folder))
            {
                AssetDatabase.CreateFolder("Assets", "CueStrike");
                AssetDatabase.CreateFolder("Assets/CueStrike", "Editor");
                AssetDatabase.CreateFolder("Assets/CueStrike/Editor", "MCP");
            }

            string assetPath = "Assets/CueStrike/Editor/MCP/McpSettings.asset";
            AssetDatabase.CreateAsset(_settings, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            AddLog($"Settings asset created at {assetPath}");
        }

        private void AddLog(string message)
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss");
            _logs.Insert(0, $"[{timestamp}] {message}");

            if (_logs.Count > _maxLogs)
            {
                _logs.RemoveRange(_maxLogs, _logs.Count - _maxLogs);
            }

            Repaint();
        }
    }
}