using UnityEngine;
using UnityEngine.Sprites;

namespace CrossTheLine.Minigames
{
    /// <summary>Moves the consumed shield into screen space, then separates its texture into shards.</summary>
    public sealed class FastingShieldBreakGraphic : UnityEngine.UI.MaskableGraphic
    {
        public const float Duration=.9f;
        Sprite sprite;
        Vector2 origin;
        float elapsed=Duration;
        public bool Active=>sprite&&elapsed<Duration;
        public float Progress=>Mathf.Clamp01(elapsed/Duration);
        public override Texture mainTexture=>sprite?sprite.texture:Texture2D.whiteTexture;
        public void Play(Sprite artwork,Vector2 screenPoint)
        {
            sprite=artwork;var parentCanvas=canvas;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform,screenPoint,parentCanvas&&parentCanvas.renderMode!=RenderMode.ScreenSpaceOverlay?parentCanvas.worldCamera:null,out origin);
            elapsed=0;SetAllDirty();
        }
        public void Clear(){elapsed=Duration;SetVerticesDirty();}
        void Update(){if(Active)Advance(Time.deltaTime);}
        public void Advance(float dt){elapsed=Mathf.Min(Duration,elapsed+Mathf.Max(0,dt));SetVerticesDirty();}
        protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper vh)
        {
            vh.Clear();if(!Active)return;
            float zoom=Mathf.SmoothStep(0,1,elapsed/.3f),fracture=Mathf.Clamp01((elapsed-.3f)/.6f);
            Vector2 center=Vector2.Lerp(origin,rectTransform.rect.center,zoom);
            float height=Mathf.Lerp(40,Mathf.Min(180,rectTransform.rect.height*.30f),zoom),width=height*sprite.rect.width/sprite.rect.height;
            var uv=DataUtility.GetOuterUV(sprite);Color tint=Color.white;tint.a=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.35f,1,fracture));
            const int cells=4;
            for(int y=0;y<cells;y++)for(int x=0;x<cells;x++)
            {
                Vector2 offset=new Vector2((x+.5f)/cells-.5f,(y+.5f)/cells-.5f);
                Vector2 travel=offset.normalized*(40+((x*7+y*3)%5)*12)*fracture+Vector2.down*(100*fracture*fracture);
                Vector2 at=center+Vector2.Scale(offset,new Vector2(width,height))+travel;
                float spin=((x+y)%2==0?1:-1)*fracture*(25+x*12+y*7)*Mathf.Deg2Rad;
                float gap=fracture>0?Mathf.Lerp(.96f,.7f,fracture):1;
                Vector2 side=new Vector2(Mathf.Cos(spin),Mathf.Sin(spin))*width/cells*.5f*gap;
                Vector2 up=new Vector2(-Mathf.Sin(spin),Mathf.Cos(spin))*height/cells*.5f*gap;
                float u0=Mathf.Lerp(uv.x,uv.z,(float)x/cells),u1=Mathf.Lerp(uv.x,uv.z,(float)(x+1)/cells),v0=Mathf.Lerp(uv.y,uv.w,(float)y/cells),v1=Mathf.Lerp(uv.y,uv.w,(float)(y+1)/cells);
                int i=vh.currentVertCount;vh.AddVert(at-side-up,tint,new Vector2(u0,v0));vh.AddVert(at-side+up,tint,new Vector2(u0,v1));vh.AddVert(at+side+up,tint,new Vector2(u1,v1));vh.AddVert(at+side-up,tint,new Vector2(u1,v0));vh.AddTriangle(i,i+1,i+2);vh.AddTriangle(i,i+2,i+3);
            }
        }
        protected override void OnDisable(){Clear();base.OnDisable();}
    }
}
