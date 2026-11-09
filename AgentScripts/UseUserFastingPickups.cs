using System;
using System.Linq;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using CrossTheLine.Minigames;
public static class UseUserFastingPickups
{
    const string Dir="Assets/Prefabs/Minigames/FastingTopDown/";
    static string Name(FastingTopDownEntity.Kind k)=>k==FastingTopDownEntity.Kind.Book?"Bible":k==FastingTopDownEntity.Kind.Page?"Page":k==FastingTopDownEntity.Kind.Shield?"ShieldOfFaith":k==FastingTopDownEntity.Kind.Fragment?"Piece":null;
    static Sprite SpriteFor(string name)=>AssetDatabase.LoadAllAssetsAtPath("Assets/Sprite/Objects/"+name+".aseprite").OfType<Sprite>().First();
    static void SetArt(GameObject root,Sprite sprite){var r=root.transform.Find("Symbol").GetComponent<SpriteRenderer>();r.sprite=sprite;r.color=Color.white;float scale=sprite.pixelsPerUnit/32f;r.transform.localScale=Vector3.one*scale;r.transform.localPosition=-sprite.bounds.center*scale;root.transform.Find("SymbolOutline").gameObject.SetActive(false);}
    public static string Run()
    {
        if(EditorApplication.isPlaying)throw new Exception("Stop Play first");var g=UnityEngine.Object.FindAnyObjectByType<FastingTopDownGame>();if(!g||g.gameObject.scene.isDirty)throw new Exception("Open saved top-down scene");
        var kinds=new[]{FastingTopDownEntity.Kind.Book,FastingTopDownEntity.Kind.Page,FastingTopDownEntity.Kind.Shield,FastingTopDownEntity.Kind.Fragment};
        foreach(var kind in kinds){string n=Name(kind);var root=PrefabUtility.LoadPrefabContents(Dir+"Pickup.prefab");root.name=n+"Pickup";root.transform.position=Vector3.zero;root.GetComponent<FastingTopDownEntity>().kind=kind;SetArt(root,SpriteFor(n));PrefabUtility.SaveAsPrefabAsset(root,Dir+n+"Pickup.prefab");PrefabUtility.UnloadPrefabContents(root);}
        int changed=0;
        for(int i=0;i<g.entities.Length;i++){var e=g.entities[i];string n=Name(e.kind);if(n==null)continue;var kind=e.kind;int stage=e.stage;Vector3 position=e.transform.position;string name=e.name;Vector2 footprint=e.footprint;var go=e.gameObject;var asset=AssetDatabase.LoadAssetAtPath<GameObject>(Dir+n+"Pickup.prefab");PrefabUtility.ReplacePrefabAssetOfPrefabInstance(go,asset,InteractionMode.AutomatedAction);e=go.GetComponent<FastingTopDownEntity>();g.entities[i]=e;e.kind=kind;e.stage=stage;e.footprint=footprint;e.transform.position=position;e.name=name;SetArt(go,SpriteFor(n));PrefabUtility.RecordPrefabInstancePropertyModifications(e);PrefabUtility.RecordPrefabInstancePropertyModifications(e.transform);var r=e.transform.Find("Symbol").GetComponent<SpriteRenderer>();PrefabUtility.RecordPrefabInstancePropertyModifications(r);PrefabUtility.RecordPrefabInstancePropertyModifications(r.transform);EditorUtility.SetDirty(e);changed++;}
        EditorUtility.SetDirty(g);EditorSceneManager.MarkSceneDirty(g.gameObject.scene);EditorSceneManager.SaveScene(g.gameObject.scene);AssetDatabase.SaveAssets();return "Replaced "+changed+" scene pickups; created four editable item prefabs. User Aseprite files and import settings unchanged.";
    }
}
