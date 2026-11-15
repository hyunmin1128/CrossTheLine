using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using CrossTheLine.Minigames;

public static class VerifyOrbitShields
{
    const string Out="Docs/QA/OrbitShields-20261113";
    static List<string> rows;
    static void Check(bool valid,string message){if(!valid)throw new Exception(message);rows.Add("PASS "+message);}
    static async Task Frames(int n){int end=Time.frameCount+n;while(Time.frameCount<end)await Task.Yield();}
    static void Set(FastingTopDownGame game,string field,object value)=>typeof(FastingTopDownGame).GetField(field,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(game,value);
    static FastingTopDownEntity Pickup(FastingTopDownGame game)
    {
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Minigames/FastingTopDown/ShieldOfFaithPickup.prefab");var root=UnityEngine.Object.Instantiate(prefab);var e=root.GetComponent<FastingTopDownEntity>();game.Collect(e);UnityEngine.Object.Destroy(root);return e;
    }
    public static async Task<string> Run()
    {
        if(!EditorApplication.isPlaying)throw new Exception("Play mode required");Directory.CreateDirectory(Out);rows=new List<string>();var errors=new List<string>();Application.LogCallback capture=(m,s,t)=>{if(t==LogType.Error||t==LogType.Exception||t==LogType.Assert)errors.Add(m);};Application.logMessageReceived+=capture;
        var game=UnityEngine.Object.FindAnyObjectByType<FastingTopDownGame>();
        try
        {
            await Frames(3);game.Begin();game.enabled=false;if(game.encounterDirector)game.encounterDirector.enabled=false;Set(game,"wave",float.PositiveInfinity);var entities=game.entities;game.entities=Array.Empty<FastingTopDownEntity>();
            Check(game.ShieldCount==0&&!game.Shielded,"Reset starts with zero shields");
            for(int i=1;i<=3;i++){Pickup(game);var visual=game.GetComponent<FastingShieldOrbit>();Check(game.ShieldCount==i&&visual.VisibleCount==i,"Pickup gives exactly "+i+" orbiting shields");}
            Pickup(game);Check(game.ShieldCount==3&&game.GetComponent<FastingShieldOrbit>().VisibleCount==3,"Fourth pickup cannot exceed three shields");
            var orbit=game.GetComponent<FastingShieldOrbit>();var source=game.player.Find("OrbitingShields/OrbitShield1");Check(source&&source.GetComponent<SpriteRenderer>().sprite,"Uses real shield sprite");Vector3 before=source.position;await Frames(12);Check(Vector3.Distance(before,source.position)>.001f,"Orbit animation really advances");
            float health=game.Health;Check(!game.TryDamage("shield test"),"Shielded hit does not damage health");Check(game.ShieldCount==2&&game.Health==health,"One hit consumes exactly one of three shields");Check(orbit.VisibleCount==2&&orbit.BreakGraphic&&orbit.BreakGraphic.Active,"Two orbits remain and screen break effect starts");
            var properties=new MaterialPropertyBlock();game.playerBody.GetPropertyBlock(properties);Check(properties.GetFloat("_Flash")==1&&game.Invulnerable,"White flash and invulnerability start immediately");
            for(int i=0;i<25;i++)game.TryDamage("same-frame repeat");Check(game.ShieldCount==2&&game.Health==health,"25 simultaneous hits consume no further shield or health");
            float protection=game.InvulnerabilityRemaining;game.UseWater();Check(game.InvulnerabilityRemaining>=protection-.0001f,"Water never shortens existing shield immunity");
            Set(game,"grace",.55f);game.Tick(.1f,Vector2.zero);for(int i=0;i<10;i++)game.TryDamage("during flash");Check(game.ShieldCount==2&&game.Health==health,"Repeated hits during ticking white flash remain blocked");
            Set(game,"grace",0f);Set(game,"waterGrace",0f);game.TryDamage("second shield");Check(game.ShieldCount==1&&game.Health==health,"Next hit after immunity consumes second shield only");
            Set(game,"grace",0f);game.TryDamage("last shield");Check(game.ShieldCount==0&&!game.Shielded&&game.Health==health,"Last shield breaks without health loss");
            for(int i=0;i<20;i++)game.TryDamage("zero shields but still immune");Check(game.Health==health&&game.ShieldCount==0,"Last-shield immunity prevents health loss on repeat hits");
            Set(game,"grace",0f);Check(game.TryDamage("unprotected hit"),"Health damage resumes after immunity expires");Check(game.Health==health-game.settings.damage,"Unprotected hit deducts exactly one normal damage");health=game.Health;game.TryDamage("repeat health hit");Check(game.Health==health,"Health-hit immunity also blocks repeats");
            Set(game,"grace",.05f);game.Tick(.1f,Vector2.zero);game.playerBody.GetPropertyBlock(properties);Check(!game.Invulnerable&&properties.GetFloat("_Flash")==0,"Natural timer expiry clears immunity and white flash together");
            // Integration check: a blocked hazard is consumed even while shields remain.
            game.Begin();game.enabled=false;game.entities=Array.Empty<FastingTopDownEntity>();Pickup(game);Pickup(game);Pickup(game);Set(game,"wave",float.PositiveInfinity);
            var hazard=UnityEngine.Object.Instantiate(game.hazardPrefab,game.hazardRoot);hazard.kind=FastingTopDownHazard.Kind.Rock;hazard.warning=0;hazard.velocity=Vector2.zero;hazard.transform.position=game.player.position;hazard.ConfigureLanes(1,game.settings.laneSpacing);var list=(List<FastingTopDownHazard>)typeof(FastingTopDownGame).GetField("hazards",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(game);list.Add(hazard);health=game.Health;
            game.Tick(.001f,Vector2.zero);Check(game.ShieldCount==2&&game.Health==health&&!list.Contains(hazard),"Real hazard collision consumes one shield and removes hazard while two remain");Check(game.SlowRemaining==0&&game.PrayerBlockRemaining==0,"Blocked collision applies no negative status effect");
            Set(game,"grace",0f);for(int i=0;i<3;i++){var h=UnityEngine.Object.Instantiate(game.hazardPrefab,game.hazardRoot);h.kind=FastingTopDownHazard.Kind.Rock;h.warning=0;h.velocity=Vector2.zero;h.transform.position=game.player.position;h.ConfigureLanes(1,game.settings.laneSpacing);list.Add(h);}
            game.Tick(.001f,Vector2.zero);Check(game.ShieldCount==1&&game.Health==health&&list.Count==2,"Three overlapping hazards consume one shield total");game.Tick(.001f,Vector2.zero);Check(game.ShieldCount==1&&game.Health==health,"Remaining overlapping hazards cannot consume shields during immunity");
            orbit.BreakGraphic.Advance(.45f);Canvas.ForceUpdateCanvases();var mesh=orbit.BreakGraphic.canvasRenderer.GetMesh();Check(mesh.vertexCount==64,"Screen break renders 16 textured shield shards");
            orbit.BreakGraphic.Advance(1);Check(!orbit.BreakGraphic.Active,"Shatter effect finishes and clears");
            game.Begin();Check(game.ShieldCount==0&&orbit.VisibleCount==0&&!orbit.BreakGraphic.Active&&!game.Invulnerable,"Restart clears shields, fragments, and immunity");game.playerBody.GetPropertyBlock(properties);Check(properties.GetFloat("_Flash")==0,"Restart clears white flash");
            game.entities=entities;game.enabled=true;Check(errors.Count==0,"No runtime errors during regression");File.WriteAllLines(Out+"/validation.txt",rows);return "PASS "+rows.Count+" checks";
        }
        catch(Exception e){File.WriteAllLines(Out+"/validation.txt",rows.Concat(new[]{"FAIL "+e}));throw;}
        finally{Application.logMessageReceived-=capture;if(game)game.enabled=true;}
    }
    public static string Preview()
    {
        var game=UnityEngine.Object.FindAnyObjectByType<FastingTopDownGame>();game.Begin();game.enabled=false;for(int i=0;i<3;i++)Pickup(game);Time.timeScale=0;return "Three shields preview (Play mode only)";
    }
    public static string PreviewBreak(){var game=UnityEngine.Object.FindAnyObjectByType<FastingTopDownGame>();game.TryDamage("visual check");game.GetComponent<FastingShieldOrbit>().BreakGraphic.Advance(.22f);return "Shield floats toward camera";}
    public static string PreviewShards(){var game=UnityEngine.Object.FindAnyObjectByType<FastingTopDownGame>();game.GetComponent<FastingShieldOrbit>().BreakGraphic.Advance(.3f);return "Shield shatters";}
}
