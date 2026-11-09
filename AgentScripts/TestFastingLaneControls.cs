using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using CrossTheLine.Minigames;
public static class TestFastingLaneControls
{
    static int count;static System.Text.StringBuilder report;
    static void Check(bool ok,string s){if(!ok)throw new Exception("FAIL: "+s);count++;report.AppendLine("PASS: "+s);}
    static void Set(object o,string n,object v){var t=o.GetType();var f=t.GetField(n,BindingFlags.Instance|BindingFlags.NonPublic);if(f!=null)f.SetValue(o,v);else t.GetProperty(n).GetSetMethod(true).Invoke(o,new[]{v});}
    static async Task Wait(float s){var end=DateTime.UtcNow.AddSeconds(s);while(DateTime.UtcNow<end)await Task.Delay(16);}
    public static async Task<string> Run()
    {
        var g=UnityEngine.Object.FindAnyObjectByType<FastingTopDownGame>();if(!g||!Application.isPlaying)throw new Exception("Play top-down scene");count=0;report=new System.Text.StringBuilder();Keyboard keyboard=null;Key? held=null;var bg=InputSystem.settings.backgroundBehavior;var mode=InputSystem.settings.editorInputBehaviorInPlayMode;
        Action queue=()=>{if(keyboard!=null)InputSystem.QueueStateEvent(keyboard,held.HasValue?new KeyboardState(held.Value):new KeyboardState());};Action current=()=>{if(keyboard!=null)keyboard.MakeCurrent();};
        try{
            g.enabled=false;g.Begin();g.Tick(.01f,Vector2.right,false);Check(g.player.position.x==3,"Right command snaps exactly one lane immediately");g.Tick(.01f,Vector2.left,false);Check(g.player.position.x==0,"Left command snaps exactly one lane");
            g.Tick(.01f,Vector2.left,false);g.Tick(.01f,Vector2.left,false);g.Tick(.01f,Vector2.left,false);Check(g.player.position.x==-6,"Cannot leave five-lane boundary");
            g.Begin();g.Tick(.1f,Vector2.zero,false);float forward=g.player.position.y;Check(Mathf.Abs(forward-g.settings.moveSpeed*.1f)<.001f,"Automatic forward movement without input");g.Begin();g.Tick(.1f,Vector2.down,false);Check(Mathf.Abs(g.player.position.y-forward)<.001f,"Down input cannot reverse forward movement");g.Begin();g.Tick(.1f,Vector2.up,false);Check(Mathf.Abs(g.player.position.y-forward)<.001f,"Up input does not accelerate forward movement");
            float y=g.player.position.y;g.Tick(.1f,Vector2.zero,true);Check(g.Praying&&g.Prayer>0&&g.player.position.y>y,"Prayer continues during forward travel");g.Tick(.1f,Vector2.zero,false);Check(g.player.position.y>y,"Releasing prayer resumes automatic travel");
            var book=g.entities.First(e=>e.kind==FastingTopDownEntity.Kind.Book);var page=g.entities.First(e=>e.kind==FastingTopDownEntity.Kind.Page);var fragment=g.entities.First(e=>e.kind==FastingTopDownEntity.Kind.Fragment);
            g.Begin();g.Collect(fragment);Check(Mathf.Abs(g.Prayer-g.PrayerTarget*.1f)<.001f,"Fragment fills ten percent");g.Collect(page);Check(Mathf.Abs(g.Prayer-g.PrayerTarget*.35f)<.001f,"Page adds twenty-five percent");g.Collect(book);Check(g.Prayer==g.PrayerTarget&&g.StageIndex==0,"Complete Bible fills stage prayer without skipping destination");g.Collect(page);Check(g.Prayer==g.PrayerTarget,"Scripture cannot overflow stage target");
            var animator=g.playerBody.GetComponent<Animator>();Check(animator&&animator.runtimeAnimatorController,"Editable Animator is attached to player artwork");var clip=animator.runtimeAnimatorController.animationClips[0];var binding=AnimationUtility.GetObjectReferenceCurveBindings(clip)[0];var keys=AnimationUtility.GetObjectReferenceCurve(clip,binding);Check(keys.Select(k=>k.value).Distinct().Count()==4&&clip.isLooping,"Walk loop has four distinct sprite frames");Check(keys.All(k=>((Sprite)k.value).texture.width==64&&((Sprite)k.value).texture.height==64),"Every walk frame remains 64x64");
            float before=animator.GetCurrentAnimatorStateInfo(0).normalizedTime;await Wait(.22f);Check(animator.GetCurrentAnimatorStateInfo(0).normalizedTime>before,"Walk animation advances in actual Play frames");g.Begin();g.Tick(.05f,Vector2.zero,true);before=animator.GetCurrentAnimatorStateInfo(0).normalizedTime;await Wait(.22f);Check(animator.GetCurrentAnimatorStateInfo(0).normalizedTime>before&&g.Praying,"Walk animation continues while prayer is active");
            g.Begin();g.enabled=true;InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;keyboard=InputSystem.AddDevice<Keyboard>();InputSystem.onBeforeUpdate+=queue;InputSystem.onAfterUpdate+=current;keyboard.MakeCurrent();await Wait(.1f);held=Key.D;await Wait(.35f);Check(g.player.position.x==3,"Actual held D moves only one lane");await Wait(.2f);Check(g.player.position.x==3,"Holding D does not repeat");held=null;await Wait(.1f);held=Key.D;await Wait(.15f);Check(g.player.position.x==6,"Release and press D moves next lane");held=null;await Wait(.1f);held=Key.A;await Wait(.15f);Check(g.player.position.x==3,"Actual A moves one lane left");held=null;await Wait(.1f);
            g.enabled=false;g.Begin();g.player.position=new Vector3(0,4);g.Tick(.01f,Vector2.zero,false);g.SpawnWave();Directory.CreateDirectory("Docs/QA/FastingTopDown");ScreenCapture.CaptureScreenshot(Path.GetFullPath("Docs/QA/FastingTopDown/LaneControls-Walk.png"));await Wait(.3f);File.WriteAllText("Docs/QA/FastingTopDown/LaneControlsValidation.txt",report.ToString());return count+" checks passed";
        }finally{held=null;InputSystem.onBeforeUpdate-=queue;InputSystem.onAfterUpdate-=current;if(keyboard!=null)InputSystem.RemoveDevice(keyboard);InputSystem.settings.backgroundBehavior=bg;InputSystem.settings.editorInputBehaviorInPlayMode=mode;g.Begin();g.enabled=true;Time.timeScale=1;}
    }
}
