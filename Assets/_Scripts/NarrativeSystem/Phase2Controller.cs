using System.Collections;
using UnityEngine;
using _Scripts.BreathGuideSystem;
using _Scripts.DialogueSystem;
using _Scripts.EffectModules;

namespace _Scripts.NarrativeSystem
{
    /// <summary>
    /// Phase 2: Fog clearing -> immediately stop segments -> enable bush interaction
    /// </summary>
    public class Phase2Controller : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private BreathRhythmController rhythmController;
        [SerializeField] private FogClearModule fogModule;
        [SerializeField] private ButterflySpawner butterflySpawner;
        [SerializeField] private CrystalBreathResponder crystalBreath;
        [Header("Fog Clearing")]
        [Tooltip("system cycles required (cycle = inhale + exhale)")]
        [SerializeField] private int fogClearCycles;

        [Header("Timing")]
        [SerializeField] private float completionDelayAfterBushTouched = 2f;

        [Header("Debug")]
        [SerializeField] private bool showDebug = true;

        [Header("VO (Optional)")]
        [SerializeField] private string[] phase2ProgressKeys = { "p2_progress", "p1_progress_2" };

        [Header("Gaze Gate (Optional)")]
        [SerializeField] private GazeHoldUI gazeHold;

        // State (system progression)
        private bool _isActive = false;
        private bool _fogCleared = false;
        private bool _canInteract = false;
        private bool _hasInteracted = false;

        private int _completedCycles = 0;   // SYSTEM cycles for fog clearing

        // Player performance recording (does NOT affect progression)
        private bool _leftHandCompletedThisCycle = false;
        private bool _rightHandCompletedThisCycle = false;

        private int _playerCyclesCompleted = 0;
        private int _playerCyclesParticipated = 0;
        private bool _playerDidAnythingThisCycle = false;

        // Definition: player completed cycle = both hands completed
        private bool IsPlayerCycleCompleted() => _leftHandCompletedThisCycle && _rightHandCompletedThisCycle;

        public System.Action OnPhaseComplete;

        public System.Action<int, bool, bool> OnCycleRecorded;

        #region Phase Lifecycle

        public void StartPhase()
        {
            if (_isActive)
            {
                if (showDebug) Debug.LogWarning("[Phase2] Already active!");
                return;
            }

            _isActive = true;
            _fogCleared = false;
            _canInteract = false;
            _hasInteracted = false;

            _completedCycles = 0;

            _playerCyclesCompleted = 0;
            _playerCyclesParticipated = 0;

            ResetPlayerFlagsForNewCycle();


            if (showDebug) Debug.Log("[Phase2] ═══ STARTING ═══");

            DialogueController.instance?.PlayDialogue("phase2_intro");
            StartCoroutine(StartBreathingAfterDialogue());
        }

        private IEnumerator StartBreathingAfterDialogue()
        {
            while (DialogueController.instance != null && DialogueController.instance.IsPlaying())
                yield return null;

            if (gazeHold != null)
            {
                bool done = false;

                gazeHold.OnCompleted -= OnGazeDone;
                gazeHold.OnCompleted += OnGazeDone;
                gazeHold.Begin();

                void OnGazeDone()
                {
                    gazeHold.OnCompleted -= OnGazeDone;
                    done = true;
                }

                while (!done)
                    yield return null;
            }

            yield return new WaitForSeconds(2f);
            PhaseManager.Instance.ShowBreathingUI();
            StartFogClearing();
        }

        private void StartFogClearing()
        {
            if (showDebug) Debug.Log("[Phase2] Starting fog clearing...");

            rhythmController.SetMovementPatterns(
                MovementType.VerticalUp,
                MovementType.VerticalDown
            );

            // IMPORTANT: your FogClearModule should no longer hard reset fog, just capture current config
            fogModule.Initialize(fogClearCycles);

            rhythmController.OnPhaseChanged += OnBreathPhaseChanged;

            if (!rhythmController.IsRunning())
                rhythmController.StartBreathCycle();
        }

        #endregion

        #region Breath Cycle Tracking

        private void OnBreathPhaseChanged(bool isInhaling, float duration)
        {
            if (!_isActive) return;
            if (_fogCleared) return; // once fog cleared, we stop caring about breath phases

            if (isInhaling)
            {
                ResetPlayerFlagsForNewCycle();
            }
            else
            {
                OnSystemCycleBoundary();
            }
        }

        private void OnSystemCycleBoundary()
        {


            _completedCycles++;
            BreathStatsTracker.Instance?.AddPhase2SystemCycle();

            if (_completedCycles  == 1)
            {
                BreathVoHelper.instance?.TryPlayRandom(phase2ProgressKeys);
            }

            if (showDebug)
                Debug.Log($"[Phase2] ✓ Fog cycle {_completedCycles}/{fogClearCycles}");

            fogModule.ClearFogStep(_completedCycles);

            if (_completedCycles >= fogClearCycles)
               Invoke(nameof(OnFogCleared),5f);
        }

        private void ResetPlayerFlagsForNewCycle()
        {
            _leftHandCompletedThisCycle = false;
            _rightHandCompletedThisCycle = false;
            _playerDidAnythingThisCycle = false;
        }



        private void OnFogCleared()
        {
            _fogCleared = true;

            if (showDebug)
                Debug.Log("[Phase2] ✓✓✓ FOG CLEARED! Stopping segments & enabling interaction.");

            // Stop breathing / segment spawning before guiding player to bush
            StopBreathingAndSegments();
            
            DialogueController.instance?.PlayDialogue("phase2_fog_cleared");

            // Immediately enable interaction
            EnableInteraction();
            
        }

        private void StopBreathingAndSegments()
        {
            // 1) stop listening
            rhythmController.OnPhaseChanged -= OnBreathPhaseChanged;

            // 2) stop the rhythm if you have a stop API
            rhythmController.Stop();

            PhaseManager.Instance.HideBreathingUI();
        }

        #endregion

        #region Interaction

        private void EnableInteraction()
        {
            _canInteract = true;
            
            DialogueController.instance?.PlayDialogue("phase2_attention_bush");
        }

        public void OnBushTouched()
        {
            if (!_isActive) return;
            if (!_canInteract) return;
            if (_hasInteracted) return;

            _hasInteracted = true;
            

            DialogueController.instance?.PlayDialogue("phase2_bush_touched");

            if (butterflySpawner != null)
                butterflySpawner.SpawnButterflies();

            Invoke(nameof(CompletePhase), completionDelayAfterBushTouched);
        }

        #endregion

        #region Phase Completion

        private void CompletePhase()
        {
            if (!_isActive) return;

            _isActive = false;
            

            // safety unsubscribe
            rhythmController.OnPhaseChanged -= OnBreathPhaseChanged;

            OnPhaseComplete?.Invoke();
        }

        public void StopPhase()
        {
            _isActive = false;
            rhythmController.OnPhaseChanged -= OnBreathPhaseChanged;
            CancelInvoke(nameof(CompletePhase));

            if (showDebug)
                Debug.Log("[Phase2] Phase stopped");
        }

        #endregion
        
    }
}
