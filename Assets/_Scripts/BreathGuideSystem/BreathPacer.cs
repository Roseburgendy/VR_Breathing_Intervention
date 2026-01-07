using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _Scripts.BreathGuideSystem
{
    /// <summary>
    /// Breath Pacer
    /// Exposed function
    /// - SetBreathDuration(duration): Initialization
    /// - SetPhase(isInhaling, progress01): update visual according to progress
    /// </summary>
    public class BreathPacer : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Canvas canvas;
        [SerializeField] private Image backgroundRing; 
        [SerializeField] private Image fillCircle;
        [SerializeField] private TextMeshProUGUI instructionText;

        # region Private Variables
        private const float MinScale = 0.3f; // Exhale size
        private const float MaxScale = 1.0f; // Inhale size
        private float _breathDuration;
        private readonly Color _inhaleColor = new Color(0.3f, 0.8f, 1f);
        private readonly Color _exhaleColor = new Color(1f, 0.6f, 0.3f);
        private readonly AnimationCurve _breathCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        private Vector3 _initialScale = Vector3.one;
        private bool _isInhaling = true;
        private float _progress = 0f;
        #endregion

        void Awake()
        {
            if (canvas == null) canvas = GetComponent<Canvas>();

            if (fillCircle != null)
                _initialScale = fillCircle.transform.localScale;
        }
        void OnEnable()
        {
            // Ensure correct initial rendering when enabled
            ApplyVisual(_isInhaling, _progress);
        }

        /// <summary>
        /// Set current breath phase duration
        /// Initialization
        /// </summary>
        public void SetBreathDuration(float duration)
        {
            _breathDuration = Mathf.Max(0.1f, duration);
        }

        /// <summary>
        /// Update pacer visuals for the current phase.
        /// Progress
        /// </summary>
        public void SetPhase(bool inhaling, float progress)
        {
            _isInhaling = inhaling;
            _progress = Mathf.Clamp01(progress);

            ApplyVisual(_isInhaling, _progress);
        }
        
        /// <summary>
        /// Do animating
        /// </summary>
        /// <param name="inhaling">true when inhaling</param>
        /// <param name="progress01">0-1 progress</param>
        private void ApplyVisual(bool inhaling, float progress01)
        {
            if (fillCircle == null) return;

            float curvedT = _breathCurve != null ? _breathCurve.Evaluate(progress01) : progress01;

            // Inhaling
            if (inhaling)
            {
                // Inhale: scale min -> max, color exhale -> inhale
                float s = Mathf.Lerp(MinScale, MaxScale, curvedT);
                fillCircle.transform.localScale = _initialScale * s;
                fillCircle.color = Color.Lerp(_exhaleColor, _inhaleColor, curvedT);

                if (instructionText != null)
                {
                    instructionText.text = "Breathe In";
                    instructionText.color = Color.Lerp(Color.white, _inhaleColor, curvedT);
                }
            }
            //Exhaling
            else
            {
                // Exhale: scale max -> min, color inhale -> exhale
                float s = Mathf.Lerp(MaxScale, MinScale, curvedT);
                fillCircle.transform.localScale = _initialScale * s;
                fillCircle.color = Color.Lerp(_inhaleColor, _exhaleColor, curvedT);

                if (instructionText != null)
                {
                    instructionText.text = "Breathe Out";
                    instructionText.color = Color.Lerp(Color.white, _exhaleColor, curvedT);
                }
            }
        }
    }
}
