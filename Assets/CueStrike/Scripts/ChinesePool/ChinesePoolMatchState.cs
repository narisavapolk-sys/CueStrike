using UnityEngine;

namespace CueStrike.Gameplay.ChinesePool
{
    /// <summary>
    /// Match state enum for Chinese 8-Ball. Used by ChinesePoolCallShotUI and other subsystems.
    /// </summary>
    public enum ChinesePoolMatchState
    {
        Waiting,
        Break,
        OpenTable,
        RedAssigned,
        YellowAssigned,
        Playing,
        FrameOver,
        MatchOver
    }
}