using System;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using CrossTheLine.Minigames;
public static class InstallFastingSpawnSettings
{
    public static string Run(){
        if(EditorApplication.isPlaying)throw new Exception("Edit required");const string path="Assets/Settings/Minigames/FastingSpawnSettings.asset";var config=AssetDatabase.LoadAssetAtPath<FastingSpawnSettings>(path);
        if(!config){config=ScriptableObject.CreateInstance<FastingSpawnSettings>();AssetDatabase.CreateAsset(config,path);
            var kinds=new[]{new[]{FastingTopDownHazard.Kind.Rock,FastingTopDownHazard.Kind.Sand,FastingTopDownHazard.Kind.Cactus,FastingTopDownHazard.Kind.Quicksand},new[]{FastingTopDownHazard.Kind.Rock,FastingTopDownHazard.Kind.Bat,FastingTopDownHazard.Kind.Collapse,FastingTopDownHazard.Kind.Crack},new[]{FastingTopDownHazard.Kind.Rock,FastingTopDownHazard.Kind.Tomb,FastingTopDownHazard.Kind.Wall,FastingTopDownHazard.Kind.Poison},new[]{FastingTopDownHazard.Kind.Fire,FastingTopDownHazard.Kind.Thorns,FastingTopDownHazard.Kind.Pillar,FastingTopDownHazard.Kind.Reaper}};
            config.stages=new FastingSpawnSettings.Stage[4];string[] names={"사막","동굴","폐유적","마지막 시련"};for(int i=0;i<4;i++){var stage=config.stages[i]=new FastingSpawnSettings.Stage{name=names[i],frequency=1,obstacles=new FastingSpawnSettings.Entry[4]};for(int j=0;j<4;j++)stage.obstacles[j]=new FastingSpawnSettings.Entry{kind=kinds[i][j],weight=i==3?(j==3?35:65f/3):25};}
        }
        var g=UnityEngine.Object.FindAnyObjectByType<FastingTopDownGame>();g.spawnSettings=config;EditorUtility.SetDirty(config);EditorUtility.SetDirty(g);EditorSceneManager.MarkSceneDirty(g.gameObject.scene);EditorSceneManager.SaveScene(g.gameObject.scene);AssetDatabase.SaveAssets();Selection.activeObject=config;return path;
    }
}
