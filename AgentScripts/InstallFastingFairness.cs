using System;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using CrossTheLine.Minigames;
public static class InstallFastingFairness
{
    public static string Run(){
        if(EditorApplication.isPlaying)throw new Exception("Edit required");var g=UnityEngine.Object.FindAnyObjectByType<FastingTopDownGame>();
        const string path="Assets/Prefabs/Minigames/FastingTopDown/FastingStatusParticles.prefab";
        if(!AssetDatabase.LoadAssetAtPath<GameObject>(path)){
            var temp=new GameObject("FastingStatusParticles",typeof(MeshFilter),typeof(MeshRenderer),typeof(FastingStatusParticles));
            try{temp.GetComponent<FastingStatusParticles>().effectMaterial=g.playerAura.effectMaterial;PrefabUtility.SaveAsPrefabAsset(temp,path);}finally{UnityEngine.Object.DestroyImmediate(temp);}
        }
        var fx=g.player.GetComponentInChildren<FastingStatusParticles>(true);if(!fx)fx=((GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path),g.player)).GetComponent<FastingStatusParticles>();
        fx.game=g;fx.artwork=g.playerBody;fx.effectMaterial=g.playerAura.effectMaterial;EditorUtility.SetDirty(fx);PrefabUtility.RecordPrefabInstancePropertyModifications(fx);
        var mask=g.caveVisionMask;mask.fadeInSeconds=1.6f;mask.blackoutSeconds=.2f;mask.fadeOutSeconds=.8f;EditorUtility.SetDirty(mask);PrefabUtility.RecordPrefabInstancePropertyModifications(mask);
        g.settings.minimumVisibleReactionSeconds=.6f;EditorUtility.SetDirty(g.settings);
        var sprite=g.ruinsArt.verticalWall.body.sprite;float thickness=Mathf.Clamp(sprite.bounds.size.x/sprite.bounds.size.y*(g.settings.laneSpacing+.16f)*g.ruinsArt.wallThicknessScale*g.settings.laterStageObstacleScale,.3f,.8f);
        foreach(var p in new[]{g.ruinsArt.wall,g.ruinsArt.verticalWall}){string assetPath=AssetDatabase.GetAssetPath(p);var wallRoot=PrefabUtility.LoadPrefabContents(assetPath);try{var h=wallRoot.GetComponent<FastingTopDownHazard>();h.ConfigureWallSegment(p==g.ruinsArt.verticalWall,g.settings.laneSpacing,g.ruinsArt.wallThicknessScale*g.settings.laterStageObstacleScale,thickness);PrefabUtility.SaveAsPrefabAsset(wallRoot,assetPath);}finally{PrefabUtility.UnloadPrefabContents(wallRoot);}}
        var maskPath=PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(mask);if(maskPath.EndsWith("FastingVisionMask.prefab")){var root=PrefabUtility.LoadPrefabContents(maskPath);try{var m=root.GetComponent<FastingVisionMask>();m.fadeInSeconds=1.6f;m.blackoutSeconds=.2f;m.fadeOutSeconds=.8f;PrefabUtility.SaveAsPrefabAsset(root,maskPath);}finally{PrefabUtility.UnloadPrefabContents(root);}}
        EditorSceneManager.MarkSceneDirty(g.gameObject.scene);EditorSceneManager.SaveScene(g.gameObject.scene);AssetDatabase.SaveAssets();return "Saved status particle prefab, vignette timings; camera and prayer settings preserved. Spawn lanes="+g.SpawnLaneCount;
    }
}
