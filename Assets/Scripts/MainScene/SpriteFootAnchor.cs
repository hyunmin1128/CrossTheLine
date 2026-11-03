using UnityEngine;

// Animation sheets may use different pivots. Keep their feet on the same ground.
[RequireComponent(typeof(SpriteRenderer))]
public class SpriteFootAnchor : MonoBehaviour
{
    public float localFootY;
    SpriteRenderer spriteRenderer;
    void Awake() { spriteRenderer = GetComponent<SpriteRenderer>(); }
    void LateUpdate()
    {
        if (!spriteRenderer.sprite) return;
        var position = transform.localPosition;
        position.y = localFootY - spriteRenderer.sprite.bounds.min.y * transform.localScale.y;
        transform.localPosition = position;
    }
}
