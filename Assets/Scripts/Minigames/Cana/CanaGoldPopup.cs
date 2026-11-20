using TMPro;
using UnityEngine;

namespace CrossTheLine.Cana
{
    public sealed class CanaGoldPopup : MonoBehaviour
    {
        public TextMeshPro prefix;
        public TextMeshPro amountLabel;
        public SpriteRenderer coin;
        [Min(0.1f)] public float duration = 1.2f;
        public float fallDistance = 0.5f;
        private Vector3 origin;
        private float elapsed;

        public void Show(int amount)
        {
            origin = transform.position;
            amountLabel.text = amount.ToString();
        }

        private void Update()
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            var position = origin + Vector3.down * (fallDistance * progress);
            position.x = Mathf.Round(position.x * 16) / 16;
            position.y = Mathf.Round(position.y * 16) / 16;
            transform.position = position;
            var color = new Color(1, 1, 1, 1 - progress);
            prefix.color = amountLabel.color = coin.color = color;
            if (progress >= 1) Destroy(gameObject);
        }
    }
}
