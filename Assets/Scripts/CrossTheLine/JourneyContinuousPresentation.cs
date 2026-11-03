using UnityEngine;
namespace CrossTheLine
{
    [DefaultExecutionOrder(30000)]
    public sealed class JourneyContinuousPresentation : MonoBehaviour
    {
        [HideInInspector]public JourneyContinuousWorld world;
        void LateUpdate(){if(world)world.Present();}
    }
}
