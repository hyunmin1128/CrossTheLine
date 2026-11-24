using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using CrossTheLine.Minigames;
public static class ApplyFastingReadabilityHearts
{
    const string Folder="Assets/Prefabs/Minigames/FastingTopDown/";
    static void Save(Component c){EditorUtility.SetDirty(c);if(PrefabUtility.IsPartOfPrefabInstance(c))PrefabUtility.RecordPrefabInstancePropertyModifications(c);}
    static Sprite Heart(string name)=>AssetDatabase.LoadAllAssetsAtPath("Assets/Sprite/Objects/Chapter3/Shared/"+name+".aseprite").OfType<Sprite>().Single();
    static void TuneAura(FastingPlayerAura a){a.prayerRadius=new Vector2(1.15f,1.5f);a.prayerGlowColor=new Color(1,.85f,.38f,1);}
    public static string Run()
    {
        var g=UnityEngine.Object.FindAnyObjectByType<FastingTopDownGame>();
        if(EditorApplication.isPlaying||!g||g.gameObject.scene.path!="Assets/Scenes/Minigames/Chapter03_Fasting_TopDown_Test.unity")throw new Exception("Minigame edit scene required");
        g.fullHeart=Heart("Heart");g.halfHeart=Heart("HalfHeart");g.emptyHeart=Heart("ZeroHeart");
        foreach(var h in g.hearts){h.sprite=g.fullHeart;h.type=UnityEngine.UI.Image.Type.Simple;h.useSpriteMesh=false;h.preserveAspect=true;h.color=Color.white;Save(h);}
        string maskPath=Folder+"FastingVisionMask.prefab";
        if(!AssetDatabase.LoadAssetAtPath<GameObject>(maskPath)){
            var temp=new GameObject("FastingVisionMask",typeof(RectTransform),typeof(CanvasRenderer),typeof(FastingVisionMask));
            try{var rt=(RectTransform)temp.transform;rt.anchorMin=Vector2.zero;rt.anchorMax=Vector2.one;rt.offsetMin=rt.offsetMax=Vector2.zero;temp.GetComponent<FastingVisionMask>().raycastTarget=false;PrefabUtility.SaveAsPrefabAsset(temp,maskPath);}finally{UnityEngine.Object.DestroyImmediate(temp);}
        }
        var hudPath=Folder+"FastingTopDownHUD.prefab";var hud=PrefabUtility.LoadPrefabContents(hudPath);
        try{
            var mask=hud.GetComponentInChildren<FastingVisionMask>(true);
            if(!mask)mask=((GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(maskPath),hud.transform)).GetComponent<FastingVisionMask>();
            mask.transform.SetSiblingIndex(Math.Min(1,hud.transform.childCount-1));
            PrefabUtility.SaveAsPrefabAsset(hud,hudPath);
        }finally{PrefabUtility.UnloadPrefabContents(hud);}
        var parent=g.visionPanels[0].transform.parent;
        g.caveVisionMask=parent.GetComponentInChildren<FastingVisionMask>(true);
        if(!g.caveVisionMask)g.caveVisionMask=((GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(maskPath),parent)).GetComponent<FastingVisionMask>();
        g.caveVisionMask.transform.SetSiblingIndex(1);Save(g.caveVisionMask.transform);
        foreach(var file in new[]{"FastingPlayerAura.prefab","FastingEncounterDirector.prefab"}){
            var root=PrefabUtility.LoadPrefabContents(Folder+file);
            try{var aura=root.GetComponentInChildren<FastingPlayerAura>(true);if(aura)TuneAura(aura);var d=root.GetComponentInChildren<FastingEncounterDirector>(true);if(d)d.desertSpawnIntervalMultiplier=.75f;PrefabUtility.SaveAsPrefabAsset(root,Folder+file);}finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        var liveAura=UnityEngine.Object.FindAnyObjectByType<FastingPlayerAura>();TuneAura(liveAura);Save(liveAura);g.encounterDirector.desertSpawnIntervalMultiplier=.75f;Save(g.encounterDirector);
        var pp=g.gameCamera.GetComponent<UnityEngine.Rendering.Universal.PixelPerfectCamera>();
        pp.cropFrame=UnityEngine.Rendering.Universal.PixelPerfectCamera.CropFrame.StretchFill;Save(pp);
        var type=typeof(Editor).Assembly.GetType("UnityEditor.GameView");var view=Resources.FindObjectsOfTypeAll(type).FirstOrDefault();
        if(view){var prop=type.GetProperty("enterPlayModeBehavior",BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance);prop.SetValue(view,Enum.Parse(prop.PropertyType,"PlayMaximized"));}
        Save(g);EditorSceneManager.MarkSceneDirty(g.gameObject.scene);EditorSceneManager.SaveScene(g.gameObject.scene);AssetDatabase.SaveAssets();
        return "Saved native Aseprite full/half/empty hearts, opaque cave mask prefab, larger prayer bubble, desert interval x0.75 and StretchFill/PlayMaximized. Camera reference and world scale unchanged.";
    }
}
