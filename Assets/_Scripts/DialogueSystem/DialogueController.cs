using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace _Scripts.DialogueSystem
{
    public class DialogueController : MonoBehaviour
    {
        public static DialogueController instance { get; private set; }

        [Header("=== Dialogue Collection ===")]
        [SerializeField] private DialogueCollection dialogueCollection;

        [Header("=== Subtitle UI ===")]
        [SerializeField] private CanvasGroup subtitlePanel;
        [SerializeField] private TMP_Text subtitleText;

        [Header("=== Subtitle Fade ===")]
        [SerializeField] private float fadeSpeed = 0.25f;

        [Header("=== Debug ===")]
        [SerializeField] private bool showDebug = true;

        private readonly Dictionary<string, DialogueData> _dialogueCache = new Dictionary<string, DialogueData>();

        private Coroutine _currentRoutine;
        private bool _isPlaying;
        private DialogueData _currentData;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;

            CacheDialogueData();
            InitializeUI();
        }

        private void CacheDialogueData()
        {
            _dialogueCache.Clear();

            if (dialogueCollection == null || dialogueCollection.dialogues == null)
            {
                Debug.LogError("[DialogueSystem] DialogueCollection is missing.");
                return;
            }

            foreach (var d in dialogueCollection.dialogues)
            {
                if (d == null || string.IsNullOrEmpty(d.dialogueKey))
                    continue;

                if (_dialogueCache.ContainsKey(d.dialogueKey))
                {
                    Debug.LogWarning($"[DialogueSystem] Duplicate dialogue key: {d.dialogueKey}");
                    continue;
                }

                _dialogueCache.Add(d.dialogueKey, d);
            }

            if (showDebug)
                Debug.Log($"[DialogueSystem] Cached dialogues: {_dialogueCache.Count}");
        }

        private void InitializeUI()
        {
            if (subtitlePanel != null)
                subtitlePanel.alpha = 0f;

            if (subtitleText != null)
                subtitleText.text = string.Empty;
        }

        public void PlayDialogue(string dialogueKey)
        {
            if (string.IsNullOrEmpty(dialogueKey))
            {
                Debug.LogWarning("[DialogueSystem] PlayDialogue called with empty key.");
                return;
            }
            if (!_dialogueCache.TryGetValue(dialogueKey, out var data) || data == null)
            {
                Debug.LogWarning($"[DialogueSystem] Dialogue key not found: {dialogueKey}");
                return;
            }
            if (data == null)
            {
                Debug.LogWarning("[DialogueSystem] PlayDialogue called with null data.");
                return;
            }

            if (data.subtitleLines == null || data.subtitleLines.Count == 0)
            {
                Debug.LogWarning($"[DialogueSystem] Dialogue '{data.dialogueKey}' has no subtitleLines.");
                return;
            }
            StopDialogue();
            _currentData = data;
            _currentRoutine = StartCoroutine(PlayMultilineDialogueCoroutine(data));
        }
        private IEnumerator PlayMultilineDialogueCoroutine(DialogueData data)
        {
            _isPlaying = true;
            // Start VO sequence
            if (AudioManager.instance != null)
            {
                AudioManager.instance.PlayVoiceSequenceByKey(
                    data.dialogueKey,
                    data.subtitleLines
                );
            }
            // Subtitles timing 
            yield return StartCoroutine(ShowMultilineSubtitles(data.subtitleLines));
            _isPlaying = false;
            _currentData = null;
            _currentRoutine = null;
        }
        public void StopDialogue()
        {
            if (_currentRoutine != null)
            {
                StopCoroutine(_currentRoutine);
                _currentRoutine = null;
            }

            _isPlaying = false;
            _currentData = null;

            // Stop VO sequence
            if (AudioManager.instance != null)
            {
                AudioManager.instance.StopVoiceSequence();
            }

            // Reset UI
            if (subtitleText != null) subtitleText.text = string.Empty;
            if (subtitlePanel != null) subtitlePanel.alpha = 0f;
        }



        private IEnumerator ShowMultilineSubtitles(List<SubtitleLine> lines)
        {
            if (lines == null || lines.Count == 0) yield break;
            
            if (subtitlePanel == null || subtitleText == null)
            {
                foreach (var line in lines)
                    yield return new WaitForSeconds(GetLineDuration(line));
                yield break;
            }

            // Fade In
            yield return StartCoroutine(FadeSubtitle(0f, 1f));

            foreach (var line in lines)
            {
                subtitleText.text = line.content;
                yield return new WaitForSeconds(GetLineDuration(line));
            }

            // Fade Out
            yield return StartCoroutine(FadeSubtitle(1f, 0f));

            subtitleText.text = string.Empty;
        }

        private IEnumerator FadeSubtitle(float from, float to)
        {
            float elapsed = 0f;
            float dur = Mathf.Max(0.01f, fadeSpeed);

            while (elapsed < dur)
            {
                elapsed += Time.deltaTime;
                subtitlePanel.alpha = Mathf.Lerp(from, to, elapsed / dur);
                yield return null;
            }

            subtitlePanel.alpha = to;
        }

        private float GetLineDuration(SubtitleLine line)
        {
            if (line == null) return 0.05f;

            float dur;
            if (line.overrideDuration > 0f) dur = line.overrideDuration;
            else if (line.clip != null) dur = line.clip.length;
            else dur = EstimateByText(line.content);

            dur += Mathf.Max(0f, line.postDelay);
            return Mathf.Clamp(dur, 0.05f, 30f);
        }

        private float EstimateByText(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return 1.2f;

            // 简单稳定：英文按词，中文按字符兜底
            int wordCount = text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
            if (wordCount <= 1)
                wordCount = Mathf.Clamp(text.Length / 3, 1, 30);

            float wordsPerSecond = 200f / 60f;
            return Mathf.Clamp(wordCount / wordsPerSecond, 1.0f, 6.0f);
        }

        public bool IsPlaying()
        {
            return _isPlaying;
        }

        public DialogueData GetCurrentDialogue()
        {
            return _currentData;
        }
        
        
    }
}
