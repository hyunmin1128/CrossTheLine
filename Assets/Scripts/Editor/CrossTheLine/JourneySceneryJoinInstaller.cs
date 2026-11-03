using System.Linq;
using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using CrossTheLine;
public static class JourneySceneryJoinInstaller
{
 [MenuItem("Tools/CrossTheLine/Refresh Chapter Background Joins")]
 public static void MenuRun(){Debug.Log(Run());}
 public static string Run()
 {
  if(EditorApplication.isPlaying)throw new Exception("Stop Play first");
  var scene=EditorSceneManager.OpenScene("Assets/Scenes/CrossTheLineJourney.unity",OpenSceneMode.Single);
  var world=UnityEngine.Object.FindAnyObjectByType<JourneyContinuousWorld>();Build(world);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();Selection.activeGameObject=world.mapRoot;
  return "26 editable pixel landscape joins saved in Main; original chapter images and scenes retained.";
 }
 static void Bake(JourneySceneryJoin join,int index)
 {
  string dir="Assets/Sprite/Generated/ChapterJoins";Directory.CreateDirectory(dir);
  string path=dir+"/Join"+(index+1).ToString("00")+"To"+(index+2).ToString("00")+".png";
  var oldLayer=join.gameObject.layer;join.gameObject.layer=31;
  var cameraObject=new GameObject("JoinBakeCamera");var camera=cameraObject.AddComponent<Camera>();camera.enabled=false;camera.orthographic=true;camera.orthographicSize=20;camera.transform.position=new Vector3(join.transform.position.x,0,-10);camera.cullingMask=1<<31;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.clear;camera.allowHDR=camera.allowMSAA=false;
  int width=Mathf.RoundToInt(join.width*32);var rt=RenderTexture.GetTemporary(width,1280,24);var active=RenderTexture.active;
  try{camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;var image=new Texture2D(width,1280,TextureFormat.RGBA32,false);image.ReadPixels(new Rect(0,0,width,1280),0,0);image.Apply();File.WriteAllBytes(path,image.EncodeToPNG());UnityEngine.Object.DestroyImmediate(image);}
  finally{camera.targetTexture=null;RenderTexture.active=active;RenderTexture.ReleaseTemporary(rt);join.gameObject.layer=oldLayer;UnityEngine.Object.DestroyImmediate(cameraObject);}
  AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceUpdate);var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.spritePivot=Vector2.one*.5f;importer.npotScale=TextureImporterNPOTScale.None;importer.spritePixelsPerUnit=32;importer.filterMode=FilterMode.Point;importer.mipmapEnabled=false;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.alphaIsTransparency=true;importer.SaveAndReimport();
  join.SuspendMaterial();join.bakedBackground=AssetDatabase.LoadAssetAtPath<Sprite>(path);join.Refresh();
 }
 public static void Build(JourneyContinuousWorld world)
 {
  string folder="Assets/Prefabs/CrossTheLine/Environments";Directory.CreateDirectory(folder);
  string matPath="Assets/Materials/CrossTheLine/ChapterSceneryJoin.mat";
  var mat=AssetDatabase.LoadAssetAtPath<Material>(matPath);if(!mat){mat=new Material(Shader.Find("CrossTheLine/SceneryJoin"));AssetDatabase.CreateAsset(mat,matPath);}
  string prefabPath=folder+"/ChapterSceneryJoin.prefab";
  var template=AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
  if(!template||!template.GetComponent<SpriteRenderer>()){string whitePath="Assets/Sprite/Generated/Scenery/SceneryJoinWhite.png";if(!File.Exists(whitePath)){var pixel=new Texture2D(1,1);pixel.SetPixel(0,0,Color.white);pixel.Apply();File.WriteAllBytes(whitePath,pixel.EncodeToPNG());UnityEngine.Object.DestroyImmediate(pixel);}AssetDatabase.ImportAsset(whitePath);var importer=(TextureImporter)AssetImporter.GetAtPath(whitePath);importer.textureType=TextureImporterType.Sprite;importer.spritePixelsPerUnit=1;importer.filterMode=FilterMode.Point;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.mipmapEnabled=false;importer.SaveAndReimport();var quad=new GameObject("ChapterSceneryJoin");var r=quad.AddComponent<SpriteRenderer>();r.sprite=AssetDatabase.LoadAssetAtPath<Sprite>(whitePath);r.sharedMaterial=mat;r.sortingOrder=-9;r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;r.receiveShadows=false;quad.AddComponent<JourneySceneryJoin>().surface=r;template=PrefabUtility.SaveAsPrefabAsset(quad,prefabPath);UnityEngine.Object.DestroyImmediate(quad);}
  var old=world.mapRoot.transform.Find("BackgroundJoins");var widths=old?old.GetComponentsInChildren<JourneySceneryJoin>(true).ToDictionary(j=>j.name,j=>j.width):new System.Collections.Generic.Dictionary<string,float>();if(old)UnityEngine.Object.DestroyImmediate(old.gameObject);
  var root=new GameObject("BackgroundJoins");root.transform.SetParent(world.mapRoot.transform,false);
  for(int i=0;i<world.sections.Length-1;i++)
  {
   var o=(GameObject)PrefabUtility.InstantiatePrefab(template,root.transform);o.name="Join_"+(i+1).ToString("00")+"_to_"+(i+2).ToString("00");
   var join=o.GetComponent<JourneySceneryJoin>();if(widths.TryGetValue(o.name,out var savedWidth))join.width=savedWidth;join.sourceMaterial=mat;join.world=world;join.left=world.sections[i];join.right=world.sections[i+1];join.leftLandscape=JourneySceneryJoin.Edge(join.left,true);join.rightLandscape=JourneySceneryJoin.Edge(join.right,false);join.Refresh();
   if(!join.leftLandscape||!join.rightLandscape)throw new Exception("Missing join background at "+i);
   Bake(join,i);join.SuspendMaterial();EditorUtility.SetDirty(join);
  }
 }
}





