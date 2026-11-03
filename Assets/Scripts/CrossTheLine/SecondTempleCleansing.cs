using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CrossTheLine
{
    public sealed class SecondTempleCleansing : MonoBehaviour
    {
        public enum Phase { Approach, Market, Rebuke, Cleaning, Overturning, Settling, ChildrenReady, ChildrenEntering, LeadersReady, LeadersEntering, Reply, Closing }
        public int Stage {get;private set;}
        public Phase CurrentPhase {get;private set;}
        public int InspectedCount => inspected.Count(x=>x);
        public int ClearedCount => cleared.Count(x=>x);
        public bool IsCinematic => CurrentPhase==Phase.Overturning||CurrentPhase==Phase.Settling||CurrentPhase==Phase.ChildrenEntering||CurrentPhase==Phase.LeadersEntering;
        public bool AmbienceActive => marketAudio&&marketAudio.isPlaying;
        public Transform Player => player;
        public Transform[] Merchants => merchants;
        public Transform[] Children => children;
        public Transform[] Leaders => leaders;
        public Transform[] Tables => tables;
        public Vector3 EntrancePoint => new Vector3(7,-4.8f);
        public Vector3 StallPoint(int i)=>new Vector3(-8.7f+i*4.4f,-4.8f);
        public bool StallCleared(int i)=>cleared[i];
        JourneyGame game;Transform player;SpriteRenderer body,background;
        Material material;Texture2D shadowTexture;Sprite groundShadow;Sprite dot,table,coinTray,silk,cage,sheep,cow,coin,angryPortrait,normalPortrait;
        Sprite[] jesusPoses,birdPoses,work,strides,leaderPoses,childSprites,childMotion,extraChild,landscapes;
        Transform environment;Transform[] tables=new Transform[4],goods=new Transform[4],merchants=new Transform[8],animals=new Transform[2],birds=new Transform[6],coins=new Transform[18],children=new Transform[4],leaders=new Transform[2],disciples=new Transform[3];
        readonly bool[] inspected=new bool[4],cleared=new bool[4];
        const float AdultHeight=2.1f,JesusHeight=2.8f,LeaderHeight=2.35f;
        Vector3 MerchantPlace(int i)=>new Vector3(-6.6f+(i/2)*4.4f+(i%2==0?-.95f:1.1f),i%2==0?-1.75f:-1.2f);
        float ChildHeight(int i)=>i==2?1.35f:1.55f;
        Sprite ChildPose(int i,bool arrived,bool stride)=>i==3?extraChild[arrived?2:stride?1:0]:arrived?childMotion[i*2+1]:stride?childMotion[i*2]:childSprites[i];
        readonly Vector3[] childPlaces={new Vector3(4.2f,-4.5f),new Vector3(1.2f,-4.3f),new Vector3(-1.8f,-4.5f),new Vector3(-4.8f,-4.3f)};
        readonly Vector3[] leaderPlaces={new Vector3(-3.4f,-.25f),new Vector3(-6.4f,-.15f)};
        bool busy,shutdown,oldPixel;int selected=-1;float clock,motion;
        readonly bool[] fleeing=new bool[4];readonly float[] fleeClocks=new float[4];
        Vector3 cameraTarget=new Vector3(0,-.35f,-10);float cameraSize=6.25f;
        UnityEngine.Rendering.Universal.PixelPerfectCamera pixel;
        Canvas canvas;GameObject captionPanel;TextMeshProUGUI caption,subtitle;
        UnityEngine.UI.Image portrait;RectTransform portraitFrame,speakerRect,lineRect;
        AudioSource marketAudio,effects;AudioClip marketClip,woodClip,coinClip,peaceClip;
        public void Initialize(JourneyGame g,Transform p,SpriteRenderer b,Material m,Sprite square,int step)
        {JourneyObjectFactory.BeginStage(this,step);
            game=g;player=p;body=b;material=m;dot=square;BuildGroundShadow();
            var props=JourneySpriteAssets.LoadAll("CrossTheLine/SecondTempleProps");
            Sprite Find(string n)=>props.FirstOrDefault(s=>s.name==n)??throw new Exception("Temple prop missing: "+n);
            table=Find("Table");coinTray=Find("MoneyTray");silk=Find("Silk");cage=Find("Cage");sheep=Find("Sheep");cow=Find("Cow");birdPoses=new[]{Find("DoveUp"),Find("DoveDown")};coin=Find("Coin");
            jesusPoses=JourneySpriteAssets.LoadAll("CrossTheLine/SecondTempleJesus").OrderBy(s=>s.name).ToArray();leaderPoses=JourneySpriteAssets.LoadAll("CrossTheLine/SecondTempleLeaders").OrderBy(s=>s.name).ToArray();if(jesusPoses.Length!=4||leaderPoses.Length!=4)throw new Exception("Temple action and leader poses missing");
            var marketPeople=JourneySpriteAssets.LoadAll("CrossTheLine/SecondTempleMerchantsA").Concat(JourneySpriteAssets.LoadAll("CrossTheLine/SecondTempleMerchantsB")).OrderBy(s=>s.name).ToArray();if(marketPeople.Length!=16)throw new Exception("Eight distinct merchants and walking poses required");work=Enumerable.Range(0,8).Select(i=>marketPeople[i*2]).ToArray();strides=Enumerable.Range(0,8).Select(i=>marketPeople[i*2+1]).ToArray();
            var family=JourneySpriteAssets.LoadAll("CrossTheLine/ChildrenBlessingAtlas");childSprites=Enumerable.Range(0,3).Select(i=>family.First(s=>s.name=="Child"+i)).ToArray();
            childMotion=JourneySpriteAssets.LoadAll("CrossTheLine/SecondTempleChildren").OrderBy(s=>s.name).ToArray();if(childMotion.Length!=6)throw new Exception("Children poses missing");
            extraChild=new[]{"EntryVillagersWork","EntryVillagersWalk","EntryVillagersCheer"}.Select(n=>JourneySpriteAssets.LoadAll("CrossTheLine/"+n).First(s=>s.name=="Resident5")).ToArray();
            angryPortrait=JourneySpriteAssets.Load("CrossTheLine/ChildrenJesusMad");normalPortrait=JourneySpriteAssets.Load("CrossTheLine/ConfessionJesusNormal");
            landscapes=new[]{JourneySpriteAssets.Load("CrossTheLine/SecondTempleExterior"),JourneySpriteAssets.Load("CrossTheLine/SecondTempleCourtyardV2")};if(landscapes.Any(s=>!s))throw new Exception("Temple landscape missing");
            pixel=g.journeyCamera.GetComponent<UnityEngine.Rendering.Universal.PixelPerfectCamera>();if(pixel){oldPixel=pixel.enabled;pixel.enabled=false;}
            environment=JourneyObjectFactory.Create("SecondTempleCourtyard").transform;environment.SetParent(transform,false);
            background=JourneyObjectFactory.Create("성전 배경").GetOrAddComponent<SpriteRenderer>();background.transform.SetParent(environment,false);background.sharedMaterial=m;background.sortingOrder=-30;
            for(int i=0;i<4;i++)
            {
                float x=-6.6f+i*4.4f;tables[i]=Actor("장사판 "+i,table,new Vector3(x,-3.8f),1.45f,35);
                goods[i]=Actor(i==0?"환전상 동전":i==1?"비단 꾸러미":i==2?"제사용 비둘기 새장":"가축 거래용 상",i==0?coinTray:i==1?silk:i==2?cage:coinTray,new Vector3(x,-2.7f),i==2?1.15f:.68f,38);
                merchants[i*2]=Actor("상인 "+i,work[i*2],MerchantPlace(i*2),AdultHeight,45);
                merchants[i*2+1]=Actor("성전 장사 손님 "+i,work[i*2+1],MerchantPlace(i*2+1),AdultHeight,45);
            }
            animals[0]=Actor("제사용 양",sheep,new Vector3(5.6f,1.1f),1.1f,42);animals[1]=Actor("제사용 소",cow,new Vector3(8.9f,1.1f),1.4f,42);
            for(int i=0;i<6;i++)birds[i]=Actor("날아가는 비둘기 "+i,birdPoses[0],new Vector3(2+(i%3)*.2f,-1.6f),.28f,65);
            for(int i=0;i<18;i++)coins[i]=Actor("흩어지는 동전 "+i,coin,Vector3.zero,.16f,40);
            for(int i=0;i<4;i++)children[i]=Actor("성전에 들어오는 아이 "+i,ChildPose(i,false,false),childPlaces[i],ChildHeight(i),55);
            for(int i=0;i<2;i++)leaders[i]=Actor(i==0?"대제사장":"서기관",leaderPoses[i],leaderPlaces[i],LeaderHeight,48);
            for(int i=0;i<3;i++){int index=i==2?4:i;disciples[i]=Actor(JourneyGame.ActorNames[index],g.disciples[index],new Vector3(-9+i*2.4f,-7.5f),2.2f,55);}
            BuildUI();BuildAudio();ConfigureStep(step);
        }
        void BuildGroundShadow()
        {
            shadowTexture=new Texture2D(64,16,TextureFormat.RGBA32,false);
            var colors=new Color[64*16];
            for(int y=0;y<16;y++)for(int x=0;x<64;x++){float dx=(x-31.5f)/31.5f,dy=(y-7.5f)/7.5f;colors[y*64+x]=new Color(1,1,1,Mathf.Clamp01((1-dx*dx-dy*dy)*2));}
            shadowTexture.SetPixels(colors);shadowTexture.Apply();groundShadow=Sprite.Create(shadowTexture,new Rect(0,0,64,16),new Vector2(.5f,.5f),64);
        }
        Transform Actor(string name,Sprite sprite,Vector3 point,float height,int order)
        {
            var root=JourneyObjectFactory.Create(name).transform;root.SetParent(environment,false);root.position=point;
            var r=JourneyObjectFactory.Create("Sprite").GetOrAddComponent<SpriteRenderer>();r.transform.SetParent(root,false);r.sharedMaterial=material;r.sortingOrder=order;SetSprite(r,sprite,height);
            var shade=JourneyObjectFactory.Create("FootShadow").GetOrAddComponent<SpriteRenderer>();shade.transform.SetParent(root,false);shade.sprite=groundShadow;shade.sharedMaterial=material;shade.color=new Color(.27f,.18f,.11f,.16f);shade.sortingOrder=order-1;shade.transform.localScale=new Vector3(height*.55f,.56f,1);if(name.StartsWith("날아가는")||name.StartsWith("흩어지는"))shade.gameObject.SetActive(false);return root;
        }
        SpriteRenderer Render(Transform t)=>t.GetChild(0).GetComponent<SpriteRenderer>();
        void SetSprite(SpriteRenderer r,Sprite sprite,float height){r.sprite=sprite;float s=height/sprite.bounds.size.y;r.transform.localScale=Vector3.one*s;r.transform.localPosition=new Vector3(-JourneySpriteAssets.VisibleBounds(sprite).center.x*s,-JourneySpriteAssets.VisibleBounds(sprite).min.y*s);r.transform.localRotation=Quaternion.identity;}
        void JesusPose(int n){SetSprite(body,jesusPoses[n],JesusHeight*jesusPoses[n].bounds.size.y/jesusPoses.Max(s=>s.bounds.size.y));body.flipX=false;}
        public void ConfigureStep(int step)
        {JourneyObjectFactory.BeginStage(this,step);
            Stage=Mathf.Min(step,7);busy=false;clock=0;selected=-1;captionPanel.SetActive(false);
            CurrentPhase=Stage==0?Phase.Approach:Stage==1?Phase.Market:Stage==2?Phase.Rebuke:Stage==3?Phase.Cleaning:Stage==4?Phase.Settling:Stage==5?Phase.LeadersReady:Stage==6?Phase.Reply:Phase.Closing;
            if(Stage<=1){Array.Clear(inspected,0,4);Array.Clear(cleared,0,4);Array.Clear(fleeing,0,4);Array.Clear(fleeClocks,0,4);}if(Stage>=4)for(int i=0;i<4;i++)cleared[i]=true;
            background.sprite=landscapes[Stage==0?0:1];background.transform.position=JourneyStageLayout.Point(this,background.transform,"Placement 1",new Vector3(0,-.5f));background.transform.localScale=Vector3.one*Mathf.Max(24/background.sprite.bounds.size.x,16.4f/background.sprite.bounds.size.y);background.color=Color.white;
            body.enabled=true;body.maskInteraction=SpriteMaskInteraction.None;body.sortingOrder=60;player.rotation=Quaternion.identity;foreach(var walker in body.GetComponents<DiscipleWalking>())walker.enabled=false;
            player.position=Stage==0?new Vector3(-7,-4.8f):Stage<=3?StallPoint(0):new Vector3(0,-2.1f);if(Stage==2||Stage==3)JesusPose(0);else if(Stage>=4)JesusPose(3);else SetSprite(body,game.jesusFrames[0],JesusHeight);
            for(int i=0;i<4;i++){tables[i].gameObject.SetActive(Stage>=1&&Stage<=4);goods[i].gameObject.SetActive(Stage>=1&&Stage<=3&&!cleared[i]);tables[i].rotation=Quaternion.Euler(0,0,cleared[i]?-75:0);Render(tables[i]).color=Color.white;for(int j=0;j<2;j++)merchants[i*2+j].gameObject.SetActive(Stage>=1&&Stage<=3&&!fleeing[i]&&!cleared[i]);}
            foreach(var a in animals)a.gameObject.SetActive(Stage>=1&&Stage<=3&&!cleared[3]);foreach(var b in birds)b.gameObject.SetActive(false);foreach(var c in coins)c.gameObject.SetActive(false);
            for(int i=0;i<4;i++){children[i].gameObject.SetActive(Stage>=5);children[i].position=childPlaces[i];SetSprite(Render(children[i]),ChildPose(i,true,false),ChildHeight(i));}
            for(int i=0;i<2;i++){leaders[i].gameObject.SetActive(Stage>=6);leaders[i].position=leaderPlaces[i];}
            for(int i=0;i<3;i++)disciples[i].position=JourneyStageLayout.Point(this,disciples[i],"Placement 2",new Vector3(-9+i*2.4f,-7.5f));
            cameraTarget=new Vector3(0,Stage>=4?-2.3f:-1.6f,-10);cameraSize=Stage>=4?6.3f:6.25f;if(Stage==0){game.journeyCamera.transform.position=cameraTarget;game.journeyCamera.orthographicSize=cameraSize;}
            if(Stage==1&&!marketAudio.isPlaying)marketAudio.Play();if(Stage==0||Stage>=5)marketAudio.Stop();if(Stage==4)Peace();
            game.BaptismCinema(IsCinematic);Hud();
        }
        void Hud(){string[] goals={"성전 입구로 들어가기","시장통이 된 성전 살펴보기 · "+InspectedCount+" / 4","기도하는 집을 되찾으라는 말씀","장사판을 엎고 상인들을 내보내기 · "+ClearedCount+" / 4","조용해진 성전으로 들어오는 아이들","대제사장과 서기관의 비꼼","어린아이의 찬양에 관한 예수님의 말씀","기도하는 집이 된 성전"};game.SecondTempleHud(goals[Stage],Stage==0?"D / → · 성전으로    입구에서 E · 들어가기":Stage==1||Stage==3?"A / D · 장사판 앞으로 이동    가까이에서 E · "+(Stage==1?"살펴보기":"장사판 엎기"):"E · 이야기 진행");}
        public void Tick(float dt,bool canMove,bool press)
        {
            if(game.Mode=="menu"||game.Mode=="dialogue"||game.Mode=="chapter_end")return;motion+=dt;clock+=dt;
            if(Stage==3){for(int i=0;i<4;i++)if(fleeing[i])fleeClocks[i]+=dt;FleeMerchants();}
            if(CurrentPhase==Phase.Overturning){Overturn(dt);return;}
            if(CurrentPhase==Phase.Settling){float t=Mathf.Clamp01(clock/1.1f);foreach(var tableRoot in tables)Render(tableRoot).color=new Color(1,1,1,1-t);marketAudio.volume=.10f*(1-t);if(t>=1){foreach(var tRoot in tables)tRoot.gameObject.SetActive(false);marketAudio.Stop();CurrentPhase=Phase.ChildrenReady;game.BaptismCinema(false);Hud();}return;}
            if(CurrentPhase==Phase.ChildrenEntering)
            {
                for(int i=0;i<4;i++){children[i].position=Vector3.MoveTowards(new Vector3(-15-i*2.2f,childPlaces[i].y),childPlaces[i],Mathf.Max(0,clock-i*.18f)*6.5f);SetSprite(Render(children[i]),ChildPose(i,children[i].position==childPlaces[i],(int)(motion*7)%2!=0),ChildHeight(i));Render(children[i]).flipX=children[i].position==childPlaces[i]&&children[i].position.x>player.position.x;}
                if(clock>=3.6f){busy=true;game.MeetingTalk(()=>{busy=false;game.BaptismAdvance();});}return;
            }
            if(CurrentPhase==Phase.LeadersEntering){for(int i=0;i<2;i++){leaders[i].position=Vector3.MoveTowards(new Vector3(-15-i*3,leaderPlaces[i].y),leaderPlaces[i],clock*5.5f);SetSprite(Render(leaders[i]),leaderPoses[leaders[i].position!=leaderPlaces[i]?i+2:i],LeaderHeight);}if(clock>=2.4f){busy=true;game.MeetingTalk(()=>{busy=false;game.BaptismAdvance();});}return;}
            if(!canMove||busy)return;
            if(Stage<=1||Stage==3){var k=Keyboard.current;float x=k==null?0:(k.dKey.isPressed||k.rightArrowKey.isPressed?1:0)-(k.aKey.isPressed||k.leftArrowKey.isPressed?1:0);var p=player.position;p.x=Mathf.Clamp(p.x+x*dt*4,-10,Stage==0?8:5.5f);player.position=p;if(Stage!=3||x!=0){game.MeetingWalk(x!=0,x);SetSprite(body,body.sprite,JesusHeight);}}
            Hud();if(press)Interact();
        }
        void FleeMerchants()
        {
            for(int i=0;i<8;i++)if(merchants[i].gameObject.activeSelf&&fleeing[i/2])
            {
                var start=MerchantPlace(i);var turn=new Vector3(start.x-.35f,1.4f);
                float distance=fleeClocks[i/2]*4.2f;float first=Vector3.Distance(start,turn);
                var p=distance<first?Vector3.MoveTowards(start,turn,distance):turn+Vector3.left*(distance-first);
                merchants[i].position=p;
                bool stride=(int)(fleeClocks[i/2]*8)%2!=0;
                // Both poses are foot-anchored: never add a vertical bob to the body or shadow.
                SetSprite(Render(merchants[i]),stride?strides[i]:work[i],AdultHeight*Mathf.Lerp(1,.88f,Mathf.InverseLerp(start.y,turn.y,p.y)));
                Render(merchants[i]).flipX=stride&&i>=6;
                if(p.x<-14)merchants[i].gameObject.SetActive(false);
            }
        }
        void Overturn(float dt)
        {
            tables[selected].GetChild(1).gameObject.SetActive(false);float t=Mathf.Clamp01((clock-.18f)/.65f);JesusPose(clock<.55f?1:2);tables[selected].rotation=Quaternion.Euler(0,0,-75*Mathf.SmoothStep(0,1,t));
            if(clock>=.3f)goods[selected].gameObject.SetActive(false);
            if(selected==0)for(int i=0;i<18;i++){coins[i].gameObject.SetActive(true);float flight=Mathf.Clamp01((clock-.3f)/.8f);coins[i].position=JourneyStageLayout.Point(this,coins[i],"Placement 3",new Vector3(-6.6f+(i%6-2.5f)*.5f*flight,-2.65f-flight*(1.8f+(i/6)*.2f)+Mathf.Sin(flight*Mathf.PI)*(.4f+(i%3)*.12f)));coins[i].rotation=Quaternion.Euler(0,0,i*19+flight*270);}
            if(selected==2&&clock>=.4f)for(int i=0;i<6;i++){birds[i].gameObject.SetActive(true);birds[i].position=JourneyStageLayout.Point(this,birds[i],"Placement 4",new Vector3(2.2f+(i%3)*.45f-(clock-.4f)*(4.5f+i*.15f),-1.5f+i*.3f+(clock-.4f)*1.4f));SetSprite(Render(birds[i]),birdPoses[(int)(motion*9+i)%2],.42f);}
            if(selected==3&&clock>=.2f)for(int i=0;i<2;i++){var p=new Vector3(i==0?5.6f:8.9f,1.1f);p.x+=4.5f*(clock-.2f);animals[i].position=p;Render(animals[i]).flipX=true;if(p.x>14)animals[i].gameObject.SetActive(false);}
            if(clock>=1.4f&&!merchants[selected*2].gameObject.activeSelf&&!merchants[selected*2+1].gameObject.activeSelf){cleared[selected]=true;selected=-1;captionPanel.SetActive(false);foreach(var c in coins)c.gameObject.SetActive(false);foreach(var b in birds)b.gameObject.SetActive(false);CurrentPhase=Phase.Cleaning;JesusPose(0);game.BaptismCinema(false);Hud();if(ClearedCount==4)game.BaptismAdvance();}
        }
        public void Interact()
        {
            if(busy||IsCinematic)return;
            if(Stage==0&&Mathf.Abs(player.position.x-EntrancePoint.x)>1)return;
            if(Stage==1){for(int i=0;i<4;i++)if(!inspected[i]&&Vector3.Distance(player.position,StallPoint(i))<1.2f){int id=i;busy=true;string[] text={"환전상에는 동전과 제물 값을 흥정하는 말이 가득했습니다.","비단과 옷감을 사고파는 장사판이 기도하는 자리를 차지했습니다.","제사용 비둘기를 파는 새장들이 성전 안을 가득 채웠습니다.","양과 소를 거래하는 소리까지 섞여 성전은 시장통이 되어 있었습니다."};game.SecondTempleNotice(new[]{"내레이션|"+text[id]},()=>{busy=false;inspected[id]=true;Hud();if(InspectedCount==4)game.BaptismAdvance();});return;}return;}
            if(Stage==3){for(int i=0;i<4;i++)if(!cleared[i]&&Vector3.Distance(player.position,StallPoint(i))<1.2f){selected=i;player.position=StallPoint(i);clock=0;CurrentPhase=Phase.Overturning;fleeing[i]=true;fleeClocks[i]=0;caption.text="기도하는 집을 장사판으로 만들지 말라!";subtitle.text="장사판이 뒤집히고, 상인들이 출구로 떠납니다.";captionPanel.SetActive(true);effects.PlayOneShot(woodClip,.65f);if(i==0)effects.PlayOneShot(coinClip,.55f);game.BaptismCinema(true);return;}return;}
            if(Stage==4){CurrentPhase=Phase.ChildrenEntering;clock=0;caption.text="장사 소리가 사라진 성전에 아이들이 들어옵니다.";subtitle.text="호산나! 다윗의 자손께 호산나!";captionPanel.SetActive(true);for(int i=0;i<4;i++){children[i].gameObject.SetActive(true);children[i].position=JourneyStageLayout.Point(this,children[i],"Placement 5",new Vector3(-15-i*2.2f,childPlaces[i].y));}game.BaptismCinema(true);return;}
            if(Stage==5){CurrentPhase=Phase.LeadersEntering;clock=0;for(int i=0;i<2;i++){leaders[i].gameObject.SetActive(true);leaders[i].position=JourneyStageLayout.Point(this,leaders[i],"Placement 6",new Vector3(-15-i*3,leaderPlaces[i].y));}game.BaptismCinema(true);return;}
            busy=true;game.MeetingTalk(()=>{busy=false;game.BaptismAdvance();});
        }
        public void DecorateDialogue(Transform parent,RectTransform speaker,RectTransform line){speakerRect=speaker;lineRect=line;portraitFrame=Box(parent,"예수님 표정 테두리",new Vector2(.025f,.13f),new Vector2(.17f,.93f),new Color(.85f,.74f,.46f));portrait=Box(portraitFrame,"예수님 표정",new Vector2(.04f,.04f),new Vector2(.96f,.96f),Color.white).GetComponent<UnityEngine.UI.Image>();portrait.preserveAspect=true;UpdatePortrait();}
        void UpdatePortrait(){if(!portrait)return;bool show=game.DialogueSpeaker=="예수님";portraitFrame.gameObject.SetActive(show);portrait.sprite=Stage==2||Stage==3?angryPortrait:normalPortrait;speakerRect.anchorMin=new Vector2(show?.2f:.035f,.71f);lineRect.anchorMin=new Vector2(show?.2f:.035f,.2f);}
        void LateUpdate(){if(shutdown||!game)return;canvas.enabled=game.Mode!="menu"&&game.Mode!="dialogue"&&game.Mode!="chapter_end";UpdatePortrait();bool pause=game.Mode=="menu"||game.Mode=="chapter_end";if(pause!=audioPaused){audioPaused=pause;if(pause){marketAudio.Pause();effects.Pause();}else{marketAudio.UnPause();effects.UnPause();}}marketAudio.mute=effects.mute=game.SoundMuted;}
        bool audioPaused;
        public void UpdateCamera(){if(game.Mode=="menu")return;float t=1-Mathf.Exp(-3*Time.unscaledDeltaTime);game.journeyCamera.transform.position=Vector3.Lerp(game.journeyCamera.transform.position,cameraTarget,t);game.journeyCamera.orthographicSize=Mathf.Lerp(game.journeyCamera.orthographicSize,cameraSize,t);}
        RectTransform Box(Transform parent,string name,Vector2 min,Vector2 max,Color color){var o=JourneyObjectFactory.Create(name,typeof(RectTransform),typeof(UnityEngine.UI.Image));o.transform.SetParent(parent,false);var r=o.GetComponent<RectTransform>();r.anchorMin=min;r.anchorMax=max;r.offsetMin=r.offsetMax=Vector2.zero;var image=o.GetComponent<UnityEngine.UI.Image>();image.color=color;image.raycastTarget=false;return r;}
        void BuildUI(){var root=JourneyObjectFactory.Create("SecondTempleCinema",typeof(RectTransform),typeof(Canvas),typeof(UnityEngine.UI.CanvasScaler));root.transform.SetParent(transform,false);canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=90;var scaler=root.GetComponent<UnityEngine.UI.CanvasScaler>();scaler.uiScaleMode=UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1440,900);captionPanel=Box(root.transform,"성전의 변화",new Vector2(.04f,.79f),new Vector2(.73f,.94f),new Color(.08f,.17f,.18f,.88f)).gameObject;caption=Text(captionPanel.transform,"말씀",new Vector2(.04f,.43f),new Vector2(.96f,.93f),25);subtitle=Text(captionPanel.transform,"안내",new Vector2(.04f,.08f),new Vector2(.96f,.4f),17);}
        TextMeshProUGUI Text(Transform parent,string name,Vector2 min,Vector2 max,int size){var o=JourneyObjectFactory.Create(name,typeof(RectTransform));o.transform.SetParent(parent,false);var r=o.GetComponent<RectTransform>();r.anchorMin=min;r.anchorMax=max;r.offsetMin=r.offsetMax=Vector2.zero;var t=o.GetOrAddComponent<TextMeshProUGUI>();t.font=game.koreanFont;t.fontSize=size;t.color=new Color(.96f,.9f,.75f);t.raycastTarget=false;return t;}
        AudioClip Clip(string name,float seconds,Func<float,float> sample){const int rate=22050;var data=new float[(int)(seconds*rate)];for(int i=0;i<data.Length;i++)data[i]=sample(i/(float)rate);var c=AudioClip.Create(name,data.Length,1,rate,false);c.SetData(data,0);return c;}
        void BuildAudio(){marketAudio=gameObject.AddComponent<AudioSource>();marketAudio.loop=true;marketAudio.playOnAwake=false;marketAudio.volume=.10f;effects=gameObject.AddComponent<AudioSource>();effects.playOnAwake=false;marketClip=Clip("TempleMarket_OriginalAmbience",4,t=>(Mathf.Sin(t*440)+Mathf.Sin(t*717)*.5f)*(.12f+.08f*Mathf.Sin(t*3))+(Mathf.PerlinNoise(t*730,3)-.5f)*.15f);woodClip=Clip("TableOverturn_Original",.45f,t=>(Mathf.PerlinNoise(t*4300,1)-.5f)*Mathf.Exp(-t*13)*.8f+Mathf.Sin(t*110)*Mathf.Exp(-t*12)*.2f);coinClip=Clip("CoinsScatter_Original",.8f,t=>(Mathf.Sin(t*6100)+Mathf.Sin(t*8450)*.5f)*Mathf.Exp(-t*7)*.2f);peaceClip=Clip("TemplePeace_OriginalChord",1.8f,t=>(Mathf.Sin(t*2*Mathf.PI*261.63f)+Mathf.Sin(t*2*Mathf.PI*329.63f)*.6f+Mathf.Sin(t*2*Mathf.PI*392)*.4f)*.07f*Mathf.Exp(-t*1.8f));marketAudio.clip=marketClip;}
        void Peace(){effects.PlayOneShot(peaceClip);}
        public void Shutdown(){if(shutdown)return;shutdown=true;if(pixel)pixel.enabled=oldPixel;if(body)body.transform.localRotation=Quaternion.identity;if(marketAudio)marketAudio.Stop();if(effects)effects.Stop();if(groundShadow)Destroy(groundShadow);if(shadowTexture)Destroy(shadowTexture);foreach(var c in new[]{marketClip,woodClip,coinClip,peaceClip})if(c)Destroy(c);}
        void OnDestroy(){Shutdown();}
    }
}
