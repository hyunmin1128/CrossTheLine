using UnityEngine;

namespace CrossTheLine.Minigames
{
    /// <summary>Pooled visual-only sprites. Gameplay timers remain owned by the existing game/aura.</summary>
    public sealed class FastingIllustratedAura : MonoBehaviour
    {
        FastingPlayerAura aura;FastingTopDownGame game;SpriteRenderer artwork;
        SpriteRenderer bubble,prayer,prayerAura;
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
            prayerAura=Create("EditablePrayerAura",material,8);
            for(int i=0;i<water.Length;i++)water[i]=Create("IllustratedWaterTrail"+i,material,-1);
            for(int i=0;i<sparks.Length;i++)sparks[i]=Create("IllustratedAcquireSpark"+i,material,11);
        }
        static Sprite Loop(Sprite[] frames,float clock,float fps,int count=-1)=>frames!=null&&frames.Length>0?frames[(int)(clock*fps)%Mathf.Max(1,count<0?frames.Length:count)]:null;
        static void ShowCanvas(SpriteRenderer r,Sprite sprite,bool visible,Vector3 center,Vector2 worldSize,Vector2 canvas,float alpha=1)
        {
            r.enabled=visible&&sprite;if(!r.enabled)return;r.sprite=sprite;r.transform.position=center;r.color=new Color(1,1,1,alpha);
            var parent=r.transform.parent.lossyScale;
            r.transform.localScale=new Vector3(worldSize.x*sprite.pixelsPerUnit/(canvas.x*Mathf.Abs(parent.x)),worldSize.y*sprite.pixelsPerUnit/(canvas.y*Mathf.Abs(parent.y)),1);
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
            var bubbleFrame=aura.Breaking?FastingEffectSprites.Frame(art.bubble,art.bubble.Length-1):Loop(art.bubble,clock,4,Mathf.Max(1,art.bubble.Length-1));
            var bubbleSize=new Vector2(radius.x*2*64/53,radius.y*2*64/40)*(aura.Breaking?1+breaking*.7f:1);
            if(art.editableAuraSprites)ShowCanvas(bubble,bubbleFrame,game.Shielded||aura.Breaking,center,bubbleSize,Vector2.one*64,aura.Breaking?(1-breaking)*Mathf.Max(.5f,art.shieldBubbleOpacity):art.shieldBubbleOpacity);
            else Show(bubble,bubbleFrame,game.Shielded||aura.Breaking,center,bubbleSize,aura.Breaking?1-breaking:.9f);
            for(int i=0;i<sparks.Length;i++)
            {
                float angle=i*Mathf.PI/4+clock*.25f,t=aura.AcquireProgress;Vector3 offset=new Vector3(Mathf.Cos(angle)*(radius.x+t*.4f),Mathf.Sin(angle)*(radius.y+t*.4f));
                if(art.editableAuraSprites)ShowCanvas(sparks[i],art.spark,aura.Acquiring,center+offset,Vector2.one*.16f,Vector2.one*64,Mathf.Sin(t*Mathf.PI));
                else Show(sparks[i],art.spark,aura.Acquiring,center+offset,Vector2.one*.16f,Mathf.Sin(t*Mathf.PI));
            }
            Vector3 feet=new Vector3(center.x,artwork.bounds.min.y,center.z);
            for(int i=0;i<water.Length;i++)
            {
                float t=Mathf.Repeat(clock*1.7f+i/(float)water.Length,1);Show(water[i],FastingEffectSprites.Frame(art.water,(int)(t*4)),aura.BoostVisible,feet+new Vector3((i%3-1)*.2f,-.08f-t*.9f,0),Vector2.one*Mathf.Lerp(.65f,.4f,t),1-t);
            }
            bool praying=game.Running&&game.Praying;
            var at=new Vector3(center.x,artwork.bounds.max.y+.3f,center.z);
            if(art.editableAuraSprites)
            {
                ShowCanvas(prayer,Loop(art.prayer,clock,5),praying,at,Vector2.one*1.7f,Vector2.one*64);
                ShowCanvas(prayerAura,Loop(art.prayerAura,clock,5),praying,center,new Vector2(aura.prayerRadius.x*128/36.8f,aura.prayerRadius.y*128/48),Vector2.one*128,art.prayerAuraOpacity);
            }
            else {Show(prayer,Loop(art.prayer,clock,5),praying,at,Vector2.one*1.7f);prayerAura.enabled=false;}
        }
        public void Clear(){if(bubble)bubble.enabled=false;if(prayer)prayer.enabled=false;if(prayerAura)prayerAura.enabled=false;foreach(var r in water)if(r)r.enabled=false;foreach(var r in sparks)if(r)r.enabled=false;}
        void OnDisable(){Clear();}
    }
}
