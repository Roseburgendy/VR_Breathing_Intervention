using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using _Scripts.DialogueSystem;
using _Scripts.NarrativeSystem;

namespace _Scripts
{
    [System.Serializable]
    public class Sound
    {
        public string name;
        public AudioClip clip;

        [HideInInspector] public AudioSource source;
    }
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager instance { get; private set; }

        [Header("MUSIC")]
        [SerializeField] private SoundCategory musicCategory = new SoundCategory("Music");

        [Header("SFX")]
        [SerializeField] private SoundCategory sfxCategory = new SoundCategory("SFX");

        [Header("VOICE")]
        [SerializeField] private SoundCategory voiceCategory = new SoundCategory("Voice");

        [Header("AMBIENT")]
        [SerializeField] private SoundCategory ambientCategory = new SoundCategory("Ambient");

        [Header("Master Volume")]
        [Range(0f, 1f)]
        [SerializeField] private float masterVolume = 1f;

        [Header("Category Volumes")]
        [Range(0f, 1f)] [SerializeField] private float musicVolume;
        [Range(0f, 1f)] [SerializeField] private float sfxVolume;
        [Range(0f, 1f)] [SerializeField] private float voiceVolume;
        [Range(0f, 1f)] [SerializeField] private float ambientVolume;
        
        private readonly Dictionary<string, Sound> _soundDictionary = new Dictionary<string, Sound>();
        private Sound _currentMusic;
        private Sound _currentAmbientA;
        private Sound _currentAmbientB;


        // === Voice Sequence (Dialogue Multiline) ===
        private Coroutine _voiceSequenceCoroutine;
        private AudioSource _voiceSequenceSource;
        private string _currentDialogueKey;
        private float _voiceSequenceVolumeMultiplier = 1f;

