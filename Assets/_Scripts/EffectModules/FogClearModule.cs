using AtmosphericHeightFog;
using UnityEngine;
using DG.Tweening;

namespace _Scripts.EffectModules
{
    /// <summary>
    /// Smooth fog clearing module:
    /// - Does NOT force-reset fog on Initialize (reads current settings as start to avoid popping)
    /// - Smoothly transitions towards target progress each step (DOTween or speed-based)
    /// </summary>
    public class FogClearModule : MonoBehaviour
    {
        [Header("Fog Controllers")]
        [SerializeField] private HeightFogGlobal heightFogController;
        [SerializeField] private bool controlUnityFog = true;
        [SerializeField] private bool controlHeightFog = true;

        [Header("Targets (End State)")]
        [SerializeField] private float finalDensity = 0.001f;          // Unity fog density end
        [SerializeField] private float finalIntensity = 0.2f;          // Height fog intensity end
        [SerializeField] private Color warmColor = new Color(0.9f, 0.92f, 0.85f);
        

        [Tooltip("Duration to blend to each new step target. Recommend roughly = one breath cycle duration.")]
        [SerializeField] private float stepBlendDuration = 1.2f;

        [Tooltip("If DOTween is off, this is the speed (per second) used to move progress towards target.")]
        [SerializeField] private float progressLerpSpeed = 1.5f;

        [Header("Effects")]
        [SerializeField] private bool playEffectEachStep = false;      // 通常不建议每步都喷粒子，会“段落感”
        [SerializeField] private ParticleSystem clearEffect;
        [SerializeField] private AudioClip clearSound;

        // runtime
        private int _totalSteps = 1;

        // “start values” are captured from current scene to avoid popping
        private bool _capturedStart = false;
        private float _startDensity;
        private float _startIntensity;
        private Color _startColor;

        private float _currentProgress = 0f;
        private float _targetProgress = 0f;

        // DOTween handles
        private Tween _progressTween;

        /// <summary>
        /// Prepare for clearing with N steps.
        /// IMPORTANT: Does NOT overwrite current fog. It captures current fog as the start state.
        /// Call once when Phase2 starts.
        /// </summary>
        public void Initialize(int steps)
        {
            _totalSteps = Mathf.Max(1, steps);

            // Find fog controller if needed
            if (controlHeightFog && heightFogController == null)
                heightFogController = FindObjectOfType<HeightFogGlobal>();

            CaptureStartIfNeeded();

            // start from current state (progress=0 => start state)
            _currentProgress = 0f;
            _targetProgress = 0f;
            ApplyFogAtProgress(_currentProgress, force: true);
        }

        private void CaptureStartIfNeeded()
        {
            if (_capturedStart) return;

            // Unity fog start
            if (controlUnityFog)
            {
                _startDensity = RenderSettings.fogDensity;
                _startColor = RenderSettings.fogColor;
            }
            else
            {
                _startDensity = 0.05f;
                _startColor = new Color(0.7f, 0.78f, 0.86f);
            }

            // Height fog start
            if (controlHeightFog && heightFogController != null)
            {
                _startIntensity = heightFogController.fogIntensity;
                // use fogColorStart as baseline if available
                _startColor = heightFogController.fogColorStart;
            }
            else
            {
                _startIntensity = 1.0f;
            }

            _capturedStart = true;
        }

        /// <summary>
        /// Call this each time you finish a breath cycle (or whenever you want to advance one step).
        /// It will smoothly transition rather than snap.
        /// </summary>
        public void ClearFogStep(int currentStep)
        {
            currentStep = Mathf.Clamp(currentStep, 0, _totalSteps);
            _targetProgress = (float)currentStep / _totalSteps;

            TweenToTargetProgress(_targetProgress);

            if (playEffectEachStep)
                PlayClearEffect();
        }

        private void TweenToTargetProgress(float target)
        {
            _progressTween?.Kill();
            _progressTween = DOTween.To(
                    () => _currentProgress,
                    x =>
                    {
                        _currentProgress = x;
                        ApplyFogAtProgress(_currentProgress, force: false);
                    },
                    target,
                    Mathf.Max(0.01f, stepBlendDuration)
                )
                .SetEase(Ease.InOutSine);
        }
        

        private void ApplyFogAtProgress(float progress, bool force)
        {
            progress = Mathf.Clamp01(progress);

            // derived values
            float density = Mathf.Lerp(_startDensity, finalDensity, progress);
            float intensity = Mathf.Lerp(_startIntensity, finalIntensity, progress);
            Color color = Color.Lerp(_startColor, warmColor, progress);

            if (controlUnityFog)
            {
                // do not toggle fog on/off unless you explicitly want it
                if (force && !RenderSettings.fog) RenderSettings.fog = true;
                RenderSettings.fogDensity = density;
                RenderSettings.fogColor = color;
            }

            if (controlHeightFog && heightFogController != null)
            {
                heightFogController.fogIntensity = intensity;
                heightFogController.fogColorStart = color;
            }
        }

        private void PlayClearEffect()
        {
            if (clearEffect != null) clearEffect.Play();
        }

        public float GetProgress() => _currentProgress;

        /// <summary>
        /// Optional: If you want to re-capture start state (e.g., restarting game).
        /// </summary>
        public void ResetCapture()
        {
            _capturedStart = false;
        }
    }
}
