using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
public static class AuditFastingCleanup
{
    public static string Run()
    {
        const string dir="Docs/QA/FastingCleanup-20261116";Directory.CreateDirectory(dir);
        var paths=AssetDatabase.GetAllAssetPaths().Where(p=>p.StartsWith("Assets/")&&!AssetDatabase.IsValidFolder(p)).OrderBy(p=>p).ToArray();
        var graph=new List<string>();foreach(var p in paths)graph.Add(p+"\t"+string.Join("|",AssetDatabase.GetDependencies(p,false).Where(d=>d!=p)));
        File.WriteAllLines(dir+"/AssetDependencies.tsv",graph);
        foreach(var scene in new[]{"Chapter03_Fasting_Test","Chapter03_Fasting_TopDown_Test"})File.WriteAllLines(dir+"/"+scene+"-dependencies.txt",AssetDatabase.GetDependencies("Assets/Scenes/Minigames/"+scene+".unity",true).OrderBy(p=>p));
        File.WriteAllLines(dir+"/BuildScenes.tsv",EditorBuildSettings.scenes.Select(s=>s.enabled+"\t"+s.path));
        var scope=paths.Where(p=>p.Contains("/Minigames/Fasting")||p.Contains("/Effect/Fasting/")||p.Contains("FastingDifficulty")||p.Contains("FastingTopDown")||p.Contains("Chapter03_Fasting")||p.Contains("FastingDesert")||p.Contains("FastingEffect"));File.WriteAllLines(dir+"/Scope.txt",scope);
        return paths.Length+" assets audited; dependencies and build scenes exported to "+dir;
    }
}
