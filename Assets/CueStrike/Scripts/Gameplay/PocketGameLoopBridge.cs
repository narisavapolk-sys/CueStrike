using UnityEngine;
using CueStrike.Gameplay.ChinesePool;
using CueStrike.MascotSystem;

namespace CueStrike.Gameplay
{
    /// <summary>
    /// R44 — turns pocket detection into real game flow.
    /// BallPottedTracker -> GameManager.ProcessPottedBall -> score/turn rules,
    /// and optionally announces the pot through BoReferee.
    /// </summary>
    public sealed class PocketGameLoopBridge : MonoBehaviour
    {
        [SerializeField] private BallPottedTracker _tracker;
        [SerializeField] private ChinesePoolGameManager _gameManager;
        [SerializeField] private BoReferee _boReferee;
        [SerializeField] private ChinesePoolBallSetup _ballSetup;
        private bool _subscribed;
        private bool _ballSetupSubscribed;
        private bool _trackingStarted;
        private float _nextResolve;

        private void Start()
        {
            ResolveReferences();
            Subscribe();
            SubscribeBallSetup();
            RefreshSpawnedBalls();
        }

        private void Update()
        {
            if (Time.time >= _nextResolve)
            {
                _nextResolve = Time.time + 0.5f;
                ResolveReferences();
                Subscribe();
                SubscribeBallSetup();
                RefreshSpawnedBalls();
            }
        }

        private void ResolveReferences()
        {
            if (_tracker == null) _tracker = FindAnyObjectByType<BallPottedTracker>();
            if (_gameManager == null) _gameManager = ChinesePoolGameManager.Instance ?? FindAnyObjectByType<ChinesePoolGameManager>();
            if (_boReferee == null) _boReferee = FindAnyObjectByType<BoReferee>();
            if (_ballSetup == null) _ballSetup = FindAnyObjectByType<ChinesePoolBallSetup>();
        }

        private void Subscribe()
        {
            if (_subscribed || _tracker == null) return;
            _tracker.OnBallPotted += HandleBallPotted;
            _subscribed = true;
            Debug.Log("[PocketGameLoop] Subscribed BallPottedTracker -> GameManager.");
        }

        private void SubscribeBallSetup()
        {
            if (_ballSetupSubscribed || _ballSetup == null) return;
            _ballSetup.BallsSpawned += HandleBallsSpawned;
            _ballSetupSubscribed = true;
            Debug.Log("[PocketGameLoop] Subscribed BallSetup.BallsSpawned -> tracker.");
        }

        private void HandleBallsSpawned(Transform[] transforms)
        {
            if (_tracker == null) return;
            _tracker.RegisterSpawnedBalls(transforms);
            if (!_trackingStarted)
            {
                _tracker.StartTracking();
                _trackingStarted = true;
            }
        }

        private void RefreshSpawnedBalls()
        {
            if (_tracker == null || _ballSetup == null) return;
            var transforms = new System.Collections.Generic.List<Transform>();
            for (int id = 1; id <= 15; id++)
            {
                var ball = _ballSetup.GetBallById(id);
                transforms.Add(ball != null ? ball.transform : null);
            }
            _tracker.SetBallTransforms(transforms.ToArray());
            if (!_subscribed || _trackingStarted) return;
            _tracker.StartTracking();
            _trackingStarted = true;
        }

        private void HandleBallPotted(int ballNumber, int playerNumber)
        {
            Debug.Log($"[PocketGameLoop] BallPotted ball={ballNumber}, player={playerNumber} -> processing shot result.");
            _gameManager?.ProcessPottedBall(ballNumber);
            _boReferee?.OnBallPotted(playerNumber, 1, 1);
        }

        private void OnDestroy()
        {
            if (_subscribed && _tracker != null) _tracker.OnBallPotted -= HandleBallPotted;
            if (_ballSetupSubscribed && _ballSetup != null) _ballSetup.BallsSpawned -= HandleBallsSpawned;
            _subscribed = false;
            _ballSetupSubscribed = false;
        }

        public bool IsWired() => _tracker != null && _gameManager != null && _ballSetup != null && _subscribed;
    }
}
