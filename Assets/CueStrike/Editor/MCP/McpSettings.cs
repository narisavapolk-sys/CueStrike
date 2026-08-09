using UnityEngine;

namespace CueStrike.MCP
{
    /// <summary>
    /// Configuration settings for the MCP Server.
    /// Stored as a ScriptableObject for easy editing in the Unity Editor.
    /// </summary>
    [CreateAssetMenu(fileName = "McpSettings", menuName = "CueStrike/MCP/Settings")]
    public class McpSettings : ScriptableObject
    {
        [Header("Server Configuration")]
        [Tooltip("Port for the HTTP server (default: 8080)")]
        public int port = 8080;

        [Tooltip("Require authentication token for requests")]
        public bool requireAuth = false;

        [Tooltip("Authentication token (used if requireAuth is true)")]
        public string authToken = "";

        [Tooltip("Enable CORS for browser-based clients")]
        public bool enableCors = true;

        [Header("Limits")]
        [Tooltip("Maximum request body size in bytes (default: 1MB)")]
        public int maxRequestSize = 1024 * 1024;

        [Tooltip("Request timeout in seconds (default: 30s)")]
        public int requestTimeoutSeconds = 30;

        [Header("Logging")]
        [Tooltip("Log all requests to Unity Console")]
        public bool logRequests = true;

        [Tooltip("Log response bodies (can be verbose)")]
        public bool logResponses = false;

        /// <summary>
        /// Validates the settings and returns any errors.
        /// </summary>
        public string Validate()
        {
            if (port < 1 || port > 65535)
                return $"Invalid port: {port}. Must be between 1 and 65535.";

            if (requireAuth && string.IsNullOrEmpty(authToken))
                return "Authentication is required but no token is set.";

            if (maxRequestSize < 1024)
                return "Max request size must be at least 1KB.";

            if (requestTimeoutSeconds < 1)
                return "Request timeout must be at least 1 second.";

            return null; // Valid
        }
    }
}