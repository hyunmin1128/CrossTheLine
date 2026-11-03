using UnityEngine;
namespace CrossTheLine
{
    [DisallowMultipleComponent,DefaultExecutionOrder(-100)]
    public sealed class JordanScrollSettings : MonoBehaviour
    {
        public GameObject backgroundPrefab;
        public GameObject editorBackgroundPreview;
        [Tooltip("World-space background and camera vertical centre.")] public float cameraY=.65f;
        public float startX=-42f;
        public float horizontalLead=2f;
        [Min(.1f)] public float followSharpness=5f;
        [Min(30f)] public float worldWidth=90f;
        public const int PixelsPerUnit=32, ViewWidth=960, ViewHeight=540;
        public const float CameraSize=8.4375f;
        void Awake(){if(editorBackgroundPreview)editorBackgroundPreview.SetActive(false);}
    }
}
