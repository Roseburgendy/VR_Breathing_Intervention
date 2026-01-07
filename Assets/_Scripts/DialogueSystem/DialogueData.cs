using System;
using System.Collections.Generic;
using UnityEngine;

namespace _Scripts.DialogueSystem
{
    [Serializable]
    public class SubtitleLine
    {
        [TextArea(1, 4)]
        public string content;
        public AudioClip clip; 
        public float overrideDuration = 0f;
        public float postDelay = 0f; 
    }

    [CreateAssetMenu(fileName = "DialogueData", menuName = "FlowSpring/Narrative/Dialogue Data")]
    public class DialogueData : ScriptableObject
    {
        [Header("Identification")]
        public string dialogueKey;
        [Header("Subtitle Lines (Required)")]
        public List<SubtitleLine> subtitleLines = new List<SubtitleLine>();

        private void OnValidate()
        {
            if (string.IsNullOrEmpty(dialogueKey))
                Debug.LogWarning($"[DialogueData] '{name}' has no dialogueKey!", this);

            if (subtitleLines == null || subtitleLines.Count == 0)
                Debug.LogWarning($"[DialogueData] '{name}' has no subtitleLines!", this);
        }
    }
}