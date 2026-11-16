using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using CrossTheLine.Minigames;
public static class ValidateFastingCleanup
{
    public static string Run()
    {
        const string dir="Docs/QA/FastingCleanup-20261116";
        var before=new HashSet<string>(File.ReadAllLines(dir+"/Chapter03_Fasting_TopDown_Test-dependencies.txt"));
        var after=new HashSet<string>(AssetDatabase.GetDependencies("Assets/Scenes/Minigames/Chapter03_Fasting_TopDown_Test.unity",true));
        var lost=before.Except(after).ToArray();if(lost.Length>0)throw new Exception("Current dependencies lost: "+string.Join(",",lost));
        var missing=new HashSet<string>();
        foreach(var asset in AssetDatabase.GetAllAssetPaths().Where(p=>p.StartsWith("Assets/")&&!AssetDatabase.IsValidFolder(p)&&new[]{".unity",".prefab",".asset",".mat",".controller",".anim"}.Contains(Path.GetExtension(p))))
        foreach(System.Text.RegularExpressions.Match match in System.Text.RegularExpressions.Regex.Matches(File.ReadAllText(asset),@"guid: ([a-f0-9]{32})"))
        {var guid=match.Groups[1].Value;if(!guid.StartsWith("0000000000000000")&&string.IsNullOrEmpty(AssetDatabase.GUIDToAssetPath(guid)))missing.Add(asset+"\t"+guid);}
        File.WriteAllLines(dir+"/AfterMissingGuids.tsv",missing.OrderBy(x=>x));var added=missing.Except(File.ReadAllLines(dir+"/BaselineMissingGuids.tsv")).ToArray();if(added.Length>0)throw new Exception("New missing GUIDs: "+string.Join(",",added));
        var g=UnityEngine.Object.FindAnyObjectByType<FastingTopDownGame>();if(!g||!g.enabled||g.playerBody.transform.lossyScale.x<=0)throw new Exception("Scene game disabled/invisible");
        int scripts=0;foreach(var root in g.gameObject.scene.GetRootGameObjects())foreach(var t in root.GetComponentsInChildren<Transform>(true))scripts+=GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject);
        if(scripts!=0)throw new Exception("Missing scene scripts "+scripts);
        foreach(var path in after.Where(p=>p.EndsWith(".prefab")))
        {var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path);foreach(var t in prefab.GetComponentsInChildren<Transform>(true))if(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)>0)throw new Exception("Missing prefab script: "+path);}
        var oldAssets=File.ReadAllLines(dir+"/DeleteAssets.txt");if(oldAssets.Any(p=>File.Exists(p)||File.Exists(p+".meta")))throw new Exception("Deletion incomplete");
        var summary="Current minigame dependencies preserved: "+after.Count+"; lost dependencies 0; new missing GUID references 0; missing scripts in scene/dependent prefabs 0; removed assets "+oldAssets.Length+". Existing unrelated missing GUID entries: "+missing.Count;
        File.WriteAllText(dir+"/Validation.txt",summary);return summary;
    }
}
