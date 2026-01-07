using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace _Scripts.NarrativeSystem
{
    /// <summary>
    /// Reusable gaze-hold interaction with prompt + progress UI.
    /// Default detector: Look-down hold (based on camera forward.y threshold).
    /// </summary>
    public class GazeHoldUI : MonoBehaviour
    {
        [Header("Gaze Target (Camera)")]
        [SerializeField] private Camera playerCamera;

        [Header("Hold Settings")]
        [SerializeField] private float gazeHoldTime = 2f;

        [Header("Look Down Detector")]
        [Tooltip("forward.y <= threshold => looking down. Example: -0.45")]
        [SerializeField] private float lookDownYThreshold = -0.45f;

        [Header("Lost Gaze Behavior")]
        [SerializeField] private bool decayOnLostGaze = true;
        [SerializeField] private float decaySpeed = 2f;
        [SerializeField] private float lostGazeGraceTime = 0.0f;

        [Header("UI - Prompt (Instruction)")]
        [SerializeField] private GameObject gazePromptUI;

        [Header("UI - Progress (Feedback)")]
        [SerializeField] private GameObject gazeProgressUI;     // optional container
        [SerializeField] private Image gazeFillImage;           // radial filled
        [SerializeField] private CanvasGroup gazeUICanvasGroup; // fade whole block

        [Header("UI - Fade")]
        [SerializeField] private float uiFadeInDuration = 0.5f;
        [SerializeField] private float uiFadeOutDuration = 0.3f;

        [Header("Debug")]
        [SerializeField] private bool showDebug = false;

        // Runtime state
        private bool _active = false;
        private float _currentHold = 0f;
        private bool _isGazing = false;
        private float _lostGazeTimer = 0f;

        // Events
        public System.Action OnCompleted;
        public System.Action<float> OnProgress; // normalized 0..1

        void Awake()
        {
            if (playerCamera == null) playerCamera = Camera.main;
        }

        void Start()
        {
            HideAllUIImmediate();
            ResetProgressImmediate();
        }

        void Update()
        {
            if (!_active) return;
            UpdateGazeDetection();
        }

        /// <summary>
        /// Begin gaze-hold interaction.
        /// </summary>
        public void Begin()
        {
            if (_active) return;

            _active = true;
            _currentHold = 0f;
            _isGazing = false;
            _lostGazeTimer = 0f;

            ShowPromptUI();
            HideProgressUIImmediate();
            UpdateProgressUI(0f);

            if (showDebug) Debug.Log("[GazeHold] Begin");
        }

        /// <summary>
        /// Cancel interaction and hide UI.
        /// </summary>
        public void Cancel(bool hideUI = true)
        {
            _active = false;
            _isGazing = false;

            if (hideUI)
                FadeOutAllUI(() => HideAllUIImmediate());
            else
                HideAllUIImmediate();

            ResetProgressImmediate();

            if (showDebug) Debug.Log("[GazeHold] Cancel");
        }

        public bool IsActive() => _active;

        private void UpdateGazeDetection()
        {
            if (playerCamera == null) return;

            bool isLookingDown = playerCamera.transform.forward.y <= lookDownYThreshold;

            if (isLookingDown)
            {
                _lostGazeTimer = 0f;

                if (!_isGazing)
                {
                    _isGazing = true;
                    OnGazeStart();
                }

                _currentHold += Time.deltaTime;
                _currentHold = Mathf.Clamp(_currentHold, 0f, gazeHoldTime);

                float norm = _currentHold / gazeHoldTime;
                UpdateProgressUI(norm);
                OnProgress?.Invoke(norm);

                if (_currentHold >= gazeHoldTime)
                    OnGazeComplete();
            }
            else
            {
                if (_isGazing) _isGazing = false;

                if (lostGazeGraceTime > 0f)
                {
                    _lostGazeTimer += Time.deltaTime;
                    if (_lostGazeTimer < lostGazeGraceTime)
                    {
                        float norm = _currentHold / gazeHoldTime;
                        UpdateProgressUI(norm);
                        OnProgress?.Invoke(norm);
                        return;
                    }
                }

                if (decayOnLostGaze)
                {
                    _currentHold = Mathf.Max(0f, _currentHold - Time.deltaTime * Mathf.Max(0.01f, decaySpeed));
                }
                else
                {
                    _currentHold = 0f;
                }

                float norm2 = _currentHold / gazeHoldTime;
                UpdateProgressUI(norm2);
                OnProgress?.Invoke(norm2);

                if (_currentHold <= 0.001f)
                    HideProgressUIImmediate();
            }
        }

        private void OnGazeStart()
        {
            ShowProgressUI();
        }

        private void OnGazeComplete()
        {
            _active = false;
            
            if (showDebug) Debug.Log("[GazeHold] Completed");

            FadeOutAllUI(() => HideAllUIImmediate());
            Invoke(nameof(FireCompleted), 0.05f);
        }

        private void FireCompleted()
        {
            OnCompleted?.Invoke();
        }

        #region UI Helpers

        private void ShowPromptUI()
        {
            if (gazePromptUI != null) gazePromptUI.SetActive(true);

            if (gazeUICanvasGroup != null)
            {
                gazeUICanvasGroup.DOKill();
                gazeUICanvasGroup.alpha = 0f;
                gazeUICanvasGroup.DOFade(1f, uiFadeInDuration);
            }
        }

        private void ShowProgressUI()
        {
            if (gazeProgressUI != null) gazeProgressUI.SetActive(true);

            if (gazeProgressUI == null && gazeFillImage != null)
                gazeFillImage.enabled = true;
        }

        private void HideProgressUIImmediate()
        {
            if (gazeProgressUI != null) gazeProgressUI.SetActive(false);

            if (gazeProgressUI == null && gazeFillImage != null)
                gazeFillImage.enabled = false;

            if (gazeFillImage != null)
                gazeFillImage.fillAmount = 0f;
        }

        private void UpdateProgressUI(float normalized)
        {
            if (gazeFillImage == null) return;
            gazeFillImage.fillAmount = Mathf.Clamp01(normalized);
        }

        private void ResetProgressImmediate()
        {
            _currentHold = 0f;
            _lostGazeTimer = 0f;
            if (gazeFillImage != null) gazeFillImage.fillAmount = 0f;
        }

        private void HideAllUIImmediate()
        {
            if (gazePromptUI != null) gazePromptUI.SetActive(false);
            HideProgressUIImmediate();

            if (gazeUICanvasGroup != null)
            {
                gazeUICanvasGroup.DOKill();
                gazeUICanvasGroup.alpha = 0f;
            }
        }

        private void FadeOutAllUI(System.Action onComplete)
        {
            if (gazeUICanvasGroup != null)
            {
                gazeUICanvasGroup.DOKill();
                gazeUICanvasGroup.DOFade(0f, uiFadeOutDuration).OnComplete(() => onComplete?.Invoke());
            }
            else
            {
                onComplete?.Invoke();
            }
        }

        #endregion
    }
}
