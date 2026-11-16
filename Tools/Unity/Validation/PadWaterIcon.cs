using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using CrossTheLine.Minigames;

public static class PadWaterIcon
{
    public static string Apply()
    {
        if(EditorApplication.isPlaying)throw new Exception("Edit mode required");var hud=UnityEngine.Object.FindAnyObjectByType<FastingPortraitHUD>();var scene=hud.game.gameObject.scene;var backup="Docs/Backups/WaterIconPadding-"+DateTime.Now.ToString("yyyyMMdd-HHmmss");Directory.CreateDirectory(backup);File.Copy(scene.path,backup+"/BeforeDisk.unity",true);if(scene.isDirty){EditorSceneManager.SaveScene(scene);File.Copy(scene.path,backup+"/SavedUserState.unity",true);}
        AssetDatabase.ImportAsset("Assets/Sprite/Objects/WaterSkin.png",ImportAssetOptions.ForceUpdate);
        var image=hud.waterSkin;var rect=image.rectTransform;rect.sizeDelta=new Vector2(84,84);rect.anchoredPosition=new Vector2(484,56);image.type=UnityEngine.UI.Image.Type.Simple;image.preserveAspect=true;image.useSpriteMesh=false;
        EditorUtility.SetDirty(image);EditorUtility.SetDirty(rect);PrefabUtility.RecordPrefabInstancePropertyModifications(image);PrefabUtility.RecordPrefabInstancePropertyModifications(rect);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);return "PASS full sprite retained, padded 84x84 at (484,56), whole-rectangle image rendering; backup="+backup;
    }
    public static string Preview()
    {
        var h=UnityEngine.Object.FindAnyObjectByType<FastingPortraitHUD>();h.game.Begin();h.game.enabled=false;h.Refresh();return "Water icon preview";
    }
}
