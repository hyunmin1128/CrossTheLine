using UnityEngine;

namespace CrossTheLine.Cana
{
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class CanaItemDurationOverlay : MonoBehaviour
    {
        private Texture2D texture;
        private Sprite sprite;
        private SpriteRenderer image;
        private readonly Color32[] pixels = new Color32[24 * 24];
        private int lastStep = -1;
        public float Progress { get; private set; }

        private void Awake()
        {
            image = GetComponent<SpriteRenderer>();
            texture = new Texture2D(24, 24, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            sprite = Sprite.Create(texture, new Rect(0, 0, 24, 24), Vector2.zero, 16, 0, SpriteMeshType.FullRect);
            image.sprite = sprite;
            image.enabled = false;
        }

        public void SetProgress(bool active, float progress)
        {
            image.enabled = active;
            Progress = Mathf.Clamp01(progress);
            if (!active) return;
            int step = Mathf.FloorToInt(Progress * 192);
            if (step == lastStep) return;
            lastStep = step;
            float limit = Progress * Mathf.PI * 2;
            for (int y = 0; y < 24; y++)
                for (int x = 0; x < 24; x++)
                {
                    float angle = Mathf.Atan2(x - 11.5f, y - 11.5f);
                    if (angle < 0) angle += Mathf.PI * 2;
                    pixels[y * 24 + x] = new Color32(0, 0, 0, angle <= limit ? (byte)150 : (byte)0);
                }
            texture.SetPixels32(pixels);
            texture.Apply(false);
        }

        private void OnDestroy()
        {
            if (sprite) Destroy(sprite);
            if (texture) Destroy(texture);
        }
    }
}
