using System;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using CrossTheLine.Minigames;
public static class InstallFastingHitVignette
{
    public static string Run()
    {
        var g=UnityEngine.Object.FindAnyObjectByType<FastingTopDownGame>();if(EditorApplication.isPlaying||!g||g.gameObject.scene.path!="Assets/Scenes/Minigames/Chapter03_Fasting_TopDown_Test.unity")throw new Exception("Edit minigame scene required");
        string path="Assets/Prefabs/Minigames/FastingTopDown/FastingHitVignette.prefab";var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if(!prefab)
        {
            var root=new GameObject("HitVignette",typeof(RectTransform),typeof(CanvasRenderer),typeof(FastingHitVignette));var rect=(RectTransform)root.transform;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;root.GetComponent<FastingHitVignette>().raycastTarget=false;prefab=PrefabUtility.SaveAsPrefabAsset(root,path);UnityEngine.Object.DestroyImmediate(root);
        }
        var canvas=g.heading.GetComponentInParent<Canvas>();
        if(!g.hitVignette)g.hitVignette=((GameObject)PrefabUtility.InstantiatePrefab(prefab,canvas.transform)).GetComponent<FastingHitVignette>();
        g.hitVignette.transform.SetAsFirstSibling();g.hitVignette.raycastTarget=false;
        PrefabUtility.RecordPrefabInstancePropertyModifications(g);PrefabUtility.RecordPrefabInstancePropertyModifications(g.hitVignette);
        EditorSceneManager.MarkSceneDirty(g.gameObject.scene);EditorSceneManager.SaveScene(g.gameObject.scene);AssetDatabase.SaveAssets();
        return "Vignette prefab installed below HUD: actual health damage red; shield block blue; invulnerable contacts ignored.";
    }
}
