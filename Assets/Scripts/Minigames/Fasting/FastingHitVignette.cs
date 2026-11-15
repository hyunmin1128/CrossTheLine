using UnityEngine;

namespace CrossTheLine.Minigames
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class FastingHitVignette : UnityEngine.UI.MaskableGraphic
    {
        public enum HitKind { None, Damage, Shield }
        public float damageSeconds=.65f,shieldSeconds=.45f,attackSeconds=.08f;
        [Range(1,3)] public float opacityMultiplier=1.9f;
        [Range(1,3)] public float durationMultiplier=1.7f;
        public float peakHoldSeconds=.25f;
        [Range(.2f,.6f)] public float maximumOpacity=.4f;
        [Range(.5f,.9f)] public float clearCenter=.72f;
        public Color damageColor=new Color(.68f,.08f,.14f,.18f);
        public Color shieldColor=new Color(.18f,.52f,1,.12f);
        [System.Serializable]
        public sealed class ObstacleStyle
        {
            public FastingTopDownHazard.Kind obstacle;
            public Color tint;
            public float seconds;
            public ObstacleStyle(FastingTopDownHazard.Kind obstacle,Color tint,float seconds){this.obstacle=obstacle;this.tint=tint;this.seconds=seconds;}
        }
        public ObstacleStyle[] obstacleStyles=DefaultStyles();
        ObstacleStyle style;
        public FastingTopDownHazard.Kind? CurrentObstacle{get;private set;}
        public HitKind CurrentKind{get;private set;}
        public float Elapsed{get;private set;}
        public bool Active=>CurrentKind!=HitKind.None;
        float Duration=>(CurrentKind==HitKind.Shield?shieldSeconds:style!=null?style.seconds:damageSeconds)*durationMultiplier;
        public float CurrentDuration=>Duration;
        protected override void Awake(){base.Awake();raycastTarget=false;}
        public void Play(bool shield){style=null;CurrentObstacle=null;CurrentKind=shield?HitKind.Shield:HitKind.Damage;Elapsed=0;SetVerticesDirty();}
        public void PlayDamage(string cause){if(System.Enum.TryParse(cause,true,out FastingTopDownHazard.Kind obstacle))PlayDamage(obstacle);else Play(false);}
        public void PlayDamage(FastingTopDownHazard.Kind obstacle)
        {
            Play(false);CurrentObstacle=obstacle;if(obstacleStyles!=null)foreach(var candidate in obstacleStyles)if(candidate!=null&&candidate.obstacle==obstacle){style=candidate;break;}SetVerticesDirty();
        }
        public void Clear(){style=null;CurrentObstacle=null;CurrentKind=HitKind.None;Elapsed=0;SetVerticesDirty();}
        void Update(){if(Active)Advance(Time.deltaTime);}
        public void Advance(float dt){if(!Active)return;Elapsed+=Mathf.Max(0,dt);if(Elapsed>=Duration){CurrentKind=HitKind.None;CurrentObstacle=null;style=null;}SetVerticesDirty();}
        protected override void OnDisable(){Clear();base.OnDisable();}
        protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper vh)
        {
            vh.Clear();if(!Active)return;
            Rect r=rectTransform.rect;float start=Mathf.Max(.001f,attackSeconds);
            float holdEnd=Mathf.Min(start+peakHoldSeconds,Duration*.6f);
            float envelope=Elapsed<start?Mathf.SmoothStep(0,1,Elapsed/start):1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(holdEnd,Mathf.Max(holdEnd+.001f,Duration),Elapsed));
            Color tint=CurrentKind==HitKind.Shield?shieldColor:style!=null?style.tint:damageColor;
            const int columns=48,rows=28;
            for(int y=0;y<rows;y++)for(int x=0;x<columns;x++)
            {
                int i=vh.currentVertCount;Vertex(vh,r,x/(float)columns,y/(float)rows,tint,envelope);Vertex(vh,r,x/(float)columns,(y+1f)/rows,tint,envelope);Vertex(vh,r,(x+1f)/columns,(y+1f)/rows,tint,envelope);Vertex(vh,r,(x+1f)/columns,y/(float)rows,tint,envelope);vh.AddTriangle(i,i+1,i+2);vh.AddTriangle(i,i+2,i+3);
            }
        }
        void Vertex(UnityEngine.UI.VertexHelper vh,Rect r,float u,float v,Color tint,float envelope)
        {
            float x=Mathf.Abs(u*2-1),y=Mathf.Abs(v*2-1);
            float distance=Mathf.Pow(x*x*x*x+y*y*y*y,.25f);
            float intensity=Mathf.SmoothStep(0,1,Mathf.InverseLerp(clearCenter,1.08f,distance));
            if(style!=null)
            {
                float angle=Mathf.Atan2(v-.5f,u-.5f),pattern=Pattern(style.obstacle,u,v,angle);
                intensity*=Mathf.Clamp01(pattern);
                // Keep a restrained common damage cue at the very outside, without stacking opacity.
                Color baseTint=tint;tint=Color.Lerp(baseTint,damageColor,Mathf.SmoothStep(0,1,Mathf.InverseLerp(.96f,1.18f,distance))*.45f);tint.a=baseTint.a;
            }
            intensity=Mathf.Round(intensity*8)/8; // restrained steps retain the pixel-art character
            tint.a=Mathf.Min(maximumOpacity,tint.a*intensity*envelope*opacityMultiplier);
            vh.AddVert(new Vector3(Mathf.Lerp(r.xMin,r.xMax,u),Mathf.Lerp(r.yMin,r.yMax,v)),tint,Vector2.zero);
        }
        float Pattern(FastingTopDownHazard.Kind obstacle,float u,float v,float angle)
        {
            float t=Elapsed;
            switch(obstacle)
            {
                case FastingTopDownHazard.Kind.Rock:return .65f+.35f*Hash(Mathf.Floor(u*13),Mathf.Floor(v*9));
                case FastingTopDownHazard.Kind.Cactus:return .45f+.55f*Mathf.Pow(Mathf.Abs(Mathf.Sin(angle*12)),4);
                case FastingTopDownHazard.Kind.Sand:return .7f+.3f*Mathf.Sin(v*34+u*13-t*16);
                case FastingTopDownHazard.Kind.Quicksand:return (.4f+.6f*(1-v))*(.8f+.2f*Mathf.Sin(angle*6-t*7));
                case FastingTopDownHazard.Kind.Bat:return .65f+.35f*Mathf.Abs(Mathf.Sin(angle*5+t*5));
                case FastingTopDownHazard.Kind.Collapse:return .55f+.45f*Hash(Mathf.Floor(u*11),Mathf.Floor(v*8+t*5));
                case FastingTopDownHazard.Kind.Crack:return .35f+.65f*Mathf.Pow(Mathf.Abs(Mathf.Sin(angle*9+u*5)),5);
                case FastingTopDownHazard.Kind.Tomb:return .65f+.35f*Mathf.Pow(Mathf.Cos(angle*7+t*2),2);
                case FastingTopDownHazard.Kind.Wall:return .55f+.45f*Mathf.Repeat(Mathf.Floor(u*16)+Mathf.Floor(v*10),2);
                case FastingTopDownHazard.Kind.Poison:return .7f+.3f*Mathf.Sin(angle*7-t*6+v*13);
                case FastingTopDownHazard.Kind.Fire:return (.65f+.35f*(1-v))*(.8f+.2f*Mathf.Sin(u*43-v*17+t*12));
                case FastingTopDownHazard.Kind.Thorns:return .4f+.6f*Mathf.Pow(Mathf.Abs(Mathf.Sin(angle*18+v*6)),3);
                case FastingTopDownHazard.Kind.Pillar:return .5f+.5f*Mathf.Pow(Mathf.Sin(u*55+t*3),2);
                case FastingTopDownHazard.Kind.Reaper:return .65f+.35f*Mathf.Pow(Mathf.Cos(angle*3-t*2),4);
                default:return 1;
            }
        }
        static float Hash(float x,float y)=>Mathf.Repeat(Mathf.Sin(x*127.1f+y*311.7f)*43758.5453f,1);
        public static ObstacleStyle[] DefaultStyles()=>new[]{
            new ObstacleStyle(FastingTopDownHazard.Kind.Rock,new Color(.48f,.22f,.16f,.18f),.60f),
            new ObstacleStyle(FastingTopDownHazard.Kind.Sand,new Color(.72f,.48f,.14f,.18f),.80f),
            new ObstacleStyle(FastingTopDownHazard.Kind.Cactus,new Color(.22f,.47f,.12f,.18f),.65f),
            new ObstacleStyle(FastingTopDownHazard.Kind.Quicksand,new Color(.42f,.25f,.08f,.18f),.95f),
            new ObstacleStyle(FastingTopDownHazard.Kind.Bat,new Color(.32f,.22f,.52f,.16f),.75f),
            new ObstacleStyle(FastingTopDownHazard.Kind.Collapse,new Color(.46f,.35f,.25f,.18f),.70f),
            new ObstacleStyle(FastingTopDownHazard.Kind.Crack,new Color(.35f,.20f,.19f,.18f),.65f),
            new ObstacleStyle(FastingTopDownHazard.Kind.Tomb,new Color(.4f,.25f,.52f,.16f),.80f),
            new ObstacleStyle(FastingTopDownHazard.Kind.Wall,new Color(.35f,.4f,.48f,.16f),.60f),
            new ObstacleStyle(FastingTopDownHazard.Kind.Poison,new Color(.42f,.53f,.12f,.18f),.90f),
            new ObstacleStyle(FastingTopDownHazard.Kind.Fire,new Color(.95f,.32f,.05f,.18f),.75f),
            new ObstacleStyle(FastingTopDownHazard.Kind.Thorns,new Color(.27f,.32f,.11f,.18f),.75f),
            new ObstacleStyle(FastingTopDownHazard.Kind.Pillar,new Color(1,.48f,.08f,.18f),.85f),
            new ObstacleStyle(FastingTopDownHazard.Kind.Reaper,new Color(.32f,.17f,.4f,.18f),.90f)};
    }
}
