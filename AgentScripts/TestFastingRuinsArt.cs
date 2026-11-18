using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using CrossTheLine.Minigames;
public static class TestFastingRuinsArt
{
    public static string Run(){
        if(!Application.isPlaying)throw new Exception("Play required");var g=UnityEngine.Object.FindAnyObjectByType<FastingTopDownGame>();g.enabled=false;int n=0;Action<bool,string> ok=(b,s)=>{if(!b)throw new Exception(s);n++;};
        var flags=BindingFlags.NonPublic|BindingFlags.Instance;
        try{
            g.Begin();typeof(FastingTopDownGame).GetProperty("StageIndex").SetValue(g,2);typeof(FastingTopDownGame).GetMethod("ApplyStage",flags).Invoke(g,new object[]{false});
            ok(g.backgrounds.All(b=>b.sprite==g.ruinsArt.background&&b.color==Color.white),"Ruins background without tint");
            foreach(var b in g.backgrounds)ok(Mathf.Abs(b.bounds.size.x-30)<.01f&&Mathf.Abs(b.bounds.size.y-16.875f)<.01f,"Background world dimensions preserved");
            foreach(var kind in new[]{FastingTopDownHazard.Kind.Rock,FastingTopDownHazard.Kind.Wall,FastingTopDownHazard.Kind.Tomb,FastingTopDownHazard.Kind.Poison}){
                g.Begin();typeof(FastingTopDownGame).GetProperty("StageIndex").SetValue(g,2);foreach(var e in g.entities)e.gameObject.SetActive(false);
                typeof(FastingTopDownGame).GetMethod("SpawnAbove",flags).Invoke(g,new object[]{kind,0,1,0f,true,true,0,1});
                var h=g.Hazards.Single();var prefab=g.ruinsArt.PrefabFor(kind,null);ok(h.body.sprite==prefab.body.sprite,"Correct art for "+kind);ok(h.velocity==Vector2.zero&&!h.rotateRollingRock,"Fixed ruin obstacle "+kind);
                h.UpdateApproach(g.player.position);ok(Vector2.Distance(h.body.bounds.center,h.transform.position)<.05f,"Artwork and collider aligned "+kind);
                h.transform.position=g.player.position;h.warning=0;typeof(FastingEncounterDirector).GetField("nextRandom",flags).SetValue(g.encounterDirector,100f);typeof(FastingEncounterDirector).GetField("nextWave",flags).SetValue(g.encounterDirector,100f);g.Tick(.01f,Vector2.zero);ok(g.Health==4,"Collision damage "+kind);
                if(kind==FastingTopDownHazard.Kind.Tomb)ok(g.PrayerBlockRemaining>0,"Tomb blocks prayer");
                if(kind==FastingTopDownHazard.Kind.Poison)ok((float)typeof(FastingTopDownGame).GetField("reverse",flags).GetValue(g)>0,"Poison reverses controls");
            }
            g.Begin();foreach(var stage in new[]{0,2}){var e=g.entities.First(x=>x.stage==stage&&x.kind==FastingTopDownEntity.Kind.Water);var s=e.GetComponentsInChildren<SpriteRenderer>().First(r=>r.name=="Symbol");ok(UnityEditor.AssetDatabase.GetAssetPath(s.sprite).EndsWith(stage==0?"Oasis.png":"Well.png"),"Recovery art stage "+stage);e.SetUsed(true);ok(e.consumed&&s.color.a<1,"Used supply fades");e.SetUsed(false);ok(!e.consumed&&s.color==Color.white,"Supply resets");}
            return n+" checks passed: stage3 background and dimensions, all four spawned prefabs, alignment, collision, tomb/poison effects, oasis/well and used-state reset.";
        }finally{Time.timeScale=1;g.Begin();g.enabled=true;}
    }
}
