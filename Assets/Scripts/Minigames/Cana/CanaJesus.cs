using System.Collections;
using UnityEngine;

namespace CrossTheLine.Cana
{
    public sealed class CanaJesus : MonoBehaviour
    {
        [Min(0.1f)] public float prayerDuration = 0.5f;
        [Min(0)] public float interactionDistance = 1.5f;
        [Min(0)] public float idleAnimationSpeed = 1;
        [Min(0)] public float effectAnimationSpeed = 1;
        public Animator idleAnimator;
        public SpriteRenderer characterSprite;
        public Sprite prayingSprite;
        public CanaInventory inventory;
        public RuntimeAnimatorController monkeyIdleAnimation;
        public Sprite monkeyPrayingSprite;
        private RuntimeAnimatorController normalIdleAnimation;
        public GameObject prayerEffect;
        public Vector3 effectOffset = new Vector3(0, -0.0625f, 0);
        private CanaPlayer prayingPlayer;
        private Coroutine prayer;
        private Sprite idleSprite;

        private void Awake()
        {
            idleSprite = characterSprite.sprite;
            normalIdleAnimation = idleAnimator.runtimeAnimatorController;
            prayerEffect.SetActive(false);
        }

        private void Update()
        {
            RefreshAppearance();
            idleAnimator.speed = idleAnimationSpeed;
        }

        private void RefreshAppearance()
        {
            bool monkey = inventory && inventory.ComplaintsBlocked;
            if (prayingPlayer)
            {
                idleAnimator.enabled = false;
                characterSprite.sprite = monkey ? monkeyPrayingSprite : prayingSprite;
                return;
            }
            var controller = monkey ? monkeyIdleAnimation : normalIdleAnimation;
            if (idleAnimator.runtimeAnimatorController != controller)
            {
                idleAnimator.runtimeAnimatorController = controller;
                idleAnimator.Play(0, 0, 0);
            }
            idleAnimator.enabled = true;
        }

        public bool IsInRange(CanaPlayer player)
        {
            return Vector2.Distance(transform.position, player.transform.position) <= interactionDistance;
        }

        public bool TryPray(CanaPlayer player)
        {
            if (prayingPlayer || !IsInRange(player) || !player.BeginPrayer()) return false;
            prayingPlayer = player;
            prayer = StartCoroutine(Pray());
            return true;
        }

        private IEnumerator Pray()
        {
            idleAnimator.enabled = false;
            characterSprite.sprite = inventory && inventory.ComplaintsBlocked ? monkeyPrayingSprite : prayingSprite;
            prayerEffect.transform.SetParent(prayingPlayer.transform, false);
            prayerEffect.transform.localPosition = effectOffset;
            prayerEffect.GetComponent<CanaOneShotEffect>().animationSpeed = effectAnimationSpeed;
            prayerEffect.GetComponent<Animator>().speed = effectAnimationSpeed;
            prayerEffect.SetActive(true);
            yield return new WaitForSeconds(prayerDuration);
            EndPrayer(true);
        }

        public void CancelPrayer(CanaPlayer player)
        {
            if (prayingPlayer != player) return;
            if (prayer != null) StopCoroutine(prayer);
            EndPrayer(false);
        }

        private void EndPrayer(bool completed)
        {
            if (!completed) prayerEffect.SetActive(false);
            characterSprite.sprite = idleSprite;
            idleAnimator.enabled = true;
            if (prayingPlayer) prayingPlayer.FinishPrayer(completed);
            prayingPlayer = null;
            prayer = null;
            RefreshAppearance();
        }

        private void OnDisable()
        {
            if (prayingPlayer) CancelPrayer(prayingPlayer);
        }
    }
}
