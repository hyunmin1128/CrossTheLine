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
        [Header("Canvas 화면")]
        public GameObject introduction,fightPanel,resultPanel,completePanel,pausePanel;
        public TMP_Text countText,promptText,biteText,resultText,completeText,timerText;
        public UnityEngine.UI.Image catchFill,netFill;
        public RectTransform zoneRect,fishRect;
        public RectTransform pullingCrew,pullingNet;
        public UnityEngine.UI.Image pullingNetImage;
        public UnityEngine.UI.Image tearLeft,tearRight;
        public UnityEngine.UI.Button startButton,retryButton;
        public NetFishingSession Session { get; private set; }
        public bool Paused { get; private set; }
        public int ActiveSpot { get; private set; }
        public float LastTapTime { get; private set; }=-100;
        Vector3 boatRest,pullRest,netRest;
        NetFishingSession.Phase previous;
        void Start()
        {
            boatRest=boat.position;pullRest=pullingCrew.anchoredPosition;netRest=pullingNet.anchoredPosition;
            startButton.onClick.AddListener(StartGame);retryButton.onClick.AddListener(Restart);
            Restart();
        }
        void OnDestroy(){if(startButton)startButton.onClick.RemoveListener(StartGame);if(retryButton)retryButton.onClick.RemoveListener(Restart);}
        public void Restart()
        {
            Session=new NetFishingSession(settings,randomSeed!=0?randomSeed:System.Environment.TickCount);
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
            return Session.Cast();
        }
        public void Tick(float dt,Vector2 direction,bool cast,bool tap)
        {
            if(Session==null||Paused)return;
            dt=Mathf.Clamp(dt,0,.1f);
            if(Session.State==NetFishingSession.Phase.Exploring)
            {
                var p=boat.position+(Vector3)Vector2.ClampMagnitude(direction,1)*settings.boatSpeed*dt;
                p.x=Mathf.Clamp(p.x,settings.boatBounds.xMin,settings.boatBounds.xMax);p.y=Mathf.Clamp(p.y,settings.boatBounds.yMin,settings.boatBounds.yMax);boat.position=p;
                if(cast)TryCast();
            }
            if(tap&&Session.State==NetFishingSession.Phase.Fighting)LastTapTime=Time.unscaledTime;
            Session.Step(dt,tap);
            if(previous!=Session.State)
            {
                if(Session.State==NetFishingSession.Phase.Exploring&&previous!=NetFishingSession.Phase.Introduction&&fishingSpots.Length>1)
                    ActiveSpot=(ActiveSpot+Random.Range(1,fishingSpots.Length))%fishingSpots.Length;
                previous=Session.State;
            }
            Refresh();
        }
        public void Refresh()
        {
            if(Session==null)return;
            var state=Session.State;bool fighting=state==NetFishingSession.Phase.Fighting;
            if(worldRoot)worldRoot.SetActive(!fighting&&state!=NetFishingSession.Phase.Result&&state!=NetFishingSession.Phase.Replacing&&state!=NetFishingSession.Phase.Complete);
            promptText.gameObject.SetActive(!fighting&&state!=NetFishingSession.Phase.Introduction&&state!=NetFishingSession.Phase.Complete&&!Paused);
            if(tearLeft)tearLeft.gameObject.SetActive(state==NetFishingSession.Phase.Replacing);
            if(tearRight)tearRight.gameObject.SetActive(state==NetFishingSession.Phase.Replacing);
            if(crewBody)crewBody.sprite=state==NetFishingSession.Phase.Casting&&Session.PhaseTime<.35f?crewThrow:crewIdle;
            introduction.SetActive(state==NetFishingSession.Phase.Introduction);
            fightPanel.SetActive(fighting);resultPanel.SetActive(state==NetFishingSession.Phase.Result||state==NetFishingSession.Phase.Replacing);
            completePanel.SetActive(state==NetFishingSession.Phase.Complete);pausePanel.SetActive(Paused);
            countText.text=$"잡은 물고기  {Session.FishCaught} / {settings.goalFish}";
            netFill.fillAmount=Session.Integrity;catchFill.fillAmount=Session.Progress;
            netFill.color=Session.Integrity<.3f?new Color(.88f,.35f,.25f):new Color(.8f,.64f,.35f);
            zoneRect.anchorMin=new Vector2(.08f,Session.Zone-settings.catchZoneHeight*.5f);zoneRect.anchorMax=new Vector2(.92f,Session.Zone+settings.catchZoneHeight*.5f);zoneRect.offsetMin=zoneRect.offsetMax=Vector2.zero;
            zoneRect.GetComponent<UnityEngine.UI.Image>().color=Session.Inside?new Color(.35f,.77f,.58f,.9f):new Color(.45f,.68f,.61f,.65f);
            fishRect.anchorMin=fishRect.anchorMax=new Vector2(.5f,Session.Fish);fishRect.anchoredPosition=Vector2.zero;
            timerText.text=$"남은 시간  {Mathf.CeilToInt(Mathf.Max(0,settings.maximumCatchSeconds-Session.CastElapsed))}초";
            resultText.text=state==NetFishingSession.Phase.Replacing?$"{Session.ResultMessage}\n{Mathf.CeilToInt(Mathf.Max(0,settings.replacementSeconds-Session.PhaseTime))}초 후 다시 던질 수 있습니다\n잡은 물고기는 유지됩니다":$"{Session.ResultMessage}\n누적 {Session.FishCaught} / {settings.goalFish}마리";
            completeText.text=$"150마리의 물고기를 모았습니다\n총 {Mathf.FloorToInt(Session.Elapsed/60)}분 {Mathf.FloorToInt(Session.Elapsed%60)}초\n그물 {Session.Casts}회 · 놓침 {Session.Escapes}회 · 교체 {Session.BrokenNets}회\nR · 다시 시작";
            if(state==NetFishingSession.Phase.Exploring)
            {
                bool near=fishingSpots.Length>0&&Vector2.Distance(boat.position,fishingSpots[ActiveSpot].position)<=settings.castRadius;
                promptText.text=near?"E / Space · 보글거리는 곳에 그물 던지기":"WASD / 방향키 · 보글거리는 지점으로 이동";
            }
            else promptText.text=fighting?"Space / 마우스 클릭 연타 · 초록 구간에 물고기를 유지하세요":state==NetFishingSession.Phase.Waiting?"그물이 물속으로 가라앉았습니다…":state==NetFishingSession.Phase.Casting?"그물 던지는 중":state==NetFishingSession.Phase.Bite?"물고기가 걸렸습니다!":"Esc · 일시정지";
            biteText.text=state==NetFishingSession.Phase.Waiting?new string('.',1+Mathf.FloorToInt(Session.PhaseTime*2)%3):state==NetFishingSession.Phase.Bite?"!":"";
            if(fishingSpots.Length>0)
            {
                var view=sceneCamera.WorldToViewportPoint(fishingSpots[ActiveSpot].position);
                var r=(RectTransform)biteText.transform;r.anchorMin=r.anchorMax=new Vector2(view.x,view.y);r.anchoredPosition=new Vector2(0,38);
            }
            castNet.gameObject.SetActive((state==NetFishingSession.Phase.Casting&&Session.PhaseTime>=.35f)||state==NetFishingSession.Phase.Waiting||state==NetFishingSession.Phase.Bite);
            if(castNet.gameObject.activeSelf&&fishingSpots.Length>0)
            {
                float t=state==NetFishingSession.Phase.Casting?Mathf.Clamp01(Session.PhaseTime/Mathf.Max(.01f,settings.castSeconds)):1;
                castNet.position=Vector3.Lerp(boat.position,fishingSpots[ActiveSpot].position,t)+Vector3.up*Mathf.Sin(t*Mathf.PI)*1.2f;
                castNet.localScale=Vector3.one*Mathf.Lerp(.4f,.9f,t);
                castNet.GetComponent<SpriteRenderer>().color=new Color(1,1,1,state==NetFishingSession.Phase.Waiting?.4f:1);
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
            if(Session.State==NetFishingSession.Phase.Fighting)
            {
                float pull=Mathf.Exp(-Mathf.Max(0,Time.unscaledTime-LastTapTime)*10);
                pullingCrew.anchoredPosition=(Vector2)pullRest+new Vector2(-pull*9,Mathf.Sin(Time.time*4)*2);
                pullingCrew.localRotation=Quaternion.Euler(0,0,pull*2);
                pullingNet.anchoredPosition=(Vector2)netRest+new Vector2(-pull*15,pull*5);
                pullingNetImage.color=Color.Lerp(new Color(.78f,.94f,1),new Color(.95f,.38f,.28f),1-Session.Integrity);
            }
        }
    }
}
