using UnityEngine;
namespace CrossTheLine.Minigames
{
    public sealed class FastingSandstorm : MonoBehaviour
    {
        public Vector2 hitSize=new Vector2(2.1f,.65f);
        public float speed=5;
        public bool spent;
        public SpriteRenderer groundFill;
        public Transform groundFrame;
        public Vector2 PreviousPosition{get;private set;}
        public Rect SweptArea{get;private set;}
        public void Advance(float dt)
        {
            var p=transform.position;PreviousPosition=p;float previous=p.x;p.x-=Mathf.Max(0,speed)*dt;transform.position=p;
            SweptArea=Rect.MinMaxRect(p.x-hitSize.x*.5f,p.y-hitSize.y*.5f,previous+hitSize.x*.5f,p.y+hitSize.y*.5f);
            RefreshFootprint();
        }
        public bool Hits(Vector2 feet)=>TryHit(feet,feet,new Vector2(.4f,.22f),out _);
        public bool TryHit(Vector2 previousFeet,Vector2 feet,Vector2 feetSize,out float entry)
        {
            entry=0;if(spent)return false;
            // Relative motion avoids counting a place the player had already left
            // as a hit merely because the wind crossed it later in the frame.
            var start=previousFeet-PreviousPosition;var end=feet-(Vector2)transform.position;
            var velocity=end-start;var half=(hitSize+feetSize)*.5f;float exit=1;
            for(int axis=0;axis<2;axis++){
                if(Mathf.Abs(velocity[axis])<.000001f){if(Mathf.Abs(start[axis])>half[axis])return false;continue;}
                float a=(-half[axis]-start[axis])/velocity[axis],b=(half[axis]-start[axis])/velocity[axis];
                entry=Mathf.Max(entry,Mathf.Min(a,b));exit=Mathf.Min(exit,Mathf.Max(a,b));if(entry>exit)return false;
            }
            return entry<=1&&exit>=0;
        }
        void OnValidate(){RefreshFootprint();}
        public void RefreshFootprint()
        {
            if(groundFill){groundFill.transform.localPosition=Vector3.zero;groundFill.transform.localScale=new Vector3(hitSize.x*32,hitSize.y*32,1);groundFill.enabled=!spent;}
            if(groundFrame){groundFrame.localScale=new Vector3(hitSize.x,hitSize.y,1);groundFrame.gameObject.SetActive(!spent);FastingReadability.FitFrame(groundFrame);}
        }
    }
}
