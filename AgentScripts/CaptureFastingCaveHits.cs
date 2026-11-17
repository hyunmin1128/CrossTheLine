using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using CrossTheLine.Minigames;
public static class CaptureFastingCaveHits
{
    static void StartHit(FastingTopDownGame g,FastingTopDownHazard.Kind kind)
    {
        g.Begin();typeof(FastingTopDownGame).GetProperty("StageIndex").SetValue(g,1);typeof(FastingTopDownGame).GetMethod("ApplyStage",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(g,new object[]{false});foreach(var e in g.entities)e.gameObject.SetActive(false);
        foreach(var field in new[]{"nextWave","nextRandom","remaining"})typeof(FastingEncounterDirector).GetField(field,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(g.encounterDirector,100f);
        var h=UnityEngine.Object.Instantiate(g.caveArt.PrefabFor(kind,null),g.hazardRoot);h.warning=0;h.ConfigureLanes(1,3);h.transform.position=g.player.position;((List<FastingTopDownHazard>)g.Hazards).Add(h);g.Tick(.01f,Vector2.zero);
    }
    public static async Task<string> Run()
    {
        if(!Application.isPlaying)throw new Exception("Play required");var g=UnityEngine.Object.FindAnyObjectByType<FastingTopDownGame>();g.enabled=false;Time.timeScale=0;const string dir="Docs/QA/FastingTopDown/CaveHits-20261117";Directory.CreateDirectory(dir);
        try
        {
            StartHit(g,FastingTopDownHazard.Kind.Bat);for(int i=0;i<5;i++){g.Tick(.1f,Vector2.zero);g.hitFeedback.Advance(.1f);g.hitVignette.Advance(.1f);g.screenHitFeedback.Advance(.1f);}Canvas.ForceUpdateCanvases();await Task.Delay(150);ScreenCapture.CaptureScreenshot(dir+"/Bat-Airborne.png");await Task.Delay(200);
            StartHit(g,FastingTopDownHazard.Kind.Crack);for(int i=0;i<4;i++){g.Tick(.1f,Vector2.zero);g.hitFeedback.Advance(.1f);g.hitVignette.Advance(.1f);g.screenHitFeedback.Advance(.1f);}Canvas.ForceUpdateCanvases();await Task.Delay(150);ScreenCapture.CaptureScreenshot(dir+"/Crack-Hidden.png");await Task.Delay(200);
            for(int i=0;i<7;i++){g.Tick(.1f,Vector2.zero);g.hitFeedback.Advance(.1f);g.hitVignette.Advance(.1f);g.screenHitFeedback.Advance(.1f);}Canvas.ForceUpdateCanvases();await Task.Delay(150);ScreenCapture.CaptureScreenshot(dir+"/Crack-Return.png");await Task.Delay(200);
            return "Captured actual collision states: bat air/vision, crack disappearance and protected return.";
        }
        finally{Time.timeScale=1;g.Begin();g.enabled=true;}
    }
}
