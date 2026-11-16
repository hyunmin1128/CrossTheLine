using UnityEngine;
using UnityEngine.Sprites;

namespace CrossTheLine.Minigames
{
    /// <summary>Moves the consumed shield into screen space, then separates its texture into shards.</summary>
    public sealed class FastingShieldBreakGraphic : UnityEngine.UI.MaskableGraphic
    {
        public const float Duration=1.8f;
        public const float MinimumProtectionSeconds=.9f;
        public FastingEffectSprites effectSprites;
        Sprite sprite;
        Vector2 origin;
        float elapsed=Duration;
        public bool Active=>sprite&&elapsed<Duration;
        public float Progress=>Mathf.Clamp01(elapsed/Duration);
        Sprite IllustratedFrame=>effectSprites?FastingEffectSprites.Frame(effectSprites.shieldBreak,elapsed<.45f?0:elapsed<.60f?1:elapsed<.75f?2:elapsed<1.05f?3:elapsed<1.4f?4:5):null;
        public override Texture mainTexture=>IllustratedFrame?IllustratedFrame.texture:sprite?sprite.texture:Texture2D.whiteTexture;
        public void Play(Sprite artwork,Vector2 screenPoint)
        {
            sprite=artwork;var parentCanvas=canvas;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform,screenPoint,parentCanvas&&parentCanvas.renderMode!=RenderMode.ScreenSpaceOverlay?parentCanvas.worldCamera:null,out origin);
            elapsed=0;SetAllDirty();
        }
        public void Clear(){elapsed=Duration;SetVerticesDirty();}
        void Update(){if(Active)Advance(Time.deltaTime);}
        public void Advance(float dt){var before=IllustratedFrame;elapsed=Mathf.Min(Duration,elapsed+Mathf.Max(0,dt));if(before!=IllustratedFrame)SetMaterialDirty();SetVerticesDirty();}
        protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper vh)
        {
            vh.Clear();if(!Active)return;
            float zoom=Mathf.SmoothStep(0,1,elapsed/.45f),fracture=Mathf.Clamp01((elapsed-.75f)/1.05f);
            float crack=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.45f,.75f,elapsed));
            Vector2 center=Vector2.Lerp(origin,rectTransform.rect.center,zoom);
            float height=Mathf.Lerp(40,Mathf.Min(180,rectTransform.rect.height*.30f),zoom),width=height*sprite.rect.width/sprite.rect.height;
            if(IllustratedFrame)
            {
                var frame=IllustratedFrame;var bounds=DataUtility.GetOuterUV(frame);float size=height*1.25f;Color c=Color.white;c.a=1-Mathf.InverseLerp(1.6f,Duration,elapsed);
                vh.AddVert(center+new Vector2(-size/2,-size/2),c,new Vector2(bounds.x,bounds.y));vh.AddVert(center+new Vector2(-size/2,size/2),c,new Vector2(bounds.x,bounds.w));vh.AddVert(center+new Vector2(size/2,size/2),c,new Vector2(bounds.z,bounds.w));vh.AddVert(center+new Vector2(size/2,-size/2),c,new Vector2(bounds.z,bounds.y));vh.AddTriangle(0,1,2);vh.AddTriangle(0,2,3);return;
            }
            var uv=DataUtility.GetOuterUV(sprite);Color tint=Color.white;tint.a=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.35f,1,fracture));
            const int cells=4;
            for(int y=0;y<cells;y++)for(int x=0;x<cells;x++)
            {
                Vector2 p0=CrackPoint(x,y),p1=CrackPoint(x,y+1),p2=CrackPoint(x+1,y+1),p3=CrackPoint(x+1,y);
                Vector2 offset=(p0+p1+p2+p3)*.25f;
                Vector2 travel=offset.normalized*(40+((x*7+y*3)%5)*12)*fracture+Vector2.down*(100*fracture*fracture);
                Vector2 at=center+Vector2.Scale(offset,new Vector2(width,height))+travel;
                float spin=((x+y)%2==0?1:-1)*fracture*(25+x*12+y*7)*Mathf.Deg2Rad;
                float gap=fracture>0?Mathf.Lerp(.95f,.7f,fracture):1-.05f*crack;
                int i=vh.currentVertCount;ShardVertex(vh,p0,offset,at,width,height,gap,spin,tint,uv);ShardVertex(vh,p1,offset,at,width,height,gap,spin,tint,uv);ShardVertex(vh,p2,offset,at,width,height,gap,spin,tint,uv);ShardVertex(vh,p3,offset,at,width,height,gap,spin,tint,uv);vh.AddTriangle(i,i+1,i+2);vh.AddTriangle(i,i+2,i+3);
            }
        }
        static Vector2 CrackPoint(int x,int y)=>new Vector2(x/4f-.5f+(x==0||x==4?0:Mathf.Sin(x*17+y*31)*.055f),y/4f-.5f+(y==0||y==4?0:Mathf.Sin(x*29+y*13)*.055f));
        static void ShardVertex(UnityEngine.UI.VertexHelper vh,Vector2 p,Vector2 pivot,Vector2 at,float width,float height,float gap,float angle,Color tint,Vector4 uv)
        {
            Vector2 q=Vector2.Scale(p-pivot,new Vector2(width,height))*gap;
            Vector2 position=at+new Vector2(q.x*Mathf.Cos(angle)-q.y*Mathf.Sin(angle),q.x*Mathf.Sin(angle)+q.y*Mathf.Cos(angle));
            vh.AddVert(position,tint,new Vector2(Mathf.Lerp(uv.x,uv.z,p.x+.5f),Mathf.Lerp(uv.y,uv.w,p.y+.5f)));
        }
        protected override void OnDisable(){Clear();base.OnDisable();}
    }
}
