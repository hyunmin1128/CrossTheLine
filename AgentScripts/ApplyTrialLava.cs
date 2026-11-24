using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.U2D.Aseprite;
using CrossTheLine.Minigames;
public static class ApplyTrialLava
{
    public static string Run()
    {
        if(EditorApplication.isPlaying)throw new Exception("Edit mode required");
        const string path="Assets/Sprite/Objects/Chapter3/Stage4/Lava.aseprite";
        AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
        var importer=(AsepriteImporter)AssetImporter.GetAtPath(path);
        importer.spritePixelsPerUnit=32;importer.filterMode=FilterMode.Point;importer.layerImportMode=LayerImportModes.MergeFrame;
        importer.pivotSpace=PivotSpaces.Canvas;importer.pivotAlignment=SpriteAlignment.Center;importer.SaveAndReimport();
        var sprite=AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().Single();
        var art=AssetDatabase.LoadAssetAtPath<FastingTrialArt>("Assets/Settings/Minigames/FastingTrialArt.asset");
        string prefabPath=AssetDatabase.GetAssetPath(art.fire.gameObject);
        const string backup="Docs/Backups/LavaReplacement-20261121";Directory.CreateDirectory(backup);
        if(!File.Exists(backup+"/TrialFire.prefab"))File.Copy(prefabPath,backup+"/TrialFire.prefab");
        var root=PrefabUtility.LoadPrefabContents(prefabPath);
        try
        {
            var h=root.GetComponent<FastingTopDownHazard>();h.SetArtwork(sprite);h.animationFrames=Array.Empty<Sprite>();
            h.artworkWidth=sprite.bounds.size.x;h.artworkCenter=sprite.bounds.center;
            h.collisionHeight=Mathf.Clamp(sprite.bounds.size.y/h.artworkWidth*2.25f*.75f,.8f,1.4f);
            h.scrollOnly=true;h.rotateRollingRock=false;h.expandsOnApproach=false;h.fixedApproachScale=true;
            h.ConfigureLanes(1,3);PrefabUtility.SaveAsPrefabAsset(root,prefabPath);
        }
        finally{PrefabUtility.UnloadPrefabContents(root);}
        AssetDatabase.SaveAssets();
        return "Replaced trial Fire artwork with user's Lava.aseprite; one static ground opening per spawn, one lane; preserved stage Fire damage/burn settings and original source files. Prefab="+prefabPath;
    }
}
