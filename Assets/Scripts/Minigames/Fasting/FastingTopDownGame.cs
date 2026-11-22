using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
namespace CrossTheLine.Minigames
{
    public sealed partial class FastingTopDownGame : MonoBehaviour
    {
        public FastingTopDownSettings settings;
        public FastingSpawnSettings spawnSettings;
        public bool TryChooseObstacle(out FastingTopDownHazard.Kind kind){if(spawnSettings){bool chosen=spawnSettings.TryPick(StageIndex,out kind);if(chosen)CurrentBalance.selected[(int)kind]++;return chosen;}kind=ChooseKind();CurrentBalance.selected[(int)kind]++;return true;}
        public Transform player,world,hazardRoot,gate;
        public Camera gameCamera;
        public SpriteRenderer playerBody,feet;
        public Sprite idle,prayingSprite;
        public FastingTopDownEntity[] entities;
        public FastingTopDownHazard hazardPrefab;
        public FastingDesertObstacleArt desertArt;
        public FastingEffectSprites effectSprites;
        public FastingPlayArea playArea;
        public FastingHitFeedback hitFeedback;
        public FastingScreenHitFeedback screenHitFeedback;
        public SpriteRenderer[] backgrounds,laneGuides;
        public TMP_Text heading,status,items,help,result;
        public UnityEngine.UI.Image prayerFill,transition;
        public UnityEngine.UI.Image[] hearts,visionPanels;
        public Sprite fullHeart,halfHeart,emptyHeart;
        public FastingVisionMask caveVisionMask;
        public GameObject introduction,resultPanel;
        public UnityEngine.UI.Button startButton,retryButton;
        public int StageIndex{get;private set;}
        public float Health{get;private set;}
        public float Prayer{get;private set;} public float PrayerTarget=>settings.GetPrayerTarget(StageIndex);
        public float Elapsed{get;private set;}
        public bool Running{get;private set;}
        public bool Complete{get;private set;}
        public bool Shielded=>ShieldCount>0;
        public bool HasWater{get;private set;}
        public string Message{get;private set;}="";
        public bool Praying{get;private set;}
        float prayerHeldFor;
        public float SlowRemaining=>slow; public float PrayerBlockRemaining=>Mathf.Max(prayerLock,root);
        public float VisionRemaining=>vision;
        public IReadOnlyList<FastingTopDownHazard> Hazards=>hazards;
        readonly List<FastingTopDownHazard> hazards=new List<FastingTopDownHazard>();
        float thornSlow;float wave,grace,slow,root,prayerLock,vision,buff,fever,swap,waterGrace,restart,waterNotice;
        int flames,waveNumber,lastVisionWave; float cameraY; public float CurrentSpeed=>settings.StageSpeed(StageIndex)*settings.scrollSpeedMultiplier; public int FlameStacks=>flames; public bool FeverActive=>fever>0; public float FeverRemaining=>fever; public float FireGauge=>FeverActive?Mathf.Clamp01(fever/settings.feverSeconds):Mathf.Clamp01((float)flames/Mathf.Max(1,settings.flamesForFever));
        bool failed; float waterBoost,reverse; bool gateArmed; Vector3[] entityStarts,backgroundStarts; public bool GateVisible=>gateArmed;
        void Start(){InitializeStageConfigurations();CacheMapStarts();Application.runInBackground=true;JourneyRuntimeFonts.Apply(gameObject.scene);startButton.onClick.AddListener(Begin);retryButton.onClick.AddListener(Begin);ResetState();introduction.SetActive(true);Running=false;Refresh();}
        void OnDestroy(){ReleaseStageConfigurations();if(startButton)startButton.onClick.RemoveListener(Begin);if(retryButton)retryButton.onClick.RemoveListener(Begin);}
        int testStartStage;
        public void BeginStageForTesting(int stage){testStartStage=Mathf.Clamp(stage,0,3);Begin();}
        public void Begin(){ResetState();if(testStartStage>0){StageIndex=testStartStage;ApplyStage(true);}introduction.SetActive(false);Running=true;Refresh();}
        void ClearHazards(){foreach(var h in hazards)if(h)Destroy(h.gameObject);hazards.Clear();}
        void ResetState(){ResetBalance();if(caveVisionMask)caveVisionMask.Clear();thornSlow=0;ResetRunStatistics();if(hitVignette)hitVignette.Clear();prayerHeldFor=0;grace=0;RefreshInvulnerability();if(screenHitFeedback)screenHitFeedback.Clear();if(hitFeedback)hitFeedback.Clear();ClearHazards();StageIndex=0;ApplyStageConfiguration();Health=settings.maximumHealth;Prayer=0;Elapsed=0;ResetShields();HasWater=true;Complete=false;failed=false;Praying=false;restart=0;flames=0;waveNumber=0;buff=fever=grace=slow=root=prayerLock=vision=swap=waterGrace=0;waterBoost=reverse=0;wave=2;Message="WASD 이동 · Space 기도 · E 수통";resultPanel.SetActive(false);player.position=Vector3.zero;ApplyStage(true);}
        void Update(){var k=Keyboard.current;if(k!=null&&k.rKey.wasPressedThisFrame){Begin();return;}if(!Running){if(introduction.activeSelf&&k!=null&&k.enterKey.wasPressedThisFrame)Begin();return;}
            Vector2 direction=Vector2.zero;if(k!=null){direction.x=(k.dKey.isPressed||k.rightArrowKey.isPressed?1:0)-(k.aKey.isPressed||k.leftArrowKey.isPressed?1:0);direction.y=(k.wKey.isPressed||k.upArrowKey.isPressed?1:0)-(k.sKey.isPressed||k.downArrowKey.isPressed?1:0);if(k.eKey.wasPressedThisFrame)UseWater();}Tick(Time.deltaTime,direction,k!=null&&k.spaceKey.isPressed);}
        public bool CanMove(Vector2 p)=>playArea?playArea.Contains(p):Mathf.Abs(p.x)<=6.7f&&p.y>=0;
        public void Tick(float dt,Vector2 direction,bool legacyPrayerInput=false)
        {
            if(!Running)return;dt=Mathf.Clamp(dt,0,.1f);Elapsed+=dt;CurrentBalance.seconds+=dt;grace=Mathf.Max(0,grace-dt);RefreshInvulnerability();
            RefreshDarknessProtection(dt);if(failed)return;
            if(swap>0){ConsumeFever(dt);swap-=dt;transition.color=new Color(.04f,.03f,.04f,Mathf.Sin(Mathf.Clamp01(swap/.6f)*Mathf.PI));if(swap<=0){StageIndex++;Health=Mathf.Min(settings.maximumHealth,Health+1);Prayer=0;player.position=Vector3.zero;ApplyStage(false);}Refresh();return;}
            thornSlow=Mathf.Max(0,thornSlow-dt);slow=Mathf.Max(0,slow-dt);root=Mathf.Max(0,root-dt);prayerLock=Mathf.Max(0,prayerLock-dt);vision=Mathf.Max(0,vision-dt);buff=Mathf.Max(0,buff-dt);waterGrace=Mathf.Max(0,waterGrace-dt);waterNotice=Mathf.Max(0,waterNotice-dt);waterBoost=Mathf.Max(0,waterBoost-dt);reverse=Mathf.Max(0,reverse-dt);if(reverse>0)direction=-direction;
            Vector2 from=player.position;
            bool prayerEngaged=legacyPrayerInput&&Prayer<PrayerTarget&&PrayerBlockRemaining<=0;
            float previousHold=prayerHeldFor;
            prayerHeldFor=prayerEngaged?prayerHeldFor+dt:0;
            float readyAfter=Mathf.Max(0,settings.prayerPreparationSeconds);
            Praying=prayerEngaged&&prayerHeldFor>=readyAfter;
            float chargingTime=prayerEngaged?Mathf.Max(0,prayerHeldFor-Mathf.Max(previousHold,readyAfter)):0;
            if(Praying){DirectPrayerSeconds+=chargingTime;float boostedTime=fever>0?Mathf.Clamp(fever-(dt-chargingTime),0,chargingTime):0;float beforePrayer=Prayer;Prayer=Mathf.Min(PrayerTarget,Prayer+settings.prayerPerSecond*(buff>0?1.5f:1)*(chargingTime+boostedTime));CurrentBalance.directPrayer+=Prayer-beforePrayer;}
            ConsumeFever(dt);
            cameraY+=CurrentSpeed*dt;
            if(playArea)
            {
                Follow();
                // Scrolling moves the valid rectangle before input is applied. Bring both
                // coordinates inside it first so the new bottom edge cannot reject lateral input.
                player.position=playArea.Clamp(player.position);
            }
            if(root<=0)
            {
                Vector2 motion=Vector2.ClampMagnitude(direction,1)*settings.manualMoveSpeed*(prayerEngaged?settings.prayerMoveMultiplier:1)*(slow>0?.55f:thornSlow>0?settings.thornMoveMultiplier:1)*(waterBoost>0?1.3f:1)*dt;
                Vector2 target=player.position;target.x=playArea?playArea.Clamp(target+new Vector2(motion.x,0)).x:Mathf.Clamp(target.x+motion.x,-6.7f,6.7f);
                if(CanMove(target))player.position=target;else Message="바위에 막힘 · 다른 방향으로 피하세요";
                target=player.position;target.y=playArea?playArea.Clamp(target+new Vector2(0,CurrentSpeed*dt+motion.y)).y:Mathf.Max(0,target.y+CurrentSpeed*dt+motion.y);
                if(CanMove(target))player.position=target;else Message="앞이 막힘 · 좌우로 피하세요";
            }
            BlockRuinsWalls(from);KeepInsideCamera();
            RefreshInvulnerability();
            Vector2 to=player.position;
            foreach(var e in entities){if(!e||!e.gameObject.activeSelf)continue;if(e.kind==FastingTopDownEntity.Kind.Cover)continue;if(e.consumed||!e.Contains(to))continue;
                if(e.kind==FastingTopDownEntity.Kind.Water){if(StageIndex<3&&!HasWater){HasWater=true;waterGrace=.15f;e.SetUsed(true);CurrentBalance.pickups[(int)e.kind]++;Message="수통 충전 · 이 우물은 사용 완료";}}
                else Collect(e);
            }
            for(int i=hazards.Count-1;i>=0;i--){var h=hazards[i];if(!h){hazards.RemoveAt(i);continue;}h.UpdateApproach(to);if(FitsSpawnRoad(h))h.TrackVisibility(gameCamera,dt);bool contact=h.Advance(dt,from,to,out var entry);bool insideRoad=FitsSpawnRoad(h);if(!insideRoad)h.HideOutsideRoad();if(contact&&insideRoad&&h.ReadyToHit){int shieldsBefore=ShieldCount;bool damaged=TryDamage(h.kind.ToString());if(damaged||ShieldCount<shieldsBefore){if(h.kind!=FastingTopDownHazard.Kind.Reaper&&(StageIndex!=2||h.kind!=FastingTopDownHazard.Kind.Wall)){h.spent=true;h.gameObject.SetActive(false);}if(damaged)ApplyEffect(h.kind);}}if(h.transform.position.y<cameraY-gameCamera.orthographicSize-3||SideHazardExited(h)){if(!h.spent)AvoidedHazards++;h.spent=true;h.gameObject.SetActive(false);}if(h.spent){Destroy(h.gameObject);hazards.RemoveAt(i);}}
            if(encounterDirector)encounterDirector.Tick(dt);else{wave-=dt;if(wave<=0){SpawnWave();wave=settings.NextSpawnInterval(StageIndex);}}
            if(StageIndex>0&&waveNumber>0&&waveNumber%4==0&&lastVisionWave!=waveNumber){vision=settings.visionSeconds;lastVisionWave=waveNumber;}
            ArmGateIfReady();RecycleMap();
            if(gateArmed&&player.position.y>=gate.position.y-.05f){if(Prayer>=PrayerTarget){if(StageIndex==3){Complete=true;Running=false;resultPanel.SetActive(true);result.text=BuildRunResult(true);}else{swap=.6f;ClearHazards();Message="기도 완료 · 다음 장소로 이동";}}}
            Follow();Refresh();
        }
        public bool TryDamage(string cause){if(!Running||Invulnerable||DarknessProtecting||failed)return false;if(Shielded){BreakShield();return false;}prayerHeldFor=0;Praying=false;float previousHealth=Health;float amount=DamageFor(cause);Health=Mathf.Max(0,Health-amount);if(Health<previousHealth){RecordDamage(cause);if(hitVignette)hitVignette.PlayDamage(cause);}grace=settings.hitGrace;RefreshInvulnerability();Message=$"피해 -{amount:0.#}칸 · {cause}";if(Health<=0){failed=true;Running=false;resultPanel.SetActive(true);result.text=BuildRunResult(false);Message="지침 소진";}return true;}
        void ApplyEffect(FastingTopDownHazard.Kind k){if(screenHitFeedback)screenHitFeedback.Play(k);if(hitFeedback)hitFeedback.Play(k);if(k==FastingTopDownHazard.Kind.Sand&&hitFeedback)grace=Mathf.Max(grace,hitFeedback.sandAirSeconds);if(k==FastingTopDownHazard.Kind.Sand||k==FastingTopDownHazard.Kind.Bat)slow=1.2f;if(k==FastingTopDownHazard.Kind.Thorns)thornSlow=settings.thornSlowSeconds;if(k==FastingTopDownHazard.Kind.Quicksand)root=settings.quicksandStunSeconds;if(k==FastingTopDownHazard.Kind.Reaper)root=.4f;if(k==FastingTopDownHazard.Kind.Crack){root=hitFeedback?hitFeedback.crackFallSeconds:.9f;grace=Mathf.Max(grace,hitFeedback?hitFeedback.crackFallSeconds+hitFeedback.crackBlinkSeconds:1.7f);}if(k==FastingTopDownHazard.Kind.Bat){slow=settings.batSlowSeconds;vision=settings.batVisionSeconds;if(hitFeedback)grace=Mathf.Max(grace,hitFeedback.batAirSeconds);}if(k==FastingTopDownHazard.Kind.Fire||k==FastingTopDownHazard.Kind.Pillar)prayerLock=settings.burnPrayerBlockSeconds;if(k==FastingTopDownHazard.Kind.Pillar){root=hitFeedback?hitFeedback.pillarAirSeconds:1.5f;grace=Mathf.Max(grace,hitFeedback?hitFeedback.pillarProtectionSeconds:3);}if(k==FastingTopDownHazard.Kind.Tomb)prayerLock=settings.tombPrayerBlockSeconds;if(k==FastingTopDownHazard.Kind.Reaper)vision=settings.visionSeconds;if(k==FastingTopDownHazard.Kind.Poison){reverse=settings.poisonReverseSeconds;Message="오염된 물 · 방향 반전 / E 수통으로 정화";}grace=Mathf.Max(grace,root+.45f);}
        public void UseWater(){if(!HasWater||!Running||failed)return;HasWater=false;thornSlow=0;slow=root=prayerLock=vision=reverse=0;waterBoost=3;Health=Mathf.Min(settings.maximumHealth,Health+1);grace=Mathf.Max(grace,.5f);Message="수통 사용 · 정화 / 지침 +1 / 이동 가속 3초";}
        public void Collect(FastingTopDownEntity e){float beforeItemPrayer=Prayer;CurrentBalance.pickups[(int)e.kind]++;switch(e.kind){case FastingTopDownEntity.Kind.Shield:CollectShield(e);break;case FastingTopDownEntity.Kind.Flame:CollectSpiritFlame();break;default:float amount=PrayerTarget*(e.kind==FastingTopDownEntity.Kind.Book?(CurrentStageConfiguration?Mathf.Clamp01(CurrentStageConfiguration.bibleCharge):1f):settings.ScriptureFraction(StageIndex,e.kind==FastingTopDownEntity.Kind.Page));Prayer=Mathf.Min(PrayerTarget,Prayer+amount);Message=$"말씀 수집 · 기도 +{amount}";break;}CurrentBalance.itemPrayer+=Prayer-beforeItemPrayer;e.consumed=true;e.gameObject.SetActive(false);ArmGateIfReady();}
        public void SpawnWave()
        {
            waveNumber++;if(obstaclePatterns&&obstaclePatterns.Spawn(this))return;if(!TryChooseObstacle(out var kind))return;
            if(kind==FastingTopDownHazard.Kind.Sand){int origin=UnityEngine.Random.Range(0,3);if(origin>0){SpawnSideSand(origin==1);return;}}
            if(StageIndex==0){int lanes=kind==FastingTopDownHazard.Kind.Sand?UnityEngine.Random.Range(1,3):1;int first=UnityEngine.Random.value<.6f?Mathf.Clamp(Mathf.RoundToInt(player.position.x/settings.laneSpacing+SpawnLaneCenter),0,SpawnLaneCount-lanes):UnityEngine.Random.Range(0,SpawnLaneCount+1-lanes);SpawnAbove(kind,first,lanes);return;}
            if(StageIndex==3&&trialArt){SpawnAbove(kind,UnityEngine.Random.Range(0,SpawnLaneCount),1);return;}
            int safe=Mathf.Clamp(Mathf.RoundToInt(player.position.x/settings.laneSpacing+SpawnLaneCenter)+UnityEngine.Random.Range(-1,2),0,SpawnLaneCount-1);
            int count=StageIndex==3&&kind==FastingTopDownHazard.Kind.Reaper?4:2;
            for(int i=0;i<count;i++)SpawnAbove(kind,(safe+1+i)%SpawnLaneCount,1);
        }
        public void SpawnRandomObstacle()
        {
            if(!TryChooseObstacle(out var kind))return;
            if(kind==FastingTopDownHazard.Kind.Sand&&UnityEngine.Random.value<.4f){SpawnSideSand(UnityEngine.Random.value<.5f);return;}
            int lanes=kind==FastingTopDownHazard.Kind.Sand?UnityEngine.Random.Range(1,3):1;
            // No player tracking or pattern sequence: choose the type, column and spacing independently.
            SpawnAbove(kind,UnityEngine.Random.Range(0,SpawnLaneCount+1-lanes),lanes,UnityEngine.Random.Range(0,1.8f));
        }
        void SpawnAbove(FastingTopDownHazard.Kind kind,int firstLane,int lanes,float heightOffset=0,bool keepLane=false,bool caveFormation=true,int formationId=0,int formationCount=1)
        {
            if(StageIndex==3&&trialArt&&caveFormation&&(kind==FastingTopDownHazard.Kind.Thorns||kind==FastingTopDownHazard.Kind.Pillar||kind==FastingTopDownHazard.Kind.Reaper)){SpawnTrialFormation(kind,firstLane,heightOffset);return;}
            if(StageIndex==2&&ruinsArt&&caveFormation&&(kind==FastingTopDownHazard.Kind.Tomb||kind==FastingTopDownHazard.Kind.Wall)){SpawnRuinsFormation(kind,firstLane,heightOffset);return;}
            if(StageIndex==1&&caveArt&&caveFormation&&(kind==FastingTopDownHazard.Kind.Bat||kind==FastingTopDownHazard.Kind.Rock||kind==FastingTopDownHazard.Kind.Collapse)){SpawnCaveFormation(kind,firstLane,heightOffset);return;}
            var prefab=StageIndex==0&&desertArt?desertArt.PrefabFor(kind,hazardPrefab):hazardPrefab;
            if(StageIndex==1&&caveArt)prefab=caveArt.PrefabFor(kind,prefab);
            if(StageIndex==2&&ruinsArt)prefab=ruinsArt.PrefabFor(kind,prefab);
            if(StageIndex==3&&trialArt)prefab=trialArt.PrefabFor(kind,prefab);
            var h=Instantiate(prefab,hazardRoot);h.kind=kind;h.formationId=formationId;h.formationCount=formationCount;h.name=$"Stage{StageIndex+1}_{kind}_Lanes{lanes}";
            h.warning=0;h.lifetime=30;
            h.transform.position=new Vector3(SpawnLaneX(firstLane+(lanes-1)*.5f),cameraY+gameCamera.orthographicSize+2.5f);
            bool moving=kind==FastingTopDownHazard.Kind.Rock||kind==FastingTopDownHazard.Kind.Sand||kind==FastingTopDownHazard.Kind.Bat||kind==FastingTopDownHazard.Kind.Collapse||kind==FastingTopDownHazard.Kind.Reaper;
            h.velocity=moving&&!h.scrollOnly?new Vector2(0,-(kind==FastingTopDownHazard.Kind.Sand?2.2f:3.1f+StageIndex*.45f))*settings.hazardSpeedMultiplier:Vector2.zero;
            h.body.color=StageIndex==3?new Color(1,.4f,.13f):StageIndex==1?new Color(.55f,.48f,.75f):new Color(.84f,.66f,.34f);
            if(StageIndex==0&&desertArt)desertArt.Apply(h);
            if(StageIndex==1&&caveArt)caveArt.Apply(h);
            if(StageIndex==2&&ruinsArt)h.body.color=Color.white;
            if(StageIndex==3&&trialArt)h.body.color=Color.white;
            if(StageIndex==2&&ruinsArt)h.collisionHeight*=ruinsArt.obstacleScale;
            h.ConfigureLanes(lanes,StageIndex==2&&ruinsArt?settings.laneSpacing*ruinsArt.obstacleScale:StageIndex==3&&(kind==FastingTopDownHazard.Kind.Thorns||kind==FastingTopDownHazard.Kind.Pillar)?settings.laneSpacing/.75f:settings.laneSpacing);if(StageIndex>0)h.Enlarge(settings.laterStageObstacleScale);h.transform.position=new Vector3(h.transform.position.x,cameraY+gameCamera.orthographicSize+Mathf.Max(2.5f,h.body.bounds.extents.y+1)+Mathf.Max(0,heightOffset),0);if(!AcceptHazard(h,keepLane)){Destroy(h.gameObject);return;}RegisterHazard(h);
        }
        void SpawnSideSand(bool fromLeft)
        {
            var prefab=desertArt?desertArt.PrefabFor(FastingTopDownHazard.Kind.Sand,hazardPrefab):hazardPrefab;
            var h=Instantiate(prefab,hazardRoot);h.kind=FastingTopDownHazard.Kind.Sand;
            h.name=fromLeft?"Sand_LeftToRight":"Sand_RightToLeft";h.warning=0;h.lifetime=30;
            if(desertArt)desertArt.Apply(h);
            h.ConfigureLanes(1,settings.laneSpacing);
            float halfWidth=gameCamera.orthographicSize*gameCamera.aspect;
            float margin=Mathf.Max(h.body.bounds.extents.x*1.25f,.5f)+.5f;
            float x=gameCamera.transform.position.x+(fromLeft?-1:1)*(halfWidth+margin);
            float y=cameraY+UnityEngine.Random.Range(-gameCamera.orthographicSize*.65f,gameCamera.orthographicSize*.55f);
            h.transform.position=new Vector3(x,y,0);
            // Match camera scrolling vertically so the visible trajectory is horizontal.
            h.velocity=new Vector2(fromLeft?settings.sideSandSpeed:-settings.sideSandSpeed,CurrentSpeed);h.body.flipX=!fromLeft;
            if(!AcceptHazard(h)){Destroy(h.gameObject);return;}RegisterHazard(h);
        }
        bool SideHazardExited(FastingTopDownHazard h)
        {
            if(h.kind!=FastingTopDownHazard.Kind.Sand||Mathf.Abs(h.velocity.x)<.01f)return false;
            float edge=gameCamera.orthographicSize*gameCamera.aspect+Mathf.Max(2,h.body.bounds.extents.x+1);
            float x=h.transform.position.x-gameCamera.transform.position.x;
            return h.velocity.x>0?x>edge:x< -edge;
        }
        public void RefreshHealthHearts()
        {
            for(int i=0;i<hearts.Length;i++)
            {
                var heart=hearts[i];if(!heart)continue;
                if(fullHeart&&emptyHeart){heart.sprite=Health>=i+1?fullHeart:Health>i&&halfHeart?halfHeart:emptyHeart;heart.type=UnityEngine.UI.Image.Type.Simple;heart.preserveAspect=true;heart.fillAmount=1;heart.color=Color.white;}
                else heart.fillAmount=Mathf.Clamp01(Health-i);
            }
        }
        FastingTopDownHazard.Kind ChooseKind()
        {
            FastingTopDownHazard.Kind[][] kinds={
                new[]{FastingTopDownHazard.Kind.Rock,FastingTopDownHazard.Kind.Sand,FastingTopDownHazard.Kind.Cactus,FastingTopDownHazard.Kind.Quicksand},
                new[]{FastingTopDownHazard.Kind.Rock,FastingTopDownHazard.Kind.Bat,FastingTopDownHazard.Kind.Collapse,FastingTopDownHazard.Kind.Crack},
                new[]{FastingTopDownHazard.Kind.Rock,FastingTopDownHazard.Kind.Tomb,FastingTopDownHazard.Kind.Wall,FastingTopDownHazard.Kind.Poison},
                new[]{FastingTopDownHazard.Kind.Fire,FastingTopDownHazard.Kind.Thorns,FastingTopDownHazard.Kind.Pillar,FastingTopDownHazard.Kind.Reaper}};

            var list=kinds[Mathf.Clamp(StageIndex,0,kinds.Length-1)];return list[UnityEngine.Random.Range(0,list.Length)];
        }
        void ApplyStage(bool first){ApplyStageConfiguration();if(caveVisionMask)caveVisionMask.Clear();thornSlow=0;prayerHeldFor=0;Praying=false;CacheMapStarts();gateArmed=false;gate.gameObject.SetActive(false);for(int n=0;n<entities.Length;n++)if(entities[n])entities[n].transform.position=entityStarts[n];for(int n=0;n<backgrounds.Length;n++)if(backgrounds[n])backgrounds[n].transform.position=backgroundStarts[n];ClearHazards();cameraY=2.5f;wave=1.5f;waveNumber=lastVisionWave=0;slow=root=prayerLock=vision=0;transition.color=Color.clear;gate.position=new Vector3(0,settings.stageLength);foreach(var e in entities){if(!e)continue;e.SetUsed(false);e.gameObject.SetActive(false);if(e.stage==StageIndex)PlaceEntity(e,true);}if(encounterDirector)encounterDirector.ResetEncounter();Color[] colors={Color.white,new Color(.4f,.45f,.62f),new Color(.65f,.59f,.55f),new Color(.75f,.37f,.28f)};ApplyStageBackgrounds(colors[StageIndex]);Message=first?"WASD 이동 · Space 기도 · E 수통":$"{settings.places[StageIndex]} · 지침 1칸 회복";Follow();}
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
            foreach(var e in entities){if(!e||e.stage!=StageIndex||e.kind==FastingTopDownEntity.Kind.Cover)continue;if(e.transform.position.y<player.position.y-7)PlaceEntity(e,false);}
        }
        void KeepInsideCamera()
        {
            if(playArea){player.position=playArea.Clamp(player.position);return;}
            float minY=Mathf.Max(0,cameraY-3.8f),maxY=cameraY+4.5f;Vector2 p=player.position;float y=Mathf.Clamp(p.y,minY,maxY);
            if(Mathf.Abs(y-p.y)<.0001f)return;
            var desired=new Vector2(p.x,y);
            if(CanMove(desired)){player.position=desired;return;}
            for(float offset=.25f;offset<=2;offset+=.25f)foreach(int side in new[]{-1,1}){var alternate=new Vector2(Mathf.Clamp(p.x+side*offset,-6.7f,6.7f),y);if(CanMove(alternate)){player.position=alternate;return;}}
        }
        void Follow(){gameCamera.transform.position=new Vector3(0,cameraY,-10);foreach(var lane in laneGuides)if(lane)lane.transform.position=new Vector3(lane.transform.position.x,cameraY,lane.transform.position.z);}
        public void Refresh(){if(!heading)return;int day=CurrentDay;heading.text=$"{settings.places[StageIndex]} · {day}/40일";prayerFill.fillAmount=Mathf.Clamp01(Prayer/PrayerTarget);status.text="";items.text="";help.text="";RefreshHealthHearts();feet.color=new Color(1,1,1,.75f);float opacity=StageIndex>0&&vision>0&&!caveVisionMask?settings.visionOpacity:0;foreach(var p in visionPanels)p.color=new Color(.03f,.025f,.06f,opacity);float halfHeight=gameCamera.orthographicSize,halfWidth=halfHeight*gameCamera.aspect;Vector3 screen=gameCamera.orthographic?new Vector3(.5f+(player.position.x-gameCamera.transform.position.x)/(2*halfWidth),.5f+(player.position.y-gameCamera.transform.position.y)/(2*halfHeight)):gameCamera.WorldToViewportPoint(player.position);float cx=Mathf.Clamp01(screen.x),cy=Mathf.Clamp01(screen.y),halfX=.11f,halfY=.22f;if(caveVisionMask){var at=playerBody.bounds.center;caveVisionMask.Set(StageIndex>0&&vision>0?1:0,new Vector2(.5f+(at.x-gameCamera.transform.position.x)/(2*halfWidth),.5f+(at.y-gameCamera.transform.position.y)/(2*halfHeight)),gameCamera);}SetPanel(0,new Vector2(0,0),new Vector2(Mathf.Max(0,cx-halfX),1));SetPanel(1,new Vector2(Mathf.Min(1,cx+halfX),0),Vector2.one);SetPanel(2,new Vector2(cx-halfX,0),new Vector2(cx+halfX,Mathf.Max(0,cy-halfY)));SetPanel(3,new Vector2(cx-halfX,Mathf.Min(1,cy+halfY)),new Vector2(cx+halfX,1));}
        MaterialPropertyBlock flashProperties;
        void RefreshInvulnerability(){if(!playerBody)return;if(flashProperties==null)flashProperties=new MaterialPropertyBlock();playerBody.color=new Color(1,1,1,hitFeedback?hitFeedback.ArtworkOpacity:1);playerBody.GetPropertyBlock(flashProperties);flashProperties.SetFloat("_Flash",grace>0&&Mathf.FloorToInt(Elapsed*12)%2==0?1:0);playerBody.SetPropertyBlock(flashProperties);}
        bool PickupPathClear(FastingTopDownHazard h){foreach(var e in entities)if(e&&e.gameObject.activeSelf&&!e.consumed&&e.kind!=FastingTopDownEntity.Kind.Cover&&PathOverlaps(h,e))return false;return true;}
        bool PickupPositionClear(FastingTopDownEntity e){foreach(var h in hazards)if(h&&!h.spent&&PathOverlaps(h,e))return false;return true;}
        bool PathOverlaps(FastingTopDownHazard h,FastingTopDownEntity e)
        {
            float duration=Mathf.Clamp((e.transform.position.y-player.position.y+8)/CurrentSpeed,0,30);
            Vector2 a=h.CollisionCenter,b=a+h.velocity*duration;
            float growth=h.MaximumApproachScale;
            Vector2 half=h.size*growth*.5f+e.footprint*.5f+Vector2.one*.35f;
            Vector2 relative=(Vector2)e.transform.position-a,d=b-a;float enter=0,leave=1;
            for(int axis=0;axis<2;axis++){if(Mathf.Abs(d[axis])<.0001f){if(Mathf.Abs(relative[axis])>half[axis])return false;}else{float lo=(relative[axis]-half[axis])/d[axis],hi=(relative[axis]+half[axis])/d[axis];if(lo>hi){float temp=lo;lo=hi;hi=temp;}enter=Mathf.Max(enter,lo);leave=Mathf.Min(leave,hi);if(enter>leave)return false;}}return true;
        }
        void SetPanel(int i,Vector2 min,Vector2 max){var r=visionPanels[i].rectTransform;r.anchorMin=min;r.anchorMax=max;r.offsetMin=r.offsetMax=Vector2.zero;}
    }
}
