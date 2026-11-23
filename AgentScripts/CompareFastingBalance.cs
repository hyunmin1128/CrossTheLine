using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using Newtonsoft.Json;
using CrossTheLine.Minigames;
public static class CompareFastingBalance
{
    const string Folder="Docs/BalanceTests/2026-11-23";
    static float Dt=.1f;
    static readonly string[] Arms={"Current","Move30","Move30_Target196_216","Move30_Target196_216_Block2"};
    static readonly float[] Horizons={.2f,.45f,.75f,1.1f};
    static FastingTopDownGame game;
    static List<Trial> results=new List<Trial>();
    public sealed class Trial
    {
        public int arm,policy,seed,stage,hits,spawned,rejected,waterPickups;
        public string outcome;
        public float seconds,prayerRatio,health,prayingSeconds,blockedSeconds,rootSeconds,heldSeconds,itemPrayer,directPrayer;
    }
    public static async Task<string> Pilot()=>await Run(2);
    public static async Task<string> Full()=>await Run(6);
    public static async Task<string> FineStep()=>await Run(2,1f/60f);
    public static async Task<string> Timing()
    {
        if(!EditorApplication.isPlaying)throw new Exception("Play mode required");
        string folder=Folder+"/TenMinutes";Directory.CreateDirectory(folder);
        var request=Newtonsoft.Json.Linq.JObject.Parse(File.ReadAllText(folder+"/request.json"));
        string label=(string)request["label"];var targets=request["targets"].ToObject<float[]>();int seeds=(int)request["seeds"],offset=(int)request["seedOffset"];
        Dt=1f/30f;game=UnityEngine.Object.FindAnyObjectByType<FastingTopDownGame>();game.enabled=false;
        var profiles=game.stageConfigurations;var baseline=profiles.Select(JsonUtility.ToJson).ToArray();
        var advanced=profiles.Select(p=>JsonUtility.ToJson(p.advancedSettings)).ToArray();
        var random=UnityEngine.Random.state;string[] keys={"CrossTheLine.Fasting.BestDay","CrossTheLine.Fasting.PreviousDay"};
        var has=keys.Select(PlayerPrefs.HasKey).ToArray();var prefs=keys.Select(k=>PlayerPrefs.GetInt(k)).ToArray();
        string path=game.BalanceReportPath;bool existed=File.Exists(path);byte[] bytes=existed?File.ReadAllBytes(path):null;
        var canvases=game.gameObject.scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Canvas>(true)).ToArray();
        var states=canvases.Select(c=>c.enabled).ToArray();foreach(var c in canvases)c.enabled=false;
        bool feedback=game.hitFeedback.enabled;game.hitFeedback.enabled=false;
        var rows=new List<object>();
        try
        {
            for(int seed=0;seed<seeds;seed++)
            {
                for(int i=0;i<4;i++)profiles[i].prayerTarget=targets[i];
                UnityEngine.Random.InitState(8400+(seed+offset)*97);game.BeginStageForTesting(0);game.enabled=false;
                var visits=new List<object>();float seconds=0,entered=0,decision=0,gateTime=-1;int stage=0;
                string outcome="timeout";Vector2 direction=Vector2.zero;bool prayer=true;
                for(int step=0;step<27000;step++)
                {
                    if(decision<=0){Choose(1,out direction,out prayer);decision=.2f;}decision-=Dt;
                    game.Tick(Dt,direction,prayer);game.hitFeedback.Advance(Dt);seconds+=Dt;
                    if(gateTime<0&&game.GateVisible)gateTime=seconds-entered;
                    if(game.StageIndex!=stage){visits.Add(new{stage=stage+1,seconds=seconds-entered,completed=true,gateAt=gateTime});stage=game.StageIndex;entered=seconds;gateTime=-1;}
                    if(game.Complete){outcome="clear";break;}if(!game.Running){outcome="dead";break;}
                    if(step%20==19)await Task.Yield();
                }
                visits.Add(new{stage=stage+1,seconds=seconds-entered,completed=game.Complete,gateAt=gateTime});
                rows.Add(new{seed=seed+offset,targets,outcome,seconds,lastStage=game.StageIndex+1,health=game.Health,hits=game.DamageHitCount,stages=visits});
                File.WriteAllText(folder+"/"+label+".json",JsonConvert.SerializeObject(rows,Formatting.Indented));
                File.WriteAllText(folder+"/progress.json",JsonConvert.SerializeObject(new{label,finished=rows.Count,total=seeds,last=rows.Last()},Formatting.Indented));
                await Task.Yield();
            }
            return "Finished "+label+": "+rows.Count+" continuous journeys at 30 Hz";
        }
        finally
        {
            for(int i=0;i<profiles.Length;i++)JsonUtility.FromJsonOverwrite(baseline[i],profiles[i]);
            game.BeginStageForTesting(0);game.enabled=false;game.hitFeedback.enabled=feedback;
            for(int i=0;i<canvases.Length;i++)if(canvases[i])canvases[i].enabled=states[i];
            UnityEngine.Random.state=random;for(int i=0;i<keys.Length;i++){if(has[i])PlayerPrefs.SetInt(keys[i],prefs[i]);else PlayerPrefs.DeleteKey(keys[i]);}PlayerPrefs.Save();
            if(existed)File.WriteAllBytes(path,bytes);else if(File.Exists(path))File.Delete(path);
            File.WriteAllText(folder+"/"+label+"-restored.json",JsonConvert.SerializeObject(new{profilesRestored=profiles.Select(JsonUtility.ToJson).SequenceEqual(baseline),advancedUntouched=profiles.Select(p=>JsonUtility.ToJson(p.advancedSettings)).SequenceEqual(advanced),prefsRestored=true,telemetryRestored=true,trialCount=rows.Count},Formatting.Indented));
        }
    }
    public static async Task<string> Journey()
    {
        if(!EditorApplication.isPlaying)throw new Exception("Play mode required");
        string folder=Folder+"/TargetOnly";Directory.CreateDirectory(folder);Dt=1f/30f;
        game=UnityEngine.Object.FindAnyObjectByType<FastingTopDownGame>();game.enabled=false;
        var profiles=game.stageConfigurations;var json=profiles.Select(JsonUtility.ToJson).ToArray();
        var advanced=profiles.Select(p=>JsonUtility.ToJson(p.advancedSettings)).ToArray();
        var random=UnityEngine.Random.state;string[] keys={"CrossTheLine.Fasting.BestDay","CrossTheLine.Fasting.PreviousDay"};
        var has=keys.Select(PlayerPrefs.HasKey).ToArray();var prefs=keys.Select(k=>PlayerPrefs.GetInt(k)).ToArray();
        string path=game.BalanceReportPath;bool existed=File.Exists(path);byte[] bytes=existed?File.ReadAllBytes(path):null;
        var canvases=game.gameObject.scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Canvas>(true)).ToArray();
        var canvasStates=canvases.Select(c=>c.enabled).ToArray();foreach(var c in canvases)c.enabled=false;
        bool feedback=game.hitFeedback.enabled;game.hitFeedback.enabled=false;
        var rows=new List<object>();
        try
        {
            for(int seed=0;seed<6;seed++)for(int arm=0;arm<2;arm++)
            {
                for(int i=0;i<profiles.Length;i++)JsonUtility.FromJsonOverwrite(json[i],profiles[i]);
                profiles[2].prayerTarget=arm==0?234:196;profiles[3].prayerTarget=arm==0?252:216;
                UnityEngine.Random.InitState(8400+seed*97);game.BeginStageForTesting(0);game.enabled=false;
                var visits=new List<object>();float seconds=0,entered=0,held=0,decision=0;int stage=0;
                object at180=null;string outcome="timeout";Vector2 direction=Vector2.zero;bool prayer=true;
                for(int step=0;step<18000;step++)
                {
                    if(decision<=0){Choose(1,out direction,out prayer);decision=.2f;}decision-=Dt;
                    if(prayer)held+=Dt;game.Tick(Dt,direction,prayer);game.hitFeedback.Advance(Dt);seconds+=Dt;
                    if(at180==null&&seconds>=180)at180=new{stage=game.StageIndex+1,health=game.Health,prayer=game.Prayer,target=game.PrayerTarget};
                    if(game.StageIndex!=stage){visits.Add(new{stage=stage+1,seconds=seconds-entered,exitHealth=game.Health});stage=game.StageIndex;entered=seconds;}
                    if(game.Complete){outcome="clear";break;}if(!game.Running){outcome="dead";break;}
                    if(step%20==19)await Task.Yield();
                }
                visits.Add(new{stage=stage+1,seconds=seconds-entered,exitHealth=game.Health});
                rows.Add(new{seed,arm,setting=arm==0?"Original234_252":"TargetOnly196_216",outcome,seconds,lastStage=game.StageIndex+1,health=game.Health,hits=game.DamageHitCount,heldSeconds=held,at180,stages=visits});
                File.WriteAllText(folder+"/continuous-trials.json",JsonConvert.SerializeObject(rows,Formatting.Indented));
                File.WriteAllText(folder+"/progress.json",JsonConvert.SerializeObject(new{finished=rows.Count,total=12,last=rows.Last()},Formatting.Indented));
                await Task.Yield();
            }
            return "Completed 12 continuous four-stage automated trials at 30 Hz; "+folder;
        }
        finally
        {
            for(int i=0;i<profiles.Length;i++)JsonUtility.FromJsonOverwrite(json[i],profiles[i]);
            game.BeginStageForTesting(0);game.enabled=false;game.hitFeedback.enabled=feedback;
            for(int i=0;i<canvases.Length;i++)if(canvases[i])canvases[i].enabled=canvasStates[i];
            UnityEngine.Random.state=random;for(int i=0;i<keys.Length;i++){if(has[i])PlayerPrefs.SetInt(keys[i],prefs[i]);else PlayerPrefs.DeleteKey(keys[i]);}PlayerPrefs.Save();
            if(existed)File.WriteAllBytes(path,bytes);else if(File.Exists(path))File.Delete(path);
            File.WriteAllText(folder+"/restored.json",JsonConvert.SerializeObject(new{profilesRestored=profiles.Select(JsonUtility.ToJson).SequenceEqual(json),advancedUntouched=profiles.Select(p=>JsonUtility.ToJson(p.advancedSettings)).SequenceEqual(advanced),prefsRestored=true,telemetryRestored=true,trialCount=rows.Count},Formatting.Indented));
        }
    }
    static async Task<string> Run(int seedCount,float stepSize=.1f)
    {
        Dt=stepSize;string suffix=Dt<.02f?"-60hz":"";
        if(!EditorApplication.isPlaying)throw new Exception("Play mode required");
        Directory.CreateDirectory(Folder);game=UnityEngine.Object.FindAnyObjectByType<FastingTopDownGame>();
        game.enabled=false;var profiles=game.stageConfigurations;
        var baseline=profiles.Select(JsonUtility.ToJson).ToArray();
        var originalAdvanced=profiles.Select(p=>p.advancedSettings).ToArray();
        var advancedJson=originalAdvanced.Select(JsonUtility.ToJson).ToArray();
        var clones=originalAdvanced.Select(UnityEngine.Object.Instantiate).ToArray();
        var random=UnityEngine.Random.state;
        string[] prefKeys={"CrossTheLine.Fasting.BestDay","CrossTheLine.Fasting.PreviousDay"};
        var hadKeys=prefKeys.Select(PlayerPrefs.HasKey).ToArray();var savedPrefs=prefKeys.Select(k=>PlayerPrefs.GetInt(k)).ToArray();
        string telemetry=game.BalanceReportPath;bool hadTelemetry=File.Exists(telemetry);byte[] savedTelemetry=hadTelemetry?File.ReadAllBytes(telemetry):null;
        var canvases=game.gameObject.scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Canvas>(true)).ToArray();
        var canvasEnabled=canvases.Select(c=>c.enabled).ToArray();foreach(var c in canvases)c.enabled=false;
        bool feedbackEnabled=game.hitFeedback.enabled;game.hitFeedback.enabled=false;
        results.Clear();int total=4*2*4*seedCount;
        try
        {
            for(int seed=0;seed<seedCount;seed++)for(int policy=0;policy<2;policy++)for(int stage=0;stage<4;stage++)for(int arm=0;arm<4;arm++)
            {
                for(int i=0;i<profiles.Length;i++)
                {
                    JsonUtility.FromJsonOverwrite(baseline[i],profiles[i]);JsonUtility.FromJsonOverwrite(advancedJson[i],clones[i]);profiles[i].advancedSettings=clones[i];
                    if(arm>0)profiles[i].prayerMoveMultiplier=.3f;
                    if(arm>=2&&i==2)profiles[i].prayerTarget=196;
                    if(arm>=2&&i==3)profiles[i].prayerTarget=216;
                    if(arm==3){clones[i].burnPrayerBlockSeconds=2;clones[i].tombPrayerBlockSeconds=2;}
                }
                UnityEngine.Random.InitState(8400+seed*97+stage*7919);
                game.BeginStageForTesting(stage);game.enabled=false;
                var t=new Trial{arm=arm,policy=policy,seed=seed,stage=stage};var balance=game.CurrentBalance;
                Vector2 direction=Vector2.zero;bool prayer=true;float decision=0;float lastRatio=0;
                for(int step=0;step<Mathf.CeilToInt(180/Dt);step++)
                {
                    if(decision<=0){Choose(policy,out direction,out prayer);decision=.2f;}decision-=Dt;
                    t.blockedSeconds+=game.PrayerBlockRemaining>0?Dt:0;t.rootSeconds+=game.RootStatusRemaining>0?Dt:0;t.heldSeconds+=prayer?Dt:0;
                    game.Tick(Dt,direction,prayer);game.hitFeedback.Advance(Dt);
                    t.seconds+=Dt;lastRatio=game.Prayer/game.PrayerTarget;
                    if(game.StageIndex!=stage||game.Complete){t.outcome="clear";lastRatio=1;break;}
                    if(!game.Running){t.outcome="dead";break;}
                    if(step%20==19)await Task.Yield();
                }
                if(t.outcome==null)t.outcome="timeout";
                t.health=game.Health;t.prayerRatio=lastRatio;t.hits=game.DamageHitCount;t.prayingSeconds=game.DirectPrayerSeconds;
                t.spawned=balance.spawned.Sum();t.rejected=balance.rejected.Sum()+balance.suppressedGroups;
                t.waterPickups=balance.pickups[(int)FastingTopDownEntity.Kind.Water];t.itemPrayer=balance.itemPrayer;t.directPrayer=balance.directPrayer;
                results.Add(t);
                File.WriteAllText(Folder+"/progress.json",JsonConvert.SerializeObject(new{finished=results.Count,total,last=t,arm=Arms[arm],seedCount},Formatting.Indented));
                File.WriteAllText(Folder+"/trials-"+seedCount+"seeds"+suffix+".json",JsonConvert.SerializeObject(results,Formatting.Indented));
                await Task.Yield();
            }
            return "Finished "+results.Count+" seeded Play-mode fixed-step automated trials; reports="+Folder;
        }
        finally
        {
            for(int i=0;i<profiles.Length;i++){JsonUtility.FromJsonOverwrite(baseline[i],profiles[i]);profiles[i].advancedSettings=originalAdvanced[i];UnityEngine.Object.Destroy(clones[i]);}
            game.BeginStageForTesting(0);game.enabled=false;game.hitFeedback.enabled=feedbackEnabled;
            for(int i=0;i<canvases.Length;i++)if(canvases[i])canvases[i].enabled=canvasEnabled[i];
            UnityEngine.Random.state=random;
            for(int i=0;i<prefKeys.Length;i++){if(hadKeys[i])PlayerPrefs.SetInt(prefKeys[i],savedPrefs[i]);else PlayerPrefs.DeleteKey(prefKeys[i]);}PlayerPrefs.Save();
            if(hadTelemetry)File.WriteAllBytes(telemetry,savedTelemetry);else if(File.Exists(telemetry))File.Delete(telemetry);
            File.WriteAllText(Folder+"/restored.json",JsonConvert.SerializeObject(new{profilesRestored=profiles.Select(JsonUtility.ToJson).SequenceEqual(baseline),advancedAssetsUntouched=originalAdvanced.Select(JsonUtility.ToJson).SequenceEqual(advancedJson),userPrefsRestored=true,telemetryRestored=true,trialCount=results.Count},Formatting.Indented));
        }
    }
    static void Choose(int policy,out Vector2 direction,out bool prayer)
    {
        direction=Vector2.zero;prayer=true;float best=float.PositiveInfinity;
        bool obscured=game.VisionRemaining>0||game.DarknessProtecting;
        var player=(Vector2)game.player.position;float cameraY=game.gameCamera.transform.position.y;
        var visible=game.Hazards.Where(h=>h&&!h.spent&&h.body.enabled&&h.transform.position.y<cameraY+game.gameCamera.orthographicSize+1&&(!obscured||(Mathf.Abs(h.CollisionCenter.x-player.x)<3.2f&&Mathf.Abs(h.CollisionCenter.y-player.y)<3.6f))).ToArray();
        for(int p=0;p<(policy==0?1:2);p++)for(int x=-1;x<=1;x++)for(int y=-1;y<=1;y++)
        {
            bool pray=p==0;var input=Vector2.ClampMagnitude(new Vector2(x,y),1);
            float speed=game.settings.manualMoveSpeed*(pray?game.settings.prayerMoveMultiplier:1)*(game.SlowStatusRemaining>0?.55f:1)*(game.WaterBoostRemaining>0?1.3f:1);
            var motion=game.RootStatusRemaining>0?Vector2.zero:input*speed;
            float score=pray?-.8f:.6f;
            foreach(float time in Horizons)
            {
                var predicted=player+motion*time+Vector2.up*game.CurrentSpeed*time;
                var area=game.playArea.RootArea;area.y+=game.CurrentSpeed*time;
                if(predicted.x<area.xMin||predicted.x>area.xMax||predicted.y<area.yMin||predicted.y>area.yMax)score+=15;
                predicted.x=Mathf.Clamp(predicted.x,area.xMin,area.xMax);predicted.y=Mathf.Clamp(predicted.y,area.yMin,area.yMax);
                foreach(var h in visible)
                {
                    if(h.warning>time)continue;
                    var at=h.CollisionCenter+h.velocity*time;
                    var sweep=h.GetComponent<FastingReaperSweep>();
                    if(sweep&&sweep.Choreographed&&sweep.CurrentPhase!=FastingReaperSweep.AttackPhase.Dashing)at=h.CollisionCenter+Vector2.down*sweep.dashSpeed*Mathf.Max(0,time-h.warning);
                    var extent=h.size*.5f+new Vector2(.4f,.32f);var delta=predicted-at;
                    float distance=Mathf.Max(Mathf.Abs(delta.x)/extent.x,Mathf.Abs(delta.y)/extent.y);
                    float danger=distance<1?500:distance<1.5f?(1.5f-distance)*25:0;
                    if(h.kind==FastingTopDownHazard.Kind.Wall&&game.StageIndex==2)danger*=1.7f;
                    score+=danger*(game.Invulnerable?.2f:1);
                }
                float desiredY=cameraY+game.CurrentSpeed*time-1.5f;
                score+=Mathf.Abs(predicted.y-desiredY)*.04f+Mathf.Abs(predicted.x)*.012f;
                if(game.GateVisible)score+=Mathf.Max(0,game.gate.position.y-predicted.y)*.8f;
            }
            foreach(var item in game.entities)
            {
                if(!item||!item.gameObject.activeSelf||item.consumed||item.stage!=game.StageIndex||item.kind==FastingTopDownEntity.Kind.Cover)continue;
                var delta=(Vector2)item.transform.position-player;if(delta.y<-.5f||delta.y>7||Mathf.Abs(delta.x)>8)continue;
                float weight=item.kind==FastingTopDownEntity.Kind.Water?(game.HasWater?0:1.4f):item.kind==FastingTopDownEntity.Kind.Shield?1.0f:.55f;
                score+=weight*(Vector2.Distance(player+motion*.7f,(Vector2)item.transform.position)-delta.magnitude);
            }
            if(score<best){best=score;direction=input;prayer=pray;}
        }
        if(game.HasWater&&(game.Health<=2||game.PrayerBlockRemaining>1||game.RootStatusRemaining>.5f))game.UseWater();
    }
}
