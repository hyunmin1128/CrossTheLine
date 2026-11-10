using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using CrossTheLine.Minigames;
public static class TestFastingTopEntry
{
    static void Set(object g,string n,object v)=>g.GetType().GetField(n,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(g,v);
    public static async Task<string> Run()
    {
        var g=UnityEngine.Object.FindAnyObjectByType<FastingTopDownGame>();if(!g||!Application.isPlaying)throw new Exception("Play scene");int count=0;var log=new System.Text.StringBuilder();Action<bool,string> check=(ok,s)=>{if(!ok)throw new Exception(s);count++;log.AppendLine("PASS: "+s);};
        try{
            g.enabled=false;g.Begin();foreach(var e in g.entities)e.gameObject.SetActive(false);Set(g,"wave",float.MaxValue);Set(g,"grace",999f);var state=UnityEngine.Random.state;try{UnityEngine.Random.InitState(117);for(int i=0;i<40;i++)g.SpawnWave();}finally{UnityEngine.Random.state=state;}
            float top=g.gameCamera.transform.position.y+g.gameCamera.orthographicSize;
            check(g.Hazards.All(h=>h.body.bounds.min.y>top),"All obstacle artwork starts completely above visible screen");check(g.Hazards.All(h=>h.warning==0&&h.body.enabled),"No delayed pop-in from ground warning squares");
            check(g.Hazards.All(h=>!h.footprint&&!h.arrow&&!h.GetComponentsInChildren<SpriteRenderer>().Any(r=>r.name.StartsWith("HitAreaBorder"))),"Ground rectangles, arrows and borders removed from spawned hazards");check(!g.feet.gameObject.activeSelf&&g.laneGuides.All(r=>!r.gameObject.activeSelf),"Foot and lane debug boxes hidden");
            var sand=g.Hazards.Where(h=>h.kind==FastingTopDownHazard.Kind.Sand).ToArray();check(sand.All(h=>h.occupiedLanes==1||h.occupiedLanes==2)&&sand.Select(h=>h.occupiedLanes).Distinct().Count()==2,"Sand spawns both one-lane and adjacent two-lane patterns");check(sand.All(h=>h.velocity.x==0&&h.velocity.y<0),"Sand now moves top to bottom");check(sand.Where(h=>h.occupiedLanes==2).All(h=>h.GetComponentsInChildren<SpriteRenderer>().Length==2),"Two-lane sand uses two pixel clouds instead of stretched art");
            var rocks=g.Hazards.Where(h=>h.kind==FastingTopDownHazard.Kind.Rock).ToArray();check(rocks.All(h=>h.occupiedLanes==1&&h.velocity.x==0&&h.velocity.y<0),"Rocks occupy one lane and roll downward");var cactus=g.Hazards.First(h=>h.kind==FastingTopDownHazard.Kind.Cactus);float cactusY=cactus.transform.position.y,screenBefore=cactus.transform.position.y-g.gameCamera.transform.position.y;check(cactus.velocity==Vector2.zero,"Cactus is installed on the ground");
            for(int i=0;i<20;i++)g.Tick(.1f,Vector2.zero);check(Mathf.Abs(cactus.transform.position.y-cactusY)<.001f&&cactus.transform.position.y-g.gameCamera.transform.position.y<screenBefore,"Camera scrolling brings fixed cactus down into view");check(rocks.Any(h=>h&&h.transform.position.y<g.gameCamera.transform.position.y+g.gameCamera.orthographicSize),"Rolling rocks enter naturally from screen top");var rolling=rocks.First(h=>h);float angle=rolling.body.transform.localEulerAngles.z;rolling.Advance(.18f,new Vector2(100,100),new Vector2(100,100),out _);check(Mathf.Abs(Mathf.DeltaAngle(angle,rolling.body.transform.localEulerAngles.z))>1,"Rock sprite rotates as it rolls");
            g.Begin();foreach(var e in g.entities)e.gameObject.SetActive(false);Set(g,"wave",float.MaxValue);Set(g,"grace",999f);g.SpawnWave();for(int i=0;i<15;i++)g.Tick(.1f,Vector2.zero);g.SpawnWave();for(int i=0;i<15;i++)g.Tick(.1f,Vector2.zero);g.SpawnWave();for(int i=0;i<15;i++)g.Tick(.1f,Vector2.zero);
            Directory.CreateDirectory("Docs/QA/FastingTopDown");ScreenCapture.CaptureScreenshot(Path.GetFullPath("Docs/QA/FastingTopDown/TopEntry-NoIndicators.png"));await Task.Delay(300);File.WriteAllText("Docs/QA/FastingTopDown/TopEntryValidation.txt",log.ToString());return count+" checks passed";
        }finally{g.Begin();g.enabled=true;Time.timeScale=1;}
    }
}
