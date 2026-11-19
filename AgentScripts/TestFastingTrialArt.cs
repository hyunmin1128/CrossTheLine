using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using CrossTheLine.Minigames;
public static class TestFastingTrialArt
{
    public static string Run(){
        if(!Application.isPlaying)throw new Exception("Play required");var g=UnityEngine.Object.FindAnyObjectByType<FastingTopDownGame>();g.enabled=false;var flags=BindingFlags.Instance|BindingFlags.NonPublic;int n=0;Action<bool,string> ok=(b,s)=>{if(!b)throw new Exception(s);n++;};
        try{
            foreach(var kind in new[]{FastingTopDownHazard.Kind.Fire,FastingTopDownHazard.Kind.Thorns,FastingTopDownHazard.Kind.Pillar,FastingTopDownHazard.Kind.Reaper}){
                g.Begin();typeof(FastingTopDownGame).GetProperty("StageIndex").SetValue(g,3);typeof(FastingTopDownGame).GetMethod("ApplyStage",flags).Invoke(g,new object[]{false});foreach(var e in g.entities)e.gameObject.SetActive(false);
                ok(g.backgrounds.All(b=>b.sprite==g.trialArt.background&&b.color==Color.white),"Background art/tint");
                foreach(var bg in g.backgrounds)ok(Mathf.Abs(bg.bounds.size.x-30)<.01f&&Mathf.Abs(bg.bounds.size.y-16.875f)<.01f,"World dimensions");
                g.SpawnPatternObstacle(kind,0,1,0);var h=g.Hazards.Single();ok(h.body.sprite==g.trialArt.PrefabFor(kind,null).body.sprite,"Spawn art "+kind);ok((h.velocity==Vector2.zero)==(kind!=FastingTopDownHazard.Kind.Reaper),"Motion type");h.UpdateApproach(g.player.position);ok(Vector2.Distance(h.body.bounds.center,h.transform.position)<.05f,"Centered artwork");
                if(kind==FastingTopDownHazard.Kind.Fire||kind==FastingTopDownHazard.Kind.Pillar){ok(h.animationFrames.Length==5,"Five frames");var first=h.body.sprite;h.Advance(.15f,new Vector2(100,100),new Vector2(100,100),out _);ok(h.body.sprite!=first,"Animation advances");}
                h.transform.position=g.player.position;h.velocity=Vector2.zero;typeof(FastingEncounterDirector).GetField("nextWave",flags).SetValue(g.encounterDirector,100f);typeof(FastingEncounterDirector).GetField("nextRandom",flags).SetValue(g.encounterDirector,100f);g.Tick(.01f,Vector2.zero);ok(g.Health==4,"Actual collision "+kind);if(kind==FastingTopDownHazard.Kind.Fire||kind==FastingTopDownHazard.Kind.Pillar)ok(g.PrayerBlockRemaining>0,"Fire blocks prayer");
            }
            g.Begin();ok(g.backgrounds.All(b=>b.sprite!=g.trialArt.background),"Retry restores desert");return n+" checks passed: background scale, four spawned prefabs, fixed/moving behavior, 5-frame animation, collision, prayer lock and retry.";
        }finally{Time.timeScale=1;g.Begin();g.enabled=true;}
    }
}
