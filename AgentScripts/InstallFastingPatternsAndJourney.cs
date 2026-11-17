using System;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using CrossTheLine.Minigames;
public static class InstallFastingPatternsAndJourney
{
    const string PresentationPath="Assets/Prefabs/Minigames/FastingTopDown/FastingJourneyPresentation.prefab";
    static RectTransform Rect(string name,Transform parent,Vector2 anchor,Vector2 pos,Vector2 size)
    {
        var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();if(parent)r.SetParent(parent,false);r.anchorMin=r.anchorMax=r.pivot=anchor;r.anchoredPosition=pos;r.sizeDelta=size;return r;
    }
    static void Stretch(RectTransform r){r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;}
    static UnityEngine.UI.Image Image(RectTransform r,Color color){var i=r.gameObject.AddComponent<UnityEngine.UI.Image>();i.color=color;i.raycastTarget=false;return i;}
    static void CreatePresentation(FastingTopDownGame g)
    {
        var root=Rect("FastingJourneyPresentation",null,Vector2.zero,Vector2.zero,Vector2.zero);Stretch(root);
        var fx=root.gameObject.AddComponent<FastingJourneyPresentation>();
        var atmosphere=Rect("DayNightTint",root,Vector2.zero,Vector2.zero,Vector2.zero);Stretch(atmosphere);fx.atmosphere=Image(atmosphere,Color.clear);
        PrefabUtility.SaveAsPrefabAsset(root.gameObject,PresentationPath);UnityEngine.Object.DestroyImmediate(root.gameObject);
    }
    static void TuneHUD(GameObject root)
    {
        if(!root.transform.Find("FastingJourneyPresentation"))PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PresentationPath),root.transform);
        root.transform.Find("FastingJourneyPresentation").SetAsFirstSibling();
        var panel=(RectTransform)root.transform.Find("ResultPanel");panel.sizeDelta=new Vector2(650,350);
        var result=panel.Find("Result").GetComponent<TMP_Text>();result.rectTransform.anchoredPosition=new Vector2(0,25);result.rectTransform.sizeDelta=new Vector2(600,230);result.fontSize=22;result.enableAutoSizing=false;result.color=new Color(1,.96f,.85f);result.alignment=TextAlignmentOptions.Center;
        ((RectTransform)panel.Find("Retry")).anchoredPosition=new Vector2(0,-135);panel.Find("Retry/Label").GetComponent<TMP_Text>().text="다시 도전 [R]";
    }
    public static string Run()
    {
        if(EditorApplication.isPlaying)throw new Exception("Edit mode required");var g=UnityEngine.Object.FindAnyObjectByType<FastingTopDownGame>();if(!g||g.gameObject.scene.path!="Assets/Scenes/Minigames/Chapter03_Fasting_TopDown_Test.unity")throw new Exception("Open current minigame");
        CreatePresentation(g);
        var directorPath=PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(g.encounterDirector.gameObject);var directorRoot=PrefabUtility.LoadPrefabContents(directorPath);
        try{if(!directorRoot.GetComponent<FastingObstaclePatterns>())directorRoot.AddComponent<FastingObstaclePatterns>();PrefabUtility.SaveAsPrefabAsset(directorRoot,directorPath);}finally{PrefabUtility.UnloadPrefabContents(directorRoot);}
        g.obstaclePatterns=g.encounterDirector.GetComponent<FastingObstaclePatterns>();if(!g.obstaclePatterns)g.obstaclePatterns=g.encounterDirector.gameObject.AddComponent<FastingObstaclePatterns>();
        const string hudPath="Assets/Prefabs/Minigames/FastingTopDown/FastingTopDownHUD.prefab";var hudRoot=PrefabUtility.LoadPrefabContents(hudPath);
        try{TuneHUD(hudRoot);PrefabUtility.SaveAsPrefabAsset(hudRoot,hudPath);}finally{PrefabUtility.UnloadPrefabContents(hudRoot);}
        var ui=UnityEngine.Object.FindAnyObjectByType<FastingPortraitHUD>();TuneHUD(ui.gameObject);var presentation=ui.GetComponentInChildren<FastingJourneyPresentation>(true);presentation.game=g;PrefabUtility.RecordPrefabInstancePropertyModifications(presentation);
        foreach(var c in g.resultPanel.GetComponentsInChildren<Component>(true))if(c)PrefabUtility.RecordPrefabInstancePropertyModifications(c);
        PrefabUtility.RecordPrefabInstancePropertyModifications(presentation.transform);EditorUtility.SetDirty(g);EditorSceneManager.MarkSceneDirty(g.gameObject.scene);EditorSceneManager.SaveScene(g.gameObject.scene);AssetDatabase.SaveAssets();
        return "Installed editable weighted patterns, day/night + day announcement prefab, and expanded run result panel. Prayer/items/settings unchanged.";
    }
}
