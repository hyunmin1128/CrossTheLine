using UnityEngine;
using UnityEngine.InputSystem;
namespace CrossTheLine
{
    public sealed partial class DesertTemptation
    {
        TemptationScrollSettings scrolling;
        TemptationScrollSegment[] locations;
        SpriteRenderer summitRidge;
        SpriteRenderer skyCoverage;
        SpriteRenderer summitSkyOverhang;
        Rect oldViewport; float oldAspect;
        bool scrollBuilt; int location;
        public Transform Jesus=>jesus;
        public Transform Satan=>satan;
        public int LocationIndex=>location;
        public TemptationScrollSegment[] Locations=>locations;
        void BuildScrollMap()
        {
            if(scrollBuilt)return;
            if(!scrolling.backgroundPrefab)throw new System.InvalidOperationException("Chapter3 panorama prefab is missing");
            var camera=game.journeyCamera;oldViewport=camera.rect;oldAspect=camera.aspect;
            stageObjects=Instantiate(scrolling.editorBackgroundPreview?scrolling.editorBackgroundPreview:scrolling.backgroundPrefab,transform,false);
            stageObjects.name="Chapter03_HorizontalLandscape";stageObjects.SetActive(true);
            locations=stageObjects.GetComponentsInChildren<TemptationScrollSegment>(true);
            System.Array.Sort(locations,(a,b)=>a.location.CompareTo(b.location));
            if(locations.Length!=4)throw new System.InvalidOperationException("Four chapter3 locations required");
            foreach(var l in locations){l.background.sharedMaterial=material;l.background.color=Color.white;}
            var continuous=stageObjects.transform.Find("WildernessContinuousPanorama");
            land=continuous?continuous.GetComponent<SpriteRenderer>():locations[0].background;
            var ridge=stageObjects.transform.Find("SummitRidgeBackground");
            summitRidge=ridge?ridge.GetComponent<SpriteRenderer>():null;
            var sky=stageObjects.transform.Find("SkyCoverage");
            skyCoverage=sky?sky.GetComponent<SpriteRenderer>():null;
            var overhang=stageObjects.transform.Find("SkyOverhang_PlayOnly");
            summitSkyOverhang=overhang?overhang.GetComponent<SpriteRenderer>():null;
            if(overhang)overhang.gameObject.SetActive(true);
            var profile=jesus.GetComponent<JourneyCharacterView>();if(profile&&profile.idle)profile.displayHeight=profile.idle.rect.height/32f;
            scrollBuilt=true;
        }
        void ConfigureScrollStep(int step)
        {
            bool first=!scrollBuilt;BuildScrollMap();
            TrialIndex=Mathf.Clamp(step,0,2);asked=false;clock=0;film.SetActive(false);game.BaptismCinema(false);
            jesusRenderer.transform.localRotation=Quaternion.identity;staff.gameObject.SetActive(false);
            if(first)
            {
                location=step==0?0:TrialIndex+1;jesus.position=locations[location].entrance.position;
                cameraTarget=new Vector3(Mathf.Clamp(jesus.position.x+2,0,90),scrolling.cameraY,-10);
                game.journeyCamera.transform.position=cameraTarget;
            }
            if(step==0&&location==0){CurrentStage=Stage.Walk;satan.gameObject.SetActive(false);ResetWilderness();}
            else if(step>=1)BeginHillAscent(first);
            else TeleportToTrial(TrialIndex+1);
            UpdateScrollCamera(0);
        }
        void SetScrollHud(string title,string hint)=>game.TemptationHud(title,hint);
        void TeleportToTrial(int destination)
        {
            location=destination;
            jesus.position=locations[location].jesusMark.position;
            game.BaptismWalk(false,1);PlaceSatan();AnchorEnemy();
            UpdateScrollCamera(0);RevealTrial();
        }
        void RevealTrial()
        {
            PlaceSatan();AnchorEnemy();satanRenderer.color=new Color(1,1,1,0);
            CurrentStage=Stage.Transition;clock=0;film.SetActive(true);
            caption.text=TrialIndex==0?"광야 · 시험하는 자가 나타납니다.":TrialIndex==1?"언덕의 능선 · 두 번째 시험":"산꼭대기의 공터 · 세 번째 시험";
            game.BaptismWalk(false,1);game.BaptismCinema(true);
        }
        void BeginTravel(int destination)
        {
            CurrentStage=Stage.Travel;asked=false;clock=0;
            satan.gameObject.SetActive(false);staff.gameObject.SetActive(false);film.SetActive(false);game.BaptismCinema(false);
            SetScrollHud(destination==1?"첫 번째 시험 · 광야의 바위터":destination==2?"두 번째 시험 · 성전 꼭대기":"세 번째 시험 · 높은 산","A / D · ← / →  오른쪽 길을 따라 시험 장소로 이동하세요");
            if(location==destination)PlaceSatan();
        }
        void PlaceSatan()
        {
            satan.position=locations[location].satanMark.position;satanRenderer.color=Color.white;
            SetEnemyFrame(TrialIndex==0?1:2);satan.gameObject.SetActive(true);satanRenderer.flipX=false;
        }
        float HorizontalInput()
        {
            var k=Keyboard.current;return k==null?0:(k.dKey.isPressed||k.rightArrowKey.isPressed?1:0)-(k.aKey.isPressed||k.leftArrowKey.isPressed?1:0);
        }
        void MoveScroll(float x,float dt,float right)
        {
            var l=locations[location];var p=jesus.position;
            p.x=Mathf.Clamp(p.x+x*scrolling.speed*(staffCollected&&location==0?scrolling.staffWalkSpeed:1)*dt,l.entrance.position.x,right);p.y=RouteY(p.x);p.z=0;jesus.position=p;
            game.BaptismWalk(x!=0,x);AnchorEnemy();
        }
        void AnchorEnemy()
        {
            if(!satanRenderer.sprite)return;
            var b=JourneySpriteAssets.VisibleBounds(satanRenderer.sprite);
            // The renderer is its own actor: position its opaque feet on the marker.
            satan.position=locations[location].satanMark.position-new Vector3(b.center.x*satan.localScale.x,b.min.y*satan.localScale.y,0);
        }
        void TickScroll(float dt,bool canMove,bool press)
        {
            if(game.Mode=="menu")return;
            if(!canMove&&!IsCinematic)return;
            clock+=dt;if(CurrentStage==Stage.Walk||CurrentStage==Stage.Fasting)HoldStaff();
            if(CurrentStage==Stage.Walk&&canMove)
            {
                float branchX=locations[0].entrance.position.x+3;
                MoveScroll(HorizontalInput(),dt,staffCollected?locations[0].exit.position.x:branchX+.6f);HoldStaff();
                if(!staffCollected&&Mathf.Abs(jesus.position.x-branchX)<1&&press){CurrentStage=Stage.Pickup;clock=0;film.SetActive(true);caption.text="나뭇가지를 주워 지팡이로 삼으셨습니다.";game.BaptismWalk(false,1);game.BaptismCinema(true);}
                else if(staffCollected&&jesus.position.x>=locations[0].jesusMark.position.x)BeginFasting();
            }
            else if(CurrentStage==Stage.Pickup){if(clock>=.8f){staffCollected=true;HoldStaff();film.SetActive(false);game.BaptismCinema(false);CurrentStage=Stage.Walk;SetScrollHud("광야의 길 · 금식 기도","D / →  ·  지팡이를 짚으며 천천히 걸으세요");}}
            else if(CurrentStage==Stage.Fasting)
            {
                TickFasting(dt);
            }
            else if(CurrentStage==Stage.Collapse)TickCollapse();
            else if(CurrentStage==Stage.Ascent&&canMove)TickHillAscent(dt);
            else if(CurrentStage==Stage.Travel&&canMove)
            {
                float input=HorizontalInput();var p=jesus.position;
                p.x=Mathf.Clamp(p.x+input*scrolling.speed*dt,locations[0].entrance.position.x,locations[1].jesusMark.position.x);
                p.y=locations[0].GroundY;p.z=0;jesus.position=p;game.BaptismWalk(input!=0,input);
                int next=p.x>=(locations[0].transform.position.x+locations[1].transform.position.x)*.5f?1:0;
                if(next!=location){location=next;if(location==1){PlaceSatan();AnchorEnemy();}else satan.gameObject.SetActive(false);}
                if(p.x>=locations[1].jesusMark.position.x-.05f)RevealTrial();
            }
            else if(CurrentStage==Stage.Transition)
            {
                satanRenderer.color=new Color(1,1,1,Mathf.SmoothStep(0,1,Mathf.Clamp01(clock/.65f)));
                if(clock>=1.2f){CurrentStage=Stage.Trial;film.SetActive(false);game.BaptismCinema(false);Interact();}
            }
            else if(CurrentStage==Stage.Trial&&canMove){MoveScroll(HorizontalInput(),dt,locations[location].jesusMark.position.x);if(press)Interact();}
            else if(CurrentStage==Stage.Victory)
            {
                satanRenderer.color=new Color(1,1,1,Mathf.Clamp01(1-clock/2));caption.text=clock<2?"사탄아, 물러가라!":"마귀는 떠나고, 천사들이 나아와 섬겼습니다.";
                for(int i=0;i<3;i++)
                {
                    angels[i].gameObject.SetActive(clock>=2);float target=jesus.position.x+2.5f+i*2.3f;
                    angels[i].position=new Vector3(Mathf.Lerp(locations[location].exit.position.x+i*2.3f,target,Mathf.Clamp01((clock-2)/3)),locations[location].GroundAt(Mathf.Lerp(locations[location].exit.position.x+i*2.3f,target,Mathf.Clamp01((clock-2)/3))),0);
                    var r=angels[i].GetComponent<SpriteRenderer>();var b=JourneySpriteAssets.VisibleBounds(r.sprite);angels[i].position-=new Vector3(b.center.x*angels[i].localScale.x,b.min.y*angels[i].localScale.y,0);
                }
                if(clock>=6){VictoryFinished=true;CurrentStage=Stage.Complete;film.SetActive(false);game.BaptismCinema(false);game.BaptismAdvance();}
            }
        }
        void UpdateScrollCamera(float dt)
        {
            var c=game.journeyCamera;float aspect=Screen.height>0?(float)Screen.width/Screen.height:16f/9;
            const float targetAspect=16f/9;
            c.rect=aspect>targetAspect?new Rect((1-targetAspect/aspect)*.5f,0,targetAspect/aspect,1):new Rect(0,(1-aspect/targetAspect)*.5f,1,aspect/targetAspect);
            c.aspect=targetAspect;c.orthographicSize=JordanScrollSettings.CameraSize;
            float finalX=locations[3].transform.position.x;
            float target=Mathf.Clamp(jesus.position.x+2,0,finalX);
            if(location==3&&CurrentStage!=Stage.Ascent){target=finalX;dt=0;}
            
            
            float x=dt<=0?target:Mathf.Lerp(c.transform.position.x,target,1-Mathf.Exp(-scrolling.followSharpness*dt));
            float rise=locations[3].background.transform.position.y-scrolling.cameraY;
            float climb=Mathf.SmoothStep(0,1,Mathf.InverseLerp(locations[2].transform.position.x,finalX,x));
            c.transform.position=new Vector3(Mathf.Round(x*32)/32,Mathf.Round((scrolling.cameraY+rise*climb)*32)/32,-10);
            if(skyCoverage){var skyPosition=skyCoverage.transform.position;skyPosition.y=c.transform.position.y;skyCoverage.transform.position=skyPosition;}
            if(summitRidge)
            {
                float reveal=Mathf.SmoothStep(0,1,Mathf.InverseLerp(locations[3].entrance.position.x+8,locations[3].jesusMark.position.x,jesus.position.x));
                summitRidge.color=new Color(1,1,1,1-reveal);
                if(summitSkyOverhang)summitSkyOverhang.color=summitRidge.color;
            }
        }
    }
}




