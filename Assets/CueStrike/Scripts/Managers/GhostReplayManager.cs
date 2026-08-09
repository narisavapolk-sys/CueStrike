using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;
using CueStrike.Data;
using CueStrike.Replay;

namespace CueStrike.Managers
{
    public class GhostReplayManager : MonoBehaviour
    {
        public const int MAX_SLOTS = 5;
        public static GhostReplayManager Instance { get; private set; }

        [Header("References")]
        [SerializeField] private GhostReplayRecorder recorder;
        [SerializeField] private GhostReplayPlayer player;

        private string SaveDirectory => Application.persistentDataPath + "/Replays/";
        private GhostReplaySlotInfo[] slots = new GhostReplaySlotInfo[MAX_SLOTS];

        public event Action<int> OnSlotSaved;
        public event Action<int> OnSlotDeleted;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            Directory.CreateDirectory(SaveDirectory);
            LoadAllMetadata();
        }

        public bool SaveToSlot(int slotIndex, string customName = null)
        {
            if (!ValidateSlot(slotIndex)) return false;
            if (recorder == null || !recorder.HasRecording())
            {
                Debug.LogWarning("[GhostReplayManager] No recording to save");
                return false;
            }

            var data = recorder.GetLastReplay();
            if (!string.IsNullOrEmpty(customName))
                data.replayName = customName;

            // Save .dat file
            string datPath = GetDatPath(slotIndex);
            try
            {
                using (FileStream fs = new FileStream(datPath, FileMode.Create))
                {
                    BinaryFormatter bf = new BinaryFormatter();
                    bf.Serialize(fs, data);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[GhostReplayManager] Save failed: {e.Message}");
                return false;
            }

            // Update metadata
            slots[slotIndex] = new GhostReplaySlotInfo
            {
                slotIndex = slotIndex,
                isOccupied = true,
                replayName = data.replayName,
                dateSaved = data.dateSaved,
                duration = data.shotDuration,
                score = data.score,
                pocketedCount = data.pocketedBallIds.Count
            };
            SaveMetadata();

            OnSlotSaved?.Invoke(slotIndex);
            Debug.Log($"[GhostReplayManager] Saved to slot {slotIndex}: {data.replayName}");
            return true;
        }

        public GhostReplayData LoadFromSlot(int slotIndex)
        {
            if (!ValidateSlot(slotIndex)) return null;
            if (!IsSlotOccupied(slotIndex))
            {
                Debug.LogWarning($"[GhostReplayManager] Slot {slotIndex} is empty");
                return null;
            }

            string datPath = GetDatPath(slotIndex);
            try
            {
                using (FileStream fs = new FileStream(datPath, FileMode.Open))
                {
                    BinaryFormatter bf = new BinaryFormatter();
                    return bf.Deserialize(fs) as GhostReplayData;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[GhostReplayManager] Load failed: {e.Message}");
                return null;
            }
        }

        public void DeleteSlot(int slotIndex)
        {
            if (!ValidateSlot(slotIndex)) return;
            
            string datPath = GetDatPath(slotIndex);
            if (File.Exists(datPath)) File.Delete(datPath);

            slots[slotIndex] = new GhostReplaySlotInfo { slotIndex = slotIndex };
            SaveMetadata();
            OnSlotDeleted?.Invoke(slotIndex);
        }

        public bool IsSlotOccupied(int slotIndex)
        {
            return ValidateSlot(slotIndex) && slots[slotIndex].isOccupied;
        }

        public GhostReplaySlotInfo GetSlotInfo(int slotIndex)
        {
            return ValidateSlot(slotIndex) ? slots[slotIndex] : null;
        }

        public List<GhostReplaySlotInfo> GetAllSlotsInfo()
        {
            return new List<GhostReplaySlotInfo>(slots);
        }

        public void PlaySlot(int slotIndex)
        {
            var data = LoadFromSlot(slotIndex);
            if (data != null && player != null)
            {
                player.PlayReplay(data);
            }
        }

        public void ExportSlot(int slotIndex, string destinationPath)
        {
            if (!IsSlotOccupied(slotIndex)) return;
            string source = GetDatPath(slotIndex);
            if (File.Exists(source))
            {
                File.Copy(source, destinationPath, true);
                Debug.Log($"[GhostReplayManager] Exported slot {slotIndex} to {destinationPath}");
            }
        }

        // -------- Private --------

        private bool ValidateSlot(int index)
        {
            if (index < 0 || index >= MAX_SLOTS)
            {
                Debug.LogError($"[GhostReplayManager] Invalid slot index: {index}");
                return false;
            }
            return true;
        }

        private string GetDatPath(int slot) => SaveDirectory + $"replay_slot_{slot}.dat";
        private string GetMetaPath() => SaveDirectory + "replay_meta.json";

        private void SaveMetadata()
        {
            string json = JsonUtility.ToJson(new GhostReplayMetadata { slots = slots }, true);
            File.WriteAllText(GetMetaPath(), json);
        }

        private void LoadAllMetadata()
        {
            string metaPath = GetMetaPath();
            if (!File.Exists(metaPath))
            {
                for (int i = 0; i < MAX_SLOTS; i++) slots[i] = new GhostReplaySlotInfo { slotIndex = i };
                return;
            }

            try
            {
                string json = File.ReadAllText(metaPath);
                var meta = JsonUtility.FromJson<GhostReplayMetadata>(json);
                if (meta?.slots != null)
                {
                    for (int i = 0; i < Mathf.Min(meta.slots.Length, MAX_SLOTS); i++)
                        slots[i] = meta.slots[i];
                }
            }
            catch
            {
                for (int i = 0; i < MAX_SLOTS; i++) slots[i] = new GhostReplaySlotInfo { slotIndex = i };
            }
        }
    }

    [Serializable]
    public class GhostReplaySlotInfo
    {
        public int slotIndex;
        public bool isOccupied;
        public string replayName;
        public string dateSaved;
        public float duration;
        public int score;
        public int pocketedCount;
    }

    [Serializable]
    public class GhostReplayMetadata
    {
        public GhostReplaySlotInfo[] slots;
    }
} // namespace CueStrike.Managers