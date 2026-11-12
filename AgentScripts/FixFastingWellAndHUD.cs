using System;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using CrossTheLine.Minigames;
using TMPro;
public static class FixFastingWellAndHUD
{
    static void Align(Transform root)
    {
        var card=root.Find("PortraitCard");var item=root.Find("Items");if(!item)item=card.Find("Items");
        if(!card||!item)throw new Exception("HUD card/items missing");
        item.SetParent(card,false);var r=(RectTransform)item;r.anchorMin=r.anchorMax=r.pivot=Vector2.zero;r.anchoredPosition=new Vector2(142,12);r.sizeDelta=new Vector2(284,22);r.localScale=Vector3.one;
        var label=item.GetComponent<TMP_Text>();label.fontSize=16;label.enableAutoSizing=false;label.alignment=TextAlignmentOptions.MidlineLeft;
    }
    public static string Run()
    {
        var g=UnityEngine.Object.FindAnyObjectByType<FastingTopDownGame>();if(EditorApplication.isPlaying||!g||g.gameObject.scene.isDirty)throw new Exception("Saved Edit scene required");
        string path="Assets/Prefabs/Minigames/FastingTopDown/FastingTopDownHUD.prefab";
        var root=PrefabUtility.LoadPrefabContents(path);Align(root.transform);PrefabUtility.SaveAsPrefabAsset(root,path);PrefabUtility.UnloadPrefabContents(root);
        // Preserve the user's 0.5 card scale and other scene overrides.
        var item=g.items.rectTransform;item.anchorMin=item.anchorMax=item.pivot=Vector2.zero;item.anchoredPosition=new Vector2(142,12);item.sizeDelta=new Vector2(284,22);item.localScale=Vector3.one;
        g.items.fontSize=16;g.items.alignment=TextAlignmentOptions.MidlineLeft;
        PrefabUtility.RecordPrefabInstancePropertyModifications(item);PrefabUtility.RecordPrefabInstancePropertyModifications(g.items);
        EditorSceneManager.MarkSceneDirty(g.gameObject.scene);EditorSceneManager.SaveScene(g.gameObject.scene);AssetDatabase.SaveAssets();
        return "Items parent="+item.parent.name+"; card scale="+item.parent.localScale;
    }
}
