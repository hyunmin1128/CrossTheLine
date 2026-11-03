using UnityEngine;
using System.Linq;
namespace CrossTheLine
{
 [ExecuteAlways,DefaultExecutionOrder(32000)]
 public sealed class JourneySceneryJoin:MonoBehaviour
 {
  public JourneyContinuousWorld world;
  public JourneyChapterSection left,right;
  public SpriteRenderer leftLandscape,rightLandscape;
  [Range(2,16)]public float width=10;
  public Renderer surface; public Material sourceMaterial; public Sprite bakedBackground;
  Material display,original;SpriteRenderer live;float scanAt;
  public static SpriteRenderer Edge(JourneyChapterSection section,bool rightEdge)
  {
   if(!section||!section.preview)return null;
   float edge=rightEdge?section.WorldMaxX:section.WorldMinX;
   return section.preview.GetComponentsInChildren<SpriteRenderer>(true)
    .Where(r=>r.enabled&&r.sprite&&r.bounds.size.x>8&&r.bounds.size.y>3&&r.sortingOrder<0&&IsLandscape(r))
    .OrderBy(r=>Mathf.Abs((rightEdge?r.bounds.max.x:r.bounds.min.x)-edge)).ThenBy(r=>r.sortingOrder).FirstOrDefault();
  }
  static bool IsLandscape(SpriteRenderer r)=>r.name!="SkyCoverage"&&r.name!="SkyOverhang_PlayOnly"&&r.name!="FinalPlateauUnderfill";
  void LateUpdate(){Refresh();}
  public void Refresh()
  {
   if(!world||!left||!right||!surface||!leftLandscape||!rightLandscape)return;
   bool visible=!Application.isPlaying||world.ActiveSection==left||world.ActiveSection==right;
   // Keep the final temptation panorama intact until the player leaves this chapter.
   if(Application.isPlaying&&world.ActiveSection==left&&left.chapterIndex==2&&!world.WalkingToNext)visible=false;
   surface.enabled=visible;if(!visible)return;
   float x=(left.WorldMaxX+right.WorldMinX)*.5f;
   transform.position=new Vector3(x,0,3);transform.localScale=new Vector3(width,40,1);
   if(bakedBackground){var sr=surface as SpriteRenderer;sr.sprite=bakedBackground;transform.localScale=Vector3.one;surface.sharedMaterial=world.GetComponent<JourneyGame>().spriteMaterial;return;}
   SpriteRenderer l=leftLandscape,r=rightLandscape;
   if(Application.isPlaying&&Time.unscaledTime>=scanAt)
   {
    live=world.GetComponentsInChildren<SpriteRenderer>().Where(s=>s.enabled&&s.sprite&&s.bounds.size.x>8&&s.bounds.size.y>3&&s.sortingOrder<0&&IsLandscape(s))
     .OrderBy(s=>Mathf.Abs((world.ActiveSection==left?s.bounds.max.x:s.bounds.min.x)-x)).ThenBy(s=>s.sortingOrder).FirstOrDefault();scanAt=Time.unscaledTime+.15f;
   }
   if(Application.isPlaying&&live){if(world.ActiveSection==left)l=live;else if(world.ActiveSection==right)r=live;}
   if(!display){original=sourceMaterial?sourceMaterial:surface.sharedMaterial;if(!original)return;display=new Material(original){hideFlags=HideFlags.HideAndDontSave};surface.sharedMaterial=display;}
   Set(l,"_LeftTex","_LeftBounds","_LeftUV");Set(r,"_RightTex","_RightBounds","_RightUV");
   display.SetVector("_JoinFlips",new Vector4(l.flipX?1:0,r.flipX?1:0,0,0));display.SetVector("_Join",new Vector4(x,width,0,0));surface.SetPropertyBlock(null);
  }
  public void SuspendMaterial(){if(surface&&original)surface.sharedMaterial=original;if(display){if(Application.isPlaying)Destroy(display);else DestroyImmediate(display);}display=null;original=null;}
  void OnDisable(){SuspendMaterial();}
  void Set(SpriteRenderer r,string tex,string bounds,string uv)
  {
   var b=r.bounds;var s=r.sprite;var rect=s.textureRect;
   display.SetTexture(tex,s.texture);display.SetVector(bounds,new Vector4(b.min.x,b.min.y,b.size.x,b.size.y));
   display.SetVector(uv,new Vector4(rect.x/s.texture.width,rect.y/s.texture.height,rect.width/s.texture.width,rect.height/s.texture.height));
  }
 }
}






