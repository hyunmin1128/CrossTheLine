using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using TMPro;
using CrossTheLine.Minigames.Fishing;
public static class BuildChapter08Fishing
{
    const string ScenePath="Assets/Scenes/Minigames/Chapter08_Fishing_Test.unity";
    const string Art="Assets/Sprite/Minigames/Chapter08/ExportedLayers/";
    const string Prefabs="Assets/Prefabs/Minigames/Chapter08Fishing/";
    static Sprite sea,boat,crew,throwNet,net,fish;
    static Color C(string hex){ColorUtility.TryParseHtmlString("#"+hex,out var c);return c;}
    static GameObject Go(string name,Transform parent,params Type[] types){var o=new GameObject(name,types);if(parent)o.transform.SetParent(parent,false);return o;}
    static Sprite Load(string path)=>AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().First();
    static SpriteRenderer World(string name,Transform parent,Sprite sprite,int order){var r=Go(name,parent,typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();r.sprite=sprite;r.sortingOrder=order;return r;}
    static RectTransform Rect(string name,Transform parent,Vector2 position,Vector2 size){var r=Go(name,parent,typeof(RectTransform)).GetComponent<RectTransform>();r.anchorMin=r.anchorMax=position;r.sizeDelta=size;return r;}
    static UnityEngine.UI.Image Image(string name,Transform parent,Vector2 position,Vector2 size,Color color,Sprite sprite=null){var r=Rect(name,parent,position,size);var image=r.gameObject.AddComponent<UnityEngine.UI.Image>();image.color=color;image.sprite=sprite;image.raycastTarget=false;return image;}
    static TMP_Text Text(string name,Transform parent,string content,Vector2 position,Vector2 size,float fontSize,Color color){var r=Rect(name,parent,position,size);var text=r.gameObject.AddComponent<TextMeshProUGUI>();text.font=TMP_Settings.defaultFontAsset;text.fontSize=fontSize;text.text=content;text.color=color;text.alignment=TextAlignmentOptions.Center;text.raycastTarget=false;text.textWrappingMode=TextWrappingModes.Normal;return text;}
    static GameObject Panel(string name,Transform parent,Color color){var image=Image(name,parent,new Vector2(.5f,.5f),new Vector2(960,540),color);image.raycastTarget=true;return image.gameObject;}
    static UnityEngine.UI.Button Button(string name,Transform parent,string title,Vector2 position){var img=Image(name,parent,position,new Vector2(240,44),C("B88B4E"));img.raycastTarget=true;var b=img.gameObject.AddComponent<UnityEngine.UI.Button>();b.targetGraphic=img;Text("Label",img.transform,title,new Vector2(.5f,.5f),new Vector2(230,40),19,C("172C38"));return b;}
    static UnityEngine.UI.Image Bar(string name,Transform parent,Vector2 position,Vector2 size,Color color,bool vertical=false){var bg=Image(name,parent,position,size,C("152E43"));var fill=Image("Fill",bg.transform,new Vector2(.5f,.5f),size-new Vector2(6,6),color);fill.sprite=AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");fill.type=UnityEngine.UI.Image.Type.Filled;fill.fillMethod=vertical?UnityEngine.UI.Image.FillMethod.Vertical:UnityEngine.UI.Image.FillMethod.Horizontal;fill.fillAmount=1;return fill;}
    static void Import(string name,bool aligned=false){var importer=(TextureImporter)AssetImporter.GetAtPath(Art+name+".png");importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.spritePixelsPerUnit=30;importer.filterMode=FilterMode.Point;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.mipmapEnabled=false;importer.alphaIsTransparency=true;importer.npotScale=TextureImporterNPOTScale.None;if(aligned){var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);settings.spriteAlignment=9;settings.spritePivot=new Vector2(148f/320,70f/180);settings.spriteMeshType=SpriteMeshType.FullRect;importer.SetTextureSettings(settings);}importer.SaveAndReimport();}
    public static string Run()
    {
        if(EditorApplication.isPlaying)throw new Exception("Edit mode required");
        if(EditorSceneManager.GetActiveScene().isDirty)throw new Exception("Preserve unsaved scene first");
        if(File.Exists(ScenePath))throw new Exception("Scene exists: edit instead of overwriting");
        Directory.CreateDirectory(Prefabs);Directory.CreateDirectory("Assets/Settings/Minigames");AssetDatabase.Refresh();
        Import("Sea");Import("Boat",true);Import("PeterAndrew",true);Import("ThrowNet",true);Import("Net");
        sea=Load(Art+"Sea.png");boat=Load(Art+"Boat.png");crew=Load(Art+"PeterAndrew.png");throwNet=Load(Art+"ThrowNet.png");net=Load(Art+"Net.png");fish=Load("Assets/Sprite/Generated/PixelSprites/FeedingProps/OfferingFish.png");
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var cam=Go("FishingCamera",null,typeof(Camera),typeof(AudioListener)).GetComponent<Camera>();cam.orthographic=true;cam.orthographicSize=3;cam.transform.position=new Vector3(0,0,-10);cam.backgroundColor=C("0868B5");cam.clearFlags=CameraClearFlags.SolidColor;cam.tag="MainCamera";
        var settings=ScriptableObject.CreateInstance<NetFishingSettings>();settings.boatBounds=new Rect(-4.1f,-2,8.2f,3.55f);settings.boatSpeed=1.8f;settings.castRadius=.9f;AssetDatabase.CreateAsset(settings,"Assets/Settings/Minigames/Chapter08Fishing.asset");
        var root=Go("Chapter08_NetFishing",null);var game=root.AddComponent<NetFishingGame>();game.settings=settings;game.sceneCamera=cam;
        BuildWorld(root.transform,game);BuildUI(root.transform,game);
        var events=Go("EventSystem",null,typeof(EventSystem),typeof(InputSystemUIInputModule));events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
        PrefabUtility.SaveAsPrefabAssetAndConnect(root,Prefabs+"Chapter08Fishing.prefab",InteractionMode.AutomatedAction);
        EditorSceneManager.SaveScene(scene,ScenePath);
        if(!EditorBuildSettings.scenes.Any(s=>s.path==ScenePath))EditorBuildSettings.scenes=EditorBuildSettings.scenes.Concat(new[]{new EditorBuildSettingsScene(ScenePath,true)}).ToArray();
        return "Created editable fishing scene, prefabs and settings; original scenes and Aseprite preserved";
    }
    static void BuildWorld(Transform root,NetFishingGame game)
    {
        var world=Go("EditableSea",root);World("Sea_Background",world.transform,sea,-10);
        var boatRoot=Go("Boat",world.transform);boatRoot.transform.position=new Vector3(-2.8f,-1.1f,0);World("Hull",boatRoot.transform,boat,2);game.crewBody=World("Peter_Andrew",boatRoot.transform,crew,3);game.crewIdle=crew;game.crewThrow=throwNet;game.boat=boatRoot.transform;
        PrefabUtility.SaveAsPrefabAssetAndConnect(boatRoot,Prefabs+"FishingBoat.prefab",InteractionMode.AutomatedAction);
        game.castNet=World("ThrownNet",world.transform,net,5).transform;game.castNet.gameObject.SetActive(false);
        var spots=Go("FishingSpots",world.transform);var points=new[]{new Vector2(-1.5f,.7f),new Vector2(2.7f,-.4f),new Vector2(.6f,-1.6f),new Vector2(-3.2f,1.1f),new Vector2(3.4f,1.2f)};
        game.fishingSpots=new Transform[points.Length];game.spotBubbles=new SpriteRenderer[points.Length];var bubble=AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
        for(int i=0;i<points.Length;i++){var spot=Go("BubbleSpot_"+(i+1),spots.transform);spot.transform.position=points[i];game.fishingSpots[i]=spot.transform;var visual=Go("Bubbles",spot.transform);game.spotBubbles[i]=visual.AddComponent<SpriteRenderer>();game.spotBubbles[i].sprite=bubble;game.spotBubbles[i].color=new Color(.8f,.98f,1,.55f);game.spotBubbles[i].sortingOrder=1;game.spotBubbles[i].transform.localScale=Vector3.one*.5f;for(int n=0;n<4;n++){var b=World("Bubble_"+n,visual.transform,bubble,1);b.color=new Color(.75f,.96f,1,.65f);b.transform.localPosition=new Vector3((n%2-.5f)*.8f,(n/2-.5f)*.4f,0);b.transform.localScale=Vector3.one*.2f;}}
    }
    static void BuildUI(Transform root,NetFishingGame game)
    {
        var ui=Go("FishingCanvas",root,typeof(RectTransform),typeof(Canvas),typeof(UnityEngine.UI.CanvasScaler),typeof(UnityEngine.UI.GraphicRaycaster));ui.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;var scaler=ui.GetComponent<UnityEngine.UI.CanvasScaler>();scaler.uiScaleMode=UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(960,540);scaler.matchWidthOrHeight=.5f;
        var header=Image("CatchHeader",ui.transform,new Vector2(.18f,.94f),new Vector2(310,48),C("E9DEBA"));game.countText=Text("FishCount",header.transform,"잡은 물고기  0 / 150",new Vector2(.5f,.5f),new Vector2(300,42),21,C("183B4D"));
        game.netFill=Bar("NetDurability",ui.transform,new Vector2(.82f,.94f),new Vector2(250,16),C("CBA459"));Text("NetLabel",ui.transform,"그물 내구도",new Vector2(.82f,.985f),new Vector2(250,22),16,Color.white);
        game.promptText=Text("ControlPrompt",ui.transform,"",new Vector2(.5f,.045f),new Vector2(920,35),18,Color.white);game.biteText=Text("BiteNotice",ui.transform,"",new Vector2(.5f,.5f),new Vector2(100,65),42,C("FFE9A3"));
        BuildFight(ui.transform,game);BuildOverlays(ui.transform,game);
        header.transform.SetAsLastSibling();game.netFill.transform.parent.SetAsLastSibling();ui.transform.Find("NetLabel").SetAsLastSibling();
        game.fightPanel.SetActive(false);game.resultPanel.SetActive(false);game.completePanel.SetActive(false);game.pausePanel.SetActive(false);
    }
    static void BuildFight(Transform ui,NetFishingGame game)
    {
        game.fightPanel=Panel("FishingFight",ui,new Color(.02f,.13f,.22f,.94f));var parent=game.fightPanel.transform;
        Image("Sea_Backdrop",parent,new Vector2(.5f,.5f),new Vector2(960,540),new Color(.55f,.7f,.8f,.4f),sea);
        Text("FightTitle",parent,"베드로와 안드레 · 함께 그물을 당기세요",new Vector2(.37f,.88f),new Vector2(610,45),24,C("F1DFC0"));
        Image("BoatHull",parent,new Vector2(.28f,.45f),new Vector2(860,484),Color.white,boat);
        game.pullingCrew=(RectTransform)Image("PeterAndrew_Pull",parent,new Vector2(.28f,.45f),new Vector2(860,484),Color.white,crew).transform;
        game.pullingNetImage=Image("Net_Pull",parent,new Vector2(.48f,.43f),new Vector2(150,150),Color.white,net);game.pullingNet=(RectTransform)game.pullingNetImage.transform;
        var track=Image("FishTrack",parent,new Vector2(.75f,.52f),new Vector2(76,314),C("152F40"));
        game.zoneRect=(RectTransform)Image("SafeCatchZone",track.transform,new Vector2(.5f,.5f),new Vector2(66,100),C("73BD8C")).transform;
        game.fishRect=(RectTransform)Image("MovingFish",track.transform,new Vector2(.5f,.5f),new Vector2(48,32),Color.white,fish).transform;
        game.catchFill=Bar("CatchProgress",parent,new Vector2(.85f,.52f),new Vector2(24,314),C("E1C56C"),true);
        Text("ProgressLabel",parent,"포획",new Vector2(.85f,.84f),new Vector2(80,30),16,Color.white);
        Text("TapHelp",parent,"연타하면 초록 구간이 올라갑니다\n손을 떼면 내려갑니다\n물고기를 구간 안에 두고 연타하세요",new Vector2(.32f,.19f),new Vector2(560,80),20,C("E8DDBC"));
        game.timerText=Text("FightTime",parent,"남은 시간 30초",new Vector2(.79f,.16f),new Vector2(280,40),20,Color.white);
        Text("NetWarning",parent,"구간을 벗어나면 그물이 손상됩니다",new Vector2(.5f,.07f),new Vector2(850,28),17,C("E7BA7D"));
    }
    static void BuildOverlays(Transform ui,NetFishingGame game)
    {
        game.resultPanel=Panel("CatchResult",ui,new Color(.02f,.1f,.18f,.88f));game.resultText=Text("Result",game.resultPanel.transform,"",new Vector2(.5f,.5f),new Vector2(850,180),27,C("F8E3B6"));
        game.completePanel=Panel("Complete",ui,new Color(.02f,.1f,.18f,.95f));game.completeText=Text("Summary",game.completePanel.transform,"",new Vector2(.5f,.57f),new Vector2(800,210),27,C("F8E3B6"));game.retryButton=Button("Restart",game.completePanel.transform,"다시 낚시하기",new Vector2(.5f,.23f));
        Image("James",game.completePanel.transform,new Vector2(.16f,.48f),new Vector2(100,100),Color.white,Load("Assets/Sprite/Unit/Student/James(Zebedee)/RealJames(Zebedee).png"));
        Image("John",game.completePanel.transform,new Vector2(.84f,.48f),new Vector2(100,100),Color.white,Load("Assets/Sprite/Unit/Student/Yohan2/RealYohan2.png"));
        game.introduction=Panel("Introduction",ui,new Color(.02f,.1f,.18f,.94f));Text("Title",game.introduction.transform,"갈릴리 바다 · 그물 낚시",new Vector2(.5f,.82f),new Vector2(820,60),32,C("F5D895"));
        Text("Instructions",game.introduction.transform,"배를 움직여 보글거리는 지점을 찾아보세요.\n\nWASD / 방향키: 배 이동     E / Space: 그물 던지기\n입질 표시 … ! 후 낚시 화면이 열립니다.\nSpace / 마우스 연타: 초록 구간을 위로 올립니다.\n물고기를 초록 구간 안에 두고 연타하면 포획 게이지가 찹니다.\n\n한 번에 1~15마리 · 목표 150마리 · 한 번의 씨름은 최대 30초\n물고기를 놓치면 이번 포획만 실패합니다.\n그물이 찢어지면 6초 후 새 그물로 교체합니다. 잡은 수는 유지됩니다.\nEsc: 일시정지",new Vector2(.5f,.51f),new Vector2(850,290),20,C("EAE2C9"));game.startButton=Button("StartFishing",game.introduction.transform,"낚시 시작 [Enter]",new Vector2(.5f,.15f));
        game.pausePanel=Panel("Pause",ui,new Color(0,.04f,.1f,.93f));Text("PauseText",game.pausePanel.transform,"잠시 쉬는 중\nEsc · 계속하기",new Vector2(.5f,.5f),new Vector2(750,150),30,C("EAE2C9"));
    }
}
