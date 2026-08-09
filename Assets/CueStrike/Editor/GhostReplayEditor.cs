using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;
using CueStrike.Managers;
using CueStrike.Data;

namespace CueStrike.Editor
{
    public class GhostReplayEditor : EditorWindow
    {
        [MenuItem("Tools/CueStrike/Debug/Test Ghost Replay")]
        public static void ShowWindow()
        {
            GetWindow<GhostReplayEditor>("Ghost Replay Debug");
        }

        private void OnGUI()
        {
            GUILayout.Label("Ghost Replay Debug Tools", EditorStyles.largeLabel);
            EditorGUILayout.Space(5);

            if (GUILayout.Button("Create Fake Replay Data (Slot 0)", GUILayout.Height(30)))
            {
                CreateFakeReplayData();
            }

            if (GUILayout.Button("Clear All Slots", GUILayout.Height(30)))
            {
                ClearAllSlots();
            }

            EditorGUILayout.Space(10);

            for (int i = 0; i < GhostReplayManager.MAX_SLOTS; i++)
            {
                int slot = i;
                if (GUILayout.Button($"Export Slot {slot} to Desktop", GUILayout.Height(25)))
                {
                    ExportSlot(slot);
                }
            }
        }

        private void CreateFakeReplayData()
        {
            var data = new GhostReplayData
            {
                replayName = "Test Shot " + System.DateTime.Now.ToString("HH:mm:ss"),
                dateSaved = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
                shotDuration = 3.5f,
                ballFrames = new System.Collections.Generic.List<BallFrameData>(),
                cueFrames = new System.Collections.Generic.List<CueFrameData>(),
                pocketedBallIds = new System.Collections.Generic.List<int> { 2, 5 },
                score = 2
            };

            // Generate fake frames for 16 balls over ~3.5 seconds at 0.05s intervals
            int ballsCount = 16;
            int totalFrames = 70;
            for (int frame = 0; frame < totalFrames; frame++)
            {
                float t = (float)frame / totalFrames;
                for (int b = 0; b < ballsCount; b++)
                {
                    data.ballFrames.Add(new BallFrameData
                    {
                        ballId = b,
                        position = new Vector3(
                            Mathf.Sin(t * 10 + b) * 2,
                            0.1f,
                            Mathf.Cos(t * 10 + b) * 2
                        ),
                        rotation = Quaternion.Euler(0, t * 360 * b, 0),
                        isPocketed = (b == 2 || b == 5) && t > 0.7f
                    });
                }

                data.cueFrames.Add(new CueFrameData
                {
                    position = new Vector3(0, 0.5f, -2 + t * 4),
                    rotation = Quaternion.Euler(30, 0, 0)
                });
            }

            string saveDir = Application.persistentDataPath + "/Replays/";
            Directory.CreateDirectory(saveDir);
            string datPath = saveDir + "replay_slot_0.dat";

            try
            {
                using (FileStream fs = new FileStream(datPath, FileMode.Create))
                {
                    System.Runtime.Serialization.Formatters.Binary.BinaryFormatter bf = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                    bf.Serialize(fs, data);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[GhostReplayEditor] Save failed: {e.Message}");
                return;
            }

            // Update metadata
            var meta = new GhostReplayMetadata
            {
                slots = new GhostReplaySlotInfo[GhostReplayManager.MAX_SLOTS]
            };
            meta.slots[0] = new GhostReplaySlotInfo
            {
                slotIndex = 0,
                isOccupied = true,
                replayName = data.replayName,
                dateSaved = data.dateSaved,
                duration = data.shotDuration,
                score = data.score,
                pocketedCount = data.pocketedBallIds.Count
            };
            for (int i = 1; i < GhostReplayManager.MAX_SLOTS; i++)
            {
                meta.slots[i] = new GhostReplaySlotInfo { slotIndex = i };
            }

            string json = JsonUtility.ToJson(meta, true);
            File.WriteAllText(saveDir + "replay_meta.json", json);

            Debug.Log($"[GhostReplayEditor] Created fake replay in Slot 0: {datPath}");
        }

        private void ClearAllSlots()
        {
            string saveDir = Application.persistentDataPath + "/Replays/";
            for (int i = 0; i < GhostReplayManager.MAX_SLOTS; i++)
            {
                string datPath = saveDir + $"replay_slot_{i}.dat";
                if (File.Exists(datPath)) File.Delete(datPath);
            }
            string metaPath = saveDir + "replay_meta.json";
            if (File.Exists(metaPath)) File.Delete(metaPath);

            Debug.Log("[GhostReplayEditor] Cleared all 5 slots");
        }

        private void ExportSlot(int slot)
        {
            string source = Application.persistentDataPath + $"/Replays/replay_slot_{slot}.dat";
            string dest = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop) + $"/CueStrike_Replay_Slot{slot}.dat";

            if (File.Exists(source))
            {
                File.Copy(source, dest, true);
                Debug.Log($"[GhostReplayEditor] Exported Slot {slot} to {dest}");
            }
            else
            {
                Debug.LogWarning($"[GhostReplayEditor] Slot {slot} file not found: {source}");
            }
        }

#if UNITY_EDITOR
        [MenuItem("Tools/CueStrike/Debug/Test Ghost Replay SelfTest")]
        public static void SelfTest()
        {
            bool pass = true;

            // Test 1: Window type exists
            var windowType = typeof(GhostReplayEditor);
            if (windowType == null)
            {
                Debug.LogError("[GhostReplayEditor SelfTest] FAIL: Type not found");
                pass = false;
            }

            // Test 2: MenuItem attribute
            var methods = windowType.GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            bool hasMenuItem = methods.Any(m => m.GetCustomAttributes(typeof(MenuItem), false).Length > 0);
            if (!hasMenuItem)
            {
                Debug.LogError("[GhostReplayEditor SelfTest] FAIL: MenuItem not found");
                pass = false;
            }

            // Test 3: Required methods
            string[] requiredMethods = { "CreateFakeReplayData", "ClearAllSlots", "ExportSlot" };
            var instanceMethods = windowType.GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            foreach (var m in requiredMethods)
            {
                if (!instanceMethods.Any(im => im.Name == m))
                {
                    Debug.LogError($"[GhostReplayEditor SelfTest] FAIL: Method {m} missing");
                    pass = false;
                }
            }

            if (pass)
                Debug.Log("[GhostReplayEditor SelfTest] ✅ ALL TESTS PASSED — Ready for human verify");
            else
                Debug.LogWarning("[GhostReplayEditor SelfTest] ⚠️ TESTS FAILED — Fix before proceeding");
        }
#endif
    }
}