using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using CrossTheLine.Minigames;
public static class TestFastingRevisionNov12
{
    static void Set(object o,string n,object v)=>o.GetType().GetField(n,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(o,v);
    public static string Run()
    {
        var g=UnityEngine.Object.FindAnyObjectByType<FastingTopDownGame>();if(!Application.isPlaying||!g)throw new Exception("Play minigame");g.enabled=false;
        int checks=0;Action<bool,string> ok=(v,m)=>{if(!v)throw new Exception(m);checks++;};
        Action clean=()=>{g.Begin();Set(g,"wave",float.MaxValue);foreach(var e in g.entities)e.gameObject.SetActive(false);};
        try
        {
            clean();g.Tick(.1f,Vector2.right);ok(g.Prayer==0&&!g.Praying,"No automatic prayer");float x=g.player.position.x;
            g.Tick(.1f,Vector2.right,true);ok(Mathf.Abs(g.Prayer-.1f)<.001f,"Held prayer charges 1/sec");ok(Mathf.Abs((g.player.position.x-x)-.225f)<.001f,"Manual movement halved");
            g.Tick(.1f,Vector2.zero,false);ok(Mathf.Abs(g.Prayer-.1f)<.001f,"Release stops prayer");
            var well=g.entities.First(e=>e.kind==FastingTopDownEntity.Kind.Water&&e.stage==0);well.gameObject.SetActive(true);well.SetUsed(false);well.transform.position=g.player.position;
            g.UseWater();ok(!g.HasWater,"Water consumed");g.Tick(.01f,Vector2.zero);ok(g.HasWater&&well.consumed,"Well supplies once");g.UseWater();g.Tick(.01f,Vector2.zero);ok(!g.HasWater,"No refill from exhausted well");ok(well.GetComponentsInChildren<SpriteRenderer>().Any(r=>r.color.r<.5f),"Used well darkened");
            clean();g.TryDamage("test");float health=g.Health;ok(!g.TryDamage("again")&&g.Health==health,"Invulnerable grace blocks second hit");
            g.Tick(.01f,Vector2.zero);var mpb=new MaterialPropertyBlock();g.playerBody.GetPropertyBlock(mpb);ok(mpb.GetFloat("_Flash")==1,"White flash active");ok(g.playerBody.sharedMaterial.shader.name=="CrossTheLine/FastingWhiteFlash","Flash shader installed");
            ok(g.screenHitFeedback.shieldColor.b>g.screenHitFeedback.shieldColor.r,"Blue shield");ok(g.screenHitFeedback.damageEdgeColor.a<=.15f,"Subtle damage red");
            g.hitFeedback.Play(FastingTopDownHazard.Kind.Sand);g.hitFeedback.Advance(.55f);ok(g.hitFeedback.Active&&g.playerBody.transform.localPosition.y>.9f,"Higher airborne for sand");g.hitFeedback.Advance(.3f);ok(g.hitFeedback.Active,"Airborne lasts beyond old .65 sec");g.hitFeedback.Advance(.3f);ok(!g.hitFeedback.Active,"Airborne ends");
            foreach(float maximum in new[]{Mathf.Sqrt(2),2f})
            {
                var h=UnityEngine.Object.Instantiate(g.desertArt.quicksandPrefab);h.kind=FastingTopDownHazard.Kind.Quicksand;h.ConfigureLanes(1,3);h.quicksandMaximumScale=maximum;h.transform.position=new Vector3(0,20);h.UpdateApproach(Vector2.zero);var initial=h.size;h.transform.position=Vector3.up;h.UpdateApproach(Vector2.zero);ok(Mathf.Abs(h.size.x/initial.x-maximum)<.001f,"Quicksand width matches scale");ok(Mathf.Abs(h.size.x*h.size.y/(initial.x*initial.y)-maximum*maximum)<.001f,"Quicksand area 2/4 cells");UnityEngine.Object.Destroy(h.gameObject);
            }
            foreach(int direction in new[]{-1,1})foreach(float y in new[]{0f,1.15f,1.35f})
            {
                var h=UnityEngine.Object.Instantiate(g.desertArt.sandstormPrefab);h.kind=FastingTopDownHazard.Kind.Sand;h.ConfigureLanes(1,3);h.warning=0;h.velocity=new Vector2(direction*100,0);h.transform.position=new Vector3(-direction*5,y);bool hit=h.Advance(.1f,Vector2.zero,Vector2.zero,out _);ok(hit==(y<1.3f),"Side sand swept ellipse direction="+direction+" offset="+y);UnityEngine.Object.Destroy(h.gameObject);
            }
            clean();int count=0;var clearMethod=typeof(FastingTopDownGame).GetMethod("PickupPathClear",BindingFlags.Instance|BindingFlags.NonPublic);
            foreach(var e in g.entities.Where(e=>e.stage==0&&e.kind!=FastingTopDownEntity.Kind.Cover)){e.SetUsed(false);e.gameObject.SetActive(true);}
            for(int i=0;i<100;i++)g.SpawnWave();foreach(var h in g.Hazards){ok((bool)clearMethod.Invoke(g,new object[]{h}),"Spawn trajectory avoids item reservations");count++;}ok(count>5,"Random hazards still spawn with items reserved");
            g.Refresh();UnityEngine.Object.FindAnyObjectByType<FastingPortraitHUD>().Refresh();ok(g.status.text==""&&g.help.text=="","No debug text");var hud=UnityEngine.Object.FindAnyObjectByType<FastingPortraitHUD>();ok(hud.prayerText.text==""&&hud.healthText.text=="","Gauge has no numbers");
            return checks+" checks passed; "+count+" random hazards accepted; side tornado hits center at vertical offset <=1.30 world units (elliptical boundary).";
        }
        finally{g.Begin();g.enabled=true;}
    }
}
