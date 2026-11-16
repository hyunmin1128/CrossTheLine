using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using CrossTheLine.Minigames;
public static class CaptureFastingObstaclePatterns
{
    public static async Task<string> Run()
    {
        var g=UnityEngine.Object.FindAnyObjectByType<FastingTopDownGame>();if(!Application.isPlaying)throw new Exception("Play required");g.enabled=false;Time.timeScale=0;var patterns=g.obstaclePatterns;var random=UnityEngine.Random.state;
        const string dir="Docs/QA/FastingTopDown/PatternsJourney-20261116";Directory.CreateDirectory(dir);
        try
        {
            foreach(var p in new[]{FastingObstaclePatterns.Pattern.Alternating,FastingObstaclePatterns.Pattern.StaggeredPair,FastingObstaclePatterns.Pattern.Passage,FastingObstaclePatterns.Pattern.SandBands})
            {
                UnityEngine.Random.InitState(20261116);g.Begin();foreach(var e in g.entities)e.gameObject.SetActive(false);typeof(FastingEncounterDirector).GetProperty("OpenLane").SetValue(g.encounterDirector,2);typeof(FastingObstaclePatterns).GetProperty("CurrentPattern").SetValue(patterns,p);typeof(FastingObstaclePatterns).GetProperty("WaveIndex").SetValue(patterns,0);
                for(int i=0;i<72;i++)g.Tick(.05f,Vector2.zero);
                UnityEngine.Object.FindAnyObjectByType<FastingJourneyPresentation>().Refresh(3);Canvas.ForceUpdateCanvases();await Task.Delay(180);ScreenCapture.CaptureScreenshot(dir+"/Pattern-"+p+".png");await Task.Delay(220);
            }
            return "Captured four patterns after 3.6 seconds of normal encounter ticks.";
        }
        finally{UnityEngine.Random.state=random;Time.timeScale=1;g.Begin();g.enabled=true;}
    }
}
