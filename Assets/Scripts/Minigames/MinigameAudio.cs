using UnityEngine;

namespace CrossTheLine.Minigames
{
    internal sealed class MinigameAudio
    {
        readonly GameObject owner;
        AudioSource music;
        AudioSource effects;
        AudioSource cue;
        AudioSource phaseAudio;
        AudioSource effectLoop;

        public void SetEffectLoop(AudioClip clip, float volume = .3f)
        {
            if (!clip)
            {
                if (effectLoop) effectLoop.Stop();
                return;
            }
            if (!effectLoop) effectLoop = CreateSource();
            effectLoop.loop = true;
            effectLoop.volume = volume;
            if (effectLoop.clip == clip && effectLoop.isPlaying) return;
            effectLoop.clip = clip;
            effectLoop.Play();
        }

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

        public void PlayEffect(AudioClip clip, float volume = .65f)
        {
            if (!clip) return;
            if (!effects) effects = CreateSource();
            effects.PlayOneShot(clip, volume);
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

        public void SetPhaseAudio(AudioClip clip)
        {
            if (phaseAudio)
            {
                phaseAudio.Stop();
                phaseAudio.clip = null;
            }
            if (!clip)
            {
                if (music) music.UnPause();
                return;
            }

            if (music) music.Pause();
            if (!phaseAudio) phaseAudio = CreateSource();
            phaseAudio.clip = clip;
            phaseAudio.volume = .4f;
            phaseAudio.Play();
        }

        public void SetPaused(bool paused)
        {
            if (phaseAudio && phaseAudio.clip)
            {
                if (paused) phaseAudio.Pause();
                else if (phaseAudio.clip) phaseAudio.UnPause();
            }
            if (music)
            {
                if (paused) music.Pause();
                else if (!phaseAudio || !phaseAudio.clip) music.UnPause();
            }
            if (effects) effects.Stop();
            if (cue) cue.Stop();
        }

        public void Stop()
        {
            if (effectLoop) effectLoop.Stop();
            if (phaseAudio) { phaseAudio.Stop(); phaseAudio.clip = null; }
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
