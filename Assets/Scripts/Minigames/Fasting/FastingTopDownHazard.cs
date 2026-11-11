using UnityEngine;
namespace CrossTheLine.Minigames
{
    public sealed class FastingTopDownHazard : MonoBehaviour
    {
        public enum Kind { Rock, Sand, Cactus, Quicksand, Bat, Collapse, Crack, Tomb, Wall, Poison, Fire, Thorns, Pillar, Reaper }
        public Kind kind;
        public Vector2 size=new Vector2(1.7f,1),velocity;
        public float warning,lifetime=30;
        public bool spent;
        public SpriteRenderer body;
        [HideInInspector] public SpriteRenderer footprint,arrow;
        public int occupiedLanes=1;
        public float artworkWidth;
        public Sprite[] animationFrames;
        public float framesPerSecond=8;
        float phase;Vector3 baseBodyScale;bool visualReady;SpriteRenderer secondBody;
        public float ApproachScale{get;private set;}=1;
        public bool Active=>warning<=0&&!spent;
        public void SetArtwork(Sprite sprite)
        {
            body.sprite=sprite;body.color=Color.white;body.transform.localScale=Vector3.one;
            body.transform.localPosition=-sprite.bounds.center;visualReady=false;
            animationFrames=null;
        }
        public void SetAnimation(Sprite[] frames,float speed){animationFrames=frames;framesPerSecond=Mathf.Max(1,speed);if(frames!=null&&frames.Length>0)body.sprite=frames[0];}
        public void ConfigureLanes(int count,float laneWidth)
        {
            occupiedLanes=Mathf.Clamp(count,1,2);
            float desiredWidth=laneWidth*.75f;
            float visualWidth=artworkWidth>0?artworkWidth:body.sprite?body.sprite.bounds.size.x:1;
            float scale=desiredWidth/Mathf.Max(.01f,visualWidth);
            body.transform.localScale=Vector3.one*scale;
            body.transform.localPosition=occupiedLanes==2?new Vector3(-laneWidth*.45f,0):Vector3.zero;
            size=new Vector2((occupiedLanes-1)*laneWidth+desiredWidth,1.15f);
            if(occupiedLanes==2){var o=new GameObject("SecondSandCloud");o.transform.SetParent(transform,false);o.transform.localPosition=new Vector3(laneWidth*.45f,0);o.transform.localScale=body.transform.localScale;secondBody=o.AddComponent<SpriteRenderer>();secondBody.sprite=body.sprite;secondBody.sharedMaterial=body.sharedMaterial;secondBody.color=body.color;secondBody.sortingOrder=body.sortingOrder;}
            visualReady=false;
        }
        public void UpdateApproach(Vector2 player)
        {
            if(!body)return;if(!visualReady){baseBodyScale=body.transform.localScale;visualReady=true;}
            ApproachScale=Mathf.Lerp(.8f,1.25f,1-Mathf.Clamp01(Vector2.Distance(player,transform.position)/9));
            body.transform.localScale=baseBodyScale*ApproachScale;
            if(secondBody)secondBody.transform.localScale=baseBodyScale*ApproachScale;
        }
        public bool Advance(float dt,Vector2 from,Vector2 to,out float time)
        {
            time=0;if(spent)return false;
            if(warning>0){float before=warning;warning-=dt;body.enabled=false;if(secondBody)secondBody.enabled=false;if(warning>0)return false;from=Vector2.Lerp(from,to,Mathf.Clamp01(before/dt));dt=Mathf.Max(0,dt-before);}
            phase+=dt;lifetime-=dt;
            if(animationFrames!=null&&animationFrames.Length>0){body.sprite=animationFrames[Mathf.FloorToInt(phase*framesPerSecond)%animationFrames.Length];if(secondBody){secondBody.sprite=body.sprite;secondBody.flipX=body.flipX;}}
            if(lifetime<=0){spent=true;gameObject.SetActive(false);return false;}
            bool visible=!((kind==Kind.Pillar||kind==Kind.Crack)&&phase%2>1.1f);
            body.enabled=visible;if(secondBody)secondBody.enabled=visible;if(!visible)return false;
            if(kind==Kind.Rock)body.transform.localRotation=Quaternion.Euler(0,0,(Mathf.FloorToInt(phase*6)%4)*90);
            Vector2 start=transform.position;transform.position+=(Vector3)(velocity*dt);
            Vector2 a=from-start,b=to-(Vector2)transform.position,d=b-a,extent=size*.5f+new Vector2(.28f,.2f);
            float enter=0,leave=1;
            for(int axis=0;axis<2;axis++){if(Mathf.Abs(d[axis])<.00001f){if(Mathf.Abs(a[axis])>extent[axis])return false;}else{float l=(-extent[axis]-a[axis])/d[axis],h=(extent[axis]-a[axis])/d[axis];if(l>h){float tmp=l;l=h;h=tmp;}enter=Mathf.Max(enter,l);leave=Mathf.Min(leave,h);if(enter>leave)return false;}}
            time=enter;return true;
        }
    }
}
