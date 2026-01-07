using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;
using _Scripts;
using UnityEngine.EventSystems;

namespace _Scripts.UI
{
    /// <summary>
    /// Main menu controller for GameStart scene
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        [Header("Scene to Load")]
        [SerializeField] private string playSceneName = "PlayScene";
        [SerializeField] private string trainingSceneName = "TrainingScene";
        
        
        [Header("UI Elements")]
        [SerializeField] private Button startNarrativeButton;
        [SerializeField] private Button startTrainingButton;
        [SerializeField] private Button aboutButton;
        [SerializeField] private Button quitButton;
        [SerializeField] private Button returnButton;
        
        [SerializeField] private CanvasGroup menuCanvasGroup;
        [SerializeField] private GameObject aboutPanel;
        [SerializeField] private GameObject menuPanel;
        
        [Header("Title Animation")]
        [SerializeField] private TextMeshPro titleText;

        [SerializeField] private float titleFadeInDuration;
        [SerializeField] private float titleFloatAmount;
        [SerializeField] private float titleFloatDuration;

        #region Unity Lifecycle
        
        void Start()
        {
            SetupButtons();
            PlayIntroAnimation();
            PlayMenuMusic();
        }
        
        #endregion
        
        #region Initialization
        
        void SetupButtons()
        {
            if (startNarrativeButton != null)
            {
                startNarrativeButton.onClick.AddListener(OnStartNarrativeButtonClicked);
                AddHoverSound(startNarrativeButton);
            }

            if (startTrainingButton != null)
            {
                startTrainingButton.onClick.AddListener(OnStartTrainingButtonClicked);
                AddHoverSound(startTrainingButton);
            }

            if (aboutButton != null)
            {
                aboutButton.onClick.AddListener(OnAboutButtonClicked);
                AddHoverSound(aboutButton);
            }

            if (quitButton != null)
            {
                quitButton.onClick.AddListener(OnQuitButtonClicked);
                AddHoverSound(quitButton);
            }

            if (returnButton != null)
            {
                returnButton.onClick.AddListener(OnReturnButtonClicked);
                AddHoverSound(returnButton);
            }

            aboutPanel.SetActive(false);
            menuPanel.SetActive(true);
        }

        #endregion
        
        #region Animations
        
        void PlayIntroAnimation()
        {
            if (menuCanvasGroup != null)
            {
                // Fade in menu
                menuCanvasGroup.alpha = 0f;
                menuCanvasGroup.DOFade(1f, titleFadeInDuration);
            }
            
            if (titleText != null)
            {
                // Title float animation
                RectTransform titleRect = titleText.GetComponent<RectTransform>();
                if (titleRect != null)
                {
                    Vector2 originalPos = titleRect.anchoredPosition;
                    
                    // Fade in
                    titleText.DOFade(0f, 0f);
                    titleText.DOFade(1f, titleFadeInDuration);
                    
                    // Float up and down
                    DOTween.Sequence()
                        .Append(titleRect.DOAnchorPosY(originalPos.y + titleFloatAmount, titleFloatDuration)
                            .SetEase(Ease.InOutSine))
                        .Append(titleRect.DOAnchorPosY(originalPos.y, titleFloatDuration)
                            .SetEase(Ease.InOutSine))
                        .SetLoops(-1);
                }
            }
        }
        
        #endregion
        
        #region Button Callbacks
        
        void OnStartNarrativeButtonClicked()
        {
            PlayButtonClickSound();
            
            // Disable all buttons
            SetButtonsInteractable(false);

            TransitionManager.instance.TransitionToLevel(playSceneName);
        }
        void OnStartTrainingButtonClicked()
        {
            PlayButtonClickSound();
            
            // Disable all buttons
            SetButtonsInteractable(false);

            TransitionManager.instance.TransitionToLevel(trainingSceneName);
        }
        
        void OnAboutButtonClicked()
        {

            PlayButtonClickSound();
            
            // TODO: Open About panel
            EnableAboutPanel();
        }
        
        void OnReturnButtonClicked()
        {
            PlayButtonClickSound();
            EnableMainMenu();
        }
        public void EnableMainMenu()
        {
            menuPanel.SetActive(true);
            aboutPanel.SetActive(false);
        }
        public void EnableAboutPanel()
        {
            menuPanel.SetActive(false);
            aboutPanel.SetActive(true);
        }
        
        void OnQuitButtonClicked()
        {

            PlayButtonClickSound();
            
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #else
            Application.Quit();
            #endif
        }
        
        #endregion
        
        
        #region Audio
        
        void PlayMenuMusic()
        {
            AudioManager.instance.Play("menuMusic");
        }
        
        void PlayButtonClickSound()
        {
            AudioManager.instance.Play("buttonClick");
        }
        
        
        #endregion
        
        #region Helpers
        
        void SetButtonsInteractable(bool interactable)
        {
            if (startNarrativeButton != null) startNarrativeButton.interactable = interactable;
            if (startTrainingButton != null) startTrainingButton.interactable = interactable;
            if (aboutButton != null) aboutButton.interactable = interactable;
            if (quitButton != null) quitButton.interactable = interactable;
            if (returnButton != null) returnButton.interactable = interactable;
        }
        void AddHoverSound(Button btn)
        {
            var trigger = btn.GetComponent<EventTrigger>();
            if (trigger == null) trigger = btn.gameObject.AddComponent<EventTrigger>();

            if (trigger.triggers == null) trigger.triggers = new System.Collections.Generic.List<EventTrigger.Entry>();

            var entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            entry.callback.AddListener((_) =>
            {
                // 不可交互时不播
                if (!btn.interactable) return;

                PlayButtonHoverSound();
            });

            trigger.triggers.Add(entry);
        }

        void PlayButtonHoverSound()
        {
            // 与 click 同一个
            AudioManager.instance.Play("buttonHover");
        }

        
        #endregion
    }
}