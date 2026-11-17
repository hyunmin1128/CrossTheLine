using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using CrossTheLine.Minigames;
public static class ConfigureFastingCaveHits
{
    static void Tune(FastingHitFeedback fx){fx.batAirSeconds=1.1f;fx.batAirHeight=1.8f;fx.crackFallSeconds=.9f;fx.crackBlinkSeconds=.8f;fx.crackFallDepth=1.2f;}
    public static string Run()
    {
        if(EditorApplication.isPlaying)throw new Exception("Edit mode required");var g=UnityEngine.Object.FindAnyObjectByType<FastingTopDownGame>();if(!g||!g.hitFeedback)throw new Exception("Current game required");
        g.settings.crackDamage=2;g.settings.batSlowSeconds=1.8f;g.settings.batVisionSeconds=1.4f;EditorUtility.SetDirty(g.settings);
        var path=PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(g.hitFeedback.gameObject);var root=PrefabUtility.LoadPrefabContents(path);
        try{Tune(root.GetComponent<FastingHitFeedback>());root.GetComponent<FastingHitFeedback>().crackGroundSprite=g.caveArt.crack;PrefabUtility.SaveAsPrefabAsset(root,path);}finally{PrefabUtility.UnloadPrefabContents(root);}
        Tune(g.hitFeedback);g.hitFeedback.crackGroundSprite=g.caveArt.crack;PrefabUtility.RecordPrefabInstancePropertyModifications(g.hitFeedback);EditorSceneManager.MarkSceneDirty(g.gameObject.scene);EditorSceneManager.SaveScene(g.gameObject.scene);AssetDatabase.SaveAssets();
        return "Bat: 1.1s air / 1.8s slow / 1.4s vision; crack: 2 damage / .9s fall-return / .8s protected blink.";
    }
}
