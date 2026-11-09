using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using CrossTheLine.Minigames;
public static class TestFastingEndlessRoute
{
    static void Set(object target,string name,object value){var t=target.GetType();var f=t.GetField(name,BindingFlags.Instance|BindingFlags.NonPublic);if(f!=null)f.SetValue(target,value);else t.GetProperty(name).GetSetMethod(true).Invoke(target,new[]{value});}
    public static async Task<string> Run()
    {
        var g=UnityEngine.Object.FindAnyObjectByType<FastingTopDownGame>();if(!Application.isPlaying||!g)throw new Exception("Play scene first");int count=0;var log=new System.Text.StringBuilder();Action<bool,string> check=(ok,msg)=>{if(!ok)throw new Exception(msg);count++;log.AppendLine("PASS: "+msg);};
        try{
            g.enabled=false;g.Begin();check(Mathf.Abs(g.settings.moveSpeed-2.5f)<.001f,"Saved speed reduced from 4.5 to 2.5");check(!g.GateVisible&&!g.gate.gameObject.activeSelf,"No finish line before prayer completion");
            g.Tick(.1f,Vector2.zero,false);check(Mathf.Abs(g.player.position.y-.25f)<.001f,"Actual automatic travel uses slower speed");
            g.player.position=new Vector3(3,24);g.Tick(.1f,Vector2.zero,false);check(g.player.position.y>24&&g.StageIndex==0&&!g.GateVisible,"Route continues past former fixed endpoint without enough prayer");
            g.player.position=new Vector3(3,120);g.Tick(.1f,Vector2.zero,false);g.Tick(.1f,Vector2.zero,false);check(g.StageIndex==0&&!g.GateVisible,"Insufficient prayer still does not end a long run");check(g.backgrounds.Any(b=>b.bounds.min.y<=g.gameCamera.transform.position.y&&b.bounds.max.y>=g.gameCamera.transform.position.y),"Recycled backgrounds cover camera far beyond original map");check(g.entities.Any(e=>e.stage==0&&e.transform.position.y>120),"Shelters and pickup candidates continue ahead");check(g.laneGuides.All(l=>Mathf.Abs(l.transform.position.y-g.gameCamera.transform.position.y)<.01f),"Five-lane guides follow long route");
            var book=g.entities.First(e=>e.kind==FastingTopDownEntity.Kind.Book);float here=g.player.position.y;g.Collect(book);float line=g.gate.position.y;check(g.GateVisible&&g.gate.gameObject.activeSelf,"Bible completion reveals finish line immediately");check(Mathf.Abs(line-here-g.settings.finishLineDistance)<.001f,"Line spawns six units ahead of current position");g.Tick(.1f,Vector2.zero,false);check(g.StageIndex==0&&g.gate.position.y==line,"Line stays fixed and does not instantly change stages");
            g.player.position=new Vector3(3,line);g.Tick(.01f,Vector2.zero,false);for(int i=0;i<14;i++)g.Tick(.05f,Vector2.zero,false);check(g.StageIndex==1&&g.Prayer==0&&!g.GateVisible&&!g.gate.gameObject.activeSelf,"Crossing line advances stage and hides next line until prayer");
            g.Begin();Set(g,"wave",float.MaxValue);foreach(var e in g.entities)e.gameObject.SetActive(false);for(int i=0;i<Mathf.CeilToInt(g.PrayerTarget/g.settings.prayerPerSecond/.05f)+2;i++)g.Tick(.05f,Vector2.zero,true);check(g.GateVisible&&g.Prayer==g.PrayerTarget,"Channelled prayer also reveals line at target");
            g.Begin();check(!g.GateVisible&&g.backgrounds[0].transform.position.y==-2,"Restart restores map origins and hides finish line");
            for(int stage=0;stage<4;stage++){g.Collect(book);g.player.position=new Vector3(3,g.gate.position.y);g.Tick(.01f,Vector2.zero,false);for(int i=0;i<14;i++)g.Tick(.05f,Vector2.zero,false);}check(g.Complete&&!g.Running,"All four conditional finish lines lead to forty-day completion");
            g.Begin();g.player.position=new Vector3(0,80);g.Tick(.01f,Vector2.zero,false);g.Tick(.01f,Vector2.zero,false);g.Collect(book);g.Refresh();Directory.CreateDirectory("Docs/QA/FastingTopDown");ScreenCapture.CaptureScreenshot(Path.GetFullPath("Docs/QA/FastingTopDown/PrayerFinishLine.png"));await Task.Delay(300);File.WriteAllText("Docs/QA/FastingTopDown/EndlessRouteValidation.txt",log.ToString());return count+" checks passed";
        }finally{g.Begin();g.enabled=true;Time.timeScale=1;}
    }
}
