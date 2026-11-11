using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using CrossTheLine.Minigames;
public static class TestFastingScreenFeedback
{
    public static async Task<string> Run()
    {
        var g=UnityEngine.Object.FindAnyObjectByType<FastingTopDownGame>();
        if(!Application.isPlaying||!g||!g.screenHitFeedback)throw new Exception("Play minigame with screen feedback");
        var fx=g.screenHitFeedback;int checks=0;g.enabled=false;fx.enabled=false;
        // Disabling a Graphic hides it, so keep it enabled and freeze its timing via timeScale.
        fx.enabled=true;float previous=Time.timeScale;Time.timeScale=0;
        Action<bool,string> check=(ok,msg)=>{if(!ok)throw new Exception(msg);checks++;};
        try
        {
            foreach(var kind in new[]{FastingTopDownHazard.Kind.Rock,FastingTopDownHazard.Kind.Cactus,FastingTopDownHazard.Kind.Sand,FastingTopDownHazard.Kind.Quicksand})
            {
                g.Begin();foreach(var e in g.entities)e.gameObject.SetActive(false);g.SpawnWave();var h=g.Hazards.Last();h.kind=kind;h.velocity=Vector2.zero;h.transform.position=g.player.position;
                var root=g.player.position;float hp=g.Health;g.Tick(.01f,Vector2.zero);
                check(fx.Active&&g.hitFeedback.Active,"Both screen and character feedback on actual collision: "+kind);
                check(g.Health==hp-1&&!fx.raycastTarget,"Damage and nonblocking input: "+kind);
                fx.Advance(fx.effectDuration*.35f);Canvas.ForceUpdateCanvases();
                var mesh=fx.canvasRenderer.GetMesh();check(mesh&&mesh.vertexCount>0,"Visible UI geometry: "+kind);
                Rect r=fx.rectTransform.rect;
                check(mesh.vertices.All(v=>Mathf.Abs(v.x-r.center.x)>r.width*.25f||Mathf.Abs(v.y-r.center.y)>r.height*.25f),"Central area clear: "+kind);
                fx.Advance(fx.effectDuration);Canvas.ForceUpdateCanvases();
                check(!fx.Active&&fx.canvasRenderer.GetMesh().vertexCount==0,"Cleared geometry: "+kind);
                await Task.Yield();
            }
            g.Begin();foreach(var e in g.entities)e.gameObject.SetActive(false);g.Collect(g.entities.First(e=>e.kind==FastingTopDownEntity.Kind.Shield));
            float health=g.Health;g.SpawnWave();var block=g.Hazards.Last();block.velocity=Vector2.zero;block.transform.position=g.player.position;g.Tick(.01f,Vector2.zero);
            check(fx.Active&&!g.hitFeedback.Active&&g.Health==health&&!g.Shielded,"Shield screen flash without damaging character effect");
            g.Begin();check(!fx.Active,"Restart clears screen");
            return checks+" checks passed";
        }
        finally{Time.timeScale=previous;g.Begin();g.enabled=true;}
    }
}
