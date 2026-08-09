using UnityEditor;
using UnityEngine;

namespace CueStrike.Editor
{
    public class MCP_FORCE_DASHBOARD : EditorWindow
    {
        [MenuItem("CueStrike/Force Dashboard")]
        public static void ShowWindow() { GetWindow<MCP_FORCE_DASHBOARD>("Force Dash"); }
        
        void OnGUI() 
        { 
            GUILayout.Label("MCP SYSTEM ACTIVE"); 
            if (GUILayout.Button("Ping")) Debug.Log("PING OK"); 
        }
    }
}
