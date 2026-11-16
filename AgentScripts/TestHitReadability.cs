using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using CrossTheLine.Minigames;
public static class TestHitReadability
{
    public static string Run()
    {
        var g=UnityEngine.Object.FindAnyObjectByType<FastingTopDownGame>();if(!Application.isPlaying||!g)throw new Exception("Play minigame required");
        g.enabled=false;int checks=0;
        Action<bool,string> ok=(b,s)=>{if(!b)throw new Exception(s);checks++;};
        var apply=typeof(FastingTopDownGame).GetMethod("ApplyEffect",BindingFlags.NonPublic|BindingFlags.Instance);
        try
        {
            foreach(FastingTopDownHazard.Kind kind in Enum.GetValues(typeof(FastingTopDownHazard.Kind)))
            {
                g.Begin();g.TryDamage(kind.ToString());apply.Invoke(g,new object[]{kind});
                g.screenHitFeedback.Advance(.1f);g.hitVignette.Advance(.1f);Canvas.ForceUpdateCanvases();
                var debris=g.screenHitFeedback.canvasRenderer.GetMesh();var vignette=g.hitVignette.canvasRenderer.GetMesh();
                ok(debris.vertexCount>0&&debris.colors.Max(c=>c.a)<.28f,"Translucent debris "+kind);
                ok(vignette.colors.Max(c=>c.a)>.3f,"Readable vignette peak "+kind);
                ok(g.hitFeedback.Active,"Local impact preserved "+kind);
                g.screenHitFeedback.Advance(.23f);g.hitVignette.Advance(.23f);Canvas.ForceUpdateCanvases();
                ok(g.screenHitFeedback.canvasRenderer.GetMesh().vertexCount==0,"Debris clears by .33s "+kind);
                ok(g.hitVignette.canvasRenderer.GetMesh().colors.Max(c=>c.a)>.3f,"Vignette remains at .33s "+kind);
            }
            ok(g.screenHitFeedback.transform.GetSiblingIndex()<g.hitVignette.transform.GetSiblingIndex(),"Vignette above debris");
            return checks+" combined feedback checks passed across all fourteen obstacles.";
        }
        finally{g.Begin();g.enabled=true;}
    }
}
