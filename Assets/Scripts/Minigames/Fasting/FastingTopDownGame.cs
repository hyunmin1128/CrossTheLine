using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
namespace CrossTheLine.Minigames
{
    public sealed class FastingTopDownGame : MonoBehaviour
    {
        public FastingTopDownSettings settings;
        public Transform player,world,hazardRoot,gate;
        public Camera gameCamera;
        public SpriteRenderer playerBody,feet;
        public Sprite idle,prayingSprite;
        public FastingTopDownEntity[] entities;
        public FastingTopDownHazard hazardPrefab;
        public SpriteRenderer[] backgrounds,laneGuides;
        public TMP_Text heading,status,items,help,result;
        public UnityEngine.UI.Image prayerFill,transition;
        public UnityEngine.UI.Image[] hearts,visionPanels;
        public GameObject introduction,resultPanel;
        public UnityEngine.UI.Button startButton,retryButton;
        public int StageIndex{get;private set;}
        public float Health{get;private set;}
        public float Prayer{get;private set;} public float PrayerTarget=>settings.GetPrayerTarget(StageIndex);
        public float Elapsed{get;private set;}
        public bool Running{get;private set;}
        public bool Complete{get;private set;}
        public bool Shielded{get;private set;}
        public bool HasWater{get;private set;}
        public bool Sheltered=>FindCover(player.position)!=null; public bool WindSheltered=>Array.Exists(entities,e=>e&&e.gameObject.activeSelf&&e.ProtectsWind(player.position));
        public string Message{get;private set;}="";
        public bool Praying{get;private set;}
        public float SlowRemaining=>slow; public float PrayerBlockRemaining=>Mathf.Max(prayerLock,root);
        public float VisionRemaining=>vision;
        public IReadOnlyList<FastingTopDownHazard> Hazards=>hazards;
        readonly List<FastingTopDownHazard> hazards=new List<FastingTopDownHazard>();
        float wave,grace,slow,root,prayerLock,vision,buff,fever,swap,waterGrace,restart,waterNotice;
        int flames,waveNumber;
        bool failed; float waterBoost,reverse; bool gateArmed; Vector3[] entityStarts,backgroundStarts; public bool GateVisible=>gateArmed;
        void Start(){CacheMapStarts();Application.runInBackground=true;JourneyRuntimeFonts.Apply(gameObject.scene);startButton.onClick.AddListener(Begin);retryButton.onClick.AddListener(Begin);ResetState();introduction.SetActive(true);Running=false;Refresh();}
        void OnDestroy(){if(startButton)startButton.onClick.RemoveListener(Begin);if(retryButton)retryButton.onClick.RemoveListener(Begin);}
        public void Begin(){ResetState();introduction.SetActive(false);Running=true;Refresh();}
        void ClearHazards(){foreach(var h in hazards)if(h)Destroy(h.gameObject);hazards.Clear();}
        void ResetState(){ClearHazards();StageIndex=0;Health=settings.maximumHealth;Prayer=0;Elapsed=0;Shielded=false;HasWater=true;Complete=false;failed=false;Praying=false;restart=0;flames=0;waveNumber=0;buff=fever=grace=slow=root=prayerLock=vision=swap=waterGrace=0;waterBoost=reverse=0;wave=2;Message="자동 전진 / 자동 기도 · 좌우 한 칸 회피";resultPanel.SetActive(false);player.position=Vector3.zero;ApplyStage(true);}
        void Update(){var k=Keyboard.current;if(k!=null&&k.rKey.wasPressedThisFrame){Begin();return;}if(!Running){if(introduction.activeSelf&&k!=null&&k.enterKey.wasPressedThisFrame)Begin();return;}
            Vector2 direction=Vector2.zero;if(k!=null){direction.x=(k.dKey.wasPressedThisFrame||k.rightArrowKey.wasPressedThisFrame?1:0)-(k.aKey.wasPressedThisFrame||k.leftArrowKey.wasPressedThisFrame?1:0);if(k.eKey.wasPressedThisFrame)UseWater();}Tick(Time.deltaTime,direction);}
        FastingTopDownEntity FindCover(Vector2 p){foreach(var e in entities)if(e&&e.gameObject.activeSelf&&e.Protects(p))return e;return null;}
        public bool CanMove(Vector2 p){if(Mathf.Abs(p.x)>6.7f||p.y<0)return false;foreach(var e in entities)if(e&&e.gameObject.activeSelf&&e.kind==FastingTopDownEntity.Kind.Cover&&e.Contains(p))return false;return true;}
        public void Tick(float dt,Vector2 direction,bool legacyPrayerInput=false)
        {
            if(!Running)return;dt=Mathf.Clamp(dt,0,.1f);Elapsed+=dt;
            if(failed){restart-=dt;if(restart<=0)Begin();return;}
            if(swap>0){swap-=dt;transition.color=new Color(.04f,.03f,.04f,Mathf.Sin(Mathf.Clamp01(swap/.6f)*Mathf.PI));if(swap<=0){StageIndex++;Health=Mathf.Min(settings.maximumHealth,Health+1);Prayer=0;player.position=Vector3.zero;ApplyStage(false);}Refresh();return;}
            grace=Mathf.Max(0,grace-dt);slow=Mathf.Max(0,slow-dt);root=Mathf.Max(0,root-dt);prayerLock=Mathf.Max(0,prayerLock-dt);vision=Mathf.Max(0,vision-dt);buff=Mathf.Max(0,buff-dt);fever=Mathf.Max(0,fever-dt);waterGrace=Mathf.Max(0,waterGrace-dt);waterNotice=Mathf.Max(0,waterNotice-dt);waterBoost=Mathf.Max(0,waterBoost-dt);reverse=Mathf.Max(0,reverse-dt);if(reverse>0)direction=-direction;
            Vector2 from=player.position;
            Praying=Prayer<PrayerTarget&&PrayerBlockRemaining<=0;
            if(Praying)Prayer=Mathf.Min(PrayerTarget,Prayer+settings.prayerPerSecond*(buff>0?1.5f:1)*(fever>0?2:1)*dt);
            if(root<=0&&direction.x!=0)
            {
                int lane=Mathf.Clamp(Mathf.RoundToInt(player.position.x/settings.laneSpacing)+2,0,4);
                int next=lane+(direction.x>0?1:-1);
                if(next<0||next>4)Message="이동 불가 · 가장자리 길입니다";
                else {Vector2 target=player.position;target.x=(next-2)*settings.laneSpacing;if(CanMove(target)){player.position=target;Message="좌우 한 칸 이동";}else Message="이동 불가 · 해당 길에 바위가 있습니다";}
            }
            if(root<=0)
            {
                Vector2 target=player.position;target.y+=settings.moveSpeed*(slow>0?.55f:1)*(waterBoost>0?1.3f:1)*dt;
                if(CanMove(target))player.position=target;else Message="전진 막힘 · 좌우 키로 옆길을 선택하세요";
            }
            playerBody.color=grace>0?new Color(1,.75f,.65f):Color.white;
            Vector2 to=player.position;
            foreach(var e in entities){if(!e||!e.gameObject.activeSelf)continue;if(e.kind==FastingTopDownEntity.Kind.Cover){if(e.marker)e.marker.color=e.Protects(to)?new Color(.2f,1,.55f,.5f):new Color(.1f,.8f,.85f,.35f);if(e.windMarker)e.windMarker.color=e.ProtectsWind(to)?new Color(.2f,1,.55f,.5f):new Color(.1f,.8f,.85f,.35f);continue;}if(e.consumed||!e.Contains(to))continue;
                if(e.kind==FastingTopDownEntity.Kind.Water){if(StageIndex<3){HasWater=true;waterGrace=.15f;if(waterNotice<=0){Message="물 보급 · 수통 충전 / 이곳에 머무는 동안 보호";waterNotice=2;}}}
                else Collect(e);
            }
            for(int i=hazards.Count-1;i>=0;i--){var h=hazards[i];if(!h){hazards.RemoveAt(i);continue;}if(h.Advance(dt,from,to,out var entry)){var impact=Vector2.Lerp(from,to,entry);bool cover=h.kind==FastingTopDownHazard.Kind.Sand?Array.Exists(entities,e=>e&&e.gameObject.activeSelf&&e.ProtectsWind(impact)):FindCover(impact)!=null&&(h.kind==FastingTopDownHazard.Kind.Bat||h.kind==FastingTopDownHazard.Kind.Rock);if(!cover){bool shieldBefore=Shielded;bool damaged=TryDamage(h.kind.ToString());if(damaged||shieldBefore&&!Shielded){h.spent=true;h.gameObject.SetActive(false);if(damaged)ApplyEffect(h.kind);}}}if(h.spent){Destroy(h.gameObject);hazards.RemoveAt(i);}}
            wave-=dt;if(wave<=0){SpawnWave();wave=settings.waveIntervals[Mathf.Clamp(StageIndex,0,3)];}
            if(StageIndex>0&&waveNumber>0&&waveNumber%4==0&&vision<=0){vision=settings.visionSeconds;waveNumber++;}
            ArmGateIfReady();RecycleMap();
            if(gateArmed&&player.position.y>=gate.position.y-.05f){if(Prayer>=PrayerTarget){if(StageIndex==3){Complete=true;Running=false;resultPanel.SetActive(true);result.text=$"40일 기도 완료\n기록 {Elapsed:0.0}초\nR 또는 다시 테스트";}else{swap=.6f;ClearHazards();Message="기도 완료 · 다음 장소로 이동";}}}
            Follow();Refresh();
        }
        public bool TryDamage(string cause){if(grace>0||waterGrace>0||failed)return false;if(Shielded){Shielded=false;grace=settings.hitGrace;Message="믿음의 방패 소모 · 피해와 상태이상 방어";return false;}Health=Mathf.Max(0,Health-settings.damage);grace=settings.hitGrace;Message=$"피해 -{settings.damage:0.#}칸 · {cause}";if(Health<=0){failed=true;restart=1.8f;Message="지침 소진 · 잠시 후 1일부터 다시 시작";}return true;}
        void ApplyEffect(FastingTopDownHazard.Kind k){if(k==FastingTopDownHazard.Kind.Sand||k==FastingTopDownHazard.Kind.Bat||k==FastingTopDownHazard.Kind.Thorns)slow=1.2f;if(k==FastingTopDownHazard.Kind.Quicksand||k==FastingTopDownHazard.Kind.Reaper)root=.4f;if(k==FastingTopDownHazard.Kind.Crack)root=.3f;if(k==FastingTopDownHazard.Kind.Fire||k==FastingTopDownHazard.Kind.Pillar||k==FastingTopDownHazard.Kind.Tomb)prayerLock=1.5f;if(k==FastingTopDownHazard.Kind.Bat||k==FastingTopDownHazard.Kind.Reaper)vision=settings.visionSeconds;if(k==FastingTopDownHazard.Kind.Poison){reverse=1.2f;Message="오염된 물 · 방향 반전 1.2초 / E 수통으로 정화";}}
        public void UseWater(){if(!HasWater||!Running||failed)return;HasWater=false;slow=root=prayerLock=vision=reverse=0;waterBoost=3;Health=Mathf.Min(settings.maximumHealth,Health+1);grace=.5f;Message="수통 사용 · 정화 / 지침 +1 / 이동 가속 3초";}
        public void Collect(FastingTopDownEntity e){switch(e.kind){case FastingTopDownEntity.Kind.Shield:Shielded=true;Message="믿음의 방패 · 다음 충돌 자동 방어";break;case FastingTopDownEntity.Kind.Flame:buff=8;flames++;if(flames%3==0)fever=5;Message=fever>0?"불꽃 3개 · 기도 피버 5초":"성령의 불꽃 · 기도 속도 증가";break;default:float amount=PrayerTarget*(e.kind==FastingTopDownEntity.Kind.Book?1f:settings.ScriptureFraction(StageIndex,e.kind==FastingTopDownEntity.Kind.Page));Prayer=Mathf.Min(PrayerTarget,Prayer+amount);Message=$"말씀 수집 · 기도 +{amount}";break;}e.consumed=true;e.gameObject.SetActive(false);ArmGateIfReady();}
        public void SpawnWave(){waveNumber++;int safe=Mathf.Clamp(Mathf.RoundToInt(player.position.x/settings.laneSpacing)+2+UnityEngine.Random.Range(-1,2),0,4);int mask=0;int count=StageIndex==3&&waveNumber%4==0?4:StageIndex>=1?2:1;for(int n=0;n<count;n++){int lane=(safe+1+n)%5;mask|=1<<lane;}for(int lane=0;lane<5;lane++){if((mask&(1<<lane))==0)continue;var h=Instantiate(hazardPrefab,hazardRoot);h.name=$"Stage{StageIndex+1}_Wave{waveNumber}_Lane{lane+1}";h.kind=ChooseKind();h.warning=settings.warningSeconds;h.velocity=new Vector2(0,-(3.1f+StageIndex*.45f));h.transform.position=new Vector3((lane-2)*settings.laneSpacing,player.position.y+5.5f);h.lifetime=6;if(h.kind!=FastingTopDownHazard.Kind.Rock&&h.kind!=FastingTopDownHazard.Kind.Sand&&h.kind!=FastingTopDownHazard.Kind.Bat&&h.kind!=FastingTopDownHazard.Kind.Collapse&&h.kind!=FastingTopDownHazard.Kind.Reaper)h.velocity=Vector2.zero;if(h.kind==FastingTopDownHazard.Kind.Sand){h.transform.position=new Vector3(10,player.position.y+1.2f,0);h.velocity=new Vector2(-4,0);h.arrow.transform.localRotation=Quaternion.Euler(0,0,-90);}h.body.color=StageIndex==3?new Color(1,.4f,.13f):StageIndex==1?new Color(.55f,.48f,.75f):new Color(.84f,.66f,.34f);hazards.Add(h);}}
        FastingTopDownHazard.Kind ChooseKind()
        {
            FastingTopDownHazard.Kind[][] kinds={
                new[]{FastingTopDownHazard.Kind.Rock,FastingTopDownHazard.Kind.Sand,FastingTopDownHazard.Kind.Cactus,FastingTopDownHazard.Kind.Quicksand},
                new[]{FastingTopDownHazard.Kind.Rock,FastingTopDownHazard.Kind.Bat,FastingTopDownHazard.Kind.Collapse,FastingTopDownHazard.Kind.Crack},
                new[]{FastingTopDownHazard.Kind.Rock,FastingTopDownHazard.Kind.Tomb,FastingTopDownHazard.Kind.Wall,FastingTopDownHazard.Kind.Poison},
                new[]{FastingTopDownHazard.Kind.Fire,FastingTopDownHazard.Kind.Thorns,FastingTopDownHazard.Kind.Pillar,FastingTopDownHazard.Kind.Reaper}};
            return kinds[StageIndex][(waveNumber-1)%4];
        }
        void ApplyStage(bool first){CacheMapStarts();gateArmed=false;gate.gameObject.SetActive(false);for(int n=0;n<entities.Length;n++)if(entities[n])entities[n].transform.position=entityStarts[n];for(int n=0;n<backgrounds.Length;n++)if(backgrounds[n])backgrounds[n].transform.position=backgroundStarts[n];ClearHazards();wave=2;waveNumber=0;slow=root=prayerLock=vision=0;transition.color=Color.clear;gate.position=new Vector3(0,settings.stageLength);foreach(var e in entities){if(!e)continue;e.laneWidth=settings.laneSpacing;e.RefreshShelterMarkers();e.consumed=false;e.gameObject.SetActive(e.stage==StageIndex&&(e.kind==FastingTopDownEntity.Kind.Cover||e.kind==FastingTopDownEntity.Kind.Water||UnityEngine.Random.value<=settings.pickupChance));}Color[] colors={Color.white,new Color(.4f,.45f,.62f),new Color(.65f,.59f,.55f),new Color(.75f,.37f,.28f)};foreach(var bg in backgrounds)bg.color=colors[StageIndex];Message=first?"자동 전진 / 자동 기도 · 좌우 한 칸 회피":$"{settings.places[StageIndex]} · 지침 1칸 회복";Follow();}
        void CacheMapStarts()
        {
            if(entityStarts==null){entityStarts=new Vector3[entities.Length];for(int i=0;i<entities.Length;i++)if(entities[i])entityStarts[i]=entities[i].transform.position;}
            if(backgroundStarts==null){backgroundStarts=new Vector3[backgrounds.Length];for(int i=0;i<backgrounds.Length;i++)if(backgrounds[i])backgroundStarts[i]=backgrounds[i].transform.position;}
        }
        void ArmGateIfReady()
        {
            if(gateArmed||Prayer<PrayerTarget)return;
            gateArmed=true;gate.position=new Vector3(0,player.position.y+Mathf.Max(2,settings.finishLineDistance),0);gate.gameObject.SetActive(true);Message="기도 목표 완료 · 앞에 나타난 금색 전환선까지 전진하세요";
        }
        void RecycleMap()
        {
            if(backgrounds.Length>0){float height=backgrounds[0].bounds.size.y;foreach(var bg in backgrounds){if(!bg)continue;while(bg.bounds.max.y<gameCamera.transform.position.y-9)bg.transform.position+=Vector3.up*(height*backgrounds.Length);}}
            foreach(var e in entities){if(!e||e.stage!=StageIndex)continue;if(e.transform.position.y<player.position.y-7){float length=Mathf.Max(12,settings.stageLength);float cycles=Mathf.Floor((player.position.y-7-e.transform.position.y)/length)+1;e.transform.position+=Vector3.up*(cycles*length);e.consumed=false;e.gameObject.SetActive(e.kind==FastingTopDownEntity.Kind.Cover||e.kind==FastingTopDownEntity.Kind.Water||UnityEngine.Random.value<=settings.pickupChance);}}
        }
        void Follow(){gameCamera.transform.position=new Vector3(0,player.position.y+4,-10);foreach(var lane in laneGuides)if(lane)lane.transform.position=new Vector3(lane.transform.position.x,player.position.y+4,lane.transform.position.z);}
        public void Refresh(){if(!heading)return;int day=Mathf.Clamp(StageIndex*10+Mathf.FloorToInt(Prayer/PrayerTarget*10),StageIndex*10+1,StageIndex*10+10);heading.text=$"{settings.places[StageIndex]} · {day}/40일   {Elapsed:0.0}초   지침 {Health:0.#}/5";prayerFill.fillAmount=Mathf.Clamp01(Prayer/PrayerTarget);status.text=$"기도 {Prayer:0}/{PrayerTarget:0} · "+(Prayer>=PrayerTarget?"목표 완료! 위쪽 종착지로":Praying?"자동 기도 충전 중 · 좌우 회피 가능":PrayerBlockRemaining>0?$"기도 방해 · {PrayerBlockRemaining:0.0}초":WindSheltered?"모래바람 엄폐 성공 · 기도 가능":Sheltered?"낙하 장애물 엄폐 성공 · 기도 가능":"자동 전진 중 · 좌우 키로 한 칸씩 피하세요");items.text=$"수통[E] {(HasWater?"1":"0")}   방패 {(Shielded?"있음":"없음")}   불꽃 {flames}   "+(fever>0?"피버!":buff>0?"기도 가속":"");help.text=Message;for(int i=0;i<hearts.Length;i++)hearts[i].fillAmount=Mathf.Clamp01(Health-i);feet.color=Sheltered||WindSheltered?new Color(.2f,1,.5f,.8f):new Color(1,1,1,.75f);float opacity=vision>0?settings.visionOpacity:0;foreach(var p in visionPanels)p.color=new Color(.03f,.025f,.06f,opacity);Vector3 screen=gameCamera.WorldToViewportPoint(player.position);float cx=Mathf.Clamp01(screen.x),cy=Mathf.Clamp01(screen.y),halfX=.11f,halfY=.2f;SetPanel(0,new Vector2(0,0),new Vector2(Mathf.Max(0,cx-halfX),1));SetPanel(1,new Vector2(Mathf.Min(1,cx+halfX),0),Vector2.one);SetPanel(2,new Vector2(cx-halfX,0),new Vector2(cx+halfX,Mathf.Max(0,cy-halfY)));SetPanel(3,new Vector2(cx-halfX,Mathf.Min(1,cy+halfY)),new Vector2(cx+halfX,1));}
        void SetPanel(int i,Vector2 min,Vector2 max){var r=visionPanels[i].rectTransform;r.anchorMin=min;r.anchorMax=max;r.offsetMin=r.offsetMax=Vector2.zero;}
    }
}
