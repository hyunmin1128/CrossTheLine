using System;
using System.Linq;
using UnityEngine;
using CrossTheLine.Minigames;
public static class TestShieldPresentationNov15
{
    public static string Run()
    {
        var g=UnityEngine.Object.FindAnyObjectByType<FastingTopDownGame>();if(!Application.isPlaying||!g)throw new Exception("Play minigame");g.enabled=false;int checks=0;Action<bool,string> ok=(v,m)=>{if(!v)throw new Exception(m);checks++;};
        try
        {
            g.Begin();var pickup=g.entities.First(e=>e.kind==FastingTopDownEntity.Kind.Shield);
            g.Collect(pickup);ok(g.ShieldCount==1&&g.playerAura.Acquiring,"Pickup acquisition flash");g.playerAura.Advance(.2f);ok(g.playerAura.GetComponent<MeshFilter>().sharedMesh.vertexCount>500,"Pixel shield contour drawn");
            var orbit=g.GetComponent<FastingShieldOrbit>();ok(orbit.VisibleCount==1,"Original artwork orbits");g.Collect(pickup);g.Collect(pickup);g.Collect(pickup);ok(g.ShieldCount==3,"Three-shield cap preserved");orbit.Refresh();ok(orbit.VisibleCount==3,"Three shields visible");
            float hp=g.Health;g.TryDamage("test");ok(g.ShieldCount==2&&g.Health==hp,"Hit uses only one shield");ok(g.playerAura.Breaking&&!g.playerAura.Acquiring,"Acquire replaced by shatter");var fx=orbit.BreakGraphic;ok(fx&&fx.Active,"Screen shatter starts");
            ok(Mathf.Abs(g.InvulnerabilityRemaining-Mathf.Max(g.settings.hitGrace,.9f))<.001f,"Invulnerability duration unchanged");g.TryDamage("repeat");ok(g.ShieldCount==2,"Repeat hit blocked by grace");
            fx.Advance(1);ok(fx.Active&&fx.Progress<1,"Shatter visible beyond old .9 seconds");fx.Advance(.7f);ok(fx.Active,"Shatter still visible at 1.7 seconds");fx.Advance(.2f);ok(!fx.Active,"Shatter ends after 1.8 seconds");
            g.Begin();ok(!g.playerAura.Acquiring&&!g.playerAura.Breaking&&g.ShieldCount==0,"Restart clears state");ok(!fx.Active,"Restart clears screen overlay");return checks+" checks passed: shield pickup/contour/orbit/stacking/block/shatter/restart.";
        }
        finally{g.Begin();g.enabled=true;}
    }
}
