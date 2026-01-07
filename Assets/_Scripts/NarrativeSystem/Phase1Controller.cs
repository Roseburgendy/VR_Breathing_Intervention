using UnityEngine;
using _Scripts.BreathGuideSystem;
using _Scripts.DialogueSystem;
using _Scripts.EffectModules;

namespace _Scripts.NarrativeSystem
{
    /// <summary>
    /// Phase 1: Guide callback + Crystal awakening
    /// Player awakens the crystal through breathing
    /// NO fog clearing (that's Phase 2)
    /// </summary>
    public class Phase1Controller : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private BreathRhythmController rhythmController;
        [SerializeField] private CrystalGlowModule crystalModule;
        [SerializeField] private CrystalBreathResponder crystalBreath;

        [Header("Progression")]
        [SerializeField] private int breathCyclesRequired;
        [SerializeField] private bool requireBothHands = true;
        
        [Header("Debug")]
        [SerializeField] private bool showDebug = true;
        
        
        [SerializeField] private string phase1StartKey = "p1_start";
        [SerializeField] private string[] phase1ProgressKeys = { "p1_progress_1", "p1_progress_2" };

        [SerializeField] private float progressCooldown = 8f;
        [SerializeField] private int progressEveryFullBreaths;
        [SerializeField] private float voiceVol = 1f;
        
        // State
        private bool _isActive = false;
        private int _completedBreathCycles = 0;
        // Player performance (Phase1)
        private bool _playerDidAnythingThisCycle = false;
        private bool _playerHitThisCycle = false;
        private bool _completionQueued = false;

        private bool _lastWasExhale = false;
        
        public System.Action OnPhaseComplete;
        
        
        #region Phase Lifecycle
        
        public void StartPhase()
        {
            if (_isActive)
            {
                Debug.LogWarning("[Phase1] Already active!");
                return;
            }
            _isActive = true;
            _completedBreathCycles = 0;
            PlayStartVO();
            StartBreathing();
        }

        void StartBreathing()
        {
            // Set movement type - Horizontal Open/Close for awakening
            rhythmController.SetMovementPatterns(
                MovementType.HorizontalOpen,
                MovementType.HorizontalClose
            );
            
            // Subscribe to breath rhythm events (for crystal visual sync)
            rhythmController.OnPhaseChanged += OnBreathPhaseChanged;
            rhythmController.OnProgressUpdated += OnBreathProgress;
            
            // Start breath cycle
            if (!rhythmController.IsRunning())
            {
                rhythmController.StartBreathCycle();
            }
        }
        
        #endregion
        #region Breath Effects


        // ReSharper disable Unity.PerformanceAnalysis
        void OnBreathPhaseChanged(bool isInhaling, float duration)
        {
            if (!_isActive) return;

            if (!isInhaling && !_lastWasExhale)
            {
                _lastWasExhale = true;
            }
            else if (isInhaling && _lastWasExhale)
            {
                _lastWasExhale = false;
                OnFullBreathCompletedFallback();
            }

            // Crystal Effect
            if (isInhaling)
                crystalBreath.OnInhaleStart();
            else
                crystalBreath.OnExhaleStart();
        }

        void OnFullBreathCompletedFallback()
        {
            _completedBreathCycles++;
            
            if(_completedBreathCycles %2==1)
                BreathVoHelper.instance?.TryPlayRandom(phase1ProgressKeys);
            if (_completedBreathCycles >= breathCyclesRequired)
                Invoke(nameof(CompletePhase),5f);
        }


        void OnBreathProgress(float progress)
        {
            if (!_isActive) return;

            if (rhythmController.GetCurrentPhase().isInhaling)
                crystalBreath.UpdateInhale(progress);
            else
                crystalBreath.UpdateExhale(progress);
        }
        #endregion
        
        
        #region Phase Completion
        
        void CompletePhase()
        {
            _isActive = false;
            rhythmController.OnPhaseChanged -= OnBreathPhaseChanged;
            rhythmController.OnProgressUpdated -= OnBreathProgress;
            CancelInvoke(nameof(rhythmController.SpawnNextBeam));
            PhaseManager.Instance.HideBreathingUI();
            
            PlayEndVO();  
            
            if (crystalModule != null)
            {
                crystalModule.PlayCompletionEffect();
            }
            Invoke(nameof(NotifyPhaseComplete), 5f);
        }
        
        void NotifyPhaseComplete(){
            OnPhaseComplete?.Invoke();
        }
        
        #endregion
        

        #region Voice Over
        private void PlayStartVO()
        {
            if (AudioManager.instance == null) return;
            if (string.IsNullOrEmpty(phase1StartKey)) return;

            AudioManager.instance.PlayVoiceByKey(phase1StartKey, voiceVol);
        }

        private void PlayEndVO()
        {
            if (DialogueController.instance != null)
                DialogueController.instance.PlayDialogue("phase1_end");
        }
        #endregion
    }
}