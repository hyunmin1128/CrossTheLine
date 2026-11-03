using UnityEngine;
namespace CrossTheLine
{
    public sealed partial class JordanBaptism
    {
        JourneyChapterSpriteSet poses;
        Vector3 johnRestPosition;
        public SpriteRenderer JohnBody=>johnRenderer;
        public SpriteRenderer JesusBody=>jesusRenderer;
        public bool UsesAuthoredPoses=>poses&&poses.HasBaptismPoses;
        public Vector3 BaptismHeadPoint=>JourneyChapterSpriteSet.CanvasPoint(jesusRenderer,poses.jesusBaptismHead);
        public Vector3 BaptismHandPoint=>JourneyChapterSpriteSet.CanvasPoint(johnRenderer,poses.johnBaptismHand);
        Vector3 BaptismContactOffset=>(Vector3)((poses.jesusBaptismHead-poses.jesusFeet-poses.johnBaptismHand+poses.johnFeet)/32f);
        void JohnPose(Sprite sprite){JourneyChapterSpriteSet.Apply(johnRenderer,sprite,poses.johnFeet);}
        void ResetJohnPose()
        {
            if(!UsesAuthoredPoses)return;
            john.position=johnRestPosition;JohnPose(CurrentStage==Stage.Complete?poses.johnIdle:poses.johnWaiting);
        }
        void AlignBaptizingHand(Vector3 head)
        {
            john.position+=head-JourneyChapterSpriteSet.CanvasPoint(johnRenderer,poses.johnBaptismHand);
        }
        void AnimateAuthoredBaptism()
        {
            float t=phaseTime;arm.gameObject.SetActive(false);jesus.position=WaterPosition;
            if(t<.7f||t>=6.2f)game.BaptismWalk(false,1);
            else JourneyChapterSpriteSet.Apply(jesusRenderer,poses.jesusBaptism,poses.jesusFeet);
            if(t<1.2f)
            {
                JohnPose(poses.johnReady);
                john.position=Vector3.Lerp(johnRestPosition,WaterPosition+BaptismContactOffset,Mathf.SmoothStep(0,1,t/1.2f));
            }
            else if(t<5.5f){JohnPose(poses.johnBaptizing);AlignBaptizingHand(BaptismHeadPoint);}
            else
            {
                JohnPose(t<6.5f?poses.johnReady:poses.johnWaiting);
                john.position=Vector3.Lerp(WaterPosition+BaptismContactOffset,johnRestPosition,Mathf.SmoothStep(0,1,(t-5.5f)/1.7f));
            }
            subtitle.text=t<1.2f?"예수님이 고개를 숙이고 세례를 받으십니다.":t<5.5f?"요한이 예수님께 세례를 베풉니다.":"세례를 받으신 예수님이 일어나십니다.";
            UpdateWater();
            if(t>=7.2f){ResetJohnPose();game.BaptismWalk(false,1);game.BaptismAdvance();}
        }
        void AnimateAuthoredVisitor(float t)
        {
            arm.gameObject.SetActive(false);
            if(t>=3.5f&&t<6.6f)
            {
                JohnPose(poses.johnBaptizing);
                var bounds=JourneySpriteAssets.VisibleBounds(visitorRenderer.sprite);
                var head=visitorRenderer.transform.TransformPoint(new Vector3(bounds.center.x,bounds.max.y-.15f,0));
                AlignBaptizingHand(head);
            }
            else{john.position=johnRestPosition;JohnPose(t>=2.6f&&t<7.3f?poses.johnReady:poses.johnWaiting);}
        }
    }
}
