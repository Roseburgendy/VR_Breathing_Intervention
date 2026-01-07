using System;
using System.Linq;

namespace _Scripts
{
    [System.Serializable]
    public class SoundCategory
    {
        public string categoryName;
        public Sound[] sounds;

        public SoundCategory(string name)
        {
            categoryName = name;
            sounds = Array.Empty<Sound>();
        }
        public bool Contains(Sound sound)
        {
            if (sounds == null) return false;
            return sounds.Any(t => t == sound);
        }
    }
}