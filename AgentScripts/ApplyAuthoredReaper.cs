using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using CrossTheLine.Minigames;
public static class ApplyAuthoredReaper
{
    public static string Run()
    {
        if(EditorApplication.isPlaying)throw new Exception("Edit mode required");
        AssetDatabase.Refresh();
        var frames=new Sprite[16];
        for(int i=0;i<16;i++)
        {
            string path="Assets/Sprite/Objects/RealReaper"+(i+1)+".png";
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
            importer.spritePixelsPerUnit=32;importer.filterMode=FilterMode.Point;
            importer.textureCompression=TextureImporterCompression.Uncompressed;importer.mipmapEnabled=false;
            importer.npotScale=TextureImporterNPOTScale.None;importer.alphaIsTransparency=true;
            var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);
            settings.spriteAlignment=(int)SpriteAlignment.Center;settings.spritePivot=new Vector2(.5f,.5f);
            settings.spriteMeshType=SpriteMeshType.FullRect;importer.SetTextureSettings(settings);importer.SaveAndReimport();
            frames[i]=AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if(!frames[i]||frames[i].rect.size!=new Vector2(320,180))throw new Exception("Unexpected authored canvas: "+path);
        }
        const string prefabPath="Assets/Prefabs/Minigames/FastingTopDown/TrialObstacles/TrialReaper.prefab";
        const string backup="Docs/Backups/AuthoredReaper-20261121";Directory.CreateDirectory(backup);
        if(!File.Exists(backup+"/TrialReaper.prefab"))File.Copy(prefabPath,backup+"/TrialReaper.prefab");
        var root=PrefabUtility.LoadPrefabContents(prefabPath);
        try
        {
            var h=root.GetComponent<FastingTopDownHazard>();var sweep=root.GetComponent<FastingReaperSweep>();
            h.body.sprite=frames[0];h.animationFrames=Array.Empty<Sprite>();
            sweep.slashFrames=frames;sweep.firstSwingFrame=6;sweep.attackAnimationSeconds=.4f;
            PrefabUtility.SaveAsPrefabAsset(root,prefabPath);
        }
        finally{PrefabUtility.UnloadPrefabContents(root);}
        const string clipPath="Assets/Animations/Minigames/FastingTopDown/RealReaper/RealReaperAttack.anim";
        if(!File.Exists(backup+"/RealReaperAttack.anim"))File.Copy(clipPath,backup+"/RealReaperAttack.anim");
        var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);clip.ClearCurves();clip.frameRate=100;
        float[] times={.18f,.15f,.14f,.14f,.12f,.12f,.04f,.04f,.04f,.04f,.04f,.04f,.04f,.04f,.04f,.04f};
        var keys=new ObjectReferenceKeyframe[17];float t=0;
        for(int i=0;i<16;i++){keys[i]=new ObjectReferenceKeyframe{time=t,value=frames[i]};t+=times[i];}
        keys[16]=new ObjectReferenceKeyframe{time=t,value=frames[15]};
        AnimationUtility.SetObjectReferenceCurve(clip,new EditorCurveBinding{path="Artwork",type=typeof(SpriteRenderer),propertyName="m_Sprite"},keys);
        var cs=AnimationUtility.GetAnimationClipSettings(clip);cs.loopTime=false;AnimationUtility.SetAnimationClipSettings(clip,cs);EditorUtility.SetDirty(clip);
        var idle=AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Animations/Minigames/FastingTopDown/RealReaper/RealReaperIdle.anim");
        if(idle){idle.ClearCurves();AnimationUtility.SetObjectReferenceCurve(idle,new EditorCurveBinding{path="Artwork",type=typeof(SpriteRenderer),propertyName="m_Sprite"},new[]{new ObjectReferenceKeyframe{time=0,value=frames[0]},new ObjectReferenceKeyframe{time=.2f,value=frames[0]}});EditorUtility.SetDirty(idle);}
        AssetDatabase.SaveAssets();
        return "Applied 16 authored PNG frames; shared canvas=320x180; PPU=32; clip="+t+"s; runtime windup=.85s, swing=.4s. Original drawings preserved.";
    }
}
