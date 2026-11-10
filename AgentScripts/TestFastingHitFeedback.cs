using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using CrossTheLine.Minigames;
public static class TestFastingHitFeedback
{
    public static async Task<string> Run()
    {
        var g=UnityEngine.Object.FindAnyObjectByType<FastingTopDownGame>();
        if(!Application.isPlaying||!g||!g.hitFeedback)throw new Exception("Play minigame with feedback");
        var fx=g.hitFeedback;g.enabled=false;fx.enabled=false;int checks=0;
        Action<bool,string> check=(ok,msg)=>{if(!ok)throw new Exception(msg);checks++;};
        string dir="Docs/QA/FastingTopDown/HitFeedback-20261110";Directory.CreateDirectory(dir);
        try
        {
            foreach(var kind in new[]{FastingTopDownHazard.Kind.Rock,FastingTopDownHazard.Kind.Cactus,FastingTopDownHazard.Kind.Sand,FastingTopDownHazard.Kind.Quicksand})
            {
                g.Begin();foreach(var e in g.entities)e.gameObject.SetActive(false);
                Vector3 basePos=g.playerBody.transform.localPosition,baseScale=g.playerBody.transform.localScale;
                g.SpawnWave();var h=g.Hazards.Last();h.kind=kind;h.velocity=Vector2.zero;h.transform.position=g.player.position;
                float hp=g.Health;g.Tick(.01f,Vector2.zero);
                check(g.Health==hp-1,"Collision damage "+kind);check(fx.Active,"Collision triggers feedback "+kind);
                var root=g.player.position;fx.Advance(fx.duration*.4f);
                check(g.player.position==root,"Gameplay root stays grounded "+kind);
                check(fx.GetComponent<MeshFilter>().sharedMesh.vertexCount==80,"Particles exist "+kind);
                if(kind==FastingTopDownHazard.Kind.Sand)check(g.playerBody.transform.localPosition.y>basePos.y,"Sand lift");
                if(kind==FastingTopDownHazard.Kind.Quicksand)check(g.playerBody.transform.localPosition.y<basePos.y&&g.playerBody.transform.localScale.y<baseScale.y,"Quicksand sink");
                ScreenCapture.CaptureScreenshot(dir+"/"+kind+".png");await Task.Delay(350);
                fx.Advance(fx.duration);
                check(!fx.Active&&g.playerBody.transform.localPosition==basePos&&g.playerBody.transform.localScale==baseScale,"Artwork restored "+kind);
            }
            g.Begin();g.Collect(g.entities.First(e=>e.kind==FastingTopDownEntity.Kind.Shield));foreach(var e in g.entities)e.gameObject.SetActive(false);
            g.SpawnWave();var shieldHazard=g.Hazards.Last();shieldHazard.velocity=Vector2.zero;shieldHazard.transform.position=g.player.position;
            float shieldHealth=g.Health;g.Tick(.01f,Vector2.zero);
            check(!g.Shielded&&g.Health==shieldHealth&&!fx.Active,"Shield blocks damage and damaging feedback");
            return checks+" checks passed; four actual-collision screenshots saved to "+dir;
        }
        finally{fx.Clear();fx.enabled=true;g.Begin();g.enabled=true;}
    }
}
