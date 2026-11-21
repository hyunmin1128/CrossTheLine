using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using CrossTheLine.Minigames;
public static class BuildFastingStagePrefabs
{
    public static string Run()
    {
        if(EditorApplication.isPlaying)throw new Exception("Edit mode required");
        var g=UnityEngine.Object.FindAnyObjectByType<FastingTopDownGame>();
        if(g.stageConfigurations!=null&&g.stageConfigurations.Any(x=>x))throw new Exception("Stage prefabs are already installed; refusing duplicate migration");
        string folder="Assets/Prefabs/Minigames/FastingTopDown/Stages";
        string settingsFolder="Assets/Settings/Minigames/FastingStages";
        Directory.CreateDirectory(folder);Directory.CreateDirectory(settingsFolder);AssetDatabase.Refresh();
        const string backup="Docs/Backups/FastingStagePrefabs-20261121";Directory.CreateDirectory(backup);
        File.Copy(g.gameObject.scene.path,backup+"/Scene-OnDisk.unity",true);
        const string snapshot="Assets/FastingStageMigrationSnapshot.unity";
        if(!EditorSceneManager.SaveScene(g.gameObject.scene,snapshot,true))throw new Exception("Could not preserve unsaved scene state");
        File.Copy(snapshot,backup+"/Scene-IncludingUnsavedChanges.unity",true);AssetDatabase.DeleteAsset(snapshot);
        string[] names={"Stage01_Desert","Stage02_Cave","Stage03_Ruins","Stage04_FinalTrial"};
        g.stageConfigurations=new FastingStageConfiguration[4];
        for(int i=0;i<4;i++)
        {
            var root=new GameObject(names[i]);root.transform.SetParent(g.entities.First(e=>e.stage==i).transform.parent,false);
            var c=root.AddComponent<FastingStageConfiguration>();c.stageIndex=i;c.displayName=g.settings.places[i];
            c.sharedSpawnSettings=g.spawnSettings;var stage=g.spawnSettings.stages[i];
            c.obstacleFrequency=stage.frequency;c.roadEdges=stage.roadEdges;
            c.obstacles=stage.obstacles.Select(e=>new FastingStageConfiguration.Obstacle{kind=e.kind,spawnWeight=e.weight,damage=Damage(g.settings,e.kind)}).ToArray();
            c.prayerTarget=g.settings.GetPrayerTarget(i);c.prayerPerSecond=g.settings.prayerPerSecond;
            c.pageCharge=g.settings.ScriptureFraction(i,true);c.fragmentCharge=g.settings.ScriptureFraction(i,false);c.bibleCharge=1;
            c.bibleChance=g.settings.StageDropChance(FastingTopDownEntity.Kind.Book,i);c.pageChance=g.settings.StageDropChance(FastingTopDownEntity.Kind.Page,i);c.fragmentChance=g.settings.StageDropChance(FastingTopDownEntity.Kind.Fragment,i);
            c.flameChance=g.settings.flameChance;c.shieldChance=g.settings.shieldChance;c.waterChance=g.settings.StageDropChance(FastingTopDownEntity.Kind.Water,i);
            c.waterInterval=g.settings.StageValue(g.settings.wellIntervalSeconds,i,20);
            c.waveInterval=g.settings.StageValue(g.settings.waveIntervals,i,.8f);c.attackSeconds=g.settings.StageValue(g.settings.attackSeconds,i,5);c.restSeconds=g.settings.StageValue(g.settings.restSeconds,i,3);
            c.scrollSpeed=g.settings.StageSpeed(i);c.playerMoveSpeed=g.settings.manualMoveSpeed;c.prayerMoveMultiplier=g.settings.prayerMoveMultiplier;c.hazardSpeedMultiplier=g.settings.hazardSpeedMultiplier;
            c.background=i==1?g.caveArt.background:i==2?g.ruinsArt.background:i==3?g.trialArt.background:g.backgrounds[0].sprite;
            string settingsPath=settingsFolder+"/"+names[i]+"_Advanced.asset";
            if(!AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(g.settings),settingsPath))throw new Exception("Could not create stage settings copy");
            c.advancedSettings=AssetDatabase.LoadAssetAtPath<FastingTopDownSettings>(settingsPath);
            c.desertArt=g.desertArt;c.caveArt=g.caveArt;c.ruinsArt=g.ruinsArt;c.trialArt=g.trialArt;
            foreach(var entity in g.entities.Where(e=>e.stage==i))Undo.SetTransformParent(entity.transform,root.transform,"Group fasting stage");
            PrefabUtility.SaveAsPrefabAssetAndConnect(root,folder+"/"+names[i]+".prefab",InteractionMode.AutomatedAction);
            g.stageConfigurations[i]=root.GetComponent<FastingStageConfiguration>();
        }
        EditorUtility.SetDirty(g);EditorSceneManager.MarkSceneDirty(g.gameObject.scene);AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(g.gameObject.scene);
        Selection.activeGameObject=g.stageConfigurations[0].gameObject;
        return "Installed 4 connected stage prefab instances, 4 independent advanced settings, "+g.entities.Length+" original item objects preserved and grouped. Backup includes unsaved scene edits.";
    }
    static float Damage(FastingTopDownSettings s,FastingTopDownHazard.Kind k)=>k==FastingTopDownHazard.Kind.Cactus?s.cactusDamage:k==FastingTopDownHazard.Kind.Crack?s.crackDamage:k==FastingTopDownHazard.Kind.Poison?s.poisonDamage:k==FastingTopDownHazard.Kind.Pillar?s.pillarDamage:s.damage;
}
