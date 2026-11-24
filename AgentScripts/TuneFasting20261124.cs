using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using CrossTheLine.Minigames;
using Newtonsoft.Json;
public static class TuneFasting20261124
{
    const string Dir="Docs/BalanceTests/2026-11-24/DesertPace";
    static void Backup(string path){Directory.CreateDirectory(Dir+"/Backup");string target=Dir+"/Backup/"+Path.GetFileName(path);if(!File.Exists(target))File.Copy(path,target);if(File.Exists(path+".meta")&&!File.Exists(target+".meta"))File.Copy(path+".meta",target+".meta");}
    public static string Run()
    {
        if(EditorApplication.isPlaying)throw new Exception("Edit mode required");
        string[] names={"Stage01_Desert","Stage02_Cave","Stage03_Ruins","Stage04_FinalTrial"};var changes=new List<object>();
        foreach(string name in names)
        {
            string path="Assets/Prefabs/Minigames/FastingTopDown/Stages/"+name+".prefab";Backup(path);var root=PrefabUtility.LoadPrefabContents(path);
            try
            {
                var c=root.GetComponent<FastingStageConfiguration>();float before=c.scrollSpeed;
                c.scrollSpeed=(float)Math.Round(before*1.15f,2);
                if(c.stageIndex==0)
                {
                    foreach(var obstacle in c.obstacles)if(obstacle.kind==FastingTopDownHazard.Kind.Cactus)obstacle.spawnWeight=45;
                    var settings=c.advancedSettings;string advanced=AssetDatabase.GetAssetPath(settings);Backup(advanced);
                    float sideBefore=settings.sideSandSpeed,topBefore=settings.topSandSpeed;
                    settings.sideSandSpeed=16;settings.topSandSpeed=4.4f;EditorUtility.SetDirty(settings);AssetDatabase.SaveAssetIfDirty(settings);
                    changes.Add(new{stage=1,cactusWeight=45,cactusSelectionShare=.375f,sideBefore,sideAfter=settings.sideSandSpeed,topBefore,topAfter=settings.topSandSpeed});
                }
                changes.Add(new{stage=c.stageIndex+1,scrollBefore=before,scrollAfter=c.scrollSpeed,prayerTarget=c.prayerTarget});
                PrefabUtility.SaveAsPrefabAsset(root,path);
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        string json=JsonConvert.SerializeObject(changes,Formatting.Indented);File.WriteAllText(Dir+"/changes.json",json);return json;
    }
}
