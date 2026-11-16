using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using CrossTheLine.Minigames;

// Read-only exporter: all objects are rendered in an unsaved PreviewScene.
public static class ExportTopDownEffectsCurrent
{
    const string Out="Docs/QA/TopDownEffects-20261116";
    static Scene preview;static Camera camera;static Material material;static List<string> rows;
    static GameObject Root(string name){var root=new GameObject(name);root.layer=31;SceneManager.MoveGameObjectToScene(root,preview);return root;}
    static void Save(string name,int width,int height,float ortho)
    {
        camera.orthographicSize=ortho;camera.aspect=(float)width/height;var rt=RenderTexture.GetTemporary(width,height,24,RenderTextureFormat.ARGB32);rt.filterMode=FilterMode.Point;
        var old=RenderTexture.active;camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;var image=new Texture2D(width,height,TextureFormat.RGBA32,false);image.ReadPixels(new Rect(0,0,width,height),0,0);image.Apply();Directory.CreateDirectory(Out+"/png");File.WriteAllBytes(Out+"/png/"+name+".png",image.EncodeToPNG());UnityEngine.Object.DestroyImmediate(image);camera.targetTexture=null;RenderTexture.active=old;RenderTexture.ReleaseTemporary(rt);rows.Add(name+"\t"+width+"x"+height);
    }
    static void Sprite(Sprite sprite,string name)
    {
        var root=Root(name);var renderer=root.AddComponent<SpriteRenderer>();renderer.sharedMaterial=material;renderer.sprite=sprite;root.transform.position=-sprite.bounds.center;
        Save(name,192,192,Mathf.Max(sprite.bounds.size.x,sprite.bounds.size.y)*.6f);UnityEngine.Object.DestroyImmediate(root);
    }
    static void Graphic(MaskableGraphic graphic,string name,int w,int h)
    {
        var helper=new VertexHelper();var method=graphic.GetType().GetMethod("OnPopulateMesh",BindingFlags.Instance|BindingFlags.NonPublic,null,new[]{typeof(VertexHelper)},null);method.Invoke(graphic,new object[]{helper});var mesh=new Mesh();helper.FillMesh(mesh);helper.Dispose();
        var mat=new Material(material);mat.mainTexture=graphic.mainTexture;
        var root=Root(name);root.AddComponent<MeshFilter>().sharedMesh=mesh;root.AddComponent<MeshRenderer>().sharedMaterial=mat;Save(name,w,h,h*.5f);UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(mesh);UnityEngine.Object.DestroyImmediate(mat);
    }
    static void Panels(string name,Rect[] panels,Color color)
    {
        var vh=new VertexHelper();foreach(var r in panels){int i=vh.currentVertCount;vh.AddVert(new Vector3(r.xMin-320,r.yMin-180),color,Vector2.zero);vh.AddVert(new Vector3(r.xMin-320,r.yMax-180),color,Vector2.zero);vh.AddVert(new Vector3(r.xMax-320,r.yMax-180),color,Vector2.zero);vh.AddVert(new Vector3(r.xMax-320,r.yMin-180),color,Vector2.zero);vh.AddTriangle(i,i+1,i+2);vh.AddTriangle(i,i+2,i+3);}
        var mesh=new Mesh();vh.FillMesh(mesh);vh.Dispose();var root=Root(name);root.AddComponent<MeshFilter>().sharedMesh=mesh;root.AddComponent<MeshRenderer>().sharedMaterial=material;Save(name,640,360,180);UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(mesh);
    }
    public static string Export()
    {
        var game=UnityEngine.Object.FindAnyObjectByType<FastingTopDownGame>();if(!game)throw new Exception("Open the fasting top-down scene first.");
        Directory.CreateDirectory(Out);rows=new List<string>();preview=EditorSceneManager.NewPreviewScene();
        try
        {
            camera=Root("Export camera").AddComponent<Camera>();camera.scene=preview;camera.cullingMask=1<<31;camera.orthographic=true;camera.transform.position=new Vector3(0,0,-10);camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.clear;camera.allowHDR=false;camera.allowMSAA=false;camera.nearClipPlane=.01f;camera.farClipPlane=100;
            material=game.hitFeedback.effectMaterial?new Material(game.hitFeedback.effectMaterial):new Material(Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default"));
            Sprite(game.desertArt.rock,"world-rock");Sprite(game.desertArt.cactus,"world-cactus");Sprite(game.desertArt.quicksand,"world-quicksand");
            for(int i=0;i<game.desertArt.sandstormFrames.Length;i++)Sprite(game.desertArt.sandstormFrames[i],"sandstorm-"+i);
            var fire=AssetDatabase.LoadAllAssetsAtPath("Assets/Sprite/Effect/Fire-Sheet.png").OfType<Sprite>().OrderBy(s=>s.name).ToArray();for(int i=0;i<fire.Length;i++)Sprite(fire[i],"fire-"+i);
            foreach(var kind in new[]{FastingTopDownHazard.Kind.Rock,FastingTopDownHazard.Kind.Cactus,FastingTopDownHazard.Kind.Sand,FastingTopDownHazard.Kind.Quicksand})
            {
                var root=Root("local particles");var feedback=root.AddComponent<FastingHitFeedback>();EditorUtility.CopySerialized(game.hitFeedback,feedback);feedback.artwork=null;
                if(root.GetComponent<MeshFilter>()==null)typeof(FastingHitFeedback).GetMethod("Awake",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(feedback,null);
                feedback.enabled=false;
                foreach(float phase in new[]{.2f,.5f,.8f}){feedback.Play(kind);feedback.Advance((kind==FastingTopDownHazard.Kind.Sand?feedback.sandAirSeconds:kind==FastingTopDownHazard.Kind.Quicksand?feedback.quicksandSinkSeconds:feedback.duration)*phase);camera.transform.position=new Vector3(0,.55f,-10);Save("hit-"+kind.ToString().ToLowerInvariant()+"-"+(int)(phase*100),192,192,2.15f);}
                camera.transform.position=new Vector3(0,0,-10);UnityEngine.Object.DestroyImmediate(root);
            }
            foreach(var kind in new[]{FastingTopDownHazard.Kind.Rock,FastingTopDownHazard.Kind.Cactus,FastingTopDownHazard.Kind.Sand,FastingTopDownHazard.Kind.Quicksand})
            {
                var root=Root("edge graphic");var effect=root.AddComponent<FastingScreenHitFeedback>();EditorUtility.CopySerialized(game.screenHitFeedback,effect);effect.rectTransform.sizeDelta=new Vector2(640,360);effect.rectTransform.pivot=new Vector2(.5f,.5f);effect.Play(kind);effect.Advance(effect.effectDuration*.3f);Graphic(effect,"edge-"+kind.ToString().ToLowerInvariant(),640,360);UnityEngine.Object.DestroyImmediate(root);
            }
            {
                var root=Root("shield graphic");var effect=root.AddComponent<FastingScreenHitFeedback>();EditorUtility.CopySerialized(game.screenHitFeedback,effect);effect.rectTransform.sizeDelta=new Vector2(640,360);effect.rectTransform.pivot=new Vector2(.5f,.5f);effect.Play(FastingTopDownHazard.Kind.Rock,true);effect.Advance(effect.effectDuration*.3f);Graphic(effect,"edge-shield",640,360);UnityEngine.Object.DestroyImmediate(root);
            }
            foreach(bool fever in new[]{false,true})
            {
                var root=Root("fever graphic");var ring=root.AddComponent<FastingFeverRing>();ring.rectTransform.sizeDelta=new Vector2(160,160);ring.rectTransform.pivot=new Vector2(.5f,.5f);ring.thickness=8;ring.Set(fever?1:.67f,fever);Graphic(ring,fever?"ring-fever":"ring-charging",192,192);UnityEngine.Object.DestroyImmediate(root);
            }
            Panels("screen-vision",new[]{new Rect(0,0,249.6f,360),new Rect(390.4f,0,249.6f,360),new Rect(249.6f,0,140.8f,72),new Rect(249.6f,216,140.8f,144)},new Color(.03f,.025f,.06f,game.settings.visionOpacity));
            Panels("screen-transition",new[]{new Rect(0,0,640,360)},new Color(.04f,.03f,.04f,.7f));
            var portrait=UnityEngine.Object.FindAnyObjectByType<FastingPortraitHUD>();if(portrait&&portrait.prayerState)
            {
                var colors=new[]{new Color(.65f,.7f,.9f),new Color(1,.82f,.32f),Color.gray,new Color(.45f,.9f,.8f)};var names=new[]{"preparing","praying","blocked","window"};
                for(int i=0;i<names.Length;i++){var root=Root("prayer state");var image=root.AddComponent<Image>();EditorUtility.CopySerialized(portrait.prayerState,image);image.rectTransform.sizeDelta=new Vector2(128,128);image.rectTransform.pivot=new Vector2(.5f,.5f);image.fillAmount=i==0?.5f:1;image.color=colors[i];Graphic(image,"prayer-"+names[i],192,192);UnityEngine.Object.DestroyImmediate(root);}
            }
            // Render the actual player sprite/material to document the white-hit flash.
            {
                var root=Root("player flash");var renderer=root.AddComponent<SpriteRenderer>();renderer.sprite=game.playerBody.sprite;renderer.sharedMaterial=new Material(game.playerBody.sharedMaterial);renderer.sharedMaterial.SetFloat("_Flash",1);root.transform.position=-renderer.sprite.bounds.center;Save("player-white-flash",192,192,1.2f);UnityEngine.Object.DestroyImmediate(renderer.sharedMaterial);UnityEngine.Object.DestroyImmediate(root);
            }
            Extras(game);
            File.WriteAllLines(Out+"/manifest.tsv",rows);File.WriteAllText(Out+"/notes.txt","2026-11-16: Current top-down effect export from actual meshes and sprites. PreviewScene only; original game/scene/assets unchanged.\nLocal particles: 20/50/80 percent; obstacle vignettes: .22 seconds after hit; screen fragments: 30 percent; aura: representative mid-effect states; shield breaking: .15/.55/1.05/1.50 seconds.\nCheckered backgrounds in contact sheets represent transparency. Individual PNGs retain alpha. Screen transition and vision limitation are representative UI states reconstructed from current code; other meshes invoke actual components.\nParticleSystem count in loaded scene: "+UnityEngine.Object.FindObjectsByType<ParticleSystem>(FindObjectsInactive.Include).Length);
            return "Exported "+rows.Count+" transparent PNGs to "+Out;
        }
        finally{if(material)UnityEngine.Object.DestroyImmediate(material);EditorSceneManager.ClosePreviewScene(preview);}
    }
    static void Extras(FastingTopDownGame source)
    {
        foreach(var kind in Enum.GetValues(typeof(FastingTopDownHazard.Kind)).Cast<FastingTopDownHazard.Kind>())
        {
            var root=Root("vignette");var v=root.AddComponent<FastingHitVignette>();if(source.hitVignette)EditorUtility.CopySerialized(source.hitVignette,v);v.rectTransform.sizeDelta=new Vector2(640,360);v.rectTransform.pivot=new Vector2(.5f,.5f);v.PlayDamage(kind);v.Advance(.22f);Graphic(v,"vignette-"+kind.ToString().ToLowerInvariant(),640,360);UnityEngine.Object.DestroyImmediate(root);
        }
        foreach(bool shield in new[]{false,true})
        {
            var root=Root("vignette base");var v=root.AddComponent<FastingHitVignette>();if(source.hitVignette)EditorUtility.CopySerialized(source.hitVignette,v);v.rectTransform.sizeDelta=new Vector2(640,360);v.rectTransform.pivot=new Vector2(.5f,.5f);v.Play(shield);v.Advance(.22f);Graphic(v,shield?"vignette-shield":"vignette-damage",640,360);UnityEngine.Object.DestroyImmediate(root);
        }
        var shieldSprite=AssetDatabase.LoadAllAssetsAtPath("Assets/Sprite/Objects/ShieldOfFaith.aseprite").OfType<Sprite>().First();
        foreach(float time in new[]{.15f,.55f,1.05f,1.5f})
        {
            var root=Root("shield fracture");var g=root.AddComponent<FastingShieldBreakGraphic>();g.rectTransform.sizeDelta=new Vector2(640,360);g.rectTransform.pivot=new Vector2(.5f,.5f);g.Play(shieldSprite,Vector2.zero);typeof(FastingShieldBreakGraphic).GetField("origin",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(g,new Vector2(0,-60));g.Advance(time);Graphic(g,"shield-break-"+Mathf.RoundToInt(time*100),640,360);UnityEngine.Object.DestroyImmediate(root);
        }
        if(!source.playerAura)return;
        var proxyRoot=Root("temporary aura state");var proxy=proxyRoot.AddComponent<FastingTopDownGame>();
        var artRoot=Root("aura bounds reference");var art=artRoot.AddComponent<SpriteRenderer>();art.sprite=source.playerBody.sprite;artRoot.transform.position=-art.sprite.bounds.center;art.forceRenderingOff=true;
        foreach(var name in new[]{"shield","acquire","break","water"})
        {
            typeof(FastingTopDownGame).GetProperty("ShieldCount").SetValue(proxy,name=="shield"||name=="acquire"?1:0);
            typeof(FastingTopDownGame).GetField("waterBoost",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(proxy,name=="water"?2f:0f);
            var root=Root("aura");var aura=root.AddComponent<FastingPlayerAura>();EditorUtility.CopySerialized(source.playerAura,aura);aura.game=proxy;aura.artwork=art;
            typeof(FastingPlayerAura).GetMethod("Awake",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(aura,null);
            if(name=="acquire")aura.AcquireShield();if(name=="break")aura.BreakShield();aura.Advance(.25f);Save("aura-"+name,256,256,2.25f);UnityEngine.Object.DestroyImmediate(root);
        }
        UnityEngine.Object.DestroyImmediate(artRoot);UnityEngine.Object.DestroyImmediate(proxyRoot);
    }
}


