using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using CrossTheLine.Minigames;
public static class TestFastingRuinsFormations
{
    public static string Run(){
        if(!Application.isPlaying)throw new Exception("Play required");var g=UnityEngine.Object.FindAnyObjectByType<FastingTopDownGame>();g.enabled=false;var flags=BindingFlags.Instance|BindingFlags.NonPublic;int n=0;Action<bool,string> ok=(b,s)=>{if(!b)throw new Exception(s);n++;};var rng=UnityEngine.Random.state;
        Action reset=()=>{g.Begin();typeof(FastingTopDownGame).GetProperty("StageIndex").SetValue(g,2);foreach(var e in g.entities)e.gameObject.SetActive(false);typeof(FastingEncounterDirector).GetField("nextRandom",flags).SetValue(g.encounterDirector,100f);typeof(FastingEncounterDirector).GetField("nextWave",flags).SetValue(g.encounterDirector,100f);};
        try{
            UnityEngine.Random.InitState(1811);var counts=new System.Collections.Generic.HashSet<int>();bool horizontal=false,vertical=false;
            for(int i=0;i<60;i++)foreach(var kind in new[]{FastingTopDownHazard.Kind.Tomb,FastingTopDownHazard.Kind.Wall}){reset();g.SpawnPatternObstacle(kind,0,1,0);ok(g.Hazards.Count>=2&&g.Hazards.Count<=4,"Group size");if(kind==FastingTopDownHazard.Kind.Tomb)counts.Add(g.Hazards.Count);else{horizontal|=g.Hazards.Select(h=>h.transform.position.x).Distinct().Count()>1;vertical|=g.Hazards.Select(h=>h.transform.position.x).Distinct().Count()==1;}}
            ok(counts.Count==3,"Tomb2/3/4 all appear");ok(horizontal&&vertical,"Horizontal and vertical walls");
            reset();var h=UnityEngine.Object.Instantiate(g.ruinsArt.poisonWell,g.hazardRoot);h.ConfigureLanes(1,3);h.transform.position=g.player.position;((System.Collections.Generic.List<FastingTopDownHazard>)g.Hazards).Add(h);g.Tick(.01f,Vector2.zero);ok(g.Health==3,"Poison damage2");ok((float)typeof(FastingTopDownGame).GetField("reverse",flags).GetValue(g)==3,"Reverse3seconds");g.UseWater();ok((float)typeof(FastingTopDownGame).GetField("reverse",flags).GetValue(g)==0,"Water cleanses");
            reset();h=UnityEngine.Object.Instantiate(g.ruinsArt.tomb,g.hazardRoot);h.ConfigureLanes(1,3);h.transform.position=g.player.position;((System.Collections.Generic.List<FastingTopDownHazard>)g.Hazards).Add(h);g.Tick(.01f,Vector2.zero);ok(g.PrayerBlockRemaining==3,"Tomb blocks3seconds");g.Tick(.1f,Vector2.zero,true);ok(!g.Praying,"Cannot pray while blocked");
            reset();g.UseWater();var supply=g.entities.First(e=>e.stage==2&&e.kind==FastingTopDownEntity.Kind.Water);supply.gameObject.SetActive(true);supply.transform.position=g.player.position;g.Tick(.01f,Vector2.zero);ok(g.HasWater&&supply.consumed,"Stage3 well refills flask once");
            return n+" checks passed: tomb counts, wall layouts, poison damage/reversal, cleansing, prayer block, well refill.";
        }finally{UnityEngine.Random.state=rng;Time.timeScale=1;g.Begin();g.enabled=true;}
    }
}
