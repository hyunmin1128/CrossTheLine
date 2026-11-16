using System;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using CrossTheLine.Minigames;
public static class IncreaseFastingSpawnsAndHeader
{
    public static string Run()
    {
        if(EditorApplication.isPlaying)throw new Exception("Edit mode required");
        var g=UnityEngine.Object.FindAnyObjectByType<FastingTopDownGame>();if(!g||g.gameObject.scene.path!="Assets/Scenes/Minigames/Chapter03_Fasting_TopDown_Test.unity")throw new Exception("Open minigame");
        g.settings.waveIntervals=new[]{.84f,.8f,.74f,.68f};g.settings.minimumSpawnInterval=.55f;EditorUtility.SetDirty(g.settings);
        const string path="Assets/Prefabs/Minigames/FastingTopDown/FastingTopDownHUD.prefab";var root=PrefabUtility.LoadPrefabContents(path);
        try{root.transform.Find("Heading").GetComponent<TMPro.TMP_Text>().color=Color.black;PrefabUtility.SaveAsPrefabAsset(root,path);}finally{PrefabUtility.UnloadPrefabContents(root);}
        g.heading.color=Color.black;PrefabUtility.RecordPrefabInstancePropertyModifications(g.heading);
        EditorSceneManager.MarkSceneDirty(g.gameObject.scene);EditorSceneManager.SaveScene(g.gameObject.scene);AssetDatabase.SaveAssets();
        return "Spawn intervals .84/.80/.74/.68 with .55s minimum; black heading saved in scene and HUD prefab.";
    }
}
