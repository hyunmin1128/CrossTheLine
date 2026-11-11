using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEditor.U2D.Aseprite;
using UnityEditor.U2D.Sprites;
using System.Threading.Tasks;
using CrossTheLine.Minigames;

public static class NormalizeFastingSprites
{
    const string Report="Docs/QA/SpritePPU32";
    const string Scene="Assets/Scenes/Minigames/Chapter03_Fasting_TopDown_Test.unity";
    static string Key(Sprite s){AssetDatabase.TryGetGUIDAndLocalFileIdentifier(s,out string g,out long id);return g+":"+id;}
    static readonly string[] Names={"Bible","Page","Piece","ShieldOfFaith"};
    static string[] Targets()=>Names.SelectMany(n=>new[]{"Assets/Sprite/Objects/"+n+".png","Assets/Sprite/Objects/"+n+".aseprite"}).Concat(new[]{"Assets/Sprite/Effect/Fire-Sheet.png","Assets/Sprite/Effect/Fire.aseprite"}).ToArray();
    static string[] Prefabs()=>AssetDatabase.GetDependencies(Scene,true).Where(p=>p.EndsWith(".prefab")&&p.StartsWith("Assets/")).OrderBy(p=>p,StringComparer.Ordinal).ToArray();
    static void Backup(string p,string dir){if(!File.Exists(p))return;var to=dir+"/"+p;Directory.CreateDirectory(Path.GetDirectoryName(to));File.Copy(p,to,true);}
    [Serializable] public class View {public string path,key;public Vector3 center,size,scale;public float ppu;}
    [Serializable] public class Baseline {public List<View> views=new List<View>();}
    static string Hierarchy(Transform t){return t.parent?Hierarchy(t.parent)+"/"+t.name:t.name;}
    static IEnumerable<SpriteRenderer> Renderers(GameObject root)=>root.GetComponentsInChildren<SpriteRenderer>(true);
    static IEnumerable<SpriteRenderer> SceneRenderers()=>SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(Renderers);
    static void Capture(IEnumerable<SpriteRenderer> renderers,string owner,Baseline data)
    {
        var targets=new HashSet<string>(Targets());
        foreach(var sr in renderers)if(sr.sprite&&targets.Contains(AssetDatabase.GetAssetPath(sr.sprite)))
        {
            if(sr.drawMode!=SpriteDrawMode.Simple||sr.transform.childCount!=0||sr.GetComponent<Collider2D>())throw new Exception("Requires separate scale handling: "+Hierarchy(sr.transform));
            data.views.Add(new View{path=owner+"|"+Hierarchy(sr.transform),key=Key(sr.sprite),center=sr.bounds.center,size=sr.bounds.size,scale=sr.transform.localScale,ppu=sr.sprite.pixelsPerUnit});
        }
    }
    static void Adjust(IEnumerable<SpriteRenderer> renderers,string owner,Baseline data,List<string> log)
    {
        foreach(var sr in renderers)
        {
            var old=data.views.SingleOrDefault(v=>v.path==owner+"|"+Hierarchy(sr.transform));if(old==null)continue;
            if(!sr.sprite||Key(sr.sprite)!=old.key)throw new Exception("Sprite reference changed: "+old.path);
            var scale=old.scale;float ratio=sr.sprite.pixelsPerUnit/old.ppu;scale.x*=ratio;scale.y*=ratio;scale.z=1;
            if((sr.transform.localScale-scale).sqrMagnitude>0.00000001f){sr.transform.localScale=scale;EditorUtility.SetDirty(sr.transform);if(PrefabUtility.IsPartOfPrefabInstance(sr.transform))PrefabUtility.RecordPrefabInstancePropertyModifications(sr.transform);}
            if(Vector2.Distance(sr.bounds.center,old.center)>.0001f||Vector2.Distance(sr.bounds.size,old.size)>.0001f)throw new Exception("Visual bounds changed: "+old.path+" was="+old.center+" / "+old.size+" now="+sr.bounds);
            log.Add("PASS preserved visual bounds and sprite ID: "+old.path+" scale="+scale);
        }
    }
    static ISpriteEditorDataProvider PageProvider()
    {
        var factory=new SpriteDataProviderFactories();factory.Init();var provider=factory.GetSpriteEditorDataProviderFromObject(AssetImporter.GetAtPath("Assets/Sprite/Objects/Page.png"));provider.InitSpriteEditorDataProvider();
        var edit=provider.GetDataProvider<ISpriteFrameEditCapability>();if(edit==null||!edit.GetEditCapability().HasCapability(EEditCapability.EditSpriteRect))throw new Exception("Page importer does not support safe rectangle edits.");return provider;
    }
    public static string Apply()
    {
        if(File.Exists(Report+"/apply.txt"))throw new Exception("One-off migration already applied. Restore a backup before repeating.");
        if(EditorApplication.isPlaying)throw new Exception("Exit Play mode before applying.");
        if(SceneManager.GetActiveScene().path!=Scene)throw new Exception("Expected fasting scene active.");
        Directory.CreateDirectory(Report);PageProvider();
        string backup="Docs/Backups/SpritePPU32-"+DateTime.Now.ToString("yyyyMMdd-HHmmss");
        var targets=Targets();var prefabs=Prefabs();
        foreach(var p in targets.Select(p=>p+".meta").Concat(prefabs).Append(Scene))Backup(p,backup);
        // Preserve the user's in-memory edits as the baseline before import operations.
        var scene=SceneManager.GetActiveScene();if(scene.isDirty){EditorSceneManager.SaveScene(scene);Backup(Scene,backup+"/SavedUserState");}
        var baseline=new Baseline();Capture(SceneRenderers(),Scene,baseline);
        foreach(var p in prefabs)Capture(Renderers(AssetDatabase.LoadAssetAtPath<GameObject>(p)),p,baseline);
        File.WriteAllLines(Report+"/baseline.txt",baseline.views.Select(v=>v.path+" | "+v.key+" | ppu="+v.ppu+" | center="+v.center+" | size="+v.size+" | scale="+v.scale));
        var identities=targets.ToDictionary(p=>p,p=>AssetDatabase.LoadAllAssetsAtPath(p).OfType<Sprite>().Select(Key).OrderBy(k=>k).ToArray());
        var log=new List<string>{"Backup: "+backup,"Pipeline: "+UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline.GetType().FullName};
        foreach(var p in targets)
        {
            var importer=AssetImporter.GetAtPath(p);
            if(importer is TextureImporter texture){texture.spritePixelsPerUnit=32;texture.filterMode=FilterMode.Point;texture.mipmapEnabled=false;texture.textureCompression=TextureImporterCompression.Uncompressed;texture.alphaIsTransparency=true;texture.SaveAndReimport();}
            else if(importer is AsepriteImporter ase){ase.spritePixelsPerUnit=32;ase.filterMode=FilterMode.Point;ase.mipmapEnabled=false;ase.SaveAndReimport();}
            else throw new Exception("Unsupported importer: "+p);
            var after=AssetDatabase.LoadAllAssetsAtPath(p).OfType<Sprite>().ToArray();
            if(!identities[p].SequenceEqual(after.Select(Key).OrderBy(k=>k))||after.Any(s=>Mathf.Abs(s.pixelsPerUnit-32)>.001f))throw new Exception("Sprite IDs or PPU invalid: "+p);
            log.Add("PASS import PPU32/Point/no mipmaps; IDs retained: "+p+" sprites="+after.Length);
        }
        // Expand the outdated PNG crop without deleting/recreating its Sprite subasset.
        var provider=PageProvider();var rects=provider.GetSpriteRects();if(rects.Length!=1)throw new Exception("Expected one Page PNG sprite.");
        var image=new Texture2D(2,2);image.LoadImage(File.ReadAllBytes("Assets/Sprite/Objects/Page.png"));int minX=image.width,minY=image.height,maxX=-1,maxY=-1;
        for(int y=0;y<image.height;y++)for(int x=0;x<image.width;x++)if(image.GetPixel(x,y).a>0){minX=Math.Min(minX,x);minY=Math.Min(minY,y);maxX=Math.Max(maxX,x);maxY=Math.Max(maxY,y);}
        minX=Math.Max(0,minX-1);minY=Math.Max(0,minY-1);maxX=Math.Min(image.width-1,maxX+1);maxY=Math.Min(image.height-1,maxY+1);UnityEngine.Object.DestroyImmediate(image);
        rects[0].rect=new Rect(minX,minY,maxX-minX+1,maxY-minY+1);provider.SetSpriteRects(rects);provider.Apply();AssetImporter.GetAtPath("Assets/Sprite/Objects/Page.png").SaveAndReimport();
        if(!identities["Assets/Sprite/Objects/Page.png"].SequenceEqual(AssetDatabase.LoadAllAssetsAtPath("Assets/Sprite/Objects/Page.png").OfType<Sprite>().Select(Key).OrderBy(k=>k)))throw new Exception("Page PNG subasset ID changed.");
        log.Add("PASS Page PNG crop expanded to "+rects[0].rect);
        foreach(var p in prefabs)
        {
            if(!baseline.views.Any(v=>v.path.StartsWith(p+"|",StringComparison.Ordinal)))continue;
            var root=PrefabUtility.LoadPrefabContents(p);try{Adjust(Renderers(root),p,baseline,log);PrefabUtility.SaveAsPrefabAsset(root,p);}finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        Adjust(SceneRenderers(),Scene,baseline,log);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        File.WriteAllLines(Report+"/apply.txt",log);return "PASS normalized "+targets.Length+" assets; preserved "+baseline.views.Count+" renderer bounds; backup="+backup;
    }
    public static string Audit()
    {
        Directory.CreateDirectory(Report);
        var lines=new List<string>();
        lines.Add("Playing="+EditorApplication.isPlaying+" pipeline="+(UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline?UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline.GetType().FullName:"BuiltIn"));
        for(int i=0;i<SceneManager.sceneCount;i++){var s=SceneManager.GetSceneAt(i);lines.Add("OPEN "+s.path+" dirty="+s.isDirty);}
        var paths=AssetDatabase.GetDependencies(Scene,true).Where(p=>p.StartsWith("Assets/")).ToArray();
        foreach(var p in paths.Where(p=>p.EndsWith(".png")||p.EndsWith(".aseprite")))
        {
            var sprites=AssetDatabase.LoadAllAssetsAtPath(p).OfType<Sprite>().ToArray();if(sprites.Length==0)continue;
            lines.Add("ASSET "+p+" sprites="+sprites.Length+" ppu="+sprites[0].pixelsPerUnit);
        }
        foreach(var p in paths.Where(p=>p.EndsWith(".prefab")))
        {
            var root=AssetDatabase.LoadAssetAtPath<GameObject>(p);if(!root)continue;
            foreach(var sr in root.GetComponentsInChildren<SpriteRenderer>(true))if(sr.sprite)lines.Add("PREFAB "+p+" / "+sr.name+" sprite="+AssetDatabase.GetAssetPath(sr.sprite)+" id="+Key(sr.sprite)+" scale="+sr.transform.localScale+" bounds="+sr.bounds+" draw="+sr.drawMode);
        }
        File.WriteAllLines(Report+"/before.txt",lines);return "Audit written: "+lines.Count+" rows";
    }
    static void Check(bool pass,string text,List<string> log){if(!pass)throw new Exception(text);log.Add("PASS "+text);}
    static int Missing(GameObject root)
    {
        int count=0;foreach(var t in root.GetComponentsInChildren<Transform>(true))
        {
            count+=GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject);
            foreach(var component in t.GetComponents<Component>())if(component)
            {
                var so=new SerializedObject(component);var it=so.GetIterator();while(it.NextVisible(true))if(it.propertyType==SerializedPropertyType.ObjectReference&&it.objectReferenceValue==null&&!it.objectReferenceEntityIdValue.Equals(default(UnityEngine.EntityId)))count++;
            }
        }
        return count;
    }
    public static string VerifyEdit()
    {
        if(File.Exists(Report+"/size-validation.txt"))throw new Exception("Initial normalization check is archived; use FinalAudit after pickup fitting.");
        var log=new List<string>();var baseline=new Baseline();
        foreach(var p in Prefabs())
        {
            var root=PrefabUtility.LoadPrefabContents(p);try{Adjust(Renderers(root),p,baseline,log);Check(Missing(root)==0,"No missing scripts/object references: "+p,log);if(baseline.views.Any(v=>v.path.StartsWith(p+"|",StringComparison.Ordinal)))PrefabUtility.SaveAsPrefabAsset(root,p);}finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        Adjust(SceneRenderers(),Scene,baseline,log);
        foreach(var root in SceneManager.GetActiveScene().GetRootGameObjects())Check(Missing(root)==0,"No missing scripts/object references: scene / "+root.name,log);
        var game=UnityEngine.Object.FindAnyObjectByType<FastingTopDownGame>();
        foreach(var e in game.entities)if(e){Check(e.footprint.x>0&&e.footprint.y>0,"Valid pickup collision footprint: "+e.name,log);}
        var world=SceneRenderers().Where(sr=>sr.sprite).ToArray();Check(world.All(sr=>Mathf.Abs(sr.sprite.pixelsPerUnit-32)<.001f),"All "+world.Length+" scene world SpriteRenderers use PPU32",log);
        var image=new Texture2D(2,2);image.LoadImage(File.ReadAllBytes("Assets/Sprite/Objects/Page.png"));var rect=AssetDatabase.LoadAllAssetsAtPath("Assets/Sprite/Objects/Page.png").OfType<Sprite>().Single().rect;int clipped=0;
        for(int y=0;y<image.height;y++)for(int x=0;x<image.width;x++)if(image.GetPixel(x,y).a>0&&!rect.Contains(new Vector2(x+.5f,y+.5f)))clipped++;UnityEngine.Object.DestroyImmediate(image);Check(clipped==0,"Page PNG contains every visible pixel",log);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());EditorSceneManager.SaveScene(SceneManager.GetActiveScene());AssetDatabase.SaveAssets();File.WriteAllLines(Report+"/edit-validation.txt",log);return "PASS "+log.Count+" editor checks";
    }
    static async Task Frames(int count){int end=Time.frameCount+count;while(Time.frameCount<end)await Task.Yield();}
    static void FitPickup(FastingTopDownEntity entity,List<string> log)
    {
        if(!entity||entity.kind==FastingTopDownEntity.Kind.Cover||entity.kind==FastingTopDownEntity.Kind.Water)return;
        var sr=entity.GetComponentsInChildren<SpriteRenderer>(true).SingleOrDefault(r=>r.name=="Symbol");if(!sr||!sr.sprite)return;
        var size=sr.sprite.bounds.size;float limit=Mathf.Min(entity.footprint.x,entity.footprint.y)-.05f;
        float scale=Mathf.Min(1,limit/Mathf.Max(size.x,size.y));sr.transform.localScale=new Vector3(scale,scale,1);
        sr.transform.localPosition=-Vector3.Scale(sr.sprite.bounds.center,sr.transform.localScale);sr.transform.localPosition=new Vector3(sr.transform.localPosition.x,sr.transform.localPosition.y,0);
        if(PrefabUtility.IsPartOfPrefabInstance(sr.transform))PrefabUtility.RecordPrefabInstancePropertyModifications(sr.transform);
        Check(Vector2.Distance(sr.bounds.center,entity.transform.position)<.001f,"Pickup picture centered on collection area: "+entity.name,log);
        Check(sr.bounds.size.x<=entity.footprint.x+.001f&&sr.bounds.size.y<=entity.footprint.y+.001f,"Pickup picture fits collection area: "+entity.name+" size="+sr.bounds.size,log);
    }
    public static string OptimizePickups()
    {
        if(EditorApplication.isPlaying)throw new Exception("Exit Play mode before fitting pickups.");var log=new List<string>();
        foreach(var name in new[]{"Bible","Page","Piece","ShieldOfFaith","Fire"})
        {
            string p="Assets/Prefabs/Minigames/FastingTopDown/"+name+"Pickup.prefab";var root=PrefabUtility.LoadPrefabContents(p);
            try{FitPickup(root.GetComponent<FastingTopDownEntity>(),log);PrefabUtility.SaveAsPrefabAsset(root,p);}finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        var game=UnityEngine.Object.FindAnyObjectByType<FastingTopDownGame>();foreach(var entity in game.entities)FitPickup(entity,log);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());EditorSceneManager.SaveScene(SceneManager.GetActiveScene());AssetDatabase.SaveAssets();File.WriteAllLines(Report+"/size-validation.txt",log);return "PASS "+log.Count+" pickup alignment/size checks";
    }
    public static string FixFireSheet()
    {
        const string path="Assets/Sprite/Effect/Fire-Sheet.png";
        var before=AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().Select(Key).OrderBy(k=>k).ToArray();var factory=new SpriteDataProviderFactories();factory.Init();var provider=factory.GetSpriteEditorDataProviderFromObject(AssetImporter.GetAtPath(path));provider.InitSpriteEditorDataProvider();
        var edit=provider.GetDataProvider<ISpriteFrameEditCapability>();if(edit==null||!edit.GetEditCapability().HasCapability(EEditCapability.EditSpriteRect)||!edit.GetEditCapability().HasCapability(EEditCapability.EditPivot))throw new Exception("Fire sheet safe frame edit not supported.");
        var rects=provider.GetSpriteRects().OrderBy(r=>r.name,StringComparer.Ordinal).ToArray();if(rects.Length!=4)throw new Exception("Expected four fire frames.");
        for(int i=0;i<rects.Length;i++){rects[i].rect=new Rect(i*64,0,64,64);rects[i].pivot=new Vector2(.5f,.5f);rects[i].alignment=SpriteAlignment.Center;}
        provider.SetSpriteRects(rects);provider.Apply();AssetImporter.GetAtPath(path).SaveAndReimport();
        if(!before.SequenceEqual(AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().Select(Key).OrderBy(k=>k)))throw new Exception("Fire frame IDs changed.");
        File.WriteAllText(Report+"/fire-crop-validation.txt","PASS four 64x64 frame regions; centered pivots; all original Sprite GUID/local IDs retained.\n");
        return OptimizePickups();
    }
    public static string FinalAudit()
    {
        var log=new List<string>();foreach(var p in Prefabs()){var root=AssetDatabase.LoadAssetAtPath<GameObject>(p);Check(Missing(root)==0,"No missing references in "+p,log);}
        foreach(var root in SceneManager.GetActiveScene().GetRootGameObjects())Check(Missing(root)==0,"No missing references in scene / "+root.name,log);
        var renderers=SceneRenderers().Where(r=>r.sprite).ToArray();Check(renderers.All(r=>r.sprite.pixelsPerUnit==32),"All "+renderers.Length+" scene world renderers PPU32",log);
        File.WriteAllLines(Report+"/final-validation.txt",log);return "PASS "+log.Count+" final reference/PPU checks";
    }
    public static string Preview()
    {
        if(!EditorApplication.isPlaying)throw new Exception("Play mode only.");var game=UnityEngine.Object.FindAnyObjectByType<FastingTopDownGame>();game.Begin();game.enabled=false;
        var names=new[]{"Bible","Page","Piece","ShieldOfFaith","Fire"};for(int i=0;i<names.Length;i++){var root=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Minigames/FastingTopDown/"+names[i]+"Pickup.prefab"));root.transform.position=new Vector3((i-2)*2.1f,game.gameCamera.transform.position.y+1.5f,0);}
        return "Temporary Play mode preview: Bible/Page/Piece/Shield/Fire (left to right)";
    }
    public static async Task<string> VerifyPlay()
    {
        if(!EditorApplication.isPlaying)throw new Exception("Enter Play mode first.");
        var log=new List<string>();var errors=new List<string>();Application.LogCallback capture=(message,trace,type)=>{if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)errors.Add(message);};Application.logMessageReceived+=capture;
        FastingTopDownGame game=null;
        try
        {
            var start=Time.frameCount;await Frames(8);Check(Time.frameCount>start,"Real Play mode frames advancing",log);
            game=UnityEngine.Object.FindAnyObjectByType<FastingTopDownGame>();Check(game!=null,"Fasting scene loaded",log);game.Begin();game.enabled=false;
            foreach(var name in new[]{"Bible","Page","Piece","ShieldOfFaith","Fire"})
            {
                game.Begin();var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Minigames/FastingTopDown/"+name+"Pickup.prefab");var root=UnityEngine.Object.Instantiate(prefab);var e=root.GetComponent<FastingTopDownEntity>();root.transform.position=game.player.position;e.stage=0;
                var others=game.entities;game.entities=new[]{e};float prayer=game.Prayer;game.Tick(.001f,Vector2.zero);
                Check(e.consumed&&!e.gameObject.activeSelf,"Actual collision collects "+name,log);
                if(name=="ShieldOfFaith")Check(game.Shielded,"Shield applied",log);else if(name=="Fire")Check(game.FireGauge>0,"Fire gauge advanced",log);else Check(game.Prayer>prayer,"Scripture increases prayer: "+name,log);
                var sr=root.GetComponentsInChildren<SpriteRenderer>(true).Single(r=>r.name=="Symbol");Check(sr.sprite&&Mathf.Abs(sr.sprite.pixelsPerUnit-32)<.001f&&sr.bounds.size.x<=e.footprint.x+.001f&&sr.bounds.size.y<=e.footprint.y+.001f,"Valid normalized sprite within collection area: "+name,log);
                if(name=="Fire")
                {
                    root.SetActive(true);var loop=root.GetComponentInChildren<FastingFireSpriteLoop>(true);Check(loop&&loop.frames.Length==4&&loop.frames.All(s=>s&&s.pixelsPerUnit==32&&s.rect.width==64&&s.rect.height==64),"All four complete 64x64 fire frames retain references and PPU32",log);var seen=new HashSet<Sprite>();for(int i=0;i<40;i++){await Frames(1);seen.Add(loop.target.sprite);}Check(seen.Count>1,"Fire animation advances",log);
                }
                game.entities=others;UnityEngine.Object.Destroy(root);
            }
            game.Begin();Check(game.TryDamage("PPU validation"),"Damage applies",log);game.RefreshHealthHearts();Check(game.hearts.Count(h=>h.sprite==game.emptyHeart)==1,"One empty heart after damage",log);game.UseWater();game.RefreshHealthHearts();Check(game.hearts.All(h=>h.sprite==game.fullHeart),"Water restores full hearts",log);
            var sand=UnityEngine.Object.Instantiate(game.desertArt.sandstormPrefab);game.desertArt.Apply(sand);sand.ConfigureLanes(1,game.settings.laneSpacing);Check(sand.animationFrames.Length==5&&sand.animationFrames.All(s=>s&&s.pixelsPerUnit==32),"Five valid sandstorm frames",log);var first=sand.body.sprite;sand.Advance(.15f,new Vector2(100,100),new Vector2(100,100),out _);Check(sand.body.sprite!=first,"Sandstorm animation advances",log);UnityEngine.Object.Destroy(sand.gameObject);
            game.Begin();
            var waveField=typeof(FastingTopDownGame).GetField("wave",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
            for(int stage=0;stage<4;stage++)
            {
                Check(game.StageIndex==stage,"Stage loads: "+(stage+1),log);waveField.SetValue(game,float.PositiveInfinity);
                var book=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Minigames/FastingTopDown/BiblePickup.prefab"));game.Collect(book.GetComponent<FastingTopDownEntity>());UnityEngine.Object.Destroy(book);
                Check(game.GateVisible,"Collectible unlocks stage gate: "+(stage+1),log);
                int ticks=0;while(!game.Complete&&game.StageIndex==stage&&ticks++<300)game.Tick(.1f,Vector2.zero);
                Check(stage==3?game.Complete:game.StageIndex==stage+1,"Scripted stage transition: "+(stage+1),log);
            }
            game.Begin();game.enabled=true;float elapsed=game.Elapsed;await Frames(12);Check(game.Elapsed>elapsed,"Game update advances after normalization",log);
            Check(errors.Count==0,"No runtime errors during verification",log);File.WriteAllLines(Report+"/play-validation.txt",log);return "PASS "+log.Count+" runtime checks; limited regression, not full playthrough";
        }
        finally{Application.logMessageReceived-=capture;if(game)game.enabled=true;}
    }
}
