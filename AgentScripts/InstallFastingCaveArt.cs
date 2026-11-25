using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using CrossTheLine.Minigames;
public static class InstallFastingCaveArt
{
    static void Import(string p,bool background=false)
    {
        AssetDatabase.ImportAsset(p,ImportAssetOptions.ForceSynchronousImport);var i=AssetImporter.GetAtPath(p) as TextureImporter;if(!i)throw new Exception("Not a texture: "+p);
        if(i.textureType!=TextureImporterType.Sprite){i.textureType=TextureImporterType.Sprite;i.spriteImportMode=SpriteImportMode.Single;}
        if(background)i.spritePixelsPerUnit=32;
        i.filterMode=FilterMode.Point;i.mipmapEnabled=false;i.textureCompression=TextureImporterCompression.Uncompressed;i.alphaIsTransparency=!background;i.SaveAndReimport();
    }
    static Sprite[] Sprites(string p)=>AssetDatabase.LoadAllAssetsAtPath(p).OfType<Sprite>().OrderBy(s=>s.rect.x).ToArray();
    static Rect Measure(Sprite[] frames)
    {
        var p=AssetDatabase.GetAssetPath(frames[0]);var tex=new Texture2D(2,2);tex.LoadImage(File.ReadAllBytes(p));var pixels=tex.GetPixels32();int left=100000,bottom=100000,right=-1,top=-1;
        foreach(var frame in frames){var r=frame.rect;for(int y=0;y<(int)r.height;y++)for(int x=0;x<(int)r.width;x++)if(pixels[(y+(int)r.y)*tex.width+x+(int)r.x].a>25){left=Math.Min(left,x);bottom=Math.Min(bottom,y);right=Math.Max(right,x);top=Math.Max(top,y);}}
        UnityEngine.Object.DestroyImmediate(tex);if(right<left)throw new Exception("Empty sprite");return Rect.MinMaxRect(left,bottom,right+1,top+1);
    }
    static FastingTopDownHazard Prefab(string name,FastingTopDownHazard.Kind kind,Sprite[] frames,FastingTopDownHazard template,List<string> report)
    {
        var root=UnityEngine.Object.Instantiate(template.gameObject);root.name=name;var h=root.GetComponent<FastingTopDownHazard>();h.kind=kind;h.SetArtwork(frames[0]);
        var visible=Measure(frames);float ppu=frames[0].pixelsPerUnit;h.artworkWidth=visible.width/ppu;h.artworkCenter=(visible.center-frames[0].pivot)/ppu;h.scrollOnly=kind!=FastingTopDownHazard.Kind.Bat;h.expandsOnApproach=kind==FastingTopDownHazard.Kind.Crack;h.rotateRollingRock=false;
        h.collisionHeight=Mathf.Clamp(visible.height/visible.width*2.25f*.75f,.8f,2.4f);h.ConfigureLanes(1,3);if(frames.Length>1)h.SetAnimation(frames,10);
        string path="Assets/Prefabs/Minigames/FastingTopDown/CaveObstacles/"+name+".prefab";var prefab=PrefabUtility.SaveAsPrefabAsset(root,path).GetComponent<FastingTopDownHazard>();UnityEngine.Object.DestroyImmediate(root);report.Add(name+" frames="+frames.Length+" visiblePixels="+visible+" collisionHeight="+prefab.collisionHeight);return prefab;
    }
    public static string Run()
    {
        if(EditorApplication.isPlaying)throw new Exception("Edit mode required");var g=UnityEngine.Object.FindAnyObjectByType<FastingTopDownGame>();if(!g||g.gameObject.scene.path!="Assets/Scenes/Minigames/Chapter03_Fasting_TopDown_Test.unity")throw new Exception("Current scene required");
        const string bg="Assets/Sprite/Background/Minigames/Chapter3/Stage2/Chapter3-Minigame-Stage2.png";Import(bg,true);
        foreach(var n in new[]{"Stalagmite.png","FallingRock.png","Crack.png","Bat-Sheet.png"})Import("Assets/Sprite/Objects/Chapter3/Stage2/"+n);
        string folder="Assets/Prefabs/Minigames/FastingTopDown/CaveObstacles";if(!AssetDatabase.IsValidFolder(folder))AssetDatabase.CreateFolder("Assets/Prefabs/Minigames/FastingTopDown","CaveObstacles");
        const string path="Assets/Settings/Minigames/FastingCaveObstacleArt.asset";var art=AssetDatabase.LoadAssetAtPath<FastingCaveObstacleArt>(path);if(!art){art=ScriptableObject.CreateInstance<FastingCaveObstacleArt>();AssetDatabase.CreateAsset(art,path);}
        art.background=Sprites(bg).Single();art.stalagmite=Sprites("Assets/Sprite/Objects/Chapter3/Stage2/Stalagmite.png").Single();art.fallingRock=Sprites("Assets/Sprite/Objects/Chapter3/Stage2/FallingRock.png").Single();art.crack=Sprites("Assets/Sprite/Objects/Chapter3/Stage2/Crack.png").Single();art.batFrames=Sprites("Assets/Sprite/Objects/Chapter3/Stage2/Bat-Sheet.png");if(art.batFrames.Length!=10)throw new Exception("Expected ten authored bat frames");art.batFramesPerSecond=10;
        var report=new List<string>();var template=g.desertArt.rockPrefab;
        art.stalagmitePrefab=Prefab("CaveStalagmite",FastingTopDownHazard.Kind.Rock,new[]{art.stalagmite},template,report);
        art.fallingRockPrefab=Prefab("CaveFallingRock",FastingTopDownHazard.Kind.Collapse,new[]{art.fallingRock},template,report);
        art.crackPrefab=Prefab("CaveCrack",FastingTopDownHazard.Kind.Crack,new[]{art.crack},template,report);
        art.batPrefab=Prefab("CaveBats",FastingTopDownHazard.Kind.Bat,art.batFrames,template,report);
        g.caveArt=art;EditorUtility.SetDirty(art);EditorUtility.SetDirty(g);EditorSceneManager.MarkSceneDirty(g.gameObject.scene);EditorSceneManager.SaveScene(g.gameObject.scene);AssetDatabase.SaveAssets();
        Directory.CreateDirectory("Docs/QA/FastingTopDown/CaveArt-20261117");File.WriteAllLines("Docs/QA/FastingTopDown/CaveArt-20261117/ArtImport.txt",report);
        return string.Join("; ",report)+". Connected cave background; existing sprite rects, pivots and source pixels preserved.";
    }
}
