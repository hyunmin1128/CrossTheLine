using UnityEngine;

namespace CrossTheLine.Cana
{
    public sealed class CanaWaterJar : MonoBehaviour
    {
        public SpriteRenderer jarSprite;
        [Min(0)] public float interactionDistance = 1;

        public bool IsInRange(CanaPlayer player)
        {
            var point = player.transform.position + Vector3.up * 0.75f;
            return jarSprite && Vector3.Distance(point, jarSprite.bounds.ClosestPoint(point)) <= interactionDistance;
        }

        private void OnDrawGizmosSelected()
        {
            if (!jarSprite) return;
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(jarSprite.bounds.center,
                jarSprite.bounds.size + new Vector3(interactionDistance * 2, interactionDistance * 2, 0));
        }
    }
}
