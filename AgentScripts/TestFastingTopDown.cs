using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using CrossTheLine.Minigames;
public static class TestFastingTopDown
{
    static int count;static System.Text.StringBuilder report;
    static void Check(bool condition,string message){if(!condition)throw new Exception("FAIL: "+message);count++;report.AppendLine("PASS: "+message);}
    static void Set(object target,string name,object value){var t=target.GetType();var f=t.GetField(name,BindingFlags.NonPublic|BindingFlags.Instance);if(f!=null)f.SetValue(target,value);else t.GetProperty(name,BindingFlags.Public|BindingFlags.Instance).GetSetMethod(true).Invoke(target,new[]{value});}
    static async Task Wait(float seconds){var until=DateTime.UtcNow.AddSeconds(seconds);while(DateTime.UtcNow<until)await Task.Delay(16);}
    static void Step(FastingTopDownGame g,int count,Vector2 d,bool prayer){for(int n=0;n<count;n++)g.Tick(.05f,d,prayer);}
    public static async Task<string> Run()
    {
        var g=UnityEngine.Object.FindAnyObjectByType<FastingTopDownGame>();if(!Application.isPlaying||!g)throw new Exception("Play top-down scene first");count=0;report=new System.Text.StringBuilder();Directory.CreateDirectory("Docs/QA/FastingTopDown");
        Keyboard device=null;bool pressed=false;var oldBackground=InputSystem.settings.backgroundBehavior;var oldInput=InputSystem.settings.editorInputBehaviorInPlayMode;
        Action queue=()=>{if(device!=null)InputSystem.QueueStateEvent(device,pressed?new KeyboardState(Key.W):new KeyboardState());};Action current=()=>{if(device!=null)device.MakeCurrent();};
        try{
            Check(g.introduction.activeSelf&&!g.Running,"Starts on five-item guide without advancing simulation");
            g.enabled=false;g.Begin();Check(g.Health==5&&g.StageIndex==0,"Five health; starts in desert");
            Check(g.gameCamera.orthographicSize==8.4375f,"Camera retains 960x540 world scale");
            Check(g.idle.texture.width==64&&g.idle.texture.height==64,"Temporary hero is exactly 64x64");
            Check(g.backgrounds[0].sprite.texture.width==960&&g.backgrounds[0].sprite.texture.height==540,"Background is exactly 960x540");
            Check(g.backgrounds[0].sprite.texture.filterMode==FilterMode.Point,"Point-filtered pixel art");
            Check(g.CanMove(new Vector2(0,1))&&!g.CanMove(new Vector2(8,1))&&!g.CanMove(new Vector2(0,-1)),"Map boundaries permit forward movement and reject outside");
            g.player.position=new Vector3(6,23.7f);g.Tick(.1f,Vector2.up,false);Check(g.player.position.y>23.7f,"Forward movement has no fixed endpoint boundary");g.Begin();
            Step(g,3,Vector2.up,false);Check(g.player.position.y>.2f,"Automatic travel increases world Y");
            var pos=g.player.position;Step(g,10,Vector2.up,true);Check(g.player.position.y>pos.y&&g.Prayer>0,"Prayer channels during forward travel");
            Step(g,Mathf.CeilToInt(g.PrayerTarget/g.settings.prayerPerSecond/.05f)+2,Vector2.zero,true);Check(Mathf.Abs(g.Prayer-g.PrayerTarget)<.01f,"Prayer caps at ten-day target");
            float cap=g.Prayer;Step(g,20,Vector2.zero,true);Check(g.Prayer==cap&&g.StageIndex==0,"No overflow or automatic stage skip before destination");
            g.Begin();var shield=g.entities.First(e=>e.kind==FastingTopDownEntity.Kind.Shield);g.Collect(shield);float hp=g.Health;Check(!g.TryDamage("test")&&!g.Shielded&&g.Health==hp,"Shield automatically consumes and prevents one hit");
            Set(g,"grace",0f);Check(g.TryDamage("test")&&g.Health==4,"Unshielded hit removes one health");Check(!g.TryDamage("repeat")&&g.Health==4,"Grace prevents repeated contact damage");
            Set(g,"slow",2f);Set(g,"root",1f);Set(g,"vision",1f);g.UseWater();Check(!g.HasWater&&g.Health==5&&g.SlowRemaining==0&&g.VisionRemaining==0,"Water clears effects and heals once");
            var cover=g.entities.First(e=>e.stage==0&&e.kind==FastingTopDownEntity.Kind.Cover);g.player.position=cover.transform.position+new Vector3(0,-3f);Check(g.Sheltered&&!g.CanMove(cover.transform.position),"Visible rock blocks movement and protects below");
            g.Begin();Set(g,"vision",g.settings.visionSeconds);g.Refresh();Check(g.visionPanels.All(p=>p.color.a>0&&p.color.a<.6f),"Vision reduction remains translucent");Check(g.visionPanels[0].rectTransform.anchorMax.x<.5f&&g.visionPanels[1].rectTransform.anchorMin.x>.5f,"Immediate surroundings remain uncovered");Step(g,24,Vector2.zero,false);Check(g.VisionRemaining==0,"Vision quickly recovers");
            g.Begin();g.player.position=new Vector3(0,g.settings.stageLength);g.Tick(.01f,Vector2.zero,false);Check(g.StageIndex==0&&!g.GateVisible,"Route continues until prayer requirement is met");
            Set(g,"Prayer",g.PrayerTarget);g.Tick(.01f,Vector2.zero,false);g.player.position=new Vector3(3,g.gate.position.y);g.Tick(.01f,Vector2.zero,false);Step(g,14,Vector2.zero,false);Check(g.StageIndex==1&&g.Prayer==0&&g.player.position.y<1,"Stage transition resets position and prayer");
            Check(g.entities.Where(e=>e.kind==FastingTopDownEntity.Kind.Water).All(e=>e.stage<3),"Final stage has no water source");
            for(int stage=1;stage<4;stage++){Set(g,"Prayer",g.PrayerTarget);g.Tick(.01f,Vector2.zero,false);g.player.position=new Vector3(3,g.gate.position.y);g.Tick(.01f,Vector2.zero,false);Step(g,14,Vector2.zero,false);}
            Check(g.Complete&&g.resultPanel.activeSelf&&!g.Running,"All four stages finish at forty days");
            g.Begin();g.settings.damage=.5f;g.TryDamage("half damage");Check(g.Health==4.5f,"Float damage supports half-health increments");g.settings.damage=1;
            g.Begin();g.SpawnWave();g.SpawnWave();var sand=g.Hazards.FirstOrDefault(h=>h.kind==FastingTopDownHazard.Kind.Sand);Check(sand&&sand.velocity.x<0&&sand.velocity.y==0,"Sand blows right to left in top-down scene");
            Check(cover.windMarker&&cover.ProtectsWind(cover.transform.position+new Vector3(-3f,0)),"Left shelter marker protects against lateral wind");
            var fast=UnityEngine.Object.Instantiate(g.hazardPrefab);fast.warning=0;fast.transform.position=new Vector3(0,3);fast.velocity=new Vector2(0,-10);Check(fast.Advance(.6f,Vector2.zero,Vector2.zero,out _),"Fast moving hazard cannot tunnel through stationary feet");UnityEngine.Object.Destroy(fast.gameObject);
            g.Begin();Set(g,"Health",1f);g.TryDamage("test failure");Step(g,40,Vector2.zero,false);Check(g.Health==5&&g.StageIndex==0,"Failure restarts from day one");
            float estimate=4*(g.settings.finishLineDistance/g.settings.moveSpeed+g.PrayerTarget/g.settings.prayerPerSecond)+1.8f;Check(estimate<60,"Configured no-delay travel plus prayer budget is below sixty seconds: "+estimate.ToString("0.0"));
            g.Begin();g.enabled=true;InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;device=InputSystem.AddDevice<Keyboard>();InputSystem.onBeforeUpdate+=queue;InputSystem.onAfterUpdate+=current;device.MakeCurrent();pressed=true;float start=g.player.position.y;await Wait(.5f);pressed=false;await Wait(.12f);Check(g.player.position.y>start+.1f,"Actual W input moves player in Play");float stopped=g.player.position.y;await Wait(.18f);Check(g.player.position.y>stopped,"Automatic forward travel continues after W release");
            g.enabled=false;g.Begin();g.player.position=new Vector3(0,4);g.Tick(.01f,Vector2.zero,false);g.SpawnWave();g.Refresh();ScreenCapture.CaptureScreenshot(Path.GetFullPath("Docs/QA/FastingTopDown/Gameplay.png"));await Wait(.3f);
            Set(g,"vision",1f);g.Refresh();ScreenCapture.CaptureScreenshot(Path.GetFullPath("Docs/QA/FastingTopDown/PartialVision.png"));await Wait(.3f);
            report.AppendLine("NOTE: sub-minute figure is a configured budget, not a complete human balance playthrough.");File.WriteAllText("Docs/QA/FastingTopDown/Validation.txt",report.ToString());return count+" checks passed";
        }finally{InputSystem.onBeforeUpdate-=queue;InputSystem.onAfterUpdate-=current;if(device!=null)InputSystem.RemoveDevice(device);InputSystem.settings.backgroundBehavior=oldBackground;InputSystem.settings.editorInputBehaviorInPlayMode=oldInput;g.Begin();g.enabled=true;Time.timeScale=1;}
    }
}
