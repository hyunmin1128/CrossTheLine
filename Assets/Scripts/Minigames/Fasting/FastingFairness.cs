using UnityEngine;
namespace CrossTheLine.Minigames
{
    public sealed partial class FastingTopDownGame
    {
        public Vector2 SpawnRoadRange {get{var edges=spawnSettings&&spawnSettings.stages!=null&&StageIndex<spawnSettings.stages.Length?spawnSettings.stages[StageIndex].roadEdges:new Vector2(.18f,.82f);float width=gameCamera.orthographicSize*gameCamera.aspect*2;return new Vector2(gameCamera.transform.position.x+(Mathf.Clamp(edges.x,0,.45f)-.5f)*width,gameCamera.transform.position.x+(Mathf.Clamp(edges.y,.55f,1)-.5f)*width);}}
        public int SpawnLaneCount=>Mathf.Max(5,Mathf.FloorToInt((SpawnRoadRange.y-SpawnRoadRange.x)/settings.laneSpacing));
        public float SpawnLaneCenter=>(SpawnLaneCount-1)*.5f;
        public float SpawnLaneX(float lane)=>(SpawnRoadRange.x+SpawnRoadRange.y)*.5f+(lane-SpawnLaneCenter)*settings.laneSpacing;
        public bool FitsSpawnRoad(FastingTopDownHazard h){var range=SpawnRoadRange;var visual=h.RoadVisualRange;return visual.x>=range.x-.01f&&visual.y<=range.y+.01f&&h.CollisionCenter.x-h.size.x*.5f>=range.x-.01f&&h.CollisionCenter.x+h.size.x*.5f<=range.y+.01f;}
        public float SlowStatusRemaining=>Mathf.Max(slow,thornSlow);
        public float RootStatusRemaining=>root;
        public float BurnStatusRemaining=>prayerLock;
        public float ReverseStatusRemaining=>reverse;


        public bool DarknessProtecting=>StageIndex>0&&caveVisionMask&&caveVisionMask.ObscuresGameplay;
        void RefreshDarknessProtection(float dt)
        {
            if(!caveVisionMask)return;caveVisionMask.Advance(dt);
            if(DarknessProtecting)grace=Mathf.Max(grace,caveVisionMask.recoveryGrace);
        }
    }
}
