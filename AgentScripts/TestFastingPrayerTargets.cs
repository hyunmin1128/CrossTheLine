using System;
using System.Linq;
using System.IO;
using System.Reflection;
using UnityEngine;
using CrossTheLine.Minigames;
public static class TestFastingPrayerTargets
{
    static void Set(object o,string n,object v){var t=o.GetType();var f=t.GetField(n,BindingFlags.Instance|BindingFlags.NonPublic);if(f!=null)f.SetValue(o,v);else t.GetProperty(n).GetSetMethod(true).Invoke(o,new[]{v});}
    public static string Run()
    {
        var g=UnityEngine.Object.FindAnyObjectByType<FastingTopDownGame>();if(!g||!Application.isPlaying)throw new Exception("Play top-down scene");int count=0;var report=new System.Text.StringBuilder();Action<bool,string> check=(ok,s)=>{if(!ok)throw new Exception(s);count++;report.AppendLine("PASS: "+s);};float[] expected={200,250,300,350};
        try{
            g.enabled=false;var page=g.entities.First(e=>e.kind==FastingTopDownEntity.Kind.Page);var fragment=g.entities.First(e=>e.kind==FastingTopDownEntity.Kind.Fragment);var book=g.entities.First(e=>e.kind==FastingTopDownEntity.Kind.Book);
            for(int stage=0;stage<4;stage++){
                g.Begin();Set(g,"StageIndex",stage);foreach(var e in g.entities)e.gameObject.SetActive(false);Set(g,"wave",float.MaxValue);
                check(g.PrayerTarget==expected[stage],"Stage "+(stage+1)+" target = "+expected[stage]);
                for(int n=0;n<20;n++)g.Tick(.05f,Vector2.zero,true);check(Mathf.Abs(g.Prayer-g.settings.prayerPerSecond)<.01f,"One second retains base prayer rate in stage "+(stage+1));
                g.Refresh();check(g.status.text.Contains("/"+expected[stage].ToString("0")),"HUD shows correct stage denominator");
                Set(g,"Prayer",0f);g.Collect(fragment);check(Mathf.Abs(g.Prayer-expected[stage]*g.settings.ScriptureFraction(stage,false))<.01f,"Fragment matches stage fraction");g.Collect(page);check(Mathf.Abs(g.Prayer-expected[stage]*(g.settings.ScriptureFraction(stage,false)+g.settings.ScriptureFraction(stage,true)))<.01f,"Page adds stage fraction");g.Collect(book);check(g.Prayer==expected[stage]&&g.GateVisible,"Complete Bible caps at current stage target and reveals line");g.Collect(fragment);check(g.Prayer==expected[stage],"No prayer overflow");
                report.AppendLine("BASE SECONDS: "+(expected[stage]/g.settings.prayerPerSecond).ToString("0.0"));
            }
            g.Begin();Set(g,"wave",float.MaxValue);foreach(var e in g.entities)e.gameObject.SetActive(false);for(int n=0;n<Mathf.CeilToInt(g.PrayerTarget/g.settings.prayerPerSecond/.05f)+2;n++)g.Tick(.05f,Vector2.zero,true);check(g.Prayer==200&&g.GateVisible,"Desert fills after configured unbuffed prayer duration");
            g.Begin();for(int stage=0;stage<4;stage++){check(g.PrayerTarget==expected[stage],"Actual transition selects next prayer goal");g.Collect(book);g.player.position=new Vector3(3,g.gate.position.y);g.Tick(.01f,Vector2.zero,false);for(int n=0;n<14;n++)g.Tick(.05f,Vector2.zero,false);}check(g.Complete,"Four scaled goals still complete forty days");
            Directory.CreateDirectory("Docs/QA/FastingTopDown");File.WriteAllText("Docs/QA/FastingTopDown/PrayerTargetsValidation.txt",report.ToString());return count+" checks passed";
        }finally{g.Begin();g.enabled=true;Time.timeScale=1;}
    }
}
