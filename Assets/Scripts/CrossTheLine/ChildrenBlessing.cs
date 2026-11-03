using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CrossTheLine
{
    public sealed class ChildrenBlessing : MonoBehaviour
    {
        public enum Phase { Arrival, Approaching, Request, Blocking, Blocked, Rebuke, Opening, Invitation, Ready, Blessing, Farewell }
        public Phase CurrentPhase { get; private set; }
        public int Stage { get; private set; }
        public int BlessedCount { get; private set; }
        public bool IsCinematic => CurrentPhase == Phase.Approaching || CurrentPhase == Phase.Blocking || CurrentPhase == Phase.Opening || CurrentPhase == Phase.Blessing;
        public Transform Player => player;
        public Transform[] Children => children;
        public Transform Judas => disciples[11];
        // The outer fingertips, rather than the wrist/sleeve, meet the child's crown.
        public Vector3 Hand => body.transform.TransformPoint(new Vector3(body.sprite.bounds.min.x + body.sprite.bounds.size.x * .035f, body.sprite.bounds.min.y + body.sprite.bounds.size.y * .53f));
        readonly System.Collections.Generic.Dictionary<Sprite,Vector2> crowns=new System.Collections.Generic.Dictionary<Sprite,Vector2>();
        public Vector3 ChildHead(int i)
        {
            var r=children[i].GetChild(0).GetComponent<SpriteRenderer>();var s=r.sprite;
            if(!crowns.TryGetValue(s,out var crown))
            {
                var b=JourneySpriteAssets.VisibleBounds(s);crown=new Vector2(b.center.x,b.max.y-.5f/s.pixelsPerUnit);
                if(s.texture.isReadable)
                {
                    var pixels=s.texture.GetPixels32();bool found=false;
                    for(int y=(int)s.rect.yMax-1;y>=s.rect.y&&!found;y--){float sum=0;int count=0;for(int x=(int)s.rect.x;x<s.rect.xMax;x++)if(pixels[y*s.texture.width+x].a>200){sum+=x+.5f;count++;}if(count>0){crown=new Vector2((sum/count-s.rect.x-s.pivot.x)/s.pixelsPerUnit,(y+.5f-s.rect.y-s.pivot.y)/s.pixelsPerUnit);found=true;}}
                }
                crowns[s]=crown;
            }
            if(r.flipX)crown.x=-crown.x;return r.transform.TransformPoint(crown);
        }
        public bool HandsJoined { get; private set; }
        JourneyGame game;
        Transform player;
        SpriteRenderer body, warmth;
        Transform[] children = new Transform[3], parents = new Transform[2], disciples = new Transform[12];
        readonly Vector3[] starts = { new Vector3(-3,-3), new Vector3(-4.1f,-4.5f), new Vector3(-2.2f,-4.65f) };
        readonly Vector3[] rests = { new Vector3(-6.7f,-2.45f), new Vector3(-8.7f,-4.7f), new Vector3(-6.5f,-4.9f) };
        readonly Vector3[] parentStops = { new Vector3(-5.8f,-2.4f), new Vector3(-7.6f,-4.6f) };
        readonly Vector3 arrivalOffset = new Vector3(-3,0);
        // Calibrated against each sprite's actual hair/face alpha, including sleeve clearance.
        readonly float[] blessingChildHeights = { 1.10f, 1.10f, 1.12f };
        readonly float[] crownClearance = { -.03f, -.03f, -.04f };
        Vector3 DiscipleHome(int i) => i==11 ? new Vector3(5,-3.3f) : i==0 ? new Vector3(5,-5.2f) : new Vector3(6.6f+(i-1)%4*1.35f,-1.1f-(i-1)/4*1.9f);
        Vector3 BlockPosition(int i) => i==11 ? new Vector3(-.8f,-3.3f) : new Vector3(.6f,-5.2f);
        Material material;
        Sprite blessingSprite, normalPortrait, madPortrait, happyPortrait;
        GameObject environment, cinemaPanel;
        Canvas canvas;
        TextMeshProUGUI caption, count;
        UnityEngine.UI.Image portrait;
        RectTransform portraitFrame, speakerRect, lineRect;
        AudioSource audioSource;
        AudioClip chime;
        UnityEngine.Rendering.Universal.PixelPerfectCamera pixel;
        bool oldPixel, shutdown, busy;
        float clock, cameraSize;
        Vector3 cameraTarget, contact;
        int currentChild;

        public void Initialize(JourneyGame g, Transform p, SpriteRenderer b, Material m, Sprite square, int step)
        {JourneyObjectFactory.BeginStage(this,step);
            game=g;player=p;body=b;material=m;
            var art=JourneySpriteAssets.LoadAll("CrossTheLine/ChildrenBlessingAtlas");
            Sprite Find(string name)=>art.FirstOrDefault(s=>s.name==name) ?? throw new Exception("Missing child blessing art: "+name);
            blessingSprite=Find("JesusBlessing"); normalPortrait=JourneySpriteAssets.Load("CrossTheLine/ConfessionJesusNormal"); madPortrait=JourneySpriteAssets.Load("CrossTheLine/ChildrenJesusMad"); happyPortrait=JourneySpriteAssets.Load("CrossTheLine/ConfessionJesusLaugh");
            pixel=g.journeyCamera.GetComponent<UnityEngine.Rendering.Universal.PixelPerfectCamera>(); if(pixel){oldPixel=pixel.enabled;pixel.enabled=false;}
            environment=JourneyObjectFactory.Create("JerusalemRoadFamilies");environment.transform.SetParent(transform,false);
            var bg=JourneyObjectFactory.Create("예루살렘으로 가는 따뜻한 길").GetOrAddComponent<SpriteRenderer>();bg.transform.SetParent(environment.transform,false);bg.sprite=JourneySpriteAssets.Load("CrossTheLine/ChildrenRoadLandscape");if(!bg.sprite)throw new Exception("Road art missing");bg.sharedMaterial=m;bg.sortingOrder=-30;bg.transform.localScale=Vector3.one*Mathf.Max(24/bg.sprite.bounds.size.x,13.5f/bg.sprite.bounds.size.y);
            for(int i=0;i<3;i++)children[i]=Actor("아이 "+(i+1),Find("Child"+i),starts[i],1.0f);
            parents[0]=Actor("아이들의 어머니",Find("Mother"),new Vector3(-6,-3.1f),1.9f);parents[1]=Actor("아이들의 아버지",Find("Father"),new Vector3(-7.2f,-4.15f),2.0f);
            for(int i=0;i<12;i++)disciples[i]=Actor(JourneyGame.ActorNames[i],g.disciples[i],DiscipleHome(i),1.55f);
            var glow=JourneyObjectFactory.Create("축복의 따뜻한 빛").GetOrAddComponent<SpriteRenderer>();glow.transform.SetParent(environment.transform,false);glow.sprite=square;glow.sharedMaterial=m;glow.sortingOrder=74;glow.transform.localScale=new Vector3(.11f,.11f,1);warmth=glow;
            BuildUI();BuildAudio();ConfigureStep(step);
        }
        Transform Actor(string name,Sprite sprite,Vector3 pos,float height)
        {
            var t=JourneyObjectFactory.Create(name).transform;t.SetParent(environment.transform,false);t.position=pos;
            var r=JourneyObjectFactory.Create("Sprite").GetOrAddComponent<SpriteRenderer>();r.transform.SetParent(t,false);r.sprite=sprite;r.sharedMaterial=material;r.sortingOrder=50;r.flipX=!name.StartsWith("아이 ")&&name!="아이들의 아버지";
            SetSprite(r,sprite,height);int index=Array.IndexOf(JourneyGame.ActorNames,name);if(index>=0)r.gameObject.GetOrAddComponent<DiscipleWalking>().Initialize(index,t);return t;
        }
        void SetSprite(SpriteRenderer r,Sprite sprite,float height)
        {
            r.sprite=sprite;var visible=JourneySpriteAssets.VisibleBounds(sprite);float scale=height/visible.size.y;r.transform.localScale=Vector3.one*scale;r.transform.localPosition=new Vector3(-visible.center.x*scale,-visible.min.y*scale);r.transform.localRotation=Quaternion.identity;
        }
        public void ConfigureStep(int step)
        {JourneyObjectFactory.BeginStage(this,step);
            Stage=Mathf.Min(step,5);clock=0;busy=false;HandsJoined=false;BlessedCount=Stage>=5?3:0;currentChild=0;
            CurrentPhase=Stage==0?Phase.Arrival:Stage==1?Phase.Blocking:Stage==2?Phase.Rebuke:Stage==3?Phase.Invitation:Stage==4?Phase.Ready:Phase.Farewell;
            player.position=JourneyStageLayout.Point(this,player,"Placement 1",new Vector3(2,-1.2f));player.rotation=Quaternion.identity;body.enabled=true;body.flipX=true;body.maskInteraction=SpriteMaskInteraction.None;body.sortingOrder=60;
            foreach(var walker in body.GetComponents<DiscipleWalking>())walker.enabled=false;
            if(Stage==4)SetSprite(body,blessingSprite,2.1f);else SetSprite(body,game.jesusFrames[0],1.85f);
            body.flipX=Stage==4?false:true;
            for(int i=0;i<3;i++){children[i].position=Stage>=5?rests[i]:starts[i]+(Stage==0?arrivalOffset:Vector3.zero);var child=children[i].GetChild(0).GetComponent<SpriteRenderer>();SetSprite(child,child.sprite,1);child.flipX=Stage>=5&&i==2;}
            for(int i=0;i<2;i++){parents[i].position=parentStops[i]+(Stage==0?arrivalOffset:Vector3.zero);parents[i].GetChild(0).GetComponent<SpriteRenderer>().flipX=Stage>=5?i==1:i==0;}
            for(int i=0;i<12;i++)disciples[i].position=DiscipleHome(i);
            if(Stage==2){disciples[11].position=BlockPosition(11);disciples[0].position=BlockPosition(0);}
            cameraTarget=new Vector3(0,-.5f,-10);cameraSize=6.6f;
            if(Stage==0){game.journeyCamera.transform.position=cameraTarget;game.journeyCamera.orthographicSize=cameraSize;}
            warmth.enabled=false;cinemaPanel.SetActive(Stage==1);game.BaptismCinema(IsCinematic);game.ChildrenHud(Stage==0?"다가오는 부모와 아이들을 맞이하기":Stage==4?"아이들을 맞이하고 한 명씩 축복하기":"아이들이 오는 길 열기","E · 이야기 진행");
            if(Stage==1){caption.text="부모들이 다가오자 제자들이 앞으로 나서 막았습니다.";count.text="유다와 베드로가 가족들 앞을 막습니다.";}
        }
        public void Tick(float dt,bool canMove,bool press)
        {
            if(game.Mode=="menu"||game.Mode=="dialogue"||game.Mode=="chapter_end")return;
            if(CurrentPhase==Phase.Approaching)
            {
                clock+=dt;float t=Mathf.SmoothStep(0,1,clock/1.4f);
                for(int i=0;i<2;i++)parents[i].position=parentStops[i]+arrivalOffset*(1-t);
                for(int i=0;i<3;i++)children[i].position=starts[i]+arrivalOffset*(1-t);
                if(clock>=1.4f){CurrentPhase=Phase.Request;cinemaPanel.SetActive(false);game.BaptismCinema(false);busy=true;game.MeetingTalk(()=>{busy=false;game.BaptismAdvance();});}return;
            }
            if(CurrentPhase==Phase.Blocking)
            {
                clock+=dt;float t=Mathf.SmoothStep(0,1,clock/1.25f);disciples[11].position=Vector3.Lerp(DiscipleHome(11),BlockPosition(11),t);disciples[0].position=Vector3.Lerp(DiscipleHome(0),BlockPosition(0),t);
                if(clock>=1.25f){CurrentPhase=Phase.Blocked;cinemaPanel.SetActive(false);game.BaptismCinema(false);}return;
            }
            if(CurrentPhase==Phase.Opening)
            {
                clock+=dt;float t=Mathf.SmoothStep(0,1,clock/1.2f);disciples[11].position=Vector3.Lerp(BlockPosition(11),DiscipleHome(11),t);disciples[0].position=Vector3.Lerp(BlockPosition(0),DiscipleHome(0),t);
                if(clock>=1.2f)game.BaptismAdvance();return;
            }
            if(CurrentPhase==Phase.Blessing)
            {
                clock+=dt;
                float t=clock-currentChild*4.0f;var child=children[currentChild];
                if(t<.85f){child.GetChild(0).GetComponent<SpriteRenderer>().flipX=false;child.position=Vector3.Lerp(starts[currentChild],contact,Mathf.SmoothStep(0,1,t/.85f));HandsJoined=false;}
                else if(t<1.65f){child.position=contact;if(!HandsJoined&&!game.SoundMuted)audioSource.PlayOneShot(chime,.45f);HandsJoined=true;warmth.enabled=true;warmth.transform.position=Hand;warmth.color=new Color(1,.84f,.45f,.55f);}
                else
                {
                    HandsJoined=false;warmth.enabled=false;float exit=t-1.65f;
                    child.GetChild(0).GetComponent<SpriteRenderer>().flipX=true;
                    var below=new Vector3(contact.x,-6.15f);var beside=new Vector3(rests[currentChild].x,below.y);
                    // Return along the lower lane to the parents, clear of Jesus, waiting children and the father.
                    child.position=exit<.70f?Vector3.Lerp(contact,below,Mathf.SmoothStep(0,1,exit/.70f)):exit<1.80f?Vector3.Lerp(below,beside,Mathf.SmoothStep(0,1,(exit-.70f)/1.10f)):Vector3.Lerp(beside,rests[currentChild],Mathf.SmoothStep(0,1,(exit-1.80f)/.55f));
                }
                if(t>=4.0f){child.position=rests[currentChild];child.GetChild(0).GetComponent<SpriteRenderer>().flipX=currentChild==2;BlessedCount++;currentChild++;if(currentChild==3){HandsJoined=false;warmth.enabled=false;game.BaptismAdvance();return;}PrepareChild();}
                return;
            }
            if(!canMove||busy)return;
            if(press)Interact();
        }
        void PrepareChild()
        {
            // Match the child's head to the blessing palm; preserve both actors' separate bodies.
            var r=children[currentChild].GetChild(0).GetComponent<SpriteRenderer>();SetSprite(r,r.sprite,blessingChildHeights[currentChild]);
            r.flipX=false;contact=children[currentChild].position+Hand-ChildHead(currentChild)+new Vector3(crownClearance[currentChild],0);caption.text="예수님께 다가온 아이를 따뜻하게 축복하셨습니다.";count.text="아이 "+(currentChild+1)+" / 3";

        }
        public void Interact()
        {
            if(busy||IsCinematic)return;
            if(Stage==0){CurrentPhase=Phase.Approaching;clock=0;caption.text="부모들이 아이들을 데리고 예수님께 다가왔습니다.";count.text="제자들은 아직 예수님 곁에 서 있습니다.";cinemaPanel.SetActive(true);game.BaptismCinema(true);return;}
            if(Stage==4){CurrentPhase=Phase.Blessing;clock=0;currentChild=0;PrepareChild();cinemaPanel.SetActive(true);game.BaptismCinema(true);return;}
            busy=true;game.MeetingTalk(()=>{busy=false;if(Stage==2){CurrentPhase=Phase.Opening;clock=0;caption.text="어린아이들이 내게 오는 것을 막지 말라.";count.text="제자들이 물러나고 길이 열립니다.";cinemaPanel.SetActive(true);game.BaptismCinema(true);}else game.BaptismAdvance();});
        }
        public void DecorateDialogue(Transform parent,RectTransform speaker,RectTransform line)
        {
            speakerRect=speaker;lineRect=line;
            portraitFrame=Box(parent,"예수님 표정 테두리",new Vector2(.025f,.13f),new Vector2(.17f,.93f),new Color(.85f,.74f,.46f));
            portrait=Box(portraitFrame,"예수님 표정",new Vector2(.035f,.04f),new Vector2(.965f,.96f),Color.white).GetComponent<UnityEngine.UI.Image>();portrait.preserveAspect=true;UpdatePortrait();
        }
        void UpdatePortrait(){if(!portrait)return;bool show=game.DialogueSpeaker=="예수님";portraitFrame.gameObject.SetActive(show);portrait.sprite=Stage==2?madPortrait:Stage>=3?happyPortrait:normalPortrait;speakerRect.anchorMin=new Vector2(show?.2f:.035f,.71f);lineRect.anchorMin=new Vector2(show?.2f:.035f,.2f);}
        void LateUpdate(){if(shutdown||!game)return;canvas.enabled=game.Mode!="menu"&&game.Mode!="chapter_end"&&game.Mode!="dialogue";UpdatePortrait();}
        public void UpdateCamera(){if(game.Mode=="menu")return;float t=1-Mathf.Exp(-3*Time.unscaledDeltaTime);game.journeyCamera.transform.position=Vector3.Lerp(game.journeyCamera.transform.position,cameraTarget,t);game.journeyCamera.orthographicSize=Mathf.Lerp(game.journeyCamera.orthographicSize,cameraSize,t);}
        RectTransform Box(Transform parent,string name,Vector2 min,Vector2 max,Color color){var o=JourneyObjectFactory.Create(name,typeof(RectTransform),typeof(UnityEngine.UI.Image));o.transform.SetParent(parent,false);var r=o.GetComponent<RectTransform>();r.anchorMin=min;r.anchorMax=max;r.offsetMin=r.offsetMax=Vector2.zero;var image=o.GetComponent<UnityEngine.UI.Image>();image.color=color;image.raycastTarget=false;return r;}
        TextMeshProUGUI Text(Transform parent,string name,Vector2 min,Vector2 max,int size,Color color){var o=JourneyObjectFactory.Create(name,typeof(RectTransform));o.transform.SetParent(parent,false);var r=o.GetComponent<RectTransform>();r.anchorMin=min;r.anchorMax=max;r.offsetMin=r.offsetMax=Vector2.zero;var t=o.GetOrAddComponent<TextMeshProUGUI>();t.font=game.koreanFont;t.fontSize=size;t.color=color;t.raycastTarget=false;return t;}
        void BuildUI(){var o=JourneyObjectFactory.Create("ChildrenBlessingCinema",typeof(RectTransform),typeof(Canvas),typeof(UnityEngine.UI.CanvasScaler));o.transform.SetParent(transform,false);canvas=o.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=90;var scaler=o.GetComponent<UnityEngine.UI.CanvasScaler>();scaler.uiScaleMode=UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1440,900);scaler.matchWidthOrHeight=.5f;cinemaPanel=Box(o.transform,"아이들을 향한 축복",new Vector2(.05f,.76f),new Vector2(.78f,.92f),new Color(.08f,.18f,.17f,.88f)).gameObject;caption=Text(cinemaPanel.transform,"축복",new Vector2(.04f,.43f),new Vector2(.96f,.90f),28,new Color(.96f,.9f,.75f));count=Text(cinemaPanel.transform,"안내",new Vector2(.04f,.08f),new Vector2(.96f,.4f),17,new Color(.87f,.73f,.45f));}
        void BuildAudio(){audioSource=gameObject.AddComponent<AudioSource>();audioSource.playOnAwake=false;const int rate=22050;var data=new float[rate/2];for(int i=0;i<data.Length;i++){float t=i/(float)rate;data[i]=(Mathf.Sin(t*2*Mathf.PI*523.25f)+.5f*Mathf.Sin(t*2*Mathf.PI*659.25f))*.16f*Mathf.Exp(-t*8);}chime=AudioClip.Create("ChildBlessingChime_Original",data.Length,1,rate,false);chime.SetData(data,0);}
        public void Shutdown(){if(shutdown)return;shutdown=true;if(pixel)pixel.enabled=oldPixel;if(body)body.enabled=true;if(audioSource)audioSource.Stop();if(chime)Destroy(chime);}
        void OnDestroy(){Shutdown();}
    }
}
