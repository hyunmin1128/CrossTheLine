using UnityEngine;
namespace CrossTheLine.Minigames
{
    public sealed class FastingTopDownHazard : MonoBehaviour
    {
        public enum Kind { Rock, Sand, Cactus, Quicksand, Bat, Collapse, Crack, Tomb, Wall, Poison, Fire, Thorns, Pillar, Reaper }
        public Kind kind;
        [Header("Hit Audio")]
        public AudioClip hitSound;
        [Range(0,1)] public float hitSoundVolume = .65f;
        public Vector2 size=new Vector2(1.7f,1),velocity;
        public float warning,lifetime=30;
        public bool spent;
        public SpriteRenderer body;
        [HideInInspector] public SpriteRenderer footprint,arrow;
        public int occupiedLanes=1;
        public float artworkWidth;
        public Vector2 artworkCenter;
        public Vector2 collisionOffset;
        public Vector2 CollisionCenter=>(Vector2)transform.position+collisionOffset;
        [HideInInspector] public float minimumVisibleReactionSeconds;
        float visibleSeconds;
        public bool ReadyToHit=>visibleSeconds>=minimumVisibleReactionSeconds;
        public void HideOutsideRoad(){body.enabled=false;if(secondBody)secondBody.enabled=false;}
        public bool TrackVisibility(Camera camera,float dt)
        {
            float hh=camera.orthographicSize,hw=hh*camera.aspect;var p=camera.transform.position;var b=body.bounds;
            bool firstVisibleFrame = false;
            if (dt > 0 && b.max.x >= p.x-hw && b.min.x <= p.x+hw && b.max.y >= p.y-hh && b.min.y <= p.y+hh)
            {
                firstVisibleFrame = visibleSeconds == 0;
                visibleSeconds += dt;
            }
            var color=body.color;color.a=1;body.color=color;if(secondBody)secondBody.color=color;
            return firstVisibleFrame;
        }
        public Vector2 RoadVisualRange
        {
            get
            {
                var bounds=body.sprite?body.sprite.bounds:new Bounds(Vector3.zero,Vector3.one);
                if(animationFrames!=null)foreach(var sprite in animationFrames)if(sprite)bounds.Encapsulate(sprite.bounds);
                if(reaperSweep&&reaperSweep.slashFrames!=null)foreach(var sprite in reaperSweep.slashFrames)if(sprite)bounds.Encapsulate(sprite.bounds);
                if(kind==Kind.Rock&&rotateRollingRock)
                {
                    float radius=new Vector2(Mathf.Abs(bounds.center.x)+bounds.extents.x,Mathf.Abs(bounds.center.y)+bounds.extents.y).magnitude;
                    bounds=new Bounds(Vector3.zero,new Vector3(radius*2,radius*2,0));
                }
                var range=WorldXRange(body.transform,bounds);
                if(secondBody){var second=WorldXRange(secondBody.transform,bounds);range.x=Mathf.Min(range.x,second.x);range.y=Mathf.Max(range.y,second.y);}
                return range;
            }
        }
        static Vector2 WorldXRange(Transform at,Bounds bounds)
        {
            float center=at.TransformPoint(bounds.center).x;
            float extent=Mathf.Abs(at.TransformVector(Vector3.right*bounds.extents.x).x)+Mathf.Abs(at.TransformVector(Vector3.up*bounds.extents.y).x);
            return new Vector2(center-extent,center+extent);
        }
        public float collisionHeight;
        public bool scrollOnly,rotateRollingRock=true;
        public bool expandsOnApproach;
        public bool fixedApproachScale;
        public bool authoredWallOrientation;
        [HideInInspector] public bool verticalWall;
        [HideInInspector] public int formationId,formationCount=1;
        public Sprite[] animationFrames;
        public float framesPerSecond=8;
        float phase;Vector3 baseBodyScale,bodyLaneOffset,secondLaneOffset;bool visualReady;SpriteRenderer secondBody;
        FastingReaperSweep reaperSweep;
        void Awake(){reaperSweep=GetComponent<FastingReaperSweep>();}
        public float ApproachScale{get;private set;}=1;
        public float quicksandMaximumScale=2;
        public float MaximumApproachScale=>1;
        Vector2 baseSize;
        public bool Active=>warning<=0&&!spent;
        public void SetArtwork(Sprite sprite)
        {
            body.sprite=sprite;body.color=Color.white;body.transform.localScale=Vector3.one;
            body.transform.localPosition=-sprite.bounds.center;visualReady=false;
            animationFrames=null;
        }
        public void SetAnimation(Sprite[] frames,float speed){animationFrames=frames;framesPerSecond=Mathf.Max(1,speed);if(frames!=null&&frames.Length>0)body.sprite=frames[0];}
        public void ConfigureSweep(float laneWidth){ConfigureLanes(1,laneWidth);occupiedLanes=4;size=baseSize=new Vector2(laneWidth*4,1.6f);fixedApproachScale=true;}
        public void Enlarge(float factor,bool preserveSweepWidth=false)
        {
            body.transform.localScale*=factor;body.transform.localPosition*=factor;
            if(secondBody){secondBody.transform.localScale*=factor;secondBody.transform.localPosition*=factor;}
            bodyLaneOffset*=factor;secondLaneOffset*=factor;
            size=baseSize=Vector2.Scale(baseSize,new Vector2(preserveSweepWidth?1:factor,factor));visualReady=false;
        }
        public void ConfigureWallSegment(bool vertical,float length,float thicknessMultiplier=1,float floorThickness=0)
        {
            verticalWall=vertical;
            // Slight overlap covers the transparent edge pixels in the authored wall sprite.
            length+=.16f;
            var bounds=body.sprite.bounds;bool tall=bounds.size.y>=bounds.size.x;
            body.transform.localRotation=Quaternion.Euler(0,0,authoredWallOrientation||vertical==tall?0:90);
            float scale=length/(authoredWallOrientation?(vertical?bounds.size.y:bounds.size.x):Mathf.Max(bounds.size.x,bounds.size.y));
            var wallScale=Vector3.one*scale;
            if(authoredWallOrientation){if(vertical)wallScale.x*=thicknessMultiplier;else wallScale.y*=thicknessMultiplier;}
            body.transform.localScale=baseBodyScale=wallScale;
            artworkCenter=bounds.center;bodyLaneOffset=Vector3.zero;
            body.transform.localPosition=-(body.transform.localRotation*Vector3.Scale(artworkCenter,wallScale));
            float thickness=(authoredWallOrientation?(vertical?bounds.size.x:bounds.size.y):Mathf.Min(bounds.size.x,bounds.size.y))*scale*(authoredWallOrientation?thicknessMultiplier:1);
            size=baseSize=vertical?new Vector2(thickness,length):new Vector2(length,thickness);
            collisionOffset=Vector2.zero;
            if(!vertical&&floorThickness>0){collisionOffset=Vector2.down*(thickness-floorThickness)*.5f;size=baseSize=new Vector2(length,floorThickness);}
            occupiedLanes=1;fixedApproachScale=true;visualReady=true;ApproachScale=1;
        }
        public void ConfigureLanes(int count,float laneWidth)
        {
            occupiedLanes=Mathf.Clamp(count,1,2);
            float desiredWidth=laneWidth*.75f;
            float visualWidth=artworkWidth>0?artworkWidth:body.sprite?body.sprite.bounds.size.x:1;
            float scale=desiredWidth/Mathf.Max(.01f,visualWidth);
            body.transform.localScale=Vector3.one*scale;
            bodyLaneOffset=occupiedLanes==2?new Vector3(-laneWidth*.45f,0):Vector3.zero;
            body.transform.localPosition=bodyLaneOffset-(Vector3)artworkCenter*scale;
            size=new Vector2((occupiedLanes-1)*laneWidth+desiredWidth,1.15f);
            if(kind==Kind.Sand)size.y=2.2f;
            if(collisionHeight>0)size.y=collisionHeight;
            baseSize=size;
            if(occupiedLanes==2){var o=new GameObject("SecondSandCloud");o.transform.SetParent(transform,false);secondLaneOffset=new Vector3(laneWidth*.45f,0);o.transform.localPosition=secondLaneOffset-(Vector3)artworkCenter*scale;o.transform.localScale=body.transform.localScale;secondBody=o.AddComponent<SpriteRenderer>();secondBody.sprite=body.sprite;secondBody.sharedMaterial=body.sharedMaterial;secondBody.color=body.color;secondBody.sortingOrder=body.sortingOrder;}
            visualReady=false;
        }
        public void UpdateApproach(Vector2 player)
        {
            if(!body)return;if(!visualReady){baseBodyScale=body.transform.localScale;visualReady=true;}
            ApproachScale=1;size=baseSize;
            body.transform.localScale=baseBodyScale*ApproachScale;
            if(artworkCenter!=Vector2.zero)body.transform.localPosition=bodyLaneOffset-(kind==Kind.Wall?body.transform.localRotation:Quaternion.identity)*(Vector3.Scale(artworkCenter,baseBodyScale)*ApproachScale);
            if(secondBody)secondBody.transform.localScale=baseBodyScale*ApproachScale;
            if(secondBody&&artworkCenter!=Vector2.zero)secondBody.transform.localPosition=secondLaneOffset-(Vector3)artworkCenter*(baseBodyScale.x*ApproachScale);
        }
        public bool Advance(float dt,Vector2 from,Vector2 to,out float time)
        {
            time=0;if(spent)return false;
            if(reaperSweep&&reaperSweep.Choreographed)
            {
                float fullStep=dt;
                if(!reaperSweep.PrepareStep(dt,out dt))return false;
                from=Vector2.Lerp(from,to,fullStep>0?Mathf.Clamp01(1-dt/fullStep):0);
            }
            else if(warning>0){float before=warning;warning-=dt;body.enabled=true;if(secondBody)secondBody.enabled=true;if(warning>0)return false;from=Vector2.Lerp(from,to,Mathf.Clamp01(before/dt));dt=Mathf.Max(0,dt-before);}
            phase+=dt;lifetime-=dt;
            if(reaperSweep)reaperSweep.AdvanceVisual(phase);
            if(animationFrames!=null&&animationFrames.Length>0){body.sprite=animationFrames[Mathf.FloorToInt(phase*framesPerSecond)%animationFrames.Length];if(secondBody){secondBody.sprite=body.sprite;secondBody.flipX=body.flipX;}}
            if(lifetime<=0){spent=true;gameObject.SetActive(false);return false;}
            bool damageWindowClosed=(kind==Kind.Pillar||kind==Kind.Crack)&&phase%2>1.1f;
            body.enabled=true;if(secondBody)secondBody.enabled=true;if(damageWindowClosed)return false;
            if(kind==Kind.Rock&&rotateRollingRock)body.transform.localRotation=Quaternion.Euler(0,0,(Mathf.FloorToInt(phase*10)%4)*90);
            // Speed surges stay on the authored path, preserving the reserved escape lane.
            float surge=!scrollOnly&&(kind==Kind.Rock||kind==Kind.Sand)?1+.18f*Mathf.Sin(phase*5+transform.position.x*.2f):1;
            Vector2 travel=velocity*surge;if(kind==Kind.Sand&&Mathf.Abs(velocity.x)>.01f)travel.y=velocity.y;
            Vector2 start=CollisionCenter;transform.position+=(Vector3)(travel*dt);
            Vector2 a=from-start,b=to-CollisionCenter,d=b-a,extent=size*.5f+new Vector2(.28f,.2f);
            if(kind==Kind.Sand)
            {
                // Swept ellipse: the moving cloud's opaque core, not the transparent corners.
                Vector2 na=a/extent,nd=d/extent;
                float closest=nd.sqrMagnitude>.000001f?Mathf.Clamp01(-Vector2.Dot(na,nd)/nd.sqrMagnitude):0;
                time=closest;return (na+nd*closest).sqrMagnitude<=1;
            }
            float enter=0,leave=1;
            for(int axis=0;axis<2;axis++){if(Mathf.Abs(d[axis])<.00001f){if(Mathf.Abs(a[axis])>extent[axis])return false;}else{float l=(-extent[axis]-a[axis])/d[axis],h=(extent[axis]-a[axis])/d[axis];if(l>h){float tmp=l;l=h;h=tmp;}enter=Mathf.Max(enter,l);leave=Mathf.Min(leave,h);if(enter>leave)return false;}}
            time=enter;return true;
        }
    }
}
