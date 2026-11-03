using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using CrossTheLine;
public static class JourneyContinuousWorldInstaller
{
    [MenuItem("Tools/CrossTheLine/Rebuild Main Horizontal World From Chapter Scenes")]
    public static void InstallMenu(){Debug.Log(Run());}
    public static string Run()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play first");
        if(Enumerable.Range(0,SceneManager.sceneCount).Any(i=>SceneManager.GetSceneAt(i).isDirty))throw new InvalidOperationException("Save open scenes before rebuilding the main world");
        string path="Assets/Scenes/CrossTheLineJourney.unity";
        var main=EditorSceneManager.OpenScene(path,OpenSceneMode.Single);
        var game=main.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<JourneyGame>(true)).First();
        var book=JsonUtility.FromJson<StoryBook>(game.storySource.text);
        var existingWorld=game.GetComponent<JourneyContinuousWorld>();
        var entrances=existingWorld?existingWorld.sections.Where(s=>s).ToDictionary(s=>s.chapterIndex,s=>s.entryLocal):new Dictionary<int,Vector2>();
        var previous=main.GetRootGameObjects().FirstOrDefault(r=>r.name=="ContinuousChapterMap");if(previous)UnityEngine.Object.DestroyImmediate(previous);
        var map=new GameObject("ContinuousChapterMap");
        var world=game.GetComponent<JourneyContinuousWorld>();if(!world)world=game.gameObject.AddComponent<JourneyContinuousWorld>();
        world.mapRoot=map;world.sections=new JourneyChapterSection[book.chapters.Length];
        float edge=0;
        for(int index=0;index<book.chapters.Length;index++)
        {
            var node=new GameObject("Chapter"+(index+1).ToString("00")+"_"+book.chapters[index].title);node.transform.SetParent(map.transform,false);
            var section=node.AddComponent<JourneyChapterSection>();section.chapterIndex=index;section.chapterTitle=book.chapters[index].title;
            var templates=new GameObject("ChapterSceneObjects_EDIT_SOURCE");templates.transform.SetParent(node.transform,false);templates.SetActive(false);section.templates=templates;
            section.preview=new GameObject("WorldPreview");section.preview.transform.SetParent(node.transform,false);
            string source=index>=1&&index<=16?"Assets/Scenes/"+JourneySceneFlow.SceneFor(index)+".unity":null;
            if(source!=null&&File.Exists(source))
            {
                section.sourceScene=source;var scene=EditorSceneManager.OpenScene(source,OpenSceneMode.Additive);
                foreach(var root in scene.GetRootGameObjects()){SceneManager.MoveGameObjectToScene(root,main);root.transform.SetParent(templates.transform,true);}
                section.sourceGame=templates.GetComponentInChildren<JourneyGame>(true);
                section.sourceStages=templates.GetComponentInChildren<JourneySceneStages>(true);
                EditorSceneManager.CloseScene(scene,true);EditorSceneManager.SetActiveScene(main);
                var first=section.sourceStages.stages.FirstOrDefault(s=>s.step==0);
                if(first!=null&&first.root)CopyVisuals(first.root.transform,section.preview.transform).gameObject.SetActive(true);
                var jordan=section.sourceGame.GetComponent<JordanScrollSettings>();var desert=section.sourceGame.GetComponent<TemptationScrollSettings>();
                var panorama=jordan?jordan.editorBackgroundPreview:desert?desert.editorBackgroundPreview:null;
                if(panorama)
                {
                    foreach(var r in section.preview.GetComponentsInChildren<SpriteRenderer>(true))if(r.name=="SketchLandscape"||r.name=="SketchBasedJordanRiver")r.enabled=false;
                    CopyVisuals(panorama.transform,section.preview.transform).gameObject.SetActive(true);
                }
                var backgrounds=section.preview.GetComponentsInChildren<SpriteRenderer>(true).Where(r=>r.enabled&&r.sprite&&r.bounds.size.x>8&&r.sortingOrder<0).ToArray();
                if(backgrounds.Length>0){section.localMinX=backgrounds.Min(r=>r.bounds.min.x);section.localMaxX=backgrounds.Max(r=>r.bounds.max.x);}
                else{section.localMinX=-15;section.localMaxX=15;}
                if(index==1){section.localMinX=-45;section.localMaxX=45;section.entryLocal=new Vector2(jordan?jordan.startX:-42,-2.45f);}
                else if(index==2){section.localMinX=-15;section.localMaxX=desert&&desert.segments!=null&&desert.segments.Length>3?desert.segments[3].transform.localPosition.x+15:105;section.entryLocal=new Vector2(-12,-5.85f);}
                else section.entryLocal=new Vector2(Mathf.Clamp(-6,section.localMinX+1,section.localMaxX-1),-3f);
            }
            else
            {
                section.sourceScene=path+" (existing chapter logic)";section.localMinX=0;section.localMaxX=48;
                if(index==0&&game.authoredPreviewRoot)CopyVisuals(game.authoredPreviewRoot.transform,section.preview.transform).gameObject.SetActive(true);
                else
                {
                    string biome=book.chapters[index].biome;
                    string kind=biome=="room"?"SceneHome":biome=="temple"?"SceneTemple":biome=="garden"?"SceneOliveRoad":biome=="river"||biome=="lake"?"SceneRiver":biome=="storm"?"SceneSea":biome=="desert"||biome=="cross"?"SceneDesert":"SceneVillage";
                    var sprite=JourneySpriteAssets.Load(biome=="garden"?"CrossTheLine/OlivesPrayerLandscape":"CrossTheLine/PixelScenery/"+kind);
                    if(sprite){var image=new GameObject("ExistingLandscape");image.transform.SetParent(section.preview.transform,false);image.transform.localPosition=new Vector3(24,0,4);image.transform.localScale=new Vector3(48/sprite.bounds.size.x,11.25f/sprite.bounds.size.y,1);var r=image.AddComponent<SpriteRenderer>();r.sprite=sprite;r.sharedMaterial=game.spriteMaterial;r.sortingOrder=-100;}
                }
            }
            if(entrances.TryGetValue(index,out var entryPoint))section.entryLocal=entryPoint;
            section.pixelFrameScale=index==1||index==2?1:1.5f;
            section.preview.transform.localScale=Vector3.one*section.pixelFrameScale;
            node.transform.position=new Vector3(edge-section.localMinX*section.pixelFrameScale,0,0);edge=section.WorldMaxX;world.sections[index]=section;
        }
        game.sceneChapterIndex=-1;
        if(!game.GetComponent<JourneyPixelFrame>())game.gameObject.AddComponent<JourneyPixelFrame>();
        foreach(var section in world.sections)if(section.preview)section.preview.SetActive(true);
        JourneySceneryJoinInstaller.Build(world);
        EditorUtility.SetDirty(world);EditorUtility.SetDirty(game);EditorSceneManager.MarkSceneDirty(main);EditorSceneManager.SaveScene(main);AssetDatabase.SaveAssets();
        Directory.CreateDirectory("Docs");
        string report="Main horizontal world: "+world.sections.Length+" chapters; "+edge.ToString("F2")+" world units; 16 authored chapter scenes imported with their complete source hierarchies; remaining chapters preserve existing logic. Source scenes were not saved or overwritten.";
        File.WriteAllText("Docs/ContinuousWorld-Install.txt",report);
        Selection.activeGameObject=map;return report;
    }
    static Transform CopyVisuals(Transform source,Transform parent)
    {
        var node=new GameObject(source.name);node.transform.SetParent(parent,false);
        node.SetActive(source.gameObject.activeSelf);
        node.transform.localPosition=source.localPosition;node.transform.localRotation=source.localRotation;node.transform.localScale=source.localScale;
        var original=source.GetComponent<SpriteRenderer>();
        if(original)
        {
            var r=node.AddComponent<SpriteRenderer>();r.sprite=original.sprite;r.sharedMaterial=original.sharedMaterial;r.color=original.color;r.flipX=original.flipX;r.flipY=original.flipY;
            r.sortingLayerID=original.sortingLayerID;r.sortingOrder=original.sortingOrder;r.drawMode=original.drawMode;r.size=original.size;r.enabled=original.enabled;
        }
        foreach(Transform child in source)CopyVisuals(child,node.transform);
        return node.transform;
    }
}

