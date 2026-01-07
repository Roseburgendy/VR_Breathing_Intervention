using UnityEngine;
using System.Collections.Generic;

namespace _Scripts.DialogueSystem
{
    /// <summary>
    /// ScriptableObject that holds a collection of DialogueData
    /// Allows organizing dialogues by phase or theme
    /// </summary>
    [CreateAssetMenu(fileName = "DialogueCollection", menuName = "FlowSpring/Narrative/Dialogue Collection")]
    public class DialogueCollection : ScriptableObject
    {
        [Header("Dialogues")]
        [Tooltip("All dialogue data in this collection")]
        public List<DialogueData> dialogues = new List<DialogueData>();

        /// <summary>
        /// Validate collection
        /// </summary>
        void OnValidate()
        {
            // Remove null entries
            dialogues.RemoveAll(d => d == null);
            
            // Check for duplicate keys
            HashSet<string> keys = new HashSet<string>();
            foreach (var dialogue in dialogues)
            {
                if (dialogue != null && !string.IsNullOrEmpty(dialogue.dialogueKey))
                {
                    if (keys.Contains(dialogue.dialogueKey))
                    {
                        Debug.LogWarning($"[DialogueCollection] Duplicate key '{dialogue.dialogueKey}' found!", this);
                    }
                    keys.Add(dialogue.dialogueKey);
                }
            }
        }
    }
}