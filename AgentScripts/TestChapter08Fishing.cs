using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Newtonsoft.Json;
using CrossTheLine.Minigames.Fishing;
public static class TestChapter08Fishing
{
    static void Check(bool condition,string name){if(!condition)throw new Exception(name);}
    static void ToFight(NetFishingSession s){for(int i=0;i<1000&&s.State!=NetFishingSession.Phase.Fighting;i++){if(s.State==NetFishingSession.Phase.Exploring)s.Cast();s.Step(1f/60,false);}Check(s.State==NetFishingSession.Phase.Fighting,"Fight must begin");}
    public static string Behavior()
    {
        var asset=AssetDatabase.LoadAssetAtPath<NetFishingSettings>("Assets/Settings/Minigames/Chapter08Fishing.asset");var cfg=UnityEngine.Object.Instantiate(asset);var result=new Dictionary<string,object>();
        try
        {
            cfg.startingProgress=.95f;cfg.progressPerGoodTap=1;cfg.catchZoneHeight=.8f;cfg.tapLift=.05f;
            var s=new NetFishingSession(cfg,42);s.Start();ToFight(s);s.Step(1f/60,true);Check(s.LastCatch>=1&&s.LastCatch<=15,"Catch range");int before=s.FishCaught;
            cfg.catchZoneHeight=.1f;cfg.tapLift=.4f;cfg.badTapNetDamage=100;ToFight(s);s.Step(1f/60,true);Check(s.State==NetFishingSession.Phase.Replacing,"Broken net enters replacement");Check(s.FishCaught==before,"Fish preserved after break");
            for(int i=0;i<350;i++)s.Step(1f/60,false);Check(s.State==NetFishingSession.Phase.Replacing,"Cannot cast during replacement");for(int i=0;i<15;i++)s.Step(1f/60,false);Check(s.State==NetFishingSession.Phase.Exploring&&s.Integrity==1,"Replacement recovers after six seconds");result["brokenNet"]=new{preservedFish=before,replacementSeconds=cfg.replacementSeconds,passed=true};
            cfg.progressPerGoodTap=0;cfg.progressLossPerSecond=0;cfg.netDamagePerSecond=0;cfg.badTapNetDamage=0;
            s=new NetFishingSession(cfg,43);s.Start();s.Cast();for(int i=0;i<2000&&s.State!=NetFishingSession.Phase.Result;i++)s.Step(1f/60,false);Check(s.State==NetFishingSession.Phase.Result&&s.Escapes==1,"Thirty second timeout");Check(s.CastElapsed<=30.03f,"Full cast including bite is limited to thirty seconds");result["timeout"]=new{s.CastElapsed,s.FightTime,s.Escapes};
            cfg.startingProgress=.001f;cfg.progressLossPerSecond=1;cfg.zoneFallSpeed=5;
            s=new NetFishingSession(cfg,44);s.Start();ToFight(s);for(int i=0;i<30&&s.State==NetFishingSession.Phase.Fighting;i++)s.Step(1f/60,false);Check(s.State==NetFishingSession.Phase.Result&&s.BrokenNets==0,"Zero progress loses only fish");result["escapeWithoutBrokenNet"]=true;
        }
        finally{UnityEngine.Object.DestroyImmediate(cfg);}
        string json=JsonConvert.SerializeObject(result,Formatting.Indented);File.WriteAllText("Docs/Chapter08/behavior-tests.json",json);return json;
    }
    public static string Balance()
        =>RunBalance(new[]{.016f,.017f,.018f},30,"balance-trials");
    public static string FinalBalance()
        =>RunBalance(new[]{.0155f},100,"balance-final");
    static string RunBalance(float[] gains,int seedCount,string name)
    {
        Directory.CreateDirectory("Docs/Chapter08");var original=AssetDatabase.LoadAssetAtPath<NetFishingSettings>("Assets/Settings/Minigames/Chapter08Fishing.asset");var cfg=UnityEngine.Object.Instantiate(original);var rows=new List<object>();
        try
        {
            foreach(float gain in gains)for(int policy=0;policy<2;policy++)for(int seed=0;seed<seedCount;seed++)
            {
                cfg.progressPerGoodTap=gain;var s=new NetFishingSession(cfg,1500+seed);s.Start();float decision=0;bool tap=false;var rng=new System.Random(seed+999);
                float maxFight=0;int steps=0;
                while(s.State!=NetFishingSession.Phase.Complete&&s.Elapsed<1800)
                {
                    tap=false;
                    if(s.State==NetFishingSession.Phase.Exploring&&s.PhaseTime>=3)s.Cast();
                    if(s.State==NetFishingSession.Phase.Fighting)
                    {
                        decision-=1f/60;
                        if(decision<=0){decision=policy==0?.1f:.22f;float predicted=s.Zone-cfg.zoneFallSpeed/60+cfg.tapLift;tap=predicted<=s.Fish+cfg.catchZoneHeight*.5f-.01f&&s.Zone<s.Fish+.025f;if(policy==1&&rng.NextDouble()<.05)tap=!tap;}
                    }
                    s.Step(1f/60,tap);maxFight=Mathf.Max(maxFight,s.FightTime);steps++;
                }
                rows.Add(new{gain,policy,seed,outcome=s.State.ToString(),seconds=s.Elapsed,s.Casts,s.Escapes,s.BrokenNets,s.FishCaught,maxFight});
            }
        }
        finally{UnityEngine.Object.DestroyImmediate(cfg);}
        File.WriteAllText("Docs/Chapter08/"+name+".json",JsonConvert.SerializeObject(rows,Formatting.Indented));return rows.Count+" deterministic full-run model tests finished; no gameplay assets modified";
    }
}
