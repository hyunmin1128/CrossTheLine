using UnityEngine;

namespace CrossTheLine.Minigames
{
    public sealed class FastingEncounterDirector : MonoBehaviour
    {
        public FastingTopDownGame game;
        public enum Phase { Attack, Clearing, PrayerWindow }
        public Phase CurrentPhase{get;private set;}
        public int OpenLane{get;private set;}
        public void ReserveSweepEscape(int lane){OpenLane=Mathf.Clamp(lane,0,4);}
        float remaining,nextWave;
        [Min(1)] public float patternIntervalMultiplier=1.8f;
        public Vector2 randomIntervals=new Vector2(.85f,1.65f);
        [Range(.3f,1)] public float desertSpawnIntervalMultiplier=.75f;
        public bool matchDesertSpawnRate=true;
        float StageIntervalMultiplier=>game&&(game.StageIndex==0||matchDesertSpawnRate)?desertSpawnIntervalMultiplier:1;
        float nextRandom;
        public float Frequency=>game&&game.spawnSettings?game.spawnSettings.Frequency(game.StageIndex):1;
        float ScaleInterval(float seconds)=>seconds/Mathf.Max(.01f,Frequency);
        public int SpawnAttempts{get;private set;}
        public int RandomSpawnAttempts{get;private set;}
        public int PatternSpawnAttempts{get;private set;}
        float RandomInterval()=>ScaleInterval(Random.Range(Mathf.Max(.35f,Mathf.Min(randomIntervals.x,randomIntervals.y)),Mathf.Max(.35f,Mathf.Max(randomIntervals.x,randomIntervals.y)))*StageIntervalMultiplier);
        public void ResetEncounter()
        {
            CurrentPhase=Phase.Attack;OpenLane=Random.Range(0,5);remaining=game.settings.StageValue(game.settings.attackSeconds,game.StageIndex,5);nextWave=ScaleInterval(1.5f);nextRandom=RandomInterval();SpawnAttempts=RandomSpawnAttempts=PatternSpawnAttempts=0;if(game.obstaclePatterns)game.obstaclePatterns.BeginAttack();
        }
        public void Tick(float dt)
        {
            if(game.GateVisible)return;
            if(CurrentPhase==Phase.Attack)
            {
                remaining-=dt;if(remaining<=0){CurrentPhase=Phase.Clearing;return;}
                if(Frequency<=0)return;
                // Separate clocks: random obstacles are not tied to the selected pattern or its beat.
                nextWave-=dt;if(nextWave<=0){SpawnAttempts++;PatternSpawnAttempts++;game.SpawnWave();nextWave=ScaleInterval(game.settings.NextSpawnInterval(matchDesertSpawnRate?0:game.StageIndex)*Mathf.Max(1,patternIntervalMultiplier)*StageIntervalMultiplier);}
                nextRandom-=dt;if(nextRandom<=0){SpawnAttempts++;RandomSpawnAttempts++;game.SpawnRandomObstacle();nextRandom=RandomInterval();}return;
            }
            if(CurrentPhase==Phase.Clearing)
            {
                foreach(var h in game.Hazards)
                {
                    if(!h||h.spent)continue;
                    if(Mathf.Abs(h.velocity.x)>.01f)return;
                    if(h.transform.position.y+h.size.y*.5f+1>game.player.position.y)return;
                }
                CurrentPhase=Phase.PrayerWindow;remaining=game.settings.StageValue(game.settings.restSeconds,game.StageIndex,3);return;
            }
            remaining-=dt;if(remaining<=0){OpenLane=(OpenLane+Random.Range(1,5))%5;CurrentPhase=Phase.Attack;remaining=game.settings.StageValue(game.settings.attackSeconds,game.StageIndex,5);nextWave=ScaleInterval(Random.Range(.35f,.7f));nextRandom=RandomInterval();if(game.obstaclePatterns)game.obstaclePatterns.BeginAttack();}
        }
        public bool LeavesLaneOpen(FastingTopDownHazard h)
        {
            if(Mathf.Abs(h.velocity.x)>.01f)return true; // Side winds have vertical escape space.
            float half=h.size.x*.5f*h.MaximumApproachScale+.5f;
            return Mathf.Abs(h.transform.position.x-(OpenLane-2)*game.settings.laneSpacing)>half;
        }
    }
}
