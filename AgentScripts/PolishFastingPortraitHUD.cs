using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using CrossTheLine.Minigames;
public static class PolishFastingPortraitHUD
{
    public static string Run()
    {
        if(EditorApplication.isPlaying)throw new Exception("Stop Play");var g=UnityEngine.Object.FindAnyObjectByType<FastingTopDownGame>();var ui=UnityEngine.Object.FindAnyObjectByType<FastingPortraitHUD>();if(!g||!ui||g.gameObject.scene.isDirty)throw new Exception("Open saved scene");
        if(!ui.ring.GetComponent<CanvasRenderer>())ui.ring.gameObject.AddComponent<CanvasRenderer>();ui.ring.SetAllDirty();
        var frames=AssetDatabase.LoadAllAssetsAtPath("Assets/Sprite/Effect/Fire-Sheet.png").OfType<Sprite>().OrderBy(s=>s.name).ToArray();ui.fireFrames=frames;
        string pref="Assets/Prefabs/Minigames/FastingTopDown/FirePickup.prefab";var root=PrefabUtility.LoadPrefabContents(pref);var lp=root.GetComponent<FastingFireSpriteLoop>();lp.frames=frames;lp.target.sprite=frames[0];lp.target.transform.localScale=Vector3.one*(frames[0].pixelsPerUnit/32);lp.target.transform.localPosition=-frames[0].bounds.center*lp.target.transform.localScale.x;PrefabUtility.SaveAsPrefabAsset(root,pref);PrefabUtility.UnloadPrefabContents(root);
        foreach(var e in g.entities.Where(e=>e.kind==FastingTopDownEntity.Kind.Flame)){var loop=e.GetComponent<FastingFireSpriteLoop>();loop.frames=frames;loop.target.sprite=frames[0];loop.target.transform.localScale=Vector3.one*(frames[0].pixelsPerUnit/32);loop.target.transform.localPosition=-frames[0].bounds.center*loop.target.transform.localScale.x;PrefabUtility.RecordPrefabInstancePropertyModifications(loop);PrefabUtility.RecordPrefabInstancePropertyModifications(loop.target);PrefabUtility.RecordPrefabInstancePropertyModifications(loop.target.transform);}
        foreach(var heart in g.hearts)if(heart)heart.gameObject.SetActive(false);
        g.status.fontSize=13;g.status.rectTransform.anchoredPosition=new Vector2(142,112);g.status.rectTransform.sizeDelta=new Vector2(356,18);
        var card=ui.ring.transform.parent;var portrait=card.Find("JesusPortrait");if(!card.Find("PortraitCircleMask")){var maskObj=new GameObject("PortraitCircleMask",typeof(RectTransform));var r=maskObj.GetComponent<RectTransform>();r.SetParent(card,false);r.anchorMin=r.anchorMax=r.pivot=Vector2.zero;r.anchoredPosition=new Vector2(20,20);r.sizeDelta=new Vector2(96,96);var graphic=maskObj.AddComponent<FastingFeverRing>();graphic.thickness=100;graphic.fill=1;graphic.raycastTarget=false;var mask=maskObj.AddComponent<UnityEngine.UI.Mask>();mask.showMaskGraphic=false;maskObj.transform.SetSiblingIndex(0);portrait.SetParent(r,false);var pr=portrait.GetComponent<RectTransform>();pr.anchorMin=pr.anchorMax=pr.pivot=Vector2.zero;pr.anchoredPosition=Vector2.zero;pr.sizeDelta=new Vector2(96,96);}
        var hud=ui.gameObject;PrefabUtility.SaveAsPrefabAssetAndConnect(hud,"Assets/Prefabs/Minigames/FastingTopDown/FastingTopDownHUD.prefab",InteractionMode.AutomatedAction);ui.game=g;PrefabUtility.RecordPrefabInstancePropertyModifications(ui);EditorUtility.SetDirty(g);EditorSceneManager.MarkSceneDirty(g.gameObject.scene);EditorSceneManager.SaveScene(g.gameObject.scene);AssetDatabase.SaveAssets();return "Ring renderer, transparent fire sheet, portrait mask, status spacing and duplicate hearts fixed.";
    }
}
