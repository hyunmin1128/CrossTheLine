using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CrossTheLine
{
    public sealed partial class DesertTemptation : MonoBehaviour
    {
        public enum Stage { Walk, Fasting, Trial, Transition, Victory, Complete, Travel, Pickup, Collapse, Approach, Ascent }
        public Stage CurrentStage { get; private set; }
        public bool IsCinematic=>CurrentStage==Stage.Fasting||CurrentStage==Stage.Transition||CurrentStage==Stage.Victory||CurrentStage==Stage.Pickup||CurrentStage==Stage.Collapse||CurrentStage==Stage.Approach;
        public bool VictoryFinished { get; private set; }
        public int TrialIndex { get; private set; }
        public int FastingDay { get; private set; }
        JourneyGame game;Transform jesus,satan,staff;SpriteRenderer jesusRenderer,satanRenderer,land;
        Sprite square,landSprite;Texture2D landTexture;Material material;
        GameObject stageObjects,film;TMP_Text caption;
        Transform[] angels=new Transform[3];
        UnityEngine.Rendering.Universal.PixelPerfectCamera pixel;
        bool oldPixel,shutdown,asked;
        float clock;Vector3 cameraTarget;
        readonly Color paper=new Color(.98f,.94f,.81f);
        public void Initialize(JourneyGame owner,Transform player,SpriteRenderer renderer,Material shared,Sprite primitive,int step,bool intro)
        {JourneyObjectFactory.BeginStage(this,step);
            game=owner;jesus=player;jesusRenderer=renderer;material=shared;square=primitive;scrolling=owner.GetComponent<TemptationScrollSettings>();
            pixel=game.journeyCamera.GetComponent<UnityEngine.Rendering.Universal.PixelPerfectCamera>();if(pixel){oldPixel=pixel.enabled;pixel.enabled=false;}
            var enemy=JourneyObjectFactory.Create("사탄");enemy.transform.SetParent(transform,false);satan=enemy.transform;satanRenderer=enemy.GetOrAddComponent<SpriteRenderer>();satanRenderer.sharedMaterial=material;satanRenderer.sortingOrder=58;
            if(game.satanFrames==null||game.satanFrames.Length==0)throw new InvalidOperationException("Satan sprite atlas missing.");
            SetEnemyFrame(0);
            staff=Shape(transform,"WalkingStaff",new Vector3(-6.4f,-2.6f,0),new Vector2(.075f,1.4f),new Color(.39f,.27f,.17f),53).transform;
            BuildFilm();ConfigureStep(step);
            if(!scrolling&&step==0){CurrentStage=Stage.Walk;jesus.position=JourneyStageLayout.Point(this,jesus,"Placement 1",new Vector3(-7,-3,0));satan.gameObject.SetActive(false);game.BaptismHud("걸어서 광야로 들어가기","D / →  ·  지팡이를 짚고 광야의 길을 걸으세요");}
        }
        void SetEnemyFrame(int index)
        {
            var sprite=game.satanFrames[Mathf.Min(index,game.satanFrames.Length-1)];satanRenderer.sprite=sprite;
            if(scrolling){sprite=JourneySpriteAssets.Resolve(sprite);satanRenderer.sprite=sprite;}
            float scale=scrolling?sprite.pixelsPerUnit/32f:1.95f/sprite.bounds.size.y;satan.localScale=Vector3.one*scale;
        }
        public void ConfigureStep(int step)
        {JourneyObjectFactory.BeginStage(this,step);
            if(scrolling){ConfigureScrollStep(step);return;}
            TrialIndex=Mathf.Min(step,2);clock=0;asked=false;film.SetActive(false);jesusRenderer.transform.localRotation=Quaternion.identity;
            BuildMap(TrialIndex);staff.gameObject.SetActive(TrialIndex==0);
            jesus.position=JourneyStageLayout.Point(this,jesus,"Placement 2",new Vector3(TrialIndex==2?-5:-2,TrialIndex==0?-3:1.35f,0));satan.position=JourneyStageLayout.Point(this,satan,"Placement 3",new Vector3(TrialIndex==2?-1.7f:1.7f,jesus.position.y+.92f,0));satanRenderer.color=Color.white;satan.gameObject.SetActive(true);SetEnemyFrame(TrialIndex==0?1:2);
            cameraTarget=new Vector3(0,TrialIndex==0?.6f:2.0f,-10);game.journeyCamera.transform.position=cameraTarget;game.journeyCamera.orthographicSize=5.625f;
            CurrentStage=Stage.Transition;film.SetActive(true);caption.text=TrialIndex==0?"황량한 광야":TrialIndex==1?"예루살렘 · 성전 꼭대기":"높은 산 · 눈앞에 펼쳐진 세상";game.BaptismCinema(true);
        }
        public void Tick(float dt,bool canMove,bool press)
        {
            if(shutdown)return;if(scrolling){TickScroll(dt,canMove,press);return;}clock+=dt;
            staff.position=jesus.position+new Vector3(.46f,.72f,0);
            if(CurrentStage==Stage.Walk&&canMove)
            {
                var k=Keyboard.current;float x=k==null?0:(k.dKey.isPressed||k.rightArrowKey.isPressed?1:0)-(k.aKey.isPressed||k.leftArrowKey.isPressed?1:0);
                var p=jesus.position;p.x=Mathf.Clamp(p.x+x*3.0f*dt,-8,-1.7f);jesus.position=p;game.BaptismWalk(x!=0,x);
                if(p.x>=-1.75f||press){CurrentStage=Stage.Fasting;clock=0;film.SetActive(true);game.BaptismWalk(false,1);game.BaptismCinema(true);}
            }
            else if(CurrentStage==Stage.Fasting)
            {
                FastingDay=Mathf.Clamp(Mathf.FloorToInt(clock/8f*40)+1,1,40);caption.text="40일의 금식 기도\n"+FastingDay+"일째";
                float night=(1-Mathf.Cos(clock*3.8f))*.5f;land.color=Color.Lerp(Color.white,new Color(.33f,.37f,.52f),night);
                jesusRenderer.transform.localRotation=Quaternion.Euler(0,0,-13);staff.gameObject.SetActive(false);
                if(clock>=8){land.color=Color.white;jesusRenderer.transform.localRotation=Quaternion.identity;satan.gameObject.SetActive(true);CurrentStage=Stage.Transition;clock=0;caption.text="광야에 시험하는 자가 나타납니다.";}
            }
            else if(CurrentStage==Stage.Transition)
            {
                if(clock>=2.5f){CurrentStage=Stage.Trial;film.SetActive(false);game.BaptismCinema(false);Interact();}
            }
            else if(CurrentStage==Stage.Trial&&press&&canMove)Interact();
            else if(CurrentStage==Stage.Victory)
            {
                satanRenderer.color=new Color(1,1,1,Mathf.Clamp01(1-clock/2));
                caption.text=clock<2?"사탄아, 물러가라!":"마귀는 떠나고, 천사들이 나아와 섬겼습니다.";
                for(int i=0;i<3;i++){angels[i].gameObject.SetActive(clock>=2);angels[i].position=Vector3.Lerp(new Vector3(7+i*1.2f,1.35f+(i%2)*.5f,0),new Vector3(jesus.position.x+1+i*1.05f,1.35f+(i%2)*.45f,0),Mathf.Clamp01((clock-2)/3));}
                if(clock>=6){VictoryFinished=true;CurrentStage=Stage.Complete;film.SetActive(false);game.BaptismCinema(false);game.BaptismAdvance();}
            }
        }
        public void Interact(){if(CurrentStage!=Stage.Trial||asked)return;asked=true;game.BaptismHud(TrialIndex==0?"첫 번째 시험 · 돌과 떡":TrialIndex==1?"두 번째 시험 · 벼랑 꼭대기":"세 번째 시험 · 높은 산","대화를 읽고 말씀으로 응답하세요");game.TemptationAsk();}
        public void StartVictory()
        {
            if(CurrentStage==Stage.Victory||VictoryFinished)return;
            CurrentStage=Stage.Victory;clock=0;film.SetActive(true);game.BaptismCinema(true);SetEnemyFrame(3);
            var angelSprites=JourneySpriteAssets.LoadAll("CrossTheLine/RedrawAngels");
            for(int i=0;i<3;i++)
            {
                var go=JourneyObjectFactory.Create("섬기는 천사 "+i);go.transform.SetParent(transform,false);angels[i]=go.transform;var renderer=go.GetOrAddComponent<SpriteRenderer>();renderer.sharedMaterial=material;renderer.sprite=angelSprites.Length==3?angelSprites[i]:game.supportingCast[8];renderer.sortingOrder=63+i;renderer.flipX=true;
                float scale=scrolling?renderer.sprite.pixelsPerUnit/32f:1.7f/renderer.sprite.bounds.size.y;go.transform.localScale=Vector3.one*scale;go.SetActive(false);
            }
            for(int i=0;i<18;i++)Shape(stageObjects.transform,"DisappearingLight_"+i,satan.position+new Vector3(Mathf.Sin(i*2.4f)*.65f,Mathf.Cos(i*1.7f)*.75f,0),new Vector2(.04f,.04f),new Color(.96f,.83f,.55f,.75f),75);
        }
        public void UpdateCamera(float dt){if(shutdown)return;if(scrolling){UpdateScrollCamera(dt);return;}game.journeyCamera.transform.position=Vector3.Lerp(game.journeyCamera.transform.position,cameraTarget,1-Mathf.Exp(-3*dt));}
        GameObject Shape(Transform parent,string name,Vector3 pos,Vector2 size,Color color,int order)
        {
            var go=JourneyObjectFactory.Create(name);go.transform.SetParent(parent,false);go.transform.position=pos;go.transform.localScale=new Vector3(size.x,size.y,1);var r=go.GetOrAddComponent<SpriteRenderer>();r.sprite=square;r.sharedMaterial=material;r.color=color;r.sortingOrder=order;return go;
        }
        void BuildMap(int trial)
        {
            if(scrolling){BuildScrollMap();return;}
            if(stageObjects)Destroy(stageObjects);if(landSprite)Destroy(landSprite);if(landTexture)Destroy(landTexture);
            stageObjects=JourneyObjectFactory.Create("TrialEnvironment_"+trial);stageObjects.transform.SetParent(transform,false);
            const int w=1280,h=640;var colors=new Color32[w*h];
            Color32 C(string hex){ColorUtility.TryParseHtmlString("#"+hex,out var c);return c;}
            void Box(int x,int y,int width,int height,Color32 c){for(int yy=Mathf.Max(0,y);yy<Mathf.Min(h,y+height);yy++)for(int xx=Mathf.Max(0,x);xx<Mathf.Min(w,x+width);xx++)colors[yy*w+xx]=c;}
            Box(0,0,w,h,C(trial==0?"ecd3ad":trial==1?"c8d7ce":"dabfae"));
            Box(120,500,28,28,C("f2dfb0"));
            for(int x=0;x<w;x++)
            {
                int hill=275+(int)(Mathf.Sin(x*.008f)*35+Mathf.Sin(x*.003f)*18);
                Box(x,0,1,hill,C(trial==0?"bca17e":trial==1?"a7afa0":"859c9d"));
                int nearer=218+(int)(Mathf.Sin(x*.009f+1)*21);Box(x,0,1,nearer,C(trial==0?"d4b88c":"6d8284"));
                if(trial==0)Box(x,0,1,200+(int)(Mathf.Sin(x*.004f)*15),C("dbc395"));
            }
            var random=new System.Random(310+trial);
            if(trial==0)
            {
                for(int i=0;i<1700;i++)Box(random.Next(w),random.Next(0,218),2,1,C(i%2==0?"cbb58b":"e2cea3"));
                Box(630,213,11,7,C("7f7367"));Box(648,212,8,6,C("9b8c75"));Box(660,214,9,5,C("887d6b"));
            }
            else
            {
                for(int i=0;i<36;i++){int x=i*37+random.Next(9),y=125+random.Next(50);Box(x,y,25,random.Next(14,38),C("9ca89c"));Box(x-2,y+23,29,3,C("bdbaa5"));}
                if(trial==1)
                {
                    Box(360,115,560,249,C("c5b698"));Box(345,357,590,17,C("eed6b1"));Box(345,350,590,7,C("a18e72"));
                    for(int x=386;x<900;x+=85){Box(x,128,20,221,C("f0dabe"));Box(x+20,128,7,221,C("a7977c"));Box(x-5,343,36,10,C("ead4b2"));}
                    Box(598,116,85,140,C("5c5d54"));Box(612,244,59,17,C("5c5d54"));
                    for(int i=0;i<4;i++)Box(345-i*6,112-i*5,590+i*12,5,C("baa78b"));
                }
                else
                {
                    for(int i=0;i<22;i++){int x=690+i*24,y=228+random.Next(42);Box(x,y,19,random.Next(17,35),C("aaad9a"));Box(x-2,y+28,23,3,C("d2c9ab"));Box(x+5,y+9,3,5,C("6b7e78"));}
                    for(int x=230;x<730;x++){int top=373-(int)(Mathf.Abs(x-490)*.62f);if(x>=425&&x<=600)top=373;Box(x,0,1,top,C("777e76"));Box(x,top-7,1,7,C("b0ab91"));}
                    for(int i=0;i<5;i++)Box(i*290+20,411+i%2*60,150,6,C("e2d8c3"));
                }
            }
            landTexture=new Texture2D(w,h,TextureFormat.RGBA32,false){filterMode=FilterMode.Point};landTexture.SetPixels32(colors);landTexture.Apply();landSprite=Sprite.Create(landTexture,new Rect(0,0,w,h),new Vector2(.5f,.5f),32);
            var ground=JourneyObjectFactory.Create("SketchLandscape");ground.transform.SetParent(stageObjects.transform,false);land=ground.GetOrAddComponent<SpriteRenderer>();land.sprite=landSprite;land.sharedMaterial=material;land.sortingOrder=-100;
        }
        void BuildFilm()
        {
            var canvasObject=JourneyObjectFactory.Create("TemptationFilmCanvas",typeof(RectTransform),typeof(Canvas),typeof(UnityEngine.UI.CanvasScaler));canvasObject.transform.SetParent(transform,false);var canvas=canvasObject.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=106;
            var scaler=canvasObject.GetComponent<UnityEngine.UI.CanvasScaler>();scaler.uiScaleMode=UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1280,720);
            film=JourneyObjectFactory.Create("CaptionBand",typeof(RectTransform),typeof(UnityEngine.UI.Image));film.transform.SetParent(canvasObject.transform,false);var rect=film.GetComponent<RectTransform>();rect.anchorMin=Vector2.zero;rect.anchorMax=new Vector2(1,.15f);rect.offsetMin=rect.offsetMax=Vector2.zero;
            var image=film.GetComponent<UnityEngine.UI.Image>();image.color=new Color(.08f,.11f,.13f,.93f);image.raycastTarget=false;
            var textObject=JourneyObjectFactory.Create("Caption",typeof(RectTransform));textObject.transform.SetParent(film.transform,false);var tr=textObject.GetComponent<RectTransform>();tr.anchorMin=new Vector2(.06f,.08f);tr.anchorMax=new Vector2(.94f,.92f);tr.offsetMin=tr.offsetMax=Vector2.zero;caption=textObject.GetOrAddComponent<TextMeshProUGUI>();caption.font=game.koreanFont;caption.fontSize=27;caption.color=paper;caption.alignment=TextAlignmentOptions.Center;caption.raycastTarget=false;
        }
        public void Shutdown(){if(shutdown)return;shutdown=true;if(pixel)pixel.enabled=oldPixel;if(game&&game.journeyCamera){game.journeyCamera.orthographicSize=5.625f;if(scrolling){game.journeyCamera.rect=oldViewport;game.journeyCamera.aspect=oldAspect;}}}
        void OnDestroy(){Shutdown();if(landSprite)Destroy(landSprite);if(landTexture)Destroy(landTexture);}
    }
}
