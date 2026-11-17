using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using CrossTheLine.Minigames;
public static class ConfigureRealtimeFever
{
    static void Guide(UnityEngine.GameObject root)
    {
        foreach(var t in root.GetComponentsInChildren<TMPro.TMP_Text>(true))if(t.text.Contains("기도할 때만 시간 소모")||t.text.Contains("피버 5초"))
        {t.text=t.text.Replace("기도할 때만 시간 소모","획득 즉시 6초간 감소").Replace("피버 5초","피버 6초");EditorUtility.SetDirty(t);PrefabUtility.RecordPrefabInstancePropertyModifications(t);}
    }
    public static string Run()
    {
        if(EditorApplication.isPlaying)throw new Exception("Edit mode required");var g=UnityEngine.Object.FindAnyObjectByType<FastingTopDownGame>();if(!g)throw new Exception("Current game required");g.settings.feverSeconds=6;EditorUtility.SetDirty(g.settings);
        const string path="Assets/Prefabs/Minigames/FastingTopDown/FastingTopDownHUD.prefab";var root=PrefabUtility.LoadPrefabContents(path);try{Guide(root);PrefabUtility.SaveAsPrefabAsset(root,path);}finally{PrefabUtility.UnloadPrefabContents(root);}
        Guide(g.heading.GetComponentInParent<UnityEngine.Canvas>().gameObject);EditorSceneManager.MarkSceneDirty(g.gameObject.scene);EditorSceneManager.SaveScene(g.gameObject.scene);AssetDatabase.SaveAssets();return "Fever six gameplay seconds, starts draining immediately; guide text updated in scene and HUD prefab.";
    }
}
