using UnityEngine;
using UnityEngine.UI;
using _Scripts.BreathGuideSystem;

namespace _Scripts.NarrativeSystem
{
    /// <summary>
    /// Master phase flow manager
    /// SINGLE ENTRY POINT for entire experience
    /// Manages phase transitions and breath system
    /// </summary>
    public class PhaseManager : MonoBehaviour
    {
        public static PhaseManager Instance { get; private set; }
        
        [Header("Phase Controllers")]
        [SerializeField] private Phase1Controller phase1;
        [SerializeField] private Phase2Controller phase2;
        [SerializeField] private Phase3Controller phase3;
        [SerializeField] private OpeningFlowController openingFlow;

        
        [Header("Breathing System")]
        [SerializeField] private BreathRhythmController rhythmController;
        [SerializeField] private BreathPacer breathPacer;
        
        [Header("UI")]
        [SerializeField] private GameObject completionUI;

        [Header("Pacer Anchors")] 
        [SerializeField] private GameObject pacerOrigin;
        [SerializeField] private Transform pacerAnchorPhase3;

        [SerializeField] private bool matchAnchorRotation = true;

        
        [Header("Settings")]
        [SerializeField] private bool showDebug = true;
        [SerializeField] private bool skipOpening;
        
        
        [Header("Phase 3 Trigger")]
        [SerializeField] private bool phase3RequiresTrigger = true; // 开关：是否必须区域触发
        private bool _waitingForPhase3Trigger = false;

        
        [Header("TEST / DEBUG START")]
        [SerializeField] private bool enableDebugStart;

        [Tooltip("0 = normal flow, 1 = start at phase1, 2 = start at phase2, 3 = start at phase3")]
        [Range(0, 3)]
        [SerializeField] private int debugStartPhase;

        [Tooltip("If true, force bypass Phase3 trigger requirement when debugStartPhase==3")]
        [SerializeField] private bool bypassPhase3TriggerInDebug = true;

        
        // State
        private int _currentPhase = 0;
        private bool _experienceStarted = false;
        private bool _experienceComplete = false;
        
        // Events
        public System.Action<int> OnPhaseStarted;
        public System.Action<int> OnPhaseCompleted;
        public System.Action OnExperienceStarted;
        public System.Action OnExperienceCompleted;
        
