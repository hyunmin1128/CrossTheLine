using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using CrossTheLine.Minigames;
using TMPro;
public static class ApplyFastingRevisionNov12
{
    public static string Run()
    {
        if(EditorApplication.isPlaying)throw new Exception("Stop Play");
        var g=UnityEngine.Object.FindAnyObjectByType<FastingTopDownGame>();if(!g||g.gameObject.scene.isDirty)throw new Exception("Saved minigame required");
        var fx=g.screenHitFeedback;fx.shieldColor=new Color(.18f,.62f,1,.8f);PrefabUtility.RecordPrefabInstancePropertyModifications(fx);
        string path="Assets/Prefabs/Minigames/FastingTopDown/FastingWhiteFlash.mat";
        var mat=AssetDatabase.LoadAssetAtPath<Material>(path);if(!mat){mat=new Material(Shader.Find("CrossTheLine/FastingWhiteFlash"));AssetDatabase.CreateAsset(mat,path);}
        g.playerBody.sharedMaterial=mat;PrefabUtility.RecordPrefabInstancePropertyModifications(g.playerBody);
        var hud=g.heading.GetComponentInParent<Canvas>();
        foreach(var t in hud.GetComponentsInChildren<TMP_Text>(true))
        {
            bool intro=t.transform.IsChildOf(g.introduction.transform),end=t.transform.IsChildOf(g.resultPanel.transform);
            if(intro){t.text=t.text.Replace("자동 기도","Space 누르는 동안 기도 (이동속도 절반)").Replace("기도 자동 충전","Space 기도 · 이동속도 절반");}
            else if(!end&&t!=g.heading&&t!=g.items&&t.name!="FeverText")t.text="";
            PrefabUtility.RecordPrefabInstancePropertyModifications(t);
        }
        g.settings.hitGrace=1.2f;EditorUtility.SetDirty(g.settings);
        EditorSceneManager.MarkSceneDirty(g.gameObject.scene);EditorSceneManager.SaveScene(g.gameObject.scene);AssetDatabase.SaveAssets();
        return "Saved manual prayer, blue shield, white flash material, minimal UI; "+string.Join(",",hud.GetComponentsInChildren<TMP_Text>(true).Select(t=>t.name));
    }
}
