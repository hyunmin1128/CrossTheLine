using UnityEngine;

namespace CrossTheLine.Cana
{
    [ExecuteAlways]
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class CanaDepthSort : MonoBehaviour
    {
        [Tooltip("Local Y position of the object's contact point with the floor.")]
        public float floorOffset;
        [Tooltip("Tie-breaker for objects on the same pixel row.")]
        public int orderOffset;
        private SpriteRenderer spriteRenderer;

        public float FloorY => transform.TransformPoint(new Vector3(0, floorOffset, 0)).y;

        private void OnEnable()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            Refresh();
        }

        private void LateUpdate() => Refresh();

        public void Refresh()
        {
            if (!spriteRenderer) spriteRenderer = GetComponent<SpriteRenderer>();
            spriteRenderer.sortingOrder = Mathf.Clamp(-Mathf.RoundToInt(FloorY * 16) * 10 + orderOffset, -29000, 29000);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            var point = transform.TransformPoint(new Vector3(0, floorOffset, 0));
            Gizmos.DrawLine(point + Vector3.left * 0.5f, point + Vector3.right * 0.5f);
        }
    }
}
