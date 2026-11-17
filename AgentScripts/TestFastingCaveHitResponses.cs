using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using CrossTheLine.Minigames;
public static class TestFastingCaveHitResponses
{
    static void Quiet(FastingTopDownGame g)
    {
        foreach(var e in g.entities)e.gameObject.SetActive(false);var flags=BindingFlags.NonPublic|BindingFlags.Instance;
        typeof(FastingEncounterDirector).GetField("nextWave",flags).SetValue(g.encounterDirector,100f);typeof(FastingEncounterDirector).GetField("nextRandom",flags).SetValue(g.encounterDirector,100f);typeof(FastingEncounterDirector).GetField("remaining",flags).SetValue(g.encounterDirector,100f);
    }
    static void Start(FastingTopDownGame g){g.Begin();typeof(FastingTopDownGame).GetProperty("StageIndex").SetValue(g,1);typeof(FastingTopDownGame).GetMethod("ApplyStage",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(g,new object[]{false});Quiet(g);}
    static void Hit(FastingTopDownGame g,FastingTopDownHazard.Kind kind)
    {
        var h=UnityEngine.Object.Instantiate(g.caveArt.PrefabFor(kind,null),g.hazardRoot);h.warning=0;h.velocity=Vector2.zero;h.ConfigureLanes(1,g.settings.laneSpacing);h.transform.position=g.player.position;((List<FastingTopDownHazard>)g.Hazards).Add(h);g.Tick(.01f,Vector2.zero);
    }
    public static string Run()
    {
        var g=UnityEngine.Object.FindAnyObjectByType<FastingTopDownGame>();if(!Application.isPlaying)throw new Exception("Play required");g.enabled=false;var fx=g.hitFeedback;int n=0;Action<bool,string> ok=(b,s)=>{if(!b)throw new Exception(s);n++;};
        try
        {
            Start(g);var rest=g.playerBody.transform.localPosition;var restScale=g.playerBody.transform.localScale;Hit(g,FastingTopDownHazard.Kind.Bat);
            ok(g.Health==g.settings.maximumHealth-g.settings.damage,"Actual bat collision damage");ok(g.SlowRemaining>0&&g.VisionRemaining>0,"Bat slow and obscured vision");
            var area=g.playArea.RootArea;fx.Advance(.5f);ok(g.playerBody.transform.localPosition.y>rest.y+1,"Bat launches character artwork");ok(g.playArea.RootArea==area,"Airborne animation does not move movement limits");
            float x=g.player.position.x;g.Tick(.1f,Vector2.right);ok(Mathf.Abs(g.player.position.x-x-g.settings.manualMoveSpeed*.55f*.1f)<.001f,"Slow reduces lateral movement");ok(g.visionPanels.All(p=>p.color.a>0&&p.color.a<.8f),"Partial, not opaque vision masking");float cy=.5f+(g.player.position.y-g.gameCamera.transform.position.y)/(2*g.gameCamera.orthographicSize);ok(g.visionPanels[2].rectTransform.anchorMax.y<cy&&g.visionPanels[3].rectTransform.anchorMin.y>cy,"Clear vision window follows logical player position");
            fx.Advance(2);ok(g.playerBody.transform.localPosition==rest&&g.playerBody.transform.localScale==restScale,"Bat landing restores pose");
            Start(g);Hit(g,FastingTopDownHazard.Kind.Crack);ok(g.Health==g.settings.maximumHealth-2,"Actual crack collision removes two health");ok(fx.CrackOpeningVisible,"Visual fissure remains after hazard is consumed");ok(g.Invulnerable&&g.PrayerBlockRemaining>0,"Fall locks movement/prayer and grants protection");area=g.playArea.RootArea;
            fx.Advance(.2f);ok(g.playerBody.transform.localPosition.y<rest.y&&g.playerBody.transform.localScale.x<restScale.x,"Falls and shrinks into ground");fx.Advance(.2f);ok(fx.ArtworkOpacity==0,"Character disappears in fissure");ok(g.playArea.RootArea==area,"Hidden/shrunken animation keeps same movement area");
            g.Tick(.01f,Vector2.zero);ok(g.playerBody.color.a==0,"Invulnerability refresh does not reveal hidden character");float health=g.Health;ok(!g.TryDamage("Rock")&&g.Health==health,"No repeat damage while absent");fx.Advance(.35f);ok(fx.ArtworkOpacity>0&&fx.ArtworkOpacity<1,"Character returns gradually");fx.Advance(.2f);float first=fx.ArtworkOpacity;fx.Advance(.1f);ok(fx.ArtworkOpacity!=first,"Return flickers");
            fx.Advance(2);ok(fx.ArtworkOpacity==1&&g.playerBody.transform.localPosition==rest&&g.playerBody.transform.localScale==restScale,"Full restoration after fall");
            Start(g);Hit(g,FastingTopDownHazard.Kind.Crack);for(int i=0;i<15;i++){g.Tick(.1f,Vector2.zero);fx.Advance(.1f);}ok(g.Invulnerable&&!g.TryDamage("Rock"),"Protection lasts throughout return blinking");for(int i=0;i<6;i++){g.Tick(.1f,Vector2.zero);fx.Advance(.1f);}ok(!g.Invulnerable&&g.TryDamage("Rock"),"Protection ends after recovery");
            Start(g);g.Collect(g.entities.First(e=>e.kind==FastingTopDownEntity.Kind.Shield));health=g.Health;Hit(g,FastingTopDownHazard.Kind.Crack);ok(g.Health==health&&!fx.Active&&g.ShieldCount==0,"Shield blocks fall and two-point damage");
            Start(g);Hit(g,FastingTopDownHazard.Kind.Crack);fx.Advance(.4f);g.Begin();ok(g.Health==g.settings.maximumHealth&&fx.ArtworkOpacity==1&&g.playerBody.transform.localScale==restScale,"Retry clears hidden state");
            return n+" checks passed: actual bat/crack collisions, slow/vision/airborne motion, two damage, fall-hide-return-blink, invulnerability, stable boundaries, shield and retry.";
        }
        finally{Time.timeScale=1;g.Begin();g.enabled=true;}
    }
}
