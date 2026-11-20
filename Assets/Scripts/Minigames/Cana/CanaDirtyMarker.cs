using UnityEngine;

namespace CrossTheLine.Cana
{
    public sealed class CanaDirtyMarker : MonoBehaviour
    {
        public CanaRound round;
        [Min(0.1f)] public float bobPeriod = 1.2f;
        private Vector3 origin;

        private void OnEnable() => origin = transform.localPosition;

        private void LateUpdate()
        {
            if (round && round.Finished) return;
            float pixels = Mathf.Round(Mathf.Sin(Time.time * Mathf.PI * 2 / bobPeriod));
            transform.localPosition = origin + Vector3.up * (pixels / 16);
        }

        private void OnDisable() => transform.localPosition = origin;
    }
}
