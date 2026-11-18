using System.Collections;
using UnityEngine;

namespace CrossTheLine.Cana
{
    public sealed class CanaJesus : MonoBehaviour
    {
        [Min(0.1f)] public float prayerDuration = 1.5f;
        [Min(0)] public float interactionDistance = 1.5f;
        [Min(0)] public float idleAnimationSpeed = 1;
        [Min(0)] public float effectAnimationSpeed = 1;
        public Animator idleAnimator;
        public SpriteRenderer characterSprite;
        public Sprite prayingSprite;
        public GameObject prayerEffect;
        private CanaPlayer prayingPlayer;
        private Coroutine prayer;
        private Sprite idleSprite;

        private void Awake()
        {
            idleSprite = characterSprite.sprite;
            prayerEffect.SetActive(false);
        }

        private void Update()
        {
            idleAnimator.speed = idleAnimationSpeed;
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
            characterSprite.sprite = prayingSprite;
            prayerEffect.transform.SetParent(prayingPlayer.transform, false);
            prayerEffect.transform.localPosition = Vector3.zero;
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
            prayerEffect.SetActive(false);
            prayerEffect.transform.SetParent(transform, false);
            characterSprite.sprite = idleSprite;
            idleAnimator.enabled = true;
            if (prayingPlayer) prayingPlayer.FinishPrayer(completed);
            prayingPlayer = null;
            prayer = null;
        }

        private void OnDisable()
        {
            if (prayingPlayer) CancelPrayer(prayingPlayer);
        }
    }
}
