using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
public static class DeleteAuditedFastingAssets
{
    public static string Run()
    {
        if(EditorApplication.isPlaying||EditorApplication.isCompiling)throw new Exception("Stable edit mode required");
        var plan=new HashSet<string>(File.ReadAllLines("Docs/QA/FastingCleanup-20261116/DeleteAssets.txt"));
        if(plan.Count!=33||plan.Contains("Assets/Scenes/Minigames/Chapter03_Fasting_TopDown_Test.unity"))throw new Exception("Unexpected plan");
        foreach(var p in plan)if(!File.Exists(p)||!File.Exists("Docs/Backups/FastingCleanup-20261116/"+p))throw new Exception("Missing asset/backup: "+p);
        // Recheck the complete kept asset set immediately before deletion.
        var assets=AssetDatabase.GetAllAssetPaths().Where(p=>p.StartsWith("Assets/")&&!AssetDatabase.IsValidFolder(p)).ToArray();
        foreach(var kept in assets.Where(p=>!plan.Contains(p)))foreach(var dep in AssetDatabase.GetDependencies(kept,false))if(plan.Contains(dep))throw new Exception("Kept asset "+kept+" references "+dep);
        var missing=new List<string>();foreach(var asset in assets.Where(p=>new[]{".unity",".prefab",".asset",".mat",".controller",".anim"}.Contains(Path.GetExtension(p))))
        {foreach(System.Text.RegularExpressions.Match match in System.Text.RegularExpressions.Regex.Matches(File.ReadAllText(asset),@"guid: ([a-f0-9]{32})")){var guid=match.Groups[1].Value;if(!guid.StartsWith("0000000000000000")&&string.IsNullOrEmpty(AssetDatabase.GUIDToAssetPath(guid)))missing.Add(asset+"\t"+guid);}}
        File.WriteAllLines("Docs/QA/FastingCleanup-20261116/BaselineMissingGuids.tsv",missing.Distinct());
        AssetDatabase.StartAssetEditing();
        try{foreach(var p in plan.OrderBy(p=>p.EndsWith(".cs")?1:0))if(!AssetDatabase.DeleteAsset(p))throw new Exception("Deletion failed: "+p);}
        finally{AssetDatabase.StopAssetEditing();}
        foreach(var folder in new[]{"Assets/Prefabs/Minigames/Fasting","Assets/Sprite/Minigames/Fasting"})if(Directory.Exists(folder)&&!Directory.EnumerateFileSystemEntries(folder).Any())AssetDatabase.DeleteAsset(folder);
        AssetDatabase.SaveAssets();return "Deleted 33 audited assets and metadata; no kept asset referenced the deletion set. Baseline missing GUIDs: "+missing.Distinct().Count();
    }
}
