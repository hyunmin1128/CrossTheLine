using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using TMPro;
using CrossTheLine.Minigames;
public static class UpdateFastingShelterCells
{
    public static string Run()
    {
        if(EditorApplication.isPlaying)throw new Exception("Stop Play");var g=UnityEngine.Object.FindAnyObjectByType<FastingTopDownGame>();if(!g||g.gameObject.scene.isDirty)throw new Exception("Open saved top-down scene");
        foreach(var e in g.entities){if(e.kind!=FastingTopDownEntity.Kind.Cover)continue;e.laneWidth=g.settings.laneSpacing;e.RefreshShelterMarkers();PrefabUtility.RecordPrefabInstancePropertyModifications(e);foreach(var t in e.GetComponentsInChildren<Transform>(true))PrefabUtility.RecordPrefabInstancePropertyModifications(t);if(e.windMarker)PrefabUtility.RecordPrefabInstancePropertyModifications(e.windMarker.gameObject);EditorUtility.SetDirty(e);}
        string path="Assets/Prefabs/Minigames/FastingTopDown/RockCover.prefab";var root=PrefabUtility.LoadPrefabContents(path);var cover=root.GetComponent<FastingTopDownEntity>();cover.laneWidth=g.settings.laneSpacing;cover.RefreshShelterMarkers();PrefabUtility.SaveAsPrefabAsset(root,path);PrefabUtility.UnloadPrefabContents(root);
        var guide=g.introduction.transform.Find("FiveItemGuide").GetComponent<TMP_Text>();guide.text=guide.text.Replace("Space 유지: 멈춰 기도 / 놓기: 자동 전진 재개","Space 유지: 이동하며 기도 / 좌우 회피도 가능");guide.text+="\n파란 칸: 엄폐 범위 / 초록 칸: 해당 방향 엄폐 적용";
        var controls=g.help.transform.parent.Find("Controls").GetComponent<TMP_Text>();controls.text="A/D 또는 ←/→ : 한 칸 이동 · Space 이동하며 기도 · E 수통 · R 재시작";
        foreach(var t in new[]{guide,controls}){PrefabUtility.RecordPrefabInstancePropertyModifications(t);EditorUtility.SetDirty(t);}EditorSceneManager.MarkSceneDirty(g.gameObject.scene);EditorSceneManager.SaveScene(g.gameObject.scene);AssetDatabase.SaveAssets();return "Saved lane-width shelter cells, hidden out-of-map cells, and moving-prayer guide";
    }
}
