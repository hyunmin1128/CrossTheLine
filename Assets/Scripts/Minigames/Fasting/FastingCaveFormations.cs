using System.Collections.Generic;
using UnityEngine;
namespace CrossTheLine.Minigames
{
    public sealed partial class FastingTopDownGame
    {
        int nextFormationId;
        void SpawnCaveFormation(FastingTopDownHazard.Kind kind,int preferredLane,float heightOffset)
        {
            int max=kind==FastingTopDownHazard.Kind.Bat?Mathf.Clamp(caveArt.maximumBatCount,1,3):Mathf.Clamp(caveArt.maximumFixedCount,1,2);
            int count=Random.Range(1,max+1),safe=encounterDirector?encounterDirector.OpenLane:-1;var starts=new List<int>();
            // A three-wide flock may need to shrink when the escape corridor splits the five roads.
            for(;count>0;count--){for(int i=0;i<=5-count;i++)if(safe<i||safe>=i+count)starts.Add(i);if(starts.Count>0)break;}
            if(count<=0)return;int first=starts.Contains(preferredLane)?preferredLane:starts[Random.Range(0,starts.Count)];int id=++nextFormationId;
            for(int i=0;i<count;i++)SpawnAbove(kind,first+i,1,heightOffset,true,false,id,count);
        }
    }
}
