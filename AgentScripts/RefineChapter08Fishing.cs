using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using CrossTheLine.Minigames.Fishing;
public static class RefineChapter08Fishing
{
    public static string Run()
    {
        if(EditorApplication.isPlaying)throw new Exception("Edit mode required");
        const string whitePath="Assets/Sprite/Minigames/Chapter08/FishingUIWhite.asset";
        var white=AssetDatabase.LoadAllAssetsAtPath(whitePath).OfType<Sprite>().FirstOrDefault();
        if(!white){var texture=new Texture2D(1,1,TextureFormat.RGBA32,false){name="FishingUIWhite",filterMode=FilterMode.Point};texture.SetPixel(0,0,Color.white);texture.Apply();AssetDatabase.CreateAsset(texture,whitePath);white=Sprite.Create(texture,new Rect(0,0,1,1),new Vector2(.5f,.5f),1);white.name="UI Rectangle";AssetDatabase.AddObjectToAsset(white,texture);AssetDatabase.SaveAssetIfDirty(texture);}
        string path="Assets/Prefabs/Minigames/Chapter08Fishing/Chapter08Fishing.prefab";var root=PrefabUtility.LoadPrefabContents(path);
        try
        {
            var g=root.GetComponent<NetFishingGame>();g.worldRoot=root.transform.Find("EditableSea").gameObject;
            g.fightPanel.GetComponent<UnityEngine.UI.Image>().color=new Color(.02f,.13f,.22f,1);
            g.fightPanel.transform.Find("Sea_Backdrop").GetComponent<UnityEngine.UI.Image>().color=new Color(.45f,.65f,.8f,1);
            ((RectTransform)g.fightPanel.transform.Find("FightTitle")).anchorMin=((RectTransform)g.fightPanel.transform.Find("FightTitle")).anchorMax=new Vector2(.35f,.82f);
            g.netFill.sprite=g.catchFill.sprite=white;
            var label=(RectTransform)root.transform.Find("FishingCanvas/NetLabel");label.anchorMin=label.anchorMax=new Vector2(.82f,.972f);
            var rope=new GameObject("NetRope",typeof(RectTransform),typeof(UnityEngine.UI.Image));rope.transform.SetParent(g.fightPanel.transform,false);rope.transform.SetSiblingIndex(4);var rr=(RectTransform)rope.transform;rr.anchorMin=rr.anchorMax=new Vector2(.39f,.5f);rr.sizeDelta=new Vector2(145,3);rr.localRotation=Quaternion.Euler(0,0,-6);rope.GetComponent<UnityEngine.UI.Image>().color=new Color(.55f,.38f,.19f);rope.GetComponent<UnityEngine.UI.Image>().raycastTarget=false;
            var center=new GameObject("TornNet",typeof(RectTransform));center.transform.SetParent(g.resultPanel.transform,false);var cr=(RectTransform)center.transform;cr.anchorMin=cr.anchorMax=new Vector2(.5f,.72f);cr.sizeDelta=new Vector2(170,170);
            var source=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprite/Minigames/Chapter08/ExportedLayers/Net.png");
            for(int i=0;i<2;i++){var o=new GameObject(i==0?"LeftHalf":"RightHalf",typeof(RectTransform),typeof(UnityEngine.UI.Image));o.transform.SetParent(center.transform,false);var r=(RectTransform)o.transform;r.anchorMin=r.anchorMax=new Vector2(.5f,.5f);r.sizeDelta=new Vector2(170,170);var image=o.GetComponent<UnityEngine.UI.Image>();image.sprite=source;image.raycastTarget=false;image.type=UnityEngine.UI.Image.Type.Filled;image.fillMethod=UnityEngine.UI.Image.FillMethod.Horizontal;image.fillOrigin=i;image.fillAmount=.5f;if(i==0)g.tearLeft=image;else g.tearRight=image;o.SetActive(false);}
            var result=(RectTransform)g.resultText.transform;result.anchorMin=result.anchorMax=new Vector2(.5f,.4f);result.sizeDelta=new Vector2(850,145);
            PrefabUtility.SaveAsPrefabAsset(root,path);
        }
        finally{PrefabUtility.UnloadPrefabContents(root);}
        var settings=AssetDatabase.LoadAssetAtPath<NetFishingSettings>("Assets/Settings/Minigames/Chapter08Fishing.asset");settings.progressPerGoodTap=.0155f;EditorUtility.SetDirty(settings);AssetDatabase.SaveAssetIfDirty(settings);
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        return "Opaque fishing backdrop, square gauges, rope and split-net presentation saved; charge .0155";
    }
}
