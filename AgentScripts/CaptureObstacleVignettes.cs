using System;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using CrossTheLine.Minigames;
public static class CaptureObstacleVignettes
{
    public static async Task<string> Run()
    {
        var g=UnityEngine.Object.FindAnyObjectByType<FastingTopDownGame>();if(!Application.isPlaying)throw new Exception("Play scene");float time=Time.timeScale;g.enabled=false;
        string dir="Docs/QA/FastingTopDown/ObstacleVignettes-20261115";Directory.CreateDirectory(dir);
        try
        {
            Time.timeScale=0;
            foreach(FastingTopDownHazard.Kind kind in Enum.GetValues(typeof(FastingTopDownHazard.Kind)))
            {
                g.Begin();foreach(var e in g.entities)e.gameObject.SetActive(false);g.TryDamage(kind.ToString());g.hitVignette.Advance(.16f);g.Refresh();Canvas.ForceUpdateCanvases();await Task.Delay(250);ScreenCapture.CaptureScreenshot(dir+"/"+kind+".png");await Task.Delay(300);
            }
            return "Captured fourteen actual obstacle vignette states: "+dir;
        }
        finally{Time.timeScale=time;g.Begin();g.enabled=true;}
    }
}
