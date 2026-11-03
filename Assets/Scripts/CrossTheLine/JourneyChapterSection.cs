using UnityEngine;
namespace CrossTheLine
{
    public sealed class JourneyChapterSection : MonoBehaviour
    {
        public int chapterIndex;
        public string chapterTitle,sourceScene;
        public GameObject templates,preview;
        public JourneyGame sourceGame;
        public JourneySceneStages sourceStages;
        public float localMinX,localMaxX=48;
        public Vector2 entryLocal=new Vector2(3,-3.35f);
        public float pixelFrameScale=1;
        public float WorldMinX=>transform.position.x+localMinX*pixelFrameScale;
        public float WorldMaxX=>transform.position.x+localMaxX*pixelFrameScale;
        void OnDrawGizmosSelected()
        {
            Gizmos.color=new Color(.3f,.8f,.6f,.6f);
            Gizmos.DrawWireCube(new Vector3((WorldMinX+WorldMaxX)*.5f,0,4),new Vector3((localMaxX-localMinX)*pixelFrameScale,16.875f,1));
            Gizmos.color=Color.cyan;Gizmos.DrawWireSphere(transform.position+(Vector3)entryLocal*pixelFrameScale,.35f);
        }
    }
}
