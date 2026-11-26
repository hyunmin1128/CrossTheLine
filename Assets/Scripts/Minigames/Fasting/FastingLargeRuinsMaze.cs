using System.Collections.Generic;
using UnityEngine;
namespace CrossTheLine.Minigames
{
    public sealed partial class FastingTopDownGame
    {
        bool mazeMirror;
        float WallFloorThickness {get{var sprite=ConfiguredObstacle(FastingTopDownHazard.Kind.Wall,ruinsArt.verticalWall,true).body.sprite;return Mathf.Clamp(sprite.bounds.size.x/sprite.bounds.size.y*(settings.laneSpacing+.16f)*ruinsArt.wallThicknessScale*settings.laterStageObstacleScale,.3f,.8f);}}
        void SpawnLargeRuinsMaze(float offset)
        {
            foreach(var h in hazards)if(h&&!h.spent&&h.kind==FastingTopDownHazard.Kind.Wall&&h.transform.position.y>player.position.y-2){CurrentBalance.suppressedGroups++;return;}
            var pending=new List<FastingTopDownHazard>();int id=++nextFormationId;
            float length=settings.laneSpacing,y=cameraY+gameCamera.orthographicSize+5+offset;
            int sign=mazeMirror?-1:1;mazeMirror=!mazeMirror;
            int escape=sign>0?SpawnLaneCount-1:0;float escapeX=SpawnLaneX(escape);
            foreach(var other in hazards)if(other&&!other.spent&&other.transform.position.y>player.position.y-2&&Mathf.Abs(other.transform.position.x-escapeX)<other.size.x*.5f+.65f){CurrentBalance.suppressedGroups++;return;}
            // One short-lived large roadblock with an open side, rather than a stage-wide maze.
            int segments=Mathf.Clamp(SpawnLaneCount-2,2,7);
            float roadCenter=(SpawnRoadRange.x+SpawnRoadRange.y)*.5f;
            for(int col=0;col<segments;col++)pending.Add(CreateMazeWall(false,roadCenter+sign*(SpawnLaneX(col)-roadCenter),y,id));
            // Connected vertical return makes an L rather than disconnected short blocks.
            float edge=sign>0?SpawnRoadRange.x+.5f:SpawnRoadRange.y-.5f;
            for(int i=1;i<=2;i++)pending.Add(CreateMazeWall(true,edge,y+i*length,id));
            bool clear=true;
            foreach(var h in pending)if(!FitsSpawnRoad(h)||!PickupPathClear(h)||SpawnOverlapsHazard(h)){clear=false;break;}
            foreach(var h in pending){if(clear){h.formationCount=pending.Count;RegisterHazard(h);}else {RecordRejection(h);Destroy(h.gameObject);}}
            if(clear&&encounterDirector)encounterDirector.ReserveSweepEscape(escape);
        }
        FastingTopDownHazard CreateMazeWall(bool vertical,float x,float y,int id)
        {
            var h=Instantiate(ConfiguredObstacle(FastingTopDownHazard.Kind.Wall,vertical&&ruinsArt.verticalWall?ruinsArt.verticalWall:ruinsArt.wall,vertical),hazardRoot);
            h.name=vertical?"MazeWall_Vertical":"MazeWall_Horizontal";h.formationId=id;
            h.ConfigureWallSegment(vertical,settings.laneSpacing,ruinsArt.wallThicknessScale*settings.laterStageObstacleScale,WallFloorThickness);
            h.transform.position=new Vector3(x,y);h.warning=0;h.velocity=Vector2.zero;h.scrollOnly=true;h.lifetime=40;return h;
        }
    }
}
