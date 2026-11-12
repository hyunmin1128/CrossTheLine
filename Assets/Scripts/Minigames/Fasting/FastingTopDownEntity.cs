using UnityEngine;
namespace CrossTheLine.Minigames
{
    public sealed class FastingTopDownEntity : MonoBehaviour
    {
        public enum Kind { Cover, Water, Flame, Shield, Book, Page, Fragment }
        public Kind kind;
        public int stage;
        public Vector2 footprint=new Vector2(1.5f,1);
        public float laneWidth=3;
        public SpriteRenderer marker,windMarker;
        public bool consumed;
        public void SetUsed(bool used){consumed=used;if(kind==Kind.Water)foreach(var r in GetComponentsInChildren<SpriteRenderer>(true))r.color=used?new Color(.38f,.34f,.30f,.7f):Color.white;}
        public bool Contains(Vector2 point)=>new Rect((Vector2)transform.position-footprint*.5f,footprint).Contains(point);
        public Rect ShelterArea=>new Rect((Vector2)transform.position+new Vector2(-laneWidth*.5f,-laneWidth*1.5f),Vector2.one*laneWidth);
        public Rect WindShelterArea=>new Rect((Vector2)transform.position+new Vector2(-laneWidth*1.5f,-laneWidth*.5f),Vector2.one*laneWidth);
        public bool WindCellInsideMap=>WindShelterArea.xMin>=-laneWidth*2.5f-.001f;
        public bool Protects(Vector2 point)=>kind==Kind.Cover&&ShelterArea.Contains(point);
        public bool ProtectsWind(Vector2 point)=>kind==Kind.Cover&&WindCellInsideMap&&WindShelterArea.Contains(point);
        public void RefreshShelterMarkers()
        {
            if(kind!=Kind.Cover)return;
            Fit(marker,ShelterArea);
            Fit(windMarker,WindShelterArea);
            if(windMarker)windMarker.gameObject.SetActive(WindCellInsideMap);
        }
        static void Fit(SpriteRenderer renderer,Rect area)
        {
            if(!renderer||!renderer.sprite)return;
            renderer.transform.position=new Vector3(area.center.x,area.center.y,renderer.transform.position.z);
            var parent=renderer.transform.parent;Vector3 scale=parent?parent.lossyScale:Vector3.one;
            renderer.transform.localScale=new Vector3(area.width/renderer.sprite.bounds.size.x/scale.x,area.height/renderer.sprite.bounds.size.y/scale.y,1);
        }
        void OnDrawGizmosSelected(){Gizmos.color=kind==Kind.Cover?Color.cyan:Color.yellow;Gizmos.DrawWireCube(transform.position,footprint);if(kind==Kind.Cover){Gizmos.DrawWireCube(ShelterArea.center,ShelterArea.size);if(WindCellInsideMap)Gizmos.DrawWireCube(WindShelterArea.center,WindShelterArea.size);}}
    }
}
