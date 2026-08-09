using System.Collections.Generic;
using UnityEngine;
using CueStrike.Data;

namespace CueStrike.Replay
{
    public class GhostReplayRecorder : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private CueStrikeShotManager shotManager;
        [SerializeField] private CueStrikePhysicsManager physicsManager;
        [SerializeField] private Transform cueStick;

        [Header("Settings")]
        [SerializeField] private float sampleRate = 0.05f; // ทุก 0.05 วินาที

        private bool isRecording = false;
        private float nextSampleTime = 0f;
        private GhostReplayData currentReplay;
        private float shotStartTime;

        private void OnEnable()
        {
            if (shotManager != null)
            {
                shotManager.OnShotStart += OnShotStarted;
                shotManager.OnShotEnd += OnShotEnded;
            }
        }

        private void OnDisable()
        {
            if (shotManager != null)
            {
                shotManager.OnShotStart -= OnShotStarted;
                shotManager.OnShotEnd -= OnShotEnded;
            }
        }

        private void OnShotStarted()
        {
            isRecording = true;
            shotStartTime = Time.time;
            nextSampleTime = Time.time;

            currentReplay = new GhostReplayData
            {
                replayName = "Shot_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss"),
                dateSaved = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
                ballFrames = new List<BallFrameData>(),
                cueFrames = new List<CueFrameData>(),
                pocketedBallIds = new List<int>(),
                score = 0
            };
        }

        private void FixedUpdate()
        {
            if (!isRecording) return;
            if (Time.time < nextSampleTime) return;

            nextSampleTime += sampleRate;

            // บันทึกตำแหน่งลูกทุกลูก
            int ballCount = physicsManager != null ? physicsManager.GetBallCount() : 16;
            for (int i = 0; i < ballCount; i++)
            {
                var ball = physicsManager?.GetBallById(i);
                if (ball == null) continue;

                currentReplay.ballFrames.Add(new BallFrameData
                {
                    ballId = i,
                    position = ball.position,
                    rotation = ball.rotation,
                    isPocketed = !ball.gameObject.activeInHierarchy // ถ้าลูกถูก disable = หลุม
                });
            }

            // บันทึกตำแหน่งไม้คิว
            if (cueStick != null)
            {
                currentReplay.cueFrames.Add(new CueFrameData
                {
                    position = cueStick.position,
                    rotation = cueStick.rotation
                });
            }
        }

        private void OnShotEnded()
        {
            isRecording = false;
            currentReplay.shotDuration = Time.time - shotStartTime;
            
            // ตรวจสอบลูกที่หลุม (จาก active state)
            int ballCount = physicsManager != null ? physicsManager.GetBallCount() : 16;
            for (int i = 0; i < ballCount; i++)
            {
                var ball = physicsManager?.GetBallById(i);
                if (ball != null && !ball.gameObject.activeInHierarchy)
                {
                    currentReplay.pocketedBallIds.Add(i);
                }
            }

            Debug.Log($"[GhostReplay] Recorded shot: {currentReplay.replayName}, " +
                      $"Duration: {currentReplay.shotDuration:F2}s, " +
                      $"Frames: {currentReplay.ballFrames.Count / Mathf.Max(1, ballCount)}");
        }

        public GhostReplayData GetLastReplay()
        {
            return currentReplay;
        }

        public bool HasRecording()
        {
            return currentReplay != null && currentReplay.ballFrames.Count > 0;
        }
    }
}