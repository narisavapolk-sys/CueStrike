using System;
using System.Collections.Generic;
using UnityEngine;

namespace CueStrike.NoirMemory
{
    public enum NoirMemoryTimer
    {
        FiveSeconds = 5,
        EightSeconds = 8
    }

    [Serializable]
    public class NoirMemoryPuzzleConfig
    {
        public NoirMemoryTimer revealDuration = NoirMemoryTimer.FiveSeconds;
        public bool enableAIMemory = true;
        public float aiMemoryAccuracy = 0.85f; // 0-1, higher = better AI memory
    }

    [Serializable]
    public class NoirMemoryPuzzleState
    {
        public bool isRevealPhase;      // Counting down for memorization
        public bool isNoirPhase;        // Balls are blacked out
        public bool isMemoryModeActive; // System enabled
        public float timerRemaining;
        public int currentPlayerIndex;  // Whose turn to shoot
    }
}