using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using CrossTheLine.Minigames;

[InitializeOnLoad]
public static class HeartSandstormSetup
{
    const string Report="Docs/QA/HeartSandstorm";
    const string Full="Assets/Sprite/Objects/Heart.png",Empty="Assets/Sprite/Objects/ZeroHeart.png",Sand="Assets/Sprite/Objects/SandStorm-Sheet.png";
    const string Flag="CrossTheLine.VerifyHeartSandstorm";
    static double next;
    static HeartSandstormSetup()
    {
        EditorApplication.update+=Poll;
        EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Flag,false))VerifyPlay();if(s==PlayModeStateChange.EnteredEditMode)SessionState.SetBool(Flag,false);};
    }
    static void Poll()
    {
        if(EditorApplication.timeSinceStartup<next||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)return;
        next=EditorApplication.timeSinceStartup+1;string path=Report+"/request.txt";if(!File.Exists(path))return;
        string action=File.ReadAllText(path).Trim();File.Delete(path);
        try{if(action=="apply")Apply();else if(action=="layout"){var game=UnityEngine.Object.FindAnyObjectByType<FastingTopDownGame>();Hearts(game,game.fullHeart,game.emptyHeart);EditorSceneManager.MarkSceneDirty(game.gameObject.scene);EditorSceneManager.SaveScene(game.gameObject.scene);AssetDatabase.SaveAssets();}else if(action=="play"){SessionState.SetBool(Flag,true);EditorApplication.isPlaying=true;}else throw new Exception("Unknown request");}
        catch(Exception e){File.WriteAllText(Report+"/status.txt","FAIL "+e);Debug.LogException(e);}
    }
    static void Backup(string file,string backup)
    {
        if(!File.Exists(file))return;string path=backup+"/"+file;Directory.CreateDirectory(Path.GetDirectoryName(path));File.Copy(file,path,true);
    }
    static Sprite[] Import(string path,bool multiple)
    {
        AssetDatabase.ImportAsset(path);var importer=(TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType=TextureImporterType.Sprite;if(!multiple)importer.spriteImportMode=SpriteImportMode.Single;
        importer.spritePixelsPerUnit=32;importer.filterMode=FilterMode.Point;importer.mipmapEnabled=false;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.alphaIsTransparency=true;importer.SaveAndReimport();
        return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().OrderBy(s=>s.name,StringComparer.Ordinal).ToArray();
    }
    static void Hearts(FastingTopDownGame game,Sprite full,Sprite empty)
    {
        game.fullHeart=full;game.emptyHeart=empty;
        var portrait=game.GetComponent<FastingPortraitHUD>();
        if(!portrait)portrait=UnityEngine.Object.FindAnyObjectByType<FastingPortraitHUD>();
        if(!portrait||portrait.game!=game||!portrait.healthFill)throw new Exception("Expected portrait HUD health area.");
        var parent=portrait.healthFill.transform.parent;
        foreach(var old in game.hearts)if(old)old.gameObject.SetActive(false);
        portrait.healthFill.gameObject.SetActive(false);if(portrait.healthText)portrait.healthText.gameObject.SetActive(false);
        var background=parent.GetComponent<UnityEngine.UI.Image>();if(background)background.enabled=true;
        var found=parent.Find("UserHeartRow");
        var row=found?found.GetComponent<RectTransform>():new GameObject("UserHeartRow",typeof(RectTransform)).GetComponent<RectTransform>();
        row.SetParent(parent,false);var healthRect=portrait.healthFill.rectTransform;row.anchorMin=healthRect.anchorMin;row.anchorMax=healthRect.anchorMax;row.pivot=healthRect.pivot;row.anchoredPosition=healthRect.anchoredPosition;row.sizeDelta=healthRect.sizeDelta;row.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,Mathf.Max(28,healthRect.rect.height));row.gameObject.SetActive(true);
        int count=Mathf.CeilToInt(game.settings.maximumHealth);var hearts=new UnityEngine.UI.Image[count];
        for(int i=0;i<count;i++)
        {
            string name="Heart"+(i+1);var child=row.Find(name);
            var image=child?child.GetComponent<UnityEngine.UI.Image>():new GameObject(name,typeof(RectTransform),typeof(UnityEngine.UI.Image)).GetComponent<UnityEngine.UI.Image>();
            image.transform.SetParent(row,false);var rect=image.rectTransform;rect.anchorMin=new Vector2((float)i/count+.01f,.02f);rect.anchorMax=new Vector2((float)(i+1)/count-.01f,.98f);rect.offsetMin=rect.offsetMax=Vector2.zero;
            image.gameObject.SetActive(true);image.sprite=full;image.type=UnityEngine.UI.Image.Type.Simple;image.preserveAspect=true;image.raycastTarget=false;image.color=Color.white;hearts[i]=image;
        }
        game.hearts=hearts;EditorUtility.SetDirty(game);
    }
    public static void Apply()
    {
        Directory.CreateDirectory(Report);for(int i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i).isDirty)throw new Exception("Save open scenes first.");
        var game=UnityEngine.Object.FindAnyObjectByType<FastingTopDownGame>();if(!game||!game.desertArt)throw new Exception("Open the fasting top-down scene.");
        string backup="Docs/Backups/HeartSandstorm-"+DateTime.Now.ToString("yyyyMMdd-HHmmss");
        string scenePath=game.gameObject.scene.path,artPath=AssetDatabase.GetAssetPath(game.desertArt),prefabPath=AssetDatabase.GetAssetPath(game.desertArt.sandstormPrefab);
        foreach(var p in new[]{scenePath,artPath,prefabPath,Full+".meta",Empty+".meta",Sand+".meta"})Backup(p,backup);
        var full=Import(Full,false).Single();var empty=Import(Empty,false).Single();var frames=Import(Sand,true);
        if(frames.Length!=5)throw new Exception("Expected five sandstorm frames; found "+frames.Length);
        Hearts(game,full,empty);var art=game.desertArt;art.sandstorm=frames[0];art.sandstormFrames=frames;art.sandstormFramesPerSecond=8;EditorUtility.SetDirty(art);
        var texture=new Texture2D(2,2);texture.LoadImage(File.ReadAllBytes(Sand));float width=0;
        foreach(var sprite in frames){int low=(int)sprite.rect.xMax,high=-1;for(int y=(int)sprite.rect.yMin;y<(int)sprite.rect.yMax;y++)for(int x=(int)sprite.rect.xMin;x<(int)sprite.rect.xMax;x++)if(texture.GetPixel(x,y).a>.5f){low=Math.Min(low,x);high=Math.Max(high,x);}width=Mathf.Max(width,(high-low+1)/sprite.pixelsPerUnit);}UnityEngine.Object.DestroyImmediate(texture);
        var root=PrefabUtility.LoadPrefabContents(prefabPath);
        try{var hazard=root.GetComponent<FastingTopDownHazard>();hazard.SetArtwork(frames[0]);hazard.SetAnimation(frames,8);hazard.artworkWidth=width;PrefabUtility.SaveAsPrefabAsset(root,prefabPath);}finally{PrefabUtility.UnloadPrefabContents(root);}
        EditorSceneManager.MarkSceneDirty(game.gameObject.scene);EditorSceneManager.SaveScene(game.gameObject.scene);AssetDatabase.SaveAssets();
        File.WriteAllText(Report+"/status.txt","PASS heart slots: "+game.hearts.Length+"; sand animation frames: "+frames.Length+"; backup: "+backup);
    }
    static async Task Frames(int n){int end=Time.frameCount+n;while(Time.frameCount<end)await Task.Yield();}
    static async void VerifyPlay()
    {
        var rows=new List<string>();var errors=new List<string>();int checks=0;
        Action<bool,string> check=(ok,message)=>{if(!ok)throw new Exception(message);checks++;rows.Add("PASS "+message);};
        Application.LogCallback capture=(message,trace,type)=>{if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)errors.Add(message);};Application.logMessageReceived+=capture;
        FastingTopDownGame game=null;
        try
        {
            await Frames(5);game=UnityEngine.Object.FindAnyObjectByType<FastingTopDownGame>();if(!game)throw new Exception("No game in Play mode.");game.Begin();game.enabled=false;
            Action<string,object> field=(name,value)=>typeof(FastingTopDownGame).GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(game,value);
            Action<bool> side=left=>typeof(FastingTopDownGame).GetMethod("SpawnSideSand",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(game,new object[]{left});
            game.RefreshHealthHearts();check(game.hearts.Length==5&&game.hearts.All(h=>h.sprite==game.fullHeart&&h.gameObject.activeInHierarchy),"Five full user hearts visible");
            game.TryDamage("heart test");game.RefreshHealthHearts();check(game.hearts.Count(h=>h.sprite==game.emptyHeart)==1&&game.hearts.All(h=>h.fillAmount==1),"Damage replaces one slot with visible ZeroHeart");
            game.UseWater();game.RefreshHealthHearts();check(game.hearts.All(h=>h.sprite==game.fullHeart),"Healing restores Heart");
            game.Begin();UnityEngine.Random.InitState(20261111);int leftCount=0,rightCount=0,aboveCount=0,others=0;
            for(int i=0;i<300;i++)
            {
                game.SpawnWave();var hazard=game.Hazards.Last();
                if(hazard.kind!=FastingTopDownHazard.Kind.Sand){if(Mathf.Abs(hazard.velocity.x)>.001f)throw new Exception("Another obstacle spawned sideways");others++;continue;}
                if(hazard.velocity.x>0)leftCount++;else if(hazard.velocity.x<0)rightCount++;else aboveCount++;
                if(hazard.animationFrames==null||hazard.animationFrames.Length!=5)throw new Exception("Sand animation missing");
                if(hazard.velocity.x!=0&&Mathf.Abs(hazard.velocity.y-game.CurrentSpeed)>.001f)throw new Exception("Side sand does not match camera scroll");
            }
            check(leftCount>0&&rightCount>0&&aboveCount>0,"Random sand origins: left="+leftCount+", right="+rightCount+", above="+aboveCount);
            check(others>0,"Other obstacles retain vertical-only spawning");
            var animated=game.Hazards.First(h=>h.kind==FastingTopDownHazard.Kind.Sand);var before=animated.body.sprite;
            animated.Advance(.17f,new Vector2(100,100),new Vector2(100,100),out _);check(animated.body.sprite!=before&&AssetDatabase.GetAssetPath(animated.body.sprite)==Sand,"User sandstorm animation advances");
            game.Begin();foreach(var entity in game.entities)entity.gameObject.SetActive(false);side(true);var moving=game.Hazards.Last();moving.transform.position=game.player.position+Vector3.left*3;moving.velocity=new Vector2(6,game.CurrentSpeed);
            for(int i=0;i<6;i++)game.Tick(.1f,Vector2.zero);
            check(game.Health==4&&game.SlowRemaining>0,"Horizontal sand collision damages and slows player");
            game.Begin();side(true);var exiting=game.Hazards.Last();exiting.transform.position=new Vector3(game.gameCamera.transform.position.x+game.gameCamera.orthographicSize*game.gameCamera.aspect+5,game.player.position.y+4);game.Tick(.01f,Vector2.zero);
            check(!game.Hazards.Contains(exiting),"Crossed side storm is cleaned up");
            game.Begin();field("grace",0f);game.TryDamage("preview");field("grace",0f);game.TryDamage("preview");game.Refresh();
            side(true);var left=game.Hazards.Last();left.transform.position=new Vector3(-4,4);left.Advance(.17f,new Vector2(100,100),new Vector2(100,100),out _);
            side(false);var right=game.Hazards.Last();right.transform.position=new Vector3(4,6);right.Advance(.3f,new Vector2(100,100),new Vector2(100,100),out _);
            await Frames(4);check(game.hearts.Count(h=>h.sprite==game.fullHeart)==3&&game.hearts.Count(h=>h.sprite==game.emptyHeart)==2,"Three hearts and two empty hearts remain visible");
            ScreenCapture.CaptureScreenshot(Path.GetFullPath(Report+"/heart-sandstorm-in-game.png"));await Frames(3);check(errors.Count==0,"No runtime errors");
            rows.Add("PASS "+checks+" checks; random sampling of 300 actual waves; Play frames advanced.");File.WriteAllLines(Report+"/play-validation.txt",rows);
        }
        catch(Exception e){File.WriteAllText(Report+"/play-validation.txt","FAIL "+e+"\n"+string.Join("\n",rows));}
        finally{Application.logMessageReceived-=capture;EditorApplication.isPlaying=false;}
    }
}
