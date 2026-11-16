using System;
using System.Linq;
using UnityEngine;
using CrossTheLine.Minigames;
public static class TestObstacleVignettes
{
    public static string Run()
    {
        var g=UnityEngine.Object.FindAnyObjectByType<FastingTopDownGame>();if(!Application.isPlaying||!g)throw new Exception("Play scene");g.enabled=false;var fx=g.hitVignette;int n=0;
        Action<bool,string> ok=(v,m)=>{if(!v)throw new Exception(m);n++;};
        try
        {
            var kinds=(FastingTopDownHazard.Kind[])Enum.GetValues(typeof(FastingTopDownHazard.Kind));ok(fx.obstacleStyles.Select(s=>s.obstacle).Distinct().Count()==kinds.Length,"Every obstacle configured");
            foreach(var kind in kinds)
            {
                g.Begin();float hp=g.Health;g.TryDamage(kind.ToString());ok(g.Health<hp&&fx.CurrentObstacle==kind,"Damage identifies "+kind);
                fx.Advance(.14f);Canvas.ForceUpdateCanvases();var mesh=fx.canvasRenderer.GetMesh();var vertices=mesh.vertices;var colors=mesh.colors;var r=fx.rectTransform.rect;
                ok(colors.Any(c=>c.a>0)&&colors.Max(c=>c.a)<=fx.maximumOpacity+1f/255f,"Visible but restrained "+kind);
                ok(Enumerable.Range(0,vertices.Length).Where(i=>Mathf.Abs(vertices[i].x-r.center.x)<r.width*.25f&&Mathf.Abs(vertices[i].y-r.center.y)<r.height*.25f).All(i=>colors[i].a==0),"Center clear "+kind);
                float elapsed=fx.Elapsed;g.TryDamage(kind.ToString());ok(fx.Elapsed==elapsed,"No invulnerable repeat "+kind);
                fx.Advance(fx.CurrentDuration);ok(!fx.Active,"Style expires "+kind);
                g.Begin();g.Collect(g.entities.First(e=>e.kind==FastingTopDownEntity.Kind.Shield));g.TryDamage(kind.ToString());ok(fx.CurrentKind==FastingHitVignette.HitKind.Shield&&fx.CurrentObstacle==null,"Shield overrides style "+kind);
            }
            g.Begin();g.TryDamage("unknown cause");ok(fx.Active&&fx.CurrentObstacle==null,"Unknown cause uses common red");g.Begin();ok(!fx.Active,"Reset clears style");return n+" checks passed across "+kinds.Length+" obstacle styles.";
        }
        finally{g.Begin();g.enabled=true;}
    }
}
