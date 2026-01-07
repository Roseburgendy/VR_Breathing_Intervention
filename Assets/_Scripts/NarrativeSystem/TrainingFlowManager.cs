using System.Collections;
using UnityEngine;
using _Scripts.BreathGuideSystem; 
using _Scripts.DialogueSystem; 

[System.Serializable]
public class TrainingPhaseConfig
{
    public string phaseName;
    public MovementType inhaleMovement;
    public MovementType exhaleMovement;

    public int targetCycles = 5;              // 现在建议语义 = 需要完成的“完整呼吸次数”
    public string startVOKey;

    public string[] progressKeys;             // 用 BreathVoHelper 随机播 ）
}


namespace _Scripts.NarrativeSystem 
{
    public class TrainingFlowManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private BreathRhythmController rhythmController;

        [Header("Training Plan (3 phases)")]
        [SerializeField] private TrainingPhaseConfig[] phases = new TrainingPhaseConfig[3];

        [Header("UI (optional)")]
        [SerializeField] private GameObject breathingUI;
        [SerializeField] private GameObject completionUI; 
        
        [Header("Gaze Gate")]
        [SerializeField] private GazeHoldUI gazeHoldUI; 
        [SerializeField] private bool useGazeBetweenPhases = true;

        
        private int currentPhaseIndex = -1;
        private bool isRunning = false;

        private void Awake()
        {
            if (rhythmController == null)
                rhythmController = FindObjectOfType<BreathRhythmController>(true);
        }

        private void OnEnable()
        {
            rhythmController.OnTargetCyclesReached += HandlePhaseCompleted;
            rhythmController.OnFullBreathStarted += HandleFullBreathStarted;
        }

        private void OnDisable()
        {
            rhythmController.OnTargetCyclesReached -= HandlePhaseCompleted;
            rhythmController.OnFullBreathStarted -= HandleFullBreathStarted;
        }



        private void Start()
        {
            StartTraining();
            AudioManager.instance.Play("level1Music");
            // Hide completion UI
            if (completionUI != null)
            {
                completionUI.SetActive(false);
            }
        }
        
        private IEnumerator CoRunGazeGate()
        {
            if (!useGazeBetweenPhases || gazeHoldUI == null)
                yield break;

            // 如果呼吸UI在显示，先关
            if (breathingUI != null) breathingUI.SetActive(false);

            bool done = false;

            // 订阅一次性事件
            System.Action onDone = () => done = true;
            gazeHoldUI.OnCompleted += onDone;

            // 开始 gaze
            gazeHoldUI.Begin();

            // 等完成
            while (!done) yield return null;

            // 清理订阅（必须）
            gazeHoldUI.OnCompleted -= onDone;
        }

        
        private void HandleFullBreathStarted(int startedFullBreaths)
        {
            if (currentPhaseIndex < 0 || currentPhaseIndex >= phases.Length) return;

            var cfg = phases[currentPhaseIndex];
            if (cfg.progressKeys == null || cfg.progressKeys.Length == 0) return;

            // 只在第 1 和第 3 个 cycle “开始”时播
            if (startedFullBreaths != 1 && startedFullBreaths != 3
                && startedFullBreaths != 5) return;
            
            if (startedFullBreaths > cfg.targetCycles) return;

            BreathVoHelper.instance?.TryPlayRandom(cfg.progressKeys);
        }


        public void StartTraining()
        {
            if (isRunning) return;
            isRunning = true;

            if (breathingUI != null) breathingUI.SetActive(false);

            // 先播总任务说明（无叙事）
            StartCoroutine(CoPlayIntroAndStart());
        }

        private IEnumerator CoPlayIntroAndStart()
        {
            yield return PlayVOAndWait("training_intro");
            yield return StartCoroutine(CoAdvanceToNextPhase()); // 统一入口
        }

        private IEnumerator CoAdvanceToNextPhase()
        {
            currentPhaseIndex++;

            if (currentPhaseIndex >= phases.Length)
            {
                EndTraining();
                yield break;
            }

            // Phase2/Phase3 进入前插 gaze（也就是 phaseIndex 1 和 2）
            if (currentPhaseIndex > 0)
                yield return StartCoroutine(CoRunGazeGate());

            var cfg = phases[currentPhaseIndex];

            int phaseNumber = currentPhaseIndex + 1;
            AudioManager.instance.OnPhaseStarted(phaseNumber);

            rhythmController.SetMovementPatterns(cfg.inhaleMovement, cfg.exhaleMovement);
            rhythmController.SetTargetCycles(cfg.targetCycles);

            yield return StartCoroutine(CoStartPhase(cfg));
        }


        private void StartNextPhase()
        {
            currentPhaseIndex++;

            if (currentPhaseIndex >= phases.Length)
            {
                EndTraining();
                return;
            }

            var cfg = phases[currentPhaseIndex];
            
            int phaseNumber = currentPhaseIndex + 1; // 1,2,3
            AudioManager.instance?.OnPhaseStarted(phaseNumber);

            // 1) 配置呼吸模式（Phase1/2/3 的差异点）
            rhythmController.SetMovementPatterns(cfg.inhaleMovement, cfg.exhaleMovement);

            // 2) 配置目标 cycles（保持与现有逻辑一致）
            rhythmController.SetTargetCycles(cfg.targetCycles);

            // 3) 开始 VO（任务陈述）
            StartCoroutine(CoStartPhase(cfg));
        }

        private IEnumerator CoStartPhase(TrainingPhaseConfig cfg)
        {
            yield return PlayVOAndWait(cfg.startVOKey);

            if (breathingUI != null) breathingUI.SetActive(true);

            if (!rhythmController.IsRunning())
                rhythmController.StartBreathCycle();
        }

        // 由 BreathRhythmController 在达成 targetCycles 时触发
        private void HandlePhaseCompleted()
        {
            var cfg = phases[currentPhaseIndex];
            if (breathingUI != null) breathingUI.SetActive(false);

            StartCoroutine(CoAfterPhaseCompleteAndAdvance(cfg));
        }

        private IEnumerator CoAfterPhaseCompleteAndAdvance(TrainingPhaseConfig cfg)
        {

            yield return new WaitForSeconds(0.9f);

            // 推进到下一 phase（这里会自动插 gaze）
            yield return StartCoroutine(CoAdvanceToNextPhase());
        }
        


        private void EndTraining()
        {
            isRunning = false;
            StartCoroutine(PlayVOAndWait("training_complete"));
            Invoke(nameof(ShowCompletionUI), 8f);
        }

        void ShowCompletionUI()
        {
            // Show completion UI
            if (completionUI != null)
            {
                completionUI.SetActive(true);
            }
        }

        /// <summary>
        /// 播放 VO 并等待其时长结束（沿用你现有 DialogueData.GetDuration() 逻辑）
        /// </summary>
        private IEnumerator PlayVOAndWait(string dialogueKey)
        {
            if (string.IsNullOrEmpty(dialogueKey))
                yield break;

            DialogueController.instance.PlayDialogue(dialogueKey);

            // 等待 DialogueController 自己播完（包括多行字幕、postDelay、clip长度等）
            while (DialogueController.instance != null && DialogueController.instance.IsPlaying())
                yield return null;
        }
        
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
