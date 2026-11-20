using UnityEngine;

namespace CrossTheLine.Minigames
{
    public sealed partial class FastingTopDownGame
    {
        public FastingEncounterDirector encounterDirector;
        public FastingPlayerAura playerAura;
        public FastingHitVignette hitVignette;
        public float WaterBoostRemaining=>waterBoost;
        public bool PrayerPreparing=>prayerHeldFor>0&&!Praying&&PrayerBlockRemaining<=0&&Prayer<PrayerTarget;
        public float PreparationProgress=>Mathf.Clamp01(prayerHeldFor/Mathf.Max(.01f,settings.prayerPreparationSeconds));
        public bool InPrayerWindow=>encounterDirector&&encounterDirector.CurrentPhase==FastingEncounterDirector.Phase.PrayerWindow;
        void ConsumeFever(float dt)
        {
            if(fever<=0)return;fever=Mathf.Max(0,fever-dt);if(fever<=0){flames=0;buff=0;}
        }
        void PlaceEntity(FastingTopDownEntity e,bool first)
        {
            if(e.kind==FastingTopDownEntity.Kind.Cover){e.gameObject.SetActive(false);return;}
            float gap=e.kind==FastingTopDownEntity.Kind.Water?CurrentSpeed*settings.StageValue(settings.wellIntervalSeconds,StageIndex,20):Mathf.Max(12,settings.stageLength);
            float y=first?(e.kind==FastingTopDownEntity.Kind.Water?gap:e.transform.position.y):Mathf.Max(e.transform.position.y+gap,player.position.y+gap-7);
            e.transform.position=new Vector3(0,y,0);e.SetUsed(false);
            if(Random.value>=settings.StageDropChance(e.kind,StageIndex)){e.gameObject.SetActive(false);return;}
            int start=Random.Range(0,SpawnLaneCount);bool placed=false;
            for(int n=0;n<SpawnLaneCount;n++)
            {
                e.transform.position=new Vector3(SpawnLaneX((start+n)%SpawnLaneCount),y,0);
                if(PickupPositionClear(e)&&NoOtherPickup(e)){placed=true;break;}
            }
            e.gameObject.SetActive(placed);
        }
        bool NoOtherPickup(FastingTopDownEntity e)
        {
            foreach(var other in entities)
            {
                if(!other||other==e||other.stage!=StageIndex||!other.gameObject.activeSelf||other.consumed)continue;
                Vector2 delta=other.transform.position-e.transform.position,half=(other.footprint+e.footprint)*.5f+Vector2.one*.5f;
                if(Mathf.Abs(delta.x)<half.x&&Mathf.Abs(delta.y)<half.y)return false;
            }
            return true;
        }
        bool AcceptHazard(FastingTopDownHazard h,bool keepLane=false)
        {
            bool side=Mathf.Abs(h.velocity.x)>.01f;float originalX=h.transform.position.x;string rejectedReason="escape";
            int first=Mathf.Clamp(Mathf.RoundToInt((originalX-(SpawnRoadRange.x+SpawnRoadRange.y)*.5f)/settings.laneSpacing+SpawnLaneCenter-(h.occupiedLanes-1)*.5f),0,SpawnLaneCount-h.occupiedLanes);
            for(int attempt=0;attempt<(side||keepLane?1:SpawnLaneCount+1-h.occupiedLanes);attempt++)
            {
                if(!side){int lane=(first+attempt)%(SpawnLaneCount+1-h.occupiedLanes);h.transform.position=new Vector3(SpawnLaneX(lane+(h.occupiedLanes-1)*.5f),h.transform.position.y,0);}
                if(!side&&!FitsSpawnRoad(h)){rejectedReason="decoration";continue;}
                if(encounterDirector&&!encounterDirector.LeavesLaneOpen(h))continue;
                rejectedReason="pickup";
                bool allClear=true;
                foreach(var e in entities)
                {
                    if(!e||!e.gameObject.activeSelf||e.consumed||e.stage!=StageIndex||!PathOverlaps(h,e))continue;
                    // Visible rewards never jump. Offscreen rewards can choose a new lane instead of protecting a whole road.
                    if(e.transform.position.y<cameraY+gameCamera.orthographicSize+1){allClear=false;break;}
                    Vector3 original=e.transform.position;bool moved=false;int start=Random.Range(0,SpawnLaneCount);
                    for(int n=0;n<SpawnLaneCount;n++)
                    {
                        e.transform.position=new Vector3(SpawnLaneX((start+n)%SpawnLaneCount),original.y,0);
                        if(!PathOverlaps(h,e)&&PickupPositionClear(e)&&NoOtherPickup(e)){moved=true;break;}
                    }
                    if(!moved){e.transform.position=original;allClear=false;break;}
                }
                if(allClear){rejectedReason="overlap";if(!SpawnOverlapsHazard(h))return true;}
            }
            RecordRejection(h,rejectedReason);h.transform.position=new Vector3(originalX,h.transform.position.y,0);return false;
        }
        bool SpawnOverlapsHazard(FastingTopDownHazard candidate)
        {
            Vector2 extent=Vector2.Max(candidate.size,(Vector2)candidate.body.bounds.size)*.625f;
            foreach(var other in hazards)
            {
                if(!other||other.spent||!other.gameObject.activeSelf)continue;
                if(candidate.formationId!=0&&candidate.formationId==other.formationId)continue;
                Vector2 otherExtent=Vector2.Max(other.size,(Vector2)other.body.bounds.size)*.625f;
                Vector2 gap=extent+otherExtent+new Vector2(.15f,.35f),delta=candidate.transform.position-other.transform.position;
                if(Mathf.Abs(delta.x)<gap.x&&Mathf.Abs(delta.y)<gap.y)return true;
            }
            return false;
        }
    }
}
