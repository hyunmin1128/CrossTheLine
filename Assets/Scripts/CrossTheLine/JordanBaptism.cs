using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CrossTheLine
{
    /// <summary>Chapter two's sketch-based river staging, ambient baptisms and cinematic.</summary>
    public sealed partial class JordanBaptism : MonoBehaviour
    {
        public enum Stage { Establishing, Approach, Conversation, EnterWater, Baptizing, LeaveWater, Revelation, Complete }
        public Stage CurrentStage { get; private set; }
        public bool IsCinematic => CurrentStage==Stage.Establishing||CurrentStage==Stage.Baptizing||CurrentStage==Stage.Revelation;
        public bool NarrationActive => voice&&voice.isPlaying;
        public bool VoiceWasPlayed { get; private set; }
        public float SequenceTime => phaseTime;
        public Transform John => john;
        public Transform Jesus => jesus;
        public Transform Dove => dove;
        public Vector3 WaterPosition => new Vector3(-.25f,-.8f,0);
        public Vector3 ExitPosition => new Vector3(-.25f,-2.45f,0);

        JourneyGame game;
        Transform jesus,john,visitor,dove,arm;
        SpriteRenderer jesusRenderer,johnRenderer,visitorRenderer,doveRenderer,jesusWater,visitorWater;
        Material material;
        Sprite square;
        Texture2D backdropTexture;
        Sprite backdropSprite;
        AudioSource river,voice;
        AudioClip riverClip;
        GameObject filmCanvas,letterbox,lightRays;
        TMP_Text subtitle,title;
        UnityEngine.Rendering.Universal.PixelPerfectCamera pixelCamera;
        bool previousPixelEnabled,paused,shutdown,talkStarted;
        float phaseTime,ambientTime;
        Vector3 cameraTarget;
        float cameraSize=5.625f;
        JordanScrollSettings scrolling;
        Rect previousViewport;
        float previousAspect;
        const string HeavenlyLine="이는 내 사랑하는 아들이요, 내 기뻐하는 자라.";
        readonly Color waterColor=new Color(.29f,.55f,.57f), parchment=new Color(.98f,.94f,.80f);

        public void Initialize(JourneyGame owner,Transform player,SpriteRenderer renderer,Material shared,Sprite primitive,int step,bool intro)
        {JourneyObjectFactory.BeginStage(this,step);
            game=owner;jesus=player;jesusRenderer=renderer;material=shared;square=primitive;
            poses=owner.GetComponent<JourneyChapterSpriteSet>();
            scrolling=owner.GetComponent<JordanScrollSettings>();previousViewport=game.journeyCamera.rect;previousAspect=game.journeyCamera.aspect;
            if(scrolling){var profile=jesus.GetComponent<JourneyCharacterView>();if(profile&&profile.idle)profile.displayHeight=profile.idle.rect.height/JordanScrollSettings.PixelsPerUnit;}
            pixelCamera=game.journeyCamera.GetComponent<UnityEngine.Rendering.Universal.PixelPerfectCamera>();
            if(pixelCamera){previousPixelEnabled=pixelCamera.enabled;pixelCamera.enabled=false;}
            BuildRiver();BuildCast();BuildFilmUI();BuildSound();
            jesusWater=WaterCover(jesus,62);visitorWater=WaterCover(visitor,57);
            var doveObject=JourneyObjectFactory.Create("DescendingDove");doveObject.transform.SetParent(transform);dove=doveObject.transform;
            doveRenderer=doveObject.GetOrAddComponent<SpriteRenderer>();doveRenderer.sharedMaterial=material;doveRenderer.sortingOrder=110;
            dove.gameObject.SetActive(false);
            lightRays=JourneyObjectFactory.Create("OpenedHeavensLight");lightRays.transform.SetParent(transform);lightRays.SetActive(false);
            cameraTarget=new Vector3(0,.65f,-10);game.journeyCamera.transform.position=cameraTarget;game.journeyCamera.orthographicSize=5.625f;
            ConfigureStep(step);
            if(scrolling)UpdateCamera(0);
            if(step==0&&intro)
            {
                SetStage(Stage.Establishing);cameraSize=scrolling?JordanScrollSettings.CameraSize:8.4375f;game.journeyCamera.orthographicSize=cameraSize;
                letterbox.SetActive(true);title.text="요단강의 세례";subtitle.text="강가에서 요한이 사람들에게 세례를 주고 있습니다.";
                game.BaptismCinema(true);
            }
        }
        void SetStage(Stage stage){CurrentStage=stage;phaseTime=0;}
        public void ConfigureStep(int step)
        {JourneyObjectFactory.BeginStage(this,step);
            talkStarted=false;jesusRenderer.transform.localRotation=Quaternion.identity;
            letterbox.SetActive(false);lightRays.SetActive(false);dove.gameObject.SetActive(false);arm.gameObject.SetActive(false);
            if(step==0){SetStage(Stage.Approach);jesus.position=JourneyStageLayout.Point(this,jesus,"Placement 1",new Vector3(scrolling?scrolling.startX:-7,-2.45f,0));game.BaptismHud("세례 중인 요한에게 다가가기","WASD / 방향키  ·  요한이 있는 강가로 이동하세요");}
            else if(step==1){SetStage(Stage.EnterWater);jesus.position=JourneyStageLayout.Point(this,jesus,"Placement 2",new Vector3(-.25f,-2.25f,0));game.BaptismHud("강물에 들어가 세례 받기","W / ↑  ·  요한 앞으로 천천히 들어가세요");}
            else if(step==2){SetStage(Stage.LeaveWater);jesus.position=WaterPosition;game.BaptismHud("세례를 받고 강 밖으로 나가기","S / ↓  ·  강가로 걸어 나오세요");}
            else{SetStage(Stage.Complete);jesus.position=ExitPosition;}
            game.BaptismWalk(false,1);game.BaptismCinema(false);UpdateWater();
            ResetJohnPose();
        }
        public void Tick(float dt,bool allowMovement,bool press,bool muted)
        {
            if(shutdown||paused)return;
            phaseTime+=dt;ambientTime+=dt;
            river.volume=muted?0:.10f;voice.volume=muted?0:.9f;
            if(CurrentStage==Stage.Establishing||CurrentStage==Stage.Approach)AnimateVisitor();
            else visitorWater.gameObject.SetActive(false);
            if(CurrentStage==Stage.Establishing)
            {
                cameraSize=scrolling?JordanScrollSettings.CameraSize:Mathf.Lerp(8.4375f,5.625f,Mathf.SmoothStep(0,1,phaseTime/3.2f));
                if(phaseTime>=3.2f){SetStage(Stage.Approach);letterbox.SetActive(false);game.BaptismCinema(false);}
                return;
            }
            if(CurrentStage==Stage.Baptizing){AnimateBaptism();return;}
            if(CurrentStage==Stage.Revelation){AnimateRevelation();return;}
            if(!allowMovement||CurrentStage==Stage.Conversation||CurrentStage==Stage.Complete)return;
            var keyboard=Keyboard.current;Vector2 move=Vector2.zero;
            if(keyboard!=null)
            {
                move.x=(keyboard.dKey.isPressed||keyboard.rightArrowKey.isPressed?1:0)-(keyboard.aKey.isPressed||keyboard.leftArrowKey.isPressed?1:0);
                move.y=(keyboard.wKey.isPressed||keyboard.upArrowKey.isPressed?1:0)-(keyboard.sKey.isPressed||keyboard.downArrowKey.isPressed?1:0);
            }
            var position=jesus.position+(Vector3)(move.normalized*3.25f*dt);
            float edge=scrolling?scrolling.worldWidth*.5f-1:8.5f;
            position.x=Mathf.Clamp(position.x,-edge,edge);
            position.y=Mathf.Clamp(position.y,-3.2f,CurrentStage==Stage.Approach?-2.05f:-.5f);
            jesus.position=JourneyStageLayout.Point(this,jesus,"Placement 3",new Vector3(position.x,position.y,0));
            game.BaptismWalk(move.sqrMagnitude>0,move.x);UpdateWater();
            if(CurrentStage==Stage.Approach&&Mathf.Abs(jesus.position.x+.25f)<1.6f)Interact();
            else if(CurrentStage==Stage.EnterWater&&Mathf.Abs(jesus.position.x+.25f)<.85f&&jesus.position.y>-1.35f)StartBaptism();
            else if(CurrentStage==Stage.LeaveWater&&jesus.position.y<-2.2f)StartRevelation();
            else if(press)Interact();
        }
        public void Interact()
        {
            if(CurrentStage==Stage.Approach&&!talkStarted&&Mathf.Abs(jesus.position.x+.25f)<1.8f)
            {
                talkStarted=true;SetStage(Stage.Conversation);visitor.gameObject.SetActive(false);arm.gameObject.SetActive(false);
                game.BaptismWalk(false,1);
                ResetJohnPose();
                game.BaptismSay(new[]{
                    "예수님|요한아, 나에게도 세례를 베풀어 주렴.",
                    "세례 요한|제가 당신께 세례를 받아야 할 터인데, 당신께서 제게 오십니까?",
                    "예수님|지금은 허락하라. 우리가 이와 같이 하여 모든 의를 이루는 것이 옳도다.",
                    "세례 요한|그 말씀대로 하겠습니다. 이쪽으로 들어오십시오."
                },()=>game.BaptismAdvance());
            }
            else if(CurrentStage==Stage.EnterWater&&Vector3.Distance(jesus.position,WaterPosition)<1.2f)StartBaptism();
        }
        void StartBaptism()
        {
            if(CurrentStage!=Stage.EnterWater)return;
            SetStage(Stage.Baptizing);jesus.position=WaterPosition;game.BaptismWalk(false,1);
            cameraTarget=new Vector3(.1f,.3f,-10);cameraSize=4.5f;
            arm.gameObject.SetActive(!UsesAuthoredPoses);letterbox.SetActive(true);title.text="";subtitle.text="요한이 예수님께 세례를 베풉니다.";
            game.BaptismCinema(true);visitor.gameObject.SetActive(false);UpdateWater();
        }
        void AnimateBaptism()
        {
            if(UsesAuthoredPoses){AnimateAuthoredBaptism();return;}
            float t=phaseTime;
            float kneel=t<2?Mathf.SmoothStep(0,1,t/2):t<4.8f?1:1-Mathf.SmoothStep(0,1,(t-4.8f)/2);
            jesus.position=WaterPosition+Vector3.down*(kneel*.65f);
            jesusRenderer.transform.localRotation=Quaternion.Euler(0,0,-kneel*18);
            johnRenderer.transform.localRotation=Quaternion.Euler(0,0,kneel*5);
            PointArm(jesus.position+new Vector3(.23f,1.48f,0));
            subtitle.text=t<2?"예수님이 강물 속에서 몸을 낮추십니다.":t<4.8f?"요한이 예수님께 세례를 베풉니다.":"세례를 받으신 예수님이 물에서 일어나십니다.";
            UpdateWater();
            if(t>=7.2f){johnRenderer.transform.localRotation=Quaternion.identity;game.BaptismAdvance();}
        }
        void StartRevelation()
        {
            if(CurrentStage!=Stage.LeaveWater)return;
            SetStage(Stage.Revelation);game.BaptismWalk(false,0);jesusRenderer.transform.localRotation=Quaternion.identity;
            cameraTarget=new Vector3(jesus.position.x+.35f,1.3f,-10);cameraSize=5.625f;
            letterbox.SetActive(true);title.text="하늘이 열리고";subtitle.text="";game.BaptismCinema(true);
            dove.gameObject.SetActive(true);dove.position=JourneyStageLayout.Point(this,dove,"Placement 4",new Vector3(jesus.position.x+2.5f,7.4f,0));
            lightRays.SetActive(true);
            for(int i=0;i<9;i++)
            {
                var a=new Vector3(jesus.position.x+4.3f+i*.13f,8.5f,0);
                var b=jesus.position+new Vector3((i-4)*.16f,.3f,0);
                var ray=Shape(lightRays.transform,"Ray_"+i,(a+b)*.5f,new Vector2(.14f+(i%3)*.07f,Vector3.Distance(a,b)),new Color(1,.94f,.67f,.055f),-2);
                ray.transform.rotation=Quaternion.Euler(0,0,Mathf.Atan2((b-a).y,(b-a).x)*Mathf.Rad2Deg-90);
            }
            UpdateWater();
        }
        void AnimateRevelation()
        {
            float t=phaseTime;
            var target=jesus.position+new Vector3(0,2.2f,0);
            float u=Mathf.SmoothStep(0,1,t/4.4f);
            dove.position=Vector3.Lerp(new Vector3(jesus.position.x+2.5f,7.4f,0),target,u)+new Vector3(Mathf.Sin(t*2)*.06f,Mathf.Sin(t*2.8f)*.06f,0);
            if(game.baptismDoveFrames!=null&&game.baptismDoveFrames.Length>0)
            {
                var frame=game.baptismDoveFrames[(int)(t*6)%game.baptismDoveFrames.Length];doveRenderer.sprite=frame;dove.localScale=Vector3.one*(1.10f/game.baptismDoveFrames[1].bounds.size.x);
            }
            if(t>=3.4f&&!VoiceWasPlayed)
            {
                VoiceWasPlayed=true;title.text="";subtitle.text="이는 내 사랑하는 아들이요,\n내 기뻐하는 자라.";
                if(game.baptismHeavenVoice)voice.PlayOneShot(game.baptismHeavenVoice);else Debug.LogError("Baptism narration asset missing.");
            }
            if(VoiceWasPlayed&&t>=3.4f+(game.baptismHeavenVoice?game.baptismHeavenVoice.length:7)+1.8f&&!voice.isPlaying)
            {
                SetStage(Stage.Complete);letterbox.SetActive(false);game.BaptismCinema(false);game.BaptismAdvance();
                ResetJohnPose();
            }
        }
        public void UpdateCamera(float dt)
        {
            if(shutdown)return;
            var camera=game.journeyCamera;
            if(scrolling)
            {
                const float aspect=16f/9f;
                float outputAspect=Screen.width/(float)Mathf.Max(1,Screen.height);
                float width=Mathf.Min(1,aspect/outputAspect),height=Mathf.Min(1,outputAspect/aspect);
                camera.rect=new Rect((1-width)*.5f,(1-height)*.5f,width,height);camera.aspect=aspect;
                camera.orthographicSize=JordanScrollSettings.CameraSize;
                float half=JordanScrollSettings.CameraSize*aspect;
                float target=Mathf.Clamp(jesus.position.x+scrolling.horizontalLead,-scrolling.worldWidth*.5f+half,scrolling.worldWidth*.5f-half);
                float x=dt<=0?target:Mathf.Lerp(camera.transform.position.x,target,1-Mathf.Exp(-scrolling.followSharpness*dt));
                camera.transform.position=new Vector3(Mathf.Round(x*32)/32f,scrolling.cameraY,-10);
                return;
            }
            camera.transform.position=Vector3.Lerp(camera.transform.position,cameraTarget,1-Mathf.Exp(-3*dt));
            camera.orthographicSize=Mathf.Lerp(camera.orthographicSize,cameraSize,1-Mathf.Exp(-3*dt));
        }
        void AnimateVisitor()
        {
            float t=ambientTime%11;
            visitor.gameObject.SetActive(true);
            var entry=new Vector3(2.7f,scrolling ? 1.8f : 2.35f,0);var point=WaterPosition;
            visitor.position=t<2.8f?Vector3.Lerp(entry,point,t/2.8f):t<7.3f?point:Vector3.Lerp(point,entry,(t-7.3f)/3.7f);
            float bow=t>=3.5f&&t<6.6f?Mathf.Sin((t-3.5f)/3.1f*Mathf.PI):0;
            visitor.position+=Vector3.down*(bow*.55f);visitorRenderer.transform.localRotation=Quaternion.Euler(0,0,-bow*16);
            arm.gameObject.SetActive(t>=3.5f&&t<6.6f);
            PointArm(visitor.position+new Vector3(.18f,1.45f,0));
            if(UsesAuthoredPoses)AnimateAuthoredVisitor(t);
            visitorWater.gameObject.SetActive(visitor.position.y<.8f);visitorWater.transform.localScale=new Vector3(.65f,.62f,1);
        }
        void UpdateWater()
        {
            bool inside=jesus.position.y>-2.0f;jesusWater.gameObject.SetActive(inside);
            jesusWater.color=waterColor;
            jesusWater.transform.localPosition=new Vector3(0,.32f,0);
            jesusWater.transform.localScale=new Vector3(.83f,Mathf.Clamp((jesus.position.y+2.0f)*.72f,.1f,.95f),1);
        }
        SpriteRenderer WaterCover(Transform actor,int order)
        {
            var go=Shape(actor,"WaterOcclusion",Vector3.zero,new Vector2(.72f,.68f),waterColor,order);go.transform.localPosition=new Vector3(0,.28f,0);
            var renderer=go.GetComponent<SpriteRenderer>();
            if(UsesAuthoredPoses)
            {
                renderer.enabled=false;
                var body=actor==jesus?jesusRenderer:actor==john?johnRenderer:visitorRenderer;
                var mask=go.GetOrAddComponent<SpriteMask>();mask.sprite=square;mask.isCustomRangeActive=true;
                mask.frontSortingLayerID=mask.backSortingLayerID=body.sortingLayerID;
                mask.frontSortingOrder=body.sortingOrder;mask.backSortingOrder=body.sortingOrder-1;
                body.maskInteraction=SpriteMaskInteraction.VisibleOutsideMask;
            }
            return renderer;
        }
        void PointArm(Vector3 target)
        {
            var origin=john.position+new Vector3(-.17f,1.15f,0);var delta=target-origin;
            arm.position=(target+origin)*.5f;arm.localScale=new Vector3(Mathf.Max(.1f,delta.magnitude),.105f,1);
            arm.rotation=Quaternion.Euler(0,0,Mathf.Atan2(delta.y,delta.x)*Mathf.Rad2Deg);
        }
        GameObject Shape(Transform parent,string name,Vector3 position,Vector2 size,Color color,int order)
        {
            var go=JourneyObjectFactory.Create(name);go.transform.SetParent(parent,false);go.transform.position=position;go.transform.localScale=new Vector3(size.x,size.y,1);
            var renderer=go.GetOrAddComponent<SpriteRenderer>();renderer.sprite=square;renderer.sharedMaterial=material;renderer.color=color;renderer.sortingOrder=order;return go;
        }
        Transform Person(string name,Sprite sprite,Vector3 feet,bool faceLeft,int order,out SpriteRenderer renderer)
        {
            var root=JourneyObjectFactory.Create(name);root.transform.SetParent(transform,false);root.transform.position=feet;
            var body=JourneyObjectFactory.Create("Body");body.transform.SetParent(root.transform,false);renderer=body.GetOrAddComponent<SpriteRenderer>();renderer.sprite=sprite;renderer.sharedMaterial=material;renderer.sortingOrder=order;renderer.flipX=faceLeft;
            float h=game.supportingCast.Length>4&&(sprite==game.supportingCast[3]||sprite==game.supportingCast[4])?1.12f:1.85f;
            float scale=h/sprite.bounds.size.y;body.transform.localScale=Vector3.one*scale;var visible=scrolling?JourneySpriteAssets.VisibleBounds(sprite):sprite.bounds;body.transform.localPosition=new Vector3(-visible.center.x*scale,-visible.min.y*scale,0);
            return root.transform;
        }
        void BuildCast()
        {
            john=Person("세례 요한",game.supportingCast[1],new Vector3(1.15f,-.8f,0),true,55,out johnRenderer);
            johnRestPosition=john.position;
            if(UsesAuthoredPoses)JohnPose(poses.johnWaiting);
            visitor=Person("세례를 받는 사람",game.supportingCast[6],new Vector3(2.7f,scrolling ? 1.8f : 2.35f,0),false,54,out visitorRenderer);
            var johnWater=WaterCover(john,58);johnWater.transform.localScale=new Vector3(.80f,.66f,1);
            int[] north={6,5,0,3,6};int[] south={5,0,6,4,5,6};
            for(int i=0;i<north.Length;i++)Person("맞은편 강가 사람 "+i,game.supportingCast[north[i]],new Vector3(-7+i*3.2f+(i==3?1.35f:0),scrolling ? 1.8f+(i%2)*.07f : 2.05f+(i%2)*.25f,0),i%2==0,18+i,out _);
            for(int i=0;i<south.Length;i++)Person("강가에서 기다리는 사람 "+i,game.supportingCast[south[i]],new Vector3(-8+i*3.2f,-3.65f-(i%2)*.25f,0),i%2==1,82+i,out _);
            arm=Shape(transform,"John_BaptismHand",Vector3.zero,new Vector2(.6f,.105f),new Color(.80f,.55f,.35f),59).transform;arm.gameObject.SetActive(false);
            for(int i=0;i<12;i++)Shape(transform,"WaterRipple_"+i,new Vector3(-1.2f+(i%4)*.55f,-1.05f+(i/4)*.11f,0),new Vector2(.28f,.024f),new Color(.72f,.85f,.79f,.48f),66);
        }
        void BuildFilmUI()
        {
            filmCanvas=JourneyObjectFactory.Create("BaptismFilmCanvas",typeof(RectTransform),typeof(Canvas),typeof(UnityEngine.UI.CanvasScaler));filmCanvas.transform.SetParent(transform,false);
            var canvas=filmCanvas.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=105;
            var scaler=filmCanvas.GetComponent<UnityEngine.UI.CanvasScaler>();scaler.uiScaleMode=UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1280,720);scaler.matchWidthOrHeight=.5f;
            letterbox=JourneyObjectFactory.Create("CinematicFrame",typeof(RectTransform));letterbox.transform.SetParent(filmCanvas.transform,false);var r=letterbox.GetComponent<RectTransform>();r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;
            FilmPanel(letterbox.transform,"TopBar",new Vector2(0,.93f),Vector2.one,new Color(.04f,.10f,.13f,.95f));
            var bottom=FilmPanel(letterbox.transform,"BottomBar",Vector2.zero,new Vector2(1,.145f),new Color(.04f,.10f,.13f,.95f));
            subtitle=FilmText(bottom.transform,"Subtitle",new Vector2(.055f,.08f),new Vector2(.945f,.94f),25);
            title=FilmText(letterbox.transform,"ChapterTitle",new Vector2(.12f,.77f),new Vector2(.88f,.91f),37);
            letterbox.SetActive(false);
        }
        GameObject FilmPanel(Transform parent,string name,Vector2 min,Vector2 max,Color color)
        {
            var go=JourneyObjectFactory.Create(name,typeof(RectTransform),typeof(UnityEngine.UI.Image));go.transform.SetParent(parent,false);var r=go.GetComponent<RectTransform>();r.anchorMin=min;r.anchorMax=max;r.offsetMin=r.offsetMax=Vector2.zero;var image=go.GetComponent<UnityEngine.UI.Image>();image.color=color;image.raycastTarget=false;return go;
        }
        TMP_Text FilmText(Transform parent,string name,Vector2 min,Vector2 max,int size)
        {
            var go=JourneyObjectFactory.Create(name,typeof(RectTransform));go.transform.SetParent(parent,false);var r=go.GetComponent<RectTransform>();r.anchorMin=min;r.anchorMax=max;r.offsetMin=r.offsetMax=Vector2.zero;
            var text=go.GetOrAddComponent<TextMeshProUGUI>();text.font=game.koreanFont;text.fontSize=size;text.color=parchment;text.alignment=TextAlignmentOptions.Center;text.raycastTarget=false;text.textWrappingMode=TextWrappingModes.Normal;return text;
        }
        void BuildSound()
        {
            river=gameObject.AddComponent<AudioSource>();river.loop=true;river.playOnAwake=false;river.spatialBlend=0;
            voice=gameObject.AddComponent<AudioSource>();voice.playOnAwake=false;voice.spatialBlend=0;
            var reverb=gameObject.GetOrAddComponent<AudioReverbFilter>();reverb.reverbPreset=AudioReverbPreset.User;reverb.decayTime=2.1f;reverb.reverbLevel=-1600;reverb.reflectionsLevel=-2800;reverb.dryLevel=0;
            int rate=16000;var samples=new float[rate*4];var random=new System.Random(217);float filtered=0;
            for(int i=0;i<samples.Length;i++){filtered=filtered*.92f+((float)random.NextDouble()*2-1)*.08f;samples[i]=filtered*(.6f+.15f*Mathf.Sin(i/(float)rate*2));}
            riverClip=AudioClip.Create("JordanRiverFlow",samples.Length,1,rate,false);riverClip.SetData(samples,0);river.clip=riverClip;river.volume=.10f;river.Play();
        }
        void BuildRiver()
        {
            if(scrolling&&scrolling.backgroundPrefab)
            {
                var panorama=Instantiate(scrolling.backgroundPrefab,transform,false);panorama.name="JordanHorizontalLandscape";
                panorama.transform.localPosition=new Vector3(0,scrolling.cameraY,4);
                foreach(var image in panorama.GetComponentsInChildren<SpriteRenderer>(true))image.sharedMaterial=material;
                return;
            }
            const int w=1536,h=640;var pixels=new Color32[w*h];
            Color32 C(string hex){ColorUtility.TryParseHtmlString("#"+hex,out var c);return c;}
            void Box(int x,int y,int width,int height,Color32 c){for(int yy=Mathf.Max(0,y);yy<Mathf.Min(h,y+height);yy++)for(int xx=Mathf.Max(0,x);xx<Mathf.Min(w,x+width);xx++)pixels[yy*w+xx]=c;}
            Box(0,0,w,h,C("c6d9ca"));
            for(int x=0;x<w;x++)
            {
                float worldX=(x-w/2)/32f;float center=-worldX*.04f+Mathf.Sin(worldX*.3f)*.20f;
                int lower=(int)((center-1.72f)*32+h/2),upper=(int)((center+1.05f)*32+h/2);
                Box(x,0,1,lower-7,C("c7b68b"));Box(x,lower-7,1,7,C("b5ab78"));Box(x,lower,1,upper-lower,C("4a8c92"));Box(x,upper,1,9,C("a7b484"));Box(x,upper+9,1,115,C("a7b789"));
                int mountain=upper+91+Mathf.RoundToInt((Mathf.Sin(worldX*.45f)+Mathf.Sin(worldX*.13f))*15);
                Box(x,upper+65,1,mountain-(upper+65),C("879f83"));
                Box(x,mountain,1,21,C("a5b69c"));
            }
            var random=new System.Random(221);
            for(int i=0;i<2600;i++)
            {
                int x=random.Next(w),y=random.Next(0,270);Box(x,y,2,1,C(i%3==0?"b7a67f":"d7c69b"));
            }
            for(int i=0;i<450;i++)
            {
                int x=random.Next(w);float xx=(x-w/2)/32f;float center=-xx*.04f+Mathf.Sin(xx*.3f)*.2f;
                int low=(int)((center-1.6f)*32+h/2),high=(int)((center+.92f)*32+h/2);
                Box(x,random.Next(low,Math.Max(low+1,high)),random.Next(3,18),1,C(i%3==0?"93b8ab":"6ca4a2"));
            }
            for(int i=0;i<150;i++)
            {
                int x=random.Next(w);float xx=(x-w/2)/32f;int y=(int)((-xx*.04f+Mathf.Sin(xx*.3f)*.2f+1.45f)*32+h/2);
                Box(x,y,2,random.Next(5,13),C("63885d"));Box(x+3,y,1,7,C("799464"));
            }
            for(int i=0;i<18;i++)
            {
                int x=40+i*86,y=410+random.Next(0,26);Box(x,y,4,49,C("826a49"));
                for(int f=-3;f<=3;f++){Box(x+f*7,y+48-Mathf.Abs(f)*3,15,4,C("5e7d58"));Box(x+f*7+4,y+43-Mathf.Abs(f)*3,12,4,C("5e7d58"));}
            }
            backdropTexture=new Texture2D(w,h,TextureFormat.RGBA32,false){filterMode=FilterMode.Point};backdropTexture.SetPixels32(pixels);backdropTexture.Apply();
            backdropSprite=Sprite.Create(backdropTexture,new Rect(0,0,w,h),new Vector2(.5f,.5f),32);
            var go=JourneyObjectFactory.Create("SketchBasedJordanRiver");go.transform.SetParent(transform,false);var renderer=go.GetOrAddComponent<SpriteRenderer>();renderer.sprite=backdropSprite;renderer.sharedMaterial=material;renderer.sortingOrder=-100;
        }
        public void SetPaused(bool value)
        {
            paused=value;
            if(value){river?.Pause();voice?.Pause();}
            else{river?.UnPause();voice?.UnPause();}
        }
        public void CancelCinematic(){voice?.Stop();if(letterbox)letterbox.SetActive(false);}
        public void Shutdown()
        {
            if(shutdown)return;shutdown=true;voice?.Stop();river?.Stop();
            if(pixelCamera)pixelCamera.enabled=previousPixelEnabled;
            if(game&&game.journeyCamera)game.journeyCamera.orthographicSize=5.625f;
            if(scrolling&&game&&game.journeyCamera){game.journeyCamera.rect=previousViewport;game.journeyCamera.aspect=previousAspect;}
        }
        void OnDestroy(){Shutdown();if(backdropSprite)Destroy(backdropSprite);if(backdropTexture)Destroy(backdropTexture);if(riverClip)Destroy(riverClip);}
    }
}
