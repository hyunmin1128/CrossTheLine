using System;
using System.Linq;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using CrossTheLine.Minigames;
public static class BuildDesertObstacleSprites
{
    const string Art="Assets/Sprite/Minigames/FastingTopDown/DesertObstacles",Source="Docs/ArtSources/DesertObstacles",Pref="Assets/Prefabs/Minigames/FastingTopDown/DesertObstacles";
    static readonly Color32[] Palette={new Color32(0,0,0,255),new Color32(67,43,28,255),new Color32(110,72,38,255),new Color32(154,104,54,255),new Color32(197,143,71,255),new Color32(227,179,103,255),new Color32(249,216,154,255),new Color32(128,119,96,255),new Color32(35,65,38,255),new Color32(57,102,44,255),new Color32(88,139,54,255),new Color32(135,170,72,255),new Color32(22,44,26,255),new Color32(255,240,193,255),new Color32(84,57,30,255)};
    static Color32 Quant(Color32 c){if(c.a<128)return new Color32(0,0,0,0);int best=0,min=int.MaxValue;for(int i=0;i<Palette.Length;i++){int dr=c.r-Palette[i].r,dg=c.g-Palette[i].g,db=c.b-Palette[i].b;int d=dr*dr+dg*dg+db*db;if(d<min){min=d;best=i;}}return Palette[best];}
    static void Folder(string path){if(AssetDatabase.IsValidFolder(path))return;var p=Path.GetDirectoryName(path).Replace('\\','/');Folder(p);AssetDatabase.CreateFolder(p,Path.GetFileName(path));}
    static Sprite Save(string source,string name){var raw=new Texture2D(2,2);raw.LoadImage(File.ReadAllBytes(source));var tex=new Texture2D(64,64,TextureFormat.RGBA32,false);float scale=Mathf.Min(64f/raw.width,64f/raw.height);for(int y=0;y<64;y++)for(int x=0;x<64;x++){int sx=Mathf.FloorToInt((x-31.5f)/scale+raw.width*.5f),sy=Mathf.FloorToInt((y-31.5f)/scale+raw.height*.5f);tex.SetPixel(x,y,sx>=0&&sx<raw.width&&sy>=0&&sy<raw.height?Quant(raw.GetPixel(sx,sy)):new Color32(0,0,0,0));}tex.Apply();string path=Art+"/Desert"+name+"64.png";File.WriteAllBytes(path,tex.EncodeToPNG());UnityEngine.Object.DestroyImmediate(raw);UnityEngine.Object.DestroyImmediate(tex);AssetDatabase.ImportAsset(path);var imp=(TextureImporter)AssetImporter.GetAtPath(path);imp.textureType=TextureImporterType.Sprite;imp.spriteImportMode=SpriteImportMode.Single;imp.spritePixelsPerUnit=32;imp.filterMode=FilterMode.Point;imp.mipmapEnabled=false;imp.textureCompression=TextureImporterCompression.Uncompressed;imp.alphaIsTransparency=true;imp.isReadable=false;var s=new TextureImporterSettings();imp.ReadTextureSettings(s);s.spriteMeshType=SpriteMeshType.FullRect;s.spriteAlignment=(int)SpriteAlignment.Center;s.spritePivot=new Vector2(.5f,.5f);imp.SetTextureSettings(s);imp.SaveAndReimport();return AssetDatabase.LoadAssetAtPath<Sprite>(path);}
    static FastingTopDownHazard Prefab(FastingTopDownGame game,string name,FastingTopDownHazard.Kind kind,Sprite sprite)
    {
        var root=PrefabUtility.LoadPrefabContents(AssetDatabase.GetAssetPath(game.hazardPrefab));root.name="Desert"+name;var h=root.GetComponent<FastingTopDownHazard>();h.kind=kind;h.SetArtwork(sprite);var pixel=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprite/Minigames/FastingTopDown/Pixel.png");
        for(int i=0;i<4;i++){var o=new GameObject("HitAreaBorder_"+i);o.transform.SetParent(root.transform,false);bool horizontal=i<2;float side=i%2==0?-1:1;o.transform.localPosition=horizontal?new Vector3(0,side*h.size.y*.5f):new Vector3(side*h.size.x*.5f,0);o.transform.localScale=horizontal?new Vector3(h.size.x*32,1,1):new Vector3(1,h.size.y*32,1);var r=o.AddComponent<SpriteRenderer>();r.sprite=pixel;r.sharedMaterial=h.body.sharedMaterial;r.sortingOrder=45;r.color=new Color(1,.84f,.55f,.9f);}
        var prefab=PrefabUtility.SaveAsPrefabAsset(root,Pref+"/Desert"+name+".prefab");PrefabUtility.UnloadPrefabContents(root);return prefab.GetComponent<FastingTopDownHazard>();
    }
    public static string Run()
    {
        if(EditorApplication.isPlaying)throw new Exception("Stop Play");var g=UnityEngine.Object.FindAnyObjectByType<FastingTopDownGame>();if(!g||g.gameObject.scene.isDirty)throw new Exception("Open saved top-down scene");if(File.Exists(Art+"/DesertRock64.png"))throw new Exception("Generated sprites already exist; preserve edits");Folder(Art);Folder(Pref);
        var rock=Save(Source+"/Rock.png","Rock");var sand=Save(Source+"/Sandstorm.png","Sandstorm");var cactus=Save(Source+"/Cactus.png","Cactus");var quick=Save(Source+"/Quicksand.png","Quicksand");
        var settings=ScriptableObject.CreateInstance<FastingDesertObstacleArt>();settings.rock=rock;settings.sandstorm=sand;settings.cactus=cactus;settings.quicksand=quick;settings.rockPrefab=Prefab(g,"Rock",FastingTopDownHazard.Kind.Rock,rock);settings.sandstormPrefab=Prefab(g,"Sandstorm",FastingTopDownHazard.Kind.Sand,sand);settings.cactusPrefab=Prefab(g,"Cactus",FastingTopDownHazard.Kind.Cactus,cactus);settings.quicksandPrefab=Prefab(g,"Quicksand",FastingTopDownHazard.Kind.Quicksand,quick);AssetDatabase.CreateAsset(settings,"Assets/Settings/Minigames/FastingDesertObstacleArt.asset");g.desertArt=settings;EditorUtility.SetDirty(g);EditorSceneManager.MarkSceneDirty(g.gameObject.scene);EditorSceneManager.SaveScene(g.gameObject.scene);AssetDatabase.SaveAssets();return "Four transparent 64x64 sprites (15 opaque colors + alpha), four editable prefabs, stage-one art connected.";
    }
}
