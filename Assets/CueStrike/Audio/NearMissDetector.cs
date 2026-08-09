using UnityEngine;
using System.Collections;

namespace CueStrike.Audio
{
    public class NearMissDetector : MonoBehaviour
    {
        // This script can be attached to a main game object (e.g., the table or a camera)
        // It detects when a shot is very close to a pocket but doesn't go in, triggering a "near miss" sound.

        public float detectionRadius = 1.5f; // How close a ball needs to be to a pocket to trigger a near miss
        public float velocityThreshold = 1f; // Minimum velocity for a ball to be considered "shot"
        public float minTimeInDetectionZone = 0.2f; // How long a ball needs to be near a pocket to count as near miss

        private Collider[] pockets; // Array to hold references to pocket trigger colliders
        private Vector3[] pocketPositions; // Array to hold world positions of pockets

        void Start()
        {
            // Find all pocket trigger colliders in the scene
            // You might want to tag your pockets with "Pocket" or a similar tag
            GameObject[] pocketObjects = GameObject.FindGameObjectsWithTag("Pocket");
            pockets = new Collider[pocketObjects.Length];
            pocketPositions = new Vector3[pocketObjects.Length];

            for (int i = 0; i < pocketObjects.Length; i++)
            {
                pockets[i] = pocketObjects[i].GetComponent<Collider>();
                pocketPositions[i] = pocketObjects[i].transform.position;
            }

            if (pockets.Length == 0)
            {
                Debug.LogWarning("NearMissDetector: No game objects with tag 'Pocket' found. Near miss detection might not work.");
            }
        }

        void Update()
        {
            // Iterate through all active billiard balls in the scene
            // (Assuming balls have a "Ball" tag or a specific component)
            GameObject[] balls = GameObject.FindGameObjectsWithTag("Ball");
            foreach (GameObject ball in balls)
            {
                Rigidbody ballRigidbody = ball.GetComponent<Rigidbody>();
                if (ballRigidbody != null && ballRigidbody.linearVelocity.magnitude > velocityThreshold)
                {
                    CheckForNearMiss(ball.transform.position, ballRigidbody.linearVelocity);
                }
            }
        }

        void CheckForNearMiss(Vector3 ballPosition, Vector3 ballVelocity)
        {
            for (int i = 0; i < pockets.Length; i++)
            {
                Vector3 pocketPos = pocketPositions[i];
                float distance = Vector3.Distance(ballPosition, pocketPos);

                if (distance < detectionRadius)
                {
                    // Ball is within near miss detection radius
                    // You might want to add more sophisticated logic here,
                    // e.g., tracking how long the ball stays in the zone, or predicting trajectory.
                    StartCoroutine(DetectNearMissCoroutine(ballPosition, pocketPos));
                    return; // Only trigger one near miss per ball per frame
                }
            }
        }

        IEnumerator DetectNearMissCoroutine(Vector3 ballStartPosition, Vector3 pocketTargetPosition)
        {
            float timer = 0f;
            Vector3 lastBallPosition = ballStartPosition;

            while (timer < minTimeInDetectionZone)
            {
                // Check if the ball is still moving and near the pocket
                // This is a simplified check, more robust collision prediction might be needed for a real game.
                float currentDistance = Vector3.Distance(lastBallPosition, pocketTargetPosition);
                if (currentDistance > detectionRadius + 0.1f) // +0.1f buffer
                {
                    yield break; // Ball moved away from pocket
                }

                // If ball stopped or pocketed, it's not a near miss
                Rigidbody ballRigidbody = FindObjectOfType<Rigidbody>(); // This might be too broad, consider passing ball reference
                if (ballRigidbody != null && ballRigidbody.linearVelocity.magnitude < 0.1f)
                {
                    yield break; // Ball stopped
                }

                // A more robust check would involve checking if the ball actually entered the pocket
                // For now, we assume if it's near and moving, it might be a near miss.
                // A PocketSoundDetector on the pocket itself would handle actual pocketing.

                timer += Time.deltaTime;
                yield return null;
            }

            // If we reached here, the ball was near the pocket for long enough and didn't pocket.
            if (CueStrikeAudioManager.Instance != null)
            {
                CueStrikeAudioManager.Instance.PlayNearMissGasp(pocketTargetPosition);
                Debug.Log("Near miss detected!");
            }
        }
    }
}