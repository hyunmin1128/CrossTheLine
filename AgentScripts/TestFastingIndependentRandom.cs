using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using CrossTheLine.Minigames;
public static class TestFastingIndependentRandom
{
    static void Field(object o,string n,object value)=>o.GetType().GetField(n,BindingFlags.NonPublic|BindingFlags.Instance).SetValue(o,value);
    static void Set(object o,string n,object value)=>o.GetType().GetProperty(n).SetValue(o,value);
    public static string Run()
    {
        var g=UnityEngine.Object.FindAnyObjectByType<FastingTopDownGame>();if(!Application.isPlaying)throw new Exception("Play minigame");g.enabled=false;var random=UnityEngine.Random.state;int n=0;Action<bool,string> ok=(b,s)=>{if(!b)throw new Exception(s);n++;};
        try
        {
            UnityEngine.Random.InitState(17112026);var types=new HashSet<FastingTopDownHazard.Kind>();var columns=new HashSet<int>();
            for(int stage=0;stage<4;stage++)for(int sample=0;sample<30;sample++)
            {
                g.Begin();foreach(var e in g.entities)e.gameObject.SetActive(false);Set(g,"StageIndex",stage);Set(g.encounterDirector,"OpenLane",2);Set(g.obstaclePatterns,"CurrentPattern",FastingObstaclePatterns.Pattern.Alternating);
                Field(g.encounterDirector,"nextWave",100f);Field(g.encounterDirector,"nextRandom",.001f);g.encounterDirector.Tick(.02f);
                ok(g.encounterDirector.RandomSpawnAttempts==1&&g.encounterDirector.PatternSpawnAttempts==0,"Random timer independent");ok(g.obstaclePatterns.WaveIndex==0,"Random spawn does not advance pattern");ok(g.Hazards.Count>0,"Independent obstacle created");
                foreach(var h in g.Hazards){types.Add(h.kind);columns.Add(Mathf.RoundToInt(h.transform.position.x));if(Mathf.Abs(h.velocity.x)<.01f)ok(g.encounterDirector.LeavesLaneOpen(h),"Random obstacle preserves escape space");}
                float interval=(float)g.encounterDirector.GetType().GetField("nextRandom",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(g.encounterDirector);float factor=stage==0?g.encounterDirector.desertSpawnIntervalMultiplier:1;ok(interval>=.85f*factor&&interval<=1.65f*factor,"Stage-specific random interval in configured range");
            }
            ok(types.Count>=10&&columns.Count>=5,"Varied types and columns across stages");
            g.Begin();foreach(var e in g.entities)e.gameObject.SetActive(false);Set(g.obstaclePatterns,"CurrentPattern",FastingObstaclePatterns.Pattern.Alternating);Field(g.encounterDirector,"nextWave",.001f);Field(g.encounterDirector,"nextRandom",100f);g.encounterDirector.Tick(.02f);ok(g.encounterDirector.PatternSpawnAttempts==1&&g.encounterDirector.RandomSpawnAttempts==0,"Pattern clock independent");
            int attempts=g.encounterDirector.SpawnAttempts;Set(g.encounterDirector,"CurrentPhase",FastingEncounterDirector.Phase.PrayerWindow);Field(g.encounterDirector,"remaining",100f);Field(g.encounterDirector,"nextRandom",0f);for(int i=0;i<10;i++)g.encounterDirector.Tick(.1f);ok(g.encounterDirector.SpawnAttempts==attempts,"No extra obstacles in prayer window");
            g.Begin();foreach(var e in g.entities)e.gameObject.SetActive(false);Set(g.encounterDirector,"OpenLane",2);g.SpawnPatternObstacle(FastingTopDownHazard.Kind.Rock,0,1,0);int count=g.Hazards.Count;g.SpawnPatternObstacle(FastingTopDownHazard.Kind.Rock,0,1,0);ok(count==1&&g.Hazards.Count==1,"Exact spawn overlap rejected");
            var fx=UnityEngine.Object.FindAnyObjectByType<FastingJourneyPresentation>();ok(!fx.transform.Find("DayAnnouncement"),"Date popup object absent");ok(g.heading.gameObject.activeInHierarchy&&g.heading.color==Color.black,"Existing black header kept");
            g.Begin();Set(g,"Elapsed",17f);fx.Refresh(.1f);ok(fx.atmosphere.color.b>fx.atmosphere.color.r,"Night ambience preserved");
            return n+" checks passed: independent random/pattern clocks, four-stage variety, corridor protection, spawn spacing, prayer window and no date popup.";
        }
        finally{UnityEngine.Random.state=random;Time.timeScale=1;g.Begin();g.enabled=true;}
    }
}
