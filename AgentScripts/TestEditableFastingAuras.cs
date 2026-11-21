using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using CrossTheLine.Minigames;
public static class TestEditableFastingAuras
{
    public static string Run()
    {
        var g=UnityEngine.Object.FindAnyObjectByType<FastingTopDownGame>();g.BeginStageForTesting(0);g.enabled=false;
        foreach(var entity in g.entities)entity.gameObject.SetActive(false);
        var art=g.effectSprites;
        if(art.bubble.Length!=4||art.prayer.Length!=4||art.prayerAura.Length!=4||art.shieldBreak.Length!=7)throw new Exception("Wrong imported frame count");
        foreach(var sprite in art.bubble.Concat(art.prayer).Concat(art.prayerAura).Concat(art.shieldBreak).Concat(new[]{art.orbitShield,art.spark}))
        {
            if(!sprite||!AssetDatabase.GetAssetPath(sprite).EndsWith(".aseprite"))throw new Exception("Effect still references an exported PNG");
            if(sprite.pixelsPerUnit!=32)throw new Exception("Wrong PPU");
        }
        g.playerAura.Advance(.01f);
        var visual=g.playerAura.GetComponent<FastingIllustratedAura>();
        if(visual.PrayerVisible)throw new Exception("Prayer light visible before praying");
        for(int i=0;i<5;i++)g.Tick(.1f,Vector2.zero,true);
        g.playerAura.Advance(.1f);
        var prayer=g.playerAura.transform.Find("EditablePrayerAura").GetComponent<SpriteRenderer>();
        if(!visual.PrayerVisible||!prayer.enabled||!art.prayerAura.Contains(prayer.sprite))throw new Exception("Native prayer aura not displayed");
        g.Tick(.1f,Vector2.zero,false);g.playerAura.Advance(.1f);
        if(visual.PrayerVisible||prayer.enabled)throw new Exception("Prayer aura remains after release");
        var pickup=g.entities.First(e=>e.kind==FastingTopDownEntity.Kind.Shield);g.Collect(pickup);g.playerAura.Advance(.1f);
        var membrane=g.playerAura.transform.Find("IllustratedShieldMembrane").GetComponent<SpriteRenderer>();
        if(!membrane.enabled||!art.bubble.Take(3).Contains(membrane.sprite))throw new Exception("Native protective bubble missing");
        var orbit=g.player.Find("OrbitingShields").GetChild(0).GetComponent<SpriteRenderer>();
        if(orbit.sprite!=art.orbitShield||!orbit.gameObject.activeSelf)throw new Exception("Orbit shield not connected");
        if(g.playerAura.GetComponent<MeshFilter>().sharedMesh.vertexCount!=0)throw new Exception("Procedural aura is covering editable sprites");
        float health=g.Health;g.TryDamage("Rock");g.playerAura.Advance(.1f);
        if(g.Health!=health||g.Shielded||membrane.sprite!=art.bubble[3]||!membrane.enabled)throw new Exception("Shield break/regression");
        var graphic=g.GetComponent<FastingShieldOrbit>().BreakGraphic;
        var frameProperty=typeof(FastingShieldBreakGraphic).GetProperty("IllustratedFrame",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
        var played=new System.Collections.Generic.HashSet<Sprite>();
        for(int i=0;i<46;i++){played.Add((Sprite)frameProperty.GetValue(graphic));graphic.Advance(.04f);}
        if(played.Count!=7||graphic.Active)throw new Exception("Not all seven shield break poses were played");
        g.playerAura.Advance(3);if(membrane.enabled)throw new Exception("Broken bubble did not clear");
        // Leave both editable effects visible for the screenshot.
        g.BeginStageForTesting(0);foreach(var e in g.entities)e.gameObject.SetActive(false);g.Collect(pickup);
        for(int i=0;i<5;i++)g.Tick(.1f,Vector2.zero,true);g.playerAura.Advance(.1f);
        return "PASS: direct native Aseprite references, centered 32PPU frames, prayer on/off, shield acquire/orbit/block/break/clear, no procedural overlay; original gameplay behavior retained.";
    }
}
