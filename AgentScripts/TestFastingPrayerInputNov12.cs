using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using CrossTheLine.Minigames;
public static class TestFastingPrayerInputNov12
{
    public static async Task<string> Run()
    {
        var g=UnityEngine.Object.FindAnyObjectByType<FastingTopDownGame>();Keyboard key=null;bool held=false;
        var background=InputSystem.settings.backgroundBehavior;var mode=InputSystem.settings.editorInputBehaviorInPlayMode;
        Action queue=()=>{if(key!=null)InputSystem.QueueStateEvent(key,held?new KeyboardState(Key.Space,Key.D):new KeyboardState());};Action current=()=>{if(key!=null)key.MakeCurrent();};
        try
        {
            Time.timeScale=1;g.Begin();g.enabled=true;foreach(var e in g.entities)e.gameObject.SetActive(false);
            InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            key=InputSystem.AddDevice<Keyboard>();InputSystem.onBeforeUpdate+=queue;InputSystem.onAfterUpdate+=current;
            await Task.Delay(180);float before=g.Prayer;held=true;await Task.Delay(350);float charged=g.Prayer;
            if(charged<=before||g.player.position.x<=0)throw new Exception("Space+D not charging/moving");
            held=false;await Task.Delay(100);float released=g.Prayer;await Task.Delay(220);if(g.Prayer>released+.01f)throw new Exception("Prayer continued after release");
            return "Actual InputSystem Space+D: prayer and movement work together; releasing Space stops charging.";
        }
        finally{InputSystem.onBeforeUpdate-=queue;InputSystem.onAfterUpdate-=current;if(key!=null)InputSystem.RemoveDevice(key);InputSystem.settings.backgroundBehavior=background;InputSystem.settings.editorInputBehaviorInPlayMode=mode;g.Begin();g.enabled=true;}
    }
}
