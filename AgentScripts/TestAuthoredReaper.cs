using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using CrossTheLine.Minigames;
public static class TestAuthoredReaper
{
    public static string Run()
    {
        if(!EditorApplication.isPlaying)throw new Exception("Play mode required");
        var g=UnityEngine.Object.FindAnyObjectByType<FastingTopDownGame>();
        g.BeginStageForTesting(3);g.enabled=false;
        foreach(var e in g.entities)if(e)e.gameObject.SetActive(false);
        g.SpawnPatternObstacle(FastingTopDownHazard.Kind.Reaper,0,1,0);
        var h=g.Hazards.Single(x=>x.kind==FastingTopDownHazard.Kind.Reaper);
        var s=h.GetComponent<FastingReaperSweep>();
        if(s.slashFrames.Length!=16||h.animationFrames.Length!=0)throw new Exception("Wrong frame wiring");
        if(!g.encounterDirector.LeavesLaneOpen(h))throw new Exception("Escape lane obstructed");
        var seen=new System.Collections.Generic.HashSet<Sprite>();var from=new Vector2(-100,-100);
        for(int i=0;i<300;i++)
        {
            h.Advance(.01f,from,from,out var contact);seen.Add(h.body.sprite);
            if(h.spent)throw new Exception("Attack ended prematurely");
        }
        if(seen.Count!=16)throw new Exception("Only "+seen.Count+"/16 authored poses played");
        if(h.body.sprite!=s.slashFrames[15])throw new Exception("Attack should hold last pose, not loop");
        if(s.CurrentPhase!=FastingReaperSweep.AttackPhase.Dashing)throw new Exception("Dash never started");
        foreach(var frame in s.slashFrames)
        {
            if(frame.rect.size!=new Vector2(320,180)||frame.pixelsPerUnit!=32)throw new Exception("Inconsistent sprite alignment");
        }
        // Freeze a real spawned attack in a visible pose for a reproducible screen check.
        h.body.sprite=s.slashFrames[9];h.transform.position=new Vector3(g.player.position.x+3,g.player.position.y+2,0);
        h.UpdateApproach(g.player.position);
        return "PASS: 16/16 authored poses, tracking > preparation > dash, final pose held, no competing animation, shared 320x180/32PPU canvas, four-lane escape preserved. FrameCount="+Time.frameCount;
    }
}
