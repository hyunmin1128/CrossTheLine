using System;
using UnityEngine;
using UnityEngine.Audio;

namespace CrossTheLine.Cana
{
    public enum CanaSound { Pop, Bubble, Money, Item, Serve, Pray, Doorbell, Coin, Mop, WaterBomb }
    [Serializable]
    public sealed class CanaSoundSetting
    {
        public CanaSound sound;
        public AudioClip clip;
        [Range(0, 1)] public float volume = .35f;
    }

    [DefaultExecutionOrder(-1000)]
    public sealed class CanaAudio : MonoBehaviour
    {
        public static CanaAudio Instance { get; private set; }
        public CanaShop shop;
        public CanaRound round;
        public AudioMixerGroup outputGroup;
        [Range(0, 1)] public float masterVolume = .8f;
        [Range(0, 1)] public float uiVolume = 1;
        [Range(0, 1)] public float gameplayVolume = 1;
        public CanaSoundSetting[] sounds;
        [Min(4)] public int voiceCount = 16;
        [Min(0)] public float prayerFadeSeconds = .8f;
        [Min(.01f)] public float mopFadeSeconds = .09f;
        public event Action<CanaSound> SoundPlayed;
        public readonly int[] PlayedCounts = new int[10];
        public int LastPlayFrame { get; private set; }
        sealed class Voice
        {
            public AudioSource source;
            public CanaSoundSetting setting;
            public MonoBehaviour cleaner;
            public bool active, paused, ui, fading;
            public float age, fadeElapsed, fadeDuration;
        }
        Voice[] voices;
        static bool IsUI(CanaSound sound) => sound == CanaSound.Pop || sound == CanaSound.Bubble || sound == CanaSound.Money;
        void Awake()
        {
            Instance = this;
            voices = new Voice[Mathf.Max(4, voiceCount)];
            for (int i = 0; i < voices.Length; i++)
            {
                var go = new GameObject("SFX Voice " + (i + 1));
                go.transform.SetParent(transform, false);
                var source = go.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.spatialBlend = 0;
                source.dopplerLevel = 0;
                source.outputAudioMixerGroup = outputGroup;
                voices[i] = new Voice { source = source };
            }
        }
        public static void Play(CanaSound sound) { if (Instance) Instance.PlaySound(sound); }
        public void PlaySound(CanaSound sound) => StartVoice(sound, null);
        Voice StartVoice(CanaSound sound, MonoBehaviour cleaner)
        {
            if (voices == null || sounds == null) return null;
            CanaSoundSetting setting = Array.Find(sounds, s => s.sound == sound);
            if (setting == null || !setting.clip) return null;
            Voice voice = Array.Find(voices, v => !v.active);
            if (voice == null)
            {
                foreach (var v in voices)
                    if (!v.source.loop && (voice == null || v.age > voice.age)) voice = v;
            }
            if (voice == null) return null;
            voice.source.Stop();
            voice.setting = setting;
            voice.cleaner = cleaner;
            voice.active = true;
            voice.paused = voice.fading = false;
            voice.age = voice.fadeElapsed = 0;
            voice.ui = IsUI(sound);
            voice.source.ignoreListenerPause = voice.ui;
            voice.source.loop = cleaner != null;
            voice.source.clip = setting.clip;
            voice.source.volume = Gain(voice);
            voice.source.Play();
            PlayedCounts[(int)sound]++;
            LastPlayFrame = Time.frameCount;
            SoundPlayed?.Invoke(sound);
            return voice;
        }
        float Gain(Voice voice) => masterVolume * voice.setting.volume * (voice.ui ? uiVolume : gameplayVolume);
        public static void BeginMop(MonoBehaviour cleaner)
        {
            if (!Instance || !cleaner) return;
            foreach (var v in Instance.voices) if (v.active && v.cleaner == cleaner && !v.fading) return;
            Instance.StartVoice(CanaSound.Mop, cleaner);
        }
        public static void EndMop(MonoBehaviour cleaner)
        {
            if (!Instance || Instance.voices == null) return;
            foreach (var v in Instance.voices)
                if (v.active && v.cleaner == cleaner && !v.fading) Instance.Fade(v, Instance.mopFadeSeconds);
        }
        void Fade(Voice voice, float seconds)
        {
            voice.fading = true;
            voice.fadeElapsed = 0;
            voice.fadeDuration = Mathf.Max(.01f, seconds);
        }
        void Update()
        {
            foreach (var voice in voices)
            {
                if (!voice.active) continue;
                if (voice.source.loop && (!voice.cleaner || !voice.cleaner.gameObject.activeInHierarchy || (round && round.Finished)) && !voice.fading)
                    Fade(voice, mopFadeSeconds);
                bool pause = !voice.ui && shop && shop.IsOpen;
                if (pause && !voice.paused) { voice.source.Pause(); voice.paused = true; }
                else if (!pause && voice.paused) { voice.source.UnPause(); voice.paused = false; }
                voice.age += voice.ui ? Time.unscaledDeltaTime : Time.deltaTime;
                float envelope = 1;
                if (!voice.source.loop && voice.setting.sound == CanaSound.Pray && prayerFadeSeconds > 0)
                {
                    float remaining = voice.setting.clip.length - voice.source.time;
                    envelope *= Mathf.SmoothStep(0, 1, Mathf.Clamp01(remaining / prayerFadeSeconds));
                }
                if (voice.fading)
                {
                    voice.fadeElapsed += Time.unscaledDeltaTime;
                    envelope *= 1 - Mathf.SmoothStep(0, 1, Mathf.Clamp01(voice.fadeElapsed / voice.fadeDuration));
                }
                voice.source.volume = Gain(voice) * envelope;
                if ((voice.fading && voice.fadeElapsed >= voice.fadeDuration) ||
                    (!voice.paused && !voice.source.isPlaying && voice.age > .05f))
                {
                    voice.source.Stop();
                    voice.active = false;
                    voice.cleaner = null;
                }
            }
        }
        void OnDestroy() { if (Instance == this) Instance = null; }
    }
}
