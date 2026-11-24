using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace CrossTheLine.Cana
{
    // Keep gameplay references on the original labels; render their text after the pixel camera.
    [DefaultExecutionOrder(1500)]
    [RequireComponent(typeof(Canvas))]
    public sealed class CanaTextOverlay : MonoBehaviour
    {
        public Camera gameCamera;
        public TMP_FontAsset font;
        [Min(.1f)] public float textScale = 1;
        public int pixelsPerUnit = 16;
        public Vector2Int referenceResolution = new Vector2Int(320, 180);
        public SpriteRenderer[] pixelGraphics;
        public static CanaTextOverlay Instance { get; private set; }
        private readonly List<CanaOverlayText> labels = new List<CanaOverlayText>();
        private readonly List<CanaOverlaySprite> graphics = new List<CanaOverlaySprite>();
        private readonly List<(int order, Transform target)> drawOrder = new List<(int, Transform)>();

        private void Awake()
        {
            Instance = this;
            foreach (var label in FindObjectsByType<CanaOverlayText>(FindObjectsInactive.Include))
                if (label.gameObject.scene == gameObject.scene) Register(label);
            foreach (var source in pixelGraphics)
                if (source) graphics.Add(new CanaOverlaySprite(source, transform));
        }

        public void Register(CanaOverlayText label)
        {
            if (labels.Contains(label)) return;
            label.CreateTarget(this);
            labels.Add(label);
        }

        private void LateUpdate()
        {
            if (!gameCamera || !font) return;
            drawOrder.Clear();
            for (int i = labels.Count - 1; i >= 0; i--)
            {
                var label = labels[i];
                if (!label) { labels.RemoveAt(i); continue; }
                label.Sync(this);
                drawOrder.Add((label.SourceOrder, label.target.transform));
            }
            foreach (var graphic in graphics)
            {
                graphic.Sync(this);
                drawOrder.Add((graphic.Order, graphic.Target.transform));
            }
            drawOrder.Sort((a, b) => a.order.CompareTo(b.order));
            for (int i = 0; i < drawOrder.Count; i++) drawOrder[i].target.SetSiblingIndex(i);
        }

        public Vector2 ScreenPoint(Vector3 world) => gameCamera.WorldToScreenPoint(world);
        public float ScreenScale => Vector2.Distance(ScreenPoint(Vector3.zero), ScreenPoint(Vector3.up));
        private void OnDestroy()
        {
            foreach (var graphic in graphics) graphic.Dispose();
            foreach (var label in labels) if (label) label.RestoreSource();
            if (Instance == this) Instance = null;
        }
    }
}
