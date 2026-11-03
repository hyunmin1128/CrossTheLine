using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CrossTheLine
{
    public sealed class JerusalemEntry : MonoBehaviour
    {
        public enum Phase { Commission, Village, Returning, LeavingVillage, Handover, CloaksReady, Clothing, MountReady, Mounting, Procession, CrossingGate, EnteringTown, PraiseReady, Reception, WeepReady, Weeping, Closing }
        public Phase CurrentPhase {get;private set;}
        public int Stage {get;private set;}
        public bool DonkeyReleased {get;private set;}
        public int CloaksPlaced {get;private set;}
        public bool Mounted {get;private set;}
        public bool IsCinematic => CurrentPhase==Phase.CrossingGate||CurrentPhase==Phase.EnteringTown||CurrentPhase==Phase.LeavingVillage||CurrentPhase==Phase.Handover||CurrentPhase==Phase.Clothing||CurrentPhase==Phase.Mounting||CurrentPhase==Phase.Reception||CurrentPhase==Phase.Weeping;
        public bool ReturnAtCamp {get;private set;}
        public bool HandoverComplete {get;private set;}
        public Transform WaitingJesus => waitingJesus.transform.parent;
        public bool MusicActive => music&&music.isPlaying;
        public Transform Player => player;
        public Transform Donkey => donkey.transform;
        public Transform[] Disciples => disciples;
        public Transform[] Crowd => crowd;
        public int ActivePalms => residents.Count(r=>r&&r.gameObject.activeInHierarchy&&r.Kind==EntryTownResident.WelcomeKind.Palm);
        public EntryTownResident[] Residents => residents;
        public Sprite SadPortrait => sadPortrait;
        JourneyGame game; Transform player; SpriteRenderer body,background,donkey,owner,waitingJesus;
        Material material; Sprite dot,bare,clothed,pointing,palmSprite,cloakSprite,normalPortrait,sadPortrait;
        Sprite[] mountedFrames,landscapes,griefFrames;
        Transform[] disciples=new Transform[12],crowd=new Transform[18],cloaks=new Transform[2];
        EntryTownResident[] residents=new EntryTownResident[18];
        GameObject environment; LineRenderer rope;
        Canvas canvas; GameObject captionPanel; UnityEngine.UI.Image sceneFade,letterboxTop,letterboxBottom;
        TextMeshProUGUI caption,subcaption; UnityEngine.UI.Image dialoguePortrait;
        RectTransform portraitFrame,speakerRect,lineRect;
        AudioSource music;AudioClip score;
        UnityEngine.Rendering.Universal.PixelPerfectCamera pixel;
        bool oldPixel,shutdown,busy,paused,enteringTown;float clock,motion,wave,cameraSize;
        Vector3 cameraTarget;
        const float EntryY=-3.8f;
        readonly Vector3[] crowdPoints=new[]{new Vector3(-9.2f,-.85f),new Vector3(-7,-.55f),new Vector3(-4.9f,-1),new Vector3(-2.8f,-.65f),new Vector3(-.65f,-.9f),new Vector3(1.45f,-.55f),new Vector3(3.55f,-1),new Vector3(5.75f,-.7f),new Vector3(8.2f,-.95f),new Vector3(-8.9f,-6.1f),new Vector3(-6.55f,-5.9f),new Vector3(-4.35f,-6.15f),new Vector3(-2.2f,-5.9f),new Vector3(0,-6.1f),new Vector3(2.3f,-5.95f),new Vector3(4.55f,-6.15f),new Vector3(6.7f,-5.9f),new Vector3(9,-6.1f)};
        Vector3 CampPoint(int i){if(i==4)return new Vector3(-.4f,-4.9f);if(i==1)return new Vector3(-2.6f,-4.9f);int n=i-(i>1?1:0)-(i>4?1:0);return new Vector3(4.6f+(n%5)*1.48f,n<5?-2.5f:-4.65f);}
        public void Initialize(JourneyGame g,Transform p,SpriteRenderer b,Material m,Sprite square,int step)
        {JourneyObjectFactory.BeginStage(this,step);
            game=g;player=p;body=b;material=m;dot=square;
            var art=JourneySpriteAssets.LoadAll("CrossTheLine/JerusalemEntryAtlas");
            Sprite Find(string name)=>art.FirstOrDefault(s=>s.name==name)??throw new Exception("Entry sprite missing: "+name);
            bare=Find("DonkeyBare");clothed=Find("DonkeyClothed");mountedFrames=new[]{Find("MountedJesus0"),Find("MountedJesus1")};palmSprite=JourneySpriteAssets.Load("CrossTheLine/EntryPalmFrond");if(!palmSprite)throw new Exception("Palm sprite missing");cloakSprite=Find("OuterCloak");pointing=Find("JesusPointing");
            normalPortrait=JourneySpriteAssets.Load("CrossTheLine/ConfessionJesusNormal");sadPortrait=JourneySpriteAssets.Load("CrossTheLine/EntryJesusSad");
            griefFrames=JourneySpriteAssets.LoadAll("CrossTheLine/EntryGriefAtlas").OrderBy(s=>s.name).ToArray();if(griefFrames.Length!=3)throw new Exception("Three entry grief poses required");
            landscapes=new[]{JourneySpriteAssets.Load("CrossTheLine/EntryVillageLandscape"),JourneySpriteAssets.Load("CrossTheLine/EntryGateLandscape"),JourneySpriteAssets.Load("CrossTheLine/EntryTownCityLandscape")};if(landscapes.Any(s=>!s)||!sadPortrait)throw new Exception("Entry scene art missing");
            pixel=g.journeyCamera.GetComponent<UnityEngine.Rendering.Universal.PixelPerfectCamera>();if(pixel){oldPixel=pixel.enabled;pixel.enabled=false;}
            environment=JourneyObjectFactory.Create("JerusalemEntryEnvironment");environment.transform.SetParent(transform,false);
            background=JourneyObjectFactory.Create("예루살렘 입성 배경").GetOrAddComponent<SpriteRenderer>();background.transform.SetParent(environment.transform,false);background.sharedMaterial=m;background.sortingOrder=-30;
            for(int i=0;i<12;i++)disciples[i]=Actor(JourneyGame.ActorNames[i],g.disciples[i],CampPoint(i),1.55f,true,true,i);
            var families=JourneySpriteAssets.LoadAll("CrossTheLine/ChildrenBlessingAtlas");var father=families.First(s=>s.name=="Father");var mother=families.First(s=>s.name=="Mother");
            owner=Actor("나귀의 주인",father,new Vector3(7.2f,-2.9f),1.8f,true).GetChild(0).GetComponent<SpriteRenderer>();
            waitingJesus=Actor("돌아오는 제자들을 기다리는 예수님",g.jesusFrames[0],new Vector3(-4,-2.9f),1.85f).GetChild(0).GetComponent<SpriteRenderer>();waitingJesus.sortingOrder=60;
            donkey=Actor("나귀",bare,new Vector3(2.5f,-2.9f),1.65f).GetChild(0).GetComponent<SpriteRenderer>();donkey.sortingOrder=58;
            rope=JourneyObjectFactory.Create("나귀의 고삐").GetOrAddComponent<LineRenderer>();rope.transform.SetParent(environment.transform,false);rope.sharedMaterial=m;rope.positionCount=2;rope.startWidth=rope.endWidth=.035f;rope.startColor=rope.endColor=new Color(.36f,.25f,.13f);rope.sortingOrder=40;rope.useWorldSpace=true;
            for(int i=0;i<2;i++)cloaks[i]=Actor(i==0?"빌립의 겉옷":"안드레의 겉옷",cloakSprite,Vector3.zero,.42f);
            var work=JourneySpriteAssets.LoadAll("CrossTheLine/EntryVillagersWork").OrderBy(s=>s.name).ToArray();var strides=JourneySpriteAssets.LoadAll("CrossTheLine/EntryVillagersWalk").OrderBy(s=>s.name).ToArray();var cheers=JourneySpriteAssets.LoadAll("CrossTheLine/EntryVillagersCheer").OrderBy(s=>s.name).ToArray();if(work.Length!=6||strides.Length!=6||cheers.Length!=6)throw new Exception("Six town residents in three poses required");
            int[] kinds={0,5,1,2,4,3,0,2,1,5,4,3,2,0,1,3,4,2};
            for(int i=0;i<18;i++)
            {
                int kind=kinds[i];float size=kind==5?(i<9?.94f:1.04f):(i<9?1.24f:1.44f)+(i%3)*.06f;var start=crowdPoints[i]+new Vector3(i%2==0?-.16f:.16f,i<9?.80f:-.42f);
                crowd[i]=Actor("찬양하는 사람 "+(i+1),work[kind],start,size);residents[i]=crowd[i].gameObject.GetOrAddComponent<EntryTownResident>();residents[i].Initialize(i,kind,work[kind],strides[kind],cheers[kind],start,crowdPoints[i],size,m,dot);
            }
            BuildUI();BuildMusic();ConfigureStep(step);
        }
        Transform Actor(string name,Sprite sprite,Vector3 point,float height,bool flip=false,bool walking=false,int index=0)
        {
            var root=JourneyObjectFactory.Create(name).transform;root.SetParent(environment.transform,false);root.position=point;
            var r=JourneyObjectFactory.Create("Sprite").GetOrAddComponent<SpriteRenderer>();r.transform.SetParent(root,false);r.sharedMaterial=material;r.sortingOrder=50;r.flipX=flip;SetSprite(r,sprite,height);
            if(walking)r.gameObject.GetOrAddComponent<DiscipleWalking>().Initialize(index,root);return root;
        }
        void SetSprite(SpriteRenderer r,Sprite sprite,float height)
        {
            r.sprite=sprite;float s=height/sprite.bounds.size.y;r.transform.localScale=Vector3.one*s;r.transform.localPosition=new Vector3(-sprite.bounds.center.x*s,-sprite.bounds.min.y*s);r.transform.localRotation=Quaternion.identity;
        }
        void Background(int index){background.sprite=landscapes[index];background.transform.localScale=Vector3.one*Mathf.Max(24/background.sprite.bounds.size.x,13.5f/background.sprite.bounds.size.y);background.transform.localPosition=Vector3.zero;}
        void MountedSprite(bool moving){SetSprite(donkey,mountedFrames[moving?(int)(motion*7)%2:0],2.65f);donkey.flipX=false;}
        void GriefPose(int index){SetSprite(donkey,griefFrames[index],2.65f*griefFrames[index].bounds.size.y/griefFrames.Max(s=>s.bounds.size.y));donkey.flipX=false;}
        public void ConfigureStep(int step)
        {JourneyObjectFactory.BeginStage(this,step);
            Stage=Mathf.Min(step,8);clock=0;busy=false;ReturnAtCamp=false;HandoverComplete=Stage>=3;DonkeyReleased=Stage>=2;CloaksPlaced=Stage>=4?2:0;Mounted=Stage>=5;
            CurrentPhase=Stage==0?Phase.Commission:Stage==1?Phase.Village:Stage==2?Phase.Returning:Stage==3?Phase.CloaksReady:Stage==4?Phase.MountReady:Stage==5?Phase.Procession:Stage==6?Phase.PraiseReady:Stage==7?Phase.WeepReady:Phase.Closing;
            Background(Stage==1||Stage==2?0:Stage>=6?2:1);
            foreach(var cloak in cloaks)cloak.gameObject.SetActive(false);captionPanel.SetActive(false);sceneFade.color=Color.clear;letterboxTop.color=letterboxBottom.color=Color.clear;waitingJesus.transform.parent.gameObject.SetActive(false);
            foreach(var d in disciples)d.gameObject.SetActive(true);foreach(var person in crowd){person.gameObject.SetActive(Stage>=6);foreach(var r in person.GetComponentsInChildren<SpriteRenderer>().Where(s=>s.name!="FootShadow"))r.color=Color.white;}owner.transform.parent.gameObject.SetActive(Stage==1||Stage==2);
            body.enabled=!Mounted;body.maskInteraction=SpriteMaskInteraction.None;body.flipX=false;player.rotation=Quaternion.identity;body.sortingOrder=60;
            var shadow=player.Find("FootShadow");if(shadow)shadow.gameObject.SetActive(!Mounted);
            foreach(var walker in body.GetComponents<DiscipleWalking>())walker.enabled=Stage==1||Stage==2;
            donkey.transform.parent.gameObject.SetActive(Stage!=0);rope.enabled=Stage==1||Stage==2;
            for(int i=0;i<12;i++)disciples[i].position=CampPoint(i);
            if(Stage==0){player.position=JourneyStageLayout.Point(this,player,"Placement 1",new Vector3(-4,-2.9f));SetSprite(body,pointing,1.85f);}
            else if(Stage==1||Stage==2)
            {
                player.position=JourneyStageLayout.Point(this,player,"Placement 2",new Vector3(Stage==1?-7:1.5f,-2.9f));SetSprite(body,game.disciples[4],1.85f);
                for(int i=0;i<12;i++)disciples[i].gameObject.SetActive(i==1);disciples[1].position=player.position+new Vector3(-1.8f,-2);
                SetSprite(donkey,bare,1.65f);donkey.flipX=Stage==2;donkey.transform.parent.position=Stage==1?new Vector3(3.8f,-2.9f):player.position+new Vector3(2.4f,0);
            }
            else if(Stage==3||Stage==4){player.position=JourneyStageLayout.Point(this,player,"Placement 3",new Vector3(.4f,-2.9f));SetSprite(body,game.jesusFrames[0],1.85f);SetSprite(donkey,Stage==3?bare:clothed,1.65f);donkey.flipX=true;donkey.transform.parent.position=JourneyStageLayout.Point(this,donkey.transform.parent,"Placement 4",new Vector3(2,-2.9f));}
            else {donkey.transform.parent.position=JourneyStageLayout.Point(this,donkey.transform.parent,"Placement 5",new Vector3(Stage==5?-7:Stage==6?-5:-.5f,EntryY));player.position=donkey.transform.parent.position;MountedSprite(false);UpdateProcession(false);}
            if(Stage<=6)foreach(var r in residents)r.Reset(false);
            cameraTarget=new Vector3(0,-.35f,-10);cameraSize=6.25f;if(Stage>=7){cameraTarget=new Vector3(1,-.1f,-10);cameraSize=5.7f;}
            if(Stage==0){game.journeyCamera.transform.position=cameraTarget;game.journeyCamera.orthographicSize=cameraSize;}
            if(Stage<5)music.Stop();else if(!music.isPlaying)music.Play();
            if(Stage==8){GriefPose(2);cameraTarget=new Vector3(1,-1.25f,-10);cameraSize=4.5f;letterboxTop.color=letterboxBottom.color=new Color(.03f,.05f,.06f,.85f);foreach(var person in crowd)foreach(var r in person.GetComponentsInChildren<SpriteRenderer>().Where(s=>s.name!="FootShadow"))r.color=new Color(.70f,.73f,.77f);}
            if(Stage==0)enteringTown=false;
            if(Stage==6&&enteringTown){CurrentPhase=Phase.EnteringTown;sceneFade.color=new Color(.03f,.05f,.06f,1);enteringTown=false;}
            game.BaptismCinema(IsCinematic);Hud();UpdateRope();
        }
        void Hud()
        {
            string[] goals={"맞은편 마을의 나귀를 부탁하기","빌립과 안드레 · 마을에서 나귀의 주인 만나기","허락받은 나귀를 예수님께 데려가기","두 제자의 겉옷을 나귀 등에 올리기","예수님을 나귀에 모시기","나귀를 타고 예루살렘 성문으로 들어가기","하던 일을 멈추고 맞이하는 마을 사람들","예루살렘을 바라보며 흘리시는 눈물","환호 속에 담긴 예수님의 마음 기억하기"};
            game.EntryHud(goals[Stage],Stage==1?"D / → · 마을로 가기    가까이에서 E · 나귀의 주인에게 말하기":Stage==2?(ReturnAtCamp?"A / ← · 기다리시는 예수님께 접근    E · 고삐 건네기":"A / ← · 마을에서 나가기    출구에서 E · 예수님께 돌아가기"):Stage==5?"D / → · 나귀를 타고 성문으로    가까이에서 E · 입성":"E · 이야기 진행");
        }
        public void Tick(float dt,bool canMove,bool press)
        {
            if(game.Mode=="menu"||game.Mode=="dialogue"||game.Mode=="chapter_end")return;
            motion+=dt;wave+=dt;clock+=dt;if(Stage>=6)foreach(var r in residents)r.Tick(dt,donkey.transform.parent.position.x,true,Stage>=7);
            if(CurrentPhase==Phase.CrossingGate){sceneFade.color=new Color(.03f,.05f,.06f,Mathf.Clamp01(clock/.45f));if(clock>=.45f){enteringTown=true;game.BaptismAdvance();}return;}
            if(CurrentPhase==Phase.EnteringTown){sceneFade.color=new Color(.03f,.05f,.06f,1-Mathf.Clamp01(clock/.45f));if(clock>=.45f){sceneFade.color=Color.clear;CurrentPhase=Phase.PraiseReady;clock=0;game.BaptismCinema(false);Hud();}return;}
            if(CurrentPhase==Phase.LeavingVillage)
            {
                if(clock>=.45f&&!ReturnAtCamp)EnterReturnRoad();
                sceneFade.color=new Color(.03f,.05f,.06f,clock<.45f?clock/.45f:1-Mathf.Clamp01((clock-.45f)/.45f));
                if(clock>=.9f){sceneFade.color=Color.clear;CurrentPhase=Phase.Returning;clock=0;game.BaptismCinema(false);Hud();}return;
            }
            if(CurrentPhase==Phase.Handover)
            {
                disciples[1].position=Vector3.Lerp(new Vector3(-2.2f,-2.9f),CampPoint(1),Mathf.SmoothStep(0,1,clock/.55f));
                if(clock>=.55f)disciples[1].GetChild(0).GetComponent<SpriteRenderer>().flipX=false;
                float t=Mathf.SmoothStep(0,1,Mathf.Clamp01((clock-.55f)/1.3f));waitingJesus.transform.parent.position=Vector3.Lerp(new Vector3(-4,-2.9f),new Vector3(.4f,-2.9f),t);
                SetSprite(waitingJesus,game.jesusFrames[t>0&&t<1?(int)(motion*9)%game.jesusFrames.Length:0],1.85f);UpdateRope();
                if(clock>=1.5f){captionPanel.SetActive(true);caption.text="두 제자가 예수님께 나귀의 고삐를 건네드립니다.";subcaption.text="마을에서 돌아와, 기다리시던 예수님과 다시 만났습니다.";}
                if(clock>=2.1f){HandoverComplete=true;busy=true;game.MeetingTalk(()=>{busy=false;game.BaptismAdvance();});}return;
            }
            if(CurrentPhase==Phase.Clothing)
            {
                for(int i=0;i<2;i++){float t=Mathf.Clamp01((clock-i*.45f)/.85f);var start=disciples[i==0?4:1].position+new Vector3(.25f,1);var target=donkey.transform.parent.position+new Vector3(0,1.05f);cloaks[i].position=Vector3.Lerp(start,target,Mathf.SmoothStep(0,1,t))+new Vector3(0,Mathf.Sin(t*Mathf.PI)*.5f);cloaks[i].gameObject.SetActive(t<1);if(t>=1)CloaksPlaced=Mathf.Max(CloaksPlaced,i+1);}
                if(clock>=1.5f){SetSprite(donkey,clothed,1.65f);game.BaptismAdvance();}return;
            }
            if(CurrentPhase==Phase.Mounting)
            {
                if(clock<1.0f){player.position=Vector3.Lerp(new Vector3(.4f,-2.9f),new Vector3(.55f,-2.9f),Mathf.SmoothStep(0,1,clock));game.MeetingWalk(true,1);}
                else {body.enabled=false;var shadow=player.Find("FootShadow");if(shadow)shadow.gameObject.SetActive(false);Mounted=true;MountedSprite(false);}
                if(clock>=1.4f)game.BaptismAdvance();return;
            }
            if(CurrentPhase==Phase.Reception)
            {
                donkey.transform.parent.position=Vector3.Lerp(new Vector3(-5,EntryY),new Vector3(-.5f,EntryY),Mathf.SmoothStep(0,1,clock/1.8f));MountedSprite(true);UpdateProcession(true);
                if(clock>=1.8f)game.BaptismAdvance();return;
            }
            if(CurrentPhase==Phase.Weeping)
            {
                GriefPose(clock<1.3f?0:clock<2.7f?1:2);
                float quiet=Mathf.SmoothStep(0,1,Mathf.Clamp01(clock/2));foreach(var person in crowd)foreach(var r in person.GetComponentsInChildren<SpriteRenderer>().Where(s=>s.name!="FootShadow"))r.color=Color.Lerp(Color.white,new Color(.70f,.73f,.77f),quiet);
                cameraTarget=new Vector3(1,-1.25f,-10);cameraSize=Mathf.Lerp(5.7f,4.5f,Mathf.SmoothStep(0,1,clock/3.6f));letterboxTop.color=letterboxBottom.color=new Color(.03f,.05f,.06f,.85f*quiet);
                captionPanel.SetActive(clock>=2.7f);caption.text="예루살렘을 바라보시던 예수님의 눈에 눈물이 맺혔습니다.";subcaption.text="환호 너머, 이 성의 아픔을 바라보시는 마음";
                if(clock>=4.5f){captionPanel.SetActive(false);busy=true;game.MeetingTalk(()=>{busy=false;game.BaptismAdvance();});}return;
            }
            if(!canMove||busy)return;
            var k=Keyboard.current;float x=k==null?0:(k.dKey.isPressed||k.rightArrowKey.isPressed?1:0)-(k.aKey.isPressed||k.leftArrowKey.isPressed?1:0);
            if(Stage==1||Stage==2)
            {
                var p=player.position;p.x=Mathf.Clamp(p.x+x*dt*3,ReturnAtCamp?-.4f:-7,ReturnAtCamp?7:1.5f);player.position=p;game.MeetingWalk(x!=0,x);disciples[1].position=p+new Vector3(-1.8f,ReturnAtCamp?2:-2);
                if(Stage==2){donkey.transform.parent.position=p+new Vector3(2.4f,ReturnAtCamp?2:0);donkey.flipX=true;}UpdateRope();
            }
            if(Stage==5)
            {
                var p=donkey.transform.parent.position;p.x=Mathf.Clamp(p.x+x*dt*2.6f,-7,8);donkey.transform.parent.position=p;player.position=p;MountedSprite(x!=0);UpdateProcession(x!=0);
                // Residents leave their work places on foot and keep the procession lane clear.
            }
            if(press)Interact();
        }
        void UpdateProcession(bool moving){for(int i=0;i<12;i++){disciples[i].position=donkey.transform.parent.position+new Vector3(-3.2f-i*1.3f,0);disciples[i].GetChild(0).GetComponent<SpriteRenderer>().flipX=false;}}
        void EnterReturnRoad()
        {
            ReturnAtCamp=true;Background(1);owner.transform.parent.gameObject.SetActive(false);waitingJesus.transform.parent.gameObject.SetActive(true);waitingJesus.transform.parent.position=JourneyStageLayout.Point(this,waitingJesus.transform.parent,"Placement 6",new Vector3(-4,-2.9f));
            player.position=JourneyStageLayout.Point(this,player,"Placement 7",new Vector3(7,-4.9f));disciples[1].position=player.position+new Vector3(-1.8f,2);donkey.transform.parent.position=player.position+new Vector3(2.4f,2);UpdateRope();
            cameraTarget=new Vector3(0,-.35f,-10);cameraSize=6.25f;
        }
        void UpdateRope(){if(!rope.enabled)return;var p=donkey.transform.parent.position;rope.SetPosition(0,p+new Vector3(Stage==1?.7f:-.7f,.9f));var hand=Stage==1?new Vector3(6,-2):player.position+new Vector3(.35f,.8f);if(CurrentPhase==Phase.Handover)hand=Vector3.Lerp(hand,waitingJesus.transform.parent.position+new Vector3(.35f,.8f),Mathf.SmoothStep(0,1,Mathf.Clamp01((clock-1.5f)/.6f)));rope.SetPosition(1,hand);}
        public void Interact()
        {
            if(busy||IsCinematic||Stage==1&&player.position.x<.9f||Stage==2&&player.position.x>(ReturnAtCamp?-.2f:-6.4f)||Stage==5&&donkey.transform.parent.position.x<7.3f)return;
            if(Stage==5){CurrentPhase=Phase.CrossingGate;clock=0;game.BaptismCinema(true);return;}
            if(Stage==2){CurrentPhase=ReturnAtCamp?Phase.Handover:Phase.LeavingVillage;if(ReturnAtCamp)body.flipX=false;clock=0;game.BaptismCinema(true);return;}
            if(Stage==7){CurrentPhase=Phase.Weeping;clock=0;captionPanel.SetActive(false);game.BaptismCinema(true);return;}
            busy=true;game.MeetingTalk(()=>
            {
                busy=false;
                if(Stage==3||Stage==4||Stage==6){CurrentPhase=Stage==3?Phase.Clothing:Stage==4?Phase.Mounting:Phase.Reception;clock=0;caption.text=Stage==3?"두 제자가 자신의 겉옷을 나귀 등에 올렸습니다.":Stage==4?"예수님께서 겉옷을 깐 나귀 등에 올라타셨습니다.":"호산나! 주의 이름으로 오시는 이를 찬양하라!";subcaption.text=Stage==3?"빌립의 겉옷 · 안드레의 겉옷":Stage==4?"겸손한 왕의 예루살렘 입성":"손과 빵, 옷과 종려가지로 저마다 환영했습니다.";captionPanel.SetActive(true);foreach(var cloak in cloaks)cloak.gameObject.SetActive(Stage==3);game.BaptismCinema(true);}
                else game.BaptismAdvance();
            });
        }
        public void DecorateDialogue(Transform parent,RectTransform speaker,RectTransform line)
        {
            speakerRect=speaker;lineRect=line;portraitFrame=Box(parent,"예수님 표정 테두리",new Vector2(.025f,.13f),new Vector2(.17f,.93f),new Color(.85f,.74f,.46f));
            dialoguePortrait=Box(portraitFrame,"예수님 표정",new Vector2(.035f,.04f),new Vector2(.965f,.96f),Color.white).GetComponent<UnityEngine.UI.Image>();dialoguePortrait.preserveAspect=true;UpdatePortrait();
        }
        void UpdatePortrait(){if(!dialoguePortrait)return;bool show=game.DialogueSpeaker=="예수님";portraitFrame.gameObject.SetActive(show);dialoguePortrait.sprite=Stage>=7?sadPortrait:normalPortrait;speakerRect.anchorMin=new Vector2(show?.2f:.035f,.71f);lineRect.anchorMin=new Vector2(show?.2f:.035f,.2f);}
        void LateUpdate()
        {
            if(shutdown||!game)return;canvas.enabled=game.Mode!="menu"&&game.Mode!="chapter_end"&&game.Mode!="dialogue";UpdatePortrait();bool now=game.Mode=="menu"||game.Mode=="chapter_end";
            if(now!=paused){paused=now;if(paused)music.Pause();else music.UnPause();}music.mute=game.SoundMuted;music.volume=CurrentPhase==Phase.Weeping?Mathf.Lerp(.32f,.055f,Mathf.SmoothStep(0,1,clock/2)):Stage>=7?.055f:.32f;
        }
        public void UpdateCamera(){if(game.Mode=="menu")return;float t=1-Mathf.Exp(-3*Time.unscaledDeltaTime);game.journeyCamera.transform.position=Vector3.Lerp(game.journeyCamera.transform.position,cameraTarget,t);game.journeyCamera.orthographicSize=Mathf.Lerp(game.journeyCamera.orthographicSize,cameraSize,t);}
        RectTransform Box(Transform parent,string name,Vector2 min,Vector2 max,Color color){var o=JourneyObjectFactory.Create(name,typeof(RectTransform),typeof(UnityEngine.UI.Image));o.transform.SetParent(parent,false);var r=o.GetComponent<RectTransform>();r.anchorMin=min;r.anchorMax=max;r.offsetMin=r.offsetMax=Vector2.zero;var image=o.GetComponent<UnityEngine.UI.Image>();image.color=color;image.raycastTarget=false;return r;}
        TextMeshProUGUI Text(Transform parent,string name,string value,Vector2 min,Vector2 max,int size,Color color){var o=JourneyObjectFactory.Create(name,typeof(RectTransform));o.transform.SetParent(parent,false);var r=o.GetComponent<RectTransform>();r.anchorMin=min;r.anchorMax=max;r.offsetMin=r.offsetMax=Vector2.zero;var t=o.GetOrAddComponent<TextMeshProUGUI>();t.font=game.koreanFont;t.text=value;t.fontSize=size;t.color=color;t.raycastTarget=false;return t;}
        void BuildUI()
        {
            var root=JourneyObjectFactory.Create("JerusalemEntryCinema",typeof(RectTransform),typeof(Canvas),typeof(UnityEngine.UI.CanvasScaler));root.transform.SetParent(transform,false);canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=90;var scaler=root.GetComponent<UnityEngine.UI.CanvasScaler>();scaler.uiScaleMode=UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1440,900);scaler.matchWidthOrHeight=.5f;
            sceneFade=Box(root.transform,"마을과 성문 사이 전환",Vector2.zero,Vector2.one,Color.clear).GetComponent<UnityEngine.UI.Image>();letterboxTop=Box(root.transform,"고요한 시선 위",new Vector2(0,.94f),Vector2.one,Color.clear).GetComponent<UnityEngine.UI.Image>();letterboxBottom=Box(root.transform,"고요한 시선 아래",Vector2.zero,new Vector2(1,.055f),Color.clear).GetComponent<UnityEngine.UI.Image>();
            captionPanel=Box(root.transform,"입성의 순간",new Vector2(.04f,.79f),new Vector2(.71f,.94f),new Color(.08f,.17f,.18f,.88f)).gameObject;caption=Text(captionPanel.transform,"말씀","",new Vector2(.04f,.42f),new Vector2(.96f,.91f),25,new Color(.96f,.9f,.75f));subcaption=Text(captionPanel.transform,"안내","",new Vector2(.04f,.08f),new Vector2(.96f,.40f),17,new Color(.85f,.73f,.45f));
        }
        void BuildMusic()
        {
            music=gameObject.AddComponent<AudioSource>();music.playOnAwake=false;music.loop=true;const int rate=22050;var data=new float[rate*20];int[] melody={0,4,7,12,11,7,9,7,4,7,12,14,12,11,7,4};float[] bass={130.81f,164.81f,174.61f,196};
            for(int i=0;i<data.Length;i++){float t=i/(float)rate;int beat=(int)(t/.625f);float within=t%.625f;float f=261.63f*Mathf.Pow(2,melody[beat%16]/12f);float env=Mathf.Min(within*18,1)*Mathf.Exp(-within*3.1f);float strings=(Mathf.Sin(2*Mathf.PI*f*t)+.25f*Mathf.Sin(4*Mathf.PI*f*t))*.11f*env;float b=bass[(beat/4)%4];float chord=(Mathf.Sin(2*Mathf.PI*b*t)+.4f*Mathf.Sin(2*Mathf.PI*b*1.5f*t))*.045f;float drum=Mathf.Sin(2*Mathf.PI*52*t)*Mathf.Exp(-within*22)*.08f;data[i]=(strings+chord+drum)*Mathf.Min(t*2,1)*Mathf.Min((20-t)*2,1);}
            score=AudioClip.Create("JerusalemProcession_OriginalClassicalTheme",data.Length,1,rate,false);score.SetData(data,0);music.clip=score;
        }
        public void Shutdown(){if(shutdown)return;shutdown=true;if(pixel)pixel.enabled=oldPixel;if(body)body.enabled=true;if(music)music.Stop();if(score)Destroy(score);}
        void OnDestroy(){Shutdown();}
    }
}
