using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using CrossTheLine.Minigames;

public static class VerifyCactusSpawn
{
    const string Report="Docs/QA/CactusSpawn-20261116";
    public static string Apply()
    {
        var game=UnityEngine.Object.FindAnyObjectByType<FastingTopDownGame>();var settings=game.settings;
        settings.cactusDamage=2;settings.waveIntervals=new[]{1.08f,1f,.92f,.8f};EditorUtility.SetDirty(settings);AssetDatabase.SaveAssetIfDirty(settings);Directory.CreateDirectory(Report);
        File.WriteAllText(Report+"/settings.txt","Cactus damage: 2\nBase wave intervals: 1.35/1.25/1.15/1.00 -> 1.08/1.00/0.92/0.80 seconds.\nExisting 0.8 second minimum, random variation, attack/rest durations and open-lane safety retained.\nActual accepted hazard count may be limited by existing reward/open-lane safety checks.");return "PASS settings applied";
    }
    static void Field(FastingTopDownGame g,string n,object v)=>typeof(FastingTopDownGame).GetField(n,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(g,v);
    public static string Verify()
    {
        if(!EditorApplication.isPlaying)throw new Exception("Play mode required");Directory.CreateDirectory(Report);var log=new List<string>();Action<bool,string> check=(ok,msg)=>{if(!ok)throw new Exception(msg);log.Add("PASS "+msg);};var game=UnityEngine.Object.FindAnyObjectByType<FastingTopDownGame>();
        try
        {
            game.Begin();game.enabled=false;float full=game.Health;check(game.TryDamage("Cactus")&&Mathf.Approximately(game.Health,full-2),"Cactus removes exactly 2 health");game.RefreshHealthHearts();check(game.hearts.Count(h=>h.sprite==game.emptyHeart)==2,"Cactus shows two empty hearts");float remaining=game.Health;game.TryDamage("Cactus");check(game.Health==remaining,"Cactus cannot damage twice during immunity");
            game.Begin();check(game.TryDamage("Rock")&&game.Health==full-game.settings.damage,"Rock retains ordinary damage");
            game.Begin();var root=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Minigames/FastingTopDown/ShieldOfFaithPickup.prefab"));game.Collect(root.GetComponent<FastingTopDownEntity>());UnityEngine.Object.Destroy(root);game.TryDamage("Cactus");check(game.Health==full&&game.ShieldCount==0&&game.Invulnerable,"Shield absorbs full cactus hit and grants immunity");game.TryDamage("Cactus");check(game.Health==full,"Last-shield immunity blocks cactus damage");
            var state=UnityEngine.Random.state;var old=ScriptableObject.CreateInstance<FastingTopDownSettings>();old.waveIntervals=new[]{1.35f,1.25f,1.15f,1f};
            for(int stage=0;stage<4;stage++)
            {
                double before=0,after=0;UnityEngine.Random.InitState(20261116+stage);for(int i=0;i<500;i++)before+=old.NextSpawnInterval(stage);UnityEngine.Random.InitState(20261116+stage);for(int i=0;i<500;i++)after+=game.settings.NextSpawnInterval(stage);
                check(after<before,"Stage "+(stage+1)+" sampled mean spawn interval shortened: "+(before/500).ToString("F3")+" -> "+(after/500).ToString("F3")+" seconds");
            }
            UnityEngine.Random.state=state;UnityEngine.Object.Destroy(old);
            game.Begin();game.enabled=false;var entities=game.entities;game.entities=Array.Empty<FastingTopDownEntity>();Field(game,"grace",1000f);var director=game.encounterDirector;director.ResetEncounter();
            for(int i=0;i<49;i++)director.Tick(.1f);
            check(director.SpawnAttempts>=3,"Actual encounter director attempts at least 3 waves in first attack (was typically 2)");check(game.Hazards.Count>0,"Actual hazards spawn with new schedule");check(game.Hazards.All(h=>director.LeavesLaneOpen(h)),"New spawns preserve escape lane");
            game.entities=entities;game.Begin();game.enabled=true;File.WriteAllLines(Report+"/validation.txt",log);return "PASS "+log.Count+" checks";
        }
        catch(Exception e){File.WriteAllLines(Report+"/validation.txt",log.Concat(new[]{"FAIL "+e}));throw;}
        finally{if(game)game.enabled=true;}
    }
}
