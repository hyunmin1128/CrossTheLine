using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using CrossTheLine.Minigames;
public static class InstallAuthoredVerticalWall
{
    public static string Run(){
        if(EditorApplication.isPlaying)throw new Exception("Edit required");var g=UnityEngine.Object.FindAnyObjectByType<FastingTopDownGame>();
        foreach(bool vertical in new[]{false,true}){
            string source="Assets/Sprite/Objects/Chapter3/Stage3/"+(vertical?"WallPanel-Vertical.aseprite":"WallPanel.aseprite");AssetDatabase.ImportAsset(source,ImportAssetOptions.ForceSynchronousImport);var sprite=AssetDatabase.LoadAllAssetsAtPath(source).OfType<Sprite>().Single();
            string path=vertical?"Assets/Prefabs/Minigames/FastingTopDown/RuinsObstacles/RuinsWallVertical.prefab":AssetDatabase.GetAssetPath(g.ruinsArt.wall);
            var root=PrefabUtility.LoadPrefabContents(AssetDatabase.GetAssetPath(g.ruinsArt.wall));
            try{root.name=vertical?"RuinsWallVertical":"RuinsWallPanel";var h=root.GetComponent<FastingTopDownHazard>();h.SetArtwork(sprite);h.authoredWallOrientation=true;h.fixedApproachScale=true;h.ConfigureWallSegment(vertical,g.settings.laneSpacing);var saved=PrefabUtility.SaveAsPrefabAsset(root,path).GetComponent<FastingTopDownHazard>();if(vertical)g.ruinsArt.verticalWall=saved;else g.ruinsArt.wall=saved;}finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        EditorUtility.SetDirty(g.ruinsArt);AssetDatabase.SaveAssets();return "Native vertical Aseprite connected to separate prefab; horizontal wall restored to authored orientation, both with aligned bounds.";
    }
}
