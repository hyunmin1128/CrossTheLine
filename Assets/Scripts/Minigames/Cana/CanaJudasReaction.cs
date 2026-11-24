using UnityEngine;

namespace CrossTheLine.Cana
{
    public enum CanaJudasMood { Idle, Look, Buy, Surprise, Angry }

    public sealed class CanaJudasReaction : MonoBehaviour
    {
        public Animator animator;
        public SpriteRenderer image;
        public Sprite[] idle, look, buy, surprise, angry;
        public float framesPerSecond = 10;
        public float buySeconds = 3;
        public float surpriseHoldSeconds = 3;
        public int surprisedFrame = 1;
        public int surpriseSortingOrder = 32610;
        public CanaJudasMood Mood { get; private set; }
        public bool Angered { get; private set; }
        public bool IsSurprised => Mood == CanaJudasMood.Surprise;
        private float elapsed;
        private int normalSortingOrder;

        private void Awake()
        {
            animator.enabled = false;
            normalSortingOrder = image.sortingOrder;
            SetMood(CanaJudasMood.Idle);
        }

        public void PlayLook()
        {
            if (Mood == CanaJudasMood.Buy || IsSurprised) return;
            SetMood(CanaJudasMood.Look);
        }

        public void PlayBuy() => SetMood(CanaJudasMood.Buy);

        public void PlaySurprise()
        {
            Angered = true;
            SetMood(CanaJudasMood.Surprise);
        }

        private void SetMood(CanaJudasMood mood)
        {
            Mood = mood;
            elapsed = 0;
            Draw();
        }

        private void Update()
        {
            elapsed += Time.unscaledDeltaTime;
            bool finished = Mood == CanaJudasMood.Look && elapsed >= look.Length / framesPerSecond ||
                Mood == CanaJudasMood.Buy && elapsed >= buySeconds ||
                IsSurprised && elapsed >= surprisedFrame / framesPerSecond + surpriseHoldSeconds;
            if (finished) SetMood(Angered ? CanaJudasMood.Angry : CanaJudasMood.Idle);
            else Draw();
        }

        private void Draw()
        {
            image.sortingOrder = IsSurprised ? surpriseSortingOrder : normalSortingOrder;
            Sprite[] frames = Mood switch
            {
                CanaJudasMood.Look => look,
                CanaJudasMood.Buy => buy,
                CanaJudasMood.Surprise => surprise,
                CanaJudasMood.Angry => angry,
                _ => idle
            };
            if (frames == null || frames.Length == 0) return;
            int frame = Mathf.FloorToInt(elapsed * framesPerSecond);
            frame = IsSurprised ? Mathf.Min(frame, surprisedFrame, frames.Length - 1) :
                Mood == CanaJudasMood.Look ? Mathf.Min(frame, frames.Length - 1) : frame % frames.Length;
            image.sprite = frames[frame];
        }
    }
}
