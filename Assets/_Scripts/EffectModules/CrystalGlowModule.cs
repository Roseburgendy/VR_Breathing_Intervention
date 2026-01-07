using System.Collections;
using UnityEngine;
using UnityEngine.Splines;

namespace _Scripts.EffectModules
{
    /// <summary>
    /// Handles crystal inhale/exhale visuals (emission + scale) and final effect:
    /// play multiple SplineAnimate "energy particles" that travel to points, then reveal crystals.
    /// </summary>
    public class CrystalGlowModule : MonoBehaviour
    {
        
        [Header("Final Effect (Spline + Reveal)")]
        [Tooltip("Your 4 particle objects that have SplineAnimate attached.")]
        [SerializeField] private SplineAnimate[] splineAnimators;

        [Tooltip("Crystals to reveal. If using reveal-per-arrival, index should match splineAnimators.")]
        [SerializeField] private GameObject[] crystalsToReveal;

        [Tooltip("If true: all splines start at the same time. If false: start one by one.")]
        [SerializeField] private bool playInParallel;

        [Tooltip("Delay between each spline start when playInParallel = false.")]
        [SerializeField] private float serialDelay;

        [Tooltip("If true: reveal matching crystal when each spline reaches end.")]
        [SerializeField] private bool revealOnEachArrival;

        [Tooltip("If true: ignore revealOnEachArrival and reveal all only after all splines complete.")]
        [SerializeField] private bool revealOnlyAfterAllArrive;
        
        
        private bool _finalPlayed;
        private int _completedCount;

        
        public void PlayCompletionEffect()
        {
            if (_finalPlayed) return;
            _finalPlayed = true;

            if (splineAnimators == null || splineAnimators.Length == 0)
            {
                RevealAllCrystals();
                return;
            }

            // Prepare animators for "one-shot" playback and reset to start
            for (int i = 0; i < splineAnimators.Length; i++)
            {
                var anim = splineAnimators[i];
                if (anim == null) continue;

                anim.PlayOnAwake = false;
                anim.Loop = SplineAnimate.LoopMode.Once;
                AudioManager.instance.Play("crystalRelease");
                // Reset to start without autoplay
                anim.Restart(false);
            }

            _completedCount = 0;

            if (playInParallel)
            {
                for (int i = 0; i < splineAnimators.Length; i++)
                    PlayOneSpline(i);
            }
            else
            {
                StartCoroutine(PlaySplinesSerial());
            }

            // If you want synchronous reveal at start (rare), you can do it here:
            // if (!revealOnlyAfterAllArrive && !revealOnEachArrival) RevealAllCrystals();
        }

        private IEnumerator PlaySplinesSerial()
        {
            for (int i = 0; i < splineAnimators.Length; i++)
            {
                PlayOneSpline(i);
                if (serialDelay > 0f) yield return new WaitForSeconds(serialDelay);
            }
        }

        private void PlayOneSpline(int index)
        {
            if (index < 0 || index >= splineAnimators.Length) return;

            var anim = splineAnimators[index];
            if (anim == null) return;

            // Important: subscribe BEFORE Play so end event is caught
            anim.Completed += () => HandleSplineCompleted(index);

            // Start from beginning
            anim.Restart(false);
            anim.Play();
            
        }

        private void HandleSplineCompleted(int index)
        {
            _completedCount++;
            

            // Reveal per arrival
            if (!revealOnlyAfterAllArrive && revealOnEachArrival)
            {
                RevealCrystalByIndex(index);
            }

            // Reveal after all completed
            if (revealOnlyAfterAllArrive && _completedCount >= CountValidAnimators())
            {
                RevealAllCrystals();
            }
        }

        private int CountValidAnimators()
        {
            int c = 0;
            for (int i = 0; i < splineAnimators.Length; i++)
                if (splineAnimators[i] != null) c++;
            return c;
        }

        private void RevealCrystalByIndex(int index)
        {
            if (crystalsToReveal == null) return;
            if (index < 0 || index >= crystalsToReveal.Length) return;

            var go = crystalsToReveal[index];
            if (go != null) go.SetActive(true);
        }

        private void RevealAllCrystals()
        {
            if (crystalsToReveal == null) return;

            for (int i = 0; i < crystalsToReveal.Length; i++)
            {
                if (crystalsToReveal[i] != null)
                    crystalsToReveal[i].SetActive(true);
            }
        }
    }
}
