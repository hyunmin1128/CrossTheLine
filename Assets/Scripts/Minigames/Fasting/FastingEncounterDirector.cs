using UnityEngine;

namespace CrossTheLine.Minigames
{
    public sealed class FastingEncounterDirector : MonoBehaviour
    {
        public FastingTopDownGame game;
        public enum Phase { Attack, Clearing, PrayerWindow }
        public Phase CurrentPhase{get;private set;}
        public int OpenLane{get;private set;}
        float remaining,nextWave;
        public int SpawnAttempts{get;private set;}
        public void ResetEncounter()
        {
            CurrentPhase=Phase.Attack;OpenLane=Random.Range(0,5);remaining=game.settings.StageValue(game.settings.attackSeconds,game.StageIndex,5);nextWave=1.5f;SpawnAttempts=0;
        }
        public void Tick(float dt)
        {
            if(game.GateVisible)return;
            if(CurrentPhase==Phase.Attack)
            {
                remaining-=dt;if(remaining<=0){CurrentPhase=Phase.Clearing;return;}
                nextWave-=dt;if(nextWave<=0){SpawnAttempts++;game.SpawnWave();nextWave=game.settings.NextSpawnInterval(game.StageIndex);}return;
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
            remaining-=dt;if(remaining<=0){OpenLane=(OpenLane+Random.Range(1,5))%5;CurrentPhase=Phase.Attack;remaining=game.settings.StageValue(game.settings.attackSeconds,game.StageIndex,5);nextWave=.25f;}
        }
        public bool LeavesLaneOpen(FastingTopDownHazard h)
        {
            if(Mathf.Abs(h.velocity.x)>.01f)return true; // Side winds have vertical escape space.
            float half=h.size.x*.5f*(h.kind==FastingTopDownHazard.Kind.Quicksand?h.quicksandMaximumScale:1)+.5f;
            return Mathf.Abs(h.transform.position.x-(OpenLane-2)*game.settings.laneSpacing)>half;
        }
    }
}
