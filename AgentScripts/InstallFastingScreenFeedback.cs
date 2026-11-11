using System;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using CrossTheLine.Minigames;
public static class InstallFastingScreenFeedback
{
    public static string Run()
    {
        if(EditorApplication.isPlaying)throw new Exception("Stop Play first");
        var g=UnityEngine.Object.FindAnyObjectByType<FastingTopDownGame>();
        if(!g||g.gameObject.scene.isDirty)throw new Exception("Open saved minigame scene first");
        var canvas=g.heading.GetComponentInParent<Canvas>();
        string path="Assets/Prefabs/Minigames/FastingTopDown/FastingScreenHitFeedback.prefab";
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if(!prefab)
        {
            var obj=new GameObject("ScreenHitFeedback",typeof(RectTransform),typeof(CanvasRenderer),typeof(FastingScreenHitFeedback));
            var r=obj.GetComponent<RectTransform>();r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;
            obj.GetComponent<FastingScreenHitFeedback>().raycastTarget=false;
            prefab=PrefabUtility.SaveAsPrefabAsset(obj,path);UnityEngine.Object.DestroyImmediate(obj);
        }
        if(!g.screenHitFeedback)
        {
            var obj=(GameObject)PrefabUtility.InstantiatePrefab(prefab,canvas.transform);
            g.screenHitFeedback=obj.GetComponent<FastingScreenHitFeedback>();
        }
        // World is behind this Canvas; existing gauges and menus remain above the overlay.
        g.screenHitFeedback.transform.SetAsFirstSibling();
        PrefabUtility.RecordPrefabInstancePropertyModifications(g);
        EditorUtility.SetDirty(g);EditorSceneManager.MarkSceneDirty(g.gameObject.scene);
        EditorSceneManager.SaveScene(g.gameObject.scene);AssetDatabase.SaveAssets();
        return "Screen effect prefab installed under "+canvas.name+"; center stays clear, raycast disabled";
    }
}
