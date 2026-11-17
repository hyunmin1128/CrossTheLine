using System;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using CrossTheLine.Minigames;
public static class ApplyFastingIndependentRandom
{
    static void RemoveBanner(GameObject root){var banner=root.transform.Find("DayAnnouncement");if(banner)UnityEngine.Object.DestroyImmediate(banner.gameObject);}
    static void Tune(FastingEncounterDirector d){d.patternIntervalMultiplier=1.8f;d.randomIntervals=new Vector2(.85f,1.65f);}
    public static string Run()
    {
        if(EditorApplication.isPlaying)throw new Exception("Edit mode required");var g=UnityEngine.Object.FindAnyObjectByType<FastingTopDownGame>();if(!g||g.gameObject.scene.path!="Assets/Scenes/Minigames/Chapter03_Fasting_TopDown_Test.unity")throw new Exception("Current minigame required");
        const string presentationPath="Assets/Prefabs/Minigames/FastingTopDown/FastingJourneyPresentation.prefab";var root=PrefabUtility.LoadPrefabContents(presentationPath);
        try{RemoveBanner(root);PrefabUtility.SaveAsPrefabAsset(root,presentationPath);}finally{PrefabUtility.UnloadPrefabContents(root);}
        string directorPath=PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(g.encounterDirector.gameObject);root=PrefabUtility.LoadPrefabContents(directorPath);
        try{Tune(root.GetComponent<FastingEncounterDirector>());PrefabUtility.SaveAsPrefabAsset(root,directorPath);}finally{PrefabUtility.UnloadPrefabContents(root);}
        var presentation=UnityEngine.Object.FindAnyObjectByType<FastingJourneyPresentation>();RemoveBanner(presentation.gameObject);Tune(g.encounterDirector);PrefabUtility.RecordPrefabInstancePropertyModifications(g.encounterDirector);
        EditorSceneManager.MarkSceneDirty(g.gameObject.scene);EditorSceneManager.SaveScene(g.gameObject.scene);AssetDatabase.SaveAssets();return "Date popup removed; independent random timer .85–1.65s, pattern spacing x1.8; night tint and static heading preserved.";
    }
}
