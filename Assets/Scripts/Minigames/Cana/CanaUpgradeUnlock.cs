using UnityEngine;

namespace CrossTheLine.Cana
{
    [DefaultExecutionOrder(-100)]
    public sealed class CanaUpgradeUnlock : MonoBehaviour
    {
        private enum Motion { None, Denied, Opening, Falling }
        public CanaShop shop;
        public int upgradeIndex;
        public SpriteRenderer lockSprite;
        public SpriteRenderer[] backingSprites;
        public Sprite[] openingFrames;
        public Sprite goldLock;
        public SpriteRenderer chainSprite;
        public Sprite goldChain;
        private Sprite normalChain;
        private bool Reinforced => shop && shop.PricesDoubled;
        public float openingFramesPerSecond = 10;
        public float shakeFrequency = 4;
        public float deniedDuration = 0.45f;
        public float fallDuration = 0.45f;
        private Vector3 origin, center;
        private Quaternion rotation;
        private Color color;
        private Sprite closedSprite;
        private float elapsed;
        private bool initialized;
        private Motion motion;
        public int DeniedCount { get; private set; }
        public bool IsPlaying => motion != Motion.None;
        public bool IsFalling => motion == Motion.Falling;
        public bool IsRevealing => IsFalling || motion == Motion.Opening && elapsed >= OpeningDuration;
        private float OpeningDuration => openingFrames.Length / openingFramesPerSecond;
        private bool Damaged => !Reinforced && DeniedCount >= 10;

        private void Initialize()
        {
            if (initialized) return;
            initialized = true;
            origin = lockSprite.transform.localPosition;
            center = transform.InverseTransformPoint(lockSprite.bounds.center);
            rotation = lockSprite.transform.localRotation;
            color = lockSprite.color;
            closedSprite = lockSprite.sprite;
            normalChain = chainSprite ? chainSprite.sprite : null;
        }

        public void Reinforce()
        {
            Initialize();
            if (motion == Motion.Denied || motion == Motion.Falling) motion = Motion.None;
            DeniedCount = 0;
            Pose(0, Vector3.zero);
            lockSprite.sprite = goldLock ? goldLock : closedSprite;
            if (chainSprite && goldChain) chainSprite.sprite = goldChain;
        }

        public void Play()
        {
            Begin(Motion.Opening);
            CanaAudio.Play(CanaSound.LockOn);
        }

        public bool PlayDenied()
        {
            if (IsPlaying) return false;
            Begin(Motion.Denied);
            if (!Reinforced && DeniedCount == 9)
            {
                DeniedCount++;
                Finish(true);
            }
            return true;
        }

        private void Begin(Motion next)
        {
            gameObject.SetActive(true);
            Initialize();
            motion = next;
            elapsed = 0;
            foreach (var sprite in backingSprites) sprite.enabled = next != Motion.Falling;
            lockSprite.enabled = true;
            lockSprite.color = color;
            lockSprite.sprite = Reinforced && goldLock ? goldLock : closedSprite;
            if (chainSprite) chainSprite.sprite = Reinforced && goldChain ? goldChain : normalChain;
            Pose(Damaged ? 12 : 0, Damaged ? Vector3.down * (2f / 16) : Vector3.zero);
        }

        private void Update()
        {
            if (!IsPlaying) return;
            elapsed += Time.unscaledDeltaTime;
            if (motion == Motion.Denied)
            {
                if (elapsed >= deniedDuration)
                {
                    if (!Reinforced) DeniedCount++;
                    if (!Reinforced && DeniedCount >= 20)
                    {
                        Begin(Motion.Falling);
                        shop.OnLockBroken(upgradeIndex);
                    }
                    else Finish(true);
                    return;
                }
                float wave = Mathf.Sin(elapsed * shakeFrequency * Mathf.PI * 2) * (1 - elapsed / deniedDuration);
                Pose((Damaged ? 12 : 0) + wave * 14,
                    new Vector3(Mathf.Round(wave), Damaged ? -2 : 0, 0) / 16);
            }
            else if (motion == Motion.Opening)
            {
                lockSprite.sprite = Reinforced && goldLock ? goldLock : openingFrames[Mathf.Min(Mathf.FloorToInt(elapsed * openingFramesPerSecond), openingFrames.Length - 1)];
                Pose(0, Vector3.zero);
                if (elapsed >= OpeningDuration)
                {
                    foreach (var sprite in backingSprites) sprite.enabled = false;
                    float t = Mathf.Clamp01((elapsed - OpeningDuration) / 0.15f);
                    lockSprite.color = new Color(color.r, color.g, color.b, 1 - t);
                    if (t >= 1) Finish(false);
                }
            }
            else
            {
                float t = Mathf.Clamp01(elapsed / fallDuration);
                Pose(12 + 35 * t, new Vector3(3 * t, -2 - 32 * t * t, 0) / 16);
                lockSprite.color = new Color(color.r, color.g, color.b, 1 - t);
                if (t >= 1) Finish(false);
            }
        }

        private void Pose(float angle, Vector3 offset)
        {
            Quaternion turn = Quaternion.Euler(0, 0, angle);
            Vector3 position = center + turn * (origin - center) + offset;
            position.x = Mathf.Round(position.x * 16) / 16;
            position.y = Mathf.Round(position.y * 16) / 16;
            lockSprite.transform.localPosition = position;
            lockSprite.transform.localRotation = turn * rotation;
        }

        private void Finish(bool locked)
        {
            motion = Motion.None;
            lockSprite.sprite = Reinforced && goldLock ? goldLock : closedSprite;
            lockSprite.color = color;
            Pose(locked && Damaged ? 12 : 0, locked && Damaged ? Vector3.down * (2f / 16) : Vector3.zero);
            foreach (var sprite in backingSprites) sprite.enabled = true;
            gameObject.SetActive(locked);
        }

        private void OnDisable()
        {
            if (IsPlaying) Finish(motion == Motion.Denied);
        }
    }
}
