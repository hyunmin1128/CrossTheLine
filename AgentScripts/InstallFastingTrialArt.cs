using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using CrossTheLine.Minigames;
public static class InstallFastingTrialArt
{
    const string Folder="Assets/Prefabs/Minigames/FastingTopDown/TrialObstacles/";
    static Sprite[] Load(string path){AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);var t=AssetImporter.GetAtPath(path) as TextureImporter;if(t){t.textureType=TextureImporterType.Sprite;t.spriteImportMode=SpriteImportMode.Single;t.filterMode=FilterMode.Point;t.mipmapEnabled=false;t.textureCompression=TextureImporterCompression.Uncompressed;t.SaveAndReimport();}return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().OrderBy(s=>s.name.StartsWith("Frame_")?int.Parse(s.name.Substring(6)):0).ToArray();}
    static FastingTopDownHazard Make(string name,FastingTopDownHazard.Kind kind,Sprite[] sprites,FastingTopDownHazard template){
        var root=UnityEngine.Object.Instantiate(template.gameObject);root.name=name;
        try{var h=root.GetComponent<FastingTopDownHazard>();h.kind=kind;h.SetArtwork(sprites[0]);h.artworkWidth=sprites.Max(s=>s.bounds.size.x);h.artworkCenter=sprites[0].bounds.center;h.collisionHeight=kind==FastingTopDownHazard.Kind.Pillar?1.4f:Mathf.Clamp(sprites[0].bounds.size.y/h.artworkWidth*2.25f*.75f,1,2.3f);h.scrollOnly=kind!=FastingTopDownHazard.Kind.Reaper;h.rotateRollingRock=false;h.expandsOnApproach=false;h.ConfigureLanes(1,3);if(sprites.Length>1)h.SetAnimation(sprites,8);return PrefabUtility.SaveAsPrefabAsset(root,Folder+name+".prefab").GetComponent<FastingTopDownHazard>();}finally{UnityEngine.Object.DestroyImmediate(root);}
    }
    public static string Run(){
        if(EditorApplication.isPlaying)throw new Exception("Edit required");var g=UnityEngine.Object.FindAnyObjectByType<FastingTopDownGame>();if(!g)throw new Exception("Minigame required");
        if(!AssetDatabase.IsValidFolder(Folder.TrimEnd('/')))AssetDatabase.CreateFolder("Assets/Prefabs/Minigames/FastingTopDown","TrialObstacles");
        const string path="Assets/Settings/Minigames/FastingTrialArt.asset";var art=AssetDatabase.LoadAssetAtPath<FastingTrialArt>(path);if(!art){art=ScriptableObject.CreateInstance<FastingTrialArt>();AssetDatabase.CreateAsset(art,path);}
        art.background=Load("Assets/Sprite/Background/Chapter3-Minigame-Stage4.png").Single();
        art.fire=Make("TrialFire",FastingTopDownHazard.Kind.Fire,Load("Assets/Sprite/Objects/Chapter3/Stage4/Lava.aseprite"),g.desertArt.rockPrefab);
        art.thorns=Make("TrialThornbush",FastingTopDownHazard.Kind.Thorns,Load("Assets/Sprite/Objects/Chapter3/Stage4/Thornbush.png"),g.desertArt.rockPrefab);
        art.pillar=Make("TrialFlamePillar",FastingTopDownHazard.Kind.Pillar,Load("Assets/Sprite/Objects/Chapter3/Stage4/PillarOfFlame.aseprite"),g.desertArt.rockPrefab);
        art.reaper=Make("TrialReaper",FastingTopDownHazard.Kind.Reaper,Load("Assets/Sprite/Objects/Chapter3/Stage4/Reaper.png"),g.desertArt.rockPrefab);
        g.trialArt=art;EditorUtility.SetDirty(art);EditorUtility.SetDirty(g);EditorSceneManager.MarkSceneDirty(g.gameObject.scene);EditorSceneManager.SaveScene(g.gameObject.scene);AssetDatabase.SaveAssets();return "Final trial background + four editable prefabs; fire frames="+art.fire.animationFrames.Length+", pillar frames="+art.pillar.animationFrames.Length+". Existing four-stage progression unchanged.";
    }
}
