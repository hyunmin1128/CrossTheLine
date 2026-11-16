using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using CrossTheLine.Minigames;
public static class TestFastingPlaybackRecovery
{
    static UnityEngine.InputSystem.Key[] held=Array.Empty<UnityEngine.InputSystem.Key>();
    static void Key(params UnityEngine.InputSystem.Key[] keys){held=keys;}
    public static async Task<string> Run()
    {
        var g=UnityEngine.Object.FindAnyObjectByType<FastingTopDownGame>();if(!Application.isPlaying)throw new Exception("Play required");int n=0;
        Action<bool,string> ok=(b,s)=>{if(!b)throw new Exception(s);n++;};
        var prior=Keyboard.current;var background=InputSystem.settings.backgroundBehavior;var routing=InputSystem.settings.editorInputBehaviorInPlayMode;Keyboard device=null;
        Action queue=()=>{if(device!=null)InputSystem.QueueStateEvent(device,new KeyboardState(held));};Action current=()=>{if(device!=null)device.MakeCurrent();};
        try
        {
            held=Array.Empty<UnityEngine.InputSystem.Key>();InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            device=InputSystem.AddDevice<Keyboard>();InputSystem.onBeforeUpdate+=queue;InputSystem.onAfterUpdate+=current;device.MakeCurrent();
            Time.timeScale=1;ok(g.enabled,"Game component enabled");ok(g.playerBody.transform.lossyScale.x>0&&g.playerBody.transform.lossyScale.y>0,"Player has visible scale");
            // Queue input for the normal PlayerLoop; do not force an extra input update.
            Key(UnityEngine.InputSystem.Key.Enter);await Task.Delay(180);Key();await Task.Delay(80);ok(g.Running&&!g.introduction.activeSelf,"Enter starts the game through Update");
            var start=g.player.position;Key(UnityEngine.InputSystem.Key.D);await Task.Delay(250);Key();await Task.Delay(60);ok(g.player.position.x>start.x+.1f,"D input moves right");
            float x=g.player.position.x;Key(UnityEngine.InputSystem.Key.A);await Task.Delay(250);Key();await Task.Delay(60);ok(g.player.position.x<x-.1f,"A input moves left");
            float y=g.player.position.y;Key(UnityEngine.InputSystem.Key.W);await Task.Delay(200);Key();await Task.Delay(50);ok(g.player.position.y>y+.1f,"W input moves forward");y=g.player.position.y;Key(UnityEngine.InputSystem.Key.S);await Task.Delay(200);Key();await Task.Delay(50);ok(g.player.position.y<y-.1f,"S input moves backward against scroll");
            float prayer=g.Prayer;Key(UnityEngine.InputSystem.Key.Space);await Task.Delay(650);ok(g.Praying&&g.Prayer>prayer,"Space held prepares and charges prayer");Key();await Task.Delay(80);ok(!g.Praying,"Space released stops prayer");
            ok(g.Elapsed>0&&g.gameCamera.transform.position.y>2.5f,"Automatic forward movement and camera advance");
            g.startButton.onClick.Invoke();ok(g.Running&&g.Health==g.settings.maximumHealth,"Start/retry button path works");
            g.TryDamage("Rock");float health=g.Health;Key(UnityEngine.InputSystem.Key.E);await Task.Delay(100);Key();await Task.Delay(60);ok(!g.HasWater&&g.Health>health,"E uses water and heals");
            Vector3 scale=g.playerBody.transform.localScale,pose=g.playerBody.transform.localPosition;g.hitFeedback.Play(FastingTopDownHazard.Kind.Sand);g.hitFeedback.Advance(.3f);g.hitFeedback.Clear();ok(g.playerBody.transform.localScale==scale&&g.playerBody.transform.localPosition==pose,"Hit feedback restores valid player pose");
            Key(UnityEngine.InputSystem.Key.R);await Task.Delay(100);Key();await Task.Delay(60);ok(g.Running&&g.Health==g.settings.maximumHealth&&g.HasWater,"R retries via keyboard");
            ok(g.playerBody.isVisible,"Player visible to rendering camera");
            return n+" checks passed: Enter, WASD horizontal, Space, E, R, camera, player visibility and feedback reset.";
        }
        finally{Key();InputSystem.onBeforeUpdate-=queue;InputSystem.onAfterUpdate-=current;if(device!=null)InputSystem.RemoveDevice(device);if(prior!=null)prior.MakeCurrent();InputSystem.settings.backgroundBehavior=background;InputSystem.settings.editorInputBehaviorInPlayMode=routing;Time.timeScale=1;g.enabled=true;g.Begin();}
    }
}
