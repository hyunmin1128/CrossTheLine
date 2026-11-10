using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using CrossTheLine.Minigames;
public static class UseUserDesertArtwork
{
    public static readonly string[] Old={"Assets/Sprite/Minigames/FastingTopDown/TempDesert960x540.png","Assets/Sprite/Minigames/FastingTopDown/DesertObstacles/DesertRock64.png","Assets/Sprite/Minigames/FastingTopDown/DesertObstacles/DesertSandstorm64.png","Assets/Sprite/Minigames/FastingTopDown/DesertObstacles/DesertCactus64.png","Assets/Sprite/Minigames/FastingTopDown/DesertObstacles/DesertQuicksand64.png"};
    static Sprite Import(string path){AssetDatabase.ImportAsset(path);var i=(TextureImporter)AssetImporter.GetAtPath(path);i.textureType=TextureImporterType.Sprite;i.spriteImportMode=SpriteImportMode.Single;i.spritePixelsPerUnit=32;i.filterMode=FilterMode.Point;i.mipmapEnabled=false;i.textureCompression=TextureImporterCompression.Uncompressed;i.alphaIsTransparency=true;i.maxTextureSize=2048;var s=new TextureImporterSettings();i.ReadTextureSettings(s);s.spriteAlignment=(int)SpriteAlignment.Center;s.spritePivot=new Vector2(.5f,.5f);s.spriteMeshType=SpriteMeshType.FullRect;i.SetTextureSettings(s);i.SaveAndReimport();return AssetDatabase.LoadAssetAtPath<Sprite>(path);}
    static void Assign(FastingTopDownHazard prefab,Sprite sprite){string path=AssetDatabase.GetAssetPath(prefab);var root=PrefabUtility.LoadPrefabContents(path);var h=root.GetComponent<FastingTopDownHazard>();h.SetArtwork(sprite);var t=new Texture2D(2,2);t.LoadImage(File.ReadAllBytes(AssetDatabase.GetAssetPath(sprite)));int min=t.width,max=0;for(int y=0;y<t.height;y++)for(int x=0;x<t.width;x++)if(t.GetPixel(x,y).a>.5f){min=Math.Min(min,x);max=Math.Max(max,x);}h.artworkWidth=(max-min+1)/sprite.pixelsPerUnit;UnityEngine.Object.DestroyImmediate(t);PrefabUtility.SaveAsPrefabAsset(root,path);PrefabUtility.UnloadPrefabContents(root);}
    public static string Run()
    {
        if(EditorApplication.isPlaying)throw new Exception("Stop Play");var g=UnityEngine.Object.FindAnyObjectByType<FastingTopDownGame>();if(!g||g.gameObject.scene.isDirty)throw new Exception("Open saved test scene");var bg=Import("Assets/Sprite/Background/Chapter3-Minigame-Stage1.png");var rock=Import("Assets/Sprite/Objects/Rock.png");var sand=Import("Assets/Sprite/Objects/SandStorm.png");var cactus=Import("Assets/Sprite/Objects/Cactus.png");var pit=Import("Assets/Sprite/Objects/SandHell.png");
        foreach(var r in g.backgrounds){r.sprite=bg;r.transform.localScale=new Vector3(30/bg.bounds.size.x,16.875f/bg.bounds.size.y,1);EditorUtility.SetDirty(r);EditorUtility.SetDirty(r.transform);}
        var art=g.desertArt;art.rock=rock;art.sandstorm=sand;art.cactus=cactus;art.quicksand=pit;Assign(art.rockPrefab,rock);Assign(art.sandstormPrefab,sand);Assign(art.cactusPrefab,cactus);Assign(art.quicksandPrefab,pit);EditorUtility.SetDirty(art);EditorSceneManager.MarkSceneDirty(g.gameObject.scene);EditorSceneManager.SaveScene(g.gameObject.scene);AssetDatabase.SaveAssets();return "Applied user background (320x180 at 3x) and four 64x64 sprites; old files retained until validation/reference audit.";
    }
    public static string DeleteUnusedOld()
    {
        if(EditorApplication.isPlaying)throw new Exception("Stop Play before cleanup");var existing=Old.Where(p=>AssetDatabase.LoadMainAssetAtPath(p)).ToArray();var files=AssetDatabase.GetAllAssetPaths().Where(p=>p.StartsWith("Assets/")&&!Old.Contains(p)&&!AssetDatabase.IsValidFolder(p)).ToArray();var references=files.SelectMany(p=>AssetDatabase.GetDependencies(p,false).Where(d=>existing.Contains(d)).Select(d=>p+" -> "+d)).ToArray();if(references.Length>0)throw new Exception("Old assets still referenced:\n"+string.Join("\n",references));foreach(var p in existing)if(!AssetDatabase.DeleteAsset(p))throw new Exception("Could not delete "+p);AssetDatabase.SaveAssets();return "Deleted "+existing.Length+" unreferenced temporary PNG assets and their meta files; user PNG/Aseprite originals preserved.";
    }
}
