using UnityEngine;
namespace CrossTheLine.Minigames
{
    public sealed class FastingSandstorm : MonoBehaviour
    {
        public Vector2 hitSize=new Vector2(2.1f,.65f);
        public float speed=5;
        public bool spent;
        public Rect SweptArea{get;private set;}
        public void Advance(float dt)
        {
            var p=transform.position;float previous=p.x;p.x-=Mathf.Max(0,speed)*dt;transform.position=p;
            SweptArea=Rect.MinMaxRect(p.x-hitSize.x*.5f,p.y-hitSize.y*.5f,previous+hitSize.x*.5f,p.y+hitSize.y*.5f);
        }
        public bool Hits(Vector2 feet)=>!spent&&SweptArea.Overlaps(new Rect(feet-new Vector2(.2f,.15f),new Vector2(.4f,.45f)));
    }
}
