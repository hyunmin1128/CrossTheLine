using UnityEngine;

namespace CrossTheLine.Minigames
{
    /// <summary>Pooled visual-only sprites. Gameplay timers remain owned by the existing game/aura.</summary>
    public sealed class FastingIllustratedAura : MonoBehaviour
    {
        FastingPlayerAura aura;FastingTopDownGame game;SpriteRenderer artwork;
        SpriteRenderer bubble,prayer;
        readonly SpriteRenderer[] water=new SpriteRenderer[6],sparks=new SpriteRenderer[8];
        float clock;
        public bool WaterVisible=>water[0]&&water[0].enabled;
        public bool PrayerVisible=>prayer&&prayer.enabled;
        SpriteRenderer Create(string name,Material material,int order)
        {
            var child=new GameObject(name);child.transform.SetParent(transform,false);var r=child.AddComponent<SpriteRenderer>();r.sharedMaterial=material;r.sortingLayerID=artwork.sortingLayerID;r.sortingOrder=artwork.sortingOrder+order;r.enabled=false;return r;
        }
        public void Initialize(FastingPlayerAura owner,FastingTopDownGame target,SpriteRenderer body,Material material)
        {
            aura=owner;game=target;artwork=body;bubble=Create("IllustratedShieldMembrane",material,9);prayer=Create("IllustratedPrayerLight",material,14);
            for(int i=0;i<water.Length;i++)water[i]=Create("IllustratedWaterTrail"+i,material,-1);
            for(int i=0;i<sparks.Length;i++)sparks[i]=Create("IllustratedAcquireSpark"+i,material,11);
        }
        static void Show(SpriteRenderer r,Sprite sprite,bool visible,Vector3 position,Vector2 size,float alpha=1)
        {
            r.enabled=visible&&sprite;if(!r.enabled)return;r.sprite=sprite;r.transform.position=position;r.color=new Color(1,1,1,alpha);
            var parent=r.transform.parent.lossyScale;r.transform.localScale=new Vector3(size.x/Mathf.Max(.001f,sprite.bounds.size.x*Mathf.Abs(parent.x)),size.y/Mathf.Max(.001f,sprite.bounds.size.y*Mathf.Abs(parent.y)),1);
        }
        public void Draw(float dt)
        {
            clock+=Mathf.Max(0,dt);var art=game.effectSprites;if(!art){Clear();return;}
            Vector3 center=artwork.bounds.center;Vector2 radius=aura.MembraneRadii;float breaking=aura.BreakProgress;
            Show(bubble,FastingEffectSprites.Frame(art.bubble,aura.Breaking?3:(int)(clock*4)%3),game.Shielded||aura.Breaking,center,new Vector2(radius.x*2*64/53,radius.y*2*64/40)*(aura.Breaking?1+breaking*.7f:1),aura.Breaking?1-breaking:.9f);
            for(int i=0;i<sparks.Length;i++)
            {
                float angle=i*Mathf.PI/4+clock*.25f,t=aura.AcquireProgress;Vector3 offset=new Vector3(Mathf.Cos(angle)*(radius.x+t*.4f),Mathf.Sin(angle)*(radius.y+t*.4f));
                Show(sparks[i],art.spark,aura.Acquiring,center+offset,Vector2.one*.16f,Mathf.Sin(t*Mathf.PI));
            }
            Vector3 feet=new Vector3(center.x,artwork.bounds.min.y,center.z);
            for(int i=0;i<water.Length;i++)
            {
                float t=Mathf.Repeat(clock*1.7f+i/(float)water.Length,1);Show(water[i],FastingEffectSprites.Frame(art.water,(int)(t*4)),aura.BoostVisible,feet+new Vector3((i%3-1)*.2f,-.08f-t*.9f,0),Vector2.one*Mathf.Lerp(.65f,.4f,t),1-t);
            }
            bool praying=game.Praying||game.PrayerPreparing||game.InPrayerWindow;
            Show(prayer,FastingEffectSprites.Frame(art.prayer,game.PrayerPreparing?0:(int)(clock*5)%4),praying,new Vector3(center.x,artwork.bounds.max.y+.3f,center.z),Vector2.one*1.25f,game.PrayerPreparing?.6f:1);
        }
        public void Clear(){if(bubble)bubble.enabled=false;if(prayer)prayer.enabled=false;foreach(var r in water)if(r)r.enabled=false;foreach(var r in sparks)if(r)r.enabled=false;}
        void OnDisable(){Clear();}
    }
}
