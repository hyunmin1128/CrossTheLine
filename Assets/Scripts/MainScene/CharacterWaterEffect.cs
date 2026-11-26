using UnityEngine;

[DefaultExecutionOrder(500)]
public class CharacterWaterEffect : MonoBehaviour
{
    public SpriteRenderer character;
    public SpriteRenderer ripple;
    public Material waterMaterial;
    public Vector2 offset;
    public int sortingOffset = 1;
    [Min(1)] public int pixelsPerUnit = 16;
    public bool IsInWater { get; private set; }
    void OnEnable() { if (ripple) ripple.enabled = false; }

    void LateUpdate()
    {
        if (!character || !ripple || !waterMaterial)
        {
            IsInWater = false;
            if (ripple) ripple.enabled = false;
            return;
        }
        var bounds = waterMaterial.GetVector("_WaterBounds");
        float surface = waterMaterial.GetFloat("_SurfaceY");
        float x = character.bounds.center.x;
        IsInWater = character.enabled && character.gameObject.activeInHierarchy && x >= bounds.x && x <= bounds.y && character.bounds.min.y < surface && character.bounds.max.y > surface;
        ripple.enabled = IsInWater;
        ripple.sortingLayerID = character.sortingLayerID;
        ripple.sortingOrder = character.sortingOrder + sortingOffset;
        float grid = Mathf.Max(1, pixelsPerUnit);
        float pivotFraction = ripple.sprite ? Mathf.Repeat(ripple.sprite.pivot.x, 1) : 0;
        float alignedX = bounds.x + (Mathf.Round((x + offset.x - bounds.x) * grid - pivotFraction) + pivotFraction) / grid;
        float alignedY = surface + Mathf.Round(offset.y * grid) / grid;
        ripple.transform.position = new Vector3(alignedX, alignedY, character.transform.position.z);
    }
    void OnDisable() { IsInWater = false; if (ripple) ripple.enabled = false; }
}
