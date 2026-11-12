using System;
using System.Reflection;
using UnityEngine;
using CrossTheLine.Minigames;
public static class TestFastingPrayerPreparation
{
    public static string Run()
    {
        var g=UnityEngine.Object.FindAnyObjectByType<FastingTopDownGame>();if(!Application.isPlaying||!g)throw new Exception("Play minigame");g.enabled=false;int n=0;
        Action<bool,string> ok=(v,m)=>{if(!v)throw new Exception(m);n++;};
        Action clean=()=>{g.Begin();typeof(FastingTopDownGame).GetField("wave",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(g,float.MaxValue);foreach(var e in g.entities)e.gameObject.SetActive(false);};
        try
        {
            clean();g.Tick(.1f,Vector2.right,true);ok(g.Prayer==0,"No charge before preparation");ok(Mathf.Abs(g.player.position.x-.0675f)<.0001f,"Preparation movement already 15 percent");
            g.Tick(.1f,Vector2.zero,true);ok(g.Prayer==0,"No charge at .2 seconds");g.Tick(.1f,Vector2.zero,true);ok(g.Prayer<.0001f,"Preparation interval not awarded as prayer");
            g.Tick(.1f,Vector2.zero,true);ok(Mathf.Abs(g.Prayer-.1f)<.0001f&&g.Praying,"Charge after preparation at 1/sec");
            float x=g.player.position.x;g.Tick(.1f,Vector2.right,false);ok(Mathf.Abs(g.player.position.x-x-.45f)<.0001f&&!g.Praying,"Release immediately restores speed");ok(Mathf.Abs(g.Prayer-.1f)<.0001f,"Release stops prayer");
            g.Tick(.1f,Vector2.zero,true);ok(Mathf.Abs(g.Prayer-.1f)<.0001f,"Repress starts new preparation");
            clean();for(int i=0;i<10;i++){g.Tick(.1f,Vector2.zero,true);g.Tick(.05f,Vector2.zero,false);}ok(g.Prayer==0,"Rapid tapping cannot bypass preparation");
            clean();for(int i=0;i<10;i++)g.Tick(.1f,Vector2.zero,true);ok(Mathf.Abs(g.Prayer-.7f)<.0001f,"1 second hold gives .7 prayer");
            g.TryDamage("test");g.Tick(.1f,Vector2.zero,true);ok(!g.Praying,"Damage restarts preparation");
            clean();g.Tick(.1f,Vector2.zero,true);g.Begin();foreach(var e in g.entities)e.gameObject.SetActive(false);g.Tick(.1f,Vector2.zero,true);ok(g.Prayer==0&&!g.Praying,"Restart clears preparation");
            return n+" checks passed: preparation, 15% speed, immediate release, tapping, damage and restart.";
        }
        finally{g.Begin();g.enabled=true;}
    }
}
