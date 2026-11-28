using UnityEngine;

namespace CrossTheLine.Minigames.Temple
{
    public sealed class TempleCrowdActor : MonoBehaviour
    {
        public SpriteRenderer artwork;
        int resistance;
        float exitSpeed, feedback;
        Vector3 displayScale;

        public void Setup(TempleClearingGame.Appearance appearance, int variant)
        {
            resistance = appearance.resistance;
            exitSpeed = appearance.exitSpeed;
            if (appearance.sprites.Length > 0) artwork.sprite = appearance.sprites[variant % appearance.sprites.Length];
            if (artwork.sprite)
            {
                var bounds = artwork.sprite.bounds;
                artwork.transform.localScale = Vector3.one * (1.1f / Mathf.Max(bounds.size.x, bounds.size.y));
            }
            displayScale = artwork.transform.localScale;
        }

        public void ApplyPressure()
        {
            if (resistance <= 0) return;
            resistance--;
            feedback = .18f;
        }

        public bool Advance(float dt, Vector3 exit)
        {
            feedback = Mathf.Max(0, feedback - dt);
            artwork.transform.localScale = displayScale * (feedback > 0 ? .9f : 1);
            artwork.color = resistance <= 0 ? new Color(.72f,.9f,1) : feedback > 0 ? new Color(1,.8f,.54f) : Color.white;
            if (resistance <= 0)
            {
                artwork.flipX = exit.x < transform.position.x;
                transform.position = Vector3.MoveTowards(transform.position, exit, exitSpeed * dt);
            }
            artwork.sortingOrder = Mathf.RoundToInt(-transform.position.y * 20);
            return resistance <= 0 && (transform.position - exit).sqrMagnitude < .02f;
        }
    }
}
