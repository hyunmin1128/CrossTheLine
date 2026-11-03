using System;using System.IO;using System.Reflection;using System.Threading.Tasks;using UnityEngine;using CrossTheLine;
public static class WholeGameScreenshots{
 static readonly BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;static void Call(JourneyGame g,string n,params object[] a)=>typeof(JourneyGame).GetMethod(n,F).Invoke(g,a);
 public static Task<string> Run()=>RunRange(0,27);
 public static async Task<string> RunRange(int start,int end){var g=UnityEngine.Object.FindAnyObjectByType<JourneyGame>();var w=g.GetComponent<JourneyContinuousWorld>();bool saving=JourneySceneFlow.TestWithoutSaving;float speed=Time.timeScale;try{JourneySceneFlow.TestWithoutSaving=true;Time.timeScale=0;Directory.CreateDirectory("Docs/QA/WholeGame/Screenshots");
  for(int chapter=start;chapter<end;chapter++){w.Normalize();Call(g,"ClearStoryPanels");int step=chapter==16?1:0;typeof(JourneyGame).GetField("save",F).SetValue(g,new JourneySave{chapter=chapter,step=step,unlocked=26});typeof(JourneyGame).GetField("started",F).SetValue(g,true);Call(g,"CloseMenu");Call(g,"LoadWorld",false);Time.timeScale=1;float settle=Time.realtimeSinceStartup+1.25f;while(Time.realtimeSinceStartup<settle)await Task.Yield();Time.timeScale=0;int f=Time.frameCount;while(Time.frameCount<f+2)await Task.Yield();ScreenCapture.CaptureScreenshot(Path.GetFullPath("Docs/QA/WholeGame/Screenshots/Chapter"+(chapter+1).ToString("00")+".png"));f=Time.frameCount;while(Time.frameCount<f+3)await Task.Yield();}
  return "Captured settled chapter views "+(start+1)+"–"+end;}finally{Time.timeScale=speed>0?speed:1;JourneySceneFlow.TestWithoutSaving=saving;}
 }
}

