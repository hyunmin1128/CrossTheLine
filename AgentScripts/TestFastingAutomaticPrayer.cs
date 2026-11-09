using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using CrossTheLine.Minigames;
public static class TestFastingAutomaticPrayer
{
    static void Set(object g,string n,object v){var t=g.GetType();var f=t.GetField(n,BindingFlags.Instance|BindingFlags.NonPublic);if(f!=null)f.SetValue(g,v);else t.GetProperty(n).GetSetMethod(true).Invoke(g,new[]{v});}
    public static async Task<string> Run(){var g=UnityEngine.Object.FindAnyObjectByType<FastingTopDownGame>();if(!g||!Application.isPlaying)throw new Exception("Play scene");int count=0;var report=new System.Text.StringBuilder();Action<bool,string> check=(ok,s)=>{if(!ok)throw new Exception(s);count++;report.AppendLine("PASS: "+s);};
        try{
            g.enabled=false;g.Begin();foreach(var e in g.entities)e.gameObject.SetActive(false);Set(g,"wave",float.MaxValue);for(int i=0;i<20;i++)g.Tick(.05f,Vector2.zero);check(Mathf.Abs(g.Prayer-1)<.001f&&g.Praying,"Prayer charges one per second with no prayer input");check(g.player.position.y>0,"Automatic prayer preserves automatic forward travel");float prayer=g.Prayer;g.Tick(.05f,Vector2.right);check(g.player.position.x==3&&g.Prayer>prayer,"Lane dodge does not interrupt automatic prayer");
            Set(g,"prayerLock",1f);prayer=g.Prayer;for(int i=0;i<10;i++)g.Tick(.05f,Vector2.zero);check(g.Prayer==prayer&&!g.Praying,"Prayer debuff suspends charge");check(g.status.text.Contains("기도 방해")&&g.status.text.Contains("초"),"HUD shows remaining prayer interference time");for(int i=0;i<12;i++)g.Tick(.05f,Vector2.zero);check(g.Prayer>prayer&&g.Praying,"Charge resumes automatically when interference expires");
            Set(g,"prayerLock",2f);g.UseWater();g.Tick(.05f,Vector2.zero);check(g.Praying&&g.PrayerBlockRemaining==0,"Water purifies interference and automatic prayer resumes");
            var flame=g.entities.First(e=>e.kind==FastingTopDownEntity.Kind.Flame);g.Collect(flame);prayer=g.Prayer;g.Tick(.1f,Vector2.zero);check(Mathf.Abs(g.Prayer-prayer-.15f)<.001f,"Flame boosts automatic prayer to 1.5 per second");
            var book=g.entities.First(e=>e.kind==FastingTopDownEntity.Kind.Book);g.Collect(book);g.Tick(.1f,Vector2.zero);check(g.Prayer==g.PrayerTarget&&!g.Praying&&g.GateVisible,"Automatic prayer caps at target and finish line appears");check(!g.introduction.GetComponentsInChildren<TMPro.TMP_Text>(true).Any(t=>t.text.Contains("Space")),"Start guide no longer requests Space");
            g.Begin();foreach(var e in g.entities)e.gameObject.SetActive(false);Set(g,"wave",float.MaxValue);g.enabled=true;var until=DateTime.UtcNow.AddSeconds(.45);while(DateTime.UtcNow<until)await Task.Delay(16);check(g.Prayer>0&&g.player.position.y>0,"Actual running frames charge prayer without keyboard input");g.enabled=false;g.Refresh();Directory.CreateDirectory("Docs/QA/FastingTopDown");ScreenCapture.CaptureScreenshot(Path.GetFullPath("Docs/QA/FastingTopDown/AutomaticPrayer.png"));await Task.Delay(250);File.WriteAllText("Docs/QA/FastingTopDown/AutomaticPrayerValidation.txt",report.ToString());return count+" checks passed";
        }finally{g.Begin();g.enabled=true;Time.timeScale=1;}}
}
