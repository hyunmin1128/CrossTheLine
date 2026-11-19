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
            if(kind==FastingTopDownHazard.Kind.Wall){SpawnRuinsWall(lane,offset);return;}
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
        void SpawnRuinsWall(int preferred,float offset)
        {
            if(ruinsArt.largeMazeWalls){SpawnLargeRuinsMaze(offset);return;}
            int safe=encounterDirector?encounterDirector.OpenLane:2;
            bool vertical=Random.value<.5f;int count=Random.Range(2,Mathf.Clamp(ruinsArt.maximumWallSegments,2,5)+1);
            var starts=new System.Collections.Generic.List<int>();
            if(vertical){for(int i=0;i<5;i++)if(i!=safe)starts.Add(i);}
            else{
                for(;count>=2;count--){for(int i=0;i<=5-count;i++)if(safe<i||safe>=i+count)starts.Add(i);if(starts.Count>0)break;}
            }
            if(starts.Count==0)return;
            int first=starts.Contains(preferred)?preferred:starts[Random.Range(0,starts.Count)],id=++nextFormationId;
            float length=settings.laneSpacing;
            // Configure artwork and collision before spacing/escape checks, with no grid gaps.
            for(int i=0;i<count;i++){
                var h=Instantiate(vertical&&ruinsArt.verticalWall?ruinsArt.verticalWall:ruinsArt.wall,hazardRoot);h.name=vertical?"RuinsWall_Vertical":"RuinsWall_Horizontal";
                h.formationId=id;h.formationCount=count;h.ConfigureWallSegment(vertical,length,ruinsArt.wallThicknessScale*settings.laterStageObstacleScale);h.warning=0;h.velocity=Vector2.zero;h.scrollOnly=true;h.lifetime=30;
                float y=cameraY+gameCamera.orthographicSize+2+length*.5f+offset;
                h.transform.position=new Vector3(((vertical?first:first+i)-2)*length,y+(vertical?i*length:0),0);
                if(!AcceptHazard(h,true)){Destroy(h.gameObject);continue;}hazards.Add(h);
            }
        }
    }
}
