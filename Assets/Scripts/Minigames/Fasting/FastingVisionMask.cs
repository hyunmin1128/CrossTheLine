using UnityEngine;
namespace CrossTheLine.Minigames
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class FastingVisionMask : UnityEngine.UI.MaskableGraphic
    {
        public Vector2 visibleWorldRadius=new Vector2(3.2f,3.6f);
        [Range(.01f,.3f)] public float edgeSoftness=.12f;
        public Color obscuredColor=Color.black;
        float opacity;Vector2 center,uvRadius;
        public float Opacity=>opacity;
        public Vector2 Center=>center;
        public void Set(float amount,Vector2 at,Camera camera)
        {
            var radius=new Vector2(visibleWorldRadius.x/(2*camera.orthographicSize*camera.aspect),visibleWorldRadius.y/(2*camera.orthographicSize));
            if(Mathf.Abs(amount-opacity)<.0001f&&(at-center).sqrMagnitude<.00000001f&&radius==uvRadius)return;
            opacity=Mathf.Clamp01(amount);center=at;uvRadius=radius;SetVerticesDirty();
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
            float x=(u-center.x)/Mathf.Max(.001f,uvRadius.x),y=(v-center.y)/Mathf.Max(.001f,uvRadius.y),d=Mathf.Sqrt(x*x+y*y);var c=obscuredColor;c.a*=opacity*Mathf.SmoothStep(0,1,Mathf.InverseLerp(1-edgeSoftness,1,d));
            vh.AddVert(new Vector3(Mathf.Lerp(r.xMin,r.xMax,u),Mathf.Lerp(r.yMin,r.yMax,v)),c,Vector2.zero);
        }
    }
}
