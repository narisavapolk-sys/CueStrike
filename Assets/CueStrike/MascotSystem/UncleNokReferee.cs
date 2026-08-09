using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

namespace CueStrike.MascotSystem
{
    /// <summary>
    /// AI Referee - Uncle Nok (Elephant Mascot)
    /// Handles score announcements, foul calls, and match commentary.
    /// </summary>
    public class UncleNokReferee : MonoBehaviour
    {
        [Header("Components")]
        [SerializeField] private Animator _animator;
        [SerializeField] private AudioSource _audioSource;
        [SerializeField] private Transform _homePosition;

        [Header("Voice Clips - Announcements")]
        [SerializeField] private AudioClip[] _frameStartClips;
        [SerializeField] private AudioClip[] _frameEndClips;
        [SerializeField] private AudioClip[] _matchStartClips;
        [SerializeField] private AudioClip[] _matchEndClips;
        [SerializeField] private AudioClip[] _playerTurnStartClips;
        [SerializeField] private AudioClip[] _playerTurnEndClips;

        [Header("Voice Clips - Scoring")]
        [SerializeField] private AudioClip[] _potSuccessClips;
        [SerializeField] private AudioClip[] _centuryBreakClips;
        [SerializeField] private AudioClip[] _highBreakClips;
        [SerializeField] private AudioClip[] _maximumBreakClips;
        [SerializeField] private AudioClip[] _clearanceClips;
        [SerializeField] private AudioClip[] _breakClips;

        [Header("Voice Clips - Fouls")]
        [SerializeField] private AudioClip[] _foulCalledClips;
        [SerializeField] private AudioClip[] _foulCueBallPottedClips;
        [SerializeField] private AudioClip[] _foulNoBallContactedClips;
        [SerializeField] private AudioClip[] _foulWrongBallFirstClips;
        [SerializeField] private AudioClip[] _foulNoCushionClips;
        [SerializeField] private AudioClip[] _foulBallOffTableClips;

        [Header("Voice Clips - Special Events")]
        [SerializeField] private AudioClip[] _snookerEscapeClips;
        [SerializeField] private AudioClip[] _flukeClips;
        [SerializeField] private AudioClip[] _safetyPlayedClips;

        [Header("Animation Triggers")]
        [SerializeField] private string _idleTrigger = "Idle";
        [SerializeField] private string _speakTrigger = "Speak";
        [SerializeField] private string _announceTrigger = "Announce";
        [SerializeField] private string _celebrateTrigger = "Celebrate";
        [SerializeField] private string _disapproveTrigger = "Disapprove";
        [SerializeField] private string _thinkingTrigger = "Thinking";

        [Header("Settings")]
        [SerializeField] private float _minTimeBetweenAnnouncements = 3.0f;
        [SerializeField] private bool _enableVoice = true;
        [SerializeField] private bool _enableAnimations = true;

        private float _lastAnnouncementTime = -10f;
        private int _currentFrame = 0;
        private int _player1Score = 0;
        private int _player2Score = 0;
        private int _currentBreak = 0;
        private int _highestBreak = 0;

        public bool EnableVoice
        {
            get => _enableVoice;
            set => _enableVoice = value;
        }

        public bool EnableAnimations
        {
            get => _enableAnimations;
            set => _enableAnimations = value;
        }

        private void Awake()
        {
            if (_animator == null)
            {
                _animator = GetComponent<Animator>();
            }
            if (_audioSource == null)
            {
                _audioSource = GetComponent<AudioSource>();
            }
        }

        private void Start()
        {
            if (_homePosition != null)
            {
                transform.position = _homePosition.position;
                transform.rotation = _homePosition.rotation;
            }

            SetIdleAnimation(true);
            Debug.Log("[Uncle Nok] AI Referee initialized and ready for duty.");
        }

