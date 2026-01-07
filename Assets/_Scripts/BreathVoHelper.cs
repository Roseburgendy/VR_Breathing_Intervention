using UnityEngine;
using _Scripts.DialogueSystem;

namespace _Scripts
{
    /// <summary>
    /// Lightweight helper to safely play VO lines
    /// - Avoids interrupting current dialogue
    /// - Supports random selection
    /// - Avoids immediate repetition
    /// </summary>
    public class BreathVoHelper : MonoBehaviour
    {
        public static BreathVoHelper instance { get; private set; }
        private string _lastPlayedKey;

        private void Awake()
        {
            if (instance != null)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
        }
        
        /// <summary>
        /// Randomly play one key from a list (avoids repeating last one)
        /// </summary>
        public void TryPlayRandom(string[] keys)
        {
            if (keys == null || keys.Length == 0) return;
            string selected;
            if (keys.Length == 1)
            {
                selected = keys[0];
            }
            else
            {
                int safety = 10;
                do
                {
                    selected = keys[Random.Range(0, keys.Length)];
                    safety--;
                }
                while (selected == _lastPlayedKey && safety > 0);
            }
            AudioManager.instance.Play(selected);
            _lastPlayedKey = selected;
        }
    }
}
