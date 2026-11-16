using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using CrossTheLine.Minigames;

public static class WaterIconOnly
{
    public static string Apply()
    {
        if(EditorApplication.isPlaying)throw new Exception("Edit mode required");var hud=UnityEngine.Object.FindAnyObjectByType<FastingPortraitHUD>();var game=hud.game;
        var backup="Docs/Backups/WaterIconOnly-Scene-"+DateTime.Now.ToString("yyyyMMdd-HHmmss");Directory.CreateDirectory(backup);File.Copy(game.gameObject.scene.path,backup+"/BeforeDisk.unity",true);if(game.gameObject.scene.isDirty){EditorSceneManager.SaveScene(game.gameObject.scene);File.Copy(game.gameObject.scene.path,backup+"/SavedUserState.unity",true);}
        game.items.text="";game.items.gameObject.SetActive(false);EditorUtility.SetDirty(game.items);EditorUtility.SetDirty(game.items.gameObject);PrefabUtility.RecordPrefabInstancePropertyModifications(game.items);PrefabUtility.RecordPrefabInstancePropertyModifications(game.items.gameObject);
        var icon=hud.waterSkin.rectTransform;icon.anchoredPosition=new Vector2(484,56);EditorUtility.SetDirty(icon);PrefabUtility.RecordPrefabInstancePropertyModifications(icon);EditorSceneManager.MarkSceneDirty(game.gameObject.scene);EditorSceneManager.SaveScene(game.gameObject.scene);return "Label hidden; icon center aligned with gauge/hearts block at local (532,104), 96x96 size; state tint unchanged.";
    }
    public static string Preview()
    {
        var hud=UnityEngine.Object.FindAnyObjectByType<FastingPortraitHUD>();var game=hud.game;game.Begin();game.enabled=false;hud.Refresh();if(game.items.text!=""||game.items.gameObject.activeSelf)throw new Exception("Label still visible");var ready=hud.waterSkin.color;game.UseWater();hud.Refresh();if(hud.waterSkin.color==ready)throw new Exception("Disabled tint missing");game.Begin();game.enabled=false;hud.Refresh();return "PASS no label; original and disabled water tint preserved; icon center="+hud.waterSkin.rectTransform.anchoredPosition;
    }
}
