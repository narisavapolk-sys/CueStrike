using UnityEngine;

namespace CueStrike.MascotSystem
{
    /// <summary>
    /// R40 — Bo Panda เป็นกรรมการ (Random voice)
    /// ลอก UncleNokReferee pattern: PlayRandomClip + cooldown + animation triggers
    /// ใช้เสียงน้องโบ 14 คลิป (match_start/turn_start/pot_success/century/high_break/
    /// clearance/break_shot/foul_called/foul_cueball)
    /// </summary>
    public class BoReferee : MonoBehaviour
    {
        public enum FoulType
        {
            Generic,
            CueBallPotted,
            NoBallContacted,
            WrongBallFirst,
            NoCushionAfterContact,
            BallOffTable,
            DoubleHit,
            PushShot,
            Miscue
        }

        [Header("Components")]
        [SerializeField] private Animator _animator;
        [SerializeField] private AudioSource _audioSource;
        [SerializeField] private Transform _homePosition;

        [Header("Voice Clips - Announcements")]
        [SerializeField] private AudioClip[] _frameStartClips;
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

        [Header("Animation Triggers")]
        [SerializeField] private string _idleTrigger = "Idle";
        [SerializeField] private string _speakTrigger = "Speak";
        [SerializeField] private string _announceTrigger = "Speak";
        [SerializeField] private string _celebrateTrigger = "Celebrate";
        [SerializeField] private string _disapproveTrigger = "Disappointed";
        [SerializeField] private string _thinkingTrigger = "Speak";

        [Header("Settings")]
        [SerializeField] private float _minTimeBetweenAnnouncements = 3.0f;
        [SerializeField] private bool _enableVoice = true;
        [SerializeField] private bool _enableAnimations = true;

        private float _lastAnnouncementTime = -10f;
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
            Debug.Log("[Bo Referee] Bo Panda referee initialized and ready for duty.");
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
        // Public API for Game Systems (ลอก UncleNokReferee)
        // ============================================================

        /// <summary>เริ่มเฟรมใหม่</summary>
        public void OnFrameStart(int frameNumber)
        {
            _currentBreak = 0;
            if (!CanAnnounce()) return;
            PlayRandomClip(_frameStartClips);
            TriggerAnimation(_announceTrigger);
        }

        /// <summary>จบเฟรม</summary>
        public void OnFrameEnd(int winnerPlayerIndex)
        {
            if (!CanAnnounce()) return;
            PlayRandomClip(_matchEndClips);
            TriggerAnimation(_celebrateTrigger);
        }

        /// <summary>เริ่มแมตช์</summary>
        public void OnMatchStart()
        {
            if (!CanAnnounce()) return;
            PlayRandomClip(_matchStartClips);
            TriggerAnimation(_announceTrigger);
        }

        /// <summary>จบแมตช์</summary>
        public void OnMatchEnd(int winnerPlayerIndex)
        {
            if (!CanAnnounce()) return;
            PlayRandomClip(_matchEndClips);
            TriggerAnimation(_celebrateTrigger);
        }

        /// <summary>เริ่มตาผู้เล่น</summary>
        public void OnPlayerTurnStart(int playerIndex)
        {
            if (!CanAnnounce()) return;
            PlayRandomClip(_playerTurnStartClips);
            TriggerAnimation(_speakTrigger);
        }

        /// <summary>จบตาผู้เล่น</summary>
        public void OnPlayerTurnEnd(int playerIndex)
        {
            if (!CanAnnounce()) return;
            _currentBreak = 0;
            PlayRandomClip(_playerTurnEndClips);
            TriggerAnimation(_speakTrigger);
        }

        /// <summary>ลูกเข้าหลุม</summary>
        public void OnBallPotted(int playerIndex, int pointsScored, int ballsPotted)
        {
            if (!CanAnnounce()) return;

            _currentBreak += pointsScored;
            if (_currentBreak > _highestBreak)
            {
                _highestBreak = _currentBreak;
            }

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

        /// <summary>ฟาวล์</summary>
        public void OnFoulCommitted(FoulType foulType, int playerIndex, int penaltyPoints)
        {
            if (!CanAnnounce()) return;
            _currentBreak = 0;
            PlayRandomClip(_foulCalledClips);
            TriggerAnimation(_disapproveTrigger);

            if (foulType == FoulType.CueBallPotted)
            {
                PlayRandomClip(_foulCueBallPottedClips);
            }
        }

        /// <summary>เบรกช็อต</summary>
        public void OnBreakShot()
        {
            if (!CanAnnounce()) return;
            PlayRandomClip(_breakClips);
            TriggerAnimation(_speakTrigger);
        }
    }
}
