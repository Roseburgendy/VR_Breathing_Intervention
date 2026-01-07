using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _Scripts
{
    public class TransitionManager : MonoBehaviour
    {
        public static TransitionManager instance { get; private set; }

        [Header("Fade")]
        [SerializeField] private FadeScreen fadeScreen;
        [SerializeField] private float sceneLoadDelay = 0.1f;

        [Header("Audio (Optional)")]
        [Tooltip("Sound name/key in AudioManager for menu music (Music category).")]
        [SerializeField] private string menuMusicKey;
        [SerializeField] private string level1AmbienceKey;
        
        [Tooltip("If true, stop menu music when transitioning away from menu.")]
        [SerializeField] private bool stopMenuMusicOnTransition = true;

        [Tooltip("If true, start ambience after scene is loaded (recommended).")]
        [SerializeField] private bool startAmbienceAfterLoad = true;

        [Header("Debug")]
        [SerializeField] private bool showDebug = true;

        private bool _isTransitioning;

        void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);

            if (fadeScreen == null)
                fadeScreen = FindObjectOfType<FadeScreen>();
        }

        /// <summary>
        /// Menu -> Main/Level transition: fade out, audio switch, load scene, ambience, fade in.
        /// </summary>
        public void TransitionToLevel(string sceneName)
        {
            if (_isTransitioning)
            {
                if (showDebug) Debug.LogWarning("[TransitionManager] Transition already in progress.");
                return;
            }

            StartCoroutine(TransitionToLevelRoutine(sceneName));
        }

        private IEnumerator TransitionToLevelRoutine(string sceneName)
        {
            _isTransitioning = true;

            if (showDebug)
                Debug.Log($"[TransitionManager] TransitionToLevel: {sceneName}");

            // 1) Fade out
            if (fadeScreen != null)
            {
                fadeScreen.FadeOut();
                yield return new WaitForSeconds(fadeScreen.fadeDuration + sceneLoadDelay);
            }

            // 2) Audio switch before load (prevents overlap during black)
            if (AudioManager.instance != null)
            {
                if (stopMenuMusicOnTransition && !string.IsNullOrEmpty(menuMusicKey))
                    AudioManager.instance.Stop(menuMusicKey);
                if (!string.IsNullOrEmpty(level1AmbienceKey))
                    AudioManager.instance.Play(level1AmbienceKey);
            }

            // 3) Load scene
            AsyncOperation loadOp = SceneManager.LoadSceneAsync(sceneName);
            while (!loadOp.isDone)
                yield return null;

            // 4) Re-acquire FadeScreen in new scene (important)
            fadeScreen = FindObjectOfType<FadeScreen>();
            

            // 6) Fade in
            if (fadeScreen != null)
            {
                fadeScreen.FadeIn();
                yield return new WaitForSeconds(fadeScreen.fadeDuration);
            }

            _isTransitioning = false;

            if (showDebug)
                Debug.Log("[TransitionManager] Transition complete.");
        }
    }
}
