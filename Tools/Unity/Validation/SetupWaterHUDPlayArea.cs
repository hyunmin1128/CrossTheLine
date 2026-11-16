using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using CrossTheLine.Minigames;

public static class SetupWaterHUDPlayArea
{
    const string Out="Docs/QA/WaterHUDMovement-20261116",BackupRoot="Docs/Backups/WaterHUDMovement-20261116-161024";
    static void Dirty(UnityEngine.Object o){EditorUtility.SetDirty(o);if(PrefabUtility.IsPartOfPrefabInstance(o))PrefabUtility.RecordPrefabInstancePropertyModifications(o);}
    static RectTransform Rect(Transform parent,string name,float x,float y,float width,float height)
    {
        var r=(RectTransform)parent.Find(name);if(!r)throw new Exception("Missing HUD element "+name);r.anchorMin=r.anchorMax=Vector2.zero;r.pivot=Vector2.zero;r.anchoredPosition=new Vector2(x,y);r.sizeDelta=new Vector2(width,height);Dirty(r);return r;
    }
    static UnityEngine.Rect VisiblePixels()
    {
        float minX=1000,minY=1000,maxX=-1000,maxY=-1000;
        for(int i=0;i<4;i++)
        {
            string p="Assets/Sprite/Minigames/FastingTopDown/JesusForwardWalk/Walk_"+i+".png";var sprite=AssetDatabase.LoadAssetAtPath<Sprite>(p);var image=new Texture2D(2,2);image.LoadImage(File.ReadAllBytes(p));
            for(int y=0;y<image.height;y++)for(int x=0;x<image.width;x++)if(image.GetPixel(x,y).a>.5f){minX=Mathf.Min(minX,(x-sprite.pivot.x)/sprite.pixelsPerUnit);maxX=Mathf.Max(maxX,(x+1-sprite.pivot.x)/sprite.pixelsPerUnit);minY=Mathf.Min(minY,(y-sprite.pivot.y)/sprite.pixelsPerUnit);maxY=Mathf.Max(maxY,(y+1-sprite.pivot.y)/sprite.pixelsPerUnit);}UnityEngine.Object.DestroyImmediate(image);
        }
        return UnityEngine.Rect.MinMaxRect(minX,minY,maxX,maxY);
    }
    public static string Apply()
    {
        if(EditorApplication.isPlaying)throw new Exception("Edit mode required");if(File.Exists(Out+"/setup.txt"))throw new Exception("Already applied");var game=UnityEngine.Object.FindAnyObjectByType<FastingTopDownGame>();var hud=UnityEngine.Object.FindAnyObjectByType<FastingPortraitHUD>();var card=(RectTransform)hud.transform.Find("PortraitCard");Directory.CreateDirectory(BackupRoot);Directory.CreateDirectory(Out);File.Copy(game.gameObject.scene.path,BackupRoot+"/BeforeDisk.unity",true);
        if(game.gameObject.scene.isDirty){EditorSceneManager.SaveScene(game.gameObject.scene);File.Copy(game.gameObject.scene.path,BackupRoot+"/SavedUserState.unity",true);}
        card.sizeDelta=new Vector2(600,176);Dirty(card);
        Rect(card,hud.ring.name,12,22,132,132);var mask=Rect(card,"PortraitCircleMask",24,34,108,108);var face=mask.Find("PortraitFace") as RectTransform;if(face){face.anchorMin=Vector2.zero;face.anchorMax=Vector2.one;face.offsetMin=face.offsetMax=Vector2.zero;Dirty(face);}
        Rect(card,"PrayerTrack",156,126,300,24);Rect(card,game.prayerFill.name,156,126,300,24);Rect(card,"PrayerState",156,156,300,4);
        card.Find("HealthTrack").GetComponent<UnityEngine.UI.Image>().enabled=false;Dirty(card.Find("HealthTrack").GetComponent<UnityEngine.UI.Image>());
        var row=Rect(card,"UserHeartRow",156,58,300,58);
        for(int i=0;i<game.hearts.Length;i++){var r=game.hearts[i].rectTransform;r.anchorMin=new Vector2((float)i/game.hearts.Length,0);r.anchorMax=new Vector2((float)(i+1)/game.hearts.Length,1);r.pivot=new Vector2(.5f,.5f);r.offsetMin=new Vector2(2,0);r.offsetMax=new Vector2(-2,0);game.hearts[i].preserveAspect=true;Dirty(r);Dirty(game.hearts[i]);}
        var source=(TextureImporter)AssetImporter.GetAtPath("Assets/Sprite/Objects/WaterSkin.png");File.Copy("Assets/Sprite/Objects/WaterSkin.png.meta",BackupRoot+"/WaterSkin.png.meta",true);source.filterMode=FilterMode.Point;source.mipmapEnabled=false;source.textureCompression=TextureImporterCompression.Uncompressed;source.SaveAndReimport();
        var water=card.Find("WaterSkinIcon");var image=water?water.GetComponent<UnityEngine.UI.Image>():new GameObject("WaterSkinIcon",typeof(RectTransform),typeof(UnityEngine.UI.Image)).GetComponent<UnityEngine.UI.Image>();image.transform.SetParent(card,false);image.sprite=AssetDatabase.LoadAllAssetsAtPath("Assets/Sprite/Objects/WaterSkin.png").OfType<Sprite>().Single();image.type=UnityEngine.UI.Image.Type.Simple;image.preserveAspect=true;image.raycastTarget=false;image.color=Color.white;Rect(card,"WaterSkinIcon",484,70,96,96);Dirty(image);hud.waterSkin=image;Dirty(hud);
        Rect(card,game.items.name,476,38,112,28);game.items.fontSize=20;game.items.enableAutoSizing=false;game.items.alignment=TMPro.TextAlignmentOptions.Center;game.items.text="수통[E]";Dirty(game.items);
        var area=game.GetComponent<FastingPlayArea>();if(!area)area=game.gameObject.AddComponent<FastingPlayArea>();area.game=game;area.header=(RectTransform)hud.transform.Find("Header");area.statusPanel=card;area.background=game.backgrounds.First(b=>b);area.horizontalPixelEdges=new Vector2(52,282);area.backgroundPixelWidth=320;area.visibleSpriteRect=VisiblePixels();game.playArea=area;Dirty(area);Dirty(game);
        EditorSceneManager.MarkSceneDirty(game.gameObject.scene);EditorSceneManager.SaveScene(game.gameObject.scene);AssetDatabase.SaveAssets();
        File.WriteAllText(Out+"/setup.txt","PASS larger hearts, prayer gauge and right WaterSkin icon with label below. Shield count readout removed.\nRoad boundaries: x=52..282 in original 320-pixel background; upper/lower from actual Header bottom and PortraitCard top.\nWhole-character alpha union: "+area.visibleSpriteRect+"\nBackground markers apply to entire scrolling tile, not merely the player center.\nBackup="+BackupRoot);return "PASS HUD and expanded movement area applied";
    }
    static async Task Frames(int n){int end=Time.frameCount+n;while(Time.frameCount<end)await Task.Yield();}
    static void Field(FastingTopDownGame g,string n,object v)=>typeof(FastingTopDownGame).GetField(n,System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(g,v);
    static bool PictureFits(FastingPlayArea area)
    {
        var body=area.game.playerBody.transform;var rect=area.visibleSpriteRect;var road=area.VisibleWorldArea;var min=body.TransformPoint(rect.min);var max=body.TransformPoint(rect.max);return min.x>=road.xMin-.001f&&max.x<=road.xMax+.001f&&min.y>=road.yMin-.001f&&max.y<=road.yMax+.001f;
    }
    public static async Task<string> Verify()
    {
        if(!EditorApplication.isPlaying)throw new Exception("Play mode required");var game=UnityEngine.Object.FindAnyObjectByType<FastingTopDownGame>();var hud=UnityEngine.Object.FindAnyObjectByType<FastingPortraitHUD>();var log=new List<string>();var errors=new List<string>();Application.LogCallback capture=(m,s,t)=>{if(t==LogType.Error||t==LogType.Exception||t==LogType.Assert)errors.Add(m);};Application.logMessageReceived+=capture;Action<bool,string> check=(ok,m)=>{if(!ok)throw new Exception(m);log.Add("PASS "+m);};
        var entities=game.entities;var director=game.encounterDirector;
        try
        {
            await Frames(4);game.Begin();game.enabled=false;game.encounterDirector=null;game.entities=Array.Empty<FastingTopDownEntity>();Field(game,"wave",float.PositiveInfinity);Field(game,"grace",10000f);hud.Refresh();await Frames(3);check(game.player.position.sqrMagnitude<.0001f,"Initial spawn remains at original world origin");
            check(game.items.text=="수통[E]"&&!game.items.text.Contains("방패"),"Water-only label, no shield count");check(hud.waterSkin&&hud.waterSkin.sprite&&hud.waterSkin.color==hud.waterAvailableColor,"WaterSkin available state uses original color");
            check(game.hearts.All(h=>h.rectTransform.rect.height>=56&&h.rectTransform.rect.width>=55),"All five hearts enlarged to roughly 56x58");check(hud.waterSkin.rectTransform.anchoredPosition.x>game.prayerFill.rectTransform.anchoredPosition.x+game.prayerFill.rectTransform.rect.width,"WaterSkin sits right of gauge/hearts");check(game.items.rectTransform.anchoredPosition.y+game.items.rectTransform.rect.height<hud.waterSkin.rectTransform.anchoredPosition.y,"Water label below WaterSkin");
            game.UseWater();hud.Refresh();check(!game.HasWater&&hud.waterSkin.gameObject.activeInHierarchy&&hud.waterSkin.color==hud.waterEmptyColor,"Used water stays visible with disabled tint");
            var well=entities.First(e=>e&&e.kind==FastingTopDownEntity.Kind.Water);well.transform.position=game.player.position;well.stage=0;well.SetUsed(false);well.gameObject.SetActive(true);game.entities=new[]{well};game.Tick(.001f,Vector2.zero);hud.Refresh();check(game.HasWater&&hud.waterSkin.color==hud.waterAvailableColor,"Actual well collision restores original water tint");game.entities=Array.Empty<FastingTopDownEntity>();
            var area=game.playArea;check(area&&area.RootArea.width>13.4f,"Movement width expanded beyond old 13.4 world units");check(area.RootArea.yMin<0,"Initial movement extends below old world-zero floor");
            foreach(var target in new[]{new Vector2(-100,0),new Vector2(100,0),new Vector2(0,-100),new Vector2(0,100),new Vector2(-100,-100),new Vector2(-100,100),new Vector2(100,-100),new Vector2(100,100)}){game.player.position=area.Clamp(target);check(area.Contains(game.player.position)&&PictureFits(area),"Whole sprite stays inside marked/UI boundaries at "+target);}
            game.Begin();game.enabled=false;game.encounterDirector=null;game.entities=Array.Empty<FastingTopDownEntity>();Field(game,"wave",float.PositiveInfinity);Field(game,"grace",10000f);
            foreach(var direction in new[]{Vector2.down,Vector2.up,Vector2.left,Vector2.right}){for(int i=0;i<80;i++)game.Tick(.1f,direction);check(PictureFits(area),"Sustained input and auto-scroll respect bounds: "+direction);}
            game.hitFeedback.Play(FastingTopDownHazard.Kind.Sand);game.hitFeedback.Advance(game.hitFeedback.sandAirSeconds*.5f);game.player.position=area.Clamp(new Vector2(100,100));check(PictureFits(area),"Airborne artwork also stays below header and inside side edges");game.hitFeedback.Advance(4);
            game.entities=entities;game.encounterDirector=director;game.Begin();hud.Refresh();check(hud.waterSkin.color==hud.waterAvailableColor&&game.hearts.All(h=>h.sprite==game.fullHeart),"Restart restores water and full hearts");check(errors.Count==0,"No runtime errors during validation");File.WriteAllLines(Out+"/validation.txt",log);game.enabled=true;return "PASS "+log.Count+" checks";
        }
        catch(Exception e){File.WriteAllLines(Out+"/validation.txt",log.Concat(new[]{"FAIL "+e}));throw;}
        finally{Application.logMessageReceived-=capture;if(game){game.entities=entities;game.encounterDirector=director;game.enabled=true;}}
    }
    public static string PreviewAvailable(){var game=UnityEngine.Object.FindAnyObjectByType<FastingTopDownGame>();game.Begin();game.enabled=false;return "Water available preview";}
    public static string PreviewEmpty(){var game=UnityEngine.Object.FindAnyObjectByType<FastingTopDownGame>();game.UseWater();UnityEngine.Object.FindAnyObjectByType<FastingPortraitHUD>().Refresh();return "Water used preview";}
    public static string PreviewBounds()
    {
        var game=UnityEngine.Object.FindAnyObjectByType<FastingTopDownGame>();var area=game.playArea.VisibleWorldArea;var root=new GameObject("TemporaryPlayAreaGuide");var line=root.AddComponent<LineRenderer>();line.sharedMaterial=game.hitFeedback.effectMaterial;line.startColor=line.endColor=new Color(.15f,.9f,1,.9f);line.startWidth=line.endWidth=.035f;line.sortingOrder=1000;line.useWorldSpace=true;line.positionCount=5;line.SetPositions(new[]{new Vector3(area.xMin,area.yMin,0),new Vector3(area.xMin,area.yMax,0),new Vector3(area.xMax,area.yMax,0),new Vector3(area.xMax,area.yMin,0),new Vector3(area.xMin,area.yMin,0)});return "Temporary cyan guide shows sprite-visible area (not saved)";
    }
}
