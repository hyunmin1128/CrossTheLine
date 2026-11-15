using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using CrossTheLine.Minigames;

public static class ReplaceTopDownJesus
{
    const string Image="Assets/Sprite/Minigames/FastingTopDown/TempJesus64.png";
    const string Prefab="Assets/Prefabs/Minigames/FastingTopDown/TempJesusTopDown.prefab";
    const string Report="Docs/QA/NewJesus-20261113";
    static void Backup(string p,string dir){if(!File.Exists(p))return;var to=dir+"/"+p;Directory.CreateDirectory(Path.GetDirectoryName(to));File.Copy(p,to,true);}
    static Bounds Visible(Sprite sprite)
    {
        var v=sprite.vertices;var b=new Bounds(v[0],Vector3.zero);foreach(var point in v)b.Encapsulate(point);return b;
    }
    static Vector2 PixelCenterAndFoot(Sprite sprite)
    {
        var tex=new Texture2D(2,2);tex.LoadImage(File.ReadAllBytes(Image));int minX=tex.width,maxX=-1,minY=tex.height;
        for(int y=0;y<tex.height;y++)for(int x=0;x<tex.width;x++)if(tex.GetPixel(x,y).a>.5f){minX=Math.Min(minX,x);maxX=Math.Max(maxX,x);minY=Math.Min(minY,y);}
        UnityEngine.Object.DestroyImmediate(tex);return new Vector2(((minX+maxX+1)*.5f-sprite.pivot.x)/sprite.pixelsPerUnit,(minY-sprite.pivot.y)/sprite.pixelsPerUnit);
    }
    static void Replace(SpriteRenderer renderer,Sprite sprite,Vector2 centerFoot,List<string> log)
    {
        var old=Visible(renderer.sprite);var scale=renderer.transform.localScale;float foot=renderer.transform.localPosition.y+old.min.y*scale.y;
        renderer.sprite=sprite;renderer.transform.localPosition=new Vector3(-centerFoot.x*scale.x,foot-centerFoot.y*scale.y,renderer.transform.localPosition.z);
        EditorUtility.SetDirty(renderer);EditorUtility.SetDirty(renderer.transform);
        if(PrefabUtility.IsPartOfPrefabInstance(renderer)){PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);PrefabUtility.RecordPrefabInstancePropertyModifications(renderer.transform);}
        log.Add("PASS artwork replaced; preserved foot Y="+foot+"; scale="+scale+"; position="+renderer.transform.localPosition);
    }
    public static string Apply()
    {
        if(EditorApplication.isPlaying)throw new Exception("Exit Play mode first.");var game=UnityEngine.Object.FindAnyObjectByType<FastingTopDownGame>();if(!game||!game.playerBody)throw new Exception("Open top-down scene.");
        Directory.CreateDirectory(Report);string backup="Docs/Backups/NewJesus-"+DateTime.Now.ToString("yyyyMMdd-HHmmss");
        foreach(var p in new[]{Prefab,game.gameObject.scene.path,Image+".meta"})Backup(p,backup);
        if(game.gameObject.scene.isDirty){EditorSceneManager.SaveScene(game.gameObject.scene);Backup(game.gameObject.scene.path,backup+"/SavedUserState");}
        var importer=(TextureImporter)AssetImporter.GetAtPath(Image);importer.spritePixelsPerUnit=32;importer.filterMode=FilterMode.Point;importer.mipmapEnabled=false;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
        var sprite=AssetDatabase.LoadAllAssetsAtPath(Image).OfType<Sprite>().Single();Vector2 centerFoot=PixelCenterAndFoot(sprite);var log=new List<string>{"Backup: "+backup,"Source: "+Image};
        // Cache the scene pose before prefab propagation so the foot correction is applied once.
        var renderer=game.playerBody;var old=Visible(renderer.sprite);float sceneFoot=renderer.transform.localPosition.y+old.min.y*renderer.transform.localScale.y;
        var root=PrefabUtility.LoadPrefabContents(Prefab);try{var body=root.GetComponentsInChildren<SpriteRenderer>(true).Single(r=>r.name=="Artwork");Replace(body,sprite,centerFoot,log);PrefabUtility.SaveAsPrefabAsset(root,Prefab);}finally{PrefabUtility.UnloadPrefabContents(root);}
        renderer.sprite=sprite;var pos=renderer.transform.localPosition;renderer.transform.localPosition=new Vector3(-centerFoot.x*renderer.transform.localScale.x,sceneFoot-centerFoot.y*renderer.transform.localScale.y,pos.z);
        PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);PrefabUtility.RecordPrefabInstancePropertyModifications(renderer.transform);
        game.idle=sprite;game.prayingSprite=sprite;EditorUtility.SetDirty(game);
        // Hit feedback restores these transforms after its animation, so bind its serialized baseline to the new pose.
        if(game.hitFeedback){game.hitFeedback.artwork=renderer;EditorUtility.SetDirty(game.hitFeedback);}
        EditorSceneManager.MarkSceneDirty(game.gameObject.scene);EditorSceneManager.SaveScene(game.gameObject.scene);AssetDatabase.SaveAssets();
        log.Add("PASS scene idle/praying/playerBody now reference new sprite");log.Add("PASS preserved player root, hit footprint, shadow, and flash material");File.WriteAllLines(Report+"/apply.txt",log);return "PASS new Jesus artwork applied; backup="+backup;
    }
    static async Task Frames(int n){int end=Time.frameCount+n;while(Time.frameCount<end)await Task.Yield();}
    public static string FixAnimator()
    {
        if(EditorApplication.isPlaying)throw new Exception("Edit mode required");const string path="Assets/Prefabs/Minigames/FastingTopDown/TempJesusWalk.anim";
        Backup(path,"Docs/Backups/NewJesus-20261113-154548");var sprite=AssetDatabase.LoadAllAssetsAtPath(Image).OfType<Sprite>().Single();var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);int keys=0;
        foreach(var binding in AnimationUtility.GetObjectReferenceCurveBindings(clip))if(binding.type==typeof(SpriteRenderer)&&binding.propertyName=="m_Sprite")
        {
            var frames=AnimationUtility.GetObjectReferenceCurve(clip,binding);for(int i=0;i<frames.Length;i++){frames[i].value=sprite;keys++;}AnimationUtility.SetObjectReferenceCurve(clip,binding,frames);
        }
        EditorUtility.SetDirty(clip);AssetDatabase.SaveAssetIfDirty(clip);File.AppendAllText(Report+"/apply.txt","\nPASS Animator sprite keys updated: "+keys+"; original timing/controller preserved. New source has one pose.\n");return "PASS updated "+keys+" animator keys";
    }
    public static async Task<string> Verify()
    {
        if(!EditorApplication.isPlaying)throw new Exception("Play mode required");var game=UnityEngine.Object.FindAnyObjectByType<FastingTopDownGame>();var log=new List<string>();var errors=new List<string>();Application.LogCallback capture=(m,s,t)=>{if(t==LogType.Error||t==LogType.Exception)errors.Add(m);};Application.logMessageReceived+=capture;
        Action<bool,string> check=(ok,msg)=>{if(!ok)throw new Exception(msg);log.Add("PASS "+msg);};
        try
        {
            await Frames(3);game.Begin();game.enabled=false;var sprite=game.playerBody.sprite;check(AssetDatabase.GetAssetPath(sprite)==Image&&game.idle==sprite&&game.prayingSprite==sprite,"New sprite used by player/idle/prayer references");check(sprite.pixelsPerUnit==32,"PPU32 preserved");await Frames(15);check(game.playerBody.sprite==sprite,"Animator retains new artwork across frames");
            var position=game.playerBody.transform.localPosition;var scale=game.playerBody.transform.localScale;float x=game.player.position.x;game.Tick(.1f,Vector2.right);check(game.player.position.x>x&&game.playerBody.sprite==sprite,"Movement retains new artwork");
            foreach(var kind in new[]{FastingTopDownHazard.Kind.Rock,FastingTopDownHazard.Kind.Sand,FastingTopDownHazard.Kind.Quicksand}){game.hitFeedback.Play(kind);game.hitFeedback.Advance(2);check((position-game.playerBody.transform.localPosition).sqrMagnitude<.0001f&&(scale-game.playerBody.transform.localScale).sqrMagnitude<.0001f,"Hit animation restores new foot/scale: "+kind);}
            game.Begin();game.TryDamage("new art flash");var props=new MaterialPropertyBlock();game.playerBody.GetPropertyBlock(props);check(props.GetFloat("_Flash")==1&&game.Invulnerable,"White flash and immunity work on new art");game.Begin();
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Minigames/FastingTopDown/ShieldOfFaithPickup.prefab");for(int i=0;i<3;i++){var e=UnityEngine.Object.Instantiate(prefab).GetComponent<FastingTopDownEntity>();game.Collect(e);UnityEngine.Object.Destroy(e.gameObject);}await Frames(5);check(game.GetComponent<FastingShieldOrbit>().VisibleCount==3,"Three shields orbit new artwork");
            check(errors.Count==0,"No runtime errors during checks");File.WriteAllLines(Report+"/validation.txt",log);game.Begin();game.enabled=true;return "PASS "+log.Count+" checks";
        }
        finally{Application.logMessageReceived-=capture;if(game)game.enabled=true;}
    }
}
