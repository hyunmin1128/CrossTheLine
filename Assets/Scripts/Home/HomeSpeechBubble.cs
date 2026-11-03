using UnityEngine;
using TMPro;

// Reuses HomeDialogueUI for line sequencing and typewriter playback.
[RequireComponent(typeof(CanvasRenderer))]
public class HomeSpeechBubble : UnityEngine.UI.MaskableGraphic
{
    public Texture2D texture;
    public override Texture mainTexture => texture ? texture : Texture2D.whiteTexture;
    const float Corner = 3;
    const float TailWidth = 8;
    const float TailHeight = 5;

    protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper vh)
    {
        vh.Clear();
        if (!texture) return;
        Rect r = rectTransform.rect;
        // Original 64px art: top corners and edge, mirrored at the bottom.
        float[] xs = { r.xMin, r.xMin + Corner, r.xMax - Corner, r.xMax };
        float[] ys = { r.yMin, r.yMin + Corner, r.yMax - Corner, r.yMax };
        float[] us = { 13, 16, 48, 51 };
        for (int x = 0; x < 3; x++)
        {
            Quad(vh, xs[x], ys[0], xs[x + 1], ys[1], us[x], 51, us[x + 1], 48);
            Quad(vh, xs[x], ys[1], xs[x + 1], ys[2], us[x], 47, us[x + 1], 48);
            Quad(vh, xs[x], ys[2], xs[x + 1], ys[3], us[x], 48, us[x + 1], 51);
        }
        // Overlap the bottom border by one art pixel to join the white interior.
        Quad(vh, -TailWidth / 2, r.yMin - TailHeight + 1, TailWidth / 2, r.yMin + 1, 28, 9, 36, 14);
    }

    void Quad(UnityEngine.UI.VertexHelper vh, float x0, float y0, float x1, float y1,
        float u0, float v0, float u1, float v1)
    {
        int start = vh.currentVertCount;
        vh.AddVert(new Vector3(x0, y0), color, new Vector2(u0 / texture.width, v0 / texture.height));
        vh.AddVert(new Vector3(x0, y1), color, new Vector2(u0 / texture.width, v1 / texture.height));
        vh.AddVert(new Vector3(x1, y1), color, new Vector2(u1 / texture.width, v1 / texture.height));
        vh.AddVert(new Vector3(x1, y0), color, new Vector2(u1 / texture.width, v0 / texture.height));
        vh.AddTriangle(start, start + 1, start + 2);
        vh.AddTriangle(start + 2, start + 3, start);
    }

    public void Fit(TMP_Text text)
    {
        const float maxTextWidth = 120;
        Vector2 preferred = text.GetPreferredValues(text.text, maxTextWidth, Mathf.Infinity);
        float width = Mathf.Ceil(Mathf.Min(preferred.x, maxTextWidth));
        rectTransform.sizeDelta = new Vector2(Mathf.Max(16, width + 12), Mathf.Max(12, Mathf.Ceil(preferred.y) + 8));
        text.rectTransform.anchorMin = Vector2.zero;
        text.rectTransform.anchorMax = Vector2.one;
        text.rectTransform.offsetMin = new Vector2(6, 4);
        text.rectTransform.offsetMax = new Vector2(-6, -4);
        SetVerticesDirty();
    }

    public static HomeDialogueUI Create(HomeDialogueUI template, Transform owner, Vector3 position)
    {
        var root = new GameObject("MonologueBubble", typeof(RectTransform), typeof(Canvas));
        root.transform.SetParent(owner, false);
        root.transform.position = position;
        // Art and world sprites share 16 pixels per world unit, irrespective of NPC scale.
        root.transform.localScale = new Vector3(1f / (16 * owner.lossyScale.x), 1f / (16 * owner.lossyScale.y), 1);
        var canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 100;
        var body = new GameObject("Body", typeof(RectTransform), typeof(CanvasRenderer), typeof(HomeSpeechBubble));
        body.transform.SetParent(root.transform, false);
        var bubble = body.GetComponent<HomeSpeechBubble>();
        bubble.texture = template.bubbleTexture;
        bubble.raycastTarget = false;
        bubble.rectTransform.pivot = new Vector2(0.5f, 0);
        var label = new GameObject("MonologueText", typeof(RectTransform), typeof(TextMeshProUGUI));
        label.transform.SetParent(body.transform, false);
        var text = label.GetComponent<TextMeshProUGUI>();
        text.font = template.dialogueText.font;
        text.fontSharedMaterial = template.dialogueText.fontSharedMaterial;
        text.fontSize = 8;
        text.color = Color.black;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;
        var playback = root.AddComponent<HomeDialogueUI>();
        playback.panel = body;
        playback.dialogueText = text;
        playback.charactersPerSecond = template.charactersPerSecond;
        playback.bubbleView = bubble;
        return playback;
    }
}
