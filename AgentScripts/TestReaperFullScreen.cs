using System;
using System.Linq;
using UnityEngine;
using CrossTheLine.Minigames;
public static class TestReaperFullScreen
{
    public static string Run()
    {
        var g=UnityEngine.Object.FindAnyObjectByType<FastingTopDownGame>();g.BeginStageForTesting(3);g.enabled=false;
        foreach(var e in g.entities)if(e)e.gameObject.SetActive(false);
        g.SpawnPatternObstacle(FastingTopDownHazard.Kind.Reaper,0,1,0);
        var h=g.Hazards.Single(x=>x.kind==FastingTopDownHazard.Kind.Reaper);var s=h.GetComponent<FastingReaperSweep>();
        float top=g.gameCamera.transform.position.y+g.gameCamera.orthographicSize;
        float bottom=g.gameCamera.transform.position.y-g.gameCamera.orthographicSize;
        if(Mathf.Abs(h.body.bounds.max.y-(top-s.topInset))>.01f)throw new Exception("Not at screen top");
        var safe=new Vector2(-100,-100);
        for(int i=0;i<100;i++)h.Advance(.01f,safe,safe,out var hit);
        if(Mathf.Abs(h.body.bounds.max.y-(top-s.topInset))>.01f)throw new Exception("Tracking moved down");
        int frames=0;while(s.CurrentPhase!=FastingReaperSweep.AttackPhase.Dashing&&frames++<200)h.Advance(.01f,safe,safe,out var hit);
        if(s.CurrentPhase!=FastingReaperSweep.AttackPhase.Dashing)throw new Exception("Never attacked");
        h.TrackVisibility(g.gameCamera,3);
        g.player.position=new Vector3(h.transform.position.x,h.transform.position.y-2,0);float health=g.Health;
        for(int i=0;i<15;i++)g.Tick(.01f,Vector2.zero,false);
        if(g.Health>=health)throw new Exception("Attack did not hit the player");
        if(h.spent||!h.gameObject.activeSelf)throw new Exception("Reaper vanished on contact");
        for(int i=0;i<250&&!h.spent;i++)g.Tick(.01f,Vector2.zero,false);
        if(!h.spent)throw new Exception("Attack not cleaned up below screen");
        // Prepare another attack at the top for visual inspection.
        g.BeginStageForTesting(3);foreach(var e in g.entities)if(e)e.gameObject.SetActive(false);
        g.SpawnPatternObstacle(FastingTopDownHazard.Kind.Reaper,0,1,0);g.enabled=false;
        return "PASS: aligned at camera top, remained at top during tracking, damaged player without disappearing, crossed bottom and despawned. FrameCount="+Time.frameCount;
    }
}
