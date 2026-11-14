using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using CrossTheLine.Minigames;
public static class CaptureFastingEffectsCatalogue
{
    public static async Task<string> Run()
    {
        var g=UnityEngine.Object.FindAnyObjectByType<FastingTopDownGame>();if(!Application.isPlaying||!g)throw new Exception("Play minigame");
        string dir="Docs/ArtReferences/EffectsCatalogue-20261112";Directory.CreateDirectory(dir);float time=Time.timeScale;g.enabled=false;
        string[] names={"01-Rock","02-Cactus","03-Sandstorm","04-Quicksand","05-WaterBoost","06-ShieldActive","07-ShieldBreak","08-Invulnerability","09-Fever"};
        try
        {
            for(int n=0;n<names.Length;n++)
            {
                g.Begin();g.enabled=false;Time.timeScale=0;foreach(var e in g.entities)e.gameObject.SetActive(false);
                if(n<4)
                {
                    var kinds=new[]{FastingTopDownHazard.Kind.Rock,FastingTopDownHazard.Kind.Cactus,FastingTopDownHazard.Kind.Sand,FastingTopDownHazard.Kind.Quicksand};
                    g.TryDamage(kinds[n].ToString());typeof(FastingTopDownGame).GetMethod("ApplyEffect",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(g,new object[]{kinds[n]});
                    g.hitFeedback.Advance(n==2?.675f:n==3?1f:.2f);g.screenHitFeedback.Advance(.18f);
                }
                if(n==4){g.UseWater();g.playerAura.Advance(.2f);}
                if(n==5||n==6)
                {
                    g.Collect(g.entities.First(e=>e.kind==FastingTopDownEntity.Kind.Shield));g.playerAura.Advance(.1f);
                    if(n==6){g.TryDamage("Rock");g.playerAura.Advance(.15f);g.screenHitFeedback.Advance(.15f);}
                }
                if(n==7){g.TryDamage("Rock");g.Tick(.01f,Vector2.zero);g.screenHitFeedback.Play(FastingTopDownHazard.Kind.Rock);}
                if(n==8){var flame=g.entities.First(e=>e.kind==FastingTopDownEntity.Kind.Flame);for(int i=0;i<3;i++)g.Collect(flame);}
                var hud=UnityEngine.Object.FindAnyObjectByType<FastingPortraitHUD>();g.Refresh();hud.Refresh();Canvas.ForceUpdateCanvases();
                await Task.Delay(300);ScreenCapture.CaptureScreenshot(dir+"/"+names[n]+".png");await Task.Delay(350);
            }
            return "Captured nine current effect states without changing scene assets: "+dir;
        }
        finally{Time.timeScale=time;g.Begin();g.enabled=true;}
    }
}
