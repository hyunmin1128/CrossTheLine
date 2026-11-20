using UnityEngine;
namespace CrossTheLine.Minigames
{
    public sealed partial class FastingTopDownGame
    {
        void BlockRuinsWalls(Vector2 from)
        {
            if(StageIndex!=2)return;
            Vector2 to=player.position;bool blocked=false;
            foreach(var h in hazards){
                if(!h||h.spent||h.kind!=FastingTopDownHazard.Kind.Wall)continue;
                var r=new Rect(h.CollisionCenter-h.size*.5f-new Vector2(.28f,.2f),h.size+new Vector2(.56f,.4f));
                if(from.y>=r.yMin&&from.y<=r.yMax){
                    if(from.x<=r.xMin&&to.x>=r.xMin){to.x=r.xMin-.001f;blocked=true;}
                    else if(from.x>=r.xMax&&to.x<=r.xMax){to.x=r.xMax+.001f;blocked=true;}
                }
                if(to.x>=r.xMin&&to.x<=r.xMax){
                    if(from.y<=r.yMin&&to.y>=r.yMin){to.y=r.yMin-.001f;blocked=true;}
                    else if(from.y>=r.yMax&&to.y<=r.yMax){to.y=r.yMax+.001f;blocked=true;}
                }
                if(r.Contains(to)){to=from;blocked=true;}
            }
            player.position=to;
            // Do not let the camera's bottom clamp push a stopped player through a wall.
            if(blocked&&playArea){float overlap=playArea.RootArea.yMin-player.position.y;if(overlap>0){cameraY-=overlap;Follow();}}
        }
        void SpawnRuinsFormation(FastingTopDownHazard.Kind kind,int lane,float offset)
        {
            if(kind==FastingTopDownHazard.Kind.Wall){SpawnRuinsWall(lane,offset);return;}
            int safe=encounterDirector?encounterDirector.OpenLane:2;
            int first=lane==safe?(lane+1)%SpawnLaneCount:lane;
            int count=Random.Range(2,(kind==FastingTopDownHazard.Kind.Tomb?ruinsArt.maximumTombCount:ruinsArt.maximumWallSegments)+1);
            bool vertical=kind==FastingTopDownHazard.Kind.Wall&&Random.value<.5f;
            int side=first<safe?-1:1,adjacent=first+side;
            bool pair=adjacent>=0&&adjacent<SpawnLaneCount&&adjacent!=safe;
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
            if(vertical){for(int i=0;i<SpawnLaneCount;i++)if(i!=safe)starts.Add(i);}
            else{
                for(;count>=2;count--){for(int i=0;i<=SpawnLaneCount-count;i++)if(safe<i||safe>=i+count)starts.Add(i);if(starts.Count>0)break;}
            }
            if(starts.Count==0)return;
            int first=starts.Contains(preferred)?preferred:starts[Random.Range(0,starts.Count)],id=++nextFormationId;
            float length=settings.laneSpacing;
            // Configure artwork and collision before spacing/escape checks, with no grid gaps.
            for(int i=0;i<count;i++){
                var h=Instantiate(vertical&&ruinsArt.verticalWall?ruinsArt.verticalWall:ruinsArt.wall,hazardRoot);h.name=vertical?"RuinsWall_Vertical":"RuinsWall_Horizontal";
                h.formationId=id;h.formationCount=count;h.ConfigureWallSegment(vertical,length,ruinsArt.wallThicknessScale*settings.laterStageObstacleScale,WallFloorThickness);h.warning=0;h.velocity=Vector2.zero;h.scrollOnly=true;h.lifetime=30;
                float y=cameraY+gameCamera.orthographicSize+2+length*.5f+offset;
                h.transform.position=new Vector3(SpawnLaneX(vertical?first:first+i),y+(vertical?i*length:0),0);
                if(!AcceptHazard(h,true)){Destroy(h.gameObject);continue;}RegisterHazard(h);
            }
        }
    }
}