        void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
            Initialize();
        }
        private void Start()
        {
            // Ambience start globally
            PlayGlobalAmbience();

            // Subscribe to Phase start event
            if (PhaseManager.Instance != null)
            {
                PhaseManager.Instance.OnPhaseStarted += OnPhaseStarted;
            }
        }

        public void OnPhaseStarted(int phase)
        {
            switch (phase)
            {
                case 2:
                    Play("level2Music");
                    break;

                case 3:
                    Play("level3Music");
                    break;
            }
        }
        void Initialize()
        {
            if (FindObjectOfType<AudioListener>() == null)
                Debug.LogError("[AudioManager] No AudioListener found in scene.");

            CreateAudioSources(musicCategory, loop: true, categoryVol: musicVolume);
            CreateAudioSources(ambientCategory, loop: true, categoryVol: ambientVolume);
            CreateAudioSources(sfxCategory, loop: false, categoryVol: sfxVolume);
            CreateAudioSources(voiceCategory, loop: false, categoryVol: voiceVolume);

            BuildDictionary(musicCategory);
            BuildDictionary(ambientCategory);
            BuildDictionary(sfxCategory);
            BuildDictionary(voiceCategory);
            
            _voiceSequenceSource = gameObject.AddComponent<AudioSource>();
            _voiceSequenceSource.playOnAwake = false;
            _voiceSequenceSource.loop = false;
            _voiceSequenceSource.spatialBlend = 0f;
            _voiceSequenceSource.pitch = 1f;

            ApplyVolumes();
        }

        void CreateAudioSources(SoundCategory category, bool loop, float categoryVol)
        {
            if (category.sounds == null) return;

            foreach (var sound in category.sounds)
            {
                if (sound == null || sound.clip == null || string.IsNullOrEmpty(sound.name))
                    continue;
                var go = new GameObject($"Audio_{category.categoryName}_{sound.name}");
                go.transform.SetParent(transform);
                var src = go.AddComponent<AudioSource>();
                
                // Audio Source Configuration
                src.clip = sound.clip;
                src.playOnAwake = false;
                src.loop = loop;
                src.spatialBlend = 0f;
                src.pitch = 1f;
                src.volume = Mathf.Clamp01(masterVolume * categoryVol);

                sound.source = src;
            }
        }

        private void BuildDictionary(SoundCategory category)
        {
            if (category.sounds == null) return;

            foreach (var sound in category.sounds)
            {
                if (sound == null || string.IsNullOrEmpty(sound.name)) continue;
                if (_soundDictionary.ContainsKey(sound.name))
                {
                    Debug.LogWarning($"[AudioManager] Duplicate sound name: {sound.name}");
                    continue;
                }
                _soundDictionary.Add(sound.name, sound);
            }
        }

        public void Play(string soundName)
        {
            if (!_soundDictionary.TryGetValue(soundName, out var sound) || sound.source == null)
            {
                Debug.LogWarning($"[AudioManager] Sound not found or has no source: {soundName}");
                return;
            } 
            // SFX/Voice
            ApplyVolumes();
            sound.source.Play();
        }
        void ApplyVolumes()
        {
            ApplyCategoryVolume(musicCategory, musicVolume);
            ApplyCategoryVolume(sfxCategory, sfxVolume);
            ApplyCategoryVolume(voiceCategory, voiceVolume);
            ApplyCategoryVolume(ambientCategory, ambientVolume);
        }
        void ApplyCategoryVolume(SoundCategory category, float categoryVol)
        {
            if (category.sounds == null) return;

            float vol = Mathf.Clamp01(masterVolume * categoryVol);
            foreach (var s in category.sounds)
            {
                if (s?.source == null) continue;
                s.source.volume = vol;
            }
        }
        public void Stop(string soundName)
        {
            if (_soundDictionary.TryGetValue(soundName, out var sound) && sound.source != null)
                sound.source.Stop();
        }
        // Existing key-based single voice (keep for other systems if needed)
        public void PlayVoiceByKey(string voiceKey, float volumeMultiplier = 1f)
        {
            if (string.IsNullOrEmpty(voiceKey)) return;
            if (!_soundDictionary.TryGetValue(voiceKey, out var sound) || sound?.source == null || sound.clip == null)
            {
                Debug.LogWarning($"[AudioManager] Voice key not found or no clip: {voiceKey}");
                return;
            }
            if (!voiceCategory.Contains(sound))
            {
                Debug.LogWarning($"[AudioManager] '{voiceKey}' is not in Voice category.");
                return;
            }
            sound.source.volume = Mathf.Clamp01(masterVolume * voiceVolume * volumeMultiplier);
            sound.source.Stop();
            sound.source.Play();
        }
        
        // Dialogue multiline voice sequence 
        public void PlayVoiceSequenceByKey(string dialogueKey, List<SubtitleLine> lines)
        {
            if (lines == null || lines.Count == 0) return;
            StopVoiceSequence();
            _currentDialogueKey = dialogueKey;
            _voiceSequenceCoroutine = StartCoroutine(PlayVoiceSequenceCoroutine(lines));
        }
        private IEnumerator PlayVoiceSequenceCoroutine(List<SubtitleLine> lines)
        {
            foreach (var line in lines)
            {
                if (line == null) continue;
                if (line.clip != null) // If has clip, play it
                {
                    _voiceSequenceSource.Stop();
                    _voiceSequenceSource.clip = line.clip;
                    _voiceSequenceSource.Play();
                    float wait = (line.overrideDuration > 0f) ? line.overrideDuration : line.clip.length;
                    yield return new WaitForSeconds(Mathf.Max(0.01f, wait));
                }
                else // No clip: wait by overrideDuration, otherwise minimal wait
                {
                    float wait = (line.overrideDuration > 0f) ? line.overrideDuration : 0.5f;
                    yield return new WaitForSeconds(Mathf.Max(0.01f, wait));
                }
                if (line.postDelay > 0f) // Post delays
                    yield return new WaitForSeconds(line.postDelay);
            }
            _voiceSequenceCoroutine = null;
            _currentDialogueKey = null;
        }

        public void StopVoiceSequence()
        {
            if (_voiceSequenceCoroutine != null)
            {
                StopCoroutine(_voiceSequenceCoroutine);
                _voiceSequenceCoroutine = null;
            }

            if (_voiceSequenceSource != null)
            {
                _voiceSequenceSource.Stop();
                _voiceSequenceSource.clip = null;
            }

            _currentDialogueKey = null;
        }
        

        private void PlayGlobalAmbience()
        {
                PlayAmbient("amb_wind", 0);
            
                PlayAmbient("amb_waterfall", 1);
        }
        private void PlayAmbient(string soundName, int slot)
        {
            if (!_soundDictionary.TryGetValue(soundName, out var sound) || sound.source == null)
            {
                Debug.LogWarning($"[AudioManager] Ambient not found or has no source: {soundName}");
                return;
            }
            if (!ambientCategory.Contains(sound)) return;
            if (slot == 0)
            {
                _currentAmbientA = sound;
            }
            else
            {
                _currentAmbientB = sound;
            }
            ApplyVolumes();
            sound.source.Play();
        }

        public void StopMusic()
        {
            if (_currentMusic != null && _currentMusic.source != null)
            {
                _currentMusic.source.Stop();
                _currentMusic = null;
            }
        }
        private void StopAmbient(int slot)
        {
            Sound target = (slot == 0) ? _currentAmbientA : _currentAmbientB;

            if (target != null && target.source != null)
            {
                target.source.Stop();

                if (slot == 0) _currentAmbientA = null;
                else _currentAmbientB = null;
            }
        }
        public void StopAllAmbients()
        {
            StopAmbient(0);
            StopAmbient(1);
        }
        
    }
}
