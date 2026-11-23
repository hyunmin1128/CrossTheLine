using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Newtonsoft.Json;
using CrossTheLine.Minigames.Fishing;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
public static class TestChapter08FishingScene
{
    public static async Task<string> Input()
    {
        var g=UnityEngine.Object.FindAnyObjectByType<NetFishingGame>();var original=Keyboard.current;var test=InputSystem.AddDevice<Keyboard>();var result=new Dictionary<string,object>();
        try
        {
            g.Restart();g.enabled=true;test.MakeCurrent();InputSystem.QueueStateEvent(test,new KeyboardState(Key.Enter));await Task.Delay(160);
            result["enterStarts"]=g.Session.State==NetFishingSession.Phase.Exploring;InputSystem.QueueStateEvent(test,new KeyboardState());await Task.Delay(80);
            float x=g.boat.position.x;InputSystem.QueueStateEvent(test,new KeyboardState(Key.D));await Task.Delay(300);InputSystem.QueueStateEvent(test,new KeyboardState());await Task.Delay(80);result["DMovement"]=g.boat.position.x-x;
            InputSystem.QueueStateEvent(test,new KeyboardState(Key.Escape));await Task.Delay(120);result["escapePauses"]=g.Paused;InputSystem.QueueStateEvent(test,new KeyboardState());await Task.Delay(80);InputSystem.QueueStateEvent(test,new KeyboardState(Key.Escape));await Task.Delay(120);InputSystem.QueueStateEvent(test,new KeyboardState());await Task.Delay(80);result["escapeResumes"]=!g.Paused;
            g.enabled=false;g.boat.position=g.fishingSpots[g.ActiveSpot].position;g.enabled=true;InputSystem.QueueStateEvent(test,new KeyboardState(Key.E));await Task.Delay(100);InputSystem.QueueStateEvent(test,new KeyboardState());await Task.Delay(80);result["ECasts"]=g.Session.State!=NetFishingSession.Phase.Exploring;
            g.enabled=false;for(int i=0;i<100&&g.Session.State!=NetFishingSession.Phase.Fighting;i++)g.Tick(.1f,Vector2.zero,false,false);
            float progress=g.Session.Progress;g.enabled=true;InputSystem.QueueStateEvent(test,new KeyboardState(Key.Space));await Task.Delay(350);InputSystem.QueueStateEvent(test,new KeyboardState());await Task.Delay(80);
            result["holdingSpaceDoesNotRepeatTaps"]=g.Session.Progress<=progress+g.settings.progressPerGoodTap+.001f;
            if(!(bool)result["enterStarts"]||(float)result["DMovement"]<=.1f||!(bool)result["escapePauses"]||!(bool)result["escapeResumes"]||!(bool)result["ECasts"]||!(bool)result["holdingSpaceDoesNotRepeatTaps"])throw new Exception(JsonConvert.SerializeObject(result));
        }
        finally{InputSystem.RemoveDevice(test);if(original!=null)original.MakeCurrent();g.Restart();g.enabled=false;}
        string json=JsonConvert.SerializeObject(result,Formatting.Indented);File.WriteAllText("Docs/Chapter08/input-checks.json",json);return json;
    }
    public static async Task<string> Run()
    {
        var g=UnityEngine.Object.FindAnyObjectByType<NetFishingGame>();if(!g||!Application.isPlaying)throw new Exception("Play the fishing scene first");g.enabled=false;
        var source=g.settings;var clone=UnityEngine.Object.Instantiate(source);var random=UnityEngine.Random.state;int originalSeed=g.randomSeed;var rows=new List<object>();var checks=new Dictionary<string,object>();
        try
        {
            g.settings=clone;g.Restart();g.startButton.onClick.Invoke();checks["startButton"]=g.Session.State==NetFishingSession.Phase.Exploring;checks["cannotCastFarAway"]=!g.TryCast();
            for(int i=0;i<100;i++)g.Tick(.1f,new Vector2(-1,-1),false,false);checks["movementClamped"]=g.boat.position.x>=clone.boatBounds.xMin&&g.boat.position.y>=clone.boatBounds.yMin;
            float time=g.Session.Elapsed;Vector3 position=g.boat.position;g.TogglePause();g.Tick(.1f,Vector2.one,true,true);checks["pauseFreezesTimeAndBoat"]=g.Session.Elapsed==time&&g.boat.position==position;g.TogglePause();
            foreach(var entry in checks)if(!(bool)entry.Value)throw new Exception(entry.Key);
            for(int seed=0;seed<12;seed++)
            {
                UnityEngine.Random.InitState(seed+1700);g.randomSeed=1700+seed;g.Restart();g.StartGame();var decisionRandom=new System.Random(seed+888);float decision=0,maxCast=0;
                for(int step=0;step<108000&&g.Session.State!=NetFishingSession.Phase.Complete;step++)
                {
                    Vector2 move=Vector2.zero;bool cast=false,tap=false;
                    if(g.Session.State==NetFishingSession.Phase.Exploring&&g.Session.PhaseTime>=.8f){var d=(Vector2)g.fishingSpots[g.ActiveSpot].position-(Vector2)g.boat.position;cast=d.magnitude<.8f;move=cast?Vector2.zero:d;}
                    if(g.Session.State==NetFishingSession.Phase.Fighting){decision-=1f/60;if(decision<=0){decision=.22f;float next=g.Session.Zone-clone.zoneFallSpeed/60+clone.tapLift;tap=next<=g.Session.Fish+clone.catchZoneHeight*.5f-.01f&&g.Session.Zone<g.Session.Fish+.025f;if(decisionRandom.NextDouble()<.05)tap=!tap;}}
                    g.Tick(1f/60,move,cast,tap);maxCast=Mathf.Max(maxCast,g.Session.CastElapsed);
                    if(step%120==119)await Task.Yield();
                }
                rows.Add(new{seed,outcome=g.Session.State.ToString(),seconds=g.Session.Elapsed,g.Session.FishCaught,g.Session.Casts,g.Session.Escapes,g.Session.BrokenNets,maxCast});
                File.WriteAllText("Docs/Chapter08/scene-trials.json",JsonConvert.SerializeObject(rows,Formatting.Indented));await Task.Yield();
            }
        }
        finally{g.settings=source;g.randomSeed=originalSeed;UnityEngine.Random.state=random;g.Restart();g.enabled=false;UnityEngine.Object.Destroy(clone);}
        File.WriteAllText("Docs/Chapter08/scene-checks.json",JsonConvert.SerializeObject(checks,Formatting.Indented));return "12 full scene/controller runs plus button, cast radius, boundary and pause checks completed";
    }
}
