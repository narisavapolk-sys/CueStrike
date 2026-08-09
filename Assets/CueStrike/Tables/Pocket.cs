using UnityEngine;
using CueStrike;
using CueStrike.Audio;

[RequireComponent(typeof(Collider))]
public class Pocket : MonoBehaviour
{
    public int scoreValue = 1; // generic value, rules manager will interpret
    public enum PocketSide { Left, Right, Center }
    public PocketSide pocketSide = PocketSide.Center;

    void Awake()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true;
        gameObject.tag = "Pocket";
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Ball"))
        {
            var rb = other.attachedRigidbody;
            var idComp = other.GetComponent<BallIdentity>();
            int ballId = idComp != null ? idComp.ballId : -1;
            var rules    = CueStrikeRulesManager.Instance;
            var audioMgr = CueStrikeAudioManager.Instance;
            var fxMgr    = CueStrikeFXManager.Instance;

            fxMgr?.SpawnPocketGlow(transform.position);
            audioMgr?.PlayPocket();

            if (rules != null)
            {
                rules.BallPotted(ballId, pocketSide.ToString());

                // --- Potted Ball Tracker: record which player potted this ball ---
                if (ballId != 0) // ignore cue ball
                {
                    var tracker = CueStrike.Gameplay.CueStrikePottedBallTracker.Instance;
                    tracker?.RegisterPotted(ballId, rules.currentPlayer);
                }
                // -----------------------------------------------------------------
            }
            other.gameObject.SetActive(false);
        }
    }
}
