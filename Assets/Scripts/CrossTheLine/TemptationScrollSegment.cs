using UnityEngine;
namespace CrossTheLine
{
    public sealed class TemptationScrollSegment : MonoBehaviour
    {
        [Range(0,3)] public int location;
        public SpriteRenderer background;
        public Transform entrance, exit, jesusMark, satanMark;
        [Tooltip("Optional left-to-right foot path. Edit these points to match the drawn slope.")]
        public Transform[] walkingPath;
        public float GroundY=>entrance.position.y;
        public float GroundAt(float x)
        {
            if(walkingPath==null||walkingPath.Length<2)return GroundY;
            for(int i=1;i<walkingPath.Length;i++)if(x<=walkingPath[i].position.x)
                return Mathf.Lerp(walkingPath[i-1].position.y,walkingPath[i].position.y,Mathf.InverseLerp(walkingPath[i-1].position.x,walkingPath[i].position.x,x));
            return walkingPath[walkingPath.Length-1].position.y;
        }
        void OnDrawGizmosSelected()
        {
            if(entrance&&exit){Gizmos.color=Color.green;Gizmos.DrawLine(entrance.position,exit.position);}
            if(walkingPath!=null)for(int i=1;i<walkingPath.Length;i++)if(walkingPath[i-1]&&walkingPath[i]){Gizmos.color=Color.yellow;Gizmos.DrawLine(walkingPath[i-1].position,walkingPath[i].position);}
            if(jesusMark){Gizmos.color=Color.cyan;Gizmos.DrawWireSphere(jesusMark.position,.25f);}
            if(satanMark){Gizmos.color=Color.magenta;Gizmos.DrawWireSphere(satanMark.position,.25f);}
        }
    }
}
