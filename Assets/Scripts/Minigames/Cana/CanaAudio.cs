using System;
using UnityEngine;
using UnityEngine.Audio;

namespace CrossTheLine.Cana
{
    public enum CanaSound { Pop, Bubble, Money, Item, Serve, Pray, Doorbell, Coin, Mop, WaterBomb, WaterPour, LockOn, Error, Dash1, Dash2, Dash3, Dash4, Teleport, Love, Good, Fail, RobotChange, Vacuum, Wow }
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
        [Header("Background music")]
        public AudioClip musicClip;
        public AudioSource musicSource;
        [Range(0, 1)] public float musicVolume = .16f;
        [Header("Water fill pitch")]
        [Range(.1f, 3)] public float waterPitchMin = 1;
        [Range(.1f, 3)] public float waterPitchMax = 1.5f;
        [Min(4)] public int voiceCount = 16;
        [Min(0)] public float prayerFadeSeconds = .8f;
        [Min(.01f)] public float mopFadeSeconds = .09f;
        public event Action<CanaSound> SoundPlayed;
        public readonly int[] PlayedCounts = new int[Enum.GetValues(typeof(CanaSound)).Length];
        public float LastPitch { get; private set; } = 1;
        public int LastPlayFrame { get; private set; }
        sealed class Voice
        {
            public AudioSource source;
            public CanaSoundSetting setting;
            public MonoBehaviour cleaner;
            public bool active, paused, ui, fading;
            public float age, fadeElapsed, fadeDuration, endAt;
        }
        Voice[] voices;
        static bool IsUI(CanaSound sound) => sound == CanaSound.Pop || sound == CanaSound.Bubble || sound == CanaSound.Money || sound == CanaSound.LockOn || sound == CanaSound.Error || sound == CanaSound.Wow;
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
            if (musicClip)
            {
                if (!musicSource)
                {
                    var musicObject = new GameObject("Music - Moonlight Swing");
                    musicObject.transform.SetParent(transform, false);
                    musicSource = musicObject.AddComponent<AudioSource>();
                }
                musicSource.playOnAwake = false;
                musicSource.spatialBlend = 0;
                musicSource.dopplerLevel = 0;
                musicSource.ignoreListenerPause = true;
                musicSource.loop = true;
                musicSource.clip = musicClip;
                musicSource.volume = masterVolume * musicVolume;
                musicSource.Play();
            }
        }
        public static void Play(CanaSound sound) { if (Instance) Instance.PlaySound(sound); }
        public void PlaySound(CanaSound sound)
        {
            if (sound == CanaSound.Teleport)
                foreach (var voice in voices)
                    if (voice.active && voice.setting.sound == sound && !voice.fading) Fade(voice, .05f);
            StartVoice(sound, null);
        }
        public static void PlayDash() => Play((CanaSound)((int)CanaSound.Dash1 + UnityEngine.Random.Range(0, 4)));
        public static void RobotTransform(MonoBehaviour robot, float seconds)
        {
            if (!Instance) return;
            StopRobotTransform(robot);
            var setting = Array.Find(Instance.sounds, s => s.sound == CanaSound.RobotChange);
            if (setting == null || !setting.clip) return;
            float duration = Mathf.Max(.01f, seconds);
            float pitch = Mathf.Max(1, setting.clip.length / duration);
            var voice = Instance.StartVoice(CanaSound.RobotChange, null, pitch);
            if (voice != null) { voice.cleaner = robot; voice.endAt = duration; }
        }
        public static void StopRobotTransform(MonoBehaviour robot)
        {
            if (!Instance || Instance.voices == null) return;
            foreach (var voice in Instance.voices)
                if (voice.active && voice.cleaner == robot && !voice.source.loop && voice.setting.sound == CanaSound.RobotChange && !voice.fading)
                    Instance.Fade(voice, .03f);
        }
        public static void WaterFilled(int portions, int capacity)
        {
            if (!Instance) return;
            float progress = capacity <= 1 ? 1 : Mathf.Clamp01((float)(portions - 1) / (capacity - 1));
            float pitch = Mathf.Lerp(Instance.waterPitchMin, Mathf.Max(Instance.waterPitchMin, Instance.waterPitchMax), progress);
            Instance.StartVoice(CanaSound.WaterPour, null, pitch);
        }
        Voice StartVoice(CanaSound sound, MonoBehaviour cleaner, float pitch = 1)
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
            voice.age = voice.fadeElapsed = voice.endAt = 0;
            voice.ui = IsUI(sound);
            voice.source.ignoreListenerPause = voice.ui;
            voice.source.loop = cleaner != null;
            voice.source.clip = setting.clip;
            voice.source.pitch = Mathf.Clamp(pitch, .1f, 3);
            LastPitch = voice.source.pitch;
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
            foreach (var v in Instance.voices) if (v.active && v.source.loop && v.cleaner == cleaner && !v.fading) return;
            Instance.StartVoice(cleaner is CanaPartTimer worker && worker.kind == CanaStaffKind.Robot ? CanaSound.Vacuum : CanaSound.Mop, cleaner);
        }
        public static void EndMop(MonoBehaviour cleaner)
        {
            if (!Instance || Instance.voices == null) return;
            foreach (var v in Instance.voices)
                if (v.active && v.source.loop && v.cleaner == cleaner && !v.fading) Instance.Fade(v, Instance.mopFadeSeconds);
        }
        void Fade(Voice voice, float seconds)
        {
            voice.fading = true;
            voice.fadeElapsed = 0;
            voice.fadeDuration = Mathf.Max(.01f, seconds);
        }
        void Update()
        {
            if (musicSource) musicSource.volume = masterVolume * musicVolume;
            foreach (var voice in voices)
            {
                if (!voice.active) continue;
                if (voice.source.loop && (!voice.cleaner || !voice.cleaner.gameObject.activeInHierarchy || (round && round.Finished)) && !voice.fading)
                    Fade(voice, mopFadeSeconds);
                bool pause = !voice.ui && shop && shop.IsOpen;
                if (pause && !voice.paused) { voice.source.Pause(); voice.paused = true; }
                else if (!pause && voice.paused) { voice.source.UnPause(); voice.paused = false; }
                voice.age += voice.ui ? Time.unscaledDeltaTime : Time.deltaTime;
                if (voice.endAt > 0 && voice.age >= Mathf.Max(0, voice.endAt - .03f) && !voice.fading)
                    Fade(voice, .03f);
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
