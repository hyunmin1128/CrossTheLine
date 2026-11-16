using System;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using CrossTheLine.Minigames;
public static class StrengthenHitVignetteNov16
{
    static void Tune(FastingHitVignette fx){fx.opacityMultiplier=1.9f;fx.durationMultiplier=1.7f;fx.peakHoldSeconds=.25f;fx.maximumOpacity=.4f;fx.clearCenter=.64f;}
    public static string Run()
    {
        var g=UnityEngine.Object.FindAnyObjectByType<FastingTopDownGame>();if(EditorApplication.isPlaying||!g||g.gameObject.scene.path!="Assets/Scenes/Minigames/Chapter03_Fasting_TopDown_Test.unity")throw new Exception("Edit minigame scene required");
        var path=AssetDatabase.GetAssetPath(PrefabUtility.GetCorrespondingObjectFromSource(g.hitVignette.gameObject));if(string.IsNullOrEmpty(path))path="Assets/Prefabs/Minigames/FastingTopDown/FastingHitVignette.prefab";
        var root=PrefabUtility.LoadPrefabContents(path);Tune(root.GetComponent<FastingHitVignette>());PrefabUtility.SaveAsPrefabAsset(root,path);PrefabUtility.UnloadPrefabContents(root);
        Tune(g.hitVignette);g.screenHitFeedback.transform.SetAsFirstSibling();g.hitVignette.transform.SetSiblingIndex(1);
        PrefabUtility.RecordPrefabInstancePropertyModifications(g.hitVignette);PrefabUtility.RecordPrefabInstancePropertyModifications(g.hitVignette.transform);PrefabUtility.RecordPrefabInstancePropertyModifications(g.screenHitFeedback.transform);
        EditorSceneManager.MarkSceneDirty(g.gameObject.scene);EditorSceneManager.SaveScene(g.gameObject.scene);AssetDatabase.SaveAssets();
        return "Opacity x1.9, duration x1.7, peak hold .25 sec; vignette above particles and below HUD.";
    }
}
