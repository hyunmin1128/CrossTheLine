using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using CrossTheLine.Minigames;
using Newtonsoft.Json;
public static class ApplyTenMinutePrayerTargets
{
    public static string VerifyRuntime()
    {
        if(!EditorApplication.isPlaying)throw new Exception("Play mode required");
        var game=UnityEngine.Object.FindAnyObjectByType<FastingTopDownGame>();game.enabled=false;
        var rows=new System.Collections.Generic.List<object>();
        try{for(int i=0;i<4;i++){game.BeginStageForTesting(i);game.enabled=false;rows.Add(new{stage=i+1,target=game.PrayerTarget,charge=game.settings.prayerPerSecond,game.settings.prayerMoveMultiplier,game.settings.manualMoveSpeed});}}
        finally{game.BeginStageForTesting(0);game.enabled=false;}
        string json=JsonConvert.SerializeObject(rows,Formatting.Indented);File.WriteAllText("Docs/BalanceTests/2026-11-23/TenMinutes/runtime-verified.json",json);return json;
    }
    public static string Run()
    {
        if(EditorApplication.isPlaying)throw new Exception("Edit mode required");
        const string dir="Docs/BalanceTests/2026-11-23/TenMinutes";
        var request=Newtonsoft.Json.Linq.JObject.Parse(File.ReadAllText(dir+"/request.json"));var targets=request["targets"].ToObject<float[]>();
        Directory.CreateDirectory(dir+"/Backup");
        var game=UnityEngine.Object.FindAnyObjectByType<FastingTopDownGame>();
        var profiles=game.stageConfigurations;
        File.WriteAllText(dir+"/before-profiles.json",JsonConvert.SerializeObject(profiles.Select(JsonUtility.ToJson),Formatting.Indented));
        for(int i=0;i<4;i++)
        {
            string path=PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(profiles[i].gameObject);
            string backup=dir+"/Backup/"+Path.GetFileName(path);
            if(!File.Exists(backup))File.Copy(path,backup);
            if(!File.Exists(backup+".meta"))File.Copy(path+".meta",backup+".meta");
            var root=PrefabUtility.LoadPrefabContents(path);
            try{root.GetComponentInChildren<FastingStageConfiguration>(true).prayerTarget=targets[i];PrefabUtility.SaveAsPrefabAsset(root,path);}
            finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        var rows=profiles.Select(c=>new{c.stageIndex,c.prayerTarget,c.prayerPerSecond,c.prayerMoveMultiplier,c.playerMoveSpeed,c.obstacleFrequency,path=PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(c.gameObject)});
        string json=JsonConvert.SerializeObject(new{testLabel=(string)request["label"],game.gameObject.scene.isDirty,profiles=rows},Formatting.Indented);
        File.WriteAllText(dir+"/applied.json",json);return json;
    }
}
