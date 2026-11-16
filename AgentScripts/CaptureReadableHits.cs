using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using CrossTheLine.Minigames;
public static class CaptureReadableHits
{
    public static async Task<string> Run()
    {
        var g=UnityEngine.Object.FindAnyObjectByType<FastingTopDownGame>();if(!Application.isPlaying)throw new Exception("Play required");
        float time=Time.timeScale;g.enabled=false;Time.timeScale=0;
        string dir="Docs/QA/FastingTopDown/ReadableHits-20261116";Directory.CreateDirectory(dir);
        var apply=typeof(FastingTopDownGame).GetMethod("ApplyEffect",BindingFlags.NonPublic|BindingFlags.Instance);
        try
        {
            foreach(var kind in new[]{FastingTopDownHazard.Kind.Rock,FastingTopDownHazard.Kind.Cactus,FastingTopDownHazard.Kind.Sand,FastingTopDownHazard.Kind.Quicksand})
            {
                g.Begin();foreach(var e in g.entities)e.gameObject.SetActive(false);g.TryDamage(kind.ToString());apply.Invoke(g,new object[]{kind});
                g.hitVignette.Advance(.16f);g.screenHitFeedback.Advance(.16f);g.hitFeedback.Advance(.16f);g.Refresh();Canvas.ForceUpdateCanvases();
                await Task.Delay(200);ScreenCapture.CaptureScreenshot(dir+"/"+kind+"-Combined.png");await Task.Delay(250);
            }
            return dir;
        }
        finally{Time.timeScale=time;g.Begin();g.enabled=true;}
    }
}
