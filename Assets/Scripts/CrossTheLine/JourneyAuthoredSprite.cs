using UnityEngine;
namespace CrossTheLine
{
 public sealed class JourneyAuthoredSprite:MonoBehaviour
 {
  public Sprite sprite;public bool shape,preserveColor;public Color authoredColor=Color.white;public Vector3 authoredScale=Vector3.one;
  bool applied;
  void LateUpdate()
  {
   if(applied)return;applied=true;var r=GetComponent<SpriteRenderer>();
   if(!shape||!sprite||!r||!r.sprite)return;
   if(JourneySpriteAssets.IsPrimitive(sprite))return;
   if(r.sprite.texture.width>1&&!r.sprite.texture.name.StartsWith("NativePixel_")&&!r.sprite.texture.name.StartsWith("Jordan")&&!r.sprite.texture.name.StartsWith("Desert"))return;
   r.sprite=sprite;r.transform.localScale=authoredScale;
   if(preserveColor)r.color=new Color(authoredColor.r,authoredColor.g,authoredColor.b,r.color.a);
   if(name=="몸통"&&transform.parent&&transform.parent.name.StartsWith("돌항아리"))
    foreach(Transform child in transform.parent)if(child.name=="항아리 어깨"||child.name=="목"||child.name=="입")child.gameObject.SetActive(false);
  }
 }

}
