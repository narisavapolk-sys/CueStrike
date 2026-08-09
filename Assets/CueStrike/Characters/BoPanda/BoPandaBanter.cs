using UnityEngine;
using UnityEngine.Events;

namespace CueStrike.MascotSystem
{
    /// <summary>
    /// Bo Panda — Hype Mascot character.
    /// Delivers reactions to game events: victory, clutch shots, misses, fouls.
    /// Used by CueStrikeMascotManager.
    /// </summary>
    public class BoPandaBanter : MonoBehaviour
    {
        [System.Serializable]
        public enum ReactionType
        {
            Victory,
            Clutch,
            SinglePot,
            Miss,
            Foul
        }

        [Header("Reaction Settings")]
        public ReactionType currentReaction = ReactionType.Victory;

        [Header("Events")]
        public UnityEvent<string> OnReactionDelivered = new UnityEvent<string>();
        public UnityEvent OnBigCelebration = new UnityEvent();

        /// <summary>
        /// Trigger a reaction by type
        /// </summary>
        public void TriggerReaction(ReactionType type)
        {
            currentReaction = type;
            string message = GetReactionMessage(type);
            Debug.Log($"[BoPanda] Reaction: {type} — {message}");
            OnReactionDelivered?.Invoke(message);
            
            if (type == ReactionType.Clutch || type == ReactionType.Victory)
            {
                OnBigCelebration?.Invoke();
            }
        }

        /// <summary>
        /// Handle shot result from MascotManager
        /// </summary>
        public void OnShotResult(CueStrike.Characters.CueStrikeMascotManager.ShotResult result, float quality)
        {
            switch (result)
            {
                case CueStrike.Characters.CueStrikeMascotManager.ShotResult.Potted:
                    TriggerReaction(quality > 0.8f ? ReactionType.Clutch : ReactionType.SinglePot);
                    break;
                case CueStrike.Characters.CueStrikeMascotManager.ShotResult.Missed:
                    TriggerReaction(ReactionType.Miss);
                    break;
                case CueStrike.Characters.CueStrikeMascotManager.ShotResult.Foul:
                    TriggerReaction(ReactionType.Foul);
                    break;
                case CueStrike.Characters.CueStrikeMascotManager.ShotResult.Win:
                    TriggerReaction(ReactionType.Victory);
                    break;
            }
        }

        /// <summary>
        /// Handle frame events
        /// </summary>
        public void OnFrameEvent(CueStrike.Characters.CueStrikeMascotManager.FrameEvent frameEvent, int playerIndex)
        {
            if (frameEvent == CueStrike.Characters.CueStrikeMascotManager.FrameEvent.MatchEnd)
            {
                TriggerReaction(ReactionType.Victory);
            }
        }

        /// <summary>
        /// Handle milestone events
        /// </summary>
        public void OnMilestone(CueStrike.Characters.CueStrikeMascotManager.MilestoneType milestone, int playerIndex, int value)
        {
            TriggerReaction(ReactionType.Clutch);
        }

        /// <summary>
        /// Handle foul events
        /// </summary>
        public void OnFoul(CueStrike.Characters.CueStrikeMascotManager.FoulType foulType, string description)
        {
            TriggerReaction(ReactionType.Foul);
        }

        /// <summary>
        /// Handle break events
        /// </summary>
        public void OnBreakEvent(CueStrike.Characters.CueStrikeMascotManager.BreakEvent breakEvent, int playerIndex)
        {
            if (breakEvent == CueStrike.Characters.CueStrikeMascotManager.BreakEvent.BreakAndRun)
            {
                TriggerReaction(ReactionType.Clutch);
            }
        }

        /// <summary>
        /// Get a message string for the reaction type
        /// </summary>
        private string GetReactionMessage(ReactionType type)
        {
            switch (type)
            {
                case ReactionType.Victory: return "BOOYAH! WE DID IT!";
                case ReactionType.Clutch:  return "OOOHHH! CLUTCH!";
                case ReactionType.SinglePot: return "NICE POT! KEEP GOING!";
                case ReactionType.Miss:    return "Aww... next time!";
                case ReactionType.Foul:    return "OH NO! FOUL!";
                default: return "Let's go!";
            }
        }

        /// <summary>
        /// Reset reaction counters
        /// </summary>
        public void ResetCounters()
        {
            Debug.Log("[BoPanda] Counters reset.");
        }
    }
}