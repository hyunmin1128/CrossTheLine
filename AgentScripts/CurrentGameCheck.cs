using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Rendering;
using CrossTheLine;

public static class CurrentGameCheck
{
    const string Output="Docs/QA/CurrentCheck-2026-11-03";
    const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
    static object Get(JourneyGame g,string n)=>typeof(JourneyGame).GetField(n,Flags).GetValue(g);
    static void Call(JourneyGame g,string n,params object[] a)=>typeof(JourneyGame).GetMethod(n,Flags).Invoke(g,a);
    public static string Assets()
    {
        Directory.CreateDirectory(Output);
        var issues=new List<string>(); int refs=0,files=0;
        var known=new Dictionary<string,bool>();
        foreach(var p in AssetDatabase.GetAllAssetPaths().Where(p=>p.StartsWith("Assets/")&&File.Exists(p)&&new[]{".unity",".prefab",".asset",".mat",".anim",".controller"}.Contains(Path.GetExtension(p))))
        {
            files++;
            string text=File.ReadAllText(p);
            foreach(Match m in Regex.Matches(text,@"guid: ([a-f0-9]{32})"))
            {
                refs++; string id=m.Groups[1].Value;
                if(id.StartsWith("0000000000000000"))continue;
                if(!known.TryGetValue(id,out bool valid)){valid=!string.IsNullOrEmpty(AssetDatabase.GUIDToAssetPath(id));known[id]=valid;}
                if(!valid)issues.Add(p+" missing GUID "+id);
            }
        }
        foreach(var s in EditorBuildSettings.scenes.Where(s=>s.enabled))if(!File.Exists(s.path))issues.Add("Build scene missing "+s.path);
        foreach(var p in AssetDatabase.FindAssets("t:Prefab").Select(AssetDatabase.GUIDToAssetPath).Where(p=>p.Contains("Characters/Disciple")))
        {
            var v=AssetDatabase.LoadAssetAtPath<GameObject>(p).GetComponent<JourneyCharacterView>();
            if(!v||!v.idle||!v.idle.texture)issues.Add("Disciple profile missing art "+p);
            else if(!v.idle.texture.name.StartsWith("Real"))issues.Add("Disciple profile not Real "+p+" "+v.idle.texture.name);
        }
        string result=$"Files={files}, GUID references={refs}, issues={issues.Count}\n"+string.Join("\n",issues.Distinct());
        File.WriteAllText(Output+"/Assets.txt",result);return result;
    }
    static async Task Frames(int n)
    {
        int target=Time.frameCount+n; double start=EditorApplication.timeSinceStartup;
        while(Time.frameCount<target){if(EditorApplication.timeSinceStartup-start>15)throw new Exception("Frame timeout");await Task.Yield();}
    }
    static async Task Settle()
    {
        double until=EditorApplication.timeSinceStartup+1.25;
        while(EditorApplication.timeSinceStartup<until)await Frames(1);
    }
    public static async Task<string> Review()
    {
        var g=UnityEngine.Object.FindAnyObjectByType<JourneyGame>();var world=g.GetComponent<JourneyContinuousWorld>();
        JourneySceneFlow.TestWithoutSaving=true;
        var rows=new List<string>();
        foreach(int c in new[]{0,2,10,11,14,15})
        {
            world.Normalize();Call(g,"ClearStoryPanels");typeof(JourneyGame).GetField("save",Flags).SetValue(g,new JourneySave{chapter=c,unlocked=26});
            Call(g,"CloseMenu");Call(g,"LoadWorld",false);await Settle();
            Action<ScriptableRenderContext,Camera> render=(ctx,cam)=>
            {
                if(cam!=g.journeyCamera||rows.Any(s=>s.StartsWith("Chapter "+(c+1)+":")))return;
                var sr=g.PlayerRoot.GetComponentInChildren<SpriteRenderer>(true);var v=cam.WorldToViewportPoint(sr.bounds.center);
                rows.Add($"Chapter {c+1}: hero={g.PlayerRoot.position} sprite={sr.sprite?.name} active={sr.gameObject.activeInHierarchy} enabled={sr.enabled} viewport={v} camera={cam.transform.position} ortho={cam.orthographicSize} aspect={cam.aspect}");
            };
            RenderPipelineManager.beginCameraRendering+=render;try{await Frames(3);}finally{RenderPipelineManager.beginCameraRendering-=render;}
            if(c==2)rows.Add("Chapter3 active satan frames: "+string.Join(",",g.satanFrames.Select(s=>s?s.name:"NULL")));
            if(c==0){ScreenCapture.CaptureScreenshot(Path.GetFullPath(Output+"/Chapter01.png"));await Frames(4);}
        }
        File.WriteAllLines(Output+"/Review.txt",rows);Time.timeScale=1;return string.Join("\n",rows);
    }
    public static async Task<string> Runtime()
    {
        if(!Application.isPlaying)throw new Exception("Play required");
        Directory.CreateDirectory(Output);
        var rows=new List<string>{"Chapter\tMode\tPlayer visible\tMovement delta\tStatus"};
        var g=UnityEngine.Object.FindAnyObjectByType<JourneyGame>();
        var book=JsonUtility.FromJson<StoryBook>(g.storySource.text);
        var world=g.GetComponent<JourneyContinuousWorld>();
        var background=InputSystem.settings.backgroundBehavior;
        var routing=InputSystem.settings.editorInputBehaviorInPlayMode;
        var keyboard=InputSystem.AddDevice<Keyboard>(); bool held=false;
        Action queue=()=>InputSystem.QueueStateEvent(keyboard,held?new KeyboardState(Key.D):new KeyboardState());
        var errors=new List<string>(); Application.LogCallback log=(m,s,t)=>{if(t==LogType.Error||t==LogType.Exception||t==LogType.Assert)errors.Add(m);};
        InputSystem.onBeforeUpdate+=queue;Application.logMessageReceived+=log;
        bool prior=JourneySceneFlow.TestWithoutSaving;JourneySceneFlow.TestWithoutSaving=true;
        try
        {
            InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            for(int c=0;c<book.chapters.Length;c++)
            {
                held=false;world.Normalize();Call(g,"ClearStoryPanels");
                typeof(JourneyGame).GetField("save",Flags).SetValue(g,new JourneySave{chapter=c,step=0,unlocked=26});
                typeof(JourneyGame).GetField("started",Flags).SetValue(g,true);
                Call(g,"CloseMenu");Call(g,"LoadWorld",false);
                await Settle();
                bool visible=false; int renders=0;
                Action<ScriptableRenderContext,Camera> render=(ctx,cam)=>
                {
                    if(cam!=g.journeyCamera)return;
                    renders++;
                    var sr=g.PlayerRoot.GetComponentInChildren<SpriteRenderer>(true);
                    if(sr&&sr.enabled&&sr.gameObject.activeInHierarchy&&sr.sprite&&sr.color.a>.05f){var v=cam.WorldToViewportPoint(sr.bounds.center);visible|=v.z>0&&v.x>=0&&v.x<=1&&v.y>=0&&v.y<=1;}
                };
                RenderPipelineManager.beginCameraRendering+=render;
                try{await Frames(12);}finally{RenderPipelineManager.beginCameraRendering-=render;}
                world.Normalize();float start=g.PlayerRoot.position.x; string mode=g.Mode;
                held=true; keyboard.MakeCurrent();await Frames(35);held=false;await Frames(2);
                world.Normalize();float delta=g.PlayerRoot.position.x-start;
                string status=!visible?"REVIEW visibility":Mathf.Abs(delta)>.01f?"PASS movement":"REVIEW scripted/blocked";
                rows.Add($"{c+1}\t{mode}\t{visible} (renders {renders})\t{delta:F3}\t{status}");
                File.WriteAllLines(Output+"/Runtime.tsv",rows);
            }
            return string.Join("\n",rows)+"\nRuntime errors="+errors.Count+"\n"+string.Join("\n",errors.Distinct());
        }
        finally
        {
            held=false;InputSystem.onBeforeUpdate-=queue;Application.logMessageReceived-=log;
            InputSystem.RemoveDevice(keyboard);InputSystem.settings.backgroundBehavior=background;
            InputSystem.settings.editorInputBehaviorInPlayMode=routing;
            JourneySceneFlow.TestWithoutSaving=prior;Time.timeScale=1;
            File.WriteAllLines(Output+"/RuntimeErrors.txt",errors);
        }
    }
}