        private void Update()
        {
            if (_homePosition != null)
            {
                Vector3 lookDirection = _homePosition.position - transform.position;
                lookDirection.y = 0;
                if (lookDirection != Vector3.zero)
                {
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDirection), Time.deltaTime * 2f);
                }
            }
        }

        private void SetIdleAnimation(bool isIdle)
        {
            if (_animator != null && _enableAnimations)
            {
                _animator.SetBool("IsIdle", isIdle);
            }
        }

        private bool CanAnnounce()
        {
            return Time.time - _lastAnnouncementTime >= _minTimeBetweenAnnouncements;
        }

        private void PlayRandomClip(AudioClip[] clips)
        {
            if (!_enableVoice || _audioSource == null || clips == null || clips.Length == 0)
            {
                return;
            }

            AudioClip clip = clips[Random.Range(0, clips.Length)];
            _audioSource.PlayOneShot(clip);
            _lastAnnouncementTime = Time.time;
        }

        private void TriggerAnimation(string triggerName)
        {
            if (_animator != null && _enableAnimations && !string.IsNullOrEmpty(triggerName))
            {
                _animator.SetTrigger(triggerName);
            }
        }

        // ============================================================
        // Public API for Game Systems
        // ============================================================

        /// <summary>
        /// Call when a new frame starts.
        /// </summary>
        public void OnFrameStart(int frameNumber)
        {
            _currentFrame = frameNumber;
            _player1Score = 0;
            _player2Score = 0;
            _currentBreak = 0;

            if (!CanAnnounce()) return;

            PlayRandomClip(_frameStartClips);
            TriggerAnimation(_announceTrigger);
        }

        /// <summary>
        /// Call when a frame ends.
        /// </summary>
        public void OnFrameEnd(int winnerPlayerIndex)
        {
            if (!CanAnnounce()) return;

            PlayRandomClip(_frameEndClips);
            TriggerAnimation(_announceTrigger);
        }

        /// <summary>
        /// Call when a match starts.
        /// </summary>
        public void OnMatchStart()
        {
            if (!CanAnnounce()) return;

            PlayRandomClip(_matchStartClips);
            TriggerAnimation(_announceTrigger);
        }

        /// <summary>
        /// Call when a match ends.
        /// </summary>
        public void OnMatchEnd(int winnerPlayerIndex)
        {
            if (!CanAnnounce()) return;

            PlayRandomClip(_matchEndClips);
            TriggerAnimation(_celebrateTrigger);
        }

        /// <summary>
        /// Call when a player's turn starts.
        /// </summary>
        public void OnPlayerTurnStart(int playerIndex)
        {
            if (!CanAnnounce()) return;

            PlayRandomClip(_playerTurnStartClips);
            TriggerAnimation(_speakTrigger);
        }

        /// <summary>
        /// Call when a player's turn ends.
        /// </summary>
        public void OnPlayerTurnEnd(int playerIndex)
        {
            if (!CanAnnounce()) return;

            _currentBreak = 0;
            PlayRandomClip(_playerTurnEndClips);
            TriggerAnimation(_speakTrigger);
        }

        /// <summary>
        /// Call when a ball is successfully potted.
        /// </summary>
        public void OnBallPotted(int playerIndex, int pointsScored, int ballsPotted)
        {
            if (!CanAnnounce()) return;

            _currentBreak += pointsScored;
            if (_currentBreak > _highestBreak)
            {
                _highestBreak = _currentBreak;
            }

            if (playerIndex == 0)
            {
                _player1Score += pointsScored;
            }
            else
            {
                _player2Score += pointsScored;
            }

            // Check for milestones
            if (_currentBreak >= 147)
            {
                PlayRandomClip(_maximumBreakClips);
                TriggerAnimation(_celebrateTrigger);
            }
            else if (_currentBreak >= 100)
            {
                PlayRandomClip(_centuryBreakClips);
                TriggerAnimation(_celebrateTrigger);
            }
            else if (_currentBreak >= 50)
            {
                PlayRandomClip(_highBreakClips);
                TriggerAnimation(_celebrateTrigger);
            }
            else
            {
                PlayRandomClip(_potSuccessClips);
                TriggerAnimation(_speakTrigger);
            }
        }

        /// <summary>
        /// Call when a foul is committed.
        /// </summary>
        public void OnFoulCommitted(FoulType foulType, int playerIndex, int penaltyPoints)
        {
            if (!CanAnnounce()) return;

            _currentBreak = 0;

            PlayRandomClip(_foulCalledClips);
            TriggerAnimation(_disapproveTrigger);

            // Foul-specific announcements
            switch (foulType)
            {
                case FoulType.CueBallPotted:
                    PlayRandomClip(_foulCueBallPottedClips);
                    break;
                case FoulType.NoBallContacted:
                    PlayRandomClip(_foulNoBallContactedClips);
                    break;
                case FoulType.WrongBallFirst:
                    PlayRandomClip(_foulWrongBallFirstClips);
                    break;
                case FoulType.NoCushionAfterContact:
                    PlayRandomClip(_foulNoCushionClips);
                    break;
                case FoulType.BallOffTable:
                    PlayRandomClip(_foulBallOffTableClips);
                    break;
            }
        }

        /// <summary>
        /// Call when a snooker is escaped.
        /// </summary>
        public void OnSnookerEscaped()
        {
            if (!CanAnnounce()) return;

            PlayRandomClip(_snookerEscapeClips);
            TriggerAnimation(_celebrateTrigger);
        }

        /// <summary>
        /// Call when a fluke occurs.
        /// </summary>
        public void OnFluke()
        {
            if (!CanAnnounce()) return;

            PlayRandomClip(_flukeClips);
            TriggerAnimation(_speakTrigger);
        }

        /// <summary>
        /// Call when a safety shot is played.
        /// </summary>
        public void OnSafetyPlayed()
        {
            if (!CanAnnounce()) return;

            PlayRandomClip(_safetyPlayedClips);
            TriggerAnimation(_thinkingTrigger);
        }

        /// <summary>
        /// Call when a break shot occurs.
        /// </summary>
        public void OnBreakShot()
        {
            if (!CanAnnounce()) return;

            PlayRandomClip(_breakClips);
            TriggerAnimation(_announceTrigger);
        }

        /// <summary>
        /// Call when a clearance is made.
        /// </summary>
        public void OnClearance(int totalPoints)
        {
            if (!CanAnnounce()) return;

            PlayRandomClip(_clearanceClips);
            TriggerAnimation(_celebrateTrigger);
        }

        // ============================================================
        // Foul Type Enum
        // ============================================================

        /// <summary>
        /// Types of fouls for specific announcements.
        /// </summary>
        public enum FoulType
        {
            Generic = 0,
            CueBallPotted = 1,
            NoBallContacted = 2,
            WrongBallFirst = 3,
            NoCushionAfterContact = 4,
            BallOffTable = 5,
            DoubleHit = 6,
            PushShot = 7,
            Miscue = 8
        }
    }
}