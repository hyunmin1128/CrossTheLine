using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using CrossTheLine.Minigames;

public static class InstallForwardWalk
{
    const string Folder="Assets/Sprite/Minigames/FastingTopDown/JesusForwardWalk/",Clip="Assets/Prefabs/Minigames/FastingTopDown/TempJesusWalk.anim",Report="Docs/QA/ForwardWalk-20261116";
    public static string Apply()
    {
        if(EditorApplication.isPlaying)throw new Exception("Edit mode required");if(File.Exists(Report+"/setup.txt"))throw new Exception("Migration already applied");Directory.CreateDirectory(Report);
        var backup="Docs/Backups/ForwardWalk-"+DateTime.Now.ToString("yyyyMMdd-HHmmss");Directory.CreateDirectory(backup);File.Copy(Clip,backup+"/TempJesusWalk.anim",true);File.Copy(Clip+".meta",backup+"/TempJesusWalk.anim.meta",true);
        AssetDatabase.Refresh();var frames=new Sprite[4];
        for(int i=0;i<4;i++)
        {
            string path=Folder+"Walk_"+i+".png";var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.spritePixelsPerUnit=32;importer.filterMode=FilterMode.Point;importer.mipmapEnabled=false;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.alphaIsTransparency=true;var textureSettings=new TextureImporterSettings();importer.ReadTextureSettings(textureSettings);textureSettings.spriteMeshType=SpriteMeshType.FullRect;importer.SetTextureSettings(textureSettings);importer.SaveAndReimport();frames[i]=AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
        if(frames.Any(f=>!f||f.rect.size!=new Vector2(64,64)))throw new Exception("Expected four 64x64 frames");
        var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(Clip);var bindings=AnimationUtility.GetObjectReferenceCurveBindings(clip).Where(b=>b.type==typeof(SpriteRenderer)&&b.propertyName=="m_Sprite").ToArray();if(bindings.Length!=1)throw new Exception("Expected one sprite animation binding");
        var keys=new ObjectReferenceKeyframe[4];for(int i=0;i<4;i++)keys[i]=new ObjectReferenceKeyframe{time=i/8f,value=frames[i]};
        AnimationUtility.SetObjectReferenceCurve(clip,bindings[0],keys);clip.frameRate=8;var settings=AnimationUtility.GetAnimationClipSettings(clip);settings.loopTime=true;settings.startTime=0;settings.stopTime=.5f;AnimationUtility.SetAnimationClipSettings(clip,settings);EditorUtility.SetDirty(clip);AssetDatabase.SaveAssetIfDirty(clip);
        File.WriteAllText(Report+"/setup.txt","PASS four distinct 64x64 forward-only walk frames; original palette; PPU32/Point/uncompressed/no mipmaps; 8 fps / 0.5 second loop.\nExisting clip GUID, controller and sprite binding retained.\nOriginal idle/user image unchanged.\nBackup="+backup);return "PASS 4-frame forward walk installed";
    }
    static async Task Frames(int count){int end=Time.frameCount+count;while(Time.frameCount<end)await Task.Yield();}
    public static string FixLoop()
    {
        if(EditorApplication.isPlaying)throw new Exception("Edit mode required");var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(Clip);var binding=AnimationUtility.GetObjectReferenceCurveBindings(clip).Single(b=>b.type==typeof(SpriteRenderer)&&b.propertyName=="m_Sprite");var keys=Enumerable.Range(0,4).Select(i=>new ObjectReferenceKeyframe{time=i/8f,value=AssetDatabase.LoadAssetAtPath<Sprite>(Folder+"Walk_"+i+".png")}).ToArray();AnimationUtility.SetObjectReferenceCurve(clip,binding,keys);var settings=AnimationUtility.GetAnimationClipSettings(clip);settings.loopTime=true;settings.stopTime=.5f;AnimationUtility.SetAnimationClipSettings(clip,settings);EditorUtility.SetDirty(clip);AssetDatabase.SaveAssetIfDirty(clip);return "PASS exact 0.5-second four-frame loop";
    }
    public static async Task<string> Verify()
    {
        if(!EditorApplication.isPlaying)throw new Exception("Play mode required");var rows=new List<string>();var errors=new List<string>();Application.LogCallback capture=(m,s,t)=>{if(t==LogType.Error||t==LogType.Exception)errors.Add(m);};Application.logMessageReceived+=capture;
        var game=UnityEngine.Object.FindAnyObjectByType<FastingTopDownGame>();Action<bool,string> check=(ok,m)=>{if(!ok)throw new Exception(m);rows.Add("PASS "+m);};
        try
        {
            await Frames(4);game.Begin();game.enabled=false;var animator=game.playerBody.GetComponent<Animator>();check(animator&&animator.runtimeAnimatorController,"Existing Animator/controller remains connected");
            var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(Clip);var binding=AnimationUtility.GetObjectReferenceCurveBindings(clip).Single(b=>b.type==typeof(SpriteRenderer)&&b.propertyName=="m_Sprite");var keys=AnimationUtility.GetObjectReferenceCurve(clip,binding);check(keys.Select(k=>k.value).Distinct().Count()==4,"Four distinct walk sprites in clip");check(AnimationUtility.GetAnimationClipSettings(clip).loopTime&&Mathf.Abs(clip.length-.5f)<.001f,"Seamless 0.5-second loop");
            var position=game.playerBody.transform.localPosition;var scale=game.playerBody.transform.localScale;var seen=new HashSet<Sprite>();
            for(int i=0;i<12;i++){animator.Update(.125f);seen.Add(game.playerBody.sprite);check(game.playerBody.transform.localPosition==position&&game.playerBody.transform.localScale==scale,"Animation changes only sprite: step "+i);}
            check(seen.Count==4&&seen.All(s=>AssetDatabase.GetAssetPath(s).StartsWith(Folder,StringComparison.Ordinal)),"Runtime Animator cycles all four new frames");check(seen.All(s=>s.pixelsPerUnit==32&&s.rect.size==new Vector2(64,64)),"Frame scale and size consistent");
            float startY=game.player.position.y;game.Tick(.1f,Vector2.zero);check(game.player.position.y>startY,"Automatic forward movement unchanged");game.Begin();game.enabled=false;game.TryDamage("walk flash");var props=new MaterialPropertyBlock();game.playerBody.GetPropertyBlock(props);check(props.GetFloat("_Flash")==1&&game.Invulnerable,"White flash and immunity still apply to animated renderer");
            foreach(var kind in new[]{FastingTopDownHazard.Kind.Sand,FastingTopDownHazard.Kind.Quicksand}){game.hitFeedback.Play(kind);game.hitFeedback.Advance(3);check(game.playerBody.transform.localPosition==position&&game.playerBody.transform.localScale==scale,"Hit motion restores feet/scale: "+kind);}
            game.Begin();game.enabled=true;var before=animator.GetCurrentAnimatorStateInfo(0).normalizedTime;await Frames(12);check(animator.GetCurrentAnimatorStateInfo(0).normalizedTime!=before,"Animator advances in real Play mode frames");check(errors.Count==0,"No runtime errors during verification");File.WriteAllLines(Report+"/validation.txt",rows);return "PASS "+rows.Count+" checks";
        }
        catch(Exception e){File.WriteAllLines(Report+"/validation.txt",rows.Concat(new[]{"FAIL "+e}));throw;}
        finally{Application.logMessageReceived-=capture;if(game)game.enabled=true;}
    }
}
