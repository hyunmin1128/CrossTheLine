using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using CrossTheLine.Minigames;
public static class StudyFastingBalancePass
{
    static Vector2 Pilot(FastingTopDownGame g)
    {
        Vector2 p=g.player.position;float best=float.PositiveInfinity,target=p.x;
        foreach(var side in g.Hazards)
        {
            if(!side||Mathf.Abs(side.velocity.x)<.1f||Mathf.Abs(side.transform.position.y-p.y)>1.7f)continue;
            float time=(p.x-side.transform.position.x)/side.velocity.x;
            if(time>-.15f&&time<1.5f){float y=side.transform.position.y;float dir=p.y<y?-1:1;if(p.y-g.gameCamera.transform.position.y< -3.3f)dir=1;return new Vector2(0,dir);}
        }
        for(int lane=0;lane<5;lane++)
        {
            float x=(lane-2)*3,score=Mathf.Abs(x-p.x)*.08f,travel=Mathf.Abs(x-p.x)/g.settings.manualMoveSpeed;
            foreach(var h in g.Hazards)
            {
                if(!h||h.spent||Mathf.Abs(h.velocity.x)>.1f)continue;
                float t=(h.transform.position.y-p.y-(h.size.y*.5f+.25f))/Mathf.Max(.01f,g.CurrentSpeed-h.velocity.y);if(t<-.3f||t>2.8f)continue;
                float half=h.size.x*.5f+.5f,left=h.transform.position.x-half,right=h.transform.position.x+half;
                if(x>left&&x<right)score+=t<.7f?150:35/(.4f+t);
                if(t<travel+.2f&&Mathf.Max(x,p.x)>left&&Mathf.Min(x,p.x)<right)score+=30;
            }
            foreach(var e in g.entities)
            {
                if(!e||!e.gameObject.activeSelf||e.consumed||Mathf.Abs(e.transform.position.x-x)>.8f)continue;
                float dy=e.transform.position.y-p.y;if(dy<0||dy>6)continue;score-= (e.kind==FastingTopDownEntity.Kind.Book?5:1)/(1+dy);
            }
            if(score<best){best=score;target=x;}
        }
        return new Vector2(Mathf.Clamp((target-p.x)/(g.settings.manualMoveSpeed*.05f),-1,1),0);
    }
    public static async Task<string> Run()
    {
        var g=UnityEngine.Object.FindAnyObjectByType<FastingTopDownGame>();if(!Application.isPlaying)throw new Exception("Play minigame");g.enabled=false;var rng=UnityEngine.Random.state;var lines=new List<string>{"mode,seed,success,seconds,health"};var wins=new List<float>();int total=0;
        try
        {
            for(int seed=0;seed<30;seed++)
            {
                UnityEngine.Random.InitState(100+seed);g.Begin();int ticks=0;
                while(g.Elapsed<300&&g.Health>0&&g.StageIndex==0)
                {
                    if(g.HasWater&&(g.Health<=3||g.PrayerBlockRemaining>.2f))g.UseWater();
                    Vector2 move=Pilot(g);g.Tick(.05f,move,move.sqrMagnitude<.01f);if(++ticks%300==0)await Task.Yield();
                }
                bool won=g.StageIndex>0;if(won)wins.Add(g.Elapsed);total++;
                lines.Add($"AvoidThenPray,{seed},{won},{g.Elapsed:0.0},{g.Health:0.0}");
            }
            string dir="Docs/QA/FastingTopDown/BalancePass-20261112";Directory.CreateDirectory(dir);File.WriteAllLines(dir+"/AvoidThenPray.csv",lines);
            return $"Avoid then pray baseline: {wins.Count}/{total} completed. "+(wins.Count>0?$"Completed min/median/max {wins.Min():0.0}/{wins.OrderBy(x=>x).ElementAt(wins.Count/2):0.0}/{wins.Max():0.0}s.":"")+" Simplified automated pilot, not human success rate. It can see world hazard positions and is not a fair human benchmark.";
        }
        finally{UnityEngine.Random.state=rng;g.Begin();g.enabled=true;}
    }
}
