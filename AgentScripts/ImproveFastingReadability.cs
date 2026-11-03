using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CrossTheLine.Minigames;
public static class ImproveFastingReadability
{
    const string Folder="Assets/Prefabs/Minigames/Fasting/";
    static Sprite pixel;static Material material;
    static Transform Root(string name,Transform parent){var existing=parent.Find(name);if(existing)return existing;var o=new GameObject(name);o.transform.SetParent(parent,false);return o.transform;}
    static SpriteRenderer Rect(string name,Transform parent,Vector3 position,Vector2 size,Color color,int sorting)
    {var t=Root(name,parent);t.localPosition=position;t.localScale=new Vector3(size.x*32,size.y*32,1);var s=t.GetComponent<SpriteRenderer>();if(!s)s=t.gameObject.AddComponent<SpriteRenderer>();s.sprite=pixel;s.sharedMaterial=material;s.color=color;s.sortingOrder=sorting;return s;}
    static Transform Frame(string name,Transform parent,Color color,int sorting)
    {var t=Root(name,parent);for(int i=0;i<4;i++)Rect("Edge"+i,t,Vector3.zero,Vector2.one,color,sorting);return t;}
    static RectTransform UI(string name,Transform parent,Vector2 anchor,Vector2 position,Vector2 size)
    {var t=parent.Find(name) as RectTransform;if(!t){var o=new GameObject(name,typeof(RectTransform));o.transform.SetParent(parent,false);t=o.GetComponent<RectTransform>();}t.anchorMin=t.anchorMax=t.pivot=anchor;t.anchoredPosition=position;t.sizeDelta=size;return t;}
    static Image Panel(string name,Transform parent,Vector2 anchor,Vector2 pos,Vector2 size,Color color)
    {var t=UI(name,parent,anchor,pos,size);var im=t.GetComponent<Image>();if(!im)im=t.gameObject.AddComponent<Image>();im.color=color;im.raycastTarget=false;return im;}
    static TMP_Text Label(string name,Transform parent,Vector2 anchor,Vector2 pos,Vector2 size,string text,int fontSize)
    {var t=UI(name,parent,anchor,pos,size);var label=t.GetComponent<TMP_Text>();if(!label)label=t.gameObject.AddComponent<TextMeshProUGUI>();label.font=TMP_Settings.defaultFontAsset;label.fontSize=fontSize;label.color=new Color(.98f,.95f,.85f);label.text=text;label.raycastTarget=false;return label;}
    public static string Patch()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Stop Play first");
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(!scene.path.EndsWith("Chapter03_Fasting_Test.unity"))throw new Exception("Open fasting test scene first");
        if(scene.isDirty)throw new Exception("Preserve unsaved scene edits before patching");
        string backup="Docs/Backups/FastingReadability-20261103";Directory.CreateDirectory(backup);
        if(!File.Exists(backup+"/Chapter03_Fasting_Test.unity"))File.Copy(scene.path,backup+"/Chapter03_Fasting_Test.unity");
        pixel=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprite/Minigames/Fasting/PrototypePixel.png");material=AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/CrossTheLine/CastUnlit.mat");
        foreach(string name in new[]{"FastingRock","FastingDryTree","FastingSandstorm","FastingHUD"}){
            string path=Folder+name+".prefab";if(!File.Exists(backup+"/"+name+".prefab"))File.Copy(path,backup+"/"+name+".prefab");
            var o=PrefabUtility.LoadPrefabContents(path);
            try{
                if(name=="FastingHUD"){
                    UI("Header",o.transform,new Vector2(0,1),new Vector2(12,-12),new Vector2(936,134));
                    UI("Day",o.transform,new Vector2(0,1),new Vector2(28,-22),new Vector2(600,28));
                    UI("StaminaLabel",o.transform,new Vector2(0,1),new Vector2(640,-24),new Vector2(114,22));
                    for(int i=0;i<5;i++)UI("Stamina"+i,o.transform,new Vector2(0,1),new Vector2(766+i*28,-24),new Vector2(22,16));
                    o.transform.Find("Status").gameObject.SetActive(false);
                    Label("MovementStatus",o.transform,new Vector2(0,1),new Vector2(28,-60),new Vector2(900,23),"Movement ready",17);
                    Label("CoverStatus",o.transform,new Vector2(0,1),new Vector2(28,-86),new Vector2(900,23),"Shelter status",17);
                    var track=Panel("ExposureTrack",o.transform,new Vector2(0,1),new Vector2(28,-120),new Vector2(190,7),new Color(.15f,.22f,.28f));
                    var fill=Panel("ExposureFill",track.transform,new Vector2(0,1),Vector2.zero,new Vector2(190,7),new Color(.95f,.74f,.25f));fill.sprite=pixel;fill.type=Image.Type.Filled;fill.fillMethod=Image.FillMethod.Horizontal;fill.fillAmount=0;
                    Label("ExposureStatus",o.transform,new Vector2(0,1),new Vector2(238,-111),new Vector2(675,23),"Exposure",16);
                    var notice=Panel("DamageNotice",o.transform,new Vector2(.5f,1),new Vector2(0,-158),new Vector2(650,38),new Color(.35f,.045f,.035f,.96f));
                    var note=Label("Label",notice.transform,new Vector2(.5f,.5f),Vector2.zero,new Vector2(630,32),"Damage reason",21);note.alignment=TextAlignmentOptions.Center;notice.gameObject.SetActive(false);
                    UI("Footer",o.transform,Vector2.zero,new Vector2(12,2),new Vector2(930,24));
                    UI("Instructions",o.transform,Vector2.zero,new Vector2(26,4),new Vector2(900,20));o.transform.Find("Instructions").GetComponent<TMP_Text>().fontSize=14;
                }else if(name=="FastingSandstorm"){
                    var storm=o.GetComponent<FastingSandstorm>();var old=o.transform.Find("PixelWind");if(old)old.gameObject.SetActive(false);
                    Rect("DustLower",o.transform,new Vector3(0,.28f,0),new Vector2(2.2f,.65f),new Color(1,.7f,.2f,.8f),250);
                    Rect("DustMiddle",o.transform,new Vector3(.15f,.85f,0),new Vector2(1.9f,.55f),new Color(1,.79f,.3f,.75f),250);
                    Rect("DustTop",o.transform,new Vector3(.4f,1.22f,0),new Vector2(1.35f,.3f),new Color(1,.88f,.5f,.7f),250);
                    for(int streak=0;streak<10;streak++)Rect("WindStreak"+streak,o.transform,new Vector3(-.65f+(streak*7%11)*.12f,.18f+streak*.105f,0),new Vector2(.35f+(streak%3)*.16f,.035f),streak%2==0?new Color(1,.95f,.7f,.9f):new Color(.58f,.29f,.04f,.55f),254);
                    storm.groundFill=Rect("GroundHitArea",o.transform,Vector3.zero,storm.hitSize,new Color(1,.18f,.09f,.65f),280);
                    storm.groundFrame=Frame("HitAreaOutline",o.transform,new Color(1,.94f,.75f),282);
                    Rect("DirectionShaft_Left",o.transform,new Vector3(0,.8f,0),new Vector2(1.3f,.1f),new Color(.16f,.075f,.025f),260);
                    var up=Rect("LeftArrowTop",o.transform,new Vector3(-.58f,.95f,0),new Vector2(.5f,.11f),new Color(.16f,.075f,.025f),260);up.transform.localRotation=Quaternion.Euler(0,0,40);
                    var down=Rect("LeftArrowBottom",o.transform,new Vector3(-.58f,.65f,0),new Vector2(.5f,.11f),new Color(.16f,.075f,.025f),260);down.transform.localRotation=Quaternion.Euler(0,0,-40);
                    storm.RefreshFootprint();
                }else{
                    var c=o.GetComponent<FastingCover>();if(name=="FastingRock"&&Mathf.Approximately(c.solid.width,.6f)){c.solid=new UnityEngine.Rect(-.9f,-.35f,1.8f,.7f);c.shelter=new UnityEngine.Rect(-2.8f,-.35f,1.65f,.7f);}
                    var old=o.transform.Find("ShelterGround_LeftOfWind");if(old)old.gameObject.SetActive(false);
                    Rect("VisibleShelterArea",o.transform,c.shelter.center,c.shelter.size,new Color(.05f,.8f,.6f,.23f),65);
                    var frame=Frame("ShelterOutline",o.transform,new Color(.3f,1,.8f,.9f),66);frame.localPosition=c.shelter.center;frame.localScale=new Vector3(c.shelter.width,c.shelter.height,1);FastingReadability.FitFrame(frame);
                }
                PrefabUtility.SaveAsPrefabAsset(o,path);
            }finally{PrefabUtility.UnloadPrefabContents(o);}
        }
        var game=UnityEngine.Object.FindAnyObjectByType<FastingMinigame>();var feedback=game.GetComponent<FastingReadability>();if(!feedback)feedback=game.gameObject.AddComponent<FastingReadability>();feedback.game=game;
        foreach(var bg in game.backgrounds)bg.color=new Color(.72f,.72f,.72f);
        var guides=GameObject.Find("FastingGroundGuides");if(!guides)guides=new GameObject("FastingGroundGuides");
        float center=game.settings.ground.center.x;
        for(int i=0;i<3;i++){
            Rect("Lane"+i,guides.transform,new Vector3(center,game.settings.lanes[i],0),new Vector2(game.settings.ground.width,1.04f),new Color(.06f,.13f,.19f,.38f),12);
            for(int cell=0;cell<57;cell++)Rect("Lane"+i+"_Step"+cell,guides.transform,new Vector3(game.settings.ground.xMin+cell*1.5f,game.settings.lanes[i],0),new Vector2(.055f,.35f),new Color(.95f,.91f,.71f,.32f),14);
        }
        feedback.playerFeet=Rect("PlayerFeetHitArea",guides.transform,game.player.position,game.feetHitSize,new Color(.3f,.8f,1,.9f),320);
        feedback.playerFeetFrame=Frame("PlayerFeetOutline",guides.transform,Color.white,322);
        feedback.destinations=new SpriteRenderer[4];feedback.directionArrows=new SpriteRenderer[4];
        for(int i=0;i<4;i++){
            var pad=Rect("MoveDestination"+i,guides.transform,Vector3.zero,new Vector2(.75f,.46f),FastingReadability.SafeColor,300);feedback.destinations[i]=pad;
            // Children are normalised against the pad scale, so arrows stay on the ground.
            var shaft=Rect("Arrow",pad.transform,Vector3.zero,new Vector2(.013f,.0025f),Color.white,302);shaft.transform.localRotation=Quaternion.Euler(0,0,i==0?90:i==1?0:i==2?-90:180);feedback.directionArrows[i]=shaft;
        }
        feedback.stageGate=Rect("NextStageBoundary",guides.transform,Vector3.zero,new Vector2(.07f,3.4f),new Color(1,.8f,.15f,.8f),22).transform;
        feedback.shelterViews=game.covers.Select(c=>new FastingReadability.CoverView{cover=c,fill=c.transform.Find("VisibleShelterArea").GetComponent<SpriteRenderer>(),frame=c.transform.Find("ShelterOutline")}).ToArray();
        var hud=game.dayText.transform.parent;feedback.movementText=hud.Find("MovementStatus").GetComponent<TMP_Text>();feedback.coverText=hud.Find("CoverStatus").GetComponent<TMP_Text>();feedback.exposureText=hud.Find("ExposureStatus").GetComponent<TMP_Text>();feedback.exposureFill=hud.Find("ExposureTrack/ExposureFill").GetComponent<Image>();feedback.damagePanel=hud.Find("DamageNotice").gameObject;feedback.damageText=hud.Find("DamageNotice/Label").GetComponent<TMP_Text>();
        feedback.Refresh();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();return "Patched storm visibility, exact floor footprint, 3 lanes, destination states, cover zones, exposure meter, damage reasons. Existing positions preserved.";
    }
}
