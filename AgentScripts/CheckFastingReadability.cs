using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using CrossTheLine.Minigames;
public static class CheckFastingReadability
{
    static int checks;
    static readonly BindingFlags Flags=BindingFlags.NonPublic|BindingFlags.Instance;
    static void Set(FastingMinigame g,string name,object value)=>typeof(FastingMinigame).GetField(name,Flags).SetValue(g,value);
    static void Check(bool condition,string message){checks++;if(!condition)throw new Exception(message);}
    static async Task Frames(int n){int end=Time.frameCount+n;double deadline=EditorApplication.timeSinceStartup+10;while(Time.frameCount<end){if(EditorApplication.timeSinceStartup>deadline)throw new Exception("Frames stalled");await Task.Yield();}}
    static async Task Seconds(float seconds){double end=EditorApplication.timeSinceStartup+seconds;while(EditorApplication.timeSinceStartup<end)await Frames(1);}
    public static async Task<string> Run()
    {
        var g=UnityEngine.Object.FindAnyObjectByType<FastingMinigame>();if(!Application.isPlaying||!g)throw new Exception("Play the fasting test scene");
        var f=g.GetComponent<FastingReadability>();var priorBg=InputSystem.settings.backgroundBehavior;var priorRouting=InputSystem.settings.editorInputBehaviorInPlayMode;Keyboard key=null;bool held=false;
        Action queue=()=>{if(key!=null)InputSystem.QueueStateEvent(key,held?new KeyboardState(Key.D):new KeyboardState());};Action current=()=>{if(key!=null)key.MakeCurrent();};
        try{
            checks=0;await Frames(5);g.Restart();g.enabled=false;f.Refresh();
            Check(g.InspectStep(Vector2Int.right,out _)==FastingMinigame.StepState.Available,"Right step available at spawn");
            Check(f.exposureFill.sprite!=null&&f.exposureFill.type==UnityEngine.UI.Image.Type.Filled,"Exposure bar has a fillable sprite");
            Check(f.exposureFill.fillAmount==0,"Shelter/exposure bar begins empty");
            Check(g.InspectStep(Vector2Int.left,out _)==FastingMinigame.StepState.Boundary,"Left boundary reported");
            g.RequestStep(Vector2Int.left);f.Refresh();Check(f.movementText.text.Contains("境界")||f.movementText.text.Contains("경계"),"Blocked reason visible");
            var cover=g.covers[0];g.player.position=cover.transform.position+new Vector3(-1.5f,0,0);Set(g,"lane",0);g.Advance(.001f);f.Refresh();
            Check(g.Sheltered&&f.coverText.text.Contains("엄폐 성공"),"Shelter status matches actual protection");
            Check(g.InspectStep(Vector2Int.right,out _)==FastingMinigame.StepState.Obstacle,"Rock blocks visible width");
            Check(f.destinations[1].color.r>.9f&&f.destinations[1].color.g<.4f,"Obstacle destination is red");
            Check(Vector2.Distance(f.playerFeet.bounds.size,g.feetHitSize)<.001f,"Visible player footprint equals collision size");
            Check(Vector2.Distance(f.playerFeet.bounds.center,g.FeetHitArea.center)<.001f,"Visible player footprint equals collision center");
            g.RequestStep(Vector2Int.right);f.Refresh();Check(f.movementText.text.Contains("옆길"),"Obstacle explains detour");
            g.RequestStep(Vector2Int.down);f.Refresh();Check(g.Moving&&f.movementText.text.Contains("이동 중"),"Movement underway visible");g.Advance(.3f);f.Refresh();
            Check(!g.Sheltered&&f.coverText.text.Contains("엄폐 안 됨"),"Leaving shelter changes status");
            g.Restart();g.player.position=new Vector3(g.Gate,-6.1f,0);f.Refresh();
            Check(g.InspectStep(Vector2Int.right,out _)==FastingMinigame.StepState.Waiting,"Gate reports timer wait");g.RequestStep(Vector2Int.right);f.Refresh();Check(f.movementText.text.Contains("대기"),"Gate wait reason visible");
            g.Restart();g.TakeHit();f.Refresh();Check(f.damagePanel.activeSelf&&f.damageText.text.Contains("-1칸"),"Damage reason notice visible");
            g.Restart();Set(g,"stageTime",12f);Set(g,"exposure",g.ExposureLimit-.01f);g.Advance(.02f);f.Refresh();Check(g.DamageMessage.Contains("추위"),"Weather damage distinguished from wind");
            var storm=UnityEngine.Object.Instantiate(g.stormPrefab);storm.transform.position=new Vector3(2,0,0);storm.speed=3;storm.Advance(1);
            Check(!storm.TryHit(new Vector2(2,1.1f),new Vector2(2,0),g.feetHitSize,out _),"No false hit when player enters a lane after wind passes");
            storm.transform.position=new Vector3(2,0,0);storm.speed=4;storm.Advance(1);Check(storm.TryHit(Vector2.zero,Vector2.zero,g.feetHitSize,out _),"Fast wave still hits exposed stationary feet");
            Check(Vector2.Distance(storm.groundFill.bounds.size,storm.hitSize)<.001f,"Storm ground mark equals actual hit dimensions");
            Check(Vector2.Distance(storm.groundFill.bounds.center,storm.transform.position)<.001f,"Storm ground mark centered on actual hit rectangle");UnityEngine.Object.Destroy(storm.gameObject);
            g.Restart();g.enabled=true;InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;key=InputSystem.AddDevice<Keyboard>();InputSystem.onBeforeUpdate+=queue;InputSystem.onAfterUpdate+=current;key.MakeCurrent();
            float start=g.player.position.x;held=true;await Seconds(.85f);held=false;await Seconds(.35f);Check(g.player.position.x>=start+3-.01f,"Holding D repeats movement");float stop=g.player.position.x;await Seconds(.2f);Check(Mathf.Abs(stop-g.player.position.x)<.001f,"Releasing D stops repeat");
            g.Restart();g.enabled=false;g.player.position=cover.transform.position+new Vector3(-1.5f,0,0);Set(g,"lane",0);g.Advance(.001f);
            typeof(FastingMinigame).GetMethod("WarnWave",Flags).Invoke(g,null);typeof(FastingMinigame).GetMethod("SpawnWave",Flags).Invoke(g,null);var live=g.Storms[0];live.transform.position=new Vector3(-6,cover.transform.position.y,0);g.Advance(.01f);f.Refresh();await Frames(3);
            Directory.CreateDirectory("Docs/QA/FastingMinigame");ScreenCapture.CaptureScreenshot(Path.GetFullPath("Docs/QA/FastingMinigame/Readability-Day.png"));await Frames(4);
            Set(g,"stageTime",12f);g.Advance(.01f);f.Refresh();await Frames(3);ScreenCapture.CaptureScreenshot(Path.GetFullPath("Docs/QA/FastingMinigame/Readability-Night.png"));await Frames(4);
            string report=$"PASS {checks} checks: readable hit footprints, cover/blocked/gate status, exposure damage reason, relative swept collision, held/released real keyboard, day/night captures.";File.WriteAllText("Docs/QA/FastingMinigame/ReadabilityValidation.txt",report);return report;
        }finally{held=false;InputSystem.onBeforeUpdate-=queue;InputSystem.onAfterUpdate-=current;if(key!=null)InputSystem.RemoveDevice(key);InputSystem.settings.backgroundBehavior=priorBg;InputSystem.settings.editorInputBehaviorInPlayMode=priorRouting;Time.timeScale=1;if(g){g.enabled=true;g.Restart();}}
    }
}
