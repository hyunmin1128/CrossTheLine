using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using CrossTheLine.Minigames;
public static class TestTrialLava
{
    public static string Run()
    {
        var g=UnityEngine.Object.FindAnyObjectByType<FastingTopDownGame>();g.enabled=false;
        var choices=g.obstaclePatterns.choices;int count=0;
        try
        {
            foreach(var pattern in new[]{FastingObstaclePatterns.Pattern.Alternating,FastingObstaclePatterns.Pattern.StaggeredPair,FastingObstaclePatterns.Pattern.Passage,FastingObstaclePatterns.Pattern.SandBands})
            {
                g.BeginStageForTesting(3);foreach(var e in g.entities)e.gameObject.SetActive(false);
                g.spawnSettings.stages[3].obstacles=new[]{new FastingSpawnSettings.Entry{kind=FastingTopDownHazard.Kind.Fire,weight=1}};
                g.obstaclePatterns.choices=new[]{new FastingObstaclePatterns.Choice(pattern,1)};g.obstaclePatterns.BeginAttack();
                g.obstaclePatterns.Spawn(g);
                if(g.Hazards.Count!=1)throw new Exception("Expected one lava pit for "+pattern+", got "+g.Hazards.Count);
                var h=g.Hazards[0];
                if(AssetDatabase.GetAssetPath(h.body.sprite)!="Assets/Sprite/Objects/Lava.aseprite")throw new Exception("Old flame artwork remains");
                if(h.occupiedLanes!=1||h.formationCount!=1||h.velocity!=Vector2.zero)throw new Exception("Lava should be one fixed ground opening");
                h.UpdateApproach(g.player.position);var scale=h.body.transform.localScale;
                h.UpdateApproach(g.player.position+Vector3.up*20);if(h.body.transform.localScale!=scale)throw new Exception("Lava grows on approach");
                count++;
            }
            var pit=g.Hazards[0];pit.transform.position=g.player.position;pit.TrackVisibility(g.gameCamera,2);
            float health=g.Health;g.Tick(.01f,Vector2.zero,false);
            if(g.Health>=health||g.PrayerBlockRemaining<=0)throw new Exception("Existing fire damage/burn not retained");
            g.BeginStageForTesting(3);foreach(var e in g.entities)e.gameObject.SetActive(false);
            g.SpawnPatternObstacle(FastingTopDownHazard.Kind.Fire,1,1,0);
            var show=g.Hazards.Single();show.transform.position=g.player.position+new Vector3(3,2,0);show.UpdateApproach(g.player.position);
            return "PASS: "+count+" pattern types spawn exactly one native Lava each; fixed size, one lane, no flight, real damage + prayer-block burn retained.";
        }
        finally{g.obstaclePatterns.choices=choices;}
    }
}
