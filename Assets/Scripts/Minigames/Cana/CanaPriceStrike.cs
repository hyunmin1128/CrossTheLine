using TMPro;
using UnityEngine;

namespace CrossTheLine.Cana
{
    [DefaultExecutionOrder(1100)]
    public sealed class CanaPriceStrike : MonoBehaviour
    {
        public Material material;
        private TMP_Text label;
        private SpriteRenderer line;
        private Sprite pixel;

        private void Awake()
        {
            label = GetComponent<TMP_Text>();
            var child = new GameObject("Original Price Strike");
            child.transform.SetParent(transform, false);
            line = child.AddComponent<SpriteRenderer>();
            pixel = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), Vector2.zero, 16);
            line.sprite = pixel;
            line.sharedMaterial = material;
            line.color = new Color32(112, 55, 40, 255);
            line.sortingOrder = GetComponent<Renderer>().sortingOrder + 1;
        }

        private void LateUpdate()
        {
            int start = label.text.IndexOf("<s>");
            int end = label.text.IndexOf("</s>");
            line.enabled = start >= 0 && end > start;
            if (!line.enabled) return;
            int count = end - start - 3;
            if (label.textInfo.characterCount < count || count == 0) { line.enabled = false; return; }
            var first = label.textInfo.characterInfo[0];
            var last = label.textInfo.characterInfo[count - 1];
            Vector3 from = transform.TransformPoint(new Vector3(first.bottomLeft.x, (first.bottomLeft.y + first.topRight.y) / 2, 0));
            Vector3 to = transform.TransformPoint(new Vector3(last.topRight.x, 0, 0));
            line.transform.position = new Vector3(Mathf.Floor(from.x * 16) / 16,
                Mathf.Round(from.y * 16) / 16, transform.position.z);
            line.transform.localScale = new Vector3(Mathf.Max(1, Mathf.Ceil((to.x - line.transform.position.x) * 16)), 1, 1);
        }

        private void OnDestroy()
        {
            if (pixel) Destroy(pixel);
        }
    }
}
