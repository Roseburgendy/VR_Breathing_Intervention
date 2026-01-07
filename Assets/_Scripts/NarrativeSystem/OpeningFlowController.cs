using System.Collections;
using UnityEngine;
using _Scripts.DialogueSystem;

namespace _Scripts.NarrativeSystem
{
    /// <summary>
    /// Opening flow controller (NO Timeline / NO Cutscene).
    /// Flow:
    /// 1) Play intro dialogue (cutscene_intro)
    /// 2) Wait intro end
    /// 3) Play gaze dialogue (cutscene_gaze)
    /// 4) Wait gaze dialogue end
    /// 5) Begin gaze-hold interaction via GazeHoldUIInteractor
    /// 6) Wait gaze complete
    /// 7) Fire OnOpeningComplete
    /// </summary>
    public class OpeningFlowController : MonoBehaviour
    {
        [Header("Reusable Gaze Module")]
        [SerializeField] private GazeHoldUI gazeInteractor;

        [Header("Debug")]
        [SerializeField] private bool showDebug = true;
        [SerializeField] private bool skipGaze = false; // 测试：跳过 gaze gate（直接完成 opening）

        // State
        private bool _openingActive = false;
        private bool _waitingForGaze = false;
        private bool _gazeDone = false;

        // Coroutines
        private Coroutine _flowRoutine;

        // Events
        public System.Action OnOpeningComplete;

        void Awake()
        {
            if (gazeInteractor == null && showDebug)
                Debug.LogWarning("[OpeningFlow] GazeHoldUIInteractor not assigned. Gaze step will be skipped.");
        }

        #region Public API

        public void StartOpening()
        {
            if (_openingActive)
            {
                Debug.LogWarning("[OpeningFlow] Already active!");
                return;
            }

            _openingActive = true;
            _waitingForGaze = false;
            _gazeDone = false;

            if (showDebug) Debug.Log("[OpeningFlow] ═══ STARTING ═══");

            // Ensure previous routine stopped
            if (_flowRoutine != null)
                StopCoroutine(_flowRoutine);

            // Cancel any pending gaze
            if (gazeInteractor != null)
                gazeInteractor.Cancel(hideUI: true);

            _flowRoutine = StartCoroutine(OpeningFlowCoroutine());
        }

        public void SkipOpening()
        {
            if (_flowRoutine != null)
            {
                StopCoroutine(_flowRoutine);
                _flowRoutine = null;
            }

            _openingActive = false;
            _waitingForGaze = false;
            _gazeDone = false;

            // Stop dialogue if needed
            if (DialogueController.instance != null)
                DialogueController.instance.StopDialogue();

            // Cancel gaze UI + state
            if (gazeInteractor != null)
            {
                gazeInteractor.OnCompleted -= OnGazeCompleted;
                gazeInteractor.Cancel(hideUI: true);
            }

            CompleteOpening();
        }

        public bool IsActive() => _openingActive || _waitingForGaze;
        public bool IsWaitingForGaze() => _waitingForGaze;

        #endregion

        #region Flow

        private IEnumerator OpeningFlowCoroutine()
        {
            if (skipGaze)
            {
                if (showDebug) Debug.Log("[OpeningFlow] SkipGaze enabled -> Completing immediately.");
                _openingActive = false;
                _waitingForGaze = false;
                CompleteOpening();
                yield break;
            }

            // 1) Play intro dialogue
            if (DialogueController.instance != null)
                DialogueController.instance.PlayDialogue("cutscene_intro");

            // 2) Wait intro end
            if (DialogueController.instance != null)
            {
                while (DialogueController.instance.IsPlaying())
                    yield return null;
            }

            // 3) Play gaze dialogue
            if (DialogueController.instance != null)
                DialogueController.instance.PlayDialogue("cutscene_gaze");

            // 4) Wait gaze dialogue end
            if (DialogueController.instance != null)
            {
                while (DialogueController.instance.IsPlaying())
                    yield return null;
            }

            // 5) Begin gaze gate (reusable module)
            if (gazeInteractor != null)
            {
                BeginGazeGate();

                // 6) Wait gaze complete
                while (!_gazeDone)
                    yield return null;
            }
            else
            {
                if (showDebug) Debug.LogWarning("[OpeningFlow] gazeInteractor is null -> skipping gaze gate.");
            }

            _openingActive = false;
            _waitingForGaze = false;
            _flowRoutine = null;

            // 7) Complete
            CompleteOpening();
            
        }

        private void BeginGazeGate()
        {
            _waitingForGaze = true;
            _gazeDone = false;

            gazeInteractor.OnCompleted -= OnGazeCompleted; // 防止重复订阅
            gazeInteractor.OnCompleted += OnGazeCompleted;

            gazeInteractor.Begin();

            if (showDebug) Debug.Log("[OpeningFlow] Waiting for gaze via GazeHoldUIInteractor...");
        }

        private void OnGazeCompleted()
        {
            if (!_waitingForGaze) return;

            _gazeDone = true;
            _waitingForGaze = false;

            if (gazeInteractor != null)
                gazeInteractor.OnCompleted -= OnGazeCompleted;

            if (showDebug) Debug.Log("[OpeningFlow] ✓ Gaze complete");
        }

        #endregion

        #region Completion

        private void CompleteOpening()
        {
            if (showDebug) Debug.Log("[OpeningFlow] ═══ COMPLETE ═══");
            OnOpeningComplete?.Invoke();
        }

        #endregion
    }
}
