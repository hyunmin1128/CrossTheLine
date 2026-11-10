using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using CrossTheLine.Minigames;
public static class RemoveFastingShelters
{
    static void Clean(GameObject root){foreach(var t in root.GetComponentsInChildren<TMP_Text>(true)){if(!t.text.Contains("엄폐"))continue;t.text=string.Join("\n",t.text.Split('\n').Where(line=>!line.Contains("엄폐")));PrefabUtility.RecordPrefabInstancePropertyModifications(t);EditorUtility.SetDirty(t);}}
    public static string Run(){if(EditorApplication.isPlaying)throw new Exception("Stop Play");var g=UnityEngine.Object.FindAnyObjectByType<FastingTopDownGame>();if(!g||g.gameObject.scene.isDirty)throw new Exception("Open saved scene");var covers=g.entities.Where(e=>e&&e.kind==FastingTopDownEntity.Kind.Cover).ToArray();g.entities=g.entities.Where(e=>e&&e.kind!=FastingTopDownEntity.Kind.Cover).ToArray();foreach(var e in covers)UnityEngine.Object.DestroyImmediate(e.gameObject);var hud=g.introduction.transform.parent.gameObject;Clean(hud);EditorUtility.SetDirty(g);EditorSceneManager.MarkSceneDirty(g.gameObject.scene);EditorSceneManager.SaveScene(g.gameObject.scene);string p="Assets/Prefabs/Minigames/FastingTopDown/FastingTopDownHUD.prefab";var root=PrefabUtility.LoadPrefabContents(p);Clean(root);PrefabUtility.SaveAsPrefabAsset(root,p);PrefabUtility.UnloadPrefabContents(root);AssetDatabase.SaveAssets();return "Removed "+covers.Length+" shelter objects and their blue zones; pickup references preserved.";}
}