        #region Unity Lifecycle
        
        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }
        }
        
        void Start()
        {
            ValidateReferences();
            InitializeExperience();

            if (enableDebugStart && debugStartPhase > 0)
            {
                if (showDebug)
                    Debug.Log($"[PhaseManager] DEBUG START ENABLED → Starting at Phase {debugStartPhase}");

                StartExperienceFromPhase(debugStartPhase);
                return;
            }

            // Phase 0: Opening
            if (openingFlow != null && !skipOpening)
            {
                openingFlow.OnOpeningComplete += StartExperience;
                openingFlow.StartOpening();
            }
            else
            {
                StartExperience();
            }
        }


        
        #endregion
        
        #region Initialization
        
        void ValidateReferences()
        {
            if (rhythmController == null)
            {
                rhythmController = FindObjectOfType<BreathRhythmController>();
            }
            
            if (breathPacer == null)
            {
                breathPacer = FindObjectOfType<BreathPacer>();
            }
            
            // Subscribe to phase completions
            if (phase1 != null) phase1.OnPhaseComplete += () => CompletePhase(1);
            if (phase2 != null) phase2.OnPhaseComplete += () => CompletePhase(2);
            if (phase3 != null) phase3.OnPhaseComplete += () => CompletePhase(3);
        }
        
        void InitializeExperience()
        {
            // Hide pacer initially
            if (breathPacer != null)
            {
                breathPacer.gameObject.SetActive(false);
            }
            
            // Disable all phases
            if (phase1 != null) phase1.enabled = false;
            if (phase2 != null) phase2.enabled = false;
            if (phase3 != null) phase3.enabled = false;
            
            // Hide completion UI
            if (completionUI != null)
            {
                completionUI.SetActive(false);
            }
            
            
            if (showDebug)
            {
                Debug.Log("[PhaseManager] ═══ Initialized - Ready to Start ═══");
            }
        }
        
        #endregion
        
        #region Experience Control
        
        /// <summary>
        /// Start the entire experience
        /// </summary>
        private void StartExperience()
        {
            if (_experienceStarted)
            {
                return;
            }
            ShowBreathingUI();
            _experienceStarted = true;
            _currentPhase = 0;
            OnExperienceStarted?.Invoke();
            // Start Phase 1
            StartPhase(1);
        }
        
        /// <summary>
        /// Start a specific phase
        /// </summary>
        private void StartPhase(int phaseNumber)
        {
            _currentPhase = phaseNumber;
            OnPhaseStarted?.Invoke(phaseNumber);
            // Phase-specific pacer placement
            if (phaseNumber == 3)
            {
                RepositionPacerForPhase3();
            }
            // Start appropriate phase
            switch (phaseNumber)
            {
                case 1:
                    if (phase1 != null)
                    {
                        phase1.enabled = true;
                        phase1.StartPhase();
                    }
                    break;
                    
                case 2:
                    if (phase2 != null)
                    {
                        phase2.enabled = true;
                        phase2.StartPhase();
                    }
                    break;
                    
                case 3:
                    if (phase3 != null)
                    {
                        phase3.enabled = true;
                        phase3.StartPhase();
                    }
                    break;
            }
        }
        private void RepositionPacerForPhase3()
        {
            
            pacerOrigin.transform.position = pacerAnchorPhase3.position;

            if (matchAnchorRotation)
                pacerOrigin.transform.rotation = pacerAnchorPhase3.rotation;

            if (showDebug)
                Debug.Log("[PhaseManager] Pacer moved to Phase3 anchor.");
        }

        
        /// <summary>
        /// Called when a phase completes
        /// </summary>
        private void CompletePhase(int phaseNumber)
        {
            OnPhaseCompleted?.Invoke(phaseNumber);
            // Hide pacer after phase
            HideBreathingUI();
            // Stop breathing (will restart in next phase)
            if (rhythmController != null)
            {
                rhythmController.Stop();
            }
            // Disable current phase
            switch (phaseNumber)
            {
                case 1:
                    if (phase1 != null) phase1.enabled = false;
                    break;
                case 2:
                    if (phase2 != null) phase2.enabled = false;
                    break;
                case 3:
                    if (phase3 != null) phase3.enabled = false;
                    break;
            }
            // advance to next phase or complete
            if (phaseNumber < 3)
            {
                Invoke(nameof(StartNextPhase), 10f);
            }
            else
            {
                Invoke(nameof(CompleteExperience), 10f);
            }

        }
        
        void StartNextPhase()
        {
            StartPhase(_currentPhase + 1);
        }
        
        void CompleteExperience()
        {
            _experienceComplete = true;
            // Hide breathing UI
            HideBreathingUI();
            
            // Show completion UI
            if (completionUI != null)
            {
                completionUI.SetActive(true);
            }
            
            // Notify
            OnExperienceCompleted?.Invoke();
        }
        public void StartExperienceFromPhase(int startPhase)
        {
            if (_experienceStarted)
            {
                Debug.LogWarning("[PhaseManager] Experience already started!");
                return;
            }

            _experienceStarted = true;
            _experienceComplete = false;

            // Clamp
            startPhase = Mathf.Clamp(startPhase, 1, 3);

            // If we jump directly to Phase3 for testing, bypass trigger gating
            if (startPhase == 3 && bypassPhase3TriggerInDebug)
            {
                phase3RequiresTrigger = false;      // 直接关闭需求
                _waitingForPhase3Trigger = false;   // 确保不会卡在等待
            }

            // Set current phase to (startPhase - 1) so StartPhase(startPhase) behaves predictably
            _currentPhase = startPhase - 1;

            if (showDebug)
            {
                Debug.Log("[PhaseManager] ═══ EXPERIENCE STARTING (DEBUG JUMP) ═══");
                Debug.Log($"[PhaseManager] Jumping to Phase {startPhase}");
            }

            OnExperienceStarted?.Invoke();

            StartPhase(startPhase);
        }

        #endregion
        
        #region Breathing UI Control
        
        /// <summary>
        /// Show breathing guidance UI
        /// </summary>
        public void ShowBreathingUI()
        {
            if (breathPacer != null)
            {
                breathPacer.gameObject.SetActive(true);
            }
        }
        
        /// <summary>
        /// Hide breathing guidance UI
        /// </summary>
        public void HideBreathingUI()
        {
            if (breathPacer != null)
            {
                breathPacer.gameObject.SetActive(false);
            }
        }
        
        #endregion
        
        #region Public API
        
        public int GetCurrentPhase() => _currentPhase;
        public bool IsExperienceStarted() => _experienceStarted;
        public bool IsExperienceComplete() => _experienceComplete;
        
        /// <summary>
        /// Called by a teleport/trigger zone when player arrives at the Phase 3 area.
        /// </summary>
        public void RequestStartPhase3()
        {
            if (!_experienceStarted || _experienceComplete) return;

            // 必须先完成 Phase2
            if (_currentPhase < 2)
            {
                if (showDebug) Debug.LogWarning("[PhaseManager] Phase3 trigger ignored: Phase2 not completed yet.");
                return;
            }

            if (!phase3RequiresTrigger)
            {
                if (showDebug) Debug.LogWarning("[PhaseManager] Phase3 trigger ignored: phase3RequiresTrigger is false.");
                return;
            }

            if (!_waitingForPhase3Trigger)
            {
                // 避免重复触发/或已经开始过了
                if (showDebug) Debug.Log("[PhaseManager] Phase3 trigger ignored: not waiting.");
                return;
            }

            _waitingForPhase3Trigger = false;

            if (showDebug) Debug.Log("[PhaseManager] Phase3 trigger accepted. Starting Phase 3...");
            StartPhase(3);
        }

        #endregion

        public void TransitionToMenuUI()
        {
            if (AudioManager.instance != null)
            {
                AudioManager.instance.StopMusic();
                AudioManager.instance.StopAllAmbients();
            }

            TransitionManager.instance.TransitionToLevel("StartScene");
        }
    }
}