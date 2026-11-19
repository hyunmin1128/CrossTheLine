using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using CrossTheLine.Minigames;
public static class TestFastingTrialCombat
{
    static readonly BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
    static void Quiet(FastingTopDownGame g){foreach(var e in g.entities)e.gameObject.SetActive(false);foreach(var name in new[]{"nextWave","nextRandom","remaining"})typeof(FastingEncounterDirector).GetField(name,Flags).SetValue(g.encounterDirector,100f);}
    static void Reset(FastingTopDownGame g){g.BeginStageForTesting(3);Quiet(g);}
    static void Hit(FastingTopDownGame g,FastingTopDownHazard.Kind kind){var h=UnityEngine.Object.Instantiate(g.trialArt.PrefabFor(kind,null),g.hazardRoot);h.ConfigureLanes(1,3);h.warning=0;h.velocity=Vector2.zero;h.transform.position=g.player.position;((List<FastingTopDownHazard>)g.Hazards).Add(h);g.Tick(.01f,Vector2.zero);}
    public static string Run(){
        if(!Application.isPlaying)throw new Exception("Play required");var g=UnityEngine.Object.FindAnyObjectByType<FastingTopDownGame>();g.enabled=false;int n=0;Action<bool,string> ok=(b,s)=>{if(!b)throw new Exception(s);n++;};var rng=UnityEngine.Random.state;
        try{
            UnityEngine.Random.InitState(19112026);var thornCounts=new HashSet<int>();var pillarCounts=new HashSet<int>();
            for(int i=0;i<80;i++)foreach(var kind in new[]{FastingTopDownHazard.Kind.Thorns,FastingTopDownHazard.Kind.Pillar}){
                Reset(g);typeof(FastingEncounterDirector).GetProperty("OpenLane").SetValue(g.encounterDirector,0);g.SpawnPatternObstacle(kind,1,1,0);int count=g.Hazards.Count;
                if(kind==FastingTopDownHazard.Kind.Thorns){thornCounts.Add(count);ok(count>=1&&count<=3,"Thorn count1-3");ok(g.Hazards.Select(h=>h.transform.position.y).Distinct().Count()==1,"Thorns horizontal");}
                else{pillarCounts.Add(count);ok(count==2||count==4,"Pillar count2/4");ok(g.Hazards.Select(h=>h.transform.position.x).Distinct().Count()==2,"Pillar width2");ok(g.Hazards.Select(h=>h.transform.position.y).Distinct().Count()==count/2,"Square or horizontal");}
                foreach(var h in g.Hazards){ok(g.encounterDirector.LeavesLaneOpen(h),"Escape lane preserved");ok(h.fixedApproachScale,"Stable area scale");}
            }
            ok(thornCounts.SetEquals(new[]{1,2,3}),"All thorn variants appear");ok(pillarCounts.SetEquals(new[]{2,4}),"All pillar variants appear");
            Reset(g);Hit(g,FastingTopDownHazard.Kind.Thorns);ok(g.Health==4,"Thorn damage1");float x=g.player.position.x;g.Tick(.1f,Vector2.right);ok(Mathf.Abs(g.player.position.x-x-g.settings.manualMoveSpeed*.75f*.1f)<.001f,"Thorn slow25percent");
            Reset(g);var pose=g.playerBody.transform.localPosition;Hit(g,FastingTopDownHazard.Kind.Pillar);var area=g.playArea.RootArea;ok(g.Health==3,"Pillar damage2");ok(g.PrayerBlockRemaining>=1.5f&&g.Invulnerable,"Launch protected");g.hitFeedback.Advance(.75f);ok(g.playerBody.transform.localPosition.y>pose.y+2.8f,"High pillar lift");ok(g.playArea.RootArea==area,"Lift keeps logical movement area stable");
            g.hitFeedback.Advance(.8f);ok(g.playerBody.transform.localPosition==pose,"Landing restores artwork");ok(!g.TryDamage("Rock")&&g.Health==3,"No repeat damage during protection");
            for(int i=0;i<20;i++)g.Tick(.1f,Vector2.zero,true);ok(!g.Praying,"Burn blocks prayer");for(int i=0;i<12;i++)g.Tick(.1f,Vector2.zero,false);ok(!g.Invulnerable,"Protection expires");
            Reset(g);Hit(g,FastingTopDownHazard.Kind.Fire);ok(g.Health==4,"Fire damage1");g.Tick(.1f,Vector2.zero,true);ok(!g.Praying&&g.PrayerBlockRemaining>2.8f,"Fire burns for3seconds");
            Reset(g);g.SpawnPatternObstacle(FastingTopDownHazard.Kind.Reaper,0,1,0);ok(g.Hazards.Count==1,"One reaper");var reaper=g.Hazards[0];ok(reaper.occupiedLanes==4&&Mathf.Abs(reaper.size.x-12)<.001f,"Four-lane sweep geometry");ok(reaper.GetComponent<FastingReaperSweep>(),"Scythe trail component");ok(g.encounterDirector.LeavesLaneOpen(reaper),"One safe lane");int escape=g.encounterDirector.OpenLane;g.SpawnPatternObstacle(FastingTopDownHazard.Kind.Reaper,0,1,0);ok(g.Hazards.Count==1,"No overlapping reaper attack");
            reaper.spent=true;g.SpawnPatternObstacle(FastingTopDownHazard.Kind.Reaper,0,1,0);ok(g.encounterDirector.OpenLane!=escape,"Next sweep changes position");
            return n+" checks passed: formation variants, safe lane, actual collision damage/slow/burn, lift/landing/protection, single four-lane reaper and alternating attack.";
        }finally{UnityEngine.Random.state=rng;Time.timeScale=1;g.BeginStageForTesting(0);g.enabled=true;}
    }
}
