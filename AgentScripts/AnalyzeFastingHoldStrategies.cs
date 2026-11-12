using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using CrossTheLine.Minigames;
public static class AnalyzeFastingHoldStrategies
{
    public static async Task<string> Run()
    {
        var g=UnityEngine.Object.FindAnyObjectByType<FastingTopDownGame>();if(!Application.isPlaying||!g)throw new Exception("Play minigame");var random=UnityEngine.Random.state;g.enabled=false;
        var rows=new List<string>();
        try
        {
            foreach(float lane in new[]{-6f,0f,6f})
            {
                int wins=0;float sumTime=0,sumPrayer=0,sumWaves=0,sumAccepted=0;int alive=0;
                for(int seed=0;seed<10;seed++)
                {
                    UnityEngine.Random.InitState(100+seed);g.Begin();g.player.position=new Vector3(lane,0);var ids=new HashSet<FastingTopDownHazard>();int ticks=0;
                    while(g.Elapsed<300&&g.StageIndex==0&&g.Health>0)
                    {
                        if(g.HasWater&&g.Health<=4)g.UseWater();g.Tick(.05f,Vector2.zero,true);
                        foreach(var h in g.Hazards)if(h)ids.Add(h);
                        if(++ticks%300==0)await Task.Yield();
                    }
                    bool complete=g.StageIndex>0;if(complete)wins++;if(g.Health>0)alive++;
                    sumTime+=g.Elapsed;sumPrayer+=g.Prayer;sumAccepted+=ids.Count;sumWaves+=(int)typeof(FastingTopDownGame).GetField("waveNumber",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(g);
                }
                rows.Add($"Lane {lane}: completed={wins}/10 alive={alive}/10; mean elapsed={sumTime/10:0.0}s; prayer at stop={sumPrayer/10:0.0}; mean waves={sumWaves/10:0.0}, accepted hazards={sumAccepted/10:0.0}");
            }
            return string.Join("\n",rows)+"\nConstant Space, no directional input, automatic canteen at <=4 HP. 300-second cap. Simplified automated baseline, not human playtesting. Stage-transition resets prayer/wave counters, so counters for wins are not comparable.";
        }
        finally{UnityEngine.Random.state=random;g.Begin();g.enabled=true;}
    }
}
