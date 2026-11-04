using TMPro;
using UnityEngine;

// Keep each bitmap texel an integer number of screen pixels as the Canvas scales.
[RequireComponent(typeof(TextMeshProUGUI))]
public class BitmapTextPixelScale : MonoBehaviour
{
    TMP_Text label;
    float preferredSize;
    void Awake() { label = GetComponent<TMP_Text>(); preferredSize = label.fontSize; }
    void LateUpdate()
    {
        if (!label.font || !label.canvas) return;
        var canvas = label.canvas.rootCanvas;
        float pixelsPerUnit;
        if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            pixelsPerUnit = Mathf.Abs(label.transform.lossyScale.y);
        else
        {
            var camera = canvas.worldCamera ? canvas.worldCamera : Camera.main;
            if (!camera) return;
            var origin = camera.WorldToScreenPoint(label.transform.position);
            pixelsPerUnit = (camera.WorldToScreenPoint(label.transform.TransformPoint(Vector3.up)) - origin).magnitude;
        }
        if (pixelsPerUnit < 0.0001f) return;
        float nativeSize = label.font.faceInfo.pointSize;
        float multiple = Mathf.Max(1, Mathf.Round(preferredSize * pixelsPerUnit / nativeSize));
        float size = nativeSize * multiple / pixelsPerUnit;
        if (Mathf.Abs(label.fontSize - size) > 0.01f) label.fontSize = size;
    }
    void OnDisable() { if (label) label.fontSize = preferredSize; }
}
