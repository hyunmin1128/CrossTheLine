using System;
using System.IO;
using System.Linq;
using CrossTheLine;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
public static class Chapter02ScrollingInstaller
{
 [MenuItem("Tools/CrossTheLine/Install Chapter 2 Scrolling Prototype")]
 static void MenuInstall(){Debug.Log(Run());}
 public static string Run()
 {
  if(EditorApplication.isPlaying)throw new Exception("Stop Play first");
  string path="Assets/Sprite/Background/Chapter2.png";
  AssetDatabase.ImportAsset(path);var importer=(TextureImporter)AssetImporter.GetAtPath(path);
  importer.textureType=TextureImporterType.Sprite;importer.spritePixelsPerUnit=32;importer.filterMode=FilterMode.Point;importer.mipmapEnabled=false;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.maxTextureSize=2048;importer.SaveAndReimport();
  var active=EditorSceneManager.GetActiveScene();if(active.isDirty){if(active.path=="Assets/Scenes/Chapter02_Baptism.unity")EditorSceneManager.SaveScene(active);else throw new Exception("Save the current scene before installing chapter 2");}
  var scene=EditorSceneManager.OpenScene("Assets/Scenes/Chapter02_Baptism.unity",OpenSceneMode.Single);var game=UnityEngine.Object.FindAnyObjectByType<JourneyGame>();
  foreach(var previous in scene.GetRootGameObjects().Where(o=>o.name=="Chapter02_ScrollingBackground"))UnityEngine.Object.DestroyImmediate(previous);
  var root=new GameObject("Chapter02_ScrollingBackground");root.transform.position=new Vector3(0,.65f,4);
  var sprite=AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().First();
  for(int i=0;i<3;i++)
  {
   var tile=new GameObject("RiverTile_"+(i+1));tile.transform.SetParent(root.transform,false);tile.transform.localPosition=new Vector3((i-1)*30,0,0);
   var renderer=tile.AddComponent<SpriteRenderer>();renderer.sprite=sprite;renderer.sharedMaterial=game.spriteMaterial;renderer.sortingOrder=-100;renderer.flipX=i==1;
  }
  Directory.CreateDirectory("Assets/Prefabs/CrossTheLine/Environments");
  var prefab=PrefabUtility.SaveAsPrefabAssetAndConnect(root,"Assets/Prefabs/CrossTheLine/Environments/Chapter02_JordanPanorama.prefab",InteractionMode.AutomatedAction);
  var settings=game.GetComponent<JordanScrollSettings>();if(!settings)settings=game.gameObject.AddComponent<JordanScrollSettings>();
  settings.backgroundPrefab=prefab;settings.editorBackgroundPreview=root;settings.worldWidth=90;settings.startX=-42;settings.cameraY=.65f;
  var bank=UnityEngine.Object.FindAnyObjectByType<JourneySceneStages>();foreach(var entry in bank.stages)
  {
   string stagePath=PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(entry.root);var stage=PrefabUtility.LoadPrefabContents(stagePath);
   try
   {
    foreach(var renderer in stage.GetComponentsInChildren<SpriteRenderer>(true))if(renderer.name=="SketchBasedJordanRiver"){renderer.enabled=false;var visual=renderer.GetComponent<JourneyEditableVisual>();if(visual)visual.hideInGame=true;}
    var layout=stage.GetComponent<JourneyStageLayout>();if(layout)foreach(var anchor in layout.anchors)if(anchor.key.EndsWith(" / Placement 1")&&Mathf.Abs(anchor.defaultPosition.y+2.45f)<.01f){anchor.defaultPosition.x=-42;anchor.position.x=-42;}
    PrefabUtility.SaveAsPrefabAsset(stage,stagePath);
   }
   finally{PrefabUtility.UnloadPrefabContents(stage);}
  }
  game.journeyCamera.orthographicSize=8.4375f;game.journeyCamera.transform.position=new Vector3(-30,.65f,-10);
  var pixel=game.journeyCamera.GetComponent<UnityEngine.Rendering.Universal.PixelPerfectCamera>();if(pixel){pixel.refResolutionX=960;pixel.refResolutionY=540;pixel.assetsPPU=32;}
  game.sceneBindings.canvas.GetComponent<UnityEngine.UI.CanvasScaler>().referenceResolution=new Vector2(1280,720);
  EditorUtility.SetDirty(game);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
  string report="Chapter02 installed: three960x540/16:9 tile objects,PPU32/scale1,90x16.875 world, camera960x540/size8.4375/fixedY/horizontal follow. Middle tile mirrored to match both seam edges; other chapters untouched.";
  File.WriteAllText("Docs/Chapter02Scrolling-Install.txt",report);return report;
 }
}