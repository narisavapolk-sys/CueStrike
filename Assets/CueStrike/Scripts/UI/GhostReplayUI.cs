using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CueStrike.Managers;
using CueStrike.Replay;
using CueStrike.Data;

namespace CueStrike.UI
{
    public class GhostReplayUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GhostReplayManager manager;
        [SerializeField] private GhostReplayRecorder recorder;

        [Header("UI Elements")]
        [SerializeField] private Transform slotsContainer;
        [SerializeField] private GameObject slotPrefab;
        [SerializeField] private Button saveCurrentButton;
        [SerializeField] private TMP_InputField nameInput;

        [Header("Panels")]
        [SerializeField] private GameObject replayPanel;
        [SerializeField] private GameObject overwriteConfirmPanel;
        [SerializeField] private TextMeshProUGUI overwriteText;

        private List<GhostReplaySlotUI> slotUIs = new List<GhostReplaySlotUI>();
        private int pendingOverwriteSlot = -1;

        private void Start()
        {
            if (manager != null)
            {
                manager.OnSlotSaved += RefreshUI;
                manager.OnSlotDeleted += RefreshUI;
            }
            if (saveCurrentButton != null)
                saveCurrentButton.onClick.AddListener(OnSaveCurrentClicked);

            RefreshUI();
        }

        private void OnDestroy()
        {
            if (manager != null)
            {
                manager.OnSlotSaved -= RefreshUI;
                manager.OnSlotDeleted -= RefreshUI;
            }
        }

        public void ShowPanel()
        {
            if (replayPanel != null) replayPanel.SetActive(true);
            RefreshUI();
        }

        public void HidePanel()
        {
            if (replayPanel != null) replayPanel.SetActive(false);
        }

        private void RefreshUI(int slot = -1)
        {
            var slotsInfo = manager != null ? manager.GetAllSlotsInfo() : new List<GhostReplaySlotInfo>();
            
            for (int i = 0; i < GhostReplayManager.MAX_SLOTS; i++)
            {
                if (i >= slotUIs.Count)
                {
                    if (slotPrefab != null && slotsContainer != null)
                    {
                        var go = Instantiate(slotPrefab, slotsContainer);
                        var ui = go.GetComponent<GhostReplaySlotUI>();
                        if (ui != null) slotUIs.Add(ui);
                    }
                }

                 if (i < slotUIs.Count && slotUIs[i] != null)
                 {
                     var info = (i < slotsInfo.Count) ? slotsInfo[i] : new GhostReplaySlotInfo { slotIndex = i };
                     slotUIs[i].Setup(i, info, this);
                 }
            }

            if (saveCurrentButton != null)
                saveCurrentButton.interactable = recorder != null && recorder.HasRecording();
        }

        private void OnSaveCurrentClicked()
        {
            for (int i = 0; i < GhostReplayManager.MAX_SLOTS; i++)
            {
                if (manager != null && !manager.IsSlotOccupied(i))
                {
                    SaveToSlot(i);
                    return;
                }
            }

            Debug.LogWarning("[GhostReplayUI] All slots full. Select a slot to overwrite.");
        }

        public void OnSlotPlayClicked(int slotIndex)
        {
            if (manager != null) manager.PlaySlot(slotIndex);
        }

        public void OnSlotDeleteClicked(int slotIndex)
        {
            if (manager != null) manager.DeleteSlot(slotIndex);
        }

        public void OnSlotOverwriteClicked(int slotIndex)
        {
            if (manager != null && !manager.IsSlotOccupied(slotIndex))
            {
                SaveToSlot(slotIndex);
                return;
            }

            pendingOverwriteSlot = slotIndex;
            var info = manager != null ? manager.GetSlotInfo(slotIndex) : null;
            if (overwriteText != null)
                overwriteText.text = info != null ? $"Overwrite '{info.replayName}'?" : "Overwrite?";
            if (overwriteConfirmPanel != null)
                overwriteConfirmPanel.SetActive(true);
        }

        public void ConfirmOverwrite()
        {
            if (pendingOverwriteSlot >= 0 && manager != null)
            {
                SaveToSlot(pendingOverwriteSlot);
                pendingOverwriteSlot = -1;
            }
            if (overwriteConfirmPanel != null)
                overwriteConfirmPanel.SetActive(false);
        }

        public void CancelOverwrite()
        {
            pendingOverwriteSlot = -1;
            if (overwriteConfirmPanel != null)
                overwriteConfirmPanel.SetActive(false);
        }

        private void SaveToSlot(int slotIndex)
        {
            string name = (nameInput != null && !string.IsNullOrEmpty(nameInput.text)) ? nameInput.text : null;
            if (manager != null) manager.SaveToSlot(slotIndex, name);
            if (nameInput != null) nameInput.text = "";
        }
    }
}