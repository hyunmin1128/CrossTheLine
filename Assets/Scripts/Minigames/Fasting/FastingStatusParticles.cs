using System.Collections.Generic;
using UnityEngine;
namespace CrossTheLine.Minigames
{
    [RequireComponent(typeof(MeshFilter),typeof(MeshRenderer))]
    public sealed class FastingStatusParticles : MonoBehaviour
    {
        public FastingTopDownGame game;public SpriteRenderer artwork;public Material effectMaterial;
        public Color slowColor=new Color(.35f,.85f,1),rootColor=new Color(1,.8f,.3f),burnColor=new Color(1,.4f,.12f),reverseColor=new Color(.85f,.45f,1),visionColor=new Color(.7f,.75f,.85f),protectionColor=Color.white;
        Mesh mesh;MeshRenderer rendererComponent;float clock;
        readonly List<Vector3> vertices=new List<Vector3>();readonly List<Color> colors=new List<Color>();readonly List<int> indices=new List<int>();
        public int VisibleStatusCount{get;private set;}
        void Awake(){mesh=new Mesh{name="Fasting state particles"};mesh.MarkDynamic();GetComponent<MeshFilter>().sharedMesh=mesh;rendererComponent=GetComponent<MeshRenderer>();rendererComponent.sharedMaterial=effectMaterial;}
        void LateUpdate(){Refresh(Time.deltaTime);}
        public void Refresh(float dt){
            if(!game||!artwork||!mesh)return;clock+=dt;vertices.Clear();colors.Clear();indices.Clear();VisibleStatusCount=0;
            if(game.Running){
                var center=(Vector2)transform.InverseTransformPoint(artwork.bounds.center);var feet=(Vector2)transform.InverseTransformPoint(new Vector3(artwork.bounds.center.x,artwork.bounds.min.y));
                if(game.SlowStatusRemaining>0){VisibleStatusCount++;Orbit(feet,slowColor,.55f,.15f,-clock,8);}
                if(game.RootStatusRemaining>0){VisibleStatusCount++;for(int i=0;i<5;i++)Quad(feet+new Vector2((i-2)*.18f,-.1f),new Vector2(.035f,.12f),rootColor);}
                if(game.BurnStatusRemaining>0){VisibleStatusCount++;for(int i=0;i<8;i++){float t=Mathf.Repeat(clock*.8f+i/8f,1);var c=burnColor;c.a=1-t;Quad(center+new Vector2(Mathf.Sin(i*2.4f)*.5f,-.4f+t*1.5f),new Vector2(.035f,.065f),c);}}
                if(game.ReverseStatusRemaining>0){VisibleStatusCount++;Orbit(center+Vector2.up*.9f,reverseColor,.55f,.2f,-clock*3,6);}
                if(game.VisionRemaining>0){VisibleStatusCount++;Orbit(center+Vector2.up*1.1f,visionColor,.45f,.12f,clock,4);}
                if(game.InvulnerabilityRemaining>0){VisibleStatusCount++;Orbit(center,protectionColor,.7f,1,clock*2,4);}
            }
            mesh.Clear();mesh.SetVertices(vertices);mesh.SetColors(colors);mesh.SetTriangles(indices,0);mesh.RecalculateBounds();rendererComponent.sortingLayerID=artwork.sortingLayerID;rendererComponent.sortingOrder=artwork.sortingOrder+15;rendererComponent.enabled=vertices.Count>0;
        }
        void Orbit(Vector2 center,Color c,float x,float y,float phase,int count){for(int i=0;i<count;i++){float a=phase+i*Mathf.PI*2/count;Quad(center+new Vector2(Mathf.Cos(a)*x,Mathf.Sin(a)*y),Vector2.one*.035f,c);}}
        void Quad(Vector2 p,Vector2 half,Color c){p=new Vector2(Mathf.Round(p.x*32)/32,Mathf.Round(p.y*32)/32);int i=vertices.Count;vertices.Add(p-half);vertices.Add(p+new Vector2(-half.x,half.y));vertices.Add(p+half);vertices.Add(p+new Vector2(half.x,-half.y));for(int n=0;n<4;n++)colors.Add(c);indices.Add(i);indices.Add(i+1);indices.Add(i+2);indices.Add(i);indices.Add(i+2);indices.Add(i+3);}
        void OnDestroy(){if(mesh)Destroy(mesh);}
    }
}
