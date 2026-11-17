using System;
using UnityEditor;
using UnityEngine;
using CrossTheLine.Minigames;
public static class ApplyCaveGroupsAndPrayer
{
    public static string Run()
    {
        if(EditorApplication.isPlaying)throw new Exception("Edit mode required");var g=UnityEngine.Object.FindAnyObjectByType<FastingTopDownGame>();if(!g||!g.caveArt)throw new Exception("Cave scene required");
        g.caveArt.maximumBatCount=3;g.caveArt.maximumFixedCount=2;EditorUtility.SetDirty(g.caveArt);
        foreach(var prefab in new[]{g.caveArt.stalagmitePrefab,g.caveArt.fallingRockPrefab,g.caveArt.crackPrefab,g.caveArt.batPrefab})
        {
            string path=AssetDatabase.GetAssetPath(prefab);var root=PrefabUtility.LoadPrefabContents(path);
            try{var h=root.GetComponent<FastingTopDownHazard>();h.scrollOnly=h.kind!=FastingTopDownHazard.Kind.Bat;h.rotateRollingRock=false;h.expandsOnApproach=h.kind==FastingTopDownHazard.Kind.Crack;h.quicksandMaximumScale=2;PrefabUtility.SaveAsPrefabAsset(root,path);}finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        AssetDatabase.SaveAssets();return "Bat 1–3; fixed stalagmites/fallen rocks 1–2; expanding crack up to 2x2. Prayer light only while actively praying.";
    }
}
