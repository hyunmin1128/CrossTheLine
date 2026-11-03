using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CrossTheLine
{
    // Subdivide the actual font atlas quads, so fragments retain the chosen font's shape.
    // Every pose depends only on progress: scrubbing and Replay produce the same result.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class NativityNameFragments : UnityEngine.UI.MaskableGraphic
    {
        TMP_Text source;
        float progress, distance;
        Color tint;
        public override Texture mainTexture => source && source.fontSharedMaterial
            ? source.fontSharedMaterial.GetTexture("_MainTex") : Texture2D.whiteTexture;

        public void Show(TMP_Text text, float value, Color color, float spread)
        {
            source = text; progress = value; tint = color; distance = spread;
            raycastTarget = false;
            if (value > 0 && value < 1) source.ForceMeshUpdate();
            SetVerticesDirty();
            SetMaterialDirty();
        }
        static float Noise(int seed)
        {
            uint n = (uint)seed * 747796405u + 2891336453u;
            n = ((n >> (int)((n >> 28) + 4)) ^ n) * 277803737u;
            return ((n >> 22) ^ n) / (float)uint.MaxValue;
        }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (!source || progress <= 0 || progress >= 1) return;
            var info = source.textInfo;
            const int columns = 12, rows = 16;
            for (int ch = 0; ch < info.characterCount; ch++)
            {
                var character = info.characterInfo[ch];
                if (!character.isVisible) continue;
                var mesh = info.meshInfo[character.materialReferenceIndex];
                int v = character.vertexIndex;
                Vector3 bottomLeft = rectTransform.InverseTransformPoint(source.transform.TransformPoint(mesh.vertices[v]));
                Vector3 topRight = rectTransform.InverseTransformPoint(source.transform.TransformPoint(mesh.vertices[v + 2]));
                Vector2 uvMin = mesh.uvs0[v];
                Vector2 uvMax = mesh.uvs0[v + 2];
                Vector2 cell = (topRight - bottomLeft) / new Vector2(columns, rows);
                for (int y = 0; y < rows; y++) for (int x = 0; x < columns; x++)
                {
                    int seed = 1 + ch * 1000 + y * columns + x;
                    float delay = Noise(seed) * .4f;
                    float t = Mathf.Clamp01((progress - delay) / (1 - delay));
                    float ease = t * t * (3 - 2 * t);
                    Vector2 drift = new Vector2((Noise(seed + 71) - .35f) * 1.7f, .2f + Noise(seed + 173));
                    Vector2 center = (Vector2)bottomLeft + Vector2.Scale(new Vector2(x + .5f, y + .5f), cell) + drift * distance * ease;
                    Vector2 half = cell * (.5f * Mathf.Lerp(1, .15f, ease));
                    float angle = (Noise(seed + 293) - .5f) * ease * 2;
                    float cos = Mathf.Cos(angle), sin = Mathf.Sin(angle);
                    Color32 color = new Color(tint.r, tint.g, tint.b, tint.a * (1 - ease));
                    int index = vh.currentVertCount;
                    Add(vh, center, new Vector2(-half.x, -half.y), cos, sin, color, uvMin, uvMax, x / (float)columns, y / (float)rows);
                    Add(vh, center, new Vector2(-half.x, half.y), cos, sin, color, uvMin, uvMax, x / (float)columns, (y + 1f) / rows);
                    Add(vh, center, new Vector2(half.x, half.y), cos, sin, color, uvMin, uvMax, (x + 1f) / columns, (y + 1f) / rows);
                    Add(vh, center, new Vector2(half.x, -half.y), cos, sin, color, uvMin, uvMax, (x + 1f) / columns, y / (float)rows);
                    vh.AddTriangle(index, index + 1, index + 2);
                    vh.AddTriangle(index, index + 2, index + 3);
                }
            }
        }
        static void Add(VertexHelper vh, Vector2 center, Vector2 offset, float cos, float sin, Color32 color, Vector2 uvMin, Vector2 uvMax, float u, float v)
        {
            Vector2 p = center + new Vector2(offset.x * cos - offset.y * sin, offset.x * sin + offset.y * cos);
            vh.AddVert(p, color, new Vector2(Mathf.Lerp(uvMin.x, uvMax.x, u), Mathf.Lerp(uvMin.y, uvMax.y, v)));
        }
    }
}
