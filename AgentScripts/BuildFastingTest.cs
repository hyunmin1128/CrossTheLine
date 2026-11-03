using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using TMPro;
using CrossTheLine.Minigames;

public static class BuildFastingTest
{
    const string ScenePath="Assets/Scenes/Minigames/Chapter03_Fasting_Test.unity";
    const string Art="Assets/Sprite/Minigames/Fasting";
    const string Prefabs="Assets/Prefabs/Minigames/Fasting";
    static Material material;
    static TMP_FontAsset font;
    static void Folder(string path){if(AssetDatabase.IsValidFolder(path))return;Folder(Path.GetDirectoryName(path).Replace('\\','/'));AssetDatabase.CreateFolder(Path.GetDirectoryName(path).Replace('\\','/'),Path.GetFileName(path));}
    static GameObject Obj(string name,Transform parent=null){var o=new GameObject(name);if(parent)o.transform.SetParent(parent,false);return o;}
    static Sprite SpriteAt(string path)=>AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().OrderBy(s=>s.name).First();
    static SpriteRenderer Draw(string name,Transform parent,Sprite sprite,Vector3 p,Vector3 scale,int order,Color color)
    {var o=Obj(name,parent);o.transform.localPosition=p;o.transform.localScale=scale;var r=o.AddComponent<SpriteRenderer>();r.sprite=sprite;r.sharedMaterial=material;r.sortingOrder=order;r.color=color;return r;}
    static Sprite ArtSprite(string name,int width,int height,Func<int,int,Color32> pixel,Vector2 pivot)
    {
        var tex=new Texture2D(width,height,TextureFormat.RGBA32,false);var colors=new Color32[width*height];
        for(int y=0;y<height;y++)for(int x=0;x<width;x++)colors[y*width+x]=pixel(x,y);
        tex.SetPixels32(colors);tex.Apply();var path=$"{Art}/{name}.png";File.WriteAllBytes(path,tex.EncodeToPNG());UnityEngine.Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(path);var imp=(TextureImporter)AssetImporter.GetAtPath(path);imp.textureType=TextureImporterType.Sprite;imp.spriteImportMode=SpriteImportMode.Single;imp.spritePixelsPerUnit=32;var ts=new TextureImporterSettings();imp.ReadTextureSettings(ts);ts.spritePivot=pivot;ts.spriteAlignment=(int)SpriteAlignment.Custom;imp.SetTextureSettings(ts);imp.filterMode=FilterMode.Point;imp.mipmapEnabled=false;imp.alphaIsTransparency=true;imp.textureCompression=TextureImporterCompression.Uncompressed;imp.SaveAndReimport();return SpriteAt(path);
    }
    static GameObject SavePrefab(GameObject o,string name){var p=PrefabUtility.SaveAsPrefabAsset(o,$"{Prefabs}/{name}.prefab");UnityEngine.Object.DestroyImmediate(o);return p;}
    static RectTransform UI(string name,Transform parent,Vector2 anchor,Vector2 pos,Vector2 size)
    {var o=new GameObject(name,typeof(RectTransform));o.transform.SetParent(parent,false);var r=o.GetComponent<RectTransform>();r.anchorMin=r.anchorMax=anchor;r.pivot=anchor;r.anchoredPosition=pos;r.sizeDelta=size;return r;}
    static Image Image(string name,Transform parent,Vector2 anchor,Vector2 pos,Vector2 size,Color color)
    {var r=UI(name,parent,anchor,pos,size);var im=r.gameObject.AddComponent<UnityEngine.UI.Image>();im.color=color;im.raycastTarget=false;return im;}
    static TMP_Text Text(string name,Transform parent,Vector2 anchor,Vector2 pos,Vector2 size,string content,int sizeFont=20)
    {var r=UI(name,parent,anchor,pos,size);var t=r.gameObject.AddComponent<TextMeshProUGUI>();t.font=font;t.text=content;t.fontSize=sizeFont;t.color=new Color(.98f,.94f,.82f);t.raycastTarget=false;return t;}
    public static string Build()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Stop Play first.");
        for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)if(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)throw new Exception("Open scene has unsaved edits; preserve it before building.");
        if(File.Exists(ScenePath))throw new Exception("Test scene already exists; do not overwrite user edits.");
        Folder("Assets/Scenes/Minigames");Folder(Art);Folder(Prefabs);Folder("Assets/Settings/Minigames");
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var pipeline=GraphicsSettings.currentRenderPipeline;
        if(!pipeline||!pipeline.GetType().FullName.Contains("Universal"))throw new Exception("Expected the project's URP pipeline.");
        material=AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/CrossTheLine/CastUnlit.mat");
        font=TMP_Settings.defaultFontAsset;
        if(!font)font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
        var ink=new Color32(35,30,28,255);var rockBase=new Color32(160,121,80,255);var rockLight=new Color32(211,170,110,255);
        Func<int,int,bool> mask=(x,y)=>y>=2&&y<29&&x>=(y<8?3:y<18?8:16)&&x<(y<8?61:y<18?57:49);
        var rock=ArtSprite("PrototypeRock",64,32,(x,y)=>{if(!mask(x,y))return new Color32();if(!mask(x-1,y)||!mask(x+1,y)||!mask(x,y-1)||!mask(x,y+1))return ink;return (x+y/2)%13<3?rockLight:rockBase;},new Vector2(.5f,0));
        var tree=ArtSprite("PrototypeDryTree",64,64,(x,y)=>{
            bool trunk=x>=29&&x<=34&&y<48;
            bool branch=(y>=24&&y<44&&Math.Abs(x-(31-(y-24)))<=2)||(y>=32&&y<55&&Math.Abs(x-(32+(y-32)))<=2)||(y>=42&&y<63&&Math.Abs(x-(32-(y-42)/2))<=1);
            if(!trunk&&!branch)return new Color32();return x%3==0?new Color32(158,107,57,255):ink;
        },new Vector2(.5f,0));
        var wind=ArtSprite("PrototypeSandWind",96,24,(x,y)=>{
            bool streak=(y%5==0&&x>(y*7)%35&&x<90-(y*3)%24)||(x*17+y*13)%53<2;
            return streak?new Color32(215,174,104,220):new Color32();
        },new Vector2(.5f,.5f));
        var pixel=ArtSprite("PrototypePixel",1,1,(x,y)=>new Color32(255,255,255,255),new Vector2(.5f,.5f));
        var settings=ScriptableObject.CreateInstance<FastingSettings>();
        for(int i=0;i<8;i++)settings.stages[i]=new FastingSettings.Difficulty{label=$"{i*5+1}~{i*5+5}일",seconds=18+i*2,windSpeed=4+i*.65f,waveInterval=5.2f-i*.35f,dangerousLanes=i>=3?2:1,warningSeconds=1.5f-i*.06f,exposureSeconds=14-i*.6f};
        AssetDatabase.CreateAsset(settings,"Assets/Settings/Minigames/FastingDifficulty.asset");
        GameObject CoverPrefab(string name,Sprite art){var o=Obj(name);o.AddComponent<FastingCover>();Draw("Artwork",o.transform,art,Vector3.zero,Vector3.one,155,Color.white);Draw("ShelterGround_LeftOfWind",o.transform,pixel,new Vector3(-1.55f,0,0),new Vector3(2.1f*32,.7f*32,1),5,new Color(.13f,.25f,.2f,.22f));return SavePrefab(o,name);}
        var rockPrefab=CoverPrefab("FastingRock",rock);var treePrefab=CoverPrefab("FastingDryTree",tree);
        var stormObj=Obj("FastingSandstorm");stormObj.AddComponent<FastingSandstorm>();Draw("PixelWind",stormObj.transform,wind,new Vector3(0,.22f,0),Vector3.one,230,Color.white);var storm=SavePrefab(stormObj,"FastingSandstorm").GetComponent<FastingSandstorm>();
        var hero=Obj("FastingPlayer");var idle=SpriteAt("Assets/Sprite/Unit/Jesus/JesusIdle.png");var body=Draw("Artwork",hero.transform,idle,new Vector3(0,0,0),Vector3.one*.5f,150,Color.white);body.transform.localPosition=new Vector3(-idle.bounds.center.x*.5f,-idle.bounds.min.y*.5f,0);var heroPrefab=SavePrefab(hero,"FastingPlayer");
        var cameraObj=Obj("FastingCamera");var camera=cameraObj.AddComponent<Camera>();cameraObj.tag="MainCamera";camera.orthographic=true;camera.orthographicSize=8.4375f;camera.allowHDR=false;camera.allowMSAA=false;camera.allowDynamicResolution=false;camera.backgroundColor=new Color(.11f,.16f,.2f);cameraObj.transform.position=new Vector3(0,0,-10);cameraObj.AddComponent<AudioListener>();var extra=cameraObj.AddComponent<UniversalAdditionalCameraData>();extra.renderPostProcessing=false;
        var pp=cameraObj.AddComponent<PixelPerfectCamera>();pp.assetsPPU=32;pp.refResolutionX=960;pp.refResolutionY=540;pp.gridSnapping=PixelPerfectCamera.GridSnapping.PixelSnapping;pp.cropFrame=PixelPerfectCamera.CropFrame.Windowbox;
        var game=Obj("FastingMinigame").AddComponent<FastingMinigame>();game.settings=settings;game.gameCamera=camera;game.stormPrefab=storm;game.idle=idle;game.walk=AssetDatabase.LoadAllAssetsAtPath("Assets/Sprite/Unit/Jesus/JesusWalking.png").OfType<Sprite>().OrderBy(s=>s.name).ToArray();
        var scenery=Obj("BackgroundStrip");var background=SpriteAt("Assets/Sprite/Background/Chapter3.png");game.backgrounds=new SpriteRenderer[3];for(int i=0;i<3;i++)game.backgrounds[i]=Draw("Chapter3Tile_"+i,scenery.transform,background,new Vector3(i*30,0,0),Vector3.one,-100,Color.white);
        game.player=((GameObject)PrefabUtility.InstantiatePrefab(heroPrefab)).transform;game.player.name="Jesus_Player";game.player.position=new Vector3(-13.5f,-6.1f,0);game.body=game.player.GetComponentInChildren<SpriteRenderer>();game.artwork=game.body.transform;
        game.hazardRoot=Obj("LiveSandstorms").transform;var shelterRoot=Obj("Shelters_Editable").transform;var coverList=new System.Collections.Generic.List<FastingCover>();
        for(int i=0;i<8;i++)for(int n=0;n<2;n++){
            var o=(GameObject)PrefabUtility.InstantiatePrefab(n==0?rockPrefab:treePrefab);o.transform.SetParent(shelterRoot,false);o.name=$"Stage{i+1}_{(n==0?"Rock":"Tree")}";
            int l=(i+n)%3;o.transform.position=new Vector3(-13.5f+i*9+(n==0?3:6),settings.lanes[l],0);o.GetComponentInChildren<SpriteRenderer>().sortingOrder=100-Mathf.RoundToInt(o.transform.position.y*10)+1;coverList.Add(o.GetComponent<FastingCover>());
        }
        game.covers=coverList.ToArray();var marks=Obj("WarningMarkers").transform;game.warnings=new SpriteRenderer[3];for(int i=0;i<3;i++){game.warnings[i]=Draw("Lane"+i+"_IncomingWind",marks,pixel,new Vector3(13,settings.lanes[i]+.35f,0),new Vector3(.22f*32,.7f*32,1),240,new Color(1,.48f,.18f));game.warnings[i].enabled=false;}
        var hud=Obj("FastingHUD",null);var canvas=hud.AddComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;var scaler=hud.AddComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(960,540);scaler.matchWidthOrHeight=.5f;hud.AddComponent<GraphicRaycaster>();
        var overlay=Image("NightOverlay",hud.transform,Vector2.zero,Vector2.zero,Vector2.zero,Color.clear);overlay.rectTransform.anchorMax=Vector2.one;overlay.rectTransform.offsetMin=overlay.rectTransform.offsetMax=Vector2.zero;
        Image("Header",hud.transform,new Vector2(0,1),new Vector2(12,-12),new Vector2(800,112),new Color(.06f,.11f,.15f,.91f));
        Text("Day",hud.transform,new Vector2(0,1),new Vector2(28,-20),new Vector2(730,30),"광야 · 1 / 40일",23);
        Text("StaminaLabel",hud.transform,new Vector2(0,1),new Vector2(28,-57),new Vector2(130,23),"지침 · 남은 힘",17);
        for(int i=0;i<5;i++)Image("Stamina"+i,hud.transform,new Vector2(0,1),new Vector2(164+i*32,-58),new Vector2(25,18),new Color(.91f,.69f,.31f));
        Text("Status",hud.transform,new Vector2(0,1),new Vector2(28,-86),new Vector2(750,24),"바람을 피해 전진하세요",18);
        Image("Footer",hud.transform,Vector2.zero,new Vector2(12,10),new Vector2(930,34),new Color(.06f,.11f,.15f,.9f));
        Text("Instructions",hud.transform,Vector2.zero,new Vector2(26,15),new Vector2(900,25),"방향키 / WASD · 한 칸 이동 · R 처음부터",17);
        var panel=Image("ResultPanel",hud.transform,new Vector2(.5f,.5f),Vector2.zero,new Vector2(660,220),new Color(.06f,.11f,.15f,.97f));
        var result=Text("Result",panel.transform,new Vector2(.5f,.5f),new Vector2(0,32),new Vector2(610,92),"40일 완료",26);result.alignment=TextAlignmentOptions.Center;
        var buttonImage=Image("Replay",panel.transform,new Vector2(.5f,.5f),new Vector2(0,-64),new Vector2(210,46),new Color(.48f,.34f,.18f));buttonImage.raycastTarget=true;var button=buttonImage.gameObject.AddComponent<UnityEngine.UI.Button>();button.targetGraphic=buttonImage;var label=Text("Label",button.transform,new Vector2(.5f,.5f),Vector2.zero,new Vector2(190,40),"처음부터 다시 테스트",18);label.alignment=TextAlignmentOptions.Center;
        var hudPrefab=PrefabUtility.SaveAsPrefabAsset(hud,$"{Prefabs}/FastingHUD.prefab");UnityEngine.Object.DestroyImmediate(hud);hud=(GameObject)PrefabUtility.InstantiatePrefab(hudPrefab);
        game.dayText=hud.transform.Find("Day").GetComponent<TMP_Text>();game.statusText=hud.transform.Find("Status").GetComponent<TMP_Text>();game.instructionsText=hud.transform.Find("Instructions").GetComponent<TMP_Text>();game.nightOverlay=hud.transform.Find("NightOverlay").GetComponent<Image>();game.staminaSlots=Enumerable.Range(0,5).Select(i=>hud.transform.Find("Stamina"+i).GetComponent<Image>()).ToArray();game.resultPanel=hud.transform.Find("ResultPanel").gameObject;game.resultText=game.resultPanel.transform.Find("Result").GetComponent<TMP_Text>();game.replayButton=game.resultPanel.transform.Find("Replay").GetComponent<UnityEngine.UI.Button>();game.resultPanel.SetActive(false);
        var events=Obj("FastingEventSystem");events.AddComponent<EventSystem>();events.AddComponent<InputSystemUIInputModule>();
        EditorSceneManager.SaveScene(scene,ScenePath);AssetDatabase.SaveAssets();
        return $"Created {ScenePath}; 8 stages / {settings.stages.Sum(s=>s.seconds)} seconds minimum; 16 editable cover instances; 5 stamina slots. Existing chapters unchanged.";
    }
}
