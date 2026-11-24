using System.Collections.Generic;
using UnityEngine;

namespace CrossTheLine.Cana
{
    // Rasterize only shop art onto the original pixel grid, then composite it with native text.
    internal sealed class CanaOverlaySprite
    {
        private readonly SpriteRenderer source;
        public readonly UnityEngine.UI.RawImage Target;
        public int Order => source ? source.sortingOrder : 0;
        private Texture2D raster;
        private Sprite lastSprite;
        private Matrix4x4 lastMatrix;
        private RectInt lastRect;
        private bool lastFlipX, lastFlipY;
        private Vector2 lastOrigin;
        private readonly Dictionary<Texture, Texture2D> readable = new Dictionary<Texture, Texture2D>();

        public CanaOverlaySprite(SpriteRenderer source, Transform parent)
        {
            this.source = source;
            var go = new GameObject(source.name + " Pixels", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            Target = go.AddComponent<UnityEngine.UI.RawImage>();
            Target.raycastTarget = false;
            Target.rectTransform.anchorMin = Target.rectTransform.anchorMax = Vector2.zero;
            Target.rectTransform.pivot = Vector2.zero;
        }
        public void Sync(CanaTextOverlay overlay)
        {
            if (!source) { Target.gameObject.SetActive(false); return; }
            source.forceRenderingOff = true;
            bool visible = source.enabled && source.gameObject.activeInHierarchy && source.sprite;
            Target.gameObject.SetActive(visible);
            if (!visible) return;
            float ppu = overlay.pixelsPerUnit;
            Vector2 origin = (Vector2)overlay.gameCamera.transform.position - (Vector2)overlay.referenceResolution / (2 * ppu);
            var bounds = source.bounds;
            int x0 = Mathf.Clamp(Mathf.FloorToInt((bounds.min.x - origin.x) * ppu), 0, overlay.referenceResolution.x);
            int y0 = Mathf.Clamp(Mathf.FloorToInt((bounds.min.y - origin.y) * ppu), 0, overlay.referenceResolution.y);
            int x1 = Mathf.Clamp(Mathf.CeilToInt((bounds.max.x - origin.x) * ppu), 0, overlay.referenceResolution.x);
            int y1 = Mathf.Clamp(Mathf.CeilToInt((bounds.max.y - origin.y) * ppu), 0, overlay.referenceResolution.y);
            var rect = new RectInt(x0, y0, x1 - x0, y1 - y0);
            if (rect.width <= 0 || rect.height <= 0) { Target.gameObject.SetActive(false); return; }
            if (lastSprite != source.sprite || lastMatrix != source.transform.localToWorldMatrix || lastRect != rect ||
                lastFlipX != source.flipX || lastFlipY != source.flipY || lastOrigin != origin)
            {
                Rasterize(rect, origin, ppu);
                lastSprite = source.sprite; lastMatrix = source.transform.localToWorldMatrix; lastRect = rect;
                lastFlipX = source.flipX; lastFlipY = source.flipY; lastOrigin = origin;
            }
            var bottomLeft = origin + new Vector2(rect.x, rect.y) / ppu;
            Vector2 screenMin = overlay.ScreenPoint(new Vector3(bottomLeft.x, bottomLeft.y, 0));
            Vector2 screenMax = overlay.ScreenPoint(new Vector3(bottomLeft.x + rect.width / ppu, bottomLeft.y + rect.height / ppu, 0));
            Target.rectTransform.anchoredPosition = screenMin;
            Target.rectTransform.sizeDelta = screenMax - screenMin;
            Target.color = source.color;
        }
        private void Rasterize(RectInt rect, Vector2 origin, float ppu)
        {
            var sprite = source.sprite;
            var texture = sprite.texture;
            if (!readable.TryGetValue(texture, out var copy))
            {
                var previous = RenderTexture.active;
                var rt = RenderTexture.GetTemporary(texture.width, texture.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
                Graphics.Blit(texture, rt);
                RenderTexture.active = rt;
                copy = new Texture2D(texture.width, texture.height, TextureFormat.RGBA32, false);
                copy.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0); copy.Apply();
                RenderTexture.active = previous; RenderTexture.ReleaseTemporary(rt);
                readable.Add(texture, copy);
            }
            if (!raster) raster = new Texture2D(rect.width, rect.height, TextureFormat.RGBA32, false);
            else if (raster.width != rect.width || raster.height != rect.height) raster.Reinitialize(rect.width, rect.height);
            raster.filterMode = FilterMode.Point; raster.wrapMode = TextureWrapMode.Clamp;
            var pixels = new Color32[rect.width * rect.height];
            var atlas = copy.GetPixels32();
            var inverse = source.transform.worldToLocalMatrix;
            var spriteRect = sprite.rect;
            for (int y = 0; y < rect.height; y++) for (int x = 0; x < rect.width; x++)
            {
                var world = origin + new Vector2(rect.x + x + .5f, rect.y + y + .5f) / ppu;
                var local = inverse.MultiplyPoint3x4(new Vector3(world.x, world.y, source.transform.position.z));
                if (source.flipX) local.x = -local.x;
                if (source.flipY) local.y = -local.y;
                int sx = Mathf.FloorToInt(local.x * sprite.pixelsPerUnit + sprite.pivot.x);
                int sy = Mathf.FloorToInt(local.y * sprite.pixelsPerUnit + sprite.pivot.y);
                if (sx >= 0 && sy >= 0 && sx < spriteRect.width && sy < spriteRect.height)
                    pixels[y * rect.width + x] = atlas[((int)spriteRect.y + sy) * copy.width + (int)spriteRect.x + sx];
            }
            raster.SetPixels32(pixels); raster.Apply(); Target.texture = raster;
        }
        public void Dispose()
        {
            if (source) source.forceRenderingOff = false;
            if (raster) Object.Destroy(raster);
            foreach (var copy in readable.Values) Object.Destroy(copy);
        }
    }
}
