using UnityEngine;
namespace CrossTheLine
{
    [DisallowMultipleComponent]
    public sealed class JourneyChapterSpriteSet : MonoBehaviour
    {
        public Sprite jesusIdle,jesusBaptism;
        public Sprite johnIdle,johnWaiting,johnReady,johnBaptizing;
        [Header("Anchors in the original 64x64 PNG, bottom-left origin")]
        public Vector2 jesusFeet=new Vector2(34,1),johnFeet=new Vector2(35,1);
        public Vector2 jesusBaptismHead=new Vector2(38,55),johnBaptismHand=new Vector2(13,45);
        public bool HasBaptismPoses=>jesusBaptism&&johnWaiting&&johnReady&&johnBaptizing;
        public static void Apply(SpriteRenderer renderer,Sprite frame,Vector2 feet)
        {
            renderer.sprite=frame;renderer.flipX=false;renderer.transform.localRotation=Quaternion.identity;
            float scale=frame.pixelsPerUnit/32f;renderer.transform.localScale=Vector3.one*scale;
            renderer.transform.localPosition=-(Vector3)((feet-frame.rect.position-frame.pivot)/32f);
        }
        public static Vector3 CanvasPoint(SpriteRenderer renderer,Vector2 pixel)
        {
            var sprite=renderer.sprite;Vector2 local=(pixel-sprite.rect.position-sprite.pivot)/sprite.pixelsPerUnit;
            if(renderer.flipX)local.x=-local.x;
            return renderer.transform.TransformPoint(local);
        }
    }
}
