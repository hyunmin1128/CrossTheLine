using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using CrossTheLine.Minigames;
public static class TestFastingHitVignette
{
    public static string Run()
    {
        var g=UnityEngine.Object.FindAnyObjectByType<FastingTopDownGame>();if(!Application.isPlaying||!g||!g.hitVignette)throw new Exception("Play updated minigame");g.enabled=false;int n=0;float damage=g.settings.damage;var fx=g.hitVignette;
        Action<bool,string> ok=(v,m)=>{if(!v)throw new Exception(m);n++;};
        try
        {
            g.Begin();ok(!fx.Active,"Normal state clear");Canvas.ForceUpdateCanvases();ok(fx.canvasRenderer.GetMesh().vertexCount==0,"No normal overlay geometry");
            foreach(var e in g.entities)e.gameObject.SetActive(false);
            var h=UnityEngine.Object.Instantiate(g.desertArt.rockPrefab,g.hazardRoot);h.kind=FastingTopDownHazard.Kind.Rock;h.warning=0;h.velocity=Vector2.zero;h.ConfigureLanes(1,3);h.transform.position=g.player.position;((List<FastingTopDownHazard>)g.Hazards).Add(h);
            float hp=g.Health;g.Tick(.01f,Vector2.zero);ok(g.Health<hp&&fx.CurrentKind==FastingHitVignette.HitKind.Damage,"Actual collision triggers red only with health loss");
            fx.Advance(.1f);Canvas.ForceUpdateCanvases();var mesh=fx.canvasRenderer.GetMesh();ok(mesh.vertexCount>0,"Vignette geometry exists");
            var vertices=mesh.vertices;var colors=mesh.colors;Rect rect=fx.rectTransform.rect;
            ok(Enumerable.Range(0,vertices.Length).Where(i=>Mathf.Abs(vertices[i].x-rect.center.x)<rect.width*.25f&&Mathf.Abs(vertices[i].y-rect.center.y)<rect.height*.25f).All(i=>colors[i].a==0),"Center transparent, no white center");
            ok(colors.Max(c=>c.a)<=.181f,"Subtle red alpha cap");ok(colors.Any(c=>c.a>.1f&&c.r>c.b),"Red edge color");
            float t=fx.Elapsed;g.TryDamage("repeat");ok(fx.Elapsed==t&&g.Health==hp-g.settings.damage,"Invulnerability does not restart vignette");fx.Advance(.7f);Canvas.ForceUpdateCanvases();ok(!fx.Active&&fx.canvasRenderer.GetMesh().vertexCount==0,"Fade removes geometry");g.TryDamage("still invulnerable");ok(!fx.Active,"No vignette for extra invulnerable contact");
            g.Begin();g.Collect(g.entities.First(e=>e.kind==FastingTopDownEntity.Kind.Shield));hp=g.Health;g.TryDamage("block");ok(g.Health==hp&&g.ShieldCount==0,"Shield prevents health loss");ok(fx.CurrentKind==FastingHitVignette.HitKind.Shield,"Shield uses blue instead of red");fx.Advance(.1f);Canvas.ForceUpdateCanvases();ok(fx.canvasRenderer.GetMesh().colors.Where(c=>c.a>0).All(c=>c.b>c.r&&c.a<=.12f+1f/255f),"Blue alpha/color correct");t=fx.Elapsed;g.TryDamage("block repeat");ok(fx.Elapsed==t,"Protected contact does not restart blue");
            g.Begin();g.settings.damage=0;g.TryDamage("zero damage");ok(!fx.Active,"No red for zero health loss");g.settings.damage=damage;
            ok(!fx.raycastTarget,"Overlay does not consume input");var canvas=g.heading.GetComponentInParent<Canvas>();ok(fx.transform.GetSiblingIndex()<canvas.transform.Find("PortraitCard").GetSiblingIndex(),"HUD drawn above vignette");
            fx.Play(false);g.Begin();ok(!fx.Active,"Restart clears vignette");return n+" checks passed: normal/damage/shield/invulnerable rules, transparency, opacity, HUD and reset.";
        }
        finally{g.settings.damage=damage;g.Begin();g.enabled=true;}
    }
}
