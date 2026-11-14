using System.Collections.Generic;
using UnityEngine;

namespace CrossTheLine.Minigames
{
    [RequireComponent(typeof(MeshFilter),typeof(MeshRenderer))]
    public sealed class FastingPlayerAura : MonoBehaviour
    {
        public FastingTopDownGame game;
        public SpriteRenderer artwork;
        public Material effectMaterial;
        public Color shieldColor=new Color(.22f,.7f,1,.7f),boostColor=new Color(.35f,.88f,1,.8f);
        public Vector2 shieldRadius=new Vector2(.85f,1.15f);
        public float breakSeconds=.5f;
        Mesh mesh;Material material;MeshRenderer auraRenderer;
        float breakRemaining,clock;
        public bool Breaking=>breakRemaining>0;
        public bool BoostVisible=>game&&game.WaterBoostRemaining>0;
        readonly List<Vector3> vertices=new List<Vector3>();readonly List<Color> colors=new List<Color>();readonly List<int> triangles=new List<int>();
        void Awake()
        {
            mesh=new Mesh{name="Shield and water boost pixels"};mesh.MarkDynamic();GetComponent<MeshFilter>().sharedMesh=mesh;
            auraRenderer=GetComponent<MeshRenderer>();material=new Material(effectMaterial);auraRenderer.sharedMaterial=material;
            if(artwork){auraRenderer.sortingLayerID=artwork.sortingLayerID;auraRenderer.sortingOrder=artwork.sortingOrder+8;}
        }
        public void BreakShield(){breakRemaining=breakSeconds;}
        public void Clear(){breakRemaining=0;if(mesh)mesh.Clear();}
        void LateUpdate(){Advance(Time.deltaTime);}
        public void Advance(float dt)
        {
            if(!mesh||!game||!artwork)return;clock+=dt;breakRemaining=Mathf.Max(0,breakRemaining-dt);
            vertices.Clear();colors.Clear();triangles.Clear();
            Vector2 center=transform.InverseTransformPoint(artwork.bounds.center);
            if(game.Shielded)
            {
                for(int n=0;n<32;n++)
                {
                    float a=n*Mathf.PI*2/32,b=(n+1)*Mathf.PI*2/32;Vector2 outerA=Point(a,shieldRadius),outerB=Point(b,shieldRadius),innerA=Point(a,shieldRadius-Vector2.one*.075f),innerB=Point(b,shieldRadius-Vector2.one*.075f);
                    Color c=shieldColor;c.a*=.8f+.2f*Mathf.Sin(clock*4);Poly(center+outerA,center+outerB,center+innerB,center+innerA,c);
                }
                // Small blue facets make the persistent barrier distinguishable from the water trail.
                for(int n=0;n<6;n++){float a=n*Mathf.PI/3+clock*.35f;Quad(center+Point(a,shieldRadius),new Vector2(.06f,.09f),shieldColor);}
            }
            if(Breaking)
            {
                float t=1-breakRemaining/Mathf.Max(.01f,breakSeconds);Color c=shieldColor;c.a=1-t;
                for(int n=0;n<16;n++){float a=n*Mathf.PI/8;Quad(center+Point(a,shieldRadius+Vector2.one*t*1.4f),new Vector2(.06f,.11f)*(1-t*.5f),c);}
            }
            if(BoostVisible)
            {
                Vector2 feet=transform.InverseTransformPoint(new Vector3(artwork.bounds.center.x,artwork.bounds.min.y,0));
                for(int n=0;n<14;n++)
                {
                    float t=Mathf.Repeat(clock*1.8f+n/14f,1);Color c=boostColor;c.a*=1-t;
                    float x=(n%3-1)*.24f;Quad(feet+new Vector2(x,-.12f-t*1.2f),new Vector2(.035f,.09f+.08f*t),c);
                }
            }
            mesh.Clear();mesh.SetVertices(vertices);mesh.SetColors(colors);mesh.SetTriangles(triangles,0);mesh.RecalculateBounds();auraRenderer.enabled=vertices.Count>0;
        }
        static Vector2 Point(float a,Vector2 radius)=>new Vector2(Mathf.Round(Mathf.Cos(a)*radius.x*32)/32,Mathf.Round(Mathf.Sin(a)*radius.y*32)/32);
        void Quad(Vector2 p,Vector2 half,Color c)=>Poly(p+new Vector2(-half.x,-half.y),p+new Vector2(-half.x,half.y),p+new Vector2(half.x,half.y),p+new Vector2(half.x,-half.y),c);
        void Poly(Vector2 a,Vector2 b,Vector2 c,Vector2 d,Color color)
        {
            int i=vertices.Count;vertices.Add(a);vertices.Add(b);vertices.Add(c);vertices.Add(d);for(int n=0;n<4;n++)colors.Add(color);triangles.Add(i);triangles.Add(i+1);triangles.Add(i+2);triangles.Add(i);triangles.Add(i+2);triangles.Add(i+3);
        }
        void OnDisable(){Clear();}
        void OnDestroy(){if(mesh)Destroy(mesh);if(material)Destroy(material);}
    }
}
