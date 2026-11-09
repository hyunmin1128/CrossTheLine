using UnityEngine;
using UnityEngine.UI;
namespace CrossTheLine.Minigames
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class FastingFeverRing : MaskableGraphic
    {
        [Range(0,1)] public float fill;
        public float thickness=8;
        public bool fever;
        public void Set(float amount,bool active){if(Mathf.Abs(fill-amount)<.0001f&&fever==active)return;fill=amount;fever=active;SetVerticesDirty();}
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();var rect=GetPixelAdjustedRect();float radius=Mathf.Min(rect.width,rect.height)*.5f,inner=Mathf.Max(0,radius-thickness);Vector2 center=rect.center;
            for(int i=0;i<64;i++){Add(vh,center,radius,inner,i/64f,(i+1)/64f,new Color(.19f,.14f,.1f,.98f));float start=i/64f,end=Mathf.Min((i+1)/64f,fill);if(end>start)Add(vh,center,radius,inner,start,end,fever?new Color(1,.34f,.08f):new Color(1,.72f,.15f));}
        }
        static Vector2 Point(Vector2 c,float r,float t){float a=t*Mathf.PI*2;return c+new Vector2(Mathf.Round(Mathf.Sin(a)*r),Mathf.Round(Mathf.Cos(a)*r));}
        static void Add(VertexHelper vh,Vector2 c,float outer,float inner,float start,float end,Color color){int n=vh.currentVertCount;vh.AddVert(Point(c,outer,start),color,Vector2.zero);vh.AddVert(Point(c,outer,end),color,Vector2.zero);vh.AddVert(Point(c,inner,end),color,Vector2.zero);vh.AddVert(Point(c,inner,start),color,Vector2.zero);vh.AddTriangle(n,n+1,n+2);vh.AddTriangle(n,n+2,n+3);}
    }
}
