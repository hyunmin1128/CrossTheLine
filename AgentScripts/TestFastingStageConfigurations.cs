using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using CrossTheLine.Minigames;
public static class TestFastingStageConfigurations
{
    public static string Run()
    {
        if(!EditorApplication.isPlaying)throw new Exception("Play mode required");
        var g=UnityEngine.Object.FindAnyObjectByType<FastingTopDownGame>();g.enabled=false;
        var profiles=g.stageConfigurations;var snapshots=profiles.Select(JsonUtility.ToJson).ToArray();
        string sourcePath="Assets/Settings/Minigames/FastingTopDownDifficulty.asset";
        string source=JsonUtility.ToJson(AssetDatabase.LoadAssetAtPath<FastingTopDownSettings>(sourcePath));
        int checks=0;Action<bool,string> ok=(condition,message)=>{if(!condition)throw new Exception(message);checks++;};
        try
        {
            ok(profiles.Length==4&&g.entities.All(e=>e),"Stage and item references");
            ok(!AssetDatabase.Contains(g.settings)&&!AssetDatabase.Contains(g.spawnSettings),"Settings must be runtime copies");
            for(int i=0;i<4;i++)
            {
                var c=profiles[i];c.prayerTarget=200+i*10;c.prayerPerSecond=2.5f;
                c.pageCharge=.125f;c.fragmentCharge=.05f;c.bibleCharge=.5f;c.pageChance=.123f;
                c.obstacleFrequency=2+i;c.waveInterval=.93f;
                foreach(var entry in c.obstacles)entry.spawnWeight=0;
                c.obstacles[0].spawnWeight=100;c.obstacles[0].damage=.5f;
                g.BeginStageForTesting(i);foreach(var e in g.entities)e.gameObject.SetActive(false);
                ok(g.CurrentStageConfiguration==c&&g.PrayerTarget==c.prayerTarget,"Stage profile selection "+i);
                ok(g.settings.prayerPerSecond==2.5f&&g.settings.waveIntervals[i]==.93f,"Prayer and spawn interval "+i);
                ok(Mathf.Abs(g.spawnSettings.Frequency(i)-c.sharedSpawnSettings.globalFrequency*(2+i))<.001f,"Global x stage frequency "+i);
                for(int n=0;n<100;n++)ok(g.TryChooseObstacle(out var kind)&&kind==c.obstacles[0].kind,"Per-kind spawn weights "+i);
                float before=g.Health;ok(g.TryDamage(c.obstacles[0].kind.ToString())&&Mathf.Abs(before-g.Health-.5f)<.001f,"Individual fractional damage "+i);
                float expected=0;
                foreach(var kind in new[]{FastingTopDownEntity.Kind.Page,FastingTopDownEntity.Kind.Fragment,FastingTopDownEntity.Kind.Book})
                {
                    var item=new GameObject("QA_Scripture").AddComponent<FastingTopDownEntity>();item.kind=kind;
                    try{g.Collect(item);}finally{UnityEngine.Object.Destroy(item.gameObject);}
                    expected+=g.PrayerTarget*(kind==FastingTopDownEntity.Kind.Page?.125f:kind==FastingTopDownEntity.Kind.Fragment?.05f:.5f);
                    ok(Mathf.Abs(g.Prayer-expected)<.001f,"Actual scripture charge "+kind+" stage "+i);
                }
                ok(Mathf.Abs(g.settings.StageDropChance(FastingTopDownEntity.Kind.Page,i)-.123f)<.001f,"Item spawn chance "+i);
                ok(g.backgrounds.All(b=>b.sprite==c.background),"Stage background "+i);
                for(int n=0;n<30;n++)g.Tick(.1f,new Vector2(n%2==0?1:-1,.2f),true);
                ok(g.playerBody.enabled&&g.playerBody.sprite,"Player rendering and update "+i);
                JsonUtility.FromJsonOverwrite(snapshots[i],c);
            }
            foreach(var prefab in new[]{g.caveArt.crackPrefab,g.trialArt.pillar})
            {
                var h=UnityEngine.Object.Instantiate(prefab);h.warning=0;h.velocity=Vector2.zero;
                bool hitOn=false,hitOff=false;
                try
                {
                    for(int n=0;n<120;n++)
                    {
                        bool hit=h.Advance(.05f,h.transform.position,h.transform.position,out var when);
                        hitOn|=hit;hitOff|=!hit;ok(h.body.enabled,"Obstacle flickered: "+h.kind);
                    }
                    ok(hitOn&&hitOff,"Existing attack windows preserved: "+h.kind);
                }
                finally{UnityEngine.Object.Destroy(h.gameObject);}
            }
            ok(JsonUtility.ToJson(AssetDatabase.LoadAssetAtPath<FastingTopDownSettings>(sourcePath))==source,"Original settings asset changed during Play");
            return "PASS "+checks+" checks: all 4 stage prefab settings, weighted spawning, actual fractional damage, Bible/page/fragment charge, backgrounds, movement, visible crack/pillar over 6s with unchanged attack windows; original settings asset untouched.";
        }
        finally
        {
            for(int i=0;i<profiles.Length;i++)JsonUtility.FromJsonOverwrite(snapshots[i],profiles[i]);
            g.BeginStageForTesting(0);g.enabled=true;
        }
    }
}
