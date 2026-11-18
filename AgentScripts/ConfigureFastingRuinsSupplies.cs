using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using CrossTheLine.Minigames;
public static class ConfigureFastingRuinsSupplies
{
    static void Save(Component c){EditorUtility.SetDirty(c);if(PrefabUtility.IsPartOfPrefabInstance(c))PrefabUtility.RecordPrefabInstancePropertyModifications(c);}
    public static string Run(){
        if(EditorApplication.isPlaying)throw new Exception("Edit required");var g=UnityEngine.Object.FindAnyObjectByType<FastingTopDownGame>();
        var e=g.entities.First(e=>e.stage==1&&e.kind==FastingTopDownEntity.Kind.Water);var frames=AssetDatabase.LoadAllAssetsAtPath("Assets/Sprite/Objects/Stalactite.png").OfType<Sprite>().OrderByDescending(s=>s.rect.y).ToArray();
        var top=e.GetComponentsInChildren<SpriteRenderer>(true).First(r=>r.name=="Symbol");float scale=2.5f/frames[1].bounds.size.x;top.sprite=frames[0];top.transform.localScale=Vector3.one*scale;top.transform.localPosition=new Vector3(0,.9f)-(Vector3)frames[0].bounds.center*scale;Save(top);Save(top.transform);
        var lower=e.transform.Find("StalactiteWaterBasin");if(!lower){lower=new GameObject("StalactiteWaterBasin",typeof(SpriteRenderer)).transform;lower.SetParent(e.transform,false);}var basin=lower.GetComponent<SpriteRenderer>();basin.sprite=frames[1];basin.sharedMaterial=top.sharedMaterial;basin.sortingOrder=top.sortingOrder;lower.localScale=Vector3.one*scale;lower.localPosition=new Vector3(0,-.6f)-(Vector3)frames[1].bounds.center*scale;Save(basin);Save(lower);
        foreach(var r in e.GetComponentsInChildren<SpriteRenderer>(true).Where(r=>r.name=="SymbolOutline")){r.enabled=false;Save(r);}
        var oasis=g.entities.First(e=>e.stage==0&&e.kind==FastingTopDownEntity.Kind.Water);var art=oasis.GetComponentsInChildren<SpriteRenderer>(true).First(r=>r.name=="Symbol");scale=4.5f/art.sprite.bounds.size.x;art.transform.localScale=Vector3.one*scale;art.transform.localPosition=-(Vector3)art.sprite.bounds.center*scale;oasis.footprint=new Vector2(4.5f,3);Save(art.transform);Save(oasis);
        g.settings.poisonDamage=2;g.settings.poisonReverseSeconds=3;g.settings.tombPrayerBlockSeconds=3;EditorUtility.SetDirty(g.settings);
        EditorSceneManager.MarkSceneDirty(g.gameObject.scene);EditorSceneManager.SaveScene(g.gameObject.scene);AssetDatabase.SaveAssets();return "Two-piece stalactite water supply, 4.5-world-unit oasis, 3s tomb/reverse and poison damage2 saved.";
    }
}
