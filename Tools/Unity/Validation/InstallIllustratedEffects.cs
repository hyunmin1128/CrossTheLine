using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using CrossTheLine.Minigames;

public static class InstallIllustratedEffects
{
    const string Folder="Assets/Sprite/Effect/Fasting/",Asset="Assets/Settings/Minigames/FastingEffectSprites.asset",Report="Docs/QA/IllustratedEffects-20261116";
    static void Backup(string p,string root){if(!File.Exists(p))return;var to=root+"/"+p;Directory.CreateDirectory(Path.GetDirectoryName(to));File.Copy(p,to,true);}
    static Sprite[] Frames(string name,int count)=>Enumerable.Range(0,count).Select(i=>AssetDatabase.LoadAssetAtPath<Sprite>(Folder+name+"_"+i+".png")).ToArray();
    public static string Apply()
    {
        if(File.Exists(Report+"/setup.txt"))throw new Exception("One-off setup already applied; inspect the existing artwork asset instead of rerunning.");
        if(EditorApplication.isPlaying)throw new Exception("Edit mode required");var game=UnityEngine.Object.FindAnyObjectByType<FastingTopDownGame>();if(!game||!game.hitFeedback||!game.playerAura)throw new Exception("Expected top-down game, feedback, and aura");
        Directory.CreateDirectory(Report);const string backup="Docs/Backups/IllustratedEffects-20261116-141613";Backup(game.gameObject.scene.path,backup);
        if(game.gameObject.scene.isDirty){EditorSceneManager.SaveScene(game.gameObject.scene);Backup(game.gameObject.scene.path,backup+"/SavedUserState");}
        AssetDatabase.Refresh();
        foreach(var path in Directory.GetFiles(Folder,"*.png"))
        {
            var p=path.Replace('\\','/');var importer=(TextureImporter)AssetImporter.GetAtPath(p);importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.spritePixelsPerUnit=32;importer.filterMode=FilterMode.Point;importer.mipmapEnabled=false;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.alphaIsTransparency=true;importer.SaveAndReimport();
        }
        var art=AssetDatabase.LoadAssetAtPath<FastingEffectSprites>(Asset);if(!art){art=ScriptableObject.CreateInstance<FastingEffectSprites>();AssetDatabase.CreateAsset(art,Asset);}
        art.shieldBreak=Frames("ShieldBreak",6);art.bubble=Frames("Bubble",4);art.water=Frames("Water",4);art.prayer=Frames("Prayer",4);art.rocks=Frames("Rock",3);art.thorns=Frames("Thorn",3);art.sand=Frames("Sand",3);art.spark=AssetDatabase.LoadAssetAtPath<Sprite>(Folder+"Spark.png");EditorUtility.SetDirty(art);
        var sprites=art.shieldBreak.Concat(art.bubble).Concat(art.water).Concat(art.prayer).Concat(art.rocks).Concat(art.thorns).Concat(art.sand).Append(art.spark).ToArray();if(sprites.Any(s=>!s))throw new Exception("Missing generated sprite");
        game.effectSprites=art;game.hitFeedback.effectSprites=art;EditorUtility.SetDirty(game);EditorUtility.SetDirty(game.hitFeedback);
        if(PrefabUtility.IsPartOfPrefabInstance(game))PrefabUtility.RecordPrefabInstancePropertyModifications(game);if(PrefabUtility.IsPartOfPrefabInstance(game.hitFeedback))PrefabUtility.RecordPrefabInstancePropertyModifications(game.hitFeedback);
        EditorSceneManager.MarkSceneDirty(game.gameObject.scene);EditorSceneManager.SaveScene(game.gameObject.scene);AssetDatabase.SaveAssets();
        File.WriteAllText(Report+"/setup.txt","PASS 28 imported sprites; PPU32 / Point / no compression / no mipmaps.\nConfig: "+Asset+"\nOriginal visual source preserved: "+Folder+"Source/IllustratedEffectsAtlas.png\nAll gameplay timings, collision sizes, shield cap and invulnerability rules remain owned by existing logic.\nBackup: "+backup);return "PASS 28 artwork sprites installed";
    }
    static async Task Frames(int n){int end=Time.frameCount+n;while(Time.frameCount<end)await Task.Yield();}
    static void Field(FastingTopDownGame game,string field,object value)=>typeof(FastingTopDownGame).GetField(field,BindingFlags.NonPublic|BindingFlags.Instance).SetValue(game,value);
    static void Shield(FastingTopDownGame game){var root=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Minigames/FastingTopDown/ShieldOfFaithPickup.prefab"));game.Collect(root.GetComponent<FastingTopDownEntity>());UnityEngine.Object.Destroy(root);}
    public static async Task<string> Verify()
    {
        if(!EditorApplication.isPlaying)throw new Exception("Play mode required");var rows=new List<string>();var errors=new List<string>();Application.LogCallback capture=(m,s,t)=>{if(t==LogType.Error||t==LogType.Exception||t==LogType.Assert)errors.Add(m);};Application.logMessageReceived+=capture;
        var game=UnityEngine.Object.FindAnyObjectByType<FastingTopDownGame>();Action<bool,string> check=(ok,text)=>{if(!ok)throw new Exception(text);rows.Add("PASS "+text);};
        try
        {
            await Frames(5);game.Begin();game.enabled=false;check(game.effectSprites&&game.hitFeedback.effectSprites==game.effectSprites,"Game and feedback reference artwork asset");
            foreach(var kind in new[]{FastingTopDownHazard.Kind.Rock,FastingTopDownHazard.Kind.Cactus,FastingTopDownHazard.Kind.Sand,FastingTopDownHazard.Kind.Quicksand})
            {
                game.hitFeedback.Play(kind);game.hitFeedback.Advance(.15f);var visible=game.hitFeedback.GetComponentsInChildren<SpriteRenderer>().Where(r=>r.name.StartsWith("IllustratedParticle")&&r.enabled).ToArray();check(visible.Length==20&&visible.All(r=>r.sprite),"Twenty real illustrated particles render for "+kind);check(!game.hitFeedback.GetComponent<MeshRenderer>().enabled,"Legacy square particle mesh hidden: "+kind);game.hitFeedback.Advance(3);check(visible.All(r=>!r.enabled),"Particle pool clears: "+kind);
            }
            Shield(game);Shield(game);Shield(game);game.playerAura.Advance(.15f);var visual=game.playerAura.GetComponent<FastingIllustratedAura>();check(visual&&game.ShieldCount==3,"Illustrated aura created with three orbiting shields");check(visual.GetComponentsInChildren<SpriteRenderer>().Any(r=>r.name=="IllustratedShieldMembrane"&&r.enabled&&r.sprite),"Illustrated membrane visible");
            float health=game.Health;game.TryDamage("Rock");var graphic=game.GetComponent<FastingShieldOrbit>().BreakGraphic;check(graphic.effectSprites==game.effectSprites&&game.ShieldCount==2&&game.Health==health,"Illustrated shield break consumes one shield without health loss");
            var seen=new HashSet<Texture>();for(int i=0;i<17;i++){seen.Add(graphic.mainTexture);graphic.Advance(.1f);}check(seen.Count==6,"All six authored shield fracture frames displayed");graphic.Advance(1);check(!graphic.Active,"Authored shield break clears at original duration");
            for(int i=0;i<30;i++)game.TryDamage("repeat");check(game.ShieldCount==2&&game.Health==health&&game.Invulnerable,"Thirty repeated hits during immunity consume nothing else");
            game.UseWater();game.playerAura.Advance(.1f);check(visual.WaterVisible,"Illustrated water trail active during boost");Field(game,"waterBoost",0f);game.playerAura.Advance(.1f);check(!visual.WaterVisible,"Water trail hides when boost ends");
            game.Begin();game.enabled=false;game.Tick(.1f,Vector2.zero,true);game.Tick(.1f,Vector2.zero,true);game.Tick(.1f,Vector2.zero,true);game.Tick(.1f,Vector2.zero,true);game.playerAura.Advance(.1f);check(visual.PrayerVisible,"Authored prayer light renders while praying/preparing");
            game.Begin();game.playerAura.Advance(0);check(!visual.PrayerVisible&&!visual.WaterVisible&&game.ShieldCount==0,"Restart clears all illustrated visual states");check(errors.Count==0,"No runtime errors during regression");File.WriteAllLines(Report+"/validation.txt",rows);game.enabled=true;return "PASS "+rows.Count+" checks";
        }
        catch(Exception e){File.WriteAllLines(Report+"/validation.txt",rows.Concat(new[]{"FAIL "+e}));throw;}
        finally{Application.logMessageReceived-=capture;if(game)game.enabled=true;}
    }
    public static string Preview()
    {
        var game=UnityEngine.Object.FindAnyObjectByType<FastingTopDownGame>();game.Begin();game.enabled=false;Shield(game);Shield(game);Shield(game);game.UseWater();for(int i=0;i<4;i++)game.Tick(.1f,Vector2.zero,true);Field(game,"grace",0f);game.Tick(0,Vector2.zero,true);game.playerAura.Advance(.25f);Time.timeScale=0;return "Illustrated shield, water and prayer preview";
    }
    public static string PreviewBreak(){var game=UnityEngine.Object.FindAnyObjectByType<FastingTopDownGame>();Field(game,"grace",0f);Field(game,"waterGrace",0f);game.TryDamage("Rock");game.GetComponent<FastingShieldOrbit>().BreakGraphic.Advance(1.15f);game.playerAura.Advance(.2f);return "Illustrated authored shield fragments";}
}
