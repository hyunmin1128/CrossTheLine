using UnityEngine;

namespace CrossTheLine.Cana
{
    // The frame shares the native text's rotation and draw order, including modal shading.
    [DefaultExecutionOrder(1700)]
    [RequireComponent(typeof(CanaOverlayText))]
    public sealed class CanaHiredStamp : MonoBehaviour
    {
        [Min(.1f)] public float borderPixels = .75f;
        public Color ink = new Color32(184, 35, 40, 255);
        UnityEngine.UI.Image[] edges;
        CanaOverlayText label;
        void Awake() => label = GetComponent<CanaOverlayText>();
        void LateUpdate()
        {
            if (!label || !label.target || !label.target.gameObject.activeInHierarchy || !CanaTextOverlay.Instance) return;
            if (edges == null)
            {
                edges = new UnityEngine.UI.Image[4];
                for (int i = 0; i < edges.Length; i++)
                {
                    var go = new GameObject("Stamp Border " + i, typeof(RectTransform));
                    go.transform.SetParent(label.target.transform, false);
                    edges[i] = go.AddComponent<UnityEngine.UI.Image>();
                    edges[i].raycastTarget = false;
                }
            }
            var box = label.target.rectTransform.rect;
            float thickness = borderPixels * CanaTextOverlay.Instance.ScreenScale / CanaTextOverlay.Instance.pixelsPerUnit;
            for (int i = 0; i < edges.Length; i++)
            {
                edges[i].color = ink;
                var rect = edges[i].rectTransform;
                rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
                rect.pivot = new Vector2(.5f, .5f);
                bool horizontal = i < 2;
                rect.sizeDelta = horizontal ? new Vector2(box.width, thickness) : new Vector2(thickness, box.height);
                rect.anchoredPosition = horizontal ? new Vector2(0, (i == 0 ? 1 : -1) * (box.height - thickness) / 2) :
                    new Vector2((i == 2 ? 1 : -1) * (box.width - thickness) / 2, 0);
            }
        }
    }
}
