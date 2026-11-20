using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using CrossTheLine.Minigames;
public static class TestFastingFairness
{
    static readonly BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
    static void Field(object o,string name,object value)=>o.GetType().GetField(name,Flags).SetValue(o,value);
    static void Quiet(FastingTopDownGame g){foreach(var e in g.entities)e.gameObject.SetActive(false);foreach(var name in new[]{"remaining","nextWave","nextRandom"})Field(g.encounterDirector,name,1000f);}
    public static string Run(){
        if(!Application.isPlaying)throw new Exception("Play required");var g=UnityEngine.Object.FindAnyObjectByType<FastingTopDownGame>();g.enabled=false;int n=0;Action<bool,string> ok=(b,s)=>{if(!b)throw new Exception(s);n++;};var rng=UnityEngine.Random.state;
        try{
            UnityEngine.Random.InitState(191100);
            ok(g.SpawnLaneCount==10,"Whole viewport has10 spawn columns");
            for(int stage=0;stage<4;stage++)for(int lane=0;lane<g.SpawnLaneCount;lane++){
                g.BeginStageForTesting(stage);Quiet(g);g.encounterDirector.ReserveSweepEscape((lane+3)%g.SpawnLaneCount);
                var kind=stage==0?FastingTopDownHazard.Kind.Rock:stage==1?FastingTopDownHazard.Kind.Collapse:stage==2?FastingTopDownHazard.Kind.Poison:FastingTopDownHazard.Kind.Fire;
                g.SpawnPatternObstacle(kind,lane,1,0);ok(g.Hazards.Any(h=>Mathf.Abs(h.transform.position.x-g.SpawnLaneX(lane))<.01f),"Spawn covers stage"+stage+" lane"+lane);
            }
            g.BeginStageForTesting(3);Quiet(g);var mask=g.caveVisionMask;mask.Set(1,new Vector2(.5f,.5f),g.gameCamera);ok(mask.Opacity==0,"Vignette starts clear");mask.Advance(.3f);ok(mask.Opacity>0&&mask.Opacity<1,"Gradual edge darkness");mask.Advance(.65f);ok(mask.Opacity==1&&g.DarknessProtecting,"Brief full blackout protected");ok(!g.TryDamage("Reaper"),"Invisible attacks cannot damage");mask.Advance(.5f);ok(mask.Opacity>0&&mask.Opacity<1,"Gradual reveal");mask.Advance(.5f);ok(mask.Opacity==0,"Darkness fully clears");
            for(int pass=0;pass<2;pass++){
                g.BeginStageForTesting(2);Quiet(g);g.SpawnPatternObstacle(FastingTopDownHazard.Kind.Wall,0,1,0);ok(g.Hazards.Count==9,"Single9-piece roadblock");
                var wall=g.Hazards.First(h=>!h.verticalWall);var side=g.Hazards.First(h=>h.verticalWall);ok(wall.size.y<.81f&&wall.body.bounds.size.y>wall.size.y*3,"Front face excluded from floor collision");ok(Mathf.Abs(wall.size.y-side.size.x)<.06f,"Both wall orientations have matching floor thickness");
                float exitX=g.Hazards.Where(h=>!h.verticalWall).Average(h=>h.transform.position.x)<0?11.5f:-11.5f;
                float finish=g.Hazards.Max(h=>h.CollisionCenter.y+h.size.y*.5f)+1;
                bool clear=false;for(int frame=0;frame<400;frame++){
                    var direction=Mathf.Abs(g.player.position.x-exitX)>.2f?new Vector2(Mathf.Sign(exitX-g.player.position.x),0):Vector2.up;
                    g.Tick(.05f,direction);g.hitFeedback.Advance(.05f);ok(g.playArea.Contains(g.player.position),"Camera and wall do not push player outside");
                    foreach(var h in g.Hazards.Where(h=>h&&h.kind==FastingTopDownHazard.Kind.Wall))ok(!new Rect(h.CollisionCenter-h.size*.5f,h.size).Contains(g.player.position),"No wall tunneling");
                    if(g.player.position.y>finish){clear=true;break;}
                }
                ok(clear,"Actual scrolling/movement clears roadblock, pass"+pass);ok(g.Health==5,"Route does not require damage");
            }
            g.BeginStageForTesting(3);Quiet(g);foreach(var name in new[]{"slow","root","prayerLock","reverse","vision","grace"})Field(g,name,2f);
            var particles=g.player.GetComponentInChildren<FastingStatusParticles>();particles.Refresh(.1f);ok(particles.VisibleStatusCount==6,"Six status particle shapes active");foreach(var name in new[]{"slow","root","prayerLock","reverse","vision","grace"})Field(g,name,0f);particles.Refresh(.1f);ok(particles.VisibleStatusCount==0,"State particles expire");
            g.BeginStageForTesting(0);Quiet(g);for(int i=0;i<10;i++)g.Tick(.1f,Vector2.zero,true);ok(g.CurrentBalance.directPrayer>0&&g.CurrentBalance.seconds>.9f,"Time and prayer recorded");g.TryChooseObstacle(out _);g.SpawnPatternObstacle(FastingTopDownHazard.Kind.Rock,0,1,0);g.ExportBalanceReport();ok(System.IO.File.Exists(g.BalanceReportPath),"Report saved");var report=System.IO.File.ReadAllText(g.BalanceReportPath);ok(report.Contains("selected")&&report.Contains("rejectedForOverlap")&&report.Contains("directPrayer"),"Balance fields exported");
            return n+" checks passed: full-width spawns, vignette phases/protection, sprite-matched floor hitboxes, actual camera/movement roadblock clear both directions, state particles and telemetry. Report="+g.BalanceReportPath;
        }finally{UnityEngine.Random.state=rng;Time.timeScale=1;g.BeginStageForTesting(0);g.enabled=true;}
    }
}
