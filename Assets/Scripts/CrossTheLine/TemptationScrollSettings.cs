using UnityEngine;
namespace CrossTheLine
{
    [DisallowMultipleComponent]
    public sealed class TemptationScrollSettings : MonoBehaviour
    {
        public GameObject backgroundPrefab;
        public GameObject editorBackgroundPreview;
        public float cameraY=.65f, speed=4f, followSharpness=5f;
        [Min(8)]public float fastingSeconds=14;
        [Range(.2f,1)]public float staffWalkSpeed=.45f;
        [Tooltip("Editable locations, foot heights, walking limits and trial positions in the panorama prefab.")]
        public TemptationScrollSegment[] segments;
        void Awake(){if(editorBackgroundPreview)editorBackgroundPreview.SetActive(false);}
    }
}
