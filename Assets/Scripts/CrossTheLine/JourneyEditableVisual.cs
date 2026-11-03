using UnityEngine;
namespace CrossTheLine
{
 [DefaultExecutionOrder(9800)]
 public sealed class JourneyEditableVisual:MonoBehaviour
 {
  [HideInInspector]public Sprite baselineSprite;[HideInInspector]public Vector3 baselineScale;[HideInInspector]public Color baselineColor;
  public bool overrideSprite,overrideScale,overrideColor;
  [Tooltip("Hide a redundant decorative layer while keeping its authored object editable.")] public bool hideInGame;
  public bool overrideInitialPosition;public Vector3 initialLocalPosition;[HideInInspector]public Vector3 baselineLocalPosition;
  public Sprite sprite;public Vector3 scale=Vector3.one;public Color color=Color.white;
  public bool overrideLayout;public Vector2 anchorMin,anchorMax,offsetMin,offsetMax;
  [HideInInspector]public Vector2 baselineAnchorMin,baselineAnchorMax,baselineOffsetMin,baselineOffsetMax;
  SpriteRenderer body;bool live,editedSprite,editedScale,editedColor;
  RectTransform rect;UnityEngine.UI.Graphic graphic;bool editedLayout;
  bool editedPosition,placed;
  public void BindTemplate(GameObject template)
  {
   body=GetComponent<SpriteRenderer>();var source=template.GetComponent<SpriteRenderer>();
   editedPosition=overrideInitialPosition||template.transform.localPosition!=baselineLocalPosition;if(!overrideInitialPosition)initialLocalPosition=template.transform.localPosition;
   rect=GetComponent<RectTransform>();var sourceRect=template.GetComponent<RectTransform>();
   if(rect&&sourceRect){editedLayout=overrideLayout||sourceRect.anchorMin!=baselineAnchorMin||sourceRect.anchorMax!=baselineAnchorMax||sourceRect.offsetMin!=baselineOffsetMin||sourceRect.offsetMax!=baselineOffsetMax;if(!overrideLayout){anchorMin=sourceRect.anchorMin;anchorMax=sourceRect.anchorMax;offsetMin=sourceRect.offsetMin;offsetMax=sourceRect.offsetMax;}}
   graphic=GetComponent<UnityEngine.UI.Graphic>();var sourceGraphic=template.GetComponent<UnityEngine.UI.Graphic>();
   if(!body||!source){if(graphic&&sourceGraphic){editedColor=overrideColor||sourceGraphic.color!=baselineColor;if(!overrideColor)color=sourceGraphic.color;}live=true;return;}
   editedSprite=overrideSprite||source.sprite!=baselineSprite;if(!overrideSprite)sprite=source.sprite;
   editedScale=overrideScale||template.transform.localScale!=baselineScale;if(!overrideScale)scale=template.transform.localScale;
   editedColor=overrideColor||source.color!=baselineColor;if(!overrideColor)color=source.color;
   live=true;
  }
  Vector3 ResolvedScale()
  {
   var result=scale;
   // Saved primitive edits describe world dimensions of a unit-square sprite.
   // Keep those dimensions when presentation supplies a native-pixel replacement.
   if(body&&body.sprite&&baselineSprite&&JourneySpriteAssets.IsPrimitive(baselineSprite)&&!JourneySpriteAssets.IsPrimitive(body.sprite))
   {
    var from=baselineSprite.bounds.size;var to=body.sprite.bounds.size;
    if(to.x>0)result.x*=from.x/to.x;if(to.y>0)result.y*=from.y/to.y;
   }
   return result;
  }
  void LateUpdate(){if(!live)return;if(editedPosition&&!placed){transform.localPosition=initialLocalPosition;placed=true;}if(body){if(hideInGame)body.enabled=false;if(editedSprite&&sprite)body.sprite=sprite;if(editedScale)transform.localScale=ResolvedScale();if(editedColor)body.color=color;}if(graphic&&editedColor)graphic.color=color;if(rect&&editedLayout){rect.anchorMin=anchorMin;rect.anchorMax=anchorMax;rect.offsetMin=offsetMin;rect.offsetMax=offsetMax;}}
 }
}
