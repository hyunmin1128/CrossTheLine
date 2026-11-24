using TMPro;
using UnityEngine;

namespace CrossTheLine.Cana
{
    [RequireComponent(typeof(TextMeshPro))]
    public sealed class CanaOverlayText : MonoBehaviour
    {
        [Tooltip("Shrink long text to fit its original box at screen resolution.")]
        public bool fitToBox = true;
        [Range(.5f, 1)] public float minimumSizeRatio = .75f;
        public float rotationDegrees;
        public bool alignVisibleLeft;
        public bool centerVisibleVertically;
        public SpriteRenderer centeredPriceCoin;
        [Min(0)] public float coinGapPixels = 2;
        [System.NonSerialized] public TextMeshProUGUI target;
        private TextMeshPro source;
        private Renderer sourceRenderer;
        private readonly Vector3[] corners = new Vector3[4];
        public int SourceOrder => sourceRenderer ? sourceRenderer.sortingOrder : 0;

        private void OnEnable()
        {
            if (CanaTextOverlay.Instance) CanaTextOverlay.Instance.Register(this);
        }
        public void CreateTarget(CanaTextOverlay overlay)
        {
            if (target) return;
            source = GetComponent<TextMeshPro>();
            sourceRenderer = GetComponent<Renderer>();
            var snap = GetComponent<CanaPixelText>();
            if (snap) snap.enabled = false;
            var strike = GetComponent<CanaPriceStrike>();
            if (strike) strike.enabled = false;
            var go = new GameObject(name + " Text", typeof(RectTransform));
            go.transform.SetParent(overlay.transform, false);
            target = go.AddComponent<TextMeshProUGUI>();
            target.raycastTarget = false;
            target.font = overlay.font;
            target.fontSharedMaterial = overlay.font.material;
            target.rectTransform.anchorMin = target.rectTransform.anchorMax = Vector2.zero;
        }
        public void Sync(CanaTextOverlay overlay)
        {
            sourceRenderer.forceRenderingOff = true;
            bool visible = gameObject.activeInHierarchy && source.enabled && sourceRenderer.enabled;
            target.gameObject.SetActive(visible);
            if (!visible) return;
            var rect = source.rectTransform;
            rect.GetWorldCorners(corners);
            Vector2 bottomLeft = overlay.ScreenPoint(corners[0]);
            Vector2 topRight = overlay.ScreenPoint(corners[2]);
            var targetRect = target.rectTransform;
            targetRect.localRotation = Quaternion.Euler(0, 0, rotationDegrees);
            targetRect.pivot = rect.pivot;
            targetRect.sizeDelta = topRight - bottomLeft;
            targetRect.anchoredPosition = bottomLeft + Vector2.Scale(targetRect.sizeDelta, rect.pivot);
            target.text = source.text;
            target.color = source.color;
            target.alignment = source.alignment;
            target.fontStyle = source.fontStyle;
            target.richText = source.richText;
            target.textWrappingMode = source.textWrappingMode;
            target.overflowMode = source.overflowMode;
            float scale = overlay.ScreenScale * .1f * overlay.textScale * Mathf.Abs(source.transform.lossyScale.y);
            float size = source.fontSize * scale;
            target.enableAutoSizing = fitToBox;
            target.fontSizeMin = size * minimumSizeRatio;
            target.fontSizeMax = size;
            target.fontSize = size;
            target.characterSpacing = source.characterSpacing;
            target.wordSpacing = source.wordSpacing;
            target.lineSpacing = source.lineSpacing;
            target.margin = source.margin * (overlay.ScreenScale * Mathf.Abs(source.transform.lossyScale.y));
            if (alignVisibleLeft || centerVisibleVertically || centeredPriceCoin)
            {
                target.ForceMeshUpdate();
                Bounds ink = target.textBounds;
                Vector2 shift = Vector2.zero;
                if (alignVisibleLeft) shift.x = targetRect.rect.xMin - ink.min.x;
                if (centerVisibleVertically) shift.y = targetRect.rect.center.y - ink.center.y;
                if (centeredPriceCoin)
                {
                    Vector2 center = overlay.ScreenPoint(transform.position);
                    float coinWidth = centeredPriceCoin.enabled ? centeredPriceCoin.bounds.size.x * overlay.ScreenScale : 0;
                    float gap = centeredPriceCoin.enabled ? coinGapPixels * overlay.ScreenScale / overlay.pixelsPerUnit : 0;
                    float left = center.x - (coinWidth + gap + ink.size.x) / 2;
                    shift.x = left + coinWidth + gap - (targetRect.anchoredPosition.x + ink.min.x);
                    shift.y = center.y - (targetRect.anchoredPosition.y + ink.center.y);
                    Vector3 coinCenter = centeredPriceCoin.bounds.center;
                    Vector2 desired = (Vector2)transform.position + new Vector2((left + coinWidth / 2 - center.x) / overlay.ScreenScale, 0);
                    centeredPriceCoin.transform.position += new Vector3(desired.x - coinCenter.x, desired.y - coinCenter.y, 0);
                }
                targetRect.anchoredPosition += shift;
            }
        }
        public void RestoreSource() { if (sourceRenderer) sourceRenderer.forceRenderingOff = false; }
        private void OnDestroy()
        {
            RestoreSource();
            if (target) Destroy(target.gameObject);
        }
    }
}
