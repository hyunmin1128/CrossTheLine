using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using CrossTheLine.Minigames;
public static class TestFastingWellsAndHUD
{
    public static string Run()
    {
        var g=UnityEngine.Object.FindAnyObjectByType<FastingTopDownGame>();if(!Application.isPlaying||!g)throw new Exception("Play minigame");g.enabled=false;int n=0;
        Action<bool,string> ok=(v,m)=>{if(!v)throw new Exception(m);n++;};var a=g.entities.First(e=>e.kind==FastingTopDownEntity.Kind.Water&&e.stage==0);var b=g.entities.First(e=>e.kind==FastingTopDownEntity.Kind.Water&&e.stage==1);
        try
        {
            g.Begin();foreach(var e in g.entities)e.gameObject.SetActive(false);a.gameObject.SetActive(true);a.SetUsed(false);a.transform.position=g.player.position;
            g.Tick(.01f,Vector2.zero);ok(!a.consumed,"Full canteen does not waste well");
            g.UseWater();g.Tick(.01f,Vector2.zero);ok(g.HasWater&&a.consumed,"First well fills once");g.UseWater();g.Tick(.01f,Vector2.zero);ok(!g.HasWater,"Same placement cannot refill twice");
            b.stage=0;b.gameObject.SetActive(true);b.SetUsed(false);b.transform.position=g.player.position+Vector3.left*3;g.player.position=b.transform.position;g.Tick(.01f,Vector2.zero);ok(g.HasWater&&b.consumed,"Another independent well works");ok(a.consumed,"Old well remains used");
            g.UseWater();a.transform.position=g.player.position+Vector3.down*10;
            typeof(FastingTopDownGame).GetMethod("RecycleMap",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(g,null);
            ok(!a.consumed,"New recycled placement resets usage");ok(a.GetComponentsInChildren<SpriteRenderer>().All(r=>r.color==Color.white),"New well normal color");
            g.player.position=a.transform.position;typeof(FastingTopDownGame).GetField("cameraY",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(g,a.transform.position.y);g.Tick(.01f,Vector2.zero);ok(g.HasWater&&a.consumed,"Next well placement can refill");
            var card=g.items.transform.parent as RectTransform;ok(card.name=="PortraitCard"&&Mathf.Abs(card.localScale.x-.5f)<.001f,"Items follow existing half-size card");
            Canvas.ForceUpdateCanvases();var corners=new Vector3[4];g.items.rectTransform.GetWorldCorners(corners);ok(corners.All(p=>card.rect.Contains(card.InverseTransformPoint(p))),"Item label inside card bounds");
            g.Begin();g.enabled=false;System.IO.Directory.CreateDirectory("Docs/QA/FastingTopDown/WellsHUD-20261112");ScreenCapture.CaptureScreenshot("Docs/QA/FastingTopDown/WellsHUD-20261112/Aligned.png");
            return n+" checks passed";
        }
        finally{b.stage=1;g.Begin();g.enabled=true;}
    }
}
