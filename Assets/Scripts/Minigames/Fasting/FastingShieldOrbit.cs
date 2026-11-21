using UnityEngine;

namespace CrossTheLine.Minigames
{
    /// <summary>Reuses the user's pickup sprite; the three orbit sprites never add colliders.</summary>
    public sealed class FastingShieldOrbit : MonoBehaviour
    {
        FastingTopDownGame game;
        readonly SpriteRenderer[] shields=new SpriteRenderer[FastingTopDownGame.MaximumShields];
        Sprite sprite;
        Material spriteMaterial;
        Transform orbitRoot;
        FastingShieldBreakGraphic breakGraphic;
        float angle;
        public Vector2 orbitRadius=new Vector2(1.2f,.68f);
        [Min(0)] public float membraneClearance=.12f;
        [Range(.1f,.6f)] public float beltHeightRatio=.28f;
        public int VisibleCount{get;private set;}
        public FastingShieldBreakGraphic BreakGraphic=>breakGraphic;

        public void Initialize(FastingTopDownGame owner)
        {
            game=owner;orbitRoot=new GameObject("OrbitingShields").transform;orbitRoot.SetParent(game.player,false);
            foreach(var entity in game.entities)if(entity&&entity.kind==FastingTopDownEntity.Kind.Shield){UsePickupArtwork(entity);break;}
            for(int i=0;i<shields.Length;i++)
            {
                var child=new GameObject("OrbitShield"+(i+1));child.transform.SetParent(orbitRoot,false);
                shields[i]=child.AddComponent<SpriteRenderer>();shields[i].sprite=sprite;shields[i].sharedMaterial=spriteMaterial;child.SetActive(false);
            }
            var canvas=game.transition?game.transition.canvas:null;
            if(canvas)
            {
                var child=new GameObject("ShieldBreakOverlay",typeof(RectTransform),typeof(UnityEngine.CanvasRenderer));
                child.transform.SetParent(canvas.transform,false);breakGraphic=child.AddComponent<FastingShieldBreakGraphic>();breakGraphic.effectSprites=game.effectSprites;
                var rect=breakGraphic.rectTransform;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;
                breakGraphic.raycastTarget=false;
            }
            Refresh();
        }
        public void UsePickupArtwork(FastingTopDownEntity entity)
        {
            if(!entity)return;
            foreach(var renderer in entity.GetComponentsInChildren<SpriteRenderer>(true))if(renderer.name=="Symbol"&&renderer.sprite)
            {
                sprite=game&&game.effectSprites&&game.effectSprites.orbitShield?game.effectSprites.orbitShield:renderer.sprite;spriteMaterial=renderer.sharedMaterial;
                foreach(var shield in shields)if(shield){shield.sprite=sprite;shield.sharedMaterial=spriteMaterial;}
                return;
            }
        }
        void LateUpdate(){if(!game)return;if(game.Running)angle+=Time.deltaTime*1.7f;Refresh();}
        public void Refresh()
        {
            if(!game||!orbitRoot)return;VisibleCount=0;
            var center=game.playerBody?game.playerBody.bounds.center:game.player.position+Vector3.up*.7f;
            for(int i=0;i<shields.Length;i++)
            {
                var shield=shields[i];if(!shield)continue;bool visible=i<game.ShieldCount&&sprite;
                shield.gameObject.SetActive(visible);if(!visible)continue;VisibleCount++;
                float phase=angle+i*Mathf.PI*2/Mathf.Max(1,game.ShieldCount),depth=Mathf.Sin(phase);
                float size=.64f/Mathf.Max(.01f,sprite.bounds.size.y)*(1-depth*.12f);
                shield.transform.localScale=Vector3.one*size;
                Vector2 radius=game.playerAura?game.playerAura.MembraneRadii:orbitRadius;
                if(game.playerAura){Vector3 scale=game.playerAura.transform.lossyScale;radius=Vector2.Scale(radius,new Vector2(Mathf.Abs(scale.x),Mathf.Abs(scale.y)));}
                radius=new Vector2(Mathf.Max(.01f,radius.x),Mathf.Max(.01f,radius.y));
                Vector2 extents=Vector2.Scale(sprite.bounds.extents,new Vector2(Mathf.Abs(shield.transform.lossyScale.x),Mathf.Abs(shield.transform.lossyScale.y)));
                // A horizontal belt around the membrane: projected front/back paths intentionally cross its silhouette.
                Vector2 offset=new Vector2(Mathf.Cos(phase)*(radius.x+extents.x+membraneClearance),depth*radius.y*beltHeightRatio);
                shield.transform.position=center+(Vector3)offset;
                // Compensate the pickup's bottom pivot so the picture follows the orbit center.
                shield.transform.position-=Vector3.Scale(sprite.bounds.center,shield.transform.lossyScale);
                shield.transform.rotation=Quaternion.identity;
                shield.color=depth>0?new Color(.78f,.87f,1,.9f):Color.white;
                if(game.playerBody){shield.sortingLayerID=game.playerBody.sortingLayerID;shield.sortingOrder=game.playerBody.sortingOrder+(depth>0?-2:12);}
            }
        }
        public void Break(int index)
        {
            Refresh();index=Mathf.Clamp(index,0,shields.Length-1);
            var source=shields[index];Vector3 world=source&&source.sprite?source.bounds.center:game.player.position;
            if(breakGraphic&&sprite)breakGraphic.Play(sprite,game.gameCamera.WorldToScreenPoint(world));
        }
        public void Clear(){angle=0;if(breakGraphic)breakGraphic.Clear();Refresh();}
        void OnDestroy(){if(orbitRoot)Destroy(orbitRoot.gameObject);if(breakGraphic)Destroy(breakGraphic.gameObject);}
    }
}
