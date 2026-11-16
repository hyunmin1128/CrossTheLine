using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using CrossTheLine.Minigames;
public static class CaptureFastingJourneyUpdate
{
    public static async Task<string> Run()
    {
        var g=UnityEngine.Object.FindAnyObjectByType<FastingTopDownGame>();if(!Application.isPlaying)throw new Exception("Play required");
        string dir="Docs/QA/FastingTopDown/PatternsJourney-20261116";Directory.CreateDirectory(dir);var fx=UnityEngine.Object.FindAnyObjectByType<FastingJourneyPresentation>();
        string[] keys={"CrossTheLine.Fasting.BestDay","CrossTheLine.Fasting.PreviousDay"};bool[] exists=keys.Select(PlayerPrefs.HasKey).ToArray();int[] values=keys.Select(k=>PlayerPrefs.GetInt(k)).ToArray();g.enabled=false;Time.timeScale=0;
        try
        {
            g.Begin();foreach(var e in g.entities)e.gameObject.SetActive(false);fx.Refresh(.25f);Canvas.ForceUpdateCanvases();await Task.Delay(150);ScreenCapture.CaptureScreenshot(dir+"/DayBanner.png");await Task.Delay(200);
            typeof(FastingTopDownGame).GetProperty("Elapsed").SetValue(g,17f);fx.Refresh(3);Canvas.ForceUpdateCanvases();await Task.Delay(150);ScreenCapture.CaptureScreenshot(dir+"/Night.png");await Task.Delay(200);
            g.Begin();for(int i=0;i<18;i++)g.Tick(.1f,Vector2.zero,true);typeof(FastingTopDownGame).GetProperty("Prayer").SetValue(g,g.PrayerTarget*.65f);PlayerPrefs.SetInt(keys[1],3);
            foreach(var cause in new[]{"Sand","Cactus","Cactus"}){typeof(FastingTopDownGame).GetField("grace",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(g,0f);g.TryDamage(cause);}
            g.hitVignette.Clear();g.Refresh();fx.Refresh(0);Canvas.ForceUpdateCanvases();await Task.Delay(150);ScreenCapture.CaptureScreenshot(dir+"/Failure.png");await Task.Delay(200);
            return "Captured day, night and actual failure result: "+dir;
        }
        finally{for(int i=0;i<keys.Length;i++){if(exists[i])PlayerPrefs.SetInt(keys[i],values[i]);else PlayerPrefs.DeleteKey(keys[i]);}PlayerPrefs.Save();Time.timeScale=1;g.Begin();g.enabled=true;}
    }
}
