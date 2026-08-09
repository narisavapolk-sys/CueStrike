using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CueStrike.Managers;

namespace CueStrike.UI
{
    public class GhostReplaySlotUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI dateText;
        [SerializeField] private Button playButton;
        [SerializeField] private Button deleteButton;
        [SerializeField] private Button overwriteButton;
        [SerializeField] private GameObject emptyIndicator;

        private int slotIndex;
        private GhostReplayUI parent;

        public void Setup(int index, GhostReplaySlotInfo info, GhostReplayUI uiParent)
        {
            slotIndex = index;
            parent = uiParent;

            bool occupied = info != null && info.isOccupied;

            if (nameText != null)
                nameText.text = occupied ? info.replayName : $"Slot {index + 1} (Empty)";
            if (dateText != null)
                dateText.text = occupied ? info.dateSaved : "--";
            if (emptyIndicator != null)
                emptyIndicator.SetActive(!occupied);

            if (playButton != null)
            {
                playButton.gameObject.SetActive(occupied);
                playButton.onClick.RemoveAllListeners();
                playButton.onClick.AddListener(() => parent.OnSlotPlayClicked(slotIndex));
            }

            if (deleteButton != null)
            {
                deleteButton.gameObject.SetActive(occupied);
                deleteButton.onClick.RemoveAllListeners();
                deleteButton.onClick.AddListener(() => parent.OnSlotDeleteClicked(slotIndex));
            }

            if (overwriteButton != null)
            {
                overwriteButton.onClick.RemoveAllListeners();
                overwriteButton.onClick.AddListener(() => parent.OnSlotOverwriteClicked(slotIndex));
            }
        }
    }
}