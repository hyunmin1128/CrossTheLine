using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CrossTheLine
{
    public sealed partial class JourneyGame : MonoBehaviour
    {
        public Sprite[] jesusFrames;
        public Sprite[] disciples;
        public Sprite[] supportingCast;
        public TMP_FontAsset koreanFont;
        public TextAsset storySource;
        public JourneyAssetLibrary runtimeAssets;
        public Camera journeyCamera;
        public JourneySceneBindings sceneBindings; public GameObject authoredPreviewRoot;
        public int sceneChapterIndex = -1;
        public Material spriteMaterial;
        public Sprite[] baptismDoveFrames;
        public AudioClip baptismHeavenVoice;
        public bool baptismPreviewOnly;
        public bool temptationPreviewOnly; public bool disciplesPreviewOnly; public bool weddingPreviewOnly; public bool templePreviewOnly; public bool synagoguePreviewOnly; public bool fishingPreviewOnly; public bool matthewPreviewOnly; public bool stormPreviewOnly; public bool feedingPreviewOnly; public bool waterWalkingPreviewOnly; public bool confessionPreviewOnly; public bool prayerPreviewOnly; public bool secondTemplePreviewOnly; public SecondTempleCleansing SecondTemple {get;private set;} public bool entryPreviewOnly; public JerusalemEntry EntryScene {get;private set;} public bool childrenPreviewOnly; public ChildrenBlessing ChildrenScene {get;private set;} public LordsPrayer Prayer {get;private set;} public PeterConfession Confession {get;private set;} public WaterWalking WaterWalk {get;private set;} public FeedingMultitude Feeding {get;private set;} public StormAtSea Storm {get;private set;} public MatthewCalling Matthew {get;private set;} public MiraculousCatch Fishing {get;private set;} public SynagogueDispute Synagogue {get;private set;} public TempleCleansing Temple {get;private set;} public CanaWedding Wedding {get;private set;} public FirstDisciples Meeting { get; private set; }
        public Sprite[] satanFrames;
        public JordanBaptism Baptism { get; private set; }
        public DesertTemptation Temptation { get; private set; }

        public static readonly string[] ActorNames = { "베드로", "안드레", "야고보", "요한", "빌립", "바돌로매", "마태", "도마", "알패오의 야고보", "시몬", "다대오", "가룟 유다" };
        public int ChapterIndex => save.chapter;
        public int StepIndex => save.step;
        public string ControlledActor => currentActor;
        public string Mode => menu ? "menu" : dialog ? "dialogue" : mini ? "minigame" : ending ? "chapter_end" : (Fishing&&Fishing.InGame)||(Storm&&Storm.InGame)||(WaterWalk&&WaterWalk.InGame)||(Prayer&&Prayer.InGame) ? "minigame" : (Baptism&&Baptism.IsCinematic)||(Temptation&&Temptation.IsCinematic)||(Meeting&&Meeting.IsCinematic)||(Wedding&&Wedding.IsCinematic)||(Temple&&Temple.IsCinematic)||(Synagogue&&Synagogue.IsCinematic)||(Fishing&&Fishing.IsCinematic)||(Matthew&&Matthew.IsCinematic)||(Storm&&Storm.IsCinematic)||(Feeding&&Feeding.IsCinematic)||(WaterWalk&&WaterWalk.IsCinematic)||(Confession&&Confession.IsCinematic)||(Prayer&&Prayer.IsCinematic)||(ChildrenScene&&ChildrenScene.IsCinematic)||(EntryScene&&EntryScene.IsCinematic)||(SecondTemple&&SecondTemple.IsCinematic) ? "cutscene" : "exploration";
        public float ActiveSeconds => save.seconds;
        public int DiscoveryCount => found.Count;
        public int TotalChapters => book?.chapters.Length ?? 0;
        public string SavePath => Path.Combine(Application.persistentDataPath, "CrossTheLine-journey.json");

        StoryBook book;
        JourneySave save = new JourneySave();
        readonly HashSet<string> found = new HashSet<string>();
        JourneyWorld world;
        Transform player, companion, goal;
        SpriteRenderer playerSprite, companionSprite;
        Material actorMaterial;
        Sprite dotSprite;
        Sprite authoredJesusIdle;
        Texture2D dotTexture;
        Canvas canvas;
        RectTransform ui;
        GameObject menu, dialog, mini, ending;
        GameObject headerPanel, objectivePanel, journalButton, menuButton;
        TMP_Text hudTitle, hudObjective, hudActor, hudPrompt, hudProgress, toast;
        TMP_Text dialogSpeaker, dialogText, dialogHint;
        TMP_Text miniLabel, miniFeedback, miniHint;
        RectTransform miniPointer, miniFill, miniTarget;
        SpriteRenderer goalGlow;
        AudioSource music, effects;
        AudioClip musicClip, chime;
        readonly List<string> dialogue = new List<string>();
        Action dialogueFinished;
        int dialogueIndex, journalPage, wins, prayerIndex;
        float typing, toastUntil, miniTime, miniProgress, tension, balance, danger, musicVolume = .23f;
        bool started, gentle = true, muted;
        bool initialBaptismPreview,initialTemptationPreview,initialDisciplesPreview,initialWeddingPreview,initialTemplePreview,initialSynagoguePreview,initialFishingPreview,initialMatthewPreview,initialStormPreview,initialFeedingPreview,initialWaterWalkingPreview,initialConfessionPreview,initialPrayerPreview,initialChildrenPreview,initialEntryPreview,initialSecondTemplePreview;
        string currentActor = "예수님", miniKind;
        float spawnX, objectiveX;
        int lastServeFrame = -1;
        int lastJesusWalkFrame=-1;float jesusWalkPhase;bool jesusWalking;
        readonly float[] discoveryX = { 9, 29 };
        readonly string[] prayerParts = { "아버지께 마음을 향하기", "이름과 나라와 뜻을 구하기", "오늘의 양식을 구하기", "용서를 구하고 용서하기", "시험과 악에서 보호를 구하기" };
        readonly Color ink = new Color(.09f,.16f,.18f), paper = new Color(.95f,.90f,.78f), gold = new Color(.88f,.69f,.37f), teal = new Color(.17f,.35f,.35f);
        Chapter Chapter => book.chapters[save.chapter];
        StoryStep Step => Chapter.steps[Mathf.Min(save.step, Chapter.steps.Length - 1)];
        Keyboard K => Keyboard.current;
        bool Key(Key key) => K != null && K[key].wasPressedThisFrame;
        bool Press => Key(UnityEngine.InputSystem.Key.E) || Key(UnityEngine.InputSystem.Key.Space) || Key(UnityEngine.InputSystem.Key.Enter);
        bool Held => K != null && (K.spaceKey.isPressed || K.eKey.isPressed);

        void Awake()
        {
            JourneyAssets.Bind(runtimeAssets);
            if(sceneBindings)sceneBindings.HideTitle();
            koreanFont=JourneyRuntimeFonts.Resolve(koreanFont);
            JourneyRuntimeFonts.Apply(gameObject.scene);
            foreach(var root in gameObject.scene.GetRootGameObjects())foreach(var bank in root.GetComponentsInChildren<JourneySceneStages>(true))foreach(var stage in bank.stages)if(stage.root)stage.root.SetActive(false);
            if(authoredPreviewRoot)authoredPreviewRoot.SetActive(false);
            initialBaptismPreview=baptismPreviewOnly;initialTemptationPreview=temptationPreviewOnly;initialDisciplesPreview=disciplesPreviewOnly;initialWeddingPreview=weddingPreviewOnly;initialTemplePreview=templePreviewOnly;initialSynagoguePreview=synagoguePreviewOnly;initialFishingPreview=fishingPreviewOnly;initialMatthewPreview=matthewPreviewOnly;initialStormPreview=stormPreviewOnly;initialFeedingPreview=feedingPreviewOnly;initialWaterWalkingPreview=waterWalkingPreviewOnly;initialConfessionPreview=confessionPreviewOnly;initialPrayerPreview=prayerPreviewOnly;initialChildrenPreview=childrenPreviewOnly;initialEntryPreview=entryPreviewOnly;initialSecondTemplePreview=secondTemplePreviewOnly;
            Application.runInBackground = true;
            Time.timeScale = 1f;
            book = JsonUtility.FromJson<StoryBook>(storySource.text);
            jesusFrames=jesusFrames.Select(JourneySpriteAssets.Resolve).ToArray();disciples=disciples.Select(JourneySpriteAssets.Resolve).ToArray();supportingCast=supportingCast.Select(JourneySpriteAssets.Resolve).ToArray();
            var jesusDefinition=JourneyAssets.Load<GameObject>("CrossTheLine/Characters/Jesus")?.GetComponent<JourneyCharacterView>();if(jesusDefinition){authoredJesusIdle=jesusDefinition.idle;if(jesusDefinition.walkFrames.Length==8&&jesusDefinition.walkFrames.All(s=>s))jesusFrames=jesusDefinition.walkFrames;}
            var chapterSprites=GetComponent<JourneyChapterSpriteSet>();if(chapterSprites&&chapterSprites.jesusIdle)authoredJesusIdle=chapterSprites.jesusIdle;
            for(int i=0;i<disciples.Length;i++){var definition=JourneyAssets.Load<GameObject>("CrossTheLine/Characters/Disciple"+i.ToString("00"))?.GetComponent<JourneyCharacterView>();if(definition&&definition.idle)disciples[i]=definition.idle;}
            if (book == null || book.chapters == null || book.chapters.Length == 0 || !koreanFont || jesusFrames == null || jesusFrames.Length == 0 || disciples == null || disciples.Length != 12)
                throw new InvalidOperationException("Journey scene requires story, Korean font, Jesus frames and twelve disciples. Rebuild with JourneyBuilder.Build.");
            gentle = PlayerPrefs.GetInt("CTL_Gentle", 1) == 1;
            muted = PlayerPrefs.GetInt("CTL_Muted", 0) == 1;
            ReadSave();
            bool transferred=JourneySceneFlow.Take(out var progress,out var home);
            if(transferred){save=JsonUtility.FromJson<JourneySave>(progress);found.Clear();foreach(var d in save.discoveries??Array.Empty<string>())found.Add(d);started=true;}
            else if(sceneChapterIndex>=1&&!home){save=new JourneySave{chapter=sceneChapterIndex,unlocked=sceneChapterIndex};found.Clear();started=true;}
            if(!spriteMaterial)throw new InvalidOperationException("Journey sprite material is missing.");
            actorMaterial = new Material(spriteMaterial);
            dotTexture = new Texture2D(1,1) { filterMode = FilterMode.Point }; dotTexture.SetPixel(0,0,Color.white); dotTexture.Apply();
            dotSprite = Sprite.Create(dotTexture, new Rect(0,0,1,1), new Vector2(.5f,.5f),1);
            if(!JourneySpriteAssets.Ready)gameObject.AddComponent<JourneyPixelStyle>().Initialize(this);
            gameObject.AddComponent<JourneyPresentation>().Initialize(this);
            BuildUI(); BuildAudio();
            world = new JourneyWorld();
            LoadWorld(started);
            if(started){DisposeMenu();SetHudVisible(true);}else ShowHome();
        }

        void Update()
        {
            if (music) music.volume = muted ? 0 : (Baptism && Baptism.NarrationActive)||(Prayer&&Prayer.NarrationActive)||(EntryScene&&EntryScene.MusicActive)||(SecondTemple&&SecondTemple.AmbienceActive) ? .035f : musicVolume;
            if (effects) effects.volume = muted ? 0 : .22f;
            if (toast) toast.gameObject.SetActive(Time.unscaledTime < toastUntil);
            if (Key(UnityEngine.InputSystem.Key.Escape))
            {
                if (menu && started) CloseMenu();
                else if (!menu && started) ShowPause();
                return;
            }
            if (menu || !started) return;
            save.seconds += Time.unscaledDeltaTime;
            var continuous=GetComponent<JourneyContinuousWorld>();if(continuous&&continuous.WalkingToNext){continuous.TickExit(Time.deltaTime);return;}
            if(Baptism)Baptism.Tick(Time.deltaTime,!dialog&&!ending&&!mini,Press,muted);
            if(Temptation)Temptation.Tick(Time.deltaTime,!dialog&&!ending&&!mini,Press);
            if(Meeting)Meeting.Tick(Time.deltaTime,!dialog&&!ending&&!mini,Press);if(Wedding)Wedding.Tick(Time.deltaTime,!dialog&&!ending&&!mini,Press);if(Temple)Temple.Tick(Time.deltaTime,!dialog&&!ending&&!mini,Press);if(Synagogue)Synagogue.Tick(Time.deltaTime,!dialog&&!ending&&!mini,Press);if(Fishing)Fishing.Tick(Time.deltaTime,!dialog&&!ending&&!mini,Press);if(Matthew)Matthew.Tick(Time.deltaTime,!dialog&&!ending&&!mini,Press);if(Storm)Storm.Tick(Time.deltaTime,!dialog&&!ending&&!mini,Press);if(Feeding)Feeding.Tick(Time.deltaTime,!dialog&&!ending&&!mini,Press);if(WaterWalk)WaterWalk.Tick(Time.deltaTime,!dialog&&!ending&&!mini,Press);if(Confession)Confession.Tick(Time.deltaTime,!dialog&&!ending&&!mini,Press);if(Prayer)Prayer.Tick(Time.deltaTime,!dialog&&!ending&&!mini,Press);if(ChildrenScene)ChildrenScene.Tick(Time.deltaTime,!dialog&&!ending&&!mini,Press);if(EntryScene)EntryScene.Tick(Time.deltaTime,!dialog&&!ending&&!mini,Press);if(SecondTemple)SecondTemple.Tick(Time.deltaTime,!dialog&&!ending&&!mini,Press);
            if (dialog)
            {
                typing += Time.unscaledDeltaTime * 38;
                dialogText.maxVisibleCharacters = Mathf.FloorToInt(typing);
                if (Press) AdvanceDialogue();
                return;
            }
            if (ending) return;
            if (mini) { UpdateMini(); return; }
            if (Key(UnityEngine.InputSystem.Key.J)) { ShowJournal(); return; }
            if(Baptism||Temptation||Meeting||Wedding||Temple||Synagogue||Fishing||Matthew||Storm||Feeding||WaterWalk||Confession||Prayer||ChildrenScene||EntryScene||SecondTemple)return;
            Vector2 move = Vector2.zero;
            if (K != null)
            {
                move.x = (K.dKey.isPressed || K.rightArrowKey.isPressed ? 1 : 0) - (K.aKey.isPressed || K.leftArrowKey.isPressed ? 1 : 0);
                move.y = (K.wKey.isPressed || K.upArrowKey.isPressed ? 1 : 0) - (K.sKey.isPressed || K.downArrowKey.isPressed ? 1 : 0);
            }
            bool walking = move.sqrMagnitude > 0;
            float speed = K != null && (K.leftShiftKey.isPressed || K.rightShiftKey.isPressed) ? 5.4f : 3.5f;
            Vector3 pos = player.position + (Vector3)(move.normalized * speed * Time.deltaTime);
            pos.x = Mathf.Clamp(pos.x, 1.5f, 46.5f); pos.y = Mathf.Clamp(pos.y, -3.72f, -2.84f); pos.z = 0;
            player.position = pos;
            if (move.x != 0) playerSprite.flipX = move.x < 0;
            if (currentActor == "예수님") SetFrame(playerSprite, JesusWalkFrame(walking), walking ? 0 : 0);
            else SetFrame(playerSprite, SpriteFor(currentActor), walking ? Mathf.Sin(Time.time * 14) * .025f : 0);
            playerSprite.sortingOrder = 20 + Mathf.RoundToInt(-player.position.y * 10);
            var trail = player.position + new Vector3(playerSprite.flipX ? 1.2f : -1.2f, .22f, 0);
            companion.position = Vector3.MoveTowards(companion.position, trail, Time.deltaTime * 3.1f);
            companionSprite.flipX = playerSprite.flipX;
            companionSprite.sortingOrder = 19 + Mathf.RoundToInt(-companion.position.y * 10);
            bool nearGoal = Mathf.Abs(player.position.x - objectiveX) < 1.7f;
            int d = NearestDiscovery();
            hudPrompt.text = nearGoal ? "E  ·  " + Step.objective : d >= 0 ? "E  ·  길 위의 기억 살펴보기" : "WASD / 방향키  이동     Shift  빠르게 걷기     J  여정 기록";
            goalGlow.color = new Color(gold.r,gold.g,gold.b,.5f + Mathf.Sin(Time.time * 3) * .25f);
            if (Press)
            {
                if (nearGoal) Interact();
                else if (d >= 0) Discover(d);
            }
        }
        void LateUpdate()
        {
            var continuous=GetComponent<JourneyContinuousWorld>();if(continuous&&continuous.WalkingToNext){continuous.ExitCamera();return;}
            if(Baptism){Baptism.UpdateCamera(Time.unscaledDeltaTime);return;}
            if(Temptation){Temptation.UpdateCamera(Time.unscaledDeltaTime);return;}
            if(SecondTemple){SecondTemple.UpdateCamera();return;}if(EntryScene){EntryScene.UpdateCamera();return;}if(ChildrenScene){ChildrenScene.UpdateCamera();return;}if(Prayer){Prayer.UpdateCamera();return;}if(Confession){Confession.UpdateCamera();return;}if(WaterWalk){WaterWalk.UpdateCamera();return;}if(Feeding){Feeding.UpdateCamera();return;}if(Storm){Storm.UpdateCamera();return;}if(Matthew){Matthew.UpdateCamera();return;}if(Fishing){Fishing.UpdateCamera();return;}if(Synagogue){Synagogue.UpdateCamera();return;}if(Temple){Temple.UpdateCamera();return;}if(Wedding){Wedding.UpdateCamera();return;}if(Meeting){Meeting.UpdateCamera();return;} if (!player || !journeyCamera) return;
            float half = journeyCamera.orthographicSize * journeyCamera.aspect;
            float x = Mathf.Clamp(player.position.x + 2.0f, half, 48 - half);
            var p = journeyCamera.transform.position;
            p.x = Mathf.Round(Mathf.Lerp(p.x, x, 1 - Mathf.Exp(-5 * Time.unscaledDeltaTime)) * 32) / 32;
            p.y = .05f; p.z = -10; journeyCamera.transform.position = p;
        }
        Sprite SpriteFor(string actor)
        {
            if (actor == "예수님") return authoredJesusIdle?authoredJesusIdle:jesusFrames[0];
            int index = Array.IndexOf(ActorNames, actor);
            if(index>=0)return disciples[index];
            int support=SupportIndex(actor);
            return support>=0&&supportingCast!=null&&support<supportingCast.Length?supportingCast[support]:null;
        }
        int SupportIndex(string name)
        {
            if(name=="마리아"||name=="아이의 보호자")return 0;
            if(name=="세례 요한")return 1;
            if(name=="막달라 마리아")return 2;
            if(name=="아이"||name=="아이들")return 3;
            if(name=="성전 상인"||name=="연회장"||name=="질문하는 사람"||name=="빌라도")return 5;
            if(name=="내레이션"||name=="강물"||name=="돌"||name=="성전 꼭대기"||name=="높은 산"||name=="돌항아리"||name=="막힌 통로"||name=="대야"||name=="수건"||name=="식탁"||name=="동산"||name=="기도"||name=="은화"||name=="십자가"||name=="빈 무덤")return -1;
            return 6;
        }
        void SetFrame(SpriteRenderer sr, Sprite frame, float bob)
        {
            sr.sprite = frame;
            if(!frame)return;
            bool child=supportingCast!=null&&supportingCast.Length>4&&(frame==supportingCast[3]||frame==supportingCast[4]);
            var view=sr.GetComponentInParent<JourneyCharacterView>();
            // Native frames were tightly sliced at different heights. Keep their pixel scale
            // stable instead of stretching each walking pose back to the idle height.
            Sprite reference=view&&view.idle&&(frame==view.idle||view.walkFrames.Contains(frame))?view.idle:
                authoredJesusIdle&&(frame==authoredJesusIdle||jesusFrames.Contains(frame))?authoredJesusIdle:frame;
            float scale = (view?view.displayHeight:child?1.2f:1.85f) * frame.pixelsPerUnit / reference.rect.height;
            sr.transform.localScale = Vector3.one * scale;
            var visible=JourneySpriteAssets.VisibleBounds(frame);
            sr.transform.localPosition = new Vector3(-visible.center.x * scale, -visible.min.y * scale + bob, 0)+(view?(Vector3)view.artworkOffset:Vector3.zero);
        }
        Transform Actor(string name, Vector3 position, out SpriteRenderer sr)
        {
            int definitionIndex=Array.IndexOf(ActorNames,name);
            string definitionName=name=="예수님"?"Jesus":definitionIndex>=0?"Disciple"+definitionIndex.ToString("00"):null;var prefab=definitionName!=null?JourneyAssets.Load<GameObject>("CrossTheLine/Characters/"+definitionName):null;
            var root = prefab?Instantiate(prefab):new GameObject(name);root.name=name;root.transform.SetParent(transform); root.transform.position = position;
            var chapterSprites=GetComponent<JourneyChapterSpriteSet>();var view=root.GetComponent<JourneyCharacterView>();if(name=="예수님"&&chapterSprites&&chapterSprites.jesusIdle&&view)view.idle=chapterSprites.jesusIdle;
            var body = root.transform.Find("Sprite")?root.transform.Find("Sprite").gameObject:new GameObject("Sprite"); body.transform.SetParent(root.transform, false);
            sr = body.GetOrAddComponent<SpriteRenderer>(); sr.sharedMaterial = actorMaterial; sr.sortingOrder = 50; SetFrame(sr, SpriteFor(name),0);
            int walkIndex=Array.IndexOf(ActorNames,name);if(walkIndex>=0)sr.gameObject.AddComponent<DiscipleWalking>().Initialize(walkIndex,root.transform);
            return root.transform;
        }
        void LoadWorld(bool showIntro)
        {
            var continuous=GetComponent<JourneyContinuousWorld>();if(continuous)continuous.Normalize();
            if(started&&!AnyPreview){save.discoveries=found.OrderBy(x=>x).ToArray();if(JourneySceneFlow.Route(save.chapter,JsonUtility.ToJson(save)))return;}
            JourneyObjectFactory.Reset();
            if(Baptism){Baptism.Shutdown();Destroy(Baptism.gameObject);Baptism=null;}
            if(Temptation){Temptation.Shutdown();Destroy(Temptation.gameObject);Temptation=null;}
            if(SecondTemple){SecondTemple.Shutdown();Destroy(SecondTemple.gameObject);SecondTemple=null;}if(EntryScene){EntryScene.Shutdown();Destroy(EntryScene.gameObject);EntryScene=null;}if(ChildrenScene){ChildrenScene.Shutdown();Destroy(ChildrenScene.gameObject);ChildrenScene=null;}if(Prayer){Prayer.Shutdown();Destroy(Prayer.gameObject);Prayer=null;}if(Confession){Confession.Shutdown();Destroy(Confession.gameObject);Confession=null;}if(WaterWalk){WaterWalk.Shutdown();Destroy(WaterWalk.gameObject);WaterWalk=null;}if(Feeding){Feeding.Shutdown();Destroy(Feeding.gameObject);Feeding=null;}if(Storm){Storm.Shutdown();Destroy(Storm.gameObject);Storm=null;}if(Matthew){Matthew.Shutdown();Destroy(Matthew.gameObject);Matthew=null;}if(Fishing){Fishing.Shutdown();Destroy(Fishing.gameObject);Fishing=null;}if(Synagogue){Synagogue.Shutdown();Destroy(Synagogue.gameObject);Synagogue=null;}if(Temple){Temple.Shutdown();Destroy(Temple.gameObject);Temple=null;}if(Wedding){Wedding.Shutdown();Destroy(Wedding.gameObject);Wedding=null;}if(Meeting){Meeting.Shutdown();Destroy(Meeting.gameObject);Meeting=null;} if (player) Destroy(player.gameObject);
            if (companion) Destroy(companion.gameObject);
            if (goal) Destroy(goal.gameObject);
            var old = transform.Find("Memories"); if (old) Destroy(old.gameObject);
            if(continuous){continuous.ApplyChapter(save.chapter);RefreshChapterSprites();}
            if(save.chapter==1||save.chapter==2||save.chapter==3||save.chapter==4||save.chapter==5||save.chapter==6||save.chapter==7||save.chapter==8||save.chapter==9||save.chapter==10||save.chapter==11||save.chapter==12||save.chapter==13||save.chapter==14||save.chapter==15||save.chapter==16)world.Dispose();else world.Build(Chapter.biome, save.chapter, actorMaterial);
            if(continuous&&world.Root)world.Root.transform.SetParent(transform,true);
            currentActor = Step.actor;
            spawnX = 3;
            player = Actor(currentActor, new Vector3(spawnX,-3.35f,0), out playerSprite);
            companion = Actor(currentActor == "예수님" ? "베드로" : "예수님", new Vector3(spawnX - 1.2f,-3.1f,0), out companionSprite);
            var memories = new GameObject("Memories"); memories.transform.SetParent(transform);
            for (int i=0; i<discoveryX.Length; i++)
            {
                var marker = new GameObject("Memory_" + i); marker.transform.SetParent(memories.transform); marker.transform.position = new Vector3(discoveryX[i],-2.87f,0);
                var sr = marker.AddComponent<SpriteRenderer>(); sr.sprite = dotSprite; sr.sharedMaterial = actorMaterial; sr.color = gold; sr.sortingOrder = 10; marker.transform.localScale = new Vector3(.11f,.20f,1);
            }
            journeyCamera.transform.position = new Vector3(10,.05f,-10);
            SetObjective(false);
            if(save.chapter==1)
            {
                var baptismObject=new GameObject("Chapter02_JordanBaptism");baptismObject.transform.SetParent(transform,false);
                Baptism=baptismObject.AddComponent<JordanBaptism>();
                Baptism.Initialize(this,player,playerSprite,actorMaterial,dotSprite,save.step,showIntro);
                if(companion)companion.gameObject.SetActive(false);if(goal)goal.gameObject.SetActive(false);memories.SetActive(false);
            }
            if(save.chapter==2)
            {
                var temptationObject=new GameObject("Chapter03_DesertTemptation");temptationObject.transform.SetParent(transform,false);
                Temptation=temptationObject.AddComponent<DesertTemptation>();Temptation.Initialize(this,player,playerSprite,actorMaterial,dotSprite,save.step,showIntro);
                if(companion)companion.gameObject.SetActive(false);if(goal)goal.gameObject.SetActive(false);memories.SetActive(false);
            }
            if(save.chapter==3){var o=new GameObject("Chapter04_FirstDisciples");o.transform.SetParent(transform,false);Meeting=o.AddComponent<FirstDisciples>();Meeting.Initialize(this,player,playerSprite,actorMaterial,dotSprite,save.step);if(companion)companion.gameObject.SetActive(false);if(goal)goal.gameObject.SetActive(false);memories.SetActive(false);}
            if(save.chapter==4){var o=new GameObject("Chapter05_CanaWedding");o.transform.SetParent(transform,false);Wedding=o.AddComponent<CanaWedding>();Wedding.Initialize(this,player,playerSprite,actorMaterial,dotSprite,save.step);if(companion)companion.gameObject.SetActive(false);if(goal)goal.gameObject.SetActive(false);memories.SetActive(false);}
            if(save.chapter==5){var o=new GameObject("Chapter06_TempleCleansing");o.transform.SetParent(transform,false);Temple=o.AddComponent<TempleCleansing>();Temple.Initialize(this,player,playerSprite,actorMaterial,dotSprite,save.step);if(companion)companion.gameObject.SetActive(false);if(goal)goal.gameObject.SetActive(false);memories.SetActive(false);}
            if(save.chapter==6){var o=new GameObject("Chapter07_SynagogueDispute");o.transform.SetParent(transform,false);Synagogue=o.AddComponent<SynagogueDispute>();Synagogue.Initialize(this,player,playerSprite,actorMaterial,dotSprite,save.step);if(companion)companion.gameObject.SetActive(false);if(goal)goal.gameObject.SetActive(false);memories.SetActive(false);}
            if(save.chapter==7){var o=new GameObject("Chapter08_MiraculousCatch");o.transform.SetParent(transform,false);Fishing=o.AddComponent<MiraculousCatch>();Fishing.Initialize(this,player,playerSprite,actorMaterial,dotSprite,save.step);if(companion)companion.gameObject.SetActive(false);if(goal)goal.gameObject.SetActive(false);memories.SetActive(false);}
            if(save.chapter==8){var o=new GameObject("Chapter09_MatthewCalling");o.transform.SetParent(transform,false);Matthew=o.AddComponent<MatthewCalling>();Matthew.Initialize(this,player,playerSprite,actorMaterial,dotSprite,save.step);if(companion)companion.gameObject.SetActive(false);if(goal)goal.gameObject.SetActive(false);memories.SetActive(false);}
            if(save.chapter==9){var o=new GameObject("Chapter10_StormAtSea");o.transform.SetParent(transform,false);Storm=o.AddComponent<StormAtSea>();Storm.Initialize(this,player,playerSprite,actorMaterial,dotSprite,save.step);if(companion)companion.gameObject.SetActive(false);if(goal)goal.gameObject.SetActive(false);memories.SetActive(false);}
            if(save.chapter==10){var o=new GameObject("Chapter11_FeedingMultitude");o.transform.SetParent(transform,false);Feeding=o.AddComponent<FeedingMultitude>();Feeding.Initialize(this,player,playerSprite,actorMaterial,dotSprite,save.step);if(companion)companion.gameObject.SetActive(false);if(goal)goal.gameObject.SetActive(false);memories.SetActive(false);}
            if(save.chapter==11){var o=new GameObject("Chapter12_WaterWalking");o.transform.SetParent(transform,false);WaterWalk=o.AddComponent<WaterWalking>();WaterWalk.Initialize(this,player,playerSprite,actorMaterial,dotSprite,save.step);if(companion)companion.gameObject.SetActive(false);if(goal)goal.gameObject.SetActive(false);memories.SetActive(false);}
            if(save.chapter==12){var o=new GameObject("Chapter13_PeterConfession");o.transform.SetParent(transform,false);Confession=o.AddComponent<PeterConfession>();Confession.Initialize(this,player,playerSprite,actorMaterial,dotSprite,save.step);if(companion)companion.gameObject.SetActive(false);if(goal)goal.gameObject.SetActive(false);memories.SetActive(false);}
            if(save.chapter==13){var o=new GameObject("Chapter14_LordsPrayer");o.transform.SetParent(transform,false);Prayer=o.AddComponent<LordsPrayer>();Prayer.Initialize(this,player,playerSprite,actorMaterial,dotSprite,save.step);if(companion)companion.gameObject.SetActive(false);if(goal)goal.gameObject.SetActive(false);memories.SetActive(false);}
            if(save.chapter==14){var o=new GameObject("Chapter15_ChildrenBlessing");o.transform.SetParent(transform,false);ChildrenScene=o.AddComponent<ChildrenBlessing>();ChildrenScene.Initialize(this,player,playerSprite,actorMaterial,dotSprite,save.step);if(companion)companion.gameObject.SetActive(false);if(goal)goal.gameObject.SetActive(false);memories.SetActive(false);}
            if(save.chapter==15){var o=new GameObject("Chapter16_JerusalemEntry");o.transform.SetParent(transform,false);EntryScene=o.AddComponent<JerusalemEntry>();EntryScene.Initialize(this,player,playerSprite,actorMaterial,dotSprite,save.step);if(companion)companion.gameObject.SetActive(false);if(goal)goal.gameObject.SetActive(false);memories.SetActive(false);}
            if(save.chapter==16){var o=new GameObject("Chapter17_SecondTempleCleansing");o.transform.SetParent(transform,false);SecondTemple=o.AddComponent<SecondTempleCleansing>();SecondTemple.Initialize(this,player,playerSprite,actorMaterial,dotSprite,save.step);if(companion)companion.gameObject.SetActive(false);if(goal)goal.gameObject.SetActive(false);memories.SetActive(false);}
            if (save.step >= Chapter.steps.Length) ShowEnding();
            else if (showIntro&&save.chapter!=1&&save.chapter!=2&&save.chapter!=3&&save.chapter!=4&&save.chapter!=5&&save.chapter!=6&&save.chapter!=7&&save.chapter!=8&&save.chapter!=9&&save.chapter!=10&&save.chapter!=11&&save.chapter!=12&&save.chapter!=13&&save.chapter!=14&&save.chapter!=15&&save.chapter!=16) Say(new[]{"내레이션|" + Chapter.introduction, "길 안내|" + Step.objective + " — 가까이 가서 E를 누르세요."}, null);
        }
        void SetObjective(bool switchActor)
        {
            if (save.step >= Chapter.steps.Length) return;
            bool changed = currentActor != Step.actor;
            currentActor = Step.actor;
            SetFrame(playerSprite, SpriteFor(currentActor),0); player.name = currentActor; var walker=playerSprite.GetComponent<DiscipleWalking>();if(walker)Destroy(walker);int wi=Array.IndexOf(ActorNames,currentActor);if(wi>=0)playerSprite.gameObject.AddComponent<DiscipleWalking>().Initialize(wi,player);
            SetFrame(companionSprite, SpriteFor(currentActor == "예수님" ? "베드로" : "예수님"),0); var cw=companionSprite.GetComponent<DiscipleWalking>();if(cw)Destroy(cw);if(currentActor=="예수님")companionSprite.gameObject.AddComponent<DiscipleWalking>().Initialize(0,companion);
            objectiveX = new[]{ 6.5f,18f,34f,42f,25f }[save.step % 5];
            if (goal) Destroy(goal.gameObject);
            goal = new GameObject("Objective_" + Step.person).transform; goal.SetParent(transform); goal.position = new Vector3(objectiveX,-3.25f,0);
            // Story figures use a visible cast sprite only when that named cast member has art.
            if (SpriteFor(Step.person))
            {
                var body = new GameObject(Step.person); body.transform.SetParent(goal,false);
                var sr = body.AddComponent<SpriteRenderer>(); sr.sharedMaterial=actorMaterial; sr.sortingOrder=45; SetFrame(sr,SpriteFor(Step.person),0); sr.flipX=true;
            }
            else
            {
                var stone = new GameObject("StoryMarker"); stone.transform.SetParent(goal,false); stone.transform.localPosition=new Vector3(0,.12f,0);
                var sr = stone.AddComponent<SpriteRenderer>(); sr.sharedMaterial=actorMaterial; sr.sprite=dotSprite; sr.color=teal; sr.sortingOrder=30; stone.transform.localScale=new Vector3(.58f,.22f,1);
            }
            var light = new GameObject("InteractionLight"); light.transform.SetParent(goal,false); light.transform.localPosition=new Vector3(0,2.20f,0); light.transform.localScale=new Vector3(.16f,.16f,1);
            goalGlow=light.AddComponent<SpriteRenderer>(); goalGlow.sprite=dotSprite; goalGlow.sharedMaterial=actorMaterial; goalGlow.sortingOrder=80; goalGlow.color=gold;
            var nameplate=new GameObject("Nameplate",typeof(RectTransform),typeof(Canvas));nameplate.transform.SetParent(goal,false);nameplate.transform.localPosition=new Vector3(0,2.57f,0);nameplate.transform.localScale=Vector3.one*.008f;
            var plateCanvas=nameplate.GetComponent<Canvas>();plateCanvas.renderMode=RenderMode.WorldSpace;plateCanvas.worldCamera=journeyCamera;plateCanvas.overrideSorting=true;plateCanvas.sortingOrder=85;
            nameplate.GetComponent<RectTransform>().sizeDelta=new Vector2(260,50);
            Label(nameplate.transform,"Name",Step.person,24,paper,Vector2.zero,Vector2.one,TextAlignmentOptions.Center);
            hudTitle.text = $"{save.chapter+1:00}  /  {book.chapters.Length:00}     {Chapter.title}";
            hudObjective.text = Step.objective;
            hudActor.text = "지금의 시점  ·  " + currentActor;
            hudProgress.text = Chapter.location + "     ·     " + (save.step+1) + " / " + Chapter.steps.Length;
            hudPrompt.color=paper;
            if (switchActor && changed) Notify("시점 전환  →  " + currentActor);
            if(Baptism){goal.gameObject.SetActive(false);Baptism.ConfigureStep(save.step);}
            if(Temptation){goal.gameObject.SetActive(false);Temptation.ConfigureStep(save.step);} if(Meeting){goal.gameObject.SetActive(false);Meeting.ConfigureStep(save.step);}if(Wedding){goal.gameObject.SetActive(false);Wedding.ConfigureStep(save.step);}if(Temple){goal.gameObject.SetActive(false);Temple.ConfigureStep(save.step);}if(Synagogue){goal.gameObject.SetActive(false);Synagogue.ConfigureStep(save.step);}if(Fishing){goal.gameObject.SetActive(false);Fishing.ConfigureStep(save.step);}if(Matthew){goal.gameObject.SetActive(false);Matthew.ConfigureStep(save.step);}if(Storm){goal.gameObject.SetActive(false);Storm.ConfigureStep(save.step);}if(Feeding){goal.gameObject.SetActive(false);Feeding.ConfigureStep(save.step);}if(WaterWalk){goal.gameObject.SetActive(false);WaterWalk.ConfigureStep(save.step);}if(Confession){goal.gameObject.SetActive(false);Confession.ConfigureStep(save.step);}if(Prayer){goal.gameObject.SetActive(false);Prayer.ConfigureStep(save.step);}if(ChildrenScene){goal.gameObject.SetActive(false);ChildrenScene.ConfigureStep(save.step);}if(EntryScene){goal.gameObject.SetActive(false);EntryScene.ConfigureStep(save.step);}if(SecondTemple){goal.gameObject.SetActive(false);SecondTemple.ConfigureStep(save.step);}
        }
        int NearestDiscovery()
        {
            for(int i=0;i<discoveryX.Length;i++) if(Mathf.Abs(player.position.x-discoveryX[i])<1.25f && !found.Contains(save.chapter+":"+i)) return i;
            return -1;
        }
        void Discover(int i)
        {
            found.Add(save.chapter+":"+i); WriteSave(); Ping();
            Say(new[]{"길 위의 기억|"+Chapter.discoveries[i]},()=>Notify("여정 기록에 기억을 남겼습니다."));
        }
        public void Interact()
        {
            if (menu || dialog || mini || ending || !started) return;
            if(Baptism){Baptism.Interact();return;}
            if(Temptation){Temptation.Interact();return;} if(Meeting){Meeting.Interact();return;}if(Wedding){Wedding.Interact();return;}if(Temple){Temple.Interact();return;}if(Synagogue){Synagogue.Interact();return;}if(Fishing){Fishing.Interact();return;}if(Matthew){Matthew.Interact();return;}if(Storm){Storm.Interact();return;}if(Feeding){Feeding.Interact();return;}if(WaterWalk){WaterWalk.Interact();return;}if(Confession){Confession.Interact();return;}if(Prayer){Prayer.Interact();return;}if(ChildrenScene){ChildrenScene.Interact();return;}if(EntryScene){EntryScene.Interact();return;}if(SecondTemple){SecondTemple.Interact();return;}
            var step = Step;
            Say(step.dialogue,()=> { if(step.kind=="talk") CompleteStep(); else BeginMini(step.kind); });
        }
        void Say(IEnumerable<string> lines, Action onDone)
        {
            if(dialog) Destroy(dialog);
            dialogue.Clear(); dialogue.AddRange(lines); dialogueIndex=0; dialogueFinished=onDone;
            dialog = Panel(ui,"Dialogue",new Vector2(.055f,.065f),new Vector2(.945f,.30f),paper);
            Strip(dialog.transform,new Vector2(0,0),new Vector2(.008f,1),gold);
            dialogSpeaker = Label(dialog.transform,"Speaker","",23,teal,new Vector2(.035f,.71f),new Vector2(.90f,.94f));
            dialogText = Label(dialog.transform,"Line","",25,ink,new Vector2(.035f,.20f),new Vector2(.955f,.70f));
            dialogHint = Label(dialog.transform,"Hint","E / Space  ·  계속",15,teal,new Vector2(.68f,.025f),new Vector2(.95f,.16f),TextAlignmentOptions.Right);
            var button = dialog.AddComponent<UnityEngine.UI.Button>(); button.targetGraphic=dialog.GetComponent<UnityEngine.UI.Image>(); button.onClick.AddListener(AdvanceDialogue);
            dialog.GetComponent<UnityEngine.UI.Image>().raycastTarget=true;
            ShowLine();
            if(Confession)Confession.DecorateDialogue(dialog.transform,dialogSpeaker.rectTransform,dialogText.rectTransform);if(ChildrenScene)ChildrenScene.DecorateDialogue(dialog.transform,dialogSpeaker.rectTransform,dialogText.rectTransform);if(EntryScene)EntryScene.DecorateDialogue(dialog.transform,dialogSpeaker.rectTransform,dialogText.rectTransform);if(SecondTemple)SecondTemple.DecorateDialogue(dialog.transform,dialogSpeaker.rectTransform,dialogText.rectTransform);
        }
        void ShowLine()
        {
            string line=dialogue[dialogueIndex]; int sep=line.IndexOf('|');
            dialogSpeaker.text=sep<0?"이야기":line.Substring(0,sep); dialogText.text=sep<0?line:line.Substring(sep+1);
            typing=0; dialogText.maxVisibleCharacters=0;
        }
        public void AdvanceDialogue()
        {
            if(!dialog || menu) return;
            if(typing < dialogText.text.Length+1) { typing=100000; dialogText.maxVisibleCharacters=int.MaxValue; return; }
            dialogueIndex++;
            if(dialogueIndex<dialogue.Count) { ShowLine(); return; }
            Destroy(dialog); dialog=null;
            var done=dialogueFinished; dialogueFinished=null; done?.Invoke();
        }
        void CompleteStep()
        {
            if(Temptation&&save.step==2&&!Temptation.VictoryFinished){Temptation.StartVictory();return;}
            save.step++; WriteSave(); Ping();
            if(save.step>=Chapter.steps.Length) ShowEnding();
            else SetObjective(true);
        }
        void ShowEnding()
        {
            save.unlocked=Mathf.Max(save.unlocked,Mathf.Min(save.chapter+1,book.chapters.Length-1));
            if(save.chapter==book.chapters.Length-1) save.finished=true;
            WriteSave();
            if(save.chapter==2){NextChapter();return;}
            var continuous=GetComponent<JourneyContinuousWorld>();if(continuous&&continuous.BeginExit()){SetHudVisible(true);return;}
            ending=Panel(ui,"ChapterComplete",new Vector2(.20f,.23f),new Vector2(.80f,.77f),paper);
            Label(ending.transform,"Eyebrow",save.finished?"CROSS THE LINE  ·  여정의 끝":"CHAPTER COMPLETE",17,teal,new Vector2(.07f,.81f),new Vector2(.93f,.91f));
            Label(ending.transform,"Title",Chapter.title,35,ink,new Vector2(.07f,.60f),new Vector2(.93f,.79f));
            Label(ending.transform,"Ending",Chapter.ending,23,ink,new Vector2(.07f,.27f),new Vector2(.93f,.59f));
            Button(ending.transform,(baptismPreviewOnly||temptationPreviewOnly||disciplesPreviewOnly||weddingPreviewOnly||templePreviewOnly||synagoguePreviewOnly||fishingPreviewOnly||matthewPreviewOnly||stormPreviewOnly||feedingPreviewOnly||waterWalkingPreviewOnly||confessionPreviewOnly||prayerPreviewOnly||childrenPreviewOnly||entryPreviewOnly||secondTemplePreviewOnly)?"장면 다시 보기":save.finished?"여정 기록 돌아보기":"다음 길로",new Vector2(.07f,.08f),new Vector2(.58f,.22f),()=>{if(baptismPreviewOnly||temptationPreviewOnly||disciplesPreviewOnly||weddingPreviewOnly||templePreviewOnly||synagoguePreviewOnly||fishingPreviewOnly||matthewPreviewOnly||stormPreviewOnly||feedingPreviewOnly||waterWalkingPreviewOnly||confessionPreviewOnly||prayerPreviewOnly||childrenPreviewOnly||entryPreviewOnly||secondTemplePreviewOnly)NewGame();else if(save.finished)ShowJournal();else NextChapter();});
            Button(ending.transform,"메뉴",new Vector2(.63f,.08f),new Vector2(.93f,.22f),ShowPause,false);
        }
        public void NextChapter()
        {
            if(save.chapter>=book.chapters.Length-1) return;
            if(ending) Destroy(ending); ending=null;
            save.chapter++; save.step=0; WriteSave(); LoadWorld(true);
        }
        void BeginMini(string kind)
        {
            miniKind=kind; miniTime=miniProgress=danger=balance=0; tension=.32f; wins=prayerIndex=0; lastServeFrame=-1;
            mini=Panel(ui,"MiniGame_"+kind,new Vector2(.17f,.22f),new Vector2(.83f,.78f),paper);
            Label(mini.transform,"Actor","지금의 시점  ·  "+currentActor,18,teal,new Vector2(.06f,.86f),new Vector2(.94f,.95f));
            miniLabel=Label(mini.transform,"Title",Step.objective,32,ink,new Vector2(.06f,.70f),new Vector2(.94f,.85f));
            miniFeedback=Label(mini.transform,"Feedback","",22,ink,new Vector2(.06f,.51f),new Vector2(.94f,.68f));
            miniHint=Label(mini.transform,"Help","",18,teal,new Vector2(.06f,.10f),new Vector2(.94f,.22f));
            if(kind=="choice")
            {
                miniFeedback.text=Step.hint;
                for(int i=0;i<Step.choices.Length;i++) { int index=i; Button(mini.transform,(i+1)+"  "+Step.choices[i],new Vector2(.06f,.38f-i*.115f),new Vector2(.94f,.48f-i*.115f),()=>Answer(index)); }
                miniHint.text="숫자 1–3 또는 버튼  ·  말씀의 의미를 살펴보세요";
                return;
            }
            if(kind=="prayer") { RebuildPrayer(); return; }
            var track=Panel(mini.transform,"Track",new Vector2(.07f,.38f),new Vector2(.93f,.44f),new Color(.70f,.72f,.63f));
            miniTarget=Strip(track.transform,new Vector2(.35f,0),new Vector2(.65f,1),new Color(.41f,.63f,.52f));
            miniFill=Strip(track.transform,new Vector2(0,0),new Vector2(.01f,1),gold);
            miniPointer=Strip(track.transform,new Vector2(.48f,-.25f),new Vector2(.49f,1.25f),ink);
            if(kind=="fishing")
            {
                miniHint.text="Space / E를 누르면 당김 · 놓으면 힘을 풉니다\n초록 범위에서 당기고, 장력이 커지면 쉬세요.";
                var b=Button(mini.transform,"누르고 당기기",new Vector2(.28f,.23f),new Vector2(.72f,.34f),()=>{});
                var hold=b.gameObject.AddComponent<JourneyHoldButton>(); hold.onHeld=v=>pointerHeld=v;
            }
            else if(kind=="serve")
            {
                miniHint.text="표시가 초록 구간에 들어오면 Space / E 또는 버튼\n서두르지 않아도 됩니다. 놓치면 다시 기다리세요.";
                Button(mini.transform,"맞춰서 전하기",new Vector2(.28f,.23f),new Vector2(.72f,.34f),ServeBeat);
            }
            else
            {
                miniHint.text=kind=="balance"?"A / D 또는 ← / →로 기울기를 되돌리세요\n초록 범위에서 15초 동안 균형을 유지합니다.":"A / D 또는 ← / →로 흔들리는 시선을 중앙에\n초록 범위에서 15초 동안 마음을 모읍니다.";
                var left=Button(mini.transform,"←",new Vector2(.18f,.23f),new Vector2(.42f,.34f),()=>{}).gameObject.AddComponent<JourneyHoldButton>(); left.onHeld=v=>leftHeld=v;
                var right=Button(mini.transform,"→",new Vector2(.58f,.23f),new Vector2(.82f,.34f),()=>{}).gameObject.AddComponent<JourneyHoldButton>(); right.onHeld=v=>rightHeld=v;
            }
        }
        bool pointerHeld,leftHeld,rightHeld;
        void UpdateMini()
        {
            miniTime+=Time.deltaTime;
            if(miniKind=="choice") { if(Key(UnityEngine.InputSystem.Key.Digit1))Answer(0); else if(Key(UnityEngine.InputSystem.Key.Digit2))Answer(1); else if(Key(UnityEngine.InputSystem.Key.Digit3))Answer(2); return; }
            if(miniKind=="prayer") return;
            float dt=Time.deltaTime;
            if(miniKind=="fishing")
            {
                bool pulling=Held||pointerHeld;
                tension=Mathf.Clamp01(tension+dt*(pulling ? .28f + .08f*Mathf.Sin(miniTime*1.9f) : -.32f));
                if(pulling && tension>.18f && tension<.86f) miniProgress+=dt/(gentle?12f:17f);
                danger=tension>.94f?danger+dt:Mathf.Max(0,danger-dt*2);
                SetGauge(tension,miniProgress);
                miniFeedback.text=$"그물 {Mathf.RoundToInt(miniProgress*100)}%  ·  장력 {Mathf.RoundToInt(tension*100)}%";
                if(danger>(gentle?1.6f:1f)) { RetryMini("힘이 너무 강했습니다. 잠깐 쉬었다 다시 당겨요."); return; }
            }
            else if(miniKind=="serve")
            {
                SetGauge(BeatPosition(),wins/6f);
                if(Press) ServeBeat();
                if(!mini) return;
                miniLabel.text=Step.objective+"  ·  "+wins+" / 6";
                return;
            }
            else
            {
                float steer=(rightHeld?1:0)-(leftHeld?1:0);
                if(K!=null) steer+=(K.dKey.isPressed||K.rightArrowKey.isPressed?1:0)-(K.aKey.isPressed||K.leftArrowKey.isPressed?1:0);
                float wind=(Mathf.Sin(miniTime*1.45f)*.40f+Mathf.Sin(miniTime*2.7f+.8f)*.26f)*(gentle?.65f:1);
                balance=Mathf.Clamp(balance+dt*(wind+Mathf.Clamp(steer,-1,1)*.75f),-1,1);
                float safe=gentle?.63f:.45f;
                if(Mathf.Abs(balance)<safe)miniProgress+=dt/15f;
                danger=Mathf.Abs(balance)>.94f?danger+dt:0;
                SetGauge((balance+1)/2,miniProgress);
                miniFeedback.text=$"{Mathf.RoundToInt(miniProgress*15)} / 15초  ·  "+(Mathf.Abs(balance)<safe?"안정적입니다":"반대편으로 되돌리세요");
                if(danger>2){RetryMini("다시 마음을 모아 봅시다. 천천히 되돌려도 괜찮아요.");return;}
            }
            if(miniProgress>=1)FinishMini();
        }
        float BeatPosition()=>.5f+Mathf.Sin(miniTime*(gentle?1.65f:2.25f))*.48f;
        void SetGauge(float pointer,float progress)
        {
            float width=gentle?.19f:.12f;
            if(miniKind=="balance"||miniKind=="focus")width=(gentle?.63f:.45f)/2;
            miniTarget.anchorMin=new Vector2(miniKind=="fishing"?.18f:.5f-width,0);miniTarget.anchorMax=new Vector2(miniKind=="fishing"?.86f:.5f+width,1);
            miniPointer.anchorMin=new Vector2(Mathf.Clamp01(pointer)-.005f,-.2f);miniPointer.anchorMax=new Vector2(Mathf.Clamp01(pointer)+.005f,1.2f);
            miniFill.anchorMax=new Vector2(Mathf.Clamp01(progress),1);
        }
        void ServeBeat()
        {
            if(!mini||miniKind!="serve"||menu)return;
            if(lastServeFrame==Time.frameCount)return;
            lastServeFrame=Time.frameCount;
            float p=BeatPosition();
            if(Mathf.Abs(p-.5f)<(gentle?.19f:.12f)) { wins++; Ping(); miniFeedback.text="잘 전했습니다."; }
            else miniFeedback.text="조금 기다렸다가 초록 구간에서 다시 눌러 보세요.";
            if(wins>=6)FinishMini();
        }
        public void Answer(int index)
        {
            if(!mini || menu || miniKind!="choice")return;
            if(index==Step.answer)FinishMini(); else miniFeedback.text="응답의 의미를 다시 살펴보세요.\n"+Step.hint;
        }
        void RebuildPrayer()
        {
            var old=mini.transform.Find("PrayerChoices");if(old)Destroy(old.gameObject);
            miniFeedback.text="흐름: 아버지 → 이름·나라·뜻 → 양식 → 용서 → 보호\n지금: "+(prayerIndex+1)+" / 5";
            miniHint.text="문구를 외우지 않아도 됩니다. 위의 흐름을 참고하세요.";
            var parent=Panel(mini.transform,"PrayerChoices",new Vector2(.06f,.24f),new Vector2(.94f,.49f),new Color(0,0,0,0));
            int[] order={3,0,4,1,2};
            for(int i=0;i<5;i++)
            {
                int answer=order[i];int row=i/3,col=i%3;
                var b=Button(parent.transform,prayerParts[answer],new Vector2(col/3f,row==0?.53f:0),new Vector2((col+1)/3f-.018f,row==0?1:.47f),()=>PrayerAnswer(answer),false);
                b.interactable=answer>=prayerIndex;
            }
        }
        public void PrayerAnswer(int answer)
        {
            if(!mini||miniKind!="prayer"||menu)return;
            if(answer==prayerIndex){prayerIndex++;Ping();if(prayerIndex>=5)FinishMini();else RebuildPrayer();}
            else miniFeedback.text="안내에 있는 다음 흐름을 찾아 보세요.\n아버지 → 이름·나라·뜻 → 양식 → 용서 → 보호";
        }
        void RetryMini(string message)
        {
            miniProgress=danger=balance=0;tension=.32f;miniFeedback.text=message;Notify(message);
        }
        void FinishMini()
        {
            Destroy(mini);mini=null;pointerHeld=leftHeld=rightHeld=false;
            Notify("함께 해냈습니다. 이야기를 이어갑니다.");CompleteStep();
        }

        void BuildUI()
        {
            if(sceneBindings)
            {
                sceneBindings.Bind(this);canvas=sceneBindings.canvas;ui=canvas.GetComponent<RectTransform>();
                headerPanel=sceneBindings.header;objectivePanel=sceneBindings.objective;journalButton=sceneBindings.journalButton;menuButton=sceneBindings.menuButton;
                hudTitle=sceneBindings.chapterTitle;hudProgress=sceneBindings.location;hudObjective=sceneBindings.objectiveText;hudActor=sceneBindings.actorText;hudPrompt=sceneBindings.controls;toast=sceneBindings.toast;
                journalButton.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(()=>{if(started)ShowJournal();});menuButton.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(()=>{if(started)ShowPause();});
                return;
            }
            var root=new GameObject("JourneyUI",typeof(RectTransform),typeof(Canvas),typeof(UnityEngine.UI.CanvasScaler),typeof(UnityEngine.UI.GraphicRaycaster));
            root.transform.SetParent(transform);canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=100;
            var scaler=root.GetComponent<UnityEngine.UI.CanvasScaler>();scaler.uiScaleMode=UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1440,900);scaler.matchWidthOrHeight=.5f;
            ui=root.GetComponent<RectTransform>();
            var header=Panel(ui,"JourneyHeader",new Vector2(.035f,.85f),new Vector2(.70f,.965f),new Color(ink.r,ink.g,ink.b,.94f));
            headerPanel=header;
            hudTitle=Label(header.transform,"Chapter","",24,paper,new Vector2(.035f,.53f),new Vector2(.965f,.91f));
            hudProgress=Label(header.transform,"Location","",16,new Color(.73f,.80f,.72f),new Vector2(.035f,.15f),new Vector2(.965f,.49f));
            var objective=Panel(ui,"Objective",new Vector2(.035f,.745f),new Vector2(.61f,.83f),new Color(paper.r,paper.g,paper.b,.96f));
            objectivePanel=objective;
            hudObjective=Label(objective.transform,"ObjectiveText","",22,ink,new Vector2(.025f,.39f),new Vector2(.97f,.90f));
            hudActor=Label(objective.transform,"ActorText","",16,teal,new Vector2(.025f,.06f),new Vector2(.97f,.41f));
            journalButton=Button(ui,"여정 기록",new Vector2(.74f,.90f),new Vector2(.87f,.955f),()=>{if(started)ShowJournal();},false).gameObject;
            menuButton=Button(ui,"메뉴",new Vector2(.885f,.90f),new Vector2(.965f,.955f),()=>{if(started)ShowPause();},false).gameObject;
            hudPrompt=Label(ui,"Controls","",19,paper,new Vector2(.055f,.035f),new Vector2(.945f,.08f),TextAlignmentOptions.Center);
            var promptOutline=hudPrompt.gameObject.AddComponent<UnityEngine.UI.Shadow>();promptOutline.effectColor=new Color(0,0,0,.75f);promptOutline.effectDistance=new Vector2(1,-1);
            toast=Label(ui,"Toast","",22,gold,new Vector2(.12f,.64f),new Vector2(.88f,.71f),TextAlignmentOptions.Center);
            toast.gameObject.AddComponent<UnityEngine.UI.Shadow>();toast.gameObject.SetActive(false);
        }
        GameObject Panel(Transform parent,string name,Vector2 min,Vector2 max,Color color)
        {
            var go=sceneBindings?sceneBindings.MakeUI("Panel",name,parent):new GameObject(name,typeof(RectTransform),typeof(UnityEngine.UI.Image));go.transform.SetParent(parent,false);
            var r=go.GetComponent<RectTransform>();r.anchorMin=min;r.anchorMax=max;r.offsetMin=r.offsetMax=Vector2.zero;
            var image=go.GetComponent<UnityEngine.UI.Image>();image.color=color;image.raycastTarget=false;return go;
        }
        RectTransform Strip(Transform p,Vector2 min,Vector2 max,Color color)=>Panel(p,"Accent",min,max,color).GetComponent<RectTransform>();
        TMP_Text Label(Transform p,string name,string text,int size,Color color,Vector2 min,Vector2 max,TextAlignmentOptions align=TextAlignmentOptions.Left)
        {
            var existing=p.Find(name);var go=existing?existing.gameObject:sceneBindings?sceneBindings.MakeUI("Text",name,p):new GameObject(name,typeof(RectTransform));go.transform.SetParent(p,false);
            var r=go.GetComponent<RectTransform>();r.anchorMin=min;r.anchorMax=max;r.offsetMin=r.offsetMax=Vector2.zero;
            var t=go.GetComponent<TextMeshProUGUI>()??go.AddComponent<TextMeshProUGUI>();t.font=koreanFont;t.text=text;t.fontSize=size;t.color=color;t.alignment=align;t.textWrappingMode=TextWrappingModes.Normal;t.overflowMode=TextOverflowModes.Truncate;t.raycastTarget=false;return t;
        }
        UnityEngine.UI.Button Button(Transform p,string text,Vector2 min,Vector2 max,Action action,bool primary=true)
        {
            var go=sceneBindings?sceneBindings.MakeUI("Button","Button_"+text,p):Panel(p,"Button_"+text,min,max,primary?teal:new Color(.83f,.81f,.70f));
            var rect=go.GetComponent<RectTransform>();rect.anchorMin=min;rect.anchorMax=max;rect.offsetMin=rect.offsetMax=Vector2.zero;
            var image=go.GetComponent<UnityEngine.UI.Image>();image.raycastTarget=true;
            image.color=primary?teal:new Color(.83f,.81f,.70f);var b=go.GetOrAddComponent<UnityEngine.UI.Button>();b.targetGraphic=image;
            var colors=b.colors;colors.highlightedColor=new Color(1,.94f,.75f);colors.pressedColor=new Color(.72f,.82f,.78f);colors.disabledColor=new Color(.6f,.6f,.6f,.55f);b.colors=colors;
            Label(go.transform,"Text",text,20,primary?paper:ink,new Vector2(.04f,.08f),new Vector2(.96f,.92f),TextAlignmentOptions.Center);
            b.onClick.AddListener(()=>{ UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(null); action(); });return b;
        }
        void BeginMenu(string name)
        {
            DisposeMenu();
            SetHudVisible(false);
            if(Baptism)Baptism.SetPaused(true);
            menu=Panel(ui,name,Vector2.zero,Vector2.one,new Color(ink.r,ink.g,ink.b,.80f));
            menu.GetComponent<UnityEngine.UI.Image>().raycastTarget=true;
        }
        public void ShowHome()
        {
            if(sceneChapterIndex>=1){started=false;JourneySceneFlow.OpenHome();return;}
            DisposeMenu();
            ClearStoryPanels();started=false;
            baptismPreviewOnly=initialBaptismPreview;temptationPreviewOnly=initialTemptationPreview;disciplesPreviewOnly=initialDisciplesPreview;weddingPreviewOnly=initialWeddingPreview;templePreviewOnly=initialTemplePreview;synagoguePreviewOnly=initialSynagoguePreview;fishingPreviewOnly=initialFishingPreview;matthewPreviewOnly=initialMatthewPreview;stormPreviewOnly=initialStormPreview;feedingPreviewOnly=initialFeedingPreview;waterWalkingPreviewOnly=initialWaterWalkingPreview;confessionPreviewOnly=initialConfessionPreview;prayerPreviewOnly=initialPrayerPreview;childrenPreviewOnly=initialChildrenPreview;entryPreviewOnly=initialEntryPreview;secondTemplePreviewOnly=initialSecondTemplePreview;
            if(sceneBindings){SetHudVisible(false);if(Baptism)Baptism.SetPaused(true);menu=sceneBindings.titlePanel;sceneBindings.OpenTitle(!AnyPreview&&File.Exists(SavePath));return;}
            BeginMenu("TitleMenu");
            Label(menu.transform,"Brand","DOUBLE MONKEY  /  STORY ADVENTURE",17,gold,new Vector2(.08f,.84f),new Vector2(.85f,.89f));
            Label(menu.transform,"Title","CROSS\nTHE LINE",82,paper,new Vector2(.08f,.53f),new Vector2(.76f,.83f));
            Label(menu.transform,"Subtitle","한 걸음의 믿음, 함께 걷는 이야기",26,paper,new Vector2(.085f,.45f),new Vector2(.83f,.51f));
            Label(menu.transform,"Description","예수님의 길을 걷고, 제자의 자리에서 경험하세요.\n탐험 · 대화 · 시점 전환 · 작은 도전",20,new Color(.75f,.82f,.77f),new Vector2(.085f,.34f),new Vector2(.80f,.43f));
            Button(menu.transform,secondTemplePreviewOnly?"두 번째 성전 청소 시작":entryPreviewOnly?"예루살렘 입성 시작":childrenPreviewOnly?"아이들의 축복 시작":prayerPreviewOnly?"주기도문 배우기 시작":confessionPreviewOnly?"베드로의 고백 시작":waterWalkingPreviewOnly?"물 위의 한 걸음 시작":feedingPreviewOnly?"오병이어 장면 시작":stormPreviewOnly?"풍랑 장면 시작":matthewPreviewOnly?"마태의 부르심 시작":fishingPreviewOnly?"깊은 물의 부르심 시작":synagoguePreviewOnly?"회당 장면 시작":templePreviewOnly?"성전 장면 시작":weddingPreviewOnly?"가나 혼인잔치 시작":disciplesPreviewOnly?"제자들과의 만남 시작":temptationPreviewOnly?"광야 시험 시작":baptismPreviewOnly?"세례 장면 시작":"새 여정",new Vector2(.085f,.215f),new Vector2(.285f,.29f),()=>{if(!baptismPreviewOnly&&!temptationPreviewOnly&&!disciplesPreviewOnly&&!weddingPreviewOnly&&!templePreviewOnly&&!synagoguePreviewOnly&&!fishingPreviewOnly&&!matthewPreviewOnly&&!stormPreviewOnly&&!feedingPreviewOnly&&!waterWalkingPreviewOnly&&!confessionPreviewOnly&&!prayerPreviewOnly&&!childrenPreviewOnly&&!entryPreviewOnly&&!secondTemplePreviewOnly&&File.Exists(SavePath))ConfirmNew();else NewGame();});
            var continueButton=Button(menu.transform,"이어 걷기",new Vector2(.305f,.215f),new Vector2(.505f,.29f),ContinueGame,false);continueButton.interactable=!baptismPreviewOnly&&!temptationPreviewOnly&&!disciplesPreviewOnly&&!weddingPreviewOnly&&!templePreviewOnly&&!synagoguePreviewOnly&&!fishingPreviewOnly&&!matthewPreviewOnly&&!stormPreviewOnly&&!feedingPreviewOnly&&!waterWalkingPreviewOnly&&!confessionPreviewOnly&&!prayerPreviewOnly&&!childrenPreviewOnly&&!entryPreviewOnly&&!secondTemplePreviewOnly&&File.Exists(SavePath);
            Button(menu.transform,"설정",new Vector2(.525f,.215f),new Vector2(.665f,.29f),ShowSettings,false);
            Label(menu.transform,"Prototype","플레이 가능한 초안  ·  27장 / 목표 약 180분\n현재 대사·연출·미니게임 분량은 목표 시간보다 짧으며 확장 중입니다.",11,new Color(.72f,.79f,.73f),new Vector2(.085f,.004f),new Vector2(.93f,.043f));
        }
        public void StartSketchPreview(int chapter){baptismPreviewOnly=chapter==1;temptationPreviewOnly=chapter==2;disciplesPreviewOnly=chapter==3;weddingPreviewOnly=chapter==4;templePreviewOnly=chapter==5;synagoguePreviewOnly=chapter==6;fishingPreviewOnly=chapter==7;matthewPreviewOnly=chapter==8;stormPreviewOnly=chapter==9;feedingPreviewOnly=chapter==10;waterWalkingPreviewOnly=chapter==11;confessionPreviewOnly=chapter==12;prayerPreviewOnly=chapter==13;childrenPreviewOnly=chapter==14;entryPreviewOnly=chapter==15;secondTemplePreviewOnly=chapter==16;NewGame();}
        bool AnyPreview=>baptismPreviewOnly||temptationPreviewOnly||disciplesPreviewOnly||weddingPreviewOnly||templePreviewOnly||synagoguePreviewOnly||fishingPreviewOnly||matthewPreviewOnly||stormPreviewOnly||feedingPreviewOnly||waterWalkingPreviewOnly||confessionPreviewOnly||prayerPreviewOnly||childrenPreviewOnly||entryPreviewOnly||secondTemplePreviewOnly;
        public void BeginNewJourneyFromTitle(){if(!AnyPreview&&File.Exists(SavePath))ConfirmNew();else NewGame();}
        void DisposeMenu(){JourneyAssets.Bind(runtimeAssets);
            if(sceneBindings)sceneBindings.HideTitle();if(!menu)return;if(!sceneBindings||menu!=sceneBindings.titlePanel)Destroy(menu);menu=null;}
        void ConfirmNew()
        {
            BeginMenu("NewJourneyConfirmation");
            Label(menu.transform,"Confirm","새 여정을 시작할까요?",38,paper,new Vector2(.22f,.57f),new Vector2(.78f,.68f));
            Label(menu.transform,"Detail","현재 자동 저장의 진행과 수집한 기억을 새 여정으로 바꿉니다.",22,paper,new Vector2(.22f,.45f),new Vector2(.78f,.55f));
            Button(menu.transform,"새 여정 시작",new Vector2(.22f,.30f),new Vector2(.48f,.39f),NewGame);
            Button(menu.transform,"돌아가기",new Vector2(.52f,.30f),new Vector2(.78f,.39f),ShowHome,false);
        }
        public void NewGame()
        {
            ClearStoryPanels();save=new JourneySave{chapter=secondTemplePreviewOnly?16:entryPreviewOnly?15:childrenPreviewOnly?14:prayerPreviewOnly?13:confessionPreviewOnly?12:waterWalkingPreviewOnly?11:feedingPreviewOnly?10:stormPreviewOnly?9:matthewPreviewOnly?8:fishingPreviewOnly?7:synagoguePreviewOnly?6:templePreviewOnly?5:weddingPreviewOnly?4:disciplesPreviewOnly?3:temptationPreviewOnly?2:baptismPreviewOnly?1:0,unlocked=secondTemplePreviewOnly?16:entryPreviewOnly?15:childrenPreviewOnly?14:prayerPreviewOnly?13:confessionPreviewOnly?12:waterWalkingPreviewOnly?11:feedingPreviewOnly?10:stormPreviewOnly?9:matthewPreviewOnly?8:fishingPreviewOnly?7:synagoguePreviewOnly?6:templePreviewOnly?5:weddingPreviewOnly?4:disciplesPreviewOnly?3:temptationPreviewOnly?2:baptismPreviewOnly?1:0};found.Clear();started=true;CloseMenu();WriteSave();LoadWorld(true);
        }
        public void ContinueGame()
        {
            ClearStoryPanels();ReadSave();started=true;CloseMenu();LoadWorld(true);
        }
        void ClearStoryPanels()
        {
            if(Baptism)Baptism.CancelCinematic();
            if(dialog)Destroy(dialog);if(mini)Destroy(mini);if(ending)Destroy(ending);dialog=mini=ending=null;
            dialogueFinished=null;pointerHeld=leftHeld=rightHeld=false;
        }
        void CloseMenu(){DisposeMenu();SetHudVisible(!(Baptism&&Baptism.IsCinematic)&&!(Temptation&&Temptation.IsCinematic)&&!(Meeting&&Meeting.IsCinematic)&&!(Wedding&&Wedding.IsCinematic)&&!(Temple&&Temple.IsCinematic)&&!(Synagogue&&Synagogue.IsCinematic)&&!(Fishing&&Fishing.IsCinematic)&&!(Matthew&&Matthew.IsCinematic)&&!(Storm&&Storm.IsCinematic)&&!(Feeding&&Feeding.IsCinematic)&&!(WaterWalk&&WaterWalk.IsCinematic)&&!(Confession&&Confession.IsCinematic)&&!(Prayer&&Prayer.IsCinematic)&&!(ChildrenScene&&ChildrenScene.IsCinematic)&&!(EntryScene&&EntryScene.IsCinematic)&&!(SecondTemple&&SecondTemple.IsCinematic));if(Baptism)Baptism.SetPaused(false);}
        void SetHudVisible(bool visible)
        {
            headerPanel.SetActive(visible);objectivePanel.SetActive(visible&&!Baptism&&!Temptation&&!Meeting&&!Wedding&&!Temple&&!Synagogue&&!Fishing&&!Matthew&&!Storm&&!Feeding&&!WaterWalk&&!Confession&&!Prayer&&!ChildrenScene&&!EntryScene&&!SecondTemple);journalButton.SetActive(visible);menuButton.SetActive(visible);hudPrompt.gameObject.SetActive(visible);
        }
        void ShowPause()
        {
            WriteSave();BeginMenu("PauseMenu");
            Label(menu.transform,"Title","잠시 쉬어 가기",44,paper,new Vector2(.24f,.67f),new Vector2(.76f,.78f));
            Label(menu.transform,"Progress",$"{Chapter.title}  ·  플레이 {(int)(save.seconds/60)}분\n진행은 각 활동 완료 시 자동 저장됩니다.",22,paper,new Vector2(.24f,.53f),new Vector2(.76f,.66f));
            Button(menu.transform,"계속 걷기",new Vector2(.24f,.41f),new Vector2(.76f,.49f),CloseMenu);
            Button(menu.transform,"여정 기록",new Vector2(.24f,.30f),new Vector2(.49f,.38f),ShowJournal,false);
            Button(menu.transform,"설정",new Vector2(.51f,.30f),new Vector2(.76f,.38f),ShowSettings,false);
            Button(menu.transform,"처음 화면",new Vector2(.24f,.19f),new Vector2(.76f,.27f),()=>{started=false;ClearStoryPanels();ShowHome();},false);
        }
        public void ShowSettings()
        {
            BeginMenu("Settings");
            Label(menu.transform,"Title","나에게 맞는 속도로",40,paper,new Vector2(.20f,.69f),new Vector2(.80f,.80f));
            Label(menu.transform,"Description","편안한 도전은 타이밍 구간을 넓히고 흔들림을 줄입니다.\n실패해도 이야기는 잃지 않습니다.",23,paper,new Vector2(.20f,.53f),new Vector2(.80f,.67f));
            Button(menu.transform,"도전 난이도  ·  "+(gentle?"편안하게":"보통"),new Vector2(.20f,.39f),new Vector2(.80f,.48f),()=>{gentle=!gentle;PlayerPrefs.SetInt("CTL_Gentle",gentle?1:0);PlayerPrefs.Save();ShowSettings();});
            Button(menu.transform,"소리  ·  "+(muted?"끄기":"켜기"),new Vector2(.20f,.27f),new Vector2(.80f,.36f),()=>{muted=!muted;PlayerPrefs.SetInt("CTL_Muted",muted?1:0);PlayerPrefs.Save();ShowSettings();},false);
            Button(menu.transform,"돌아가기",new Vector2(.20f,.15f),new Vector2(.80f,.24f),()=>{if(started)ShowPause();else ShowHome();},false);
        }
        public void ShowJournal()
        {
            BeginMenu("JourneyJournal");
            var card=Panel(menu.transform,"JournalCard",new Vector2(.11f,.10f),new Vector2(.89f,.90f),paper);
            Label(card.transform,"Title","길 위의 기록",36,ink,new Vector2(.055f,.85f),new Vector2(.945f,.94f));
            Label(card.transform,"Sub",$"발견한 기억 {found.Count} / {book.chapters.Length*2}  ·  {(int)(save.seconds/60)}분 함께 걸었습니다",18,teal,new Vector2(.055f,.78f),new Vector2(.945f,.85f));
            journalPage=Mathf.Clamp(journalPage,0,(book.chapters.Length-1)/4);
            for(int i=0;i<4;i++)
            {
                int index=journalPage*4+i;if(index>=book.chapters.Length)break;
                bool unlocked=index<=save.unlocked;var ch=book.chapters[index];float top=.74f-i*.145f;
                string memories=unlocked?string.Join(" · ",ch.discoveries.Where((v,d)=>found.Contains(index+":"+d))):"아직 걷지 않은 길입니다.";
                Label(card.transform,"Entry_"+index,$"{index+1:00}  {ch.title}\n{(unlocked?ch.scripture:"잠긴 장")}\n{memories}",18,unlocked?ink:new Color(.48f,.50f,.45f),new Vector2(.055f,top-.13f),new Vector2(.74f,top));
                var b=Button(card.transform,"다시 걷기",new Vector2(.77f,top-.10f),new Vector2(.945f,top-.015f),()=>ReplayChapter(index),false);b.interactable=unlocked;
            }
            Button(card.transform,"←",new Vector2(.055f,.045f),new Vector2(.155f,.12f),()=>{journalPage=Mathf.Max(0,journalPage-1);ShowJournal();},false);
            Label(card.transform,"Page",(journalPage+1)+" / "+((book.chapters.Length+3)/4),18,teal,new Vector2(.17f,.045f),new Vector2(.32f,.12f),TextAlignmentOptions.Center);
            Button(card.transform,"→",new Vector2(.34f,.045f),new Vector2(.44f,.12f),()=>{journalPage=Mathf.Min((book.chapters.Length-1)/4,journalPage+1);ShowJournal();},false);
            Button(card.transform,"돌아가기",new Vector2(.63f,.045f),new Vector2(.945f,.12f),()=>{if(started)CloseMenu();else ShowHome();});
        }
        public void ReplayChapter(int index)
        {
            if(index<0||index>save.unlocked||index>=book.chapters.Length)return;
            ClearStoryPanels();save.chapter=index;save.step=0;save.finished=false;started=true;CloseMenu();WriteSave();LoadWorld(true);
        }
        void Notify(string text){toast.text=text;toastUntil=Time.unscaledTime+3.5f;toast.transform.SetAsLastSibling();}
        void ReadSave()
        {
            found.Clear();save=new JourneySave();
            if(baptismPreviewOnly||temptationPreviewOnly||disciplesPreviewOnly||weddingPreviewOnly||templePreviewOnly||synagoguePreviewOnly||fishingPreviewOnly||matthewPreviewOnly||stormPreviewOnly||feedingPreviewOnly||waterWalkingPreviewOnly||confessionPreviewOnly||prayerPreviewOnly||childrenPreviewOnly||entryPreviewOnly||secondTemplePreviewOnly){save.chapter=secondTemplePreviewOnly?16:entryPreviewOnly?15:childrenPreviewOnly?14:prayerPreviewOnly?13:confessionPreviewOnly?12:waterWalkingPreviewOnly?11:feedingPreviewOnly?10:stormPreviewOnly?9:matthewPreviewOnly?8:fishingPreviewOnly?7:synagoguePreviewOnly?6:templePreviewOnly?5:weddingPreviewOnly?4:disciplesPreviewOnly?3:temptationPreviewOnly?2:1;save.unlocked=save.chapter;return;}
            try
            {
                if(File.Exists(SavePath))
                {
                    var candidate=JsonUtility.FromJson<JourneySave>(File.ReadAllText(SavePath));
                    if(candidate!=null&&candidate.version==1)save=candidate;
                }
            }
            catch(Exception ex){Debug.LogWarning("Journey save could not be read: "+ex.Message);}
            save.chapter=Mathf.Clamp(save.chapter,0,book.chapters.Length-1);
            save.step=Mathf.Clamp(save.step,0,Chapter.steps.Length);
            save.unlocked=Mathf.Clamp(Mathf.Max(save.unlocked,save.chapter),0,book.chapters.Length-1);
            if(float.IsNaN(save.seconds)||float.IsInfinity(save.seconds)||save.seconds<0)save.seconds=0;
            foreach(var d in save.discoveries??Array.Empty<string>())if(!string.IsNullOrEmpty(d))found.Add(d);
        }
        void WriteSave()
        {
            if(JourneySceneFlow.TestWithoutSaving||!started||baptismPreviewOnly||temptationPreviewOnly||disciplesPreviewOnly||weddingPreviewOnly||templePreviewOnly||synagoguePreviewOnly||fishingPreviewOnly||matthewPreviewOnly||stormPreviewOnly||feedingPreviewOnly||waterWalkingPreviewOnly||confessionPreviewOnly||prayerPreviewOnly||childrenPreviewOnly||entryPreviewOnly||secondTemplePreviewOnly)return;
            save.discoveries=found.OrderBy(x=>x).ToArray();
            try
            {
                Directory.CreateDirectory(Application.persistentDataPath);
                string temp=SavePath+".tmp";File.WriteAllText(temp,JsonUtility.ToJson(save,true));
                if(File.Exists(SavePath))File.Replace(temp,SavePath,SavePath+".bak");else File.Move(temp,SavePath);
            }
            catch(Exception ex){Debug.LogWarning("Journey save failed: "+ex.Message);Notify("저장에 실패했습니다. 저장 폴더를 확인해 주세요.");}
        }
        void BuildAudio()
        {
            music=gameObject.AddComponent<AudioSource>();music.loop=true;music.playOnAwake=false;
            effects=gameObject.AddComponent<AudioSource>();effects.playOnAwake=false;
            const int rate=22050;int length=rate*24;var samples=new float[length];
            int[] melody={0,4,7,11,7,4,2,7,9,7,4,2,0,4,7,12};
            for(int i=0;i<length;i++)
            {
                float time=i/(float)rate;int beat=(int)(time/1.5f);float within=time%1.5f;
                float f=220*Mathf.Pow(2,melody[beat%melody.Length]/12f);
                float env=Mathf.Min(within*5,1)*Mathf.Exp(-within*2.6f);
                float chord=Mathf.Sin(2*Mathf.PI*110*time)*.025f+Mathf.Sin(2*Mathf.PI*164.81f*time)*.017f;
                samples[i]=(Mathf.Sin(2*Mathf.PI*f*time)+.2f*Mathf.Sin(4*Mathf.PI*f*time))*.10f*env+chord;
                samples[i]*=Mathf.Min(time,1)*Mathf.Min(24-time,1);
            }
            musicClip=AudioClip.Create("QuietRoad_OriginalSynth",length,1,rate,false);musicClip.SetData(samples,0);music.clip=musicClip;music.volume=muted?0:musicVolume;music.Play();
            var ping=new float[rate/3];for(int i=0;i<ping.Length;i++){float t=i/(float)rate;ping[i]=Mathf.Sin(2*Mathf.PI*660*t)*Mathf.Exp(-t*18)*.35f;}
            chime=AudioClip.Create("JourneyChime",ping.Length,1,rate,false);chime.SetData(ping,0);
        }
        void Ping(){if(!muted&&effects)effects.PlayOneShot(chime);}
        internal void BaptismSay(string[] lines,Action onDone)=>Say(lines,onDone);
        internal string DialogueSpeaker => dialog ? dialogSpeaker.text : string.Empty;
        internal bool SoundMuted => muted;
        internal bool GentleChallenge => gentle;
        internal void SecondTempleNotice(string[] lines,Action done)=>Say(lines,done);
        internal void SecondTempleHud(string objective,string prompt){hudProgress.text="예루살렘 성전 · "+objective;hudObjective.text=objective;hudPrompt.text=prompt;hudPrompt.color=ink;}
        internal void EntryHud(string objective,string prompt){hudProgress.text="예루살렘 입성 · "+objective;hudObjective.text=objective;hudPrompt.text=prompt;hudPrompt.color=ink;}
        internal void ChildrenHud(string objective,string prompt){hudProgress.text="예루살렘으로 가는 길 · "+objective;hudObjective.text=objective;hudPrompt.text=prompt;hudPrompt.color=ink;}
        internal void PrayerHud(string objective,string prompt){hudProgress.text="감람산 · "+objective;hudObjective.text=objective;hudPrompt.text=prompt;hudPrompt.color=ink;}
        internal void ConfessionHud(string objective,string prompt){hudProgress.text="가이사랴 빌립보 · "+objective;hudObjective.text=objective;hudPrompt.text=prompt;hudPrompt.color=ink;}
        internal void WaterWalkingHud(string objective,string prompt){hudProgress.text="갈릴리 밤바다 · "+objective;hudObjective.text=objective;hudPrompt.text=prompt;hudPrompt.color=save.step>=1?paper:ink;}
        internal void FeedingHud(string objective,string prompt){hudProgress.text="벳세다 빈 들 · "+objective;hudObjective.text=objective;hudPrompt.text=prompt;hudPrompt.color=ink;}
        internal void StormHud(string objective,string prompt){hudProgress.text="갈릴리 바다 · "+objective;hudObjective.text=objective;hudPrompt.text=prompt;hudPrompt.color=ink;}
        internal void MatthewHud(string objective,string prompt){hudProgress.text="가버나움 · "+objective;hudObjective.text=objective;hudPrompt.text=prompt;hudPrompt.color=ink;}
        internal void FishingHud(string objective,string prompt){hudProgress.text="갈릴리 호수 · "+objective;hudObjective.text=objective;hudPrompt.text=prompt;hudPrompt.color=ink;}
        internal void SynagogueHud(string objective,string prompt){hudProgress.text="회당 · "+objective;hudObjective.text=objective;hudPrompt.text=prompt;hudPrompt.color=ink;}
        internal void TempleHud(string objective,string prompt){hudProgress.text="예루살렘 성전 · "+objective;hudObjective.text=objective;hudPrompt.text=prompt;hudPrompt.color=ink;}
        internal void WeddingHud(string objective,string prompt){hudProgress.text="가나 혼인잔치 · "+objective;hudObjective.text=objective;hudPrompt.text=prompt;hudPrompt.color=ink;}
        internal void MeetingTalk(Action done)=>Say(Step.dialogue,done);
        Sprite JesusWalkFrame(bool moving){if(!moving){jesusWalkPhase=0;jesusWalking=false;}else if(!jesusWalking){jesusWalkPhase=0;jesusWalking=true;}else if(lastJesusWalkFrame!=Time.frameCount)jesusWalkPhase+=Time.deltaTime*12;lastJesusWalkFrame=Time.frameCount;return !moving&&authoredJesusIdle?authoredJesusIdle:jesusFrames[moving?(int)jesusWalkPhase%jesusFrames.Length:0];}
        internal void MeetingWalk(bool moving,float direction){SetFrame(playerSprite,currentActor=="예수님"?JesusWalkFrame(moving):SpriteFor(currentActor),0);if(direction!=0)playerSprite.flipX=direction<0;}
        internal void MeetingHud(string objective,string prompt){hudProgress.text="제자들과의 만남 · "+objective;hudObjective.text=objective;hudPrompt.text=prompt;hudPrompt.color=ink;}
        internal void BaptismAdvance()=>CompleteStep();
        internal void TemptationAsk()=>Say(Step.dialogue,()=>BeginMini("choice"));
        internal void TemptationHud(string objective,string prompt){hudObjective.text=objective;hudProgress.text="광야의 시험 · "+objective;hudPrompt.text=prompt;hudPrompt.color=ink;}
        internal void BaptismHud(string objective,string prompt){hudObjective.text=objective;hudProgress.text="요단강  ·  "+objective;hudPrompt.text=prompt;hudPrompt.color=ink;}
        internal void BaptismCinema(bool cinematic){SetHudVisible(!cinematic&&!menu);if(cinematic)toastUntil=0;}
        internal void BaptismWalk(bool walking,float direction)
        {
            SetFrame(playerSprite,JesusWalkFrame(walking),0);
            if(direction!=0)playerSprite.flipX=direction<0;
        }
        void OnApplicationPause(bool paused){if(paused)WriteSave();}
        void OnApplicationQuit(){WriteSave();}
        void OnDestroy()
        {
            world?.Dispose();if(actorMaterial)Destroy(actorMaterial);if(dotSprite)Destroy(dotSprite);if(dotTexture)Destroy(dotTexture);if(musicClip)Destroy(musicClip);if(chime)Destroy(chime);
        }
    }
    public sealed class JourneyHoldButton : MonoBehaviour, UnityEngine.EventSystems.IPointerDownHandler, UnityEngine.EventSystems.IPointerUpHandler, UnityEngine.EventSystems.IPointerExitHandler
    {
        public Action<bool> onHeld;
        public void OnPointerDown(UnityEngine.EventSystems.PointerEventData e)=>onHeld?.Invoke(true);
        public void OnPointerUp(UnityEngine.EventSystems.PointerEventData e)=>onHeld?.Invoke(false);
        public void OnPointerExit(UnityEngine.EventSystems.PointerEventData e)=>onHeld?.Invoke(false);
        void OnDisable()=>onHeld?.Invoke(false);
    }
}
