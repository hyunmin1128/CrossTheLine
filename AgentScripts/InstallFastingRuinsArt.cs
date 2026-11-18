using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using CrossTheLine.Minigames;
public static class InstallFastingRuinsArt
{
    const string Folder="Assets/Prefabs/Minigames/FastingTopDown/RuinsObstacles/";
    static Sprite Load(string path){AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);var i=AssetImporter.GetAtPath(path) as TextureImporter;if(i){i.textureType=TextureImporterType.Sprite;i.spriteImportMode=SpriteImportMode.Single;i.filterMode=FilterMode.Point;i.mipmapEnabled=false;i.textureCompression=TextureImporterCompression.Uncompressed;i.SaveAndReimport();}return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().Single();}
    static void Record(Component c){EditorUtility.SetDirty(c);if(PrefabUtility.IsPartOfPrefabInstance(c))PrefabUtility.RecordPrefabInstancePropertyModifications(c);}
    static FastingTopDownHazard Make(string name,FastingTopDownHazard.Kind kind,Sprite sprite,FastingTopDownHazard template){
        var root=UnityEngine.Object.Instantiate(template.gameObject);root.name=name;
        try{var h=root.GetComponent<FastingTopDownHazard>();h.kind=kind;h.SetArtwork(sprite);h.artworkWidth=sprite.bounds.size.x;h.artworkCenter=sprite.bounds.center;h.scrollOnly=true;h.rotateRollingRock=false;h.expandsOnApproach=false;h.velocity=Vector2.zero;h.collisionHeight=Mathf.Clamp(sprite.bounds.size.y/sprite.bounds.size.x*2.25f*.8f,.8f,2.3f);h.ConfigureLanes(1,3);return PrefabUtility.SaveAsPrefabAsset(root,Folder+name+".prefab").GetComponent<FastingTopDownHazard>();}finally{UnityEngine.Object.DestroyImmediate(root);}
    }
    static void Water(FastingTopDownEntity e,Sprite sprite){
        var symbol=e.GetComponentsInChildren<SpriteRenderer>(true).Single(r=>r.name=="Symbol");symbol.sprite=sprite;symbol.color=Color.white;float scale=2.25f/sprite.bounds.size.x;symbol.transform.localScale=Vector3.one*scale;symbol.transform.localPosition=-(Vector3)sprite.bounds.center*scale;Record(symbol);Record(symbol.transform);
        foreach(var r in e.GetComponentsInChildren<SpriteRenderer>(true).Where(r=>r.name=="SymbolOutline")){r.enabled=false;Record(r);}Record(e);
    }
    public static string Run(){
        var g=UnityEngine.Object.FindAnyObjectByType<FastingTopDownGame>();if(EditorApplication.isPlaying||!g)throw new Exception("Edit minigame required");
        if(!AssetDatabase.IsValidFolder(Folder.TrimEnd('/')))AssetDatabase.CreateFolder("Assets/Prefabs/Minigames/FastingTopDown","RuinsObstacles");
        var art=AssetDatabase.LoadAssetAtPath<FastingRuinsArt>("Assets/Settings/Minigames/FastingRuinsArt.asset");if(!art){art=ScriptableObject.CreateInstance<FastingRuinsArt>();AssetDatabase.CreateAsset(art,"Assets/Settings/Minigames/FastingRuinsArt.asset");}
        art.background=Load("Assets/Sprite/Background/Chapter3-Minigame-Stage3.aseprite");
        art.rubble=Make("RuinsCollapsedWall",FastingTopDownHazard.Kind.Rock,Load("Assets/Sprite/Objects/CollapsedWall.png"),g.desertArt.rockPrefab);
        art.wall=Make("RuinsWallPanel",FastingTopDownHazard.Kind.Wall,Load("Assets/Sprite/Objects/WallPanel.aseprite"),g.desertArt.rockPrefab);
        art.tomb=Make("RuinsSkullTomb",FastingTopDownHazard.Kind.Tomb,Load("Assets/Sprite/Objects/SkullPile.png"),g.desertArt.rockPrefab);
        art.poisonWell=Make("RuinsPoisonWell",FastingTopDownHazard.Kind.Poison,Load("Assets/Sprite/Objects/PoisonWell.png"),g.desertArt.rockPrefab);
        var oasis=Load("Assets/Sprite/Objects/Oasis.png");var well=Load("Assets/Sprite/Objects/Well.png");
        foreach(var e in g.entities.Where(e=>e.kind==FastingTopDownEntity.Kind.Water&&(e.stage==0||e.stage==2))){Water(e,e.stage==0?oasis:well);}
        // Dedicated reusable recovery prefabs retain the existing per-instance use behavior.
        foreach(var stage in new[]{0,2}){
            var e=g.entities.First(e=>e.kind==FastingTopDownEntity.Kind.Water&&e.stage==stage);var clone=UnityEngine.Object.Instantiate(e.gameObject);clone.name=stage==0?"DesertOasisSupply":"RuinsWellSupply";
            try{PrefabUtility.SaveAsPrefabAsset(clone,"Assets/Prefabs/Minigames/FastingTopDown/"+clone.name+".prefab");}finally{UnityEngine.Object.DestroyImmediate(clone);}
        }
        g.ruinsArt=art;EditorUtility.SetDirty(art);Record(g);EditorSceneManager.MarkSceneDirty(g.gameObject.scene);EditorSceneManager.SaveScene(g.gameObject.scene);AssetDatabase.SaveAssets();return "Stage3 background + four hazard prefabs saved; stage1 oasis and stage3 normal well connected. Native source files preserved.";
    }
}
