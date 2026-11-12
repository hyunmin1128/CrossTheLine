using UnityEngine;

namespace CrossTheLine.Minigames
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class FastingScreenHitFeedback : UnityEngine.UI.MaskableGraphic
    {
        [Range(.2f,1f)] public float effectDuration=.55f;
        [Range(.05f,.22f)] public float edgeDepth=.16f;
        public Color stoneColor=new Color(.32f,.23f,.17f,.95f);
        public Color thornColor=new Color(.14f,.25f,.10f,.95f);
        public Color sandColor=new Color(.65f,.39f,.12f,.85f);
        public Color shieldColor=new Color(1,.85f,.25f,.85f);
        public Color damageEdgeColor=new Color(.8f,.12f,.12f,.12f);
        float remaining;
        bool shield;
        FastingTopDownHazard.Kind kind;
        public bool Active=>remaining>0;
        protected override void Awake(){base.Awake();raycastTarget=false;}
        public void Play(FastingTopDownHazard.Kind cause,bool blocked=false){kind=cause;shield=blocked;remaining=effectDuration;SetVerticesDirty();}
        public void Clear(){remaining=0;SetVerticesDirty();}
        void Update(){if(Active)Advance(Time.deltaTime);}
        public void Advance(float dt){remaining=Mathf.Max(0,remaining-dt);SetVerticesDirty();}
        protected override void OnDisable(){Clear();base.OnDisable();}
        protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper vh)
        {
            vh.Clear();if(!Active)return;
            Rect r=rectTransform.rect;float t=1-remaining/Mathf.Max(.01f,effectDuration);
            float fade=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.45f,1,t));
            bool sand=kind==FastingTopDownHazard.Kind.Sand;
            bool pit=kind==FastingTopDownHazard.Kind.Quicksand;
            bool thorn=kind==FastingTopDownHazard.Kind.Cactus||kind==FastingTopDownHazard.Kind.Thorns;
            Color c=shield?shieldColor:thorn?thornColor:sand||pit?sandColor:stoneColor;c.a*=fade;
            float unit=r.height/540f,depth=r.height*edgeDepth;
            if(!shield){Color red=damageEdgeColor;red.a*=fade;float band=12*unit;Quad(vh,r.xMin+band/2,r.center.y,band/2,r.height/2,red);Quad(vh,r.xMax-band/2,r.center.y,band/2,r.height/2,red);Quad(vh,r.center.x,r.yMin+band/2,r.width/2,band/2,red);Quad(vh,r.center.x,r.yMax-band/2,r.width/2,band/2,red);}
            // All geometry stays at the edges. No full-screen tint or input interception.
            if(shield)
            {
                for(int side=0;side<2;side++)for(int n=0;n<12;n++)
                {float y=r.yMin+r.height*(n+.5f)/12;float x=side==0?r.xMin+18*unit:r.xMax-18*unit;Quad(vh,x,y,14*unit,20*unit,c);}
                Quad(vh,r.center.x,r.yMax-10*unit,r.width/2,10*unit,c);Quad(vh,r.center.x,r.yMin+10*unit,r.width/2,10*unit,c);
                return;
            }
            if(pit)
            {
                float rise=Mathf.Sin(t*Mathf.PI)*depth;
                for(int n=0;n<40;n++)
                {float w=r.width/40;float h=rise*(.70f+.30f*Hash(n));Quad(vh,r.xMin+(n+.5f)*w,r.yMin+h*.5f,w*.52f,h*.5f,c);}
                return;
            }
            int count=sand?56:22;
            for(int n=0;n<count;n++)
            {
                int side=n%4;float along=Hash(n*3+1),inset=Hash(n*5+3)*depth;
                if(sand)along=Mathf.Repeat(along+t*(.35f+Hash(n)*.4f),1);
                else inset*=1-Mathf.Min(1,t*1.3f);
                float x,y;
                if(side<2){x=side==0?r.xMin+inset:r.xMax-inset;y=r.yMin+along*r.height;}
                else{x=r.xMin+along*r.width;y=side==2?r.yMin+inset*.65f:r.yMax-inset*.65f;}
                x=Mathf.Round(x/(2*unit))*2*unit;y=Mathf.Round(y/(2*unit))*2*unit;
                if(thorn)
                {
                    Vector2 inward=side==0?Vector2.right:side==1?Vector2.left:side==2?Vector2.up:Vector2.down;
                    Thorn(vh,new Vector2(x,y),inward,unit*(12+Hash(n)*14),c);
                }
                else{float size=unit*(sand?2+Hash(n)*4:4+Hash(n)*7);Quad(vh,x,y,sand?size*2:size,sand?size*.5f:size,c);}
            }
        }
        static float Hash(int n)=>Mathf.Repeat(Mathf.Sin(n*127.1f+19.7f)*43758.5453f,1);
        static void Quad(UnityEngine.UI.VertexHelper vh,float x,float y,float hw,float hh,Color c)
        {
            int i=vh.currentVertCount;vh.AddVert(new Vector3(x-hw,y-hh),c,Vector2.zero);vh.AddVert(new Vector3(x-hw,y+hh),c,Vector2.zero);vh.AddVert(new Vector3(x+hw,y+hh),c,Vector2.zero);vh.AddVert(new Vector3(x+hw,y-hh),c,Vector2.zero);vh.AddTriangle(i,i+1,i+2);vh.AddTriangle(i,i+2,i+3);
        }
        static void Thorn(UnityEngine.UI.VertexHelper vh,Vector2 p,Vector2 dir,float length,Color c)
        {
            // Stepped silhouette rather than a smooth triangular spike.
            for(int k=0;k<4;k++){Vector2 at=p+dir*(length*k/4);float half=length*(4-k)/16;Quad(vh,at.x,at.y,dir.x==0?half:length/7,dir.y==0?half:length/7,c);}
        }
    }
}
