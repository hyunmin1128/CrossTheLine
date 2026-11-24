using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using CrossTheLine.Minigames.Fishing;
public static class ConfigureChapter08FishVariants
{
    public static string Run()
    {
        if(EditorApplication.isPlaying)throw new Exception("Edit mode required");
        string dir="Assets/Sprite/Minigames/Chapter08/Examples/Fish/";
        string[] names={"Fish_Round_Silver","Fish_Long_Gold","Fish_Whisker_Teal","Fish_Stocky_Copper"};var sprites=new Sprite[4];
        for(int i=0;i<names.Length;i++)
        {
            string p=dir+names[i]+".png";var importer=(TextureImporter)AssetImporter.GetAtPath(p);importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.filterMode=FilterMode.Point;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.mipmapEnabled=false;importer.alphaIsTransparency=true;importer.maxTextureSize=2048;importer.SaveAndReimport();sprites[i]=AssetDatabase.LoadAllAssetsAtPath(p).OfType<Sprite>().First();
        }
        string path="Assets/Prefabs/Minigames/Chapter08Fishing/Chapter08Fishing.prefab";var root=PrefabUtility.LoadPrefabContents(path);
        try
        {
            var g=root.GetComponent<NetFishingGame>();g.fishVariants=sprites;
            var image=g.fishRect.GetComponent<UnityEngine.UI.Image>();image.sprite=sprites[0];image.preserveAspect=true;
            g.fishRect.sizeDelta=new Vector2(Mathf.Max(64,g.fishRect.sizeDelta.x),Mathf.Max(48,g.fishRect.sizeDelta.y));
            var existing=g.resultPanel.transform.Find("CaughtFish");
            if(!existing)
            {
                var item=new GameObject("CaughtFish",typeof(RectTransform),typeof(UnityEngine.UI.Image));item.transform.SetParent(g.resultPanel.transform,false);existing=item.transform;
                var r=(RectTransform)existing;r.anchorMin=r.anchorMax=new Vector2(.5f,.72f);r.anchoredPosition=Vector2.zero;r.sizeDelta=new Vector2(180,130);
            }
            g.resultFishImage=existing.GetComponent<UnityEngine.UI.Image>();g.resultFishImage.sprite=sprites[0];g.resultFishImage.color=Color.white;g.resultFishImage.preserveAspect=true;g.resultFishImage.raycastTarget=false;existing.gameObject.SetActive(false);
            PrefabUtility.SaveAsPrefabAsset(root,path);
        }
        finally{PrefabUtility.UnloadPrefabContents(root);}
        var scene=EditorSceneManager.GetActiveScene();if(scene.path.EndsWith("Chapter08_Fishing_Test.unity")&&!scene.isDirty)EditorSceneManager.SaveScene(scene);
        return "Connected four equally likely fish sprites to the gauge and successful-catch preview";
    }
}
