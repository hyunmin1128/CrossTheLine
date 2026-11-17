using UnityEngine;

namespace CrossTheLine.Minigames
{
    /// <summary>Local pixel-shaped feedback; never moves the gameplay root or camera.</summary>
    public sealed class FastingHitFeedback : MonoBehaviour
    {
        public SpriteRenderer artwork;
        public FastingEffectSprites effectSprites;
        public Material effectMaterial;
        [Range(.2f, 1.2f)] public float duration = .65f;
        public float liftHeight = .65f, sinkDepth = .32f;
        public float sandAirSeconds=1.35f,sandAirHeight=2.4f;
        public float quicksandSinkSeconds=2;
        public float batAirSeconds=1.1f,batAirHeight=1.8f;
        public float crackFallSeconds=.9f,crackBlinkSeconds=.8f,crackFallDepth=1.2f;
        public Sprite crackGroundSprite;
        public float crackOpeningWidth=4;
        SpriteRenderer crackOpening;Vector3 openingScale;
        public bool CrackOpeningVisible=>crackOpening&&crackOpening.enabled;
        public float ArtworkOpacity{get;private set;}=1;
        public int sortingOrder = 100;
        public Color rockColor = new Color(.46f,.34f,.23f,1);
        public Color thornColor = new Color(.19f,.31f,.12f,1);
        public Color sandColor = new Color(.94f,.73f,.32f,1);
        public Color pitColor = new Color(.48f,.29f,.10f,1);
        Mesh mesh; Material material; MeshRenderer meshRenderer;
        Vector3[] vertices; Color[] colors; int[] triangles;
        Vector3 basePosition, baseScale; float remaining;
        bool artworkPoseCaptured;
        FastingTopDownHazard.Kind kind;
        const int Count = 20;
        SpriteRenderer[] particles;
        public bool Active => remaining > 0;
        float EffectDuration=>kind==FastingTopDownHazard.Kind.Sand?sandAirSeconds:kind==FastingTopDownHazard.Kind.Bat?batAirSeconds:kind==FastingTopDownHazard.Kind.Quicksand?quicksandSinkSeconds:kind==FastingTopDownHazard.Kind.Crack?crackFallSeconds+crackBlinkSeconds:duration;
        public bool TryGetRestPose(out Vector3 position,out Vector3 scale){position=basePosition;scale=baseScale;return artworkPoseCaptured;}
        void Awake()
        {
            if(artwork){basePosition=artwork.transform.localPosition;baseScale=artwork.transform.localScale;artworkPoseCaptured=true;}
            var filter=GetComponent<MeshFilter>();if(!filter)filter=gameObject.AddComponent<MeshFilter>();
            meshRenderer=GetComponent<MeshRenderer>();if(!meshRenderer)meshRenderer=gameObject.AddComponent<MeshRenderer>();
            mesh=new Mesh{name="Fasting hit particles"};mesh.MarkDynamic();filter.sharedMesh=mesh;
            material=effectMaterial?new Material(effectMaterial):new Material(Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default"));meshRenderer.sharedMaterial=material;
            if(artwork)meshRenderer.sortingLayerID=artwork.sortingLayerID;
            meshRenderer.sortingOrder=sortingOrder;meshRenderer.enabled=false;
            vertices=new Vector3[Count*4];colors=new Color[Count*4];triangles=new int[Count*6];
            for(int n=0;n<Count;n++){int v=n*4,t=n*6;triangles[t]=v;triangles[t+1]=v+1;triangles[t+2]=v+2;triangles[t+3]=v;triangles[t+4]=v+2;triangles[t+5]=v+3;}
            if(effectSprites){particles=new SpriteRenderer[Count];for(int n=0;n<Count;n++){var child=new GameObject("IllustratedParticle"+n);child.transform.SetParent(transform,false);var renderer=child.AddComponent<SpriteRenderer>();renderer.sharedMaterial=material;renderer.sortingLayerID=meshRenderer.sortingLayerID;renderer.sortingOrder=sortingOrder;renderer.enabled=false;particles[n]=renderer;}}
        }
        public void Play(FastingTopDownHazard.Kind cause)
        {
            Clear();kind=cause;remaining=EffectDuration;meshRenderer.enabled=true;Draw(0);
            if(cause==FastingTopDownHazard.Kind.Crack&&crackGroundSprite&&artwork)
            {
                if(!crackOpening){crackOpening=new GameObject("CrackGroundOpening_VisualOnly").AddComponent<SpriteRenderer>();crackOpening.sharedMaterial=material;crackOpening.sortingLayerID=artwork.sortingLayerID;crackOpening.sortingOrder=artwork.sortingOrder-2;}
                crackOpening.sprite=crackGroundSprite;crackOpening.enabled=true;crackOpening.color=Color.white;
                openingScale=Vector3.one*(crackOpeningWidth/Mathf.Max(.01f,crackGroundSprite.bounds.size.x));
                crackOpening.transform.localScale=openingScale*.8f;crackOpening.transform.position=artwork.transform.parent?artwork.transform.parent.position:transform.position;
            }
        }
        public void Clear()
        {
            remaining=0;ArtworkOpacity=1;if(meshRenderer)meshRenderer.enabled=false;
            if(crackOpening)crackOpening.enabled=false;
            if(particles!=null)foreach(var particle in particles)if(particle)particle.enabled=false;
            if(artwork&&artworkPoseCaptured){artwork.transform.localPosition=basePosition;artwork.transform.localScale=baseScale;var c=artwork.color;c.a=1;artwork.color=c;}
        }
        void Update(){if(Active)Advance(Time.deltaTime);}
        public void Advance(float dt)
        {
            if(!Active)return;
            remaining=Mathf.Max(0,remaining-dt);float t=1-remaining/Mathf.Max(.01f,EffectDuration);
            if(artwork)
            {
                float arc=Mathf.Sin(t*Mathf.PI);
                float dy=kind==FastingTopDownHazard.Kind.Sand?sandAirHeight*arc:kind==FastingTopDownHazard.Kind.Bat?batAirHeight*arc:kind==FastingTopDownHazard.Kind.Quicksand?-sinkDepth*arc:0;
                float shake=kind==FastingTopDownHazard.Kind.Rock?Mathf.Sin(t*38)*.10f*(1-t):0;
                if(kind==FastingTopDownHazard.Kind.Crack)
                {
                    float elapsed=EffectDuration-remaining,fall=elapsed/Mathf.Max(.01f,crackFallSeconds),scale;
                    if(fall<.35f){float p=Mathf.SmoothStep(0,1,fall/.35f);dy=-crackFallDepth*p;scale=Mathf.Lerp(1,.08f,p);ArtworkOpacity=1-p;}
                    else if(fall<.65f){dy=-crackFallDepth;scale=.08f;ArtworkOpacity=0;}
                    else if(fall<1){float p=Mathf.SmoothStep(0,1,(fall-.65f)/.35f);dy=-crackFallDepth*(1-p);scale=Mathf.Lerp(.08f,1,p);ArtworkOpacity=p;}
                    else{dy=0;scale=1;ArtworkOpacity=Mathf.FloorToInt((elapsed-crackFallSeconds)*12)%2==0?1:.18f;}
                    artwork.transform.localPosition=basePosition+Vector3.up*dy;artwork.transform.localScale=baseScale*scale;var c=artwork.color;c.a=ArtworkOpacity;artwork.color=c;
                }
                else
                {
                artwork.transform.localPosition=basePosition+new Vector3(shake,dy,0);
                artwork.transform.localScale=Vector3.Scale(baseScale,new Vector3(1,kind==FastingTopDownHazard.Kind.Quicksand?1-.35f*arc:1,1));
                }
            }
            Draw(t);if(remaining<=0)Clear();
        }
        void Draw(float t)
        {
            if(crackOpening&&kind==FastingTopDownHazard.Kind.Crack){crackOpening.color=new Color(1,1,1,1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.65f,1,t)));crackOpening.transform.localScale=openingScale*Mathf.Lerp(.8f,1.15f,Mathf.Clamp01(t/.2f));}
            bool pit=kind==FastingTopDownHazard.Kind.Quicksand||kind==FastingTopDownHazard.Kind.Crack;
            bool thorn=kind==FastingTopDownHazard.Kind.Cactus||kind==FastingTopDownHazard.Kind.Thorns;
            bool sand=kind==FastingTopDownHazard.Kind.Sand;
            Color color=pit?pitColor:thorn?thornColor:sand?sandColor:rockColor;
            if(sand)color=Color.Lerp(color,pitColor,.5f);
            color.a=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.55f,1,t));
            for(int n=0;n<Count;n++)
            {
                float a=n*2.399963f, radius=.28f+(n%5)*.13f;
                Vector2 p;
                if(pit){a+=t*5;p=new Vector2(Mathf.Cos(a)*radius,Mathf.Sin(a)*radius*.32f-.08f);}
                else if(thorn)p=new Vector2(Mathf.Cos(a)*(.48f+t*.14f),.65f+Mathf.Sin(a)*.55f);
                else p=new Vector2(Mathf.Cos(a)*(.15f+t*(1.4f+(n%3)*.3f)),.35f+Mathf.Sin(a)*t*1.4f+(sand?t*.6f:1.8f*t-2.5f*t*t));
                p.x=Mathf.Round(p.x*32)/32;p.y=Mathf.Round(p.y*32)/32;
                if(particles!=null&&effectSprites)
                {
                    var particle=particles[n];particle.sprite=effectSprites.Particle(kind,n);particle.enabled=particle.sprite;
                    particle.transform.localPosition=p;particle.transform.localRotation=Quaternion.Euler(0,0,thorn?-a*Mathf.Rad2Deg+45:t*(n%2==0?180:-150)+n*47);
                    float height=thorn?.30f:sand?.12f:pit?.13f:.17f+(n%3)*.035f;
                    particle.transform.localScale=Vector3.one*(height/Mathf.Max(.01f,particle.sprite?particle.sprite.bounds.size.y:1));particle.color=new Color(1,1,1,color.a);
                    continue;
                }
                float w=thorn?.035f:sand?.10f:.10f,h=thorn?.18f:sand?.055f:.08f;
                Vector2 axis=thorn?new Vector2(Mathf.Cos(a),Mathf.Sin(a)):Vector2.up;
                Vector2 side=new Vector2(axis.y,-axis.x)*w;axis*=h;
                int v=n*4;vertices[v]=p-side-axis;vertices[v+1]=p+side-axis;vertices[v+2]=p+side+axis;vertices[v+3]=p-side+axis;
                for(int k=0;k<4;k++)colors[v+k]=color;
            }
            mesh.Clear();if(particles!=null){meshRenderer.enabled=false;return;}mesh.vertices=vertices;mesh.colors=colors;mesh.triangles=triangles;mesh.RecalculateBounds();
        }
        void OnDisable(){Clear();}
        void OnDestroy(){if(crackOpening)Destroy(crackOpening.gameObject);if(mesh)Destroy(mesh);if(material)Destroy(material);}
    }
}
