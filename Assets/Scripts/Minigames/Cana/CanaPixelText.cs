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
        }
    }
}
