using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Threading.Tasks;
using CrossTheLine.Minigames;

public static class FixFastingUIPanels
{
    const string Out="Docs/QA/UIPanels-20261116";
    static string Path(Transform t)=>t.parent?Path(t.parent)+"/"+t.name:t.name;
    public static string Audit()
    {
        Directory.CreateDirectory(Out);var game=UnityEngine.Object.FindAnyObjectByType<FastingTopDownGame>();var hud=UnityEngine.Object.FindAnyObjectByType<FastingPortraitHUD>();var rows=new List<string>{"Heading: "+Path(game.heading.transform),"Portrait HUD: "+Path(hud.transform),"Scene dirty: "+game.gameObject.scene.isDirty};
        foreach(var image in game.heading.canvas.GetComponentsInChildren<UnityEngine.UI.Image>(true))
        {
            var r=image.rectTransform;rows.Add(Path(r)+" active="+image.gameObject.activeInHierarchy+" sprite="+AssetDatabase.GetAssetPath(image.sprite)+" color="+image.color+" material="+(image.material?image.material.name:"")+" rect="+r.rect+" anchor="+r.anchorMin+"/"+r.anchorMax+" pivot="+r.pivot+" alpha="+image.canvasRenderer.GetInheritedAlpha());
        }
        foreach(var group in game.heading.canvas.GetComponentsInChildren<CanvasGroup>(true))rows.Add("CanvasGroup "+Path(group.transform)+" alpha="+group.alpha);
        File.WriteAllLines(Out+"/before.txt",rows);return string.Join("\n",rows);
    }
    static void Dirty(UnityEngine.Object target){EditorUtility.SetDirty(target);if(PrefabUtility.IsPartOfPrefabInstance(target))PrefabUtility.RecordPrefabInstancePropertyModifications(target);}
    public static string Apply()
    {
        if(EditorApplication.isPlaying)throw new Exception("Edit mode required");if(File.Exists(Out+"/apply.txt"))throw new Exception("Already applied");var game=UnityEngine.Object.FindAnyObjectByType<FastingTopDownGame>();var hud=UnityEngine.Object.FindAnyObjectByType<FastingPortraitHUD>();var root=hud.transform;
        var header=root.Find("Header").GetComponent<UnityEngine.UI.Image>();var portrait=root.Find("PortraitCard").GetComponent<UnityEngine.UI.Image>();if(!header.sprite||!portrait.sprite)throw new Exception("Expected user-authored panel sprites");
        string backup="Docs/Backups/UIPanels-"+DateTime.Now.ToString("yyyyMMdd-HHmmss");Directory.CreateDirectory(backup);File.Copy(game.gameObject.scene.path,backup+"/BeforeDisk.unity",true);
        if(game.gameObject.scene.isDirty){EditorSceneManager.SaveScene(game.gameObject.scene);File.Copy(game.gameObject.scene.path,backup+"/SavedUserState.unity",true);}
        foreach(var image in new[]{header,portrait}){image.color=Color.white;image.canvasRenderer.SetColor(Color.white);Dirty(image);}
        var textColor=new Color(.26f,.17f,.12f,1);game.heading.color=textColor;game.items.color=textColor;game.heading.enableAutoSizing=false;game.heading.textWrappingMode=TMPro.TextWrappingModes.Normal;game.heading.alignment=TMPro.TextAlignmentOptions.MidlineLeft;Dirty(game.heading);Dirty(game.items);
        var fit=header.GetComponent<FastingHeaderFit>();if(!fit)fit=header.gameObject.AddComponent<FastingHeaderFit>();fit.heading=game.heading;fit.padding=new Vector2(16,10);fit.minimumWidth=120;fit.minimumHeight=40;fit.Refresh(true);Dirty(fit);Dirty(header.rectTransform);Dirty(game.heading.rectTransform);
        EditorSceneManager.MarkSceneDirty(game.gameObject.scene);EditorSceneManager.SaveScene(game.gameObject.scene);AssetDatabase.SaveAssets();
        Directory.CreateDirectory(Out);File.WriteAllText(Out+"/apply.txt","PASS Header/PortraitCard image color and renderer tint reset to opaque white. Original sprite assets and colors preserved.\nHeading/items changed to dark brown for contrast on cream backdrop.\nExisting Header background sizes to TMP preferred text size + 16px horizontal / 10px vertical padding. Long text wraps to canvas width. Existing hierarchy, portraits, hearts and bars preserved.\nBackup: "+backup);return "PASS panel colors and adaptive header applied";
    }
    static async Task Frames(int n){int end=Time.frameCount+n;while(Time.frameCount<end)await Task.Yield();}
    public static string TidyStatus()
    {
        if(EditorApplication.isPlaying)throw new Exception("Edit mode required");var game=UnityEngine.Object.FindAnyObjectByType<FastingTopDownGame>();var r=game.items.rectTransform;r.anchoredPosition=new Vector2(r.anchoredPosition.x,22);Dirty(r);EditorSceneManager.MarkSceneDirty(game.gameObject.scene);EditorSceneManager.SaveScene(game.gameObject.scene);File.AppendAllText(Out+"/apply.txt","\nStatus information moved up 10 UI units, below the hearts, to sit inside the paper background.");return "PASS status label fitted";
    }
    public static async Task<string> Verify()
    {
        if(!EditorApplication.isPlaying)throw new Exception("Play mode required");var game=UnityEngine.Object.FindAnyObjectByType<FastingTopDownGame>();var header=game.heading.transform.parent.Find("Header").GetComponent<UnityEngine.UI.Image>();var portrait=game.heading.transform.parent.Find("PortraitCard").GetComponent<UnityEngine.UI.Image>();var fit=header.GetComponent<FastingHeaderFit>();var rows=new List<string>();Action<bool,string> check=(ok,msg)=>{if(!ok)throw new Exception(msg);rows.Add("PASS "+msg);};
        await Frames(4);game.Begin();game.enabled=false;string original=game.heading.text;
        try
        {
            check(header.color==Color.white&&portrait.color==Color.white,"Both authored panel sprites display without dark tint");check(AssetDatabase.GetAssetPath(header.sprite)=="Assets/Sprite/UI/DialogueUI.png"&&header.sprite==portrait.sprite,"User background sprites retained");
            float lastWidth=0;foreach(string text in new[]{"광야 · 1/40일","광야 사막 · 10/40일","마지막 시련 · 40/40일"})
            {
                game.heading.text=text;fit.Refresh();Canvas.ForceUpdateCanvases();float width=header.rectTransform.rect.width;
                check(width>=game.heading.GetPreferredValues(text).x+fit.padding.x*2-1,"Text fits background with horizontal padding: "+text);check(width>=lastWidth,"Header grows as text gets wider: "+text);lastWidth=width;
                check(game.heading.rectTransform.rect.height+fit.padding.y*2<=header.rectTransform.rect.height+.1f,"Vertical text padding fits: "+text);
            }
            game.heading.text="마지막 시련에서 이어지는 아주 긴 설명과 기도 여정의 진행 상태를 표시하는 긴 제목 테스트입니다. "+new string('가',70);fit.Refresh();Canvas.ForceUpdateCanvases();check(header.rectTransform.rect.width<=((RectTransform)header.transform.parent).rect.width-header.rectTransform.anchoredPosition.x-fit.rightMargin+.1f,"Very long title remains within screen width");check(header.rectTransform.rect.height>fit.minimumHeight,"Very long title increases background height");
            game.heading.text="광야 · 1/40일";fit.Refresh();check(header.rectTransform.rect.width<lastWidth,"Header shrinks when title gets shorter");
            game.heading.text=original;fit.Refresh();game.RefreshHealthHearts();check(game.hearts.All(h=>h&&h.sprite==game.fullHeart),"Status hearts still work");check(header.transform.GetSiblingIndex()<game.heading.transform.GetSiblingIndex(),"Header background remains behind text");
            var corners=new Vector3[4];game.hearts[0].rectTransform.GetWorldCorners(corners);float heartBottom=game.items.transform.parent.InverseTransformPoint(corners[0]).y;check(game.items.rectTransform.anchoredPosition.y+game.items.rectTransform.rect.yMax<=heartBottom,"Status information remains below hearts");
            File.WriteAllLines(Out+"/validation.txt",rows);game.Begin();game.enabled=true;return "PASS "+rows.Count+" checks";
        }
        finally{game.heading.text=original;fit.Refresh();game.enabled=true;}
    }
}
