using UnityEngine;

namespace _Scripts.BreathGuideSystem
{
    public class BreathRhythmController : MonoBehaviour
    {
        [Header("Resonant Breathing Pattern")]
        [SerializeField] private float inhaleDuration = 4f;
        [SerializeField] private float exhaleDuration = 6f;

        [Header("References")]
        [SerializeField] private BeamSpawner beamSpawner;
        [SerializeField] private BreathPacer breathPacer;
        [SerializeField] private Transform spawnPoint;
        [SerializeField] private Transform playerPosition;
        
         private MovementType _inhaleMovement = MovementType.VerticalUp;
         private MovementType _exhaleMovement = MovementType.VerticalDown;

        public System.Action OnTargetCyclesReached;

        [Header("TrainingMode")]
        [SerializeField] private int targetFullBreaths = 0; 
        private int _fullBreathCount = 0;

        // State
        private float _currentPhaseTime;
        private bool _isInhaling = true;

        private bool _isRunning = false;          // System on/off
        private bool _isPaused = false;           // Paused time progression
        private bool _waitingForFirstBeam = false;

        // Calculated timing
        private float _spawnToPlayerDistance;
        private float _beamSpeed;
        private float _beamTravelTime;
        private float _currentPhaseDuration;

        // Events
        public System.Action<bool, float> OnPhaseChanged;
        public System.Action<float> OnProgressUpdated;
        public System.Action<int> OnFullBreathStarted;
        private int _fullBreathStartCount = 0;

        void Start()
        {
            CalculateTiming();
        }

        void Update()
        {
            if (!_isRunning || _isPaused || _waitingForFirstBeam) return;

            _currentPhaseTime += Time.deltaTime;

            // Update Pacer
            UpdatePacerProgress();

            // Phase Complete: Inhale -> Exhale or Exhale -> Inhale
            if (_currentPhaseTime >= _currentPhaseDuration)
            {
                SwitchPhase();
            }
        }

        #region Timing

        void CalculateTiming()
        {
            if (spawnPoint != null && playerPosition != null)
            {
                _spawnToPlayerDistance = Vector3.Distance(spawnPoint.position, playerPosition.position);
            }

            _beamSpeed = GetBeamSpeedFromPrefab();
            _beamTravelTime = (_beamSpeed <= 0.001f) ? 0f : _spawnToPlayerDistance / _beamSpeed;
        }
        float GetBeamSpeedFromPrefab()
        {
            if (beamSpawner == null) return 2f;

            GameObject prefab = beamSpawner.GetLeftBeamPrefab();
            if (prefab == null) return 2f;

            BeamSegment segment = prefab.GetComponent<BeamSegment>();
            return segment != null ? Mathf.Max(0.01f, segment.GetBeamFlowSpeed()) : 2f;
        }

        #endregion

        #region Rhythm Control

        public void StartBreathCycle()
        {
            if (_isRunning || beamSpawner == null) return;
            
            // Clean slate for scheduling
            CancelInvoke(nameof(OnFirstBeamArrival));
            CancelInvoke(nameof(SpawnNextBeam));
            
            // Reset States
            _isRunning = true;
            _isPaused = false;
            _waitingForFirstBeam = true;
            _currentPhaseTime = 0f;
            _fullBreathCount = 0;
            
            // Calculate timing
            CalculateTiming();
            // Call beamSpawner to spawn first inhale beam
            CallSpawning(true, inhaleDuration);
            // Wait for beam to arrive, then actually start cycle
            Invoke(nameof(OnFirstBeamArrival), _beamTravelTime);
        }
        void OnFirstBeamArrival()
        {
            if (!_isRunning) return;

            _waitingForFirstBeam = false;
            _isInhaling = true;
            _currentPhaseTime = 0f;
            _currentPhaseDuration = inhaleDuration;
            
            _fullBreathStartCount = 1;
            OnFullBreathStarted?.Invoke(_fullBreathStartCount);
            
            if (breathPacer != null)
            {
                breathPacer.SetBreathDuration(inhaleDuration);
                breathPacer.SetPhase(true, 0f);
            }
            ScheduleNextBeamSpawn();
            OnPhaseChanged?.Invoke(true, inhaleDuration);
        }

