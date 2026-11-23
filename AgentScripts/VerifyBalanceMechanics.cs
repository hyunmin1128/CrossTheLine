using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Newtonsoft.Json;
using CrossTheLine.Minigames;
using CrossTheLine.Cana;
public static class VerifyBalanceMechanics
{
    static FastingTopDownGame g;
    static BindingFlags flags=BindingFlags.Instance|BindingFlags.NonPublic;
    static void Set(string name,object value)=>typeof(FastingTopDownGame).GetField(name,flags).SetValue(g,value);
    static void Invoke(string name,params object[] args)=>typeof(FastingTopDownGame).GetMethod(name,flags).Invoke(g,args);
    public static string Run()
    {
        g=UnityEngine.Object.FindAnyObjectByType<FastingTopDownGame>();g.enabled=false;
        var data=new Dictionary<string,object>();
        string telemetry=g.BalanceReportPath;bool hadTelemetry=File.Exists(telemetry);byte[] telemetryBytes=hadTelemetry?File.ReadAllBytes(telemetry):null;
        Action reset=()=>{g.BeginStageForTesting(0);g.enabled=false;Set("wave",9999f);if(g.encounterDirector)g.encounterDirector.enabled=false;foreach(var e in g.entities)if(e)e.gameObject.SetActive(false);};
        reset();var director=g.encounterDirector;g.encounterDirector=null;
        try
        {
            var moves=new List<object>();
            foreach(float multiplier in new[]{.15f,.30f})
            {
                reset();g.settings.prayerMoveMultiplier=multiplier;float start=g.player.position.x;
                for(int i=0;i<10;i++)g.Tick(.1f,Vector2.right,true);
                moves.Add(new{multiplier,distance=g.player.position.x-start,prayer=g.Prayer});
            }
            data["prayerMovementOneSecond"]=moves;
            reset();for(int i=0;i<3;i++)Invoke("CollectSpiritFlame");float activated=g.FeverRemaining;
            for(int i=0;i<30;i++)g.Tick(.1f,Vector2.zero,false);float afterNoPrayer=g.FeverRemaining;
            Invoke("CollectSpiritFlame");float refill=g.FeverRemaining;Invoke("CollectSpiritFlame");float cap=g.FeverRemaining;
            for(int i=0;i<61;i++)g.Tick(.1f,Vector2.zero,false);
            data["fever"]=new{activated,afterNoPrayer,refill,cap,ended=!g.FeverActive};
            var locks=new List<object>();
            foreach(float duration in new[]{3f,2f})foreach(var kind in new[]{FastingTopDownHazard.Kind.Tomb,FastingTopDownHazard.Kind.Fire})
            {
                reset();g.settings.tombPrayerBlockSeconds=g.settings.burnPrayerBlockSeconds=duration;
                Invoke("ApplyEffect",kind);float initial=g.PrayerBlockRemaining;int steps=0;
                while(g.PrayerBlockRemaining>0&&steps<50){g.Tick(.1f,Vector2.zero,true);steps++;}
                locks.Add(new{kind=kind.ToString(),requested=duration,initial,recoveredAt=steps*.1f});
            }
            data["prayerLocks"]=locks;
            reset();float hp=g.Health;bool first=g.TryDamage("Rock"),second=g.TryDamage("Rock");
            data["hitGrace"]=new{first,second,healthLost=hp-g.Health,invulnerable=g.Invulnerable};
            reset();g.player.position=g.playArea.Clamp(new Vector2(0,-9999));float bottomX=g.player.position.x;
            for(int i=0;i<10;i++)g.Tick(.1f,Vector2.right,false);
            data["bottomHorizontalMovement"]=new{distance=g.player.position.x-bottomX,inside=g.CanMove(g.player.position)};
            var scene=EditorSceneManager.OpenPreviewScene("Assets/Scenes/Minigame2.unity");
            try
            {
                var roots=scene.GetRootGameObjects();var c=roots.SelectMany(r=>r.GetComponentsInChildren<CanaDissatisfaction>(true)).First();
                var outcomes=new List<object>();for(int i=1;i<=c.Capacity+1;i++){c.AddComplaint();outcomes.Add(new{input=i,c.Complaints,c.IsGameOver});}
                var shop=roots.SelectMany(r=>r.GetComponentsInChildren<CanaShop>(true)).First();
                data["cana"]=new{capacity=c.Capacity,outcomes,trainingPersistentListeners=shop.onTrainSelected.GetPersistentEventCount()};
            }
            finally{EditorSceneManager.ClosePreviewScene(scene);}
        }
        finally{g.encounterDirector=director;if(director)director.enabled=true;g.BeginStageForTesting(0);g.enabled=false;if(hadTelemetry)File.WriteAllBytes(telemetry,telemetryBytes);else if(File.Exists(telemetry))File.Delete(telemetry);}
        Directory.CreateDirectory("Docs/BalanceTests/2026-11-23");
        string json=JsonConvert.SerializeObject(data,Formatting.Indented);File.WriteAllText("Docs/BalanceTests/2026-11-23/mechanics.json",json);return json;
    }
}
