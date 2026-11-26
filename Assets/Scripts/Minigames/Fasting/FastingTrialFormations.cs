using System.Collections.Generic;
using UnityEngine;
namespace CrossTheLine.Minigames
{
    public sealed partial class FastingTopDownGame
    {
        int lastReaperEscape=-1;
        void SpawnTrialFormation(FastingTopDownHazard.Kind kind,int preferred,float offset)
        {
            if(kind==FastingTopDownHazard.Kind.Reaper){SpawnReaperSweep(offset);return;}
            int safe=encounterDirector?encounterDirector.OpenLane:2;
            int width=kind==FastingTopDownHazard.Kind.Fire?1:kind==FastingTopDownHazard.Kind.Thorns?Random.Range(1,Mathf.Clamp(trialArt.maximumThornCount,1,3)+1):2;
            int rows=kind==FastingTopDownHazard.Kind.Pillar&&Random.value<.5f?2:1;
            var starts=new List<int>();
            for(;width>0;width--){for(int s=0;s<=SpawnLaneCount-width;s++)if(safe<s||safe>=s+width)starts.Add(s);if(starts.Count>0)break;}
            if(starts.Count==0)return;int first=starts.Contains(preferred)?preferred:starts[Random.Range(0,starts.Count)];int id=++nextFormationId;
            for(int row=0;row<rows;row++)for(int col=0;col<width;col++)SpawnAbove(kind,first+col,1,offset+row*settings.laneSpacing,true,false,id,width*rows);
        }
        void SpawnReaperSweep(float offset)
        {
            foreach(var other in hazards)if(other&&!other.spent&&other.kind==FastingTopDownHazard.Kind.Reaper){CurrentBalance.suppressedGroups++;return;}
            int maxStart=SpawnLaneCount-4;
            int first=Random.Range(0,maxStart+1);if(first==lastReaperEscape)first=(first+1)%(maxStart+1);
            int escape=first>0?first-1:first+4;
            float escapeX=SpawnLaneX(escape);
            foreach(var other in hazards)if(other&&!other.spent&&other.transform.position.y>player.position.y-2&&Mathf.Abs(other.transform.position.x-escapeX)<other.size.x*other.MaximumApproachScale*.5f+.5f){CurrentBalance.suppressedGroups++;return;}
            int original=encounterDirector?encounterDirector.OpenLane:escape;
            if(encounterDirector)encounterDirector.ReserveSweepEscape(escape);
            var h=Instantiate(ConfiguredObstacle(FastingTopDownHazard.Kind.Reaper,trialArt.reaper),hazardRoot);h.name="Reaper_FourLaneScytheSweep";
            h.ConfigureSweep(settings.laneSpacing);h.Enlarge(settings.laterStageObstacleScale,true);h.warning=trialArt.reaperWarningSeconds;h.velocity=Vector2.down*trialArt.reaperSpeed;h.lifetime=12;
            // Display the authored pixels at their imported PPU rather than fitting one lane.
            h.body.transform.localScale=Vector3.one;
            h.body.transform.localPosition=-(Vector3)h.artworkCenter;
            h.transform.position=new Vector3(SpawnLaneX(first+1.5f),cameraY+gameCamera.orthographicSize+3+offset,0);
            if(!AcceptHazard(h,true)){Destroy(h.gameObject);if(encounterDirector)encounterDirector.ReserveSweepEscape(original);return;}
            var sweep=h.GetComponent<FastingReaperSweep>();
            if(sweep){sweep.BeginTracking(this,escape);if(h.spent){Destroy(h.gameObject);if(encounterDirector)encounterDirector.ReserveSweepEscape(original);return;}}
            RegisterHazard(h);lastReaperEscape=first;
        }
    }
}
