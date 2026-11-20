using UnityEngine;
namespace CrossTheLine.Minigames
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class FastingVisionMask : UnityEngine.UI.MaskableGraphic
    {
        public Vector2 visibleWorldRadius=new Vector2(3.2f,3.6f);
        [Range(.01f,.3f)] public float edgeSoftness=.12f;
        public Color obscuredColor=Color.black;
        public float fadeInSeconds=1.6f,blackoutSeconds=.2f,fadeOutSeconds=.8f;
        public float recoveryGrace=.9f;
        float opacity,elapsed;bool active,requested;Vector2 center,uvRadius;
        public bool ObscuresGameplay=>active&&opacity>.55f;
        public void Clear(){active=requested=false;opacity=0;elapsed=0;SetVerticesDirty();}
        public void Advance(float dt){if(!active)return;elapsed+=Mathf.Max(0,dt);float total=fadeInSeconds+blackoutSeconds+fadeOutSeconds;if(elapsed>=total){active=false;opacity=0;}else if(elapsed<fadeInSeconds)opacity=Mathf.SmoothStep(0,1,elapsed/Mathf.Max(.01f,fadeInSeconds));else if(elapsed<fadeInSeconds+blackoutSeconds)opacity=1;else opacity=1-Mathf.SmoothStep(0,1,(elapsed-fadeInSeconds-blackoutSeconds)/Mathf.Max(.01f,fadeOutSeconds));SetVerticesDirty();}
        public float Opacity=>opacity;
        public Vector2 Center=>center;
        public void Set(float amount,Vector2 at,Camera camera)
        {
            if(amount>0&&!requested&&!active){active=true;elapsed=0;opacity=0;}
            requested=amount>0;
            center=at;
            uvRadius=new Vector2(visibleWorldRadius.x/(2*camera.orthographicSize*camera.aspect),visibleWorldRadius.y/(2*camera.orthographicSize));
            SetVerticesDirty();
        }
        protected override void Awake(){base.Awake();raycastTarget=false;}
        protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper vh)
        {
            vh.Clear();if(opacity<=0)return;Rect r=rectTransform.rect;const int columns=96,rows=54;
            for(int y=0;y<rows;y++)for(int x=0;x<columns;x++)
            {
                int n=vh.currentVertCount;Vertex(vh,r,x/(float)columns,y/(float)rows);Vertex(vh,r,x/(float)columns,(y+1f)/rows);Vertex(vh,r,(x+1f)/columns,(y+1f)/rows);Vertex(vh,r,(x+1f)/columns,y/(float)rows);vh.AddTriangle(n,n+1,n+2);vh.AddTriangle(n,n+2,n+3);
            }
        }
        void Vertex(UnityEngine.UI.VertexHelper vh,Rect r,float u,float v)
        {
            float radiusFactor=Mathf.Lerp(6,0,opacity);float x=(u-center.x)/Mathf.Max(.0001f,uvRadius.x*radiusFactor),y=(v-center.y)/Mathf.Max(.0001f,uvRadius.y*radiusFactor),d=Mathf.Sqrt(x*x+y*y);var c=obscuredColor;c.a*=opacity>=.999f?1:opacity*Mathf.SmoothStep(0,1,Mathf.InverseLerp(1-Mathf.Clamp(edgeSoftness*4,.05f,.9f),1,d));
            vh.AddVert(new Vector3(Mathf.Lerp(r.xMin,r.xMax,u),Mathf.Lerp(r.yMin,r.yMax,v)),c,Vector2.zero);
        }
    }
}
