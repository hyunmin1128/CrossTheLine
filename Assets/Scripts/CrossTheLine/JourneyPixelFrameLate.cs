using UnityEngine;
namespace CrossTheLine
{
    [DefaultExecutionOrder(31000)]
    public sealed class JourneyPixelFrameLate:MonoBehaviour
    {
        [HideInInspector]public JourneyPixelFrame frame;
        void LateUpdate(){if(frame)frame.Present();}
    }
}
