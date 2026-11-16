using System;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using CrossTheLine.Minigames;
public static class ImproveHitReadability
{
    static void Tune(FastingHitVignette fx){fx.opacityMultiplier=2.4f;fx.maximumOpacity=.46f;fx.peakHoldSeconds=.45f;}
    static void Tune(FastingScreenHitFeedback fx){fx.debrisOpacity=.28f;fx.debrisSeconds=.32f;}
    public static string Run()
    {
        var g=UnityEngine.Object.FindAnyObjectByType<FastingTopDownGame>();
        if(EditorApplication.isPlaying||!g||g.gameObject.scene.path!="Assets/Scenes/Minigames/Chapter03_Fasting_TopDown_Test.unity")throw new Exception("Edit minigame required");
        foreach(var component in new Component[]{g.hitVignette,g.screenHitFeedback})
        {
            var path=PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(component.gameObject);
            if(string.IsNullOrEmpty(path))throw new Exception("Expected existing feedback prefab");
            var root=PrefabUtility.LoadPrefabContents(path);
            try{if(component is FastingHitVignette)Tune(root.GetComponentInChildren<FastingHitVignette>(true));else Tune(root.GetComponentInChildren<FastingScreenHitFeedback>(true));PrefabUtility.SaveAsPrefabAsset(root,path);}
            finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        Tune(g.hitVignette);Tune(g.screenHitFeedback);
        g.screenHitFeedback.transform.SetAsFirstSibling();g.hitVignette.transform.SetSiblingIndex(1);
        PrefabUtility.RecordPrefabInstancePropertyModifications(g.hitVignette);PrefabUtility.RecordPrefabInstancePropertyModifications(g.screenHitFeedback);
        PrefabUtility.RecordPrefabInstancePropertyModifications(g.hitVignette.transform);PrefabUtility.RecordPrefabInstancePropertyModifications(g.screenHitFeedback.transform);
        EditorSceneManager.MarkSceneDirty(g.gameObject.scene);EditorSceneManager.SaveScene(g.gameObject.scene);AssetDatabase.SaveAssets();
        return "Saved scene and both feedback prefabs: continuous vignette, brief translucent debris.";
    }
}
