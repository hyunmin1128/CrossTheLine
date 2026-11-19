using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using CrossTheLine.Minigames;
public static class TestFastingSpawnSettings
{
    public static string Run(){
        if(!Application.isPlaying)throw new Exception("Play required");var g=UnityEngine.Object.FindAnyObjectByType<FastingTopDownGame>();var original=g.spawnSettings;var test=UnityEngine.Object.Instantiate(original);g.spawnSettings=test;g.enabled=false;int n=0;var flags=BindingFlags.NonPublic|BindingFlags.Instance;Action<bool,string> ok=(b,s)=>{if(!b)throw new Exception(s);n++;};var rng=UnityEngine.Random.state;
        try{
            UnityEngine.Random.InitState(1911);
            for(int stage=0;stage<4;stage++){
                g.BeginStageForTesting(stage);var entries=test.stages[stage].obstacles;var counts=new int[4];for(int i=0;i<20000;i++){ok(test.TryPick(stage,out var kind),"Valid selection");counts[Array.FindIndex(entries,e=>e.kind==kind)]++;}float sum=entries.Sum(e=>e.weight);for(int j=0;j<4;j++)ok(Mathf.Abs(counts[j]/20000f-entries[j].weight/sum)<.025f,"Weighted distribution");
                for(int j=0;j<4;j++)entries[j].weight=0;ok(!test.TryPick(stage,out _),"All zero stops");entries[0].weight=100;
                foreach(var e in g.entities)e.gameObject.SetActive(false);
                foreach(var choice in new[]{FastingObstaclePatterns.Pattern.Alternating,FastingObstaclePatterns.Pattern.StaggeredPair,FastingObstaclePatterns.Pattern.Passage,FastingObstaclePatterns.Pattern.SandBands}){
                    g.BeginStageForTesting(stage);foreach(var e in g.entities)e.gameObject.SetActive(false);g.encounterDirector.ReserveSweepEscape(4);typeof(FastingObstaclePatterns).GetProperty("CurrentPattern").SetValue(g.obstaclePatterns,choice);g.SpawnWave();ok(g.Hazards.All(h=>h.kind==entries[0].kind),"Patterns respect disabled weights");
                }
            }
            g.BeginStageForTesting(3);var director=g.encounterDirector;test.globalFrequency=1;test.stages[3].frequency=1;UnityEngine.Random.InitState(42);float normal=(float)typeof(FastingEncounterDirector).GetMethod("RandomInterval",flags).Invoke(director,null);test.globalFrequency=2;UnityEngine.Random.InitState(42);float faster=(float)typeof(FastingEncounterDirector).GetMethod("RandomInterval",flags).Invoke(director,null);ok(Mathf.Abs(faster*2-normal)<.00001f,"Global2x halves interval");test.stages[3].frequency=.5f;ok(director.Frequency==1,"Stage frequency multiplies global");
            test.globalFrequency=0;director.ResetEncounter();director.Tick(.5f);ok(director.SpawnAttempts==0,"Global0 stops spawning");ok(g.settings.attackSeconds.SequenceEqual(new[]{5f,5.5f,6f,6.5f}),"Attack durations preserved");
            return n+" checks passed: 80000 weighted draws, zero/disabled entries, all patterns, global/stage multipliers, pause and unchanged attack times.";
        }finally{UnityEngine.Random.state=rng;g.spawnSettings=original;UnityEngine.Object.Destroy(test);Time.timeScale=1;g.BeginStageForTesting(0);g.enabled=true;}
    }
}
