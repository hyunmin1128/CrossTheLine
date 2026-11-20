using UnityEngine;

namespace CrossTheLine.Cana
{
    public sealed class CanaOneShotEffect : MonoBehaviour
    {
        [System.Serializable]
        public struct FootAnchor { public Sprite sprite; public Vector2 position; }
        public SpriteRenderer playerSprite;
        public SpriteRenderer effectSprite;
        public FootAnchor[] footAnchors;
        public Animator animator;
        public AnimationClip clip;
        [Min(0.01f)] public float animationSpeed = 1;
        private void OnEnable()
        {
            var depth = GetComponent<CanaDepthSort>();
            if (depth) depth.enabled = false;
            animator.speed = animationSpeed;
            animator.Play(0, 0, 0);
            animator.Update(0);
        }
        private void LateUpdate()
        {
            if (!playerSprite || !effectSprite || !effectSprite.sprite) return;
            var playerDepth = playerSprite.GetComponent<CanaDepthSort>();
            if (playerDepth) playerDepth.Refresh();
            effectSprite.sortingLayerID = playerSprite.sortingLayerID;
            effectSprite.sortingOrder = playerSprite.sortingOrder - 1;
            Vector2 foot = Vector2.zero;
            foreach (var anchor in footAnchors)
                if (anchor.sprite == playerSprite.sprite) { foot = anchor.position; break; }
            if (playerSprite.flipX) foot.x = -foot.x;
            if (playerSprite.flipY) foot.y = -foot.y;
            var bounds = effectSprite.sprite.bounds;
            Vector3 local = new Vector3(foot.x - bounds.center.x, foot.y - bounds.min.y, 0);
            local.x = Mathf.Round(local.x * 16) / 16;
            local.y = Mathf.Round(local.y * 16) / 16;
            transform.localPosition = local;
        }

        private void Update()
        {
            animator.speed = animationSpeed;
            if (animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1)
                gameObject.SetActive(false);
        }
    }
}
