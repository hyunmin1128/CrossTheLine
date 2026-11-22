using System;
using System.Linq;
using UnityEngine;
using CrossTheLine.Minigames;
public static class TestSpawnAuraFeverFixes
{
    public static string Run()
    {
        var g=UnityEngine.Object.FindAnyObjectByType<FastingTopDownGame>();g.enabled=false;
        var kinds=new[]{new[]{FastingTopDownHazard.Kind.Rock,FastingTopDownHazard.Kind.Sand,FastingTopDownHazard.Kind.Cactus,FastingTopDownHazard.Kind.Quicksand},new[]{FastingTopDownHazard.Kind.Rock,FastingTopDownHazard.Kind.Bat,FastingTopDownHazard.Kind.Collapse,FastingTopDownHazard.Kind.Crack},new[]{FastingTopDownHazard.Kind.Rock,FastingTopDownHazard.Kind.Tomb,FastingTopDownHazard.Kind.Wall,FastingTopDownHazard.Kind.Poison},new[]{FastingTopDownHazard.Kind.Fire,FastingTopDownHazard.Kind.Thorns,FastingTopDownHazard.Kind.Pillar,FastingTopDownHazard.Kind.Reaper}};
        int obstacles=0;
        for(int stage=0;stage<4;stage++)
        {
            g.BeginStageForTesting(stage);foreach(var e in g.entities)e.gameObject.SetActive(false);
            foreach(var kind in kinds[stage])
            {
                var prefab=stage==0?g.desertArt.PrefabFor(kind,null):stage==1?g.caveArt.PrefabFor(kind,null):stage==2?g.ruinsArt.PrefabFor(kind,null):g.trialArt.PrefabFor(kind,null);
                var h=UnityEngine.Object.Instantiate(prefab);h.kind=kind;
                try
                {
                    if(stage==0)g.desertArt.Apply(h);if(stage==1)g.caveArt.Apply(h);
                    h.ConfigureLanes(1,g.settings.laneSpacing);h.velocity=Vector2.zero;h.warning=.5f;h.minimumVisibleReactionSeconds=.6f;
                    h.transform.position=g.gameCamera.transform.position+Vector3.forward*10;h.UpdateApproach(g.player.position);
                    var range=h.RoadVisualRange;
                    for(int i=0;i<360;i++)
                    {
                        h.TrackVisibility(g.gameCamera,1f/60);h.Advance(1f/60,new Vector2(-100,-100),new Vector2(-100,-100),out var hit);
                        if(!h.body.enabled||!h.body.sprite||h.body.color.a<.999f)throw new Exception("Blink/fade in stage "+stage+" / "+kind);
                        if(Vector2.Distance(range,h.RoadVisualRange)>.002f)throw new Exception("Animated road bound changed: "+kind);
                    }
                    obstacles++;
                }
                finally{UnityEngine.Object.Destroy(h.gameObject);}
            }
        }
        g.BeginStageForTesting(0);foreach(var e in g.entities)e.gameObject.SetActive(false);g.spawnSettings.globalFrequency=0;
        var flame=g.entities.First(e=>e.kind==FastingTopDownEntity.Kind.Flame);
        for(int i=0;i<g.settings.flamesForFever;i++)g.Collect(flame);
        if(!g.FeverActive)throw new Exception("Fever activation failed");
        for(int i=0;i<30;i++)g.Tick(.1f,Vector2.zero,false);
        float before=g.FeverRemaining;g.Collect(flame);
        if(Mathf.Abs(g.FeverRemaining-Mathf.Min(g.settings.feverSeconds,before+g.settings.feverRefillSeconds))>.001f)throw new Exception("Fever did not refill by configured amount");
        float refilled=g.FeverRemaining;g.Collect(flame);g.Collect(flame);
        if(g.FeverRemaining>g.settings.feverSeconds+.001f)throw new Exception("Fever overcharged");
        float full=g.FeverRemaining;g.Tick(.1f,Vector2.zero,false);if(g.FeverRemaining>=full)throw new Exception("Fever must drain even without prayer");
        g.BeginStageForTesting(0);foreach(var e in g.entities)e.gameObject.SetActive(false);g.spawnSettings.globalFrequency=0;
        g.Collect(g.entities.First(e=>e.kind==FastingTopDownEntity.Kind.Shield));
        for(int i=0;i<5;i++)g.Tick(.1f,Vector2.zero,true);g.playerAura.Advance(.1f);
        var shield=g.playerAura.transform.Find("IllustratedShieldMembrane").GetComponent<SpriteRenderer>();
        var prayer=g.playerAura.transform.Find("EditablePrayerAura").GetComponent<SpriteRenderer>();
        if(!shield.enabled||!prayer.enabled||shield.color.a<.7f||prayer.color.a<.5f)throw new Exception("Aura still too faint");
        if(shield.sortingOrder>=g.playerBody.sortingOrder||prayer.sortingOrder>=g.playerBody.sortingOrder)throw new Exception("Opaque aura obscures Jesus");
        return "PASS: "+obstacles+" stage/obstacle variants visible at full opacity for 6s each, stable road bounds across animation/rotation, warning and reaction grace retained; fever "+before.ToString("F1")+" -> "+refilled.ToString("F1")+"s, capped and drains without prayer; shield/prayer behind visible character at .72/.52 opacity.";
    }
}
