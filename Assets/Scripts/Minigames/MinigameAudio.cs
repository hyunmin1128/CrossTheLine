using UnityEngine;

namespace CrossTheLine.Minigames
{
    internal sealed class MinigameAudio
    {
        readonly GameObject owner;
        AudioSource music;
        AudioSource effects;
        AudioSource cue;

        public MinigameAudio(GameObject owner)
        {
            this.owner = owner;
        }

        public void SetMusic(AudioClip clip)
        {
            if (!clip)
            {
                if (music) music.Stop();
                return;
            }

            if (!music)
            {
                music = CreateSource();
                music.loop = true;
                music.volume = .3f;
            }

            if (music.clip == clip && music.isPlaying) return;
            music.clip = clip;
            music.Play();
        }

        public void PlayEffect(AudioClip clip)
        {
            if (!clip) return;
            if (!effects) effects = CreateSource();
            effects.PlayOneShot(clip, .65f);
        }

        public void PlayCue(AudioClip clip, float maximumSeconds)
        {
            if (!clip) return;
            if (!cue) cue = CreateSource();
            cue.Stop();
            cue.clip = clip;
            cue.volume = .65f;
            double start = AudioSettings.dspTime + .05;
            cue.PlayScheduled(start);
            if (clip.length > maximumSeconds) cue.SetScheduledEndTime(start + maximumSeconds);
        }

        public void Stop()
        {
            if (music) music.Stop();
            if (effects) effects.Stop();
            if (cue) cue.Stop();
        }

        AudioSource CreateSource()
        {
            var source = owner.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0;
            return source;
        }
    }
}
