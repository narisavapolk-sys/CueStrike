using System;
using System.Collections.Generic;
using UnityEngine;

namespace CueStrike.Data
{
    [Serializable]
    public class BallFrameData
    {
        public int ballId;
        public Vector3 position;
        public Quaternion rotation;
        public bool isPocketed;
    }

    [Serializable]
    public class CueFrameData
    {
        public Vector3 position;
        public Quaternion rotation;
    }

    [Serializable]
    public class GhostReplayData
    {
        public string replayName;
        public string dateSaved;
        public float shotDuration;
        public List<BallFrameData> ballFrames;
        public List<CueFrameData> cueFrames;
        public List<int> pocketedBallIds;
        public int score;
    }
}