using UnityEngine;

namespace CrossTheLine.Cana
{
    public sealed class CanaDashAfterimage : MonoBehaviour
    {
        private SpriteRenderer image;
        private Color initialColor;
        private float elapsed;
        private float lifetime;

        public void Initialize(SpriteRenderer source, float duration)
        {
            lifetime = Mathf.Max(0.01f, duration);
            image = gameObject.AddComponent<SpriteRenderer>();
            image.sprite = source.sprite;
            image.sharedMaterial = source.sharedMaterial;
            image.flipX = source.flipX;
            image.flipY = source.flipY;
            image.sortingLayerID = source.sortingLayerID;
            image.sortingOrder = source.sortingOrder - 1;
            initialColor = source.color;
            initialColor.a *= 0.35f;
            image.color = initialColor;
        }

        private void Update()
        {
            elapsed += Time.deltaTime;
            var color = initialColor;
            color.a *= 1 - Mathf.Clamp01(elapsed / lifetime);
            image.color = color;
            if (elapsed >= lifetime) Destroy(gameObject);
        }
    }
}
