using UnityEngine;
namespace CrossTheLine.Minigames
{
    [RequireComponent(typeof(FastingTopDownHazard))]
    public sealed class FastingReaperSweep : MonoBehaviour
    {
        public enum AttackPhase { Following, Preparing, Dashing }
        public Color slashColor=new Color(1,.8f,.55f,.85f);
        public float slashWidth=.09f;
        public Sprite[] slashFrames;
        [Min(1)] public int firstSwingFrame=2;
        [Min(.05f)] public float attackAnimationSeconds=.4f;
        public float preparationSeconds=.10f,cutSeconds=.07f;
        [Min(.1f)] public float followSeconds=1.25f,lockSeconds=.85f;
        [Min(1)] public float followSpeed=10,dashSpeed=24;
        [Min(0)] public float topInset=.05f;
        [Header("사신 공격 사운드")]
        public AudioClip preparationSound, alternatePreparationSound, swingSound;
        [Range(0,1)] public float preparationSoundVolume = .65f, swingSoundVolume = .65f;
        public AttackPhase CurrentPhase{get;private set;}
        public bool Choreographed{get;private set;}
        public bool RangeVisible=>range&&range.enabled;
        FastingTopDownHazard hazard;
        FastingTopDownGame game;
        LineRenderer trail,range,rangeFill;
        float clock,minX,maxX;
        void Awake()
        {
            hazard=GetComponent<FastingTopDownHazard>();
            trail=MakeLine("StraightScytheSlash",2,slashWidth,slashColor,2,false);
            range=MakeLine("LockedAttackRange",5,.055f,new Color(1,.35f,.25f,.9f),-1,true);
            rangeFill=MakeLine("LockedAttackArea",2,1,new Color(1,.2f,.12f,.13f),-2,true);
        }
        LineRenderer MakeLine(string name,int count,float width,Color color,int order,bool worldSpace)
        {
            var child=new GameObject(name);child.transform.SetParent(transform,false);
            var line=child.AddComponent<LineRenderer>();line.useWorldSpace=worldSpace;line.positionCount=count;
            line.sharedMaterial=hazard.body.sharedMaterial;line.sortingLayerID=hazard.body.sortingLayerID;
            line.sortingOrder=hazard.body.sortingOrder+order;line.widthMultiplier=width;
            line.startColor=line.endColor=color;line.enabled=false;return line;
        }
        public void BeginTracking(FastingTopDownGame owner,int escapeLane)
        {
            game=owner;var road=game.SpawnRoadRange;float half=hazard.size.x*.5f;
            minX=road.x+half+.03f;maxX=road.y-half-.03f;
            // Keep the reserved escape lane clear even while tracking changes the attack's X.
            float escape=game.SpawnLaneX(escapeLane);
            if(escape<transform.position.x)minX=Mathf.Max(minX,escape+half+.6f);
            else maxX=Mathf.Min(maxX,escape-half-.6f);
            if(minX>maxX){hazard.spent=true;gameObject.SetActive(false);return;}
            Choreographed=true;CurrentPhase=AttackPhase.Following;clock=0;
            hazard.velocity=Vector2.zero;hazard.warning=followSeconds+lockSeconds;
            transform.position=new Vector3(Mathf.Clamp(transform.position.x,minX,maxX),HoverY(),0);
            ShowFrame(0);hazard.body.enabled=true;
        }
        float HoverY()
        {
            float top=game.gameCamera.transform.position.y+game.gameCamera.orthographicSize-topInset;
            return transform.position.y+top-hazard.body.bounds.max.y;
        }
        void ShowFrame(int index)
        {
            if(slashFrames!=null&&slashFrames.Length>0)hazard.body.sprite=slashFrames[Mathf.Min(index,slashFrames.Length-1)];
            hazard.body.transform.localRotation=Quaternion.identity;
        }
        // Returns only the time left for harmful motion after tracking/telegraph time is consumed.
        public bool PrepareStep(float dt,out float travelSeconds)
        {
            travelSeconds=0;
            if(!game||!game.player){hazard.spent=true;gameObject.SetActive(false);return false;}
            float remaining=Mathf.Max(0,dt);
            while(CurrentPhase!=AttackPhase.Dashing)
            {
                float duration=CurrentPhase==AttackPhase.Following?Mathf.Max(.1f,followSeconds):Mathf.Max(.1f,lockSeconds);
                float step=Mathf.Min(remaining,Mathf.Max(0,duration-clock));clock+=step;remaining-=step;
                if(CurrentPhase==AttackPhase.Following)
                {
                    float x=Mathf.MoveTowards(transform.position.x,Mathf.Clamp(game.player.position.x,minX,maxX),followSpeed*step);
                    transform.position=new Vector3(x,HoverY(),0);
                }
                else transform.position=new Vector3(transform.position.x,HoverY(),0);
                hazard.body.enabled=true;hazard.velocity=Vector2.zero;
                if(CurrentPhase==AttackPhase.Preparing)ShowFrame(Mathf.Min(firstSwingFrame-1,Mathf.FloorToInt(clock/duration*firstSwingFrame)));
                hazard.warning=Mathf.Max(.001f,duration-clock+(CurrentPhase==AttackPhase.Following?Mathf.Max(.1f,lockSeconds):0));
                if(clock+0.00001f<duration){RefreshLines();return false;}
                clock=0;
                if(CurrentPhase==AttackPhase.Following){CurrentPhase=AttackPhase.Preparing;game.PlayReaperLaugh(this);ShowFrame(0);}
                else{CurrentPhase=AttackPhase.Dashing;game.PlayObstacleSound(swingSound,swingSoundVolume);ShowFrame(firstSwingFrame);hazard.warning=0;hazard.velocity=Vector2.down*Mathf.Max(1,dashSpeed);}
                if(remaining<=0)break;
            }
            RefreshLines();travelSeconds=CurrentPhase==AttackPhase.Dashing?remaining:0;return travelSeconds>0;
        }
        public void AdvanceVisual(float attackSeconds)
        {
            if(Choreographed)
            {
                if(CurrentPhase==AttackPhase.Dashing&&slashFrames!=null&&slashFrames.Length>0)
                {
                    int first=Mathf.Clamp(firstSwingFrame,0,slashFrames.Length-1);
                    ShowFrame(first+Mathf.FloorToInt(Mathf.Clamp01(attackSeconds/Mathf.Max(.05f,attackAnimationSeconds))*(slashFrames.Length-first)));
                }
            }
            else ShowFrame(attackSeconds<preparationSeconds?0:attackSeconds<preparationSeconds+cutSeconds?1:2);
        }
        void RefreshLines()
        {
            bool alive=!hazard.spent&&hazard.body.enabled;
            trail.enabled=alive&&hazard.Active&&(!Choreographed||CurrentPhase==AttackPhase.Dashing);
            trail.SetPosition(0,new Vector3(-hazard.size.x*.5f,0,0));trail.SetPosition(1,new Vector3(hazard.size.x*.5f,0,0));
            range.enabled=rangeFill.enabled=alive&&Choreographed&&CurrentPhase==AttackPhase.Preparing;
            if(!range.enabled)return;
            float half=hazard.size.x*.5f,top=transform.position.y+hazard.size.y*.5f;
            float bottom=game.gameCamera.transform.position.y-game.gameCamera.orthographicSize;
            float left=transform.position.x-half,right=transform.position.x+half;
            range.SetPosition(0,new Vector3(left,top));range.SetPosition(1,new Vector3(right,top));range.SetPosition(2,new Vector3(right,bottom));range.SetPosition(3,new Vector3(left,bottom));range.SetPosition(4,new Vector3(left,top));
            rangeFill.widthMultiplier=hazard.size.x;rangeFill.SetPosition(0,new Vector3(transform.position.x,top));rangeFill.SetPosition(1,new Vector3(transform.position.x,bottom));
        }
        void LateUpdate(){RefreshLines();}
    }
}
