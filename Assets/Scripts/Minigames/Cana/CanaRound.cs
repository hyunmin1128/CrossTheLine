using TMPro;
using UnityEngine;

namespace CrossTheLine.Cana
{
    public sealed class CanaRound : MonoBehaviour
    {
        public CanaDissatisfaction dissatisfaction;
        public TextMeshPro timer;
        public TextMeshPro result;
        [Min(1)] public float duration = 300;
        [SerializeField] private float elapsed;
        [SerializeField] private bool cleared;
        public float Elapsed => elapsed;
        public bool Cleared => cleared;
        public bool Finished => cleared || dissatisfaction.IsGameOver;
        private void Update()
        {
            if (!Finished)
            {
                elapsed = Mathf.Min(duration, elapsed + Time.deltaTime);
                if (elapsed >= duration) cleared = true;
            }
            int seconds = Mathf.CeilToInt(Mathf.Max(0, duration - elapsed));
            timer.text = $"{seconds / 60:00}:{seconds % 60:00}";
            result.gameObject.SetActive(Finished);
            if (Finished) result.text = cleared ? "CLEAR" : "GAME OVER";
        }
    }
}
