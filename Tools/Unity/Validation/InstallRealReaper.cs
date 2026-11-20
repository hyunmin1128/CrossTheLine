using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using CrossTheLine.Minigames;
public static class InstallRealReaper
{
 const string Prefab="Assets/Prefabs/Minigames/FastingTopDown/TrialObstacles/TrialReaper.prefab";
 const string Report="Docs/QA/RealReaper-20261120";
 public static string Apply()
 {
  if(EditorApplication.isPlaying)throw new Exception("Edit mode required");
  Directory.CreateDirectory(Report);AssetDatabase.Refresh();var frames=new Sprite[3];
  int minX=320,minY=180,maxX=0,maxY=0;
  for(int i=0;i<3;i++){
   string path="Assets/Sprite/Objects/RealReaper"+(i+1)+".png";
   var imp=(TextureImporter)AssetImporter.GetAtPath(path);imp.textureType=TextureImporterType.Sprite;imp.spriteImportMode=SpriteImportMode.Single;imp.spritePixelsPerUnit=32;imp.filterMode=FilterMode.Point;imp.mipmapEnabled=false;imp.textureCompression=TextureImporterCompression.Uncompressed;imp.alphaIsTransparency=true;
   var settings=new TextureImporterSettings();imp.ReadTextureSettings(settings);settings.spriteMeshType=SpriteMeshType.FullRect;settings.spriteAlignment=(int)SpriteAlignment.Center;imp.SetTextureSettings(settings);imp.SaveAndReimport();frames[i]=AssetDatabase.LoadAssetAtPath<Sprite>(path);
   var tex=new Texture2D(2,2);tex.LoadImage(File.ReadAllBytes(path));var pixels=tex.GetPixels32();for(int y=0;y<tex.height;y++)for(int x=0;x<tex.width;x++)if(pixels[y*tex.width+x].a>127){minX=Math.Min(minX,x);maxX=Math.Max(maxX,x);minY=Math.Min(minY,y);maxY=Math.Max(maxY,y);}UnityEngine.Object.DestroyImmediate(tex);
  }
  var root=PrefabUtility.LoadPrefabContents(Prefab);
  try{
   var h=root.GetComponent<FastingTopDownHazard>();var sweep=root.GetComponent<FastingReaperSweep>();
   h.SetArtwork(frames[0]);h.animationFrames=Array.Empty<Sprite>();h.artworkWidth=(maxX-minX+1)/32f;
   h.artworkCenter=new Vector2(((minX+maxX+1)*.5f-160)/32f,((minY+maxY+1)*.5f-90)/32f);
   h.body.transform.localScale=Vector3.one*(2.25f/h.artworkWidth);h.body.transform.localPosition=-(Vector3)h.artworkCenter*h.body.transform.localScale.x;h.body.transform.localRotation=Quaternion.identity;
   sweep.slashFrames=frames;sweep.preparationSeconds=.10f;sweep.cutSeconds=.07f;
   PrefabUtility.SaveAsPrefabAsset(root,Prefab);
  }finally{PrefabUtility.UnloadPrefabContents(root);}
  File.WriteAllText(Report+"/setup.txt","Three original 320x180 frames imported at PPU32/Point/uncompressed. Common canvas and opaque union preserve alignment. One-shot 0.10/0.07 second cut, last pose held. Four-lane collision and straight velocity retained. Original PNG and Aseprite bytes unchanged.");return "PASS RealReaper installed";
 }
 public static async Task<string> Verify()
 {
  if(!EditorApplication.isPlaying)throw new Exception("Play mode required");
  int start=Time.frameCount;while(Time.frameCount<start+4)await Task.Yield();
  var rows=new List<string>();Action<bool,string> check=(ok,label)=>{if(!ok)throw new Exception(label);rows.Add("PASS "+label);};
  var prefab=AssetDatabase.LoadAssetAtPath<FastingTopDownHazard>(Prefab);var h=UnityEngine.Object.Instantiate(prefab);h.transform.position=Vector3.zero;h.ConfigureSweep(3);h.velocity=Vector2.down*5.5f;h.warning=.7f;
  try{
   var sweep=h.GetComponent<FastingReaperSweep>();check(sweep.slashFrames.Length==3&&sweep.slashFrames.Distinct().Count()==3,"Three distinct original sprites assigned");
   float t;h.Advance(.35f,new Vector2(50,50),new Vector2(50,50),out t);check(h.transform.position==Vector3.zero&&!h.body.enabled,"Warning holds movement and hides attack");
   h.Advance(.36f,new Vector2(50,50),new Vector2(50,50),out t);check(h.body.sprite==sweep.slashFrames[0],"Preparation pose");
   h.Advance(.10f,new Vector2(50,50),new Vector2(50,50),out t);check(h.body.sprite==sweep.slashFrames[1],"Cut pose");
   h.Advance(.08f,new Vector2(50,50),new Vector2(50,50),out t);check(h.body.sprite==sweep.slashFrames[2],"Follow-through pose");
   float y=h.transform.position.y;
   for(int i=0;i<30;i++)h.Advance(.05f,new Vector2(50,50),new Vector2(50,50),out t);
   check(h.body.sprite==sweep.slashFrames[2],"No animation looping");check(Mathf.Abs(h.transform.position.x)<.0001f&&Mathf.Abs(h.transform.position.y-y+8.25f)<.001f,"Constant straight downward travel");check(h.body.transform.localRotation==Quaternion.identity,"No rocking or rotation");
   h.velocity=Vector2.zero;var c=h.CollisionCenter;check(h.Advance(.01f,c,c,out t),"Slash center hits");var escape=c+Vector2.right*7.5f;check(!h.Advance(.01f,escape,escape,out t),"Escape outside four lanes remains safe");
   await Task.Yield();var line=h.GetComponentInChildren<LineRenderer>();check(line.positionCount==2&&Mathf.Abs(line.GetPosition(0).y-line.GetPosition(1).y)<.001f,"Slash trail is straight");
   File.WriteAllLines(Report+"/validation.txt",rows);return "PASS "+rows.Count+" checks; real frame advance confirmed";
  }finally{UnityEngine.Object.Destroy(h.gameObject);}
 }
}