        // Switch after inhale/exhale phase complete : Inhale -> Exhale or Exhale -> Inhale
        void SwitchPhase()
        {
            _isInhaling = !_isInhaling;
            _currentPhaseTime = 0f;
            _currentPhaseDuration = _isInhaling ? inhaleDuration : exhaleDuration;

            if (_isInhaling)
            {
                _fullBreathCount++;
                
                _fullBreathStartCount++;
                OnFullBreathStarted?.Invoke(_fullBreathStartCount);

                // target breath cycle count reached
                if (targetFullBreaths > 0 && _fullBreathCount >= targetFullBreaths)
                {
                    CancelInvoke(nameof(SpawnNextBeam));
                    Stop();
                    OnTargetCyclesReached?.Invoke();
                    return;
                }
            }
            if (breathPacer != null)
            {
                breathPacer.SetBreathDuration(_currentPhaseDuration);
                breathPacer.SetPhase(_isInhaling, 0f);
            }

            ScheduleNextBeamSpawn();
            OnPhaseChanged?.Invoke(_isInhaling, _currentPhaseDuration);
        }
        void ScheduleNextBeamSpawn()
        {
            if (beamSpawner == null) return;

            // Prevent stacking multiple scheduled spawns
            CancelInvoke(nameof(SpawnNextBeam));

            float spawnDelay = _currentPhaseDuration - _beamTravelTime;
            if (spawnDelay < 0f)
            {
                spawnDelay = 0f;
            }
            Invoke(nameof(SpawnNextBeam), spawnDelay);
            
        }
        public void SpawnNextBeam()
        {
            if (!_isRunning) return;

            if (_isPaused)
            {
                Invoke(nameof(SpawnNextBeam), 0.1f);
                return;
            }

            bool nextIsInhaling = !_isInhaling;
            
            if (nextIsInhaling && targetFullBreaths > 0 && _fullBreathCount >= targetFullBreaths - 1)
            {
                return;
            }
            float nextPhaseDuration = nextIsInhaling ? inhaleDuration : exhaleDuration;
            CallSpawning(nextIsInhaling, nextPhaseDuration);
        }

        void CallSpawning(bool isInhaling, float phaseDuration)
        {
            if (beamSpawner == null) return;

            // Set movement type
            MovementType movement = isInhaling ? _inhaleMovement : _exhaleMovement;
            beamSpawner.SetMovementType(movement);
            beamSpawner.SetPhaseDuration(phaseDuration);
            beamSpawner.SpawnBeamSegments();
        }

        /// <summary>
        /// Updates the progress of the pacer based on the current phase of the breathing cycle
        /// </summary>
        void UpdatePacerProgress()
        {
            if (breathPacer == null) return;
            float progress = Mathf.Clamp01(_currentPhaseTime / Mathf.Max(0.01f, _currentPhaseDuration));
            breathPacer.SetPhase(_isInhaling, progress);
            OnProgressUpdated?.Invoke(progress);
        }

        #endregion

        #region Public API

        public void SetMovementPatterns(MovementType inhale, MovementType exhale)
        {
            _inhaleMovement = inhale;
            _exhaleMovement = exhale;
        }
        
        public void SetTargetCycles(int cycles)
        {
            targetFullBreaths = Mathf.Max(1, cycles);
        }
        
        public void Stop()
        {
            _isRunning = false;
            _isPaused = false;
            _waitingForFirstBeam = false;

            CancelInvoke(nameof(OnFirstBeamArrival));
            CancelInvoke(nameof(SpawnNextBeam));
            _currentPhaseTime = 0f;

        }

        public (bool isInhaling, float progress, float timeRemaining) GetCurrentPhase()
        {
            if (_waitingForFirstBeam)
                return (true, 0f, _beamTravelTime);

            float progress = Mathf.Clamp01(_currentPhaseTime / Mathf.Max(0.01f, _currentPhaseDuration));
            float timeRemaining = Mathf.Max(0f, _currentPhaseDuration - _currentPhaseTime);
            return (_isInhaling, progress, timeRemaining);
        }
        
        public bool IsRunning() => _isRunning;

        #endregion

    }
}
