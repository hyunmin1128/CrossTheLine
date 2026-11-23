using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using CrossTheLine.Minigames;
using Newtonsoft.Json;
public static class ApplyFastingPrayerTargets
{
    public static string Run()
    {
        if(EditorApplication.isPlaying)throw new Exception("Edit mode required");
        string dir="Docs/BalanceTests/2026-11-23/TargetOnly";Directory.CreateDirectory(dir+"/Backup");
        var game=UnityEngine.Object.FindAnyObjectByType<FastingTopDownGame>();
        if(!game)throw new Exception("Fasting game scene required");
        var before=game.stageConfigurations.Select(c=>JsonUtility.ToJson(c)).ToArray();
        File.WriteAllText(dir+"/before-profiles.json",JsonConvert.SerializeObject(before,Formatting.Indented));
        foreach(var pair in new[]{(2,196f),(3,216f)})
        {
            string path=PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(game.stageConfigurations[pair.Item1].gameObject);
            string backup=dir+"/Backup/"+Path.GetFileName(path);
            if(!File.Exists(backup))File.Copy(path,backup);
            if(!File.Exists(backup+".meta"))File.Copy(path+".meta",backup+".meta");
            var root=PrefabUtility.LoadPrefabContents(path);
            try{var profile=root.GetComponentInChildren<FastingStageConfiguration>(true);profile.prayerTarget=pair.Item2;PrefabUtility.SaveAsPrefabAsset(root,path);}
            finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        var result=game.stageConfigurations.Select(c=>new{c.stageIndex,c.prayerTarget,c.prayerMoveMultiplier,c.playerMoveSpeed,c.obstacleFrequency,c.advancedSettings.burnPrayerBlockSeconds,c.advancedSettings.tombPrayerBlockSeconds}).ToArray();
        string json=JsonConvert.SerializeObject(result,Formatting.Indented);File.WriteAllText(dir+"/applied.json",json);return json;
    }
}
