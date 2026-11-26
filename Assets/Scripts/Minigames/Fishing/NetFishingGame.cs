using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CrossTheLine.Minigames.Fishing
{
    public sealed class NetFishingGame : MonoBehaviour
    {
        public NetFishingSettings settings;
        public int randomSeed;
        [Header("바다와 직접 편집할 오브젝트")]
        public Transform boat,castNet;
        public Transform[] fishingSpots;
        public SpriteRenderer[] spotBubbles;
        public Camera sceneCamera;
        public GameObject worldRoot;
        public SpriteRenderer crewBody;
        public Sprite crewIdle,crewThrow;
        public Sprite[] throwingFrames,bubbleFrames;
        public Sprite submergedNet,legendFish;
        public TMP_Text fishWarningText;
        public bool FacingLeft { get; private set; }
        Vector3 castLanding;
        [Header("Canvas 화면")]
        public GameObject introduction,fightPanel,resultPanel,completePanel,pausePanel;
        public TMP_Text countText,promptText,biteText,resultText,completeText,timerText;
        public UnityEngine.UI.Image catchFill,netFill;
        public RectTransform zoneRect,fishRect;
        [Header("무작위 물고기")]
        [Tooltip("한 번 던질 때마다 같은 확률로 하나를 선택합니다. 비어 있는 항목은 제외합니다.")]
        public Sprite[] fishVariants;
        public UnityEngine.UI.Image resultFishImage;
        public Sprite CurrentFish { get; private set; }
        public int CurrentFishIndex { get; private set; }=-1;
        public RectTransform pullingCrew,pullingNet;
        [Tooltip("Aseprite Pull Net frames in playback order.")]
        public Sprite[] pullingFrames;
        [Min(1)] public float pullingFramesPerSecond=10;
        public UnityEngine.UI.Image pullingNetImage;
        public UnityEngine.UI.Image tearLeft,tearRight;
        public UnityEngine.UI.Button startButton,retryButton;
        public NetFishingSession Session { get; private set; }
        public bool Paused { get; private set; }
        public int ActiveSpot { get; private set; }
        public float LastTapTime { get; private set; }=-100;
        Vector3 boatRest,pullRest,netRest;
        UnityEngine.UI.Image pullingCrewImage;
        System.Random fishRandom;
        NetFishingSession.Phase previous;
        void Start()
        {
            boatRest=boat.position;pullRest=pullingCrew.anchoredPosition;netRest=pullingNet.anchoredPosition;
            pullingCrewImage=pullingCrew.GetComponent<UnityEngine.UI.Image>();
            startButton.onClick.AddListener(StartGame);retryButton.onClick.AddListener(Restart);
            Restart();
        }
        void OnDestroy(){if(startButton)startButton.onClick.RemoveListener(StartGame);if(retryButton)retryButton.onClick.RemoveListener(Restart);}
        public void Restart()
        {
            int seed=randomSeed!=0?randomSeed:System.Environment.TickCount;
            Session=new NetFishingSession(settings,seed);
            // Cosmetic draws must not change fish movement, rewards or the existing balance RNG.
            fishRandom=new System.Random(seed^0x278FA);
            CurrentFish=null;CurrentFishIndex=-1;FacingLeft=false;
            Paused=false;ActiveSpot=0;boat.position=boatRest;previous=Session.State;Refresh();
        }
        public void StartGame(){Session.Start();Refresh();}
        public void TogglePause(){if(Session==null||Session.State==NetFishingSession.Phase.Introduction||Session.State==NetFishingSession.Phase.Complete)return;Paused=!Paused;Refresh();}
        void Update()
        {
            if(Session==null)return;
            var k=Keyboard.current;var m=Mouse.current;
            if(k!=null&&k.escapeKey.wasPressedThisFrame)TogglePause();
            if(Paused)return;
            if(Session.State==NetFishingSession.Phase.Introduction){if(k!=null&&k.enterKey.wasPressedThisFrame)StartGame();return;}
            if(Session.State==NetFishingSession.Phase.Complete){if(k!=null&&k.rKey.wasPressedThisFrame)Restart();return;}
            Vector2 direction=Vector2.zero;
            if(k!=null){direction.x=(k.dKey.isPressed||k.rightArrowKey.isPressed?1:0)-(k.aKey.isPressed||k.leftArrowKey.isPressed?1:0);direction.y=(k.wKey.isPressed||k.upArrowKey.isPressed?1:0)-(k.sKey.isPressed||k.downArrowKey.isPressed?1:0);}
            bool tap=(k!=null&&k.spaceKey.wasPressedThisFrame)||(m!=null&&m.leftButton.wasPressedThisFrame);
            bool cast=(k!=null&&(k.eKey.wasPressedThisFrame||k.spaceKey.wasPressedThisFrame));
            Tick(Time.deltaTime,direction,cast,tap);
        }
        public bool TryCast()
        {
            if(Session==null||Session.State!=NetFishingSession.Phase.Exploring||fishingSpots.Length==0)return false;
            if(Vector2.Distance(boat.position,fishingSpots[ActiveSpot].position)>settings.castRadius)return false;
            bool legend=legendFish&&fishRandom.NextDouble()<settings.legendChance;
            SelectFish(legend);
            if(!Session.Cast(legend,CurrentFishIndex))return false;
            castLanding=GetCastLanding();
            return true;
        }
        Vector3 GetCastLanding()
        {
            Vector3 spot=fishingSpots[ActiveSpot].position;
            float side=FacingLeft?-1f:1f;
            float edge=Mathf.Max(.1f,sceneCamera.orthographicSize*sceneCamera.aspect-.65f);
            float x=boat.position.x+side*Mathf.Max(1.35f,Mathf.Abs(spot.x-boat.position.x));
            return new Vector3(Mathf.Clamp(x,-edge,edge),spot.y-.8f,0);
        }
        void SelectFish(bool legend)
        {
            if(legend)
            {
                CurrentFish=legendFish;
                CurrentFishIndex=fishVariants?.Length??0;
                ShowSelectedFish();
                return;
            }
            if(fishVariants==null)return;
            int count=0;foreach(var sprite in fishVariants)if(sprite)count++;
            if(count==0)return;
            int chosen=fishRandom.Next(count);
            for(int i=0;i<fishVariants.Length;i++)if(fishVariants[i]&&chosen--==0){CurrentFish=fishVariants[i];CurrentFishIndex=i;break;}
            ShowSelectedFish();
        }
        void ShowSelectedFish()
        {
            if(!CurrentFish)return;
            var icon=fishRect.GetComponent<UnityEngine.UI.Image>();
            icon.sprite=CurrentFish;icon.preserveAspect=true;
            if(resultFishImage){resultFishImage.sprite=CurrentFish;resultFishImage.preserveAspect=true;}
        }
        public void Tick(float dt,Vector2 direction,bool cast,bool tap)
        {
            if(Session==null||Paused)return;
            dt=Mathf.Clamp(dt,0,.1f);
            if(Session.State==NetFishingSession.Phase.Exploring)
            {
                if(Mathf.Abs(direction.x)>.01f)FacingLeft=direction.x<0;
                var p=boat.position+(Vector3)Vector2.ClampMagnitude(direction,1)*settings.boatSpeed*dt;
                p.x=Mathf.Clamp(p.x,settings.boatBounds.xMin,settings.boatBounds.xMax);p.y=Mathf.Clamp(p.y,settings.boatBounds.yMin,settings.boatBounds.yMax);boat.position=p;
                if(cast)TryCast();
            }
            if(tap&&Session.State==NetFishingSession.Phase.Fighting)LastTapTime=Time.unscaledTime;
            Session.Step(dt,tap);
            if(previous!=Session.State)
            {
                if(Session.State==NetFishingSession.Phase.Exploring&&previous!=NetFishingSession.Phase.Introduction&&fishingSpots.Length>1)
                    ChooseNextSpot();
                previous=Session.State;
            }
            Refresh();
        }
        void ChooseNextSpot()
        {
            // Move the school across the map at 50 and 100 fish.
            float targetX=Session.JourneyLeg==1?-2.5f:Session.JourneyLeg==2?0:2.5f;
            float best=float.PositiveInfinity;
            int next=ActiveSpot;
            for(int i=0;i<fishingSpots.Length;i++)
            {
                if(i==ActiveSpot)continue;
                float score=Mathf.Abs(fishingSpots[i].position.x-targetX)+Random.Range(0f,1.5f);
                if(score<best){best=score;next=i;}
            }
            ActiveSpot=next;
        }
        public void Refresh()
        {
            if(Session==null)return;
            var state=Session.State;bool fighting=state==NetFishingSession.Phase.Fighting;
            if(resultFishImage)resultFishImage.gameObject.SetActive(state==NetFishingSession.Phase.Result&&Session.LastCatch>0&&CurrentFish);
            if(worldRoot)worldRoot.SetActive(!fighting&&state!=NetFishingSession.Phase.Result&&state!=NetFishingSession.Phase.Replacing&&state!=NetFishingSession.Phase.Complete);
            promptText.gameObject.SetActive(!fighting&&state!=NetFishingSession.Phase.Introduction&&state!=NetFishingSession.Phase.Complete&&!Paused);
            if(tearLeft)tearLeft.gameObject.SetActive(state==NetFishingSession.Phase.Replacing);
            if(tearRight)tearRight.gameObject.SetActive(state==NetFishingSession.Phase.Replacing);
            if(crewBody)
            {
                crewBody.flipX=FacingLeft;
                crewBody.sprite=crewIdle;
                if(state==NetFishingSession.Phase.Casting)
                {
                    int count=throwingFrames?.Length??0;
                    if(count>0)
                    {
                        int frame=Mathf.Min(count-1,Mathf.FloorToInt(Session.PhaseTime/Mathf.Max(.01f,settings.castSeconds)*count));
                        crewBody.sprite=throwingFrames[frame];
                    }
                    else if(Session.PhaseTime<.35f)crewBody.sprite=crewThrow;
                }
            }
            introduction.SetActive(state==NetFishingSession.Phase.Introduction);
            fightPanel.SetActive(fighting);resultPanel.SetActive(state==NetFishingSession.Phase.Result||state==NetFishingSession.Phase.Replacing);
            completePanel.SetActive(state==NetFishingSession.Phase.Complete);pausePanel.SetActive(Paused);
            countText.text=$"Fish Caught: {Session.FishCaught} / {settings.goalFish}";
            netFill.fillAmount=Session.Integrity;catchFill.fillAmount=Session.Progress/Session.ProgressRequired;
            netFill.color=Session.Integrity<.3f?new Color(.88f,.35f,.25f):new Color(.8f,.64f,.35f);
            zoneRect.anchorMin=new Vector2(.08f,Session.Zone-Session.ZoneHeight*.5f);zoneRect.anchorMax=new Vector2(.92f,Session.Zone+Session.ZoneHeight*.5f);zoneRect.offsetMin=zoneRect.offsetMax=Vector2.zero;
            zoneRect.GetComponent<UnityEngine.UI.Image>().color=Session.Inside?new Color(.35f,.77f,.58f,.9f):new Color(.45f,.68f,.61f,.65f);
            fishRect.anchorMin=fishRect.anchorMax=new Vector2(.5f,Session.Fish);fishRect.anchoredPosition=Vector2.zero;
            bool danger=fighting&&Session.Integrity<=.33f;
            if(fishWarningText)
            {
                fishWarningText.gameObject.SetActive(fighting&&(danger||Session.LegendWarning));
                fishWarningText.text=Session.LegendWarning?"Dash!":"Net at Risk";
            }
            if(fighting&&Session.LegendWarning)
                fishRect.anchoredPosition=new Vector2(Mathf.Sin(Session.FightTime*72)*4,0);
            timerText.text=$"Time Left: {Mathf.CeilToInt(Mathf.Max(0,settings.maximumCatchSeconds-Session.CastElapsed))}s";
            resultText.text=state==NetFishingSession.Phase.Replacing?$"{Session.ResultMessage}\n{Mathf.CeilToInt(Mathf.Max(0,settings.replacementSeconds-Session.PhaseTime))}s until you can cast again\nYour catch is saved":$"{Session.ResultMessage}\nFish Caught: {Session.FishCaught} / {settings.goalFish}";
            if(state==NetFishingSession.Phase.Result&&Session.ReachedMilestone)
                resultText.text+=Session.JourneyLeg==2?"\n50 fish caught! The school moves to the center.":"\n100 fish caught! The school moves to the right.";
            completeText.text=$"Fish Caught: {Session.FishCaught} / {settings.goalFish}\nTotal Time: {Mathf.FloorToInt(Session.Elapsed/60)}m {Mathf.FloorToInt(Session.Elapsed%60)}s\nCasts: {Session.Casts} · Escapes: {Session.Escapes} · Nets Replaced: {Session.BrokenNets}\nR · Restart";
            if(state==NetFishingSession.Phase.Exploring)
            {
                bool near=fishingSpots.Length>0&&Vector2.Distance(boat.position,fishingSpots[ActiveSpot].position)<=settings.castRadius;
                promptText.text=near?"E / Space · Cast your net at the bubbles":"WASD / Arrow Keys · Move toward the bubbles";
            }
            else promptText.text=fighting?"Space / Click repeatedly · Keep the fish in the green zone":state==NetFishingSession.Phase.Waiting?"The net is underwater…":state==NetFishingSession.Phase.Casting?"Casting the net…":state==NetFishingSession.Phase.Bite?"Fish on!":"Esc · Pause";
            bool preview=state==NetFishingSession.Phase.Exploring&&fishingSpots.Length>0&&Vector2.Distance(boat.position,fishingSpots[ActiveSpot].position)<=settings.castRadius;
            biteText.text=preview?"◎":state==NetFishingSession.Phase.Waiting?new string('.',1+Mathf.FloorToInt(Session.PhaseTime*2)%3):state==NetFishingSession.Phase.Bite?"!":"";
            if(fishingSpots.Length>0)
            {
                var target=preview?GetCastLanding():state==NetFishingSession.Phase.Waiting||state==NetFishingSession.Phase.Bite?castLanding:fishingSpots[ActiveSpot].position;
                var view=sceneCamera.WorldToViewportPoint(target);
                var r=(RectTransform)biteText.transform;r.anchorMin=r.anchorMax=new Vector2(view.x,view.y);r.anchoredPosition=new Vector2(0,preview?0:38);
            }
            castNet.gameObject.SetActive(state==NetFishingSession.Phase.Waiting||state==NetFishingSession.Phase.Bite);
            if(castNet.gameObject.activeSelf&&fishingSpots.Length>0)
            {
                float sink=state==NetFishingSession.Phase.Bite?1:Mathf.Clamp01(Session.PhaseTime/.65f);
                castNet.position=castLanding+Vector3.up*(.55f*(1-sink));
                var renderer=castNet.GetComponent<SpriteRenderer>();
                if(submergedNet)renderer.sprite=submergedNet;
                if(crewBody)renderer.sortingOrder=crewBody.sortingOrder-2;
                renderer.flipX=FacingLeft;
                renderer.color=Color.Lerp(Color.white,new Color(.55f,.78f,1,.67f),sink);
            }
        }
        void LateUpdate()
        {
            if(Session==null)return;
            if(Session.State==NetFishingSession.Phase.Replacing&&tearLeft&&tearRight)
            {
                float t=Mathf.Clamp01(Session.PhaseTime/1.2f);
                tearLeft.rectTransform.anchoredPosition=new Vector2(-35*t,0);tearRight.rectTransform.anchoredPosition=new Vector2(35*t,0);
                tearLeft.rectTransform.localRotation=Quaternion.Euler(0,0,t*14);tearRight.rectTransform.localRotation=Quaternion.Euler(0,0,-t*14);
                tearLeft.color=tearRight.color=new Color(1,.65f,.5f,1-t);
            }
            for(int i=0;i<spotBubbles.Length;i++)if(spotBubbles[i]){bool active=i==ActiveSpot&&Session.State!=NetFishingSession.Phase.Complete;spotBubbles[i].gameObject.SetActive(active);if(active){float wave=Mathf.Sin(Time.time*3)*.12f;spotBubbles[i].transform.localScale=Vector3.one*(1+wave);spotBubbles[i].color=new Color(.75f,.96f,1,.7f+wave);}}
            if(bubbleFrames!=null&&bubbleFrames.Length>0&&ActiveSpot<spotBubbles.Length&&spotBubbles[ActiveSpot])
                spotBubbles[ActiveSpot].sprite=bubbleFrames[Mathf.FloorToInt(Time.time*9)%bubbleFrames.Length];
            if(Session.State==NetFishingSession.Phase.Fighting)
            {
                if(pullingCrewImage&&pullingFrames!=null&&pullingFrames.Length>0)
                {
                    int frame=Mathf.FloorToInt(Session.PhaseTime*Mathf.Max(1,pullingFramesPerSecond))%pullingFrames.Length;
                    pullingCrewImage.sprite=pullingFrames[frame];
                    pullingCrew.anchoredPosition=(Vector2)pullRest;
                    pullingCrew.localRotation=Quaternion.identity;
                    pullingNet.gameObject.SetActive(false);
                }
                else
                {
                    float pull=Mathf.Exp(-Mathf.Max(0,Time.unscaledTime-LastTapTime)*10);
                    pullingCrew.anchoredPosition=(Vector2)pullRest+new Vector2(-pull*9,Mathf.Sin(Time.time*4)*2);
                    pullingCrew.localRotation=Quaternion.Euler(0,0,pull*2);
                    pullingNet.anchoredPosition=(Vector2)netRest+new Vector2(-pull*15,pull*5);
                    pullingNetImage.color=Color.Lerp(new Color(.78f,.94f,1),new Color(.95f,.38f,.28f),1-Session.Integrity);
                }
                if(Session.Integrity<=.33f&&!Paused)
                    pullingCrew.anchoredPosition+=new Vector2(Mathf.Sin(Session.FightTime*65)*3,0);
            }
        }
    }
}
