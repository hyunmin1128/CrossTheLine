using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using CrossTheLine.Minigames;
using Newtonsoft.Json;
public static class TestFastingPace20261124
{
    public static async Task<string> Run()
    {
        for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++){var scene=UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);if(scene.path.Contains("Chapter08_Fishing"))foreach(var root in scene.GetRootGameObjects())root.SetActive(false);}
        var g=UnityEngine.Object.FindAnyObjectByType<FastingTopDownGame>();g.enabled=false;
        string path=g.BalanceReportPath;bool existed=File.Exists(path);byte[] bytes=existed?File.ReadAllBytes(path):null;
        var random=UnityEngine.Random.state;var result=new Dictionary<string,object>();
        try
        {
            var type=typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");var property=type.GetProperty("drawGizmos",BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance);
            var guideValues=Resources.FindObjectsOfTypeAll(type).Select(w=>(bool)property.GetValue(w)).ToArray();if(guideValues.Any(v=>v))throw new Exception("Game-view guides must be hidden in fasting Play mode");result["gameViewGizmos"]=guideValues;
            var scroll=new List<object>();
            for(int i=0;i<4;i++)
            {
                g.BeginStageForTesting(i);g.enabled=false;float start=g.gameCamera.transform.position.y;
                for(int step=0;step<10;step++)g.Tick(.1f,Vector2.zero,false);
                float moved=g.gameCamera.transform.position.y-start;if(Mathf.Abs(moved-g.CurrentSpeed)>.002f)throw new Exception("Incorrect scrolling");
                var b=g.backgrounds.Where(x=>x).OrderBy(x=>x.bounds.min.y).ToArray();float gap=0;for(int n=1;n<b.Length;n++)gap=Mathf.Max(gap,b[n].bounds.min.y-b[n-1].bounds.max.y);
                scroll.Add(new{stage=i+1,profileSpeed=g.CurrentStageConfiguration.scrollSpeed,worldScroll=g.CurrentSpeed,movedInOneSecond=moved,g.PrayerTarget,maxBackgroundGap=gap});await Task.Yield();
            }
            result["scroll"]=scroll;
            g.BeginStageForTesting(0);g.enabled=false;UnityEngine.Random.InitState(62411);int cactus=0;
            for(int i=0;i<40000;i++){if(!g.TryChooseObstacle(out var kind))throw new Exception("Selection failed");if(kind==FastingTopDownHazard.Kind.Cactus)cactus++;}
            float fraction=cactus/40000f;if(Mathf.Abs(fraction-.375f)>.015f)throw new Exception("Incorrect cactus weights");result["cactusSelection"]=new{samples=40000,cactus,observedFraction=fraction,theoreticalFraction=.375f};
            g.SpawnPatternObstacle(FastingTopDownHazard.Kind.Sand,3,1,0);
            var sand=g.Hazards.FirstOrDefault(h=>h&&h.kind==FastingTopDownHazard.Kind.Sand&&h.velocity.y<0);if(!sand)throw new Exception("Top sand was not accepted");
            if(Mathf.Abs(sand.velocity.y+6.82f)>.01f)throw new Exception("Incorrect top sand velocity");
            typeof(FastingTopDownGame).GetMethod("SpawnSideSand",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(g,new object[]{true});
            var side=g.Hazards.FirstOrDefault(h=>h&&Mathf.Abs(h.velocity.x)>1);if(!side||Mathf.Abs(side.velocity.x-16)>.01f)throw new Exception("Incorrect side sand velocity");
            result["sand"]=new{topY=sand.velocity.y,sideX=side.velocity.x,sideCameraRelativeVertical=side.velocity.y-g.CurrentSpeed};
        }
        finally{g.BeginStageForTesting(0);g.enabled=false;UnityEngine.Random.state=random;if(existed)File.WriteAllBytes(path,bytes);else if(File.Exists(path))File.Delete(path);}
        string json=JsonConvert.SerializeObject(result,Formatting.Indented);File.WriteAllText("Docs/BalanceTests/2026-11-24/DesertPace/tests.json",json);return json;
    }
}
