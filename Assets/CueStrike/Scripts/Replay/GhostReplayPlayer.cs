using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CueStrike.Data;

namespace CueStrike.Replay
{
    public class GhostReplayPlayer : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private Material ghostMaterial; // ต้องสร้างหรือ assign ทีหลัง
        [SerializeField] private float playbackSpeed = 1f;

        [Header("References")]
        [SerializeField] private Transform cueStick;

        private bool isPlaying = false;
        private GhostReplayData currentData;
        private List<GameObject> ghostBalls = new List<GameObject>();
        private GameObject ghostCue;
        private Coroutine playbackCoroutine;

        public event System.Action OnReplayStarted;
        public event System.Action OnReplayEnded;

        public void PlayReplay(GhostReplayData data)
        {
            if (data == null || data.ballFrames.Count == 0)
            {
                Debug.LogWarning("[GhostReplay] No data to play");
                return;
            }

            StopReplay();
            currentData = data;
            StartCoroutine(PlaybackCoroutine());
        }

        public void StopReplay()
        {
            if (playbackCoroutine != null)
            {
                StopCoroutine(playbackCoroutine);
                playbackCoroutine = null;
            }
            isPlaying = false;
            DestroyGhostObjects();
            OnReplayEnded?.Invoke();
        }

        public void SetPlaybackSpeed(float speed)
        {
            playbackSpeed = Mathf.Clamp(speed, 0.25f, 2f);
        }

        private IEnumerator PlaybackCoroutine()
        {
            isPlaying = true;
            OnReplayStarted?.Invoke();

            // สร้าง ghost objects
            CreateGhostObjects();

            // คำนวณจำนวน frame ต่อลูก
            int ballsCount = 16; // 0 = cue ball, 1-15 = red balls
            int totalFrames = currentData.ballFrames.Count / ballsCount;
            if (totalFrames == 0) totalFrames = 1;
            float frameDuration = currentData.shotDuration / totalFrames;

            for (int frame = 0; frame < totalFrames; frame++)
            {
                // อัปเดตตำแหน่งลูกทุกลูก
                for (int b = 0; b < ballsCount; b++)
                {
                    int dataIndex = frame * ballsCount + b;
                    if (dataIndex >= currentData.ballFrames.Count) break;

                    var frameData = currentData.ballFrames[dataIndex];
                    if (b < ghostBalls.Count && ghostBalls[b] != null)
                    {
                        ghostBalls[b].transform.position = frameData.position;
                        ghostBalls[b].transform.rotation = frameData.rotation;
                        ghostBalls[b].SetActive(!frameData.isPocketed);
                    }
                }

                // อัปเดตตำแหน่งไม้คิว
                if (frame < currentData.cueFrames.Count && ghostCue != null)
                {
                    ghostCue.transform.position = currentData.cueFrames[frame].position;
                    ghostCue.transform.rotation = currentData.cueFrames[frame].rotation;
                }

                yield return new WaitForSeconds(frameDuration / playbackSpeed);
            }

            isPlaying = false;
            OnReplayEnded?.Invoke();
        }

        private void CreateGhostObjects()
        {
            DestroyGhostObjects();

            // สร้าง ghost ball 16 ลูก
            for (int i = 0; i < 16; i++)
            {
                var ghost = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                ghost.name = $"GhostBall_{i}";
                Destroy(ghost.GetComponent<Collider>()); // ไม่มี collision
                ghostBalls.Add(ghost);

                if (ghostMaterial != null)
                {
                    var renderer = ghost.GetComponent<Renderer>();
                    if (renderer != null) renderer.material = ghostMaterial;
                }
            }

            // สร้าง ghost cue
            if (cueStick != null)
            {
                ghostCue = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                ghostCue.name = "GhostCue";
                Destroy(ghostCue.GetComponent<Collider>());
                
                if (ghostMaterial != null)
                {
                    var renderer = ghostCue.GetComponent<Renderer>();
                    if (renderer != null) renderer.material = ghostMaterial;
                }
            }
        }

        private void DestroyGhostObjects()
        {
            foreach (var ball in ghostBalls)
            {
                if (ball != null) Destroy(ball);
            }
            ghostBalls.Clear();

            if (ghostCue != null) Destroy(ghostCue);
        }

        private void OnDestroy()
        {
            DestroyGhostObjects();
        }

        public bool IsPlaying()
        {
            return isPlaying;
        }
    }
}