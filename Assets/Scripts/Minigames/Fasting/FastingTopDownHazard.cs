using UnityEngine;
namespace CrossTheLine.Minigames
{
    public sealed class FastingTopDownHazard : MonoBehaviour
    {
        public enum Kind { Rock, Sand, Cactus, Quicksand, Bat, Collapse, Crack, Tomb, Wall, Poison, Fire, Thorns, Pillar, Reaper }
        public Kind kind;
        public Vector2 size=new Vector2(1.7f,1), velocity;
        public float warning=1.4f, lifetime=8;
        public bool spent;
        float phase;
        public SpriteRenderer body,footprint,arrow;
        public bool Active=>warning<=0&&!spent;
        public bool Advance(float dt,Vector2 from,Vector2 to,out float time)
        {
            time=0;if(spent)return false;
            if(warning>0){float before=warning;warning-=dt;footprint.color=new Color(1,.7f,.12f,.45f);body.enabled=false;arrow.enabled=true;
                if(warning>0)return false;
                float residual=Mathf.Max(0,dt-before);from=Vector2.Lerp(from,to,Mathf.Clamp01(before/dt));dt=residual;
            }
            phase+=dt;
            bool intermittent=kind==Kind.Pillar||kind==Kind.Crack;
            if(intermittent&&phase%2>1.1f){body.enabled=false;footprint.color=new Color(1,.7f,.12f,.35f);lifetime-=dt;if(lifetime<=0){spent=true;gameObject.SetActive(false);}return false;}
            body.enabled=true;footprint.color=new Color(1,.12f,.08f,.65f);arrow.enabled=true;
            Vector2 start=transform.position;transform.position+=(Vector3)(velocity*dt);lifetime-=dt;
            if(lifetime<=0){spent=true;gameObject.SetActive(false);return false;}
            Vector2 a=from-start,b=to-(Vector2)transform.position,d=b-a,extent=size*.5f+new Vector2(.28f,.2f);
            float enter=0,leave=1;
            for(int axis=0;axis<2;axis++){if(Mathf.Abs(d[axis])<.00001f){if(Mathf.Abs(a[axis])>extent[axis])return false;}else{float l=(-extent[axis]-a[axis])/d[axis],h=(extent[axis]-a[axis])/d[axis];if(l>h){float tmp=l;l=h;h=tmp;}enter=Mathf.Max(enter,l);leave=Mathf.Min(leave,h);if(enter>leave)return false;}}
            time=enter;return true;
        }
    }
}
