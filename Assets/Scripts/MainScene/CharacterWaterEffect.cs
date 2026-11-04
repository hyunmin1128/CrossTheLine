using UnityEngine;

public class CharacterWaterEffect : MonoBehaviour
{
    public SpriteRenderer character;
    public SpriteRenderer ripple;
    public Material waterMaterial;
    public Vector2 offset;

    void LateUpdate()
    {
        if (!character || !ripple || !waterMaterial) return;
        var bounds = waterMaterial.GetVector("_WaterBounds");
        float surface = waterMaterial.GetFloat("_SurfaceY");
        float x = character.bounds.center.x;
        ripple.enabled = character.enabled && x >= bounds.x && x <= bounds.y && character.bounds.min.y < surface && character.bounds.max.y > surface;
        ripple.transform.position = new Vector3(x + offset.x, surface + offset.y, character.transform.position.z);
    }
    void OnDisable() { if (ripple) ripple.enabled = false; }
}
