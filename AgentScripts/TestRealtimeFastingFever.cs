using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using CrossTheLine.Minigames;
public static class TestRealtimeFastingFever
{
    static void Set(FastingTopDownGame g,string name,object value)=>typeof(FastingTopDownGame).GetField(name,BindingFlags.NonPublic|BindingFlags.Instance).SetValue(g,value);
    static void Start(FastingTopDownGame g)
    {
        g.Begin();foreach(var e in g.entities)e.gameObject.SetActive(false);foreach(var f in new[]{"nextWave","nextRandom","remaining"})typeof(FastingEncounterDirector).GetField(f,BindingFlags.NonPublic|BindingFlags.Instance).SetValue(g.encounterDirector,100f);
    }
    static void Flame(FastingTopDownGame g,int count){var e=g.entities.First(x=>x.kind==FastingTopDownEntity.Kind.Flame);for(int i=0;i<count;i++)g.Collect(e);}
    public static string Run()
    {
        var g=UnityEngine.Object.FindAnyObjectByType<FastingTopDownGame>();if(!Application.isPlaying)throw new Exception("Play required");g.enabled=false;int n=0;Action<bool,string> ok=(b,m)=>{if(!b)throw new Exception(m);n++;};
        try
        {
            Start(g);Flame(g,3);ok(g.FeverActive&&g.FireGauge==1&&g.FeverRemaining==6,"Fever begins full at six seconds");g.Tick(.1f,Vector2.zero);ok(Mathf.Abs(g.FeverRemaining-5.9f)<.001f&&g.FireGauge<1,"Immediate idle drain");ok(g.Prayer==0,"Idle grants no prayer");
            for(int i=0;i<9;i++)g.Tick(.1f,Vector2.zero);ok(Mathf.Abs(g.FeverRemaining-5)<.001f,"One idle second consumes one second");float time=g.FeverRemaining;Flame(g,1);ok(g.FeverRemaining==time,"Extra flame does not extend active fever");
            Start(g);Flame(g,3);g.Tick(.1f,Vector2.zero,true);ok(g.PrayerPreparing&&g.Prayer==0&&g.FeverRemaining<6,"Preparation consumes fever time");
            for(int i=0;i<12;i++)g.Tick(.1f,Vector2.zero,true);float activePrayer=g.Prayer;ok(Mathf.Abs(g.FeverRemaining-4.7f)<.002f,"Praying drains at same real-time rate");
            Start(g);Flame(g,1);for(int i=0;i<13;i++)g.Tick(.1f,Vector2.zero,true);ok(Mathf.Abs(activePrayer-g.Prayer*2)<.002f,"Fever prayer bonus unchanged");
            Start(g);Flame(g,3);Set(g,"root",1f);g.Tick(.1f,Vector2.zero,true);ok(!g.Praying&&g.FeverRemaining<6,"Immobilized prayer lock does not freeze timer");
            Start(g);Flame(g,3);Set(g,"swap",.5f);g.Tick(.1f,Vector2.zero);ok(g.FeverRemaining<6,"Stage transition does not bank fever");
            Start(g);Flame(g,3);typeof(FastingTopDownGame).GetProperty("Prayer").SetValue(g,g.PrayerTarget);g.Tick(.1f,Vector2.zero);ok(g.FeverRemaining<6,"Full prayer gauge does not freeze fever");
            Start(g);Flame(g,3);for(int i=0;i<61;i++)g.Tick(.1f,Vector2.zero);ok(!g.FeverActive&&g.FeverRemaining==0&&g.FlameStacks==0&&g.FireGauge==0,"Unused fever expires and empties gauge");
            var ui=UnityEngine.Object.FindAnyObjectByType<FastingPortraitHUD>();ui.Refresh();ok(!ui.feverFlame.gameObject.activeSelf&&ui.ring.fill==0,"HUD exits fever at zero");
            Start(g);Flame(g,3);Set(g,"fever",.05f);Set(g,"prayerHeldFor",1f);g.Tick(.1f,Vector2.zero,true);ok(Mathf.Abs(g.Prayer-.225f)<.001f,"Last fraction of a frame gets only remaining fever bonus");
            Start(g);Flame(g,3);g.Tick(0,Vector2.zero);ok(g.FeverRemaining==6,"Paused game time does not consume fever");g.Begin();ok(!g.FeverActive&&g.FireGauge==0,"Retry resets fever");
            ok(!g.introduction.GetComponentsInChildren<TMPro.TMP_Text>(true).Any(t=>t.text.Contains("기도할 때만 시간 소모")),"Guide updated");
            return n+" checks passed: real-time drain idle/praying/preparing/locked/transition, unchanged bonus, expiry, HUD and retry.";
        }
        finally{Time.timeScale=1;g.Begin();g.enabled=true;}
    }
    public static async Task<string> Live()
    {
        if(!Application.isPlaying)throw new Exception("Play required");var g=UnityEngine.Object.FindAnyObjectByType<FastingTopDownGame>();
        try
        {
            Start(g);g.enabled=true;Time.timeScale=1;Flame(g,3);float before=g.FeverRemaining;await Task.Delay(1200);
            var ui=UnityEngine.Object.FindAnyObjectByType<FastingPortraitHUD>();
            if(g.Praying||g.Prayer!=0||g.FeverRemaining>=before-.5f||ui.ring.fill>=.95f)throw new Exception("Live idle fever did not drain on the HUD");
            return "Live idle fever decreased from "+before+" to "+g.FeverRemaining.ToString("F2")+" seconds; portrait ring="+ui.ring.fill.ToString("F2")+" without prayer.";
        }
        finally{g.Begin();g.enabled=true;Time.timeScale=1;}
    }
}
