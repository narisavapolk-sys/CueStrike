using UnityEngine;
using System.Collections.Generic;

namespace CueStrike.AI
{
    public interface ICueStrikeAIStrategy
    {
        void Initialize(CueStrikeAIController.AIParameters parameters);
        CueStrikeAIController.ShotPlan? SelectShot(CueStrikeAIController.TableState tableState, CueStrikeAIController.AIParameters parameters);
    }

    public class CueStrikeAIEasy : ICueStrikeAIStrategy
    {
        private CueStrikeAIController.AIParameters _params;
        public void Initialize(CueStrikeAIController.AIParameters parameters) { _params = parameters; }
        public CueStrikeAIController.ShotPlan? SelectShot(CueStrikeAIController.TableState tableState, CueStrikeAIController.AIParameters parameters)
        {
            if (tableState.availableBalls.Count == 0) return null;
            var ball = tableState.availableBalls[0];
            float nearest = float.MaxValue;
            Vector3 cuePos = tableState.cueBallPosition;
            foreach (var b in tableState.availableBalls)
            {
                float d = Vector3.Distance(cuePos, b.position);
                if (d < nearest) { nearest = d; ball = b; }
            }
            return new CueStrikeAIController.ShotPlan
            {
                ballId = ball.id,
                targetPosition = ball.position + new Vector3(Random.Range(-0.2f, 0.2f), 0, Random.Range(-0.2f, 0.2f)),
                pocketIndex = Random.Range(0, 6),
                power = Random.Range(0.3f, 0.7f),
                spin = Vector3.zero,
                isSafe = false
            };
        }
    }

    public class CueStrikeAIMedium : ICueStrikeAIStrategy
    {
        private CueStrikeAIController.AIParameters _params;
        public void Initialize(CueStrikeAIController.AIParameters parameters) { _params = parameters; }
        public CueStrikeAIController.ShotPlan? SelectShot(CueStrikeAIController.TableState tableState, CueStrikeAIController.AIParameters parameters)
        {
            if (tableState.availableBalls.Count == 0) return null;
            var sorted = new List<CueStrikeAIController.BallEntry>(tableState.availableBalls);
            Vector3 cuePos = tableState.cueBallPosition;
            sorted.Sort((a, b) => Vector3.Distance(cuePos, a.position).CompareTo(Vector3.Distance(cuePos, b.position)));
            var ball = sorted[0];
            Vector3 pocketDir = (AIPocketHelper.GetPocketPosition(2) - ball.position).normalized;
            return new CueStrikeAIController.ShotPlan
            {
                ballId = ball.id,
                targetPosition = ball.position + pocketDir * 0.5f + new Vector3(Random.Range(-0.1f, 0.1f), 0, Random.Range(-0.1f, 0.1f)),
                pocketIndex = 2,
                power = Random.Range(0.5f, 0.8f),
                spin = Vector3.zero,
                isSafe = false
            };
        }
    }

    public class CueStrikeAIHard : ICueStrikeAIStrategy
    {
        private CueStrikeAIController.AIParameters _params;
        public void Initialize(CueStrikeAIController.AIParameters parameters) { _params = parameters; }
        public CueStrikeAIController.ShotPlan? SelectShot(CueStrikeAIController.TableState tableState, CueStrikeAIController.AIParameters parameters)
        {
            if (tableState.availableBalls.Count == 0) return null;
            var sorted = new List<CueStrikeAIController.BallEntry>(tableState.availableBalls);
            Vector3 cuePos = tableState.cueBallPosition;
            sorted.Sort((a, b) =>
            {
                float distA = Vector3.Distance(cuePos, a.position);
                float distB = Vector3.Distance(cuePos, b.position);
                float scoreA = distA - Vector3.Distance(a.position, AIPocketHelper.GetPocketPosition(2)) * 0.3f;
                float scoreB = distB - Vector3.Distance(b.position, AIPocketHelper.GetPocketPosition(2)) * 0.3f;
                return scoreA.CompareTo(scoreB);
            });
            var ball = sorted[0];
            int bestPocket = AIPocketHelper.GetBestPocket(ball.position);
            Vector3 pocketDir = (AIPocketHelper.GetPocketPosition(bestPocket) - ball.position).normalized;
            return new CueStrikeAIController.ShotPlan
            {
                ballId = ball.id,
                targetPosition = ball.position + pocketDir * 0.3f,
                pocketIndex = bestPocket,
                power = 0.75f,
                spin = Vector3.zero,
                isSafe = false
            };
        }
    }

    public class CueStrikeAIExpert : ICueStrikeAIStrategy
    {
        private CueStrikeAIController.AIParameters _params;
        public void Initialize(CueStrikeAIController.AIParameters parameters) { _params = parameters; }
        public CueStrikeAIController.ShotPlan? SelectShot(CueStrikeAIController.TableState tableState, CueStrikeAIController.AIParameters parameters)
        {
            if (tableState.availableBalls.Count == 0) return null;
            var sorted = new List<CueStrikeAIController.BallEntry>(tableState.availableBalls);
            Vector3 cuePos = tableState.cueBallPosition;
            sorted.Sort((a, b) =>
            {
                float scoreA = Vector3.Distance(cuePos, a.position) - Vector3.Distance(a.position, AIPocketHelper.GetPocketPosition(2)) * 0.5f;
                float scoreB = Vector3.Distance(cuePos, b.position) - Vector3.Distance(b.position, AIPocketHelper.GetPocketPosition(2)) * 0.5f;
                return scoreA.CompareTo(scoreB);
            });
            var ball = sorted[0];
            int bestPocket = AIPocketHelper.GetBestPocket(ball.position);
            Vector3 pocketDir = (AIPocketHelper.GetPocketPosition(bestPocket) - ball.position).normalized;
            return new CueStrikeAIController.ShotPlan
            {
                ballId = ball.id,
                targetPosition = ball.position + pocketDir * 0.2f,
                pocketIndex = bestPocket,
                power = 0.85f,
                spin = Vector3.zero,
                isSafe = false
            };
        }
    }

    public static class AIPocketHelper
    {
        public static Vector3 GetPocketPosition(int index)
        {
            float tableHalfX = 0.914f / 2f;
            float tableHalfZ = 1.828f / 2f;
            switch (index)
            {
                case 0: return new Vector3(-tableHalfX, 0, -tableHalfZ);
                case 1: return new Vector3(-tableHalfX, 0, 0);
                case 2: return new Vector3(-tableHalfX, 0, tableHalfZ);
                case 3: return new Vector3(tableHalfX, 0, -tableHalfZ);
                case 4: return new Vector3(tableHalfX, 0, 0);
                case 5: return new Vector3(tableHalfX, 0, tableHalfZ);
                default: return Vector3.zero;
            }
        }

        public static int GetBestPocket(Vector3 ballPosition)
        {
            int best = 0;
            float bestDist = float.MaxValue;
            for (int i = 0; i < 6; i++)
            {
                float d = Vector3.Distance(ballPosition, GetPocketPosition(i));
                if (d < bestDist) { bestDist = d; best = i; }
            }
            return best;
        }
    }
}