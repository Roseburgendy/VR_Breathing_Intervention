using System.Collections;
using UnityEngine;
using _Scripts.BreathGuideSystem;
using _Scripts.DialogueSystem;
using _Scripts.EffectModules;

namespace _Scripts.NarrativeSystem
{
    public class Phase3Controller : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private BreathRhythmController rhythmController;
        [SerializeField] private SeasonShaderController seasonShader;
        [SerializeField] private CrystalBreathResponder crystalBreath;

        [Header("Breathing Pattern")]
        [SerializeField] private MovementType inhaleType = MovementType.CircleInhale;
        [SerializeField] private MovementType exhaleType = MovementType.CircleExhale;

        [Header("Gaze Gate (Optional)")]
        [SerializeField] private GazeHoldUI gazeHold;

        [Header("Transformation")]
        [Tooltip("How many full breath cycles (inhale+exhale) to finish the season transition.")]
        [SerializeField] private int transformBreathCycles=4;

        [Header("End Sequence")]
        [SerializeField] private string phase3IntroKey = "phase3_intro";
        [SerializeField] private string phase3CompleteKey = "phase3_end";
        [SerializeField] private ParticleSystem petalsParticle;
        [SerializeField] private float endDelaySeconds;
        
        [Header("Progress VO")]
        [SerializeField] private string[] phase3ProgressKeys = {"p3_progress"};

        [Header("Debug")]
        [SerializeField] private bool showDebug = true;

        private bool _isActive;
        private bool _isTransforming;

        // cycle semantics: cycle = inhale + exhale, increment at exhale start
        private int _completedCycles;

        // current phase state (updated by events)
        private bool _isInhaling;
        private float _phaseProgress01; // 0..1 within inhale or exhale


        public System.Action OnPhaseComplete;
        
        public void StartPhase()
        {

            _isActive = true;
            _isTransforming = false;

            _completedCycles = 0;
            _isInhaling = true;
            _phaseProgress01 = 0f;

            if (showDebug)
                Debug.Log("[Phase3] START | Breathing-driven global spring transformation");

            // IMPORTANT: ensure SeasonShader is prepared once (baseline/material runtime safety)
            seasonShader.CollectRenderers();

            if (DialogueController.instance != null && !string.IsNullOrEmpty(phase3IntroKey))
                DialogueController.instance.PlayDialogue(phase3IntroKey);

            StartCoroutine(BeginBreathingAfterDialogue());
        }

        private IEnumerator BeginBreathingAfterDialogue()
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

            yield return new WaitForSeconds(0.5f);
            PhaseManager.Instance.ShowBreathingUI();



            rhythmController.SetMovementPatterns(inhaleType, exhaleType);

            rhythmController.OnPhaseChanged += OnBreathPhaseChanged;
            rhythmController.OnProgressUpdated += OnBreathProgress;

            if (!rhythmController.IsRunning())
                rhythmController.StartBreathCycle();

            _isTransforming = true;

            // Start from winter explicitly
            seasonShader.SetSeasonProgress(0f);
            
        }

        private void OnBreathPhaseChanged(bool isInhaling, float duration)
        {
            if (!_isActive || !_isTransforming) return;

            _isInhaling = isInhaling;
            _phaseProgress01 = 0f;

            // cycle increments at EXHALE start
            if (!isInhaling)
            {
                _completedCycles++;

                float total = Mathf.Max(1, transformBreathCycles);
                float t = Mathf.Clamp01(_completedCycles / total);

                if (showDebug)
                    Debug.Log($"[Phase3] cycle={_completedCycles}/{transformBreathCycles} t={t:F3}");

                // ✅ 只在 cycle 更新一次
                seasonShader.SetSeasonProgress(t);

                // progress VO：你要 %2 随机播
                if (_completedCycles % 2 == 1)
                    BreathVoHelper.instance?.TryPlayRandom(phase3ProgressKeys);

                if (t >= 1f)
                    CompleteTransformAndEnd();
            }

            
            if (isInhaling)
                crystalBreath.OnInhaleStart();
            else
                crystalBreath.OnExhaleStart();
        }
        
        private void OnBreathProgress(float progress01)
        {
            if (!_isActive || !_isTransforming) return;

            _phaseProgress01 = Mathf.Clamp01(progress01);
            
            
            if (rhythmController.GetCurrentPhase().isInhaling)
                crystalBreath.UpdateInhale(progress01);
            else
                crystalBreath.UpdateExhale(progress01);
        }


        // Convert inhale/exhale progress into 0..1 inside current cycle
        private float ComputeBreathDrivenT()
        {
            int total = Mathf.Max(1, transformBreathCycles);

            // Important: _completedCycles increments at exhale start.
            // That means:
            // - During inhale of cycle N: _completedCycles == (N-1)
            // - During exhale of cycle N: _completedCycles == N
            //
            // We want a continuous within-cycle value:
            // inhale: 0..0.5, exhale: 0.5..1

            float withinCycle01 = _isInhaling
                ? (0.0f + 0.5f * _phaseProgress01)
                : (0.5f + 0.5f * _phaseProgress01);

            // We need a "base cycle index" representing how many full cycles were completed BEFORE this cycle started.
            // Since _completedCycles jumps at exhale start, adjust base depending on phase:
            int baseCompleted = _isInhaling ? _completedCycles : (_completedCycles - 1);

            float overall = (baseCompleted + withinCycle01) / total;
            return Mathf.Clamp01(overall);
        }

        private void CompleteTransformAndEnd()
        {
            if (!_isActive) return;

            _isTransforming = false;

            // Lock to final state
            seasonShader.SetSeasonProgress(1f);

            rhythmController.OnPhaseChanged -= OnBreathPhaseChanged;
            rhythmController.OnProgressUpdated -= OnBreathProgress;
            rhythmController.Stop();
            
            PhaseManager.Instance.HideBreathingUI();

            if (DialogueController.instance != null && !string.IsNullOrEmpty(phase3CompleteKey))
                DialogueController.instance.PlayDialogue(phase3CompleteKey);

            if (petalsParticle != null)
                petalsParticle.Play();

            Invoke(nameof(TriggerCompletion), endDelaySeconds);
        }

        private void TriggerCompletion()
        {
            if (showDebug)
                Debug.Log("[Phase3] COMPLETE");

            _isActive = false;
            OnPhaseComplete?.Invoke();
        }

        public void StopPhase()
        {
            _isActive = false;
            _isTransforming = false;

            if (rhythmController != null)
            {
                rhythmController.OnPhaseChanged -= OnBreathPhaseChanged;
                rhythmController.OnProgressUpdated -= OnBreathProgress;
                rhythmController.Stop();
            }

            CancelInvoke(nameof(TriggerCompletion));
        }
    }
}
