using UnityEngine;

// A scroll speed of 1 is the physical gameplay plane; smaller values appear farther away.
[DefaultExecutionOrder(10000)]
public sealed class MainSceneParallaxLayer : MonoBehaviour
{
    public Camera viewCamera;
    [Range(0, 1)] public float scrollSpeed = .65f;
    public Vector3 referenceCameraPosition;
    [Min(1)] public int pixelsPerUnit = 16;
    Vector3 origin;
    void Awake() { origin = transform.position; }
    void LateUpdate()
    {
        if (!viewCamera) return;
        float shift = (viewCamera.transform.position.x - referenceCameraPosition.x) * (1 - scrollSpeed);
        shift = Mathf.Round(shift * pixelsPerUnit) / pixelsPerUnit;
        transform.position = origin + Vector3.right * shift;
    }
}
