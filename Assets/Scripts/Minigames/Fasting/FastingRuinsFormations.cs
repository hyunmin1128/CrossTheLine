using UnityEngine;
namespace CrossTheLine.Minigames
{
    public sealed partial class FastingTopDownGame
    {
        void BlockRuinsWalls(Vector2 from)
        {
            if(StageIndex!=2)return;
            Vector2 to=player.position;
            foreach(var h in hazards){
                if(!h||h.spent||h.kind!=FastingTopDownHazard.Kind.Wall)continue;
                var r=new Rect((Vector2)h.transform.position-h.size*.5f-new Vector2(.28f,.2f),h.size+new Vector2(.56f,.4f));
                if(r.Contains(new Vector2(to.x,from.y)))to.x=from.x;
                if(r.Contains(new Vector2(to.x,to.y)))to.y=from.y;
            }
            player.position=to;
        }
        void SpawnRuinsFormation(FastingTopDownHazard.Kind kind,int lane,float offset)
        {
            int safe=encounterDirector?encounterDirector.OpenLane:2;
            int first=lane==safe?(lane+1)%5:lane;
            int count=Random.Range(2,(kind==FastingTopDownHazard.Kind.Tomb?ruinsArt.maximumTombCount:ruinsArt.maximumWallSegments)+1);
            bool vertical=kind==FastingTopDownHazard.Kind.Wall&&Random.value<.5f;
            int side=first<safe?-1:1,adjacent=first+side;
            bool pair=adjacent>=0&&adjacent<5&&adjacent!=safe;
            int id=++nextFormationId;
            for(int i=0;i<count;i++){
                int column=vertical||!pair?first:i%2==0?first:adjacent;
                int row=vertical||!pair?i:i/2;
                int before=hazards.Count;
                SpawnAbove(kind,column,1,offset+row*(kind==FastingTopDownHazard.Kind.Wall?3.05f:2.1f),true,false,id,count);
                if(hazards.Count>before&&kind==FastingTopDownHazard.Kind.Wall){
                    var h=hazards[hazards.Count-1];
                    h.body.transform.localScale*=settings.laneSpacing/h.body.bounds.size.x;
                    h.artworkWidth=h.body.sprite.bounds.size.x;h.artworkCenter=h.body.sprite.bounds.center;
                    h.body.transform.localPosition=-(Vector3)h.artworkCenter*h.body.transform.localScale.x;
                    h.size=new Vector2(settings.laneSpacing,3.05f);
                    h.collisionHeight=3.05f;
                    h.ConfigureLanes(1,settings.laneSpacing/ .75f);
                }
            }
        }
    }
}
