using System.Collections.Generic;
using UnityEngine;
namespace CrossTheLine.Minigames
{
    public sealed partial class FastingTopDownGame
    {
        bool mazeMirror;
        void SpawnLargeRuinsMaze(float offset)
        {
            foreach(var h in hazards)if(h&&!h.spent&&h.kind==FastingTopDownHazard.Kind.Wall&&h.transform.position.y>player.position.y-2)return;
            var pending=new List<FastingTopDownHazard>();int id=++nextFormationId;
            float length=settings.laneSpacing,y=cameraY+gameCamera.orthographicSize+5+offset;
            int sign=mazeMirror?-1:1;mazeMirror=!mazeMirror;
            // Two long staggered barriers: pass the right opening, then the left opening.
            for(int row=0;row<2;row++)for(int col=0;col<7;col++)
                pending.Add(CreateMazeWall(false,sign*((row==0?-13.5f:-4.5f)+col*length),y+row*11,id));
            // Connected vertical return makes an L rather than disconnected short blocks.
            for(int i=1;i<=2;i++)pending.Add(CreateMazeWall(true,sign*-14.5f,y+i*length,id));
            bool clear=true;
            foreach(var h in pending)if(!PickupPathClear(h)||SpawnOverlapsHazard(h)){clear=false;break;}
            foreach(var h in pending){if(clear){h.formationCount=pending.Count;hazards.Add(h);}else Destroy(h.gameObject);}
        }
        FastingTopDownHazard CreateMazeWall(bool vertical,float x,float y,int id)
        {
            var h=Instantiate(vertical&&ruinsArt.verticalWall?ruinsArt.verticalWall:ruinsArt.wall,hazardRoot);
            h.name=vertical?"MazeWall_Vertical":"MazeWall_Horizontal";h.formationId=id;
            h.ConfigureWallSegment(vertical,settings.laneSpacing,ruinsArt.wallThicknessScale*settings.laterStageObstacleScale);
            h.transform.position=new Vector3(x,y);h.warning=0;h.velocity=Vector2.zero;h.scrollOnly=true;h.lifetime=40;return h;
        }
    }
}
