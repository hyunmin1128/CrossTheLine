using TMPro;
using UnityEngine;

namespace CrossTheLine.Cana
{
    [DefaultExecutionOrder(1000)]
    [RequireComponent(typeof(TMP_Text))]
    public sealed class CanaPixelText : MonoBehaviour
    {
        private TMP_Text label;
        private void Awake() => label = GetComponent<TMP_Text>();
        private void LateUpdate()
        {
            label.ForceMeshUpdate();
            Vector3 min = transform.TransformPoint(label.textBounds.min);
            transform.position += new Vector3(Mathf.Round(min.x * 16) / 16 - min.x,
                Mathf.Round(min.y * 16) / 16 - min.y, 0);
            // Snap every glyph, including separately centered lines, to the 16 PPU grid.
            foreach (var mesh in label.textInfo.meshInfo)
            {
                for (int i = 0; i < mesh.vertexCount; i++)
                {
                    Vector3 world = transform.TransformPoint(mesh.vertices[i]);
                    world.x = Mathf.Round(world.x * 16) / 16;
                    world.y = Mathf.Round(world.y * 16) / 16;
                    mesh.vertices[i] = transform.InverseTransformPoint(world);
                }
            }
            label.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices);
        }
    }
}
