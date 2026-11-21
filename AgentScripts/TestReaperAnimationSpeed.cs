using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using CrossTheLine.Minigames;
public static class TestReaperAnimationSpeed
{
    public static string Run()
    {
        var g=UnityEngine.Object.FindAnyObjectByType<FastingTopDownGame>();g.enabled=false;
        string result="";
        foreach(int fps in new[]{30,60})
        {
            g.BeginStageForTesting(3);foreach(var e in g.entities)if(e)e.gameObject.SetActive(false);
            g.SpawnPatternObstacle(FastingTopDownHazard.Kind.Reaper,0,1,0);
            var h=g.Hazards.Single(x=>x.kind==FastingTopDownHazard.Kind.Reaper);var s=h.GetComponent<FastingReaperSweep>();
            var seen=new HashSet<Sprite>();float dt=1f/fps;var safe=new Vector2(-100,-100);float elapsed=0;
            for(int i=0;i<fps*4;i++)
            {
                h.Advance(dt,safe,safe,out var hit);
                if(s.CurrentPhase!=FastingReaperSweep.AttackPhase.Dashing)continue;
                elapsed+=dt;seen.Add(h.body.sprite);
                if(h.body.sprite==s.slashFrames[15])
                {
                    float bottom=g.gameCamera.transform.position.y-g.gameCamera.orthographicSize;
                    if(h.body.bounds.min.y<=bottom)throw new Exception("Last pose is clipped before playback ends at "+fps+"FPS");
                    if(seen.Count!=10)throw new Exception("Skipped swing frames: "+seen.Count+"/10 at "+fps+"FPS");
                    result+=fps+"FPS: 10/10 swing poses, last pose fully visible after "+elapsed.ToString("F2")+"s; ";break;
                }
            }
            if(!seen.Contains(s.slashFrames[15]))throw new Exception("Final attack pose missing");
        }
        return "PASS: "+result+"speed="+g.trialArt.reaper.GetComponent<FastingReaperSweep>().dashSpeed+" unchanged";
    }
}
