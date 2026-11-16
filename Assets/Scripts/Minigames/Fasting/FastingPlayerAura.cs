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
        public float acquireSeconds=.6f;
        public Color rimColor=new Color(.45f,.9f,1,.95f),goldColor=new Color(1,.82f,.36f,.9f);
        [Range(.05f,.3f)] public float membraneOpacity=.16f;
        Mesh mesh;Material material;MeshRenderer auraRenderer;
        float breakRemaining,clock,acquireRemaining;
        FastingIllustratedAura illustrated;
        public float AcquireProgress=>1-acquireRemaining/Mathf.Max(.01f,acquireSeconds);
        public float BreakProgress=>1-breakRemaining/Mathf.Max(.01f,breakSeconds);
        public bool Acquiring=>acquireRemaining>0;
        float MembraneScale=>1+.012f*Mathf.Sin(clock*2.5f)+(Acquiring?.12f*acquireRemaining/Mathf.Max(.01f,acquireSeconds):0);
        public Vector2 MembraneRadii=>shieldRadius*MembraneScale;
        public bool Breaking=>breakRemaining>0;
        public bool BoostVisible=>game&&game.WaterBoostRemaining>0;
        readonly List<Vector3> vertices=new List<Vector3>();readonly List<Color> colors=new List<Color>();readonly List<int> triangles=new List<int>();
        void Awake()
        {
            mesh=new Mesh{name="Shield and water boost pixels"};mesh.MarkDynamic();GetComponent<MeshFilter>().sharedMesh=mesh;
            auraRenderer=GetComponent<MeshRenderer>();material=new Material(effectMaterial);auraRenderer.sharedMaterial=material;
            if(artwork){auraRenderer.sortingLayerID=artwork.sortingLayerID;auraRenderer.sortingOrder=artwork.sortingOrder+8;}
        }
        public void AcquireShield(){acquireRemaining=acquireSeconds;}
        public void BreakShield(){acquireRemaining=0;breakRemaining=breakSeconds;}
        public void Clear(){acquireRemaining=breakRemaining=0;if(mesh)mesh.Clear();if(illustrated)illustrated.Clear();}
        void LateUpdate(){Advance(Time.deltaTime);}
        public void Advance(float dt)
        {
            if(!mesh||!game||!artwork)return;clock+=dt;breakRemaining=Mathf.Max(0,breakRemaining-dt);acquireRemaining=Mathf.Max(0,acquireRemaining-dt);
            vertices.Clear();colors.Clear();triangles.Clear();
            Vector2 center=transform.InverseTransformPoint(artwork.bounds.center);
            if(game.effectSprites)
            {
                if(!illustrated){illustrated=gameObject.AddComponent<FastingIllustratedAura>();illustrated.Initialize(this,game,artwork,material);}
                illustrated.Draw(dt);
                if(game.Shielded)Bubble(center,MembraneScale,.9f+.1f*Mathf.Sin(clock*2.5f));
                mesh.Clear();mesh.SetVertices(vertices);mesh.SetColors(colors);mesh.SetTriangles(triangles,0);mesh.RecalculateBounds();auraRenderer.enabled=vertices.Count>0;return;
            }
            if(game.Shielded)
            {
                float pulse=.9f+.1f*Mathf.Sin(clock*2.5f),scale=MembraneScale;
                Bubble(center,scale,pulse);
                for(int n=0;n<2;n++){Vector2 at=center+Point(clock*.35f+n*Mathf.PI,shieldRadius*scale);Color c=rimColor;c.a=.6f;Spark(at,.045f,c);}
            }
            if(Acquiring)
            {
                float t=1-acquireRemaining/Mathf.Max(.01f,acquireSeconds);Color c=goldColor;c.a*=Mathf.Sin(Mathf.PI*t);
                for(int n=0;n<8;n++){float a=n*Mathf.PI/4;Spark(center+Point(a,shieldRadius+Vector2.one*t*.55f),.045f+.025f*(1-t),c);}
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
        void Bubble(Vector2 center,float scale,float pulse)
        {
            Vector2 radius=shieldRadius*scale;
            int rows=Mathf.CeilToInt(radius.y*64);
            // Flat, pixel-aligned translucent bands tint the character without hiding the artwork.
            for(int row=0;row<rows;row++)
            {
                float y=(row+.5f)/rows*radius.y*2-radius.y,normalized=y/radius.y;
                float width=Mathf.Floor(Mathf.Sqrt(Mathf.Max(0,1-normalized*normalized))*radius.x*32)/32;
                if(width<=0)continue;
                Color c=shieldColor;c.a=membraneOpacity*pulse*(Mathf.Abs(normalized)>.72f?1.15f:.8f);
                Quad(center+new Vector2(0,y),new Vector2(width,radius.y/rows),c);
            }
            if(game&&game.effectSprites)return;
            Color glow=shieldColor;glow.a=.1f*pulse;BubbleOutline(center,radius,glow,3);
            Color rim=rimColor;rim.a=.65f*pulse;BubbleOutline(center,radius,rim,1);
            // A few pale highlights suggest a curved protective surface, rather than an empty ring.
            Color shine=new Color(.8f,.97f,1,.32f*pulse);
            for(int n=0;n<7;n++)Quad(center+Point((118+n*5)*Mathf.Deg2Rad,radius*.87f),new Vector2(.026f,.045f),shine);
        }
        void BubbleOutline(Vector2 center,Vector2 radius,Color color,int pixels)
        {
            Vector2 previous=new Vector2(float.MaxValue,float.MaxValue);
            for(int n=0;n<160;n++)
            {
                Vector2 a=Point(n*Mathf.PI*2/160,radius),b=Point((n+1)*Mathf.PI*2/160,radius);
                int steps=Mathf.Max(1,Mathf.CeilToInt(Mathf.Max(Mathf.Abs(a.x-b.x),Mathf.Abs(a.y-b.y))*32));
                for(int k=0;k<steps;k++)
                {
                    Vector2 p=Vector2.Lerp(a,b,(float)k/steps);p=center+new Vector2(Mathf.Round(p.x*32)/32,Mathf.Round(p.y*32)/32);
                    if(p==previous)continue;Quad(p,Vector2.one*(pixels/64f),color);previous=p;
                }
            }
        }
        void Spark(Vector2 at,float size,Color c){Quad(at,new Vector2(size*.35f,size),c);Quad(at,new Vector2(size,size*.35f),c);}
        void Quad(Vector2 p,Vector2 half,Color c)=>Poly(p+new Vector2(-half.x,-half.y),p+new Vector2(-half.x,half.y),p+new Vector2(half.x,half.y),p+new Vector2(half.x,-half.y),c);
        void Poly(Vector2 a,Vector2 b,Vector2 c,Vector2 d,Color color)
        {
            int i=vertices.Count;vertices.Add(a);vertices.Add(b);vertices.Add(c);vertices.Add(d);for(int n=0;n<4;n++)colors.Add(color);triangles.Add(i);triangles.Add(i+1);triangles.Add(i+2);triangles.Add(i);triangles.Add(i+2);triangles.Add(i+3);
        }
        void OnDisable(){Clear();}
        void OnDestroy(){if(mesh)Destroy(mesh);if(material)Destroy(material);}
    }
}
