using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Rendering;
using CrossTheLine.Minigames;
public static class TestFastingMinigame
{
    static int checks;
    static readonly BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;
    static void Set(FastingMinigame g,string n,object value)=>typeof(FastingMinigame).GetField(n,F).SetValue(g,value);
    static void Check(bool ok,string message){checks++;if(!ok)throw new Exception(message);}
    static async Task Frames(int n){int end=Time.frameCount+n;double until=EditorApplication.timeSinceStartup+15;while(Time.frameCount<end){if(EditorApplication.timeSinceStartup>until)throw new Exception("Frame timeout");await Task.Yield();}}
    static async Task Seconds(float seconds){double end=EditorApplication.timeSinceStartup+seconds;while(EditorApplication.timeSinceStartup<end)await Frames(1);}
    public static async Task<string> Run()
    {
        if(!Application.isPlaying)throw new Exception("Play required");
        var g=UnityEngine.Object.FindAnyObjectByType<FastingMinigame>();if(!g)throw new Exception("Open the fasting test scene.");
        var log=new List<string>();var errors=new List<string>();Application.LogCallback capture=(m,s,t)=>{if(t==LogType.Error||t==LogType.Exception)errors.Add(m);};Application.logMessageReceived+=capture;
        var bg=InputSystem.settings.backgroundBehavior;var route=InputSystem.settings.editorInputBehaviorInPlayMode;Keyboard keyboard=null;bool held=false;
        Action queue=()=>{if(keyboard!=null)InputSystem.QueueStateEvent(keyboard,held?new KeyboardState(Key.D):new KeyboardState());};
        Action makeCurrent=()=>{if(keyboard!=null)keyboard.MakeCurrent();};
        try
        {
            checks=0;await Frames(5);g.Restart();g.enabled=false;
            Check(g.Stamina==5&&g.StageIndex==0,"Fresh start");Check(g.settings.stages.Length==8,"8 stages");Check(g.staminaSlots.Length==5,"5 UI slots");
            Check(g.settings.stages.Sum(s=>s.seconds)==200,"200-second minimum");Check(g.covers.Length==16,"16 editable covers");
            for(int i=1;i<8;i++){Check(g.settings.stages[i].windSpeed>=g.settings.stages[i-1].windSpeed,"Speed ramp");Check(g.settings.stages[i].waveInterval<=g.settings.stages[i-1].waveInterval,"Frequency ramp");}
            for(int i=0;i<50;i++){g.RequestStep(Vector2Int.left);g.Advance(.3f);}Check(Mathf.Abs(g.player.position.x-g.settings.ground.xMin)<.01f,"Left edge clamp");
            for(int i=0;i<10;i++){g.RequestStep(Vector2Int.down);g.Advance(.3f);}Check(g.player.position.y>=g.settings.ground.yMin-.01f,"Bottom clamp");
            for(int i=0;i<10;i++){g.RequestStep(Vector2Int.up);g.Advance(.3f);}Check(g.player.position.y<=g.settings.ground.yMax+.01f,"Top clamp");
            g.Restart();var cover=g.covers[0];g.player.position=cover.transform.position+new Vector3(-1.5f,0,0);
            Check(g.Sheltered,"LEFT lee side shelters from right-to-left wind");g.TakeHit();Check(g.Stamina==5,"Shelter prevents damage");
            g.player.position=cover.transform.position+new Vector3(1.5f,0,0);Check(!g.Sheltered,"Right side exposed");
            g.TakeHit();Check(g.Stamina==4,"Hit consumes exactly one slot");g.TakeHit();Check(g.Stamina==4,"Grace prevents double hit");
            g.Restart();Set(g,"waveTime",99999f);Set(g,"stageTime",g.settings.stages[0].seconds*.6f);Set(g,"exposure",g.settings.stages[0].exposureSeconds-.02f);g.Advance(.03f);Check(g.Stamina==4,"Night exposure consumes one slot");
            g.Restart();g.player.position=cover.transform.position+new Vector3(-1.5f,0,0);Set(g,"exposure",100f);g.Advance(.01f);Check(g.Stamina==5,"Shelter resets weather exposure");
            g.Restart();Set(g,"stageTime",100f);Check(g.Day==5,"Waiting at a gate does not skip days or difficulty");
            g.Restart();g.player.position=new Vector3(-12,settingsLane(g),0);Set(g,"waveTime",0f);g.Advance(.01f);g.Advance(1.6f);Check(g.Storms.Count>0,"Wave spawns after warning");
            var storm=g.Storms[0];float x=storm.transform.position.x;storm.Advance(.2f);Check(storm.transform.position.x<x,"Wind moves RIGHT to LEFT");
            storm.transform.position=g.player.position+new Vector3(2,0,0);storm.speed=30;Set(g,"invulnerable",0f);int before=g.Stamina;g.Advance(.2f);Check(g.Stamina==before-1,"Swept collision catches fast-moving storm");
            g.Restart();g.player.position=new Vector3(-12,-6.1f,0);
            for(int i=0;i<5;i++){Set(g,"invulnerable",0f);g.TakeHit();}Check(g.Restarting&&g.Stamina==0,"Zero stamina starts full reset");g.enabled=true;
            double deadline=EditorApplication.timeSinceStartup+5;while(g.Restarting&&EditorApplication.timeSinceStartup<deadline)await Frames(1);
            Check(!g.Restarting&&g.Stamina==5&&g.StageIndex==0&&g.StageTime<1,"Automatic restart returns to day 1, full stamina");g.enabled=false;
            g.Restart();
            for(int i=0;i<8;i++){g.player.position=new Vector3(g.Gate,-6.1f,0);Set(g,"stageTime",g.settings.stages[i].seconds);g.Advance(.001f);Check(i==7?g.Finished:g.StageIndex==i+1,"Stage transition "+i);}
            Check(g.Finished&&g.Day==40,"Forty-day completion");Check(g.resultPanel.activeSelf,"Completion panel visible");
            g.Restart();g.enabled=true;InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;keyboard=InputSystem.AddDevice<Keyboard>();InputSystem.onBeforeUpdate+=queue;InputSystem.onAfterUpdate+=makeCurrent;keyboard.MakeCurrent();
            var start=g.player.position;held=true;await Seconds(.12f);held=false;await Seconds(.45f);Check(g.player.position.x>start.x+.8f,"Real Input System D moves player: "+start+" -> "+g.player.position);float stopped=g.player.position.x;await Seconds(.15f);Check(Mathf.Abs(g.player.position.x-stopped)<.001f,"Release stops after one grid step");
            g.Restart();await Frames(3);bool visible=false;
            Action<ScriptableRenderContext,Camera> render=(ctx,cam)=>{if(cam==g.gameCamera){var v=cam.WorldToViewportPoint(g.body.bounds.center);visible|=v.z>0&&v.x>=0&&v.x<=1&&v.y>=0&&v.y<=1;}};
            RenderPipelineManager.beginCameraRendering+=render;try{await Frames(5);}finally{RenderPipelineManager.beginCameraRendering-=render;}
            Check(visible,"Player visible in actual rendered viewport");Check(g.body.sprite!=null,"Actual source sprite assigned");
            Directory.CreateDirectory("Docs/QA/FastingMinigame");ScreenCapture.CaptureScreenshot(Path.GetFullPath("Docs/QA/FastingMinigame/Day.png"));await Frames(4);
            Set(g,"stageTime",g.settings.stages[0].seconds*.6f);g.Advance(.01f);await Frames(3);ScreenCapture.CaptureScreenshot(Path.GetFullPath("Docs/QA/FastingMinigame/Night.png"));await Frames(4);
            Check(errors.Count==0,"Runtime console errors: "+string.Join(";",errors));log.Add($"PASS {checks} checks: stamina, reset, exposure/cover side, swept wind hits, difficulty, boundaries, completion, real keyboard, actual rendering.");
            File.WriteAllLines("Docs/QA/FastingMinigame/Validation.txt",log);return log[0];
        }
        finally{Application.logMessageReceived-=capture;InputSystem.onBeforeUpdate-=queue;InputSystem.onAfterUpdate-=makeCurrent;if(keyboard!=null)InputSystem.RemoveDevice(keyboard);InputSystem.settings.backgroundBehavior=bg;InputSystem.settings.editorInputBehaviorInPlayMode=route;Time.timeScale=1;if(g){g.enabled=true;g.Restart();}}
    }
    static float settingsLane(FastingMinigame g)=>g.settings.lanes[1];
}
