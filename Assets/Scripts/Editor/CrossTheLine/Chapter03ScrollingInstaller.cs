using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using CrossTheLine;
public static class Chapter03ScrollingInstaller
{
    [MenuItem("Tools/CrossTheLine/Install Chapter 3 Horizontal Journey")]
    public static void InstallMenu(){Debug.Log(Run());}
    public static string Run()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play mode first");
        var active=EditorSceneManager.GetActiveScene();
        if(active.isDirty)throw new InvalidOperationException("Save the current scene before installing Chapter3");
        var sprites=new Sprite[4];
        for(int i=0;i<4;i++)
        {
            string path="Assets/Sprite/Background/Chapter3"+(i==0?"":"_"+i)+".png";
            AssetDatabase.ImportAsset(path);var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType=TextureImporterType.Sprite;importer.spritePixelsPerUnit=32;importer.filterMode=FilterMode.Point;
            importer.mipmapEnabled=false;importer.textureCompression=TextureImporterCompression.Uncompressed;
            foreach(var target in new[]{"Standalone","Android","iPhone"}){var platform=importer.GetPlatformTextureSettings(target);if(platform.overridden){platform.overridden=false;importer.SetPlatformTextureSettings(platform);}}
            importer.SaveAndReimport();sprites[i]=AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().First();
            if(sprites[i].rect.size!=new Vector2(960,540))throw new InvalidOperationException(path+" must be 960x540");
        }
        var scene=EditorSceneManager.OpenScene("Assets/Scenes/Chapter03_Temptation.unity",OpenSceneMode.Single);
        var game=UnityEngine.Object.FindAnyObjectByType<JourneyGame>();
        string satanPath="Assets/Sprite/CutScene/Satan-64x64.png";
        var satanImporter=(TextureImporter)AssetImporter.GetAtPath(satanPath);
        satanImporter.spritePixelsPerUnit=32;satanImporter.filterMode=FilterMode.Point;satanImporter.mipmapEnabled=false;
        satanImporter.textureCompression=TextureImporterCompression.Uncompressed;satanImporter.SaveAndReimport();
        var satanSprite=AssetDatabase.LoadAllAssetsAtPath(satanPath).OfType<Sprite>().First();
        game.satanFrames=new[]{satanSprite,satanSprite,satanSprite,satanSprite};EditorUtility.SetDirty(game);
        var root=scene.GetRootGameObjects().FirstOrDefault(o=>o.name=="Chapter03_ScrollingBackground");
        string prefabPath="Assets/Prefabs/CrossTheLine/Environments/Chapter03_TemptationPanorama.prefab";
        if(!root)
        {
            root=new GameObject("Chapter03_ScrollingBackground");
            float[] feet={-5.85f,-5.00625f,-3.5375f,-5.85f};
            string[] names={"01_FastingWilderness","02_StonesTrial","03_TemplePinnacle","04_HighMountain"};
            for(int i=0;i<4;i++)
            {
                var segment=new GameObject(names[i]);segment.transform.SetParent(root.transform,false);segment.transform.position=new Vector3(i*30,0,0);
                var data=segment.AddComponent<TemptationScrollSegment>();data.location=i;
                var image=new GameObject("Background");image.transform.SetParent(segment.transform,false);image.transform.localPosition=new Vector3(0,.65f,4);
                var r=image.AddComponent<SpriteRenderer>();r.sprite=sprites[i];r.sharedMaterial=game.spriteMaterial;r.sortingOrder=-100;data.background=r;
                Transform Mark(string name,float x){var o=new GameObject(name);o.transform.SetParent(segment.transform,false);o.transform.localPosition=new Vector3(x,feet[i],0);return o.transform;}
                data.entrance=Mark("Entrance",i==2?-10:-12);data.exit=Mark("Exit",i==2?9:12);
                data.jesusMark=Mark("JesusTrialPosition",i==0?0:-2);data.satanMark=Mark("SatanTrialPosition",2);
            }
            Directory.CreateDirectory(Path.GetDirectoryName(prefabPath));
            PrefabUtility.SaveAsPrefabAssetAndConnect(root,prefabPath,InteractionMode.AutomatedAction);
        }
        var settings=game.GetComponent<TemptationScrollSettings>();if(!settings)settings=game.gameObject.AddComponent<TemptationScrollSettings>();
        settings.backgroundPrefab=AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);settings.editorBackgroundPreview=root;
        settings.segments=root.GetComponentsInChildren<TemptationScrollSegment>();settings.cameraY=.65f;
        var camera=game.journeyCamera;camera.orthographic=true;camera.orthographicSize=8.4375f;camera.transform.position=new Vector3(0,.65f,-10);
        camera.allowMSAA=false;camera.allowHDR=false;camera.allowDynamicResolution=false;
        var pixel=camera.GetComponent<UnityEngine.Rendering.Universal.PixelPerfectCamera>();
        if(pixel){pixel.assetsPPU=32;pixel.refResolutionX=960;pixel.refResolutionY=540;}
        EditorUtility.SetDirty(settings);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        return "Chapter3: four 960x540 backgrounds at PPU32; editable foot markers; fixed 16:9 camera size8.4375; 120-unit horizontal route.";
    }
}
