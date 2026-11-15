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
                child.transform.SetParent(canvas.transform,false);breakGraphic=child.AddComponent<FastingShieldBreakGraphic>();
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
                sprite=renderer.sprite;spriteMaterial=renderer.sharedMaterial;
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
                shield.transform.position=center+new Vector3(Mathf.Cos(phase)*1.05f,depth*.48f+Mathf.Sin(angle*1.6f+i)*.08f,0);
                float size=.64f/Mathf.Max(.01f,sprite.bounds.size.y)*(1+depth*.08f);
                shield.transform.localScale=Vector3.one*size;
                // Compensate the pickup's bottom pivot so the picture follows the orbit center.
                shield.transform.position-=Vector3.Scale(sprite.bounds.center,shield.transform.lossyScale);
                shield.transform.rotation=Quaternion.identity;
                if(game.playerBody){shield.sortingLayerID=game.playerBody.sortingLayerID;shield.sortingOrder=game.playerBody.sortingOrder+(depth>0?-2:2);}
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
