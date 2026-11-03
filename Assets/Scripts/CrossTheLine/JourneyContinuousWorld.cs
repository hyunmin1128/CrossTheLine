using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;
namespace CrossTheLine
{
    // Controllers retain their authored local coordinates. Their real scene objects are
    // placed in the long world after animation, before rendering; no render proxies or scene loads.
    [DefaultExecutionOrder(-30000),DisallowMultipleComponent]
    public sealed class JourneyContinuousWorld : MonoBehaviour
    {
        public JourneyChapterSection[] sections=Array.Empty<JourneyChapterSection>();
        public GameObject mapRoot;
        [Min(1)]public float walkingSpeed=4.5f;
        public JourneyChapterSection ActiveSection{get;private set;}
        public bool WalkingToNext{get;private set;}
        public JourneySceneStages ActiveStages=>ActiveSection?ActiveSection.sourceStages:null;
        JourneyGame game;
        Vector3 presentedOffset,lastCameraWorld,blendFrom;
        float cameraBlendUntil,exitGate,exitStartX,exitStartY;
        Sprite[] baseSatan,baseDove;AudioClip baseVoice;
        void Awake()
        {
            game=GetComponent<JourneyGame>();baseSatan=game.satanFrames;baseDove=game.baptismDoveFrames;baseVoice=game.baptismHeavenVoice;
            var late=GetComponent<JourneyContinuousPresentation>();if(!late)late=gameObject.AddComponent<JourneyContinuousPresentation>();late.world=this;
        }
        void Update(){Normalize();}
        public void Normalize()
        {
            var frame=GetComponent<JourneyPixelFrame>();if(frame)frame.Restore();
            if(presentedOffset==Vector3.zero)return;
            transform.position-=presentedOffset;
            if(game&&game.journeyCamera)game.journeyCamera.transform.position-=presentedOffset;
            presentedOffset=Vector3.zero;
        }
        public void Present()
        {
            if(!game||!ActiveSection||presentedOffset!=Vector3.zero)return;
            Vector3 offset=ActiveSection.transform.position;transform.position+=offset;
            var camera=game.journeyCamera;camera.transform.position+=offset;
            if(Time.unscaledTime<cameraBlendUntil)
            {
                float t=1-(cameraBlendUntil-Time.unscaledTime)/.8f;
                var p=Vector3.Lerp(blendFrom,camera.transform.position,Mathf.SmoothStep(0,1,t));p.x=Mathf.Round(p.x*32)/32f;camera.transform.position=p;
            }
            presentedOffset=offset;lastCameraWorld=camera.transform.position;
        }
        public void ApplyChapter(int index)
        {
            Normalize();
            if(index<0||index>=sections.Length||!sections[index])throw new InvalidOperationException("Missing continuous chapter "+index);
            bool changed=ActiveSection&&ActiveSection!=sections[index];
            ActiveSection=sections[index];WalkingToNext=false;
            foreach(var section in sections)
            {
                if(section.templates)section.templates.SetActive(false);
                if(section.preview)section.preview.SetActive(section!=ActiveSection&&Mathf.Abs(section.chapterIndex-index)<=1);
            }
            CopySettings<JordanScrollSettings>();CopySettings<TemptationScrollSettings>();CopySettings<JourneyChapterSpriteSet>();CopySettings<FirstDisciplesRoadSettings>();
            var source=ActiveSection.sourceGame;
            game.satanFrames=source?source.satanFrames:baseSatan;
            game.baptismDoveFrames=source?source.baptismDoveFrames:baseDove;
            game.baptismHeavenVoice=source?source.baptismHeavenVoice:baseVoice;
            bool nearby=changed&&lastCameraWorld.x>=ActiveSection.WorldMinX-30&&lastCameraWorld.x<=ActiveSection.WorldMaxX+30;
            cameraBlendUntil=nearby&&index!=3?Time.unscaledTime+.8f:0;if(nearby)blendFrom=lastCameraWorld;
        }
        void CopySettings<T>()where T:MonoBehaviour
        {
            var existing=GetComponent<T>();if(existing)DestroyImmediate(existing);
            var source=ActiveSection.sourceGame?ActiveSection.sourceGame.GetComponent<T>():null;
            if(!source)return;
            var target=gameObject.AddComponent<T>();
            foreach(var field in typeof(T).GetFields(BindingFlags.Public|BindingFlags.Instance|BindingFlags.DeclaredOnly))field.SetValue(target,field.GetValue(source));
        }
        public bool BeginExit()
        {
            if(!ActiveSection||ActiveSection.chapterIndex>=sections.Length-1)return false;
            WalkingToNext=true;
            game.PrepareContinuousExit();
            var next=sections[ActiveSection.chapterIndex+1];
            exitGate=(next.transform.position.x+next.entryLocal.x*next.pixelFrameScale-ActiveSection.transform.position.x)/ActiveSection.pixelFrameScale;
            exitStartX=game.PlayerRoot.position.x;exitStartY=game.PlayerRoot.position.y;
            game.ContinuousHud("오른쪽으로 걸어 다음 길로","A / D · ← / →  다음 장소로 이어지는 길을 걸으세요");
            return true;
        }
        public void TickExit(float dt)
        {
            if(game.Meeting&&!game.Meeting.ClearExit(dt)){game.ContinuousWalk(false,1);return;}
            var k=Keyboard.current;float direction=k==null?0:(k.dKey.isPressed||k.rightArrowKey.isPressed?1:0)-(k.aKey.isPressed||k.leftArrowKey.isPressed?1:0);
            var p=game.PlayerRoot.position;p.x=Mathf.Clamp(p.x+direction*walkingSpeed*dt,ActiveSection.localMinX+1,Mathf.Max(exitGate,exitStartX));
            var next=sections[ActiveSection.chapterIndex+1];float progress=Mathf.InverseLerp(exitStartX,exitGate,p.x);
            p.y=Mathf.Lerp(exitStartY,next.entryLocal.y*next.pixelFrameScale/ActiveSection.pixelFrameScale,Mathf.SmoothStep(0,1,progress));game.PlayerRoot.position=p;
            game.ContinuousWalk(direction!=0,direction);
            if(direction>0&&p.x>=exitGate-.03f){WalkingToNext=false;game.NextChapter();}
        }
        public void ExitCamera()
        {
            var c=game.journeyCamera;var p=c.transform.position;
            float target=game.PlayerRoot.position.x+2;
            p.x=Mathf.Round(Mathf.Lerp(p.x,target,1-Mathf.Exp(-5*Time.unscaledDeltaTime))*32)/32;
            if(ActiveSection.chapterIndex==2)p.y=game.PlayerRoot.position.y+6.5f;
            c.transform.position=p;
        }
    }
}

