using System;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using CrossTheLine.Minigames;
public static class InstallFastingHitFeedback
{
    public static string Run()
    {
        if(EditorApplication.isPlaying)throw new Exception("Stop Play first");
        var g=UnityEngine.Object.FindAnyObjectByType<FastingTopDownGame>();
        if(!g||g.gameObject.scene.isDirty)throw new Exception("Open saved minigame scene first");
        if(g.playerBody.transform==g.player)throw new Exception("Artwork must be a separate child of gameplay root");
        string path="Assets/Prefabs/Minigames/FastingTopDown/FastingHitFeedback.prefab";
        if(!AssetDatabase.LoadAssetAtPath<GameObject>(path))
        {
            var root=new GameObject("FastingHitFeedback",typeof(MeshFilter),typeof(MeshRenderer),typeof(FastingHitFeedback));
            PrefabUtility.SaveAsPrefabAsset(root,path);UnityEngine.Object.DestroyImmediate(root);
        }
        if(!g.hitFeedback)
        {
            var obj=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path),g.player);
            obj.transform.localPosition=Vector3.zero;g.hitFeedback=obj.GetComponent<FastingHitFeedback>();
        }
        string materialPath="Assets/Prefabs/Minigames/FastingTopDown/FastingHitFeedback.mat";
        var material=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if(!material){material=new Material(Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default"));AssetDatabase.CreateAsset(material,materialPath);}
        var content=PrefabUtility.LoadPrefabContents(path);content.GetComponent<FastingHitFeedback>().effectMaterial=material;
        PrefabUtility.SaveAsPrefabAsset(content,path);PrefabUtility.UnloadPrefabContents(content);
        g.hitFeedback.effectMaterial=material;g.hitFeedback.artwork=g.playerBody;
        PrefabUtility.RecordPrefabInstancePropertyModifications(g.hitFeedback);
        PrefabUtility.RecordPrefabInstancePropertyModifications(g);
        EditorUtility.SetDirty(g);EditorSceneManager.MarkSceneDirty(g.gameObject.scene);
        EditorSceneManager.SaveScene(g.gameObject.scene);AssetDatabase.SaveAssets();
        return "Installed editable feedback prefab; player artwork="+g.playerBody.name+"; gameplay root="+g.player.name;
    }
}
