using System;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using CrossTheLine.Minigames;
public static class InstallFastingCombatPolish
{
    public static string Run()
    {
        if(EditorApplication.isPlaying)throw new Exception("Stop Play");var g=UnityEngine.Object.FindAnyObjectByType<FastingTopDownGame>();if(!g||g.gameObject.scene.path!="Assets/Scenes/Minigames/Chapter03_Fasting_TopDown_Test.unity")throw new Exception("Minigame scene required");
        g.settings.manualMoveSpeed=5.2f;g.settings.waveIntervals=new float[]{1.35f,1.25f,1.15f,1f};g.settings.cactusDamage=2;g.settings.quicksandStunSeconds=2;EditorUtility.SetDirty(g.settings);
        g.hitFeedback.sandAirHeight=2.4f;g.hitFeedback.sandAirSeconds=1.35f;PrefabUtility.RecordPrefabInstancePropertyModifications(g.hitFeedback);
        string path="Assets/Prefabs/Minigames/FastingTopDown/FastingPlayerAura.prefab";
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(!prefab){var o=new GameObject("PlayerAura",typeof(MeshFilter),typeof(MeshRenderer),typeof(FastingPlayerAura));o.GetComponent<FastingPlayerAura>().effectMaterial=g.hitFeedback.effectMaterial;prefab=PrefabUtility.SaveAsPrefabAsset(o,path);UnityEngine.Object.DestroyImmediate(o);}
        if(!g.playerAura)g.playerAura=((GameObject)PrefabUtility.InstantiatePrefab(prefab,g.player)).GetComponent<FastingPlayerAura>();
        g.playerAura.game=g;g.playerAura.artwork=g.playerBody;g.playerAura.effectMaterial=g.hitFeedback.effectMaterial;
        PrefabUtility.RecordPrefabInstancePropertyModifications(g.playerAura);PrefabUtility.RecordPrefabInstancePropertyModifications(g);
        EditorSceneManager.MarkSceneDirty(g.gameObject.scene);EditorSceneManager.SaveScene(g.gameObject.scene);AssetDatabase.SaveAssets();
        return "Speed 5.2, denser attack waves, cactus 2, quicksand 2 seconds, airborne 2.4 / 1.35 seconds, editable shield/boost aura installed.";
    }
}
