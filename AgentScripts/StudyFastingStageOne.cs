using System;
using System.Linq;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using CrossTheLine.Minigames;
public static class StudyFastingStageOne
{
    class Result{public string mode;public int seed;public bool success;public float seconds,health;public string reason;}
    static void Set(object g,string n,object v)=>g.GetType().GetField(n,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(g,v);
    static float Median(float[] values)=>values.Length==0?float.NaN:values.OrderBy(x=>x).ElementAt(values.Length/2);
    static Vector2 Pilot(FastingTopDownGame g,bool collect)
    {
        float px=g.player.position.x,py=g.player.position.y,best=float.PositiveInfinity,target=px;
        for(int lane=0;lane<5;lane++){
            float x=(lane-2)*g.settings.laneSpacing,score=Mathf.Abs(x-px)*.12f,travel=Mathf.Abs(x-px)/g.settings.manualMoveSpeed;
            foreach(var h in g.Hazards){if(!h||h.spent)continue;float close=g.CurrentSpeed-h.velocity.y;float t=(h.transform.position.y-py-(h.size.y*.5f+.2f))/Mathf.Max(.01f,close);if(t<-.35f||t>2.2f)continue;float left=h.transform.position.x-h.size.x*.5f-.35f,right=h.transform.position.x+h.size.x*.5f+.35f;if(x>left&&x<right)score+=t<.5f?100:24/(.4f+t);if(t<travel+.1f&&t>=0&&Mathf.Max(px,x)>left&&Mathf.Min(px,x)<right)score+=35;}
            if(collect)foreach(var e in g.entities){if(!e||!e.gameObject.activeSelf||e.consumed)continue;float dy=e.transform.position.y-py;if(dy<-.4f||dy>9||Mathf.Abs(e.transform.position.x-x)>.9f)continue;float value=e.kind==FastingTopDownEntity.Kind.Book?10:e.kind==FastingTopDownEntity.Kind.Page?4:e.kind==FastingTopDownEntity.Kind.Fragment?2:e.kind==FastingTopDownEntity.Kind.Flame?1.4f:e.kind==FastingTopDownEntity.Kind.Water&&g.Health<4?3:.6f;score-=value/(1+Mathf.Max(0,dy)*.25f);}
            if(score<best){best=score;target=x;}
        }
        return new Vector2(Mathf.Clamp((target-px)/(g.settings.manualMoveSpeed*.05f),-1,1),0);
    }
    public static async Task<string> Run()
    {
        var g=UnityEngine.Object.FindAnyObjectByType<FastingTopDownGame>();if(!Application.isPlaying||!g)throw new Exception("Play stage-one test scene");var savedRandom=UnityEngine.Random.state;var results=new List<Result>();var notes=new System.Text.StringBuilder();Directory.CreateDirectory("Docs/QA/FastingTopDown/TimingStudy-20261110");int checks=0;
        Action<bool,string> check=(ok,s)=>{if(!ok)throw new Exception(s);checks++;notes.AppendLine("PASS: "+s);};
        try{
            g.enabled=false;g.Begin();float low=float.PositiveInfinity,high=0;for(int i=0;i<2000;i++){float delay=g.settings.NextSpawnInterval(0);low=Mathf.Min(low,delay);high=Mathf.Max(high,delay);check(delay>=1.8f-.001f&&delay<=3.24f+.001f,"Spawn interval in configured range");}notes.Clear();notes.AppendLine($"2000 interval samples: {low:0.000}..{high:0.000}s");
            var kinds=new Dictionary<FastingTopDownHazard.Kind,int>();var choose=typeof(FastingTopDownGame).GetMethod("ChooseKind",BindingFlags.Instance|BindingFlags.NonPublic);for(int i=0;i<2000;i++){var kind=(FastingTopDownHazard.Kind)choose.Invoke(g,null);if(!kinds.ContainsKey(kind))kinds[kind]=0;kinds[kind]++;}check(kinds.Count==4,"All four stage-one hazard kinds sampled");foreach(var pair in kinds)notes.AppendLine(pair.Key+": "+pair.Value+" / 2000");
            var scenarios=new[]{"NoItems","FragmentEvery30s","PageEvery40s","FlameEvery20s","BibleAt30s","MixedItems","FiveHitsEvery20s"};
            foreach(string mode in scenarios){g.Begin();Set(g,"wave",float.MaxValue);float next=mode=="FragmentEvery30s"?30:mode=="PageEvery40s"?40:20;bool bible=false;int ticks=0;while(g.Elapsed<240&&g.StageIndex==0&&g.Health>0){foreach(var e in g.entities)e.gameObject.SetActive(false);float t=g.Elapsed;if(mode=="BibleAt30s"&&!bible&&t>=30){g.Collect(g.entities.First(e=>e.kind==FastingTopDownEntity.Kind.Book));bible=true;}if(t>=next){if(mode=="FragmentEvery30s")g.Collect(g.entities.First(e=>e.kind==FastingTopDownEntity.Kind.Fragment));if(mode=="PageEvery40s")g.Collect(g.entities.First(e=>e.kind==FastingTopDownEntity.Kind.Page));if(mode=="FlameEvery20s")g.Collect(g.entities.First(e=>e.kind==FastingTopDownEntity.Kind.Flame));if(mode=="MixedItems")g.Collect(g.entities.First(e=>e.kind==(Mathf.RoundToInt(next)%40==0?FastingTopDownEntity.Kind.Page:FastingTopDownEntity.Kind.Flame)));if(mode=="FiveHitsEvery20s")g.TryDamage("Controlled hit");next+=mode=="FragmentEvery30s"?30:mode=="PageEvery40s"?40:20;}g.Tick(.05f,Vector2.zero);if(++ticks%250==0)await Task.Yield();}results.Add(new Result{mode=mode,seed=-1,success=g.StageIndex>0,seconds=g.Elapsed,health=g.Health,reason=g.Message});await Task.Yield();}
            foreach(string mode in new[]{"NoInput","AvoidOnly","SeekItems"})for(int seed=0;seed<20;seed++){UnityEngine.Random.InitState(1000+seed);g.Begin();int ticks=0;while(g.Elapsed<240&&g.StageIndex==0&&g.Health>0){if(mode!="NoInput"&&g.HasWater&&(g.Health<=3||g.PrayerBlockRemaining>.2f))g.UseWater();var direction=mode=="NoInput"?Vector2.zero:Pilot(g,mode=="SeekItems");g.Tick(.05f,direction);if(++ticks%250==0)await Task.Yield();}results.Add(new Result{mode=mode,seed=seed,success=g.StageIndex>0,seconds=g.Elapsed,health=g.Health,reason=g.Message});await Task.Yield();}
            string dir="Docs/QA/FastingTopDown/TimingStudy-20261110/";File.WriteAllLines(dir+"Runs.csv",new[]{"mode,seed,success,seconds,health"}.Concat(results.Select(r=>$"{r.mode},{r.seed},{r.success},{r.seconds:0.00},{r.health:0.0}")));
            var text=new System.Text.StringBuilder("# Stage-one timing study\n\nControlled Unity Tick simulation, dt=0.05s. Three simple automated policies,20 seeds each. Not human playtesting or exhaustive state-space coverage. Failed runs are excluded from completion-time statistics. No-input is a baseline, not a player ability estimate. SeekItems bot uses horizontal input only and can make poor choices.\n\n");text.AppendLine("| Mode | Complete / runs | Complete min / median / max seconds | Failed median seconds |");text.AppendLine("|---|---:|---|---:|");foreach(var group in results.GroupBy(r=>r.mode)){var won=group.Where(r=>r.success).Select(r=>r.seconds).ToArray();var lost=group.Where(r=>!r.success).Select(r=>r.seconds).ToArray();text.AppendLine($"| {group.Key} | {won.Length}/{group.Count()} | {(won.Length>0?$"{won.Min():0.0} / {Median(won):0.0} / {won.Max():0.0}":"none")} | {(lost.Length>0?Median(lost).ToString("0.0"):"none")} |");}text.AppendLine("\nControlled scenario item timing is injected for comparison; actual drops follow the current configured probabilities.\n");text.AppendLine(notes.ToString());File.WriteAllText(dir+"Summary.md",text.ToString());return text.ToString();
        }finally{UnityEngine.Random.state=savedRandom;g.Begin();g.enabled=true;Time.timeScale=1;}
    }
}
