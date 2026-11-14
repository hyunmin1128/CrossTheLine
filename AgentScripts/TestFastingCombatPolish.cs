using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using CrossTheLine.Minigames;
public static class TestFastingCombatPolish
{
    public static string Run()
    {
        var g=UnityEngine.Object.FindAnyObjectByType<FastingTopDownGame>();if(!Application.isPlaying||!g||!g.playerAura)throw new Exception("Play updated minigame");
        var director=g.encounterDirector;g.encounterDirector=null;g.enabled=false;int checks=0;
        Action<bool,string> ok=(v,m)=>{if(!v)throw new Exception(m);checks++;};
        Action clean=()=>{g.Begin();typeof(FastingTopDownGame).GetField("wave",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(g,float.MaxValue);foreach(var e in g.entities)e.gameObject.SetActive(false);};
        Action<FastingTopDownHazard.Kind> collide=kind=>{var h=UnityEngine.Object.Instantiate(g.desertArt.PrefabFor(kind,g.hazardPrefab),g.hazardRoot);h.kind=kind;g.desertArt.Apply(h);h.ConfigureLanes(1,3);h.warning=0;h.velocity=Vector2.zero;h.transform.position=g.player.position;((List<FastingTopDownHazard>)g.Hazards).Add(h);g.Tick(.01f,Vector2.zero);};
        try
        {
            clean();collide(FastingTopDownHazard.Kind.Cactus);ok(g.Health==3,"Cactus collision deals 2 HP");ok(!g.TryDamage("Rock")&&g.Health==3,"Damage grace still applies");
            clean();g.Collect(g.entities.First(e=>e.kind==FastingTopDownEntity.Kind.Shield));g.playerAura.Advance(0);ok(g.Shielded&&g.playerAura.GetComponent<MeshFilter>().sharedMesh.vertexCount>0,"Persistent shield drawn");collide(FastingTopDownHazard.Kind.Cactus);ok(g.Health==5&&!g.Shielded,"Shield blocks entire 2 damage");ok(g.playerAura.Breaking,"Shield break triggered");g.playerAura.Advance(.1f);ok(g.playerAura.GetComponent<MeshFilter>().sharedMesh.vertexCount>0,"Break fragments drawn");g.playerAura.Advance(1);ok(!g.playerAura.Breaking&&g.playerAura.GetComponent<MeshFilter>().sharedMesh.vertexCount==0,"Break fragments disappear");
            clean();g.UseWater();ok(g.WaterBoostRemaining==3,"Drinking grants boost");g.playerAura.Advance(0);ok(g.playerAura.BoostVisible&&g.playerAura.GetComponent<MeshFilter>().sharedMesh.vertexCount>0,"Boost trail drawn");float x=g.player.position.x;g.Tick(.1f,Vector2.right);ok(Mathf.Abs(g.player.position.x-x-.676f)<.001f,"Boost speed 5.2 x 1.3");
            var well=g.entities.First(e=>e.kind==FastingTopDownEntity.Kind.Water&&e.stage==0);well.gameObject.SetActive(true);well.SetUsed(false);well.transform.position=g.player.position;g.Tick(.01f,Vector2.zero);ok(g.HasWater&&well.consumed&&g.WaterBoostRemaining==3,"Oasis refill also boosts");
            clean();collide(FastingTopDownHazard.Kind.Quicksand);ok(Mathf.Abs(g.PrayerBlockRemaining-2)<.001f,"Quicksand roots 2 seconds");float fixedX=g.player.position.x;for(int i=0;i<19;i++)g.Tick(.1f,Vector2.right);ok(g.player.position.x==fixedX,"Cannot move during root");for(int i=0;i<4;i++)g.Tick(.1f,Vector2.right);ok(g.player.position.x>fixedX,"Root releases");
            clean();float baseY=g.playerBody.transform.localPosition.y;collide(FastingTopDownHazard.Kind.Sand);g.hitFeedback.Advance(.675f);ok(g.playerBody.transform.localPosition.y-baseY>2.3f,"Exaggerated airborne peak");g.hitFeedback.Advance(.8f);ok(!g.hitFeedback.Active&&Mathf.Abs(g.playerBody.transform.localPosition.y-baseY)<.001f,"Airborne returns cleanly");
            clean();g.Tick(.1f,Vector2.right);ok(Mathf.Abs(g.player.position.x-.52f)<.001f,"Higher base movement speed");ok(g.settings.waveIntervals[0]==1.35f&&g.settings.waveIntervals[3]==1,"Denser attack settings");
            return checks+" checks passed: damage, shield, boost, oasis, root, airborne and movement.";
        }
        finally{g.encounterDirector=director;g.Begin();g.enabled=true;}
    }
}
